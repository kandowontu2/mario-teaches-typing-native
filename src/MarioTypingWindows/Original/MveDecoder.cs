using System.Buffers.Binary;
using System.Text;

namespace MarioTypingWindows.Original;

internal sealed record OriginalMveFrame(byte[] Pixels, VgaPalette Palette);

internal sealed record OriginalMveMovie(
    int Width,
    int Height,
    long FrameDurationMicroseconds,
    IReadOnlyList<OriginalMveFrame> Frames,
    int AudioSampleRate,
    short AudioBitsPerSample,
    short AudioChannels,
    byte[] PcmAudio);

/// <summary>
/// Native decoder for the format-0x10 Interplay MVE streams used by the
/// enhanced CD edition. These particular movies use 8x8 raw/motion blocks,
/// palettized 320x200 video, and uncompressed unsigned 8-bit PCM.
/// </summary>
internal static class MveDecoder
{
    private static readonly byte[] Signature =
        Encoding.ASCII.GetBytes("Interplay MVE File\x1a\0");

    private const int FileHeaderLength = 26;
    private const int BlockSize = 8;

    public static OriginalMveMovie Decode(ReadOnlySpan<byte> data)
    {
        ValidateHeader(data);

        var width = 0;
        var height = 0;
        long frameDurationMicroseconds = 0;
        var audioRate = 0;
        short audioBits = 0;
        short audioChannels = 0;
        var paletteBytes = new byte[768];
        var frames = new List<OriginalMveFrame>();
        var audio = new List<byte>(data.Length / 6);
        byte[]? currentDecode = null;
        byte[]? previousDecode = null;
        byte[]? lastOutput = null;

        var sourceOffset = FileHeaderLength;
        while (sourceOffset < data.Length)
        {
            EnsureRange(data, sourceOffset, 4);
            var chunkLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sourceOffset, 2));
            var chunkEnd = checked(sourceOffset + 4 + chunkLength);
            EnsureRange(data, sourceOffset + 4, chunkLength);

            ReadOnlySpan<byte> skipMap = default;
            ReadOnlySpan<byte> decodingMap = default;
            ReadOnlySpan<byte> videoData = default;
            var opcodeOffset = sourceOffset + 4;
            while (opcodeOffset < chunkEnd)
            {
                EnsureRange(data, opcodeOffset, 4);
                var opcodeLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(opcodeOffset, 2));
                var opcode = data[opcodeOffset + 2];
                var version = data[opcodeOffset + 3];
                var payloadOffset = opcodeOffset + 4;
                EnsureRange(data, payloadOffset, opcodeLength);
                var payload = data.Slice(payloadOffset, opcodeLength);

