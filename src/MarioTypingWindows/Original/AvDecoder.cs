using System.Buffers.Binary;

namespace MarioTypingWindows.Original;

internal sealed record OriginalAvMovie(
    VgaPalette Palette,
    IReadOnlyList<byte[]> Frames,
    byte[] UnsignedPcmAudio)
{
    public const int Width = 224;
    public const int Height = 144;
    public const int FramesPerSecond = 15;
    public const int AudioSampleRate = 10_980;
}

/// <summary>
/// Native decoder for the enhanced CD edition's custom AV format. The format
/// stores two 224-pixel pages side-by-side in a 448-byte framebuffer. Each
/// frame contains a 28x18 table of either raw 8x8 blocks or motion-copy source
/// offsets, followed by one 732-byte unsigned PCM chunk.
/// </summary>
internal static class AvDecoder
{
    private const int BlocksAcross = 28;
    private const int BlocksDown = 18;
    private const int BlockSize = 8;
    private const int FramebufferStride = OriginalAvMovie.Width * 2;
    private const int FramebufferLength = FramebufferStride * OriginalAvMovie.Height;
    private const int TableEntryCount = BlocksAcross * BlocksDown;
    private const int TableLength = TableEntryCount * sizeof(ushort);
    private const int PaletteLength = 768;
    private const int AudioChunkLength = 732;
    private const int InitialAudioChunkCount = 14;
    private const int FirstVideoOffset = PaletteLength + InitialAudioChunkCount * AudioChunkLength;

    public static OriginalAvMovie Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < FirstVideoOffset)
        {
            throw new InvalidDataException("AV file is missing its palette or initial audio queue.");
        }

        var palette = VgaPalette.Decode(data[..PaletteLength]);
        var framebuffer = new byte[FramebufferLength];
        var frames = new List<byte[]>();
        var audio = new List<byte>(data.Length / 8);
        audio.AddRange(data.Slice(PaletteLength, InitialAudioChunkCount * AudioChunkLength).ToArray());
        var table = new ushort[TableEntryCount];

        var sourceOffset = FirstVideoOffset;
        var currentPage = 1;
        while (sourceOffset < data.Length)
        {
            EnsureRange(data, sourceOffset, TableLength);
            if (BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sourceOffset, 2)) == 0xfffe)
            {
                break;
            }

            var rawBlockCount = 0;
            for (var index = 0; index < table.Length; index++)
            {
                var entry = BinaryPrimitives.ReadUInt16LittleEndian(
                    data.Slice(sourceOffset + index * sizeof(ushort), sizeof(ushort)));
                table[index] = entry;
                if ((entry & 0xff00) == 0xff00)
                {
                    rawBlockCount++;
                }
            }

            var rawOffset = sourceOffset + TableLength;
            var audioOffset = checked(rawOffset + rawBlockCount * BlockSize * BlockSize);
            EnsureRange(data, audioOffset, AudioChunkLength);

            currentPage = 1 - currentPage;
            var destinationPageX = currentPage * OriginalAvMovie.Width;

            // The original decoder performs this in two passes so that motion
            // records may refer to raw blocks installed by the same packet.
            var nextRawOffset = rawOffset;
            for (var block = 0; block < table.Length; block++)
            {
                if ((table[block] & 0xff00) != 0xff00)
                {
                    continue;
                }

                var destination = BlockDestination(block, destinationPageX);
                for (var row = 0; row < BlockSize; row++)
                {
                    data.Slice(nextRawOffset + row * BlockSize, BlockSize)
                        .CopyTo(framebuffer.AsSpan(destination + row * FramebufferStride, BlockSize));
                }
                nextRawOffset += BlockSize * BlockSize;
            }

            for (var block = 0; block < table.Length; block++)
            {
                var motionSource = table[block];
                if (motionSource == ushort.MaxValue)
                {
                    continue;
                }

                var destination = BlockDestination(block, destinationPageX);
                EnsureFramebufferBlock(motionSource);
                for (var row = 0; row < BlockSize; row++)
                {
                    // Copy in the same forward order as the original routine;
                    // overlapping motion records intentionally retain that behavior.
                    for (var column = 0; column < BlockSize; column++)
                    {
                        framebuffer[destination + row * FramebufferStride + column] =
                            framebuffer[motionSource + row * FramebufferStride + column];
                    }
                }
            }

            var pixels = new byte[OriginalAvMovie.Width * OriginalAvMovie.Height];
            for (var row = 0; row < OriginalAvMovie.Height; row++)
            {
                framebuffer.AsSpan(
                        row * FramebufferStride + destinationPageX,
                        OriginalAvMovie.Width)
                    .CopyTo(pixels.AsSpan(row * OriginalAvMovie.Width));
            }
            frames.Add(pixels);
            audio.AddRange(data.Slice(audioOffset, AudioChunkLength).ToArray());
            sourceOffset = audioOffset + AudioChunkLength;
        }

        if (sourceOffset != data.Length)
        {
            throw new InvalidDataException($"AV decoder left {data.Length - sourceOffset} trailing bytes.");
        }
        if (frames.Count == 0)
        {
            throw new InvalidDataException("AV file contains no video frames.");
        }

        return new OriginalAvMovie(palette, frames, audio.ToArray());
    }

    private static int BlockDestination(int block, int pageX)
    {
        var blockX = block % BlocksAcross;
        var blockY = block / BlocksAcross;
        return blockY * BlockSize * FramebufferStride + pageX + blockX * BlockSize;
    }

    private static void EnsureFramebufferBlock(int offset)
    {
        var lastByte = offset + (BlockSize - 1) * FramebufferStride + BlockSize;
        if (offset < 0 || lastByte > FramebufferLength)
        {
            throw new InvalidDataException($"AV motion block points outside the framebuffer: {offset}.");
        }
    }

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException("AV packet extends beyond the source stream.");
        }
    }
}