                switch (opcode)
                {
                    case 0x02:
                        RequireLength(payload, 6, opcode);
                        frameDurationMicroseconds = checked(
                            (long)BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]) *
                            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2)));
                        break;
                    case 0x03:
                        if (version > 1 || payload.Length is < 6 or > 10)
                        {
                            throw new InvalidDataException("MVE has an invalid audio initialization packet.");
                        }
                        var flags = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2));
                        audioRate = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
                        audioChannels = checked((short)((flags & 1) + 1));
                        audioBits = checked((short)((((flags >> 1) & 1) + 1) * 8));
                        if (version == 1 && (flags & 4) != 0)
                        {
                            throw new NotSupportedException("Compressed Interplay DPCM is not used by this CD but is not decoded yet.");
                        }
                        break;
                    case 0x05:
                        RequireLength(payload, 8, opcode);
                        width = checked(BinaryPrimitives.ReadUInt16LittleEndian(payload[..2]) * BlockSize);
                        height = checked(BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)) * BlockSize);
                        if (version != 2 || payload[6] != 0 || width <= 0 || height <= 0)
                        {
                            throw new NotSupportedException("Only the CD's 8-bit MVE format-0x10 video is supported.");
                        }
                        currentDecode = new byte[checked(width * height)];
                        previousDecode = new byte[currentDecode.Length];
                        lastOutput = new byte[currentDecode.Length];
                        break;
                    case 0x08:
                        AppendAudioFrame(payload, audio);
                        break;
                    case 0x09:
                        AppendSilenceFrame(payload, audio, audioBits);
                        break;
                    case 0x0c:
                        ApplyPalette(payload, paletteBytes);
                        break;
                    case 0x0e:
                        skipMap = payload;
                        break;
                    case 0x0f:
                        decodingMap = payload;
                        break;
                    case 0x10:
                        videoData = payload;
                        break;
                }

                opcodeOffset = checked(payloadOffset + opcodeLength);
            }

            if (opcodeOffset != chunkEnd)
            {
                throw new InvalidDataException("MVE opcode records do not end on the chunk boundary.");
            }

            if (!videoData.IsEmpty)
            {
                if (currentDecode is null || previousDecode is null || lastOutput is null ||
                    width == 0 || height == 0 || skipMap.IsEmpty || decodingMap.IsEmpty)
                {
                    throw new InvalidDataException("MVE video frame is missing initialization or decoding maps.");
                }

                var output = DecodeFormat10Frame(
                    videoData,
                    skipMap,
                    decodingMap,
                    width,
                    height,
                    currentDecode,
                    previousDecode,
                    lastOutput,
                    frames.Count != 0);
                frames.Add(new OriginalMveFrame(output, VgaPalette.Decode(paletteBytes)));
                lastOutput = output;
                (currentDecode, previousDecode) = (previousDecode, currentDecode);
            }

            sourceOffset = chunkEnd;
        }

        if (frames.Count == 0 || width == 0 || height == 0)
        {
            throw new InvalidDataException("MVE contains no video frames.");
        }
        if (sourceOffset != data.Length)
        {
            throw new InvalidDataException("MVE has trailing data outside its final chunk.");
        }

        return new OriginalMveMovie(
            width,
            height,
            frameDurationMicroseconds,
            frames,
            audioRate,
            audioBits,
            audioChannels,
            audio.ToArray());
    }

    private static byte[] DecodeFormat10Frame(
        ReadOnlySpan<byte> videoData,
        ReadOnlySpan<byte> skipMap,
        ReadOnlySpan<byte> decodingMap,
        int width,
        int height,
        byte[] currentDecode,
        byte[] previousDecode,
        byte[] lastOutput,
        bool hasPreviousOutput)
    {
        if (videoData.Length < 14)
        {
            throw new InvalidDataException("MVE format-0x10 video packet is truncated.");
        }
        var blockWidth = BinaryPrimitives.ReadUInt16LittleEndian(videoData.Slice(8, 2));
        var blockHeight = BinaryPrimitives.ReadUInt16LittleEndian(videoData.Slice(10, 2));
        if (blockWidth * BlockSize != width || blockHeight * BlockSize != height)
        {
            throw new InvalidDataException("MVE frame dimensions changed unexpectedly.");
        }

        var changes = DecodeChangeMap(skipMap, decodingMap, blockWidth * blockHeight);
        var rawOffset = 14;

        // The original decoder performs raw installs before motion copies.
        for (var block = 0; block < changes.Length; block++)
        {
            if (!changes[block].Changed || changes[block].Opcode != 0)
            {
                continue;
            }
            if (rawOffset + BlockSize * BlockSize > videoData.Length)
            {
                throw new InvalidDataException("MVE raw block extends past its video packet.");
            }
            CopyRawBlock(videoData.Slice(rawOffset, BlockSize * BlockSize), currentDecode, width, blockWidth, block);
            rawOffset += BlockSize * BlockSize;
        }

        for (var block = 0; block < changes.Length; block++)
        {
            var change = changes[block];
            if (!change.Changed || change.Opcode == 0)
            {
                continue;
            }
            var unsignedOpcode = unchecked((ushort)change.Opcode);
            if (change.Opcode < 0)
            {
                var offset = unsignedOpcode - 0xc000;
                CopyMotionBlock(previousDecode, currentDecode, width, height, blockWidth, block, offset);
            }
            else
            {
                var offset = unsignedOpcode - 0x4000;
                CopyMotionBlock(currentDecode, currentDecode, width, height, blockWidth, block, offset);
            }
        }

        var output = new byte[checked(width * height)];
        for (var block = 0; block < changes.Length; block++)
        {
            var source = changes[block].Changed ? currentDecode : lastOutput;
            if (changes[block].Changed || hasPreviousOutput)
            {
                CopySamePositionBlock(source, output, width, blockWidth, block);
            }
        }
        return output;
    }

    private static BlockChange[] DecodeChangeMap(
        ReadOnlySpan<byte> skipMap,
        ReadOnlySpan<byte> decodingMap,
        int blockCount)
    {
        if (skipMap.Length < 2)
        {
            throw new InvalidDataException("MVE skip map is empty.");
        }
        var result = new BlockChange[blockCount];
        var skipOffset = 2;
        var decodingOffset = 0;
        var skip = BinaryPrimitives.ReadInt16LittleEndian(skipMap[..2]);
        for (var block = 0; block < blockCount; block++)
        {
            while (skip <= 0)
            {
                if (skip != short.MinValue && skip != 0)
                {
                    if (decodingOffset + 2 > decodingMap.Length)
                    {
                        throw new InvalidDataException("MVE decoding map ended before its changed blocks.");
                    }
                    result[block] = new BlockChange(
                        true,
                        BinaryPrimitives.ReadInt16LittleEndian(decodingMap.Slice(decodingOffset, 2)));
                    decodingOffset += 2;
                    break;
                }
                if (skipOffset + 2 > skipMap.Length)
                {
                    throw new InvalidDataException("MVE skip map ended before the framebuffer.");
                }
                skip = BinaryPrimitives.ReadInt16LittleEndian(skipMap.Slice(skipOffset, 2));
                skipOffset += 2;
            }
            skip = unchecked((short)(skip * 2));
        }
        return result;
    }

    private static void CopyRawBlock(
        ReadOnlySpan<byte> raw,
        byte[] destination,
        int width,
        int blockWidth,
        int block)
    {
        var x = block % blockWidth * BlockSize;
        var y = block / blockWidth * BlockSize;
        for (var row = 0; row < BlockSize; row++)
        {
            raw.Slice(row * BlockSize, BlockSize)
                .CopyTo(destination.AsSpan((y + row) * width + x, BlockSize));
        }
    }

    private static void CopyMotionBlock(
        byte[] source,
        byte[] destination,
        int width,
        int height,
        int blockWidth,
        int block,
        int linearOffset)
    {
        var x = block % blockWidth * BlockSize;
        var y = block / blockWidth * BlockSize;
        var deltaX = linearOffset % width;
        var deltaY = linearOffset / width;
        var unwrappedX = x + deltaX;
        var sourceX = unwrappedX;
        var sourceY = y + deltaY;
        if (unwrappedX >= width)
        {
            sourceX -= width;
            sourceY++;
        }
        else if (unwrappedX < 0)
        {
            sourceX += width;
            sourceY--;
        }
        if (sourceX < 0 || sourceX + BlockSize > width || sourceY < 0 || sourceY + BlockSize > height)
        {
            throw new InvalidDataException("MVE motion block points outside its decode framebuffer.");
        }

        var destinationOffset = y * width + x;
        var sourceOffset = sourceY * width + sourceX;
        for (var row = 0; row < BlockSize; row++)
        {
            for (var column = 0; column < BlockSize; column++)
            {
                destination[destinationOffset + row * width + column] =
                    source[sourceOffset + row * width + column];
            }
        }
    }

    private static void CopySamePositionBlock(
        byte[] source,
        byte[] destination,
        int width,
        int blockWidth,
        int block)
    {
        var x = block % blockWidth * BlockSize;
        var y = block / blockWidth * BlockSize;
        for (var row = 0; row < BlockSize; row++)
        {
            source.AsSpan((y + row) * width + x, BlockSize)
                .CopyTo(destination.AsSpan((y + row) * width + x, BlockSize));
        }
    }

    private static void ApplyPalette(ReadOnlySpan<byte> payload, byte[] palette)
    {
        RequireLengthAtLeast(payload, 4, 0x0c);
        var first = BinaryPrimitives.ReadUInt16LittleEndian(payload[..2]);
        var count = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2));
        var colorBytes = checked(count * 3);
        var requiredLength = checked(4 + colorBytes);
        if (first + count > 256 || payload.Length < requiredLength || payload.Length > requiredLength + 1)
        {
            throw new InvalidDataException("MVE palette packet has invalid bounds.");
        }
        payload.Slice(4, colorBytes).CopyTo(palette.AsSpan(first * 3, colorBytes));
    }

    private static void AppendAudioFrame(ReadOnlySpan<byte> payload, List<byte> audio)
    {
        RequireLengthAtLeast(payload, 6, 0x08);
        if ((BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)) & 1) == 0)
        {
            return;
        }
        var sampleBytes = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
        if (sampleBytes != payload.Length - 6)
        {
            throw new InvalidDataException("MVE PCM frame length does not match its header.");
        }
        audio.AddRange(payload[6..].ToArray());
    }

    private static void AppendSilenceFrame(ReadOnlySpan<byte> payload, List<byte> audio, short bits)
    {
        RequireLength(payload, 6, 0x09);
        if ((BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)) & 1) == 0)
        {
            return;
        }
        var sampleBytes = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
        var silence = bits == 8 ? (byte)0x80 : (byte)0;
        for (var index = 0; index < sampleBytes; index++)
        {
            audio.Add(silence);
        }
    }

    private static void ValidateHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < FileHeaderLength || !data[..Signature.Length].SequenceEqual(Signature) ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(20, 2)) != 0x001a ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(22, 2)) != 0x0100 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(24, 2)) != 0x1133)
        {
            throw new InvalidDataException("Invalid Interplay MVE header.");
        }
    }

    private static void RequireLength(ReadOnlySpan<byte> payload, int length, byte opcode)
    {
        if (payload.Length != length)
        {
            throw new InvalidDataException($"MVE opcode 0x{opcode:x2} has an invalid length.");
        }
    }

    private static void RequireLengthAtLeast(ReadOnlySpan<byte> payload, int length, byte opcode)
    {
        if (payload.Length < length)
        {
            throw new InvalidDataException($"MVE opcode 0x{opcode:x2} is truncated.");
        }
    }

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException("MVE record extends beyond the source stream.");
        }
    }

    private readonly record struct BlockChange(bool Changed, short Opcode);
}
