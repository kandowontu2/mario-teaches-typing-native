using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Drawing.Imaging;

namespace MarioTypingWindows.Original;

internal sealed record VgaPalette(int[] Argb)
{
    public static VgaPalette Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 768)
        {
            throw new InvalidDataException("A VGA palette must contain 768 bytes.");
        }

        var colors = new int[256];
        for (var index = 0; index < colors.Length; index++)
        {
            var red = ExpandDac(data[index * 3]);
            var green = ExpandDac(data[index * 3 + 1]);
            var blue = ExpandDac(data[index * 3 + 2]);
            colors[index] = Color.FromArgb(255, red, green, blue).ToArgb();
        }
        return new VgaPalette(colors);
    }

    public byte FindNearest(byte red, byte green, byte blue)
    {
        var bestIndex = 0;
        var bestDistance = long.MaxValue;
        for (var index = 0; index < Argb.Length; index++)
        {
            var color = Color.FromArgb(Argb[index]);
            var redDelta = color.R - red;
            var greenDelta = color.G - green;
            var blueDelta = color.B - blue;
            var distance = redDelta * redDelta + greenDelta * greenDelta + blueDelta * blueDelta;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }
        return (byte)bestIndex;
    }

    public VgaPalette WithRange(VgaPalette source, int start, int count)
    {
        if (start < 0 || count < 0 || start + count > Argb.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }
        var colors = (int[])Argb.Clone();
        Array.Copy(source.Argb, start, colors, start, count);
        return new VgaPalette(colors);
    }

    private static int ExpandDac(byte value)
    {
        var sixBit = Math.Min(value, (byte)63);
        return (sixBit << 2) | (sixBit >> 4);
    }
}

internal sealed record OriginalAnimationFrame(int X, int Y, OriginalSprite Sprite);

internal static class BackAnimationDecoder
{
    public static IReadOnlyList<byte[]> DecodeBan(ReadOnlySpan<byte> data, ReadOnlySpan<byte> initialPixels)
    {
        if (initialPixels.Length != CrxImage.Width * CrxImage.Height)
        {
            throw new ArgumentException("A BAN animation requires a 320x200 initial framebuffer.", nameof(initialPixels));
        }

        var framebuffer = initialPixels.ToArray();
        var frames = new List<byte[]>();
        var sourceIndex = 0;
        while (sourceIndex + 4 <= data.Length)
        {
            var destination = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sourceIndex, 2));
            var packetCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sourceIndex + 2, 2));
            if (destination == ushort.MaxValue)
            {
                break;
            }
            if (destination == 0 && packetCount == 0)
            {
                sourceIndex += 4;
                // RunBackAnim treats an empty record as a timed hold.  Keep a
                // framebuffer snapshot so ANI character frames and callback
                // sound cues remain synchronized with the BAN stream.
                frames.Add((byte[])framebuffer.Clone());
                continue;
            }

            EnsureAnimationRange(data, sourceIndex, 12);
            sourceIndex += 12; // destination, packet count, and the original 8-byte dirty rectangle
            var destinationIndex = (int)destination;
            for (var packet = 0; packet < packetCount; packet++)
            {
                EnsureAnimationRange(data, sourceIndex, 1);
                var control = data[sourceIndex++];
                if (control < 0x80)
                {
                    var length = control + 1;
                    EnsureAnimationRange(data, sourceIndex, length);
                    EnsureFrameRange(framebuffer, destinationIndex, length);
                    data.Slice(sourceIndex, length).CopyTo(framebuffer.AsSpan(destinationIndex, length));
                    sourceIndex += length;
                    destinationIndex += length;
                }
                else if (control == 0x80)
                {
                    EnsureAnimationRange(data, sourceIndex, 2);
                    destinationIndex += BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(sourceIndex, 2));
                    sourceIndex += 2;
                    if (destinationIndex > framebuffer.Length)
                    {
                        throw new InvalidDataException("BAN skip packet extends beyond the framebuffer.");
                    }
                }
                else
                {
                    var length = 257 - control;
                    EnsureAnimationRange(data, sourceIndex, 1);
                    EnsureFrameRange(framebuffer, destinationIndex, length);
                    var value = data[sourceIndex++];
                    if (value != 0)
                    {
                        framebuffer.AsSpan(destinationIndex, length).Fill(value);
                    }
                    destinationIndex += length;
                }
            }
            frames.Add((byte[])framebuffer.Clone());
        }

        if (sourceIndex > data.Length)
        {
            throw new InvalidDataException("BAN decoder read beyond the source stream.");
        }
        return frames;
    }

    public static IReadOnlyList<OriginalAnimationFrame> DecodeAni(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            throw new InvalidDataException("ANI file is truncated.");
        }

        var firstShapeOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(4, 4)));
        if (firstShapeOffset <= 0 || firstShapeOffset > data.Length || firstShapeOffset % 8 != 0)
        {
            throw new InvalidDataException("ANI file has an invalid shape-table offset.");
        }

        var frames = new List<OriginalAnimationFrame>();
        for (var tableOffset = 0; tableOffset + 8 <= firstShapeOffset; tableOffset += 8)
        {
            var x = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(tableOffset, 2));
            var y = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(tableOffset + 2, 2));
            var shapeOffset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(tableOffset + 4, 4)));
            if (shapeOffset == 0)
            {
                break;
            }

            EnsureAnimationRange(data, shapeOffset, 8);
            var spriteX = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(shapeOffset, 2));
            var spriteY = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(shapeOffset + 2, 2));
            var width = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(shapeOffset + 4, 2));
            var height = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(shapeOffset + 6, 2));
            var pixelLength = checked(width * height);
            EnsureAnimationRange(data, shapeOffset + 8, pixelLength);
            frames.Add(new OriginalAnimationFrame(
                x,
                y,
                new OriginalSprite(spriteX, spriteY, width, height, data.Slice(shapeOffset + 8, pixelLength).ToArray())));
        }
        return frames;
    }

    private static void EnsureAnimationRange(ReadOnlySpan<byte> source, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > source.Length)
        {
            throw new InvalidDataException("Animation record extends outside its source file.");
        }
    }

    private static void EnsureFrameRange(byte[] framebuffer, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > framebuffer.Length)
        {
            throw new InvalidDataException("BAN packet extends outside the 320x200 framebuffer.");
        }
    }
}

internal sealed record CrxImage(VgaPalette Palette, byte[] Pixels)
{
    public const int Width = 320;
    public const int Height = 200;

    public static CrxImage Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length <= 768)
        {
            throw new InvalidDataException("CRX file is missing its compressed image stream.");
        }

        var palette = VgaPalette.Decode(data[..768]);
        var pixels = LzssDecoder.Decode(data[768..], Width * Height, out var consumed);
        if (consumed != data.Length - 768)
        {
            throw new InvalidDataException(
                $"CRX stream has {data.Length - 768 - consumed} unexpected trailing bytes.");
        }
        return new CrxImage(palette, pixels);
    }
}

internal static class CastleBackgroundDecoder
{
    public const int SceneCount = 4;
    public const int SceneTop = 26;
    public const int SceneHeight = 129;
    public const int ScenePixelCount = CrxImage.Width * SceneHeight;

    public static IReadOnlyList<byte[]> Decode(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> basePixels)
    {
        if (basePixels.Length != CrxImage.Width * CrxImage.Height)
        {
            throw new ArgumentException(
                "A castle background requires a 320x200 base framebuffer.",
                nameof(basePixels));
        }

        var scenes = new List<byte[]>(SceneCount);
        var sourceIndex = 0;
        for (var scene = 0; scene < SceneCount; scene++)
        {
            EnsureRange(data, sourceIndex, sizeof(int));
            var packedLength = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(sourceIndex, sizeof(int)));
            sourceIndex += sizeof(int);
            if (packedLength <= 0)
            {
                throw new InvalidDataException($"Castle BGS scene {scene} has an invalid packed length.");
            }
            EnsureRange(data, sourceIndex, packedLength);

            var scenePixels = LzssDecoder.Decode(
                data.Slice(sourceIndex, packedLength),
                ScenePixelCount,
                out var consumed);
            if (consumed != packedLength)
            {
                throw new InvalidDataException(
                    $"Castle BGS scene {scene} left {packedLength - consumed} packed bytes unread.");
            }

            var framebuffer = basePixels.ToArray();
            scenePixels.CopyTo(framebuffer.AsSpan(SceneTop * CrxImage.Width, ScenePixelCount));
            scenes.Add(framebuffer);
            sourceIndex += packedLength;
        }

        // The original loader reads exactly four length-prefixed buffers. The
        // shipped file terminates them with a zero DWORD sentinel.
        if (sourceIndex + sizeof(int) == data.Length &&
            BinaryPrimitives.ReadInt32LittleEndian(data[sourceIndex..]) == 0)
        {
            sourceIndex += sizeof(int);
        }
        if (sourceIndex != data.Length)
        {
            throw new InvalidDataException(
                $"Castle BGS stream has {data.Length - sourceIndex} unexpected trailing bytes.");
        }
        return scenes;
    }

    public static byte[] ComposeScrollFrame(
        ReadOnlySpan<byte> from,
        ReadOnlySpan<byte> to,
        int completedFrames)
    {
        if (from.Length != CrxImage.Width * CrxImage.Height ||
            to.Length != CrxImage.Width * CrxImage.Height)
        {
            throw new ArgumentException("Castle scroll framebuffers must be 320x200.");
        }

        var frames = Math.Clamp(completedFrames, 0, 40);
        if (frames == 0)
        {
            return from.ToArray();
        }
        if (frames == 40)
        {
            return to.ToArray();
        }

        var result = from.ToArray();
        var shift = frames * 8;
        var remaining = CrxImage.Width - shift;
        for (var row = SceneTop; row < SceneTop + SceneHeight; row++)
        {
            var rowOffset = row * CrxImage.Width;
            from.Slice(rowOffset + shift, remaining)
                .CopyTo(result.AsSpan(rowOffset, remaining));
            to.Slice(rowOffset, shift)
                .CopyTo(result.AsSpan(rowOffset + remaining, shift));
        }
        return result;
    }

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException("Castle BGS record extends outside its source file.");
        }
    }
}

internal sealed record OriginalSprite(ushort X, ushort Y, ushort Width, ushort Height, byte[] Pixels)
{
    public int PixelCount => Width * Height;
}

internal sealed class OriginalSpriteBank
{
    private readonly Dictionary<string, OriginalSprite> sprites;

    private OriginalSpriteBank(Dictionary<string, OriginalSprite> sprites)
    {
        this.sprites = sprites;
    }

    public IReadOnlyCollection<string> Names => sprites.Keys;

    public OriginalSprite this[string name] => sprites.TryGetValue(name, out var sprite)
        ? sprite
        : throw new KeyNotFoundException($"Original sprite is missing: {name}");

    public bool TryGet(string name, out OriginalSprite sprite) => sprites.TryGetValue(name, out sprite!);

    public static OriginalSpriteBank DecodeGso(ReadOnlySpan<byte> data, string indexText)
    {
        var result = new Dictionary<string, OriginalSprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, offset) in ParseIndex(indexText))
        {
            var header = ReadHeader(data, offset);
            var length = checked(header.Width * header.Height);
            EnsureRange(data, offset + 8, length);
            result.Add(name, new OriginalSprite(
                header.X,
                header.Y,
                header.Width,
                header.Height,
                data.Slice(offset + 8, length).ToArray()));
        }
        return new OriginalSpriteBank(result);
    }

    public static OriginalSpriteBank DecodeGco(ReadOnlySpan<byte> data, string indexText)
    {
        var result = new Dictionary<string, OriginalSprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, offset) in ParseIndex(indexText))
        {
            var header = ReadHeader(data, offset);
            EnsureRange(data, offset + 8, 4);
            var compressedLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 8, 2));
            EnsureRange(data, offset + 12, compressedLength);
            var expectedLength = checked(header.Width * header.Height);
            var pixels = LzssDecoder.Decode(
                data.Slice(offset + 12, compressedLength),
                expectedLength,
                out var consumed);
            if (consumed != compressedLength)
            {
                throw new InvalidDataException($"Compressed sprite {name} did not consume its complete record.");
            }
            result.Add(name, new OriginalSprite(header.X, header.Y, header.Width, header.Height, pixels));
        }
        return new OriginalSpriteBank(result);
    }

    private static IEnumerable<(string Name, int Offset)> ParseIndex(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            var name = line[..separator].Trim();
            var offsetText = line[(separator + 1)..].Trim().Split(' ', 2)[0];
            if (!int.TryParse(offsetText, NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
            {
                throw new InvalidDataException($"Invalid GSK/GCK offset: {line}");
            }
            yield return (name, offset);
        }
    }

    private static (ushort X, ushort Y, ushort Width, ushort Height) ReadHeader(
        ReadOnlySpan<byte> data,
        int offset)
    {
        EnsureRange(data, offset, 8);
        return (
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 2, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 4, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset + 6, 2)));
    }

    private static void EnsureRange(ReadOnlySpan<byte> data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException("Sprite record extends outside its source bank.");
        }
    }
}

internal sealed class NativeRaster : IDisposable
{
    private readonly int[] argb;
    private Bitmap? bitmap;

    public NativeRaster(int width = 320, int height = 200)
    {
        Width = width;
        Height = height;
        argb = new int[checked(width * height)];
    }

    public int Width { get; }
    public int Height { get; }

    public void CopyFrom(CrxImage image)
    {
        if (Width != CrxImage.Width || Height != CrxImage.Height)
        {
            throw new InvalidOperationException("CRX image dimensions do not match this framebuffer.");
        }
        for (var index = 0; index < argb.Length; index++)
        {
            argb[index] = image.Palette.Argb[image.Pixels[index]];
        }
    }

    public void FillRectangle(int x, int y, int width, int height, int color)
    {
        var left = Math.Max(0, x);
        var top = Math.Max(0, y);
        var right = Math.Min(Width, x + width);
        var bottom = Math.Min(Height, y + height);
        for (var row = top; row < bottom; row++)
        {
            Array.Fill(argb, color, row * Width + left, Math.Max(0, right - left));
        }
    }

    public void DrawIndexedImage(
        ReadOnlySpan<byte> pixels,
        int sourceWidth,
        int sourceHeight,
        VgaPalette palette,
        int x,
        int y)
    {
        if (pixels.Length != checked(sourceWidth * sourceHeight))
        {
            throw new ArgumentException("Indexed image dimensions do not match its pixel data.", nameof(pixels));
        }

        for (var sourceY = 0; sourceY < sourceHeight; sourceY++)
        {
            var destinationY = y + sourceY;
            if ((uint)destinationY >= Height)
            {
                continue;
            }
            for (var sourceX = 0; sourceX < sourceWidth; sourceX++)
            {
                var destinationX = x + sourceX;
                if ((uint)destinationX < Width)
                {
                    argb[destinationY * Width + destinationX] =
                        palette.Argb[pixels[sourceY * sourceWidth + sourceX]];
                }
            }
        }
    }

    public void DrawIndexedHorizontalWrap(
        ReadOnlySpan<byte> pixels,
        int sourceWidth,
        int sourceY,
        int height,
        VgaPalette palette,
        int offset)
    {
        if (sourceWidth <= 0 || sourceY < 0 || height < 0 ||
            checked((sourceY + height) * sourceWidth) > pixels.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceY));
        }
        var normalizedOffset = (offset % sourceWidth + sourceWidth) % sourceWidth;
        for (var row = 0; row < height && sourceY + row < Height; row++)
        {
            var sourceRow = (sourceY + row) * sourceWidth;
            var destinationRow = (sourceY + row) * Width;
            for (var x = 0; x < Width; x++)
            {
                var sourceX = (x + normalizedOffset) % sourceWidth;
                argb[destinationRow + x] = palette.Argb[pixels[sourceRow + sourceX]];
            }
        }
    }

    public void DrawSprite(
        OriginalSprite sprite,
        VgaPalette palette,
        int x,
        int y,
        byte transparentIndex = 0,
        bool mirrorHorizontally = false)
    {
        x += sprite.X;
        y += sprite.Y;
        for (var sourceY = 0; sourceY < sprite.Height; sourceY++)
        {
            var destinationY = y + sourceY;
            if ((uint)destinationY >= Height)
            {
                continue;
            }

            for (var sourceX = 0; sourceX < sprite.Width; sourceX++)
            {
                var destinationX = x + sourceX;
                if ((uint)destinationX >= Width)
                {
                    continue;
                }

                var actualSourceX = mirrorHorizontally ? sprite.Width - 1 - sourceX : sourceX;
                var colorIndex = sprite.Pixels[sourceY * sprite.Width + actualSourceX];
                if (colorIndex != transparentIndex)
                {
                    argb[destinationY * Width + destinationX] = palette.Argb[colorIndex];
                }
            }
        }
    }

    public void DrawSpriteRemapped(
        OriginalSprite sprite,
        VgaPalette palette,
        int x,
        int y,
        byte foregroundIndex,
        byte? secondaryIndex = null)
    {
        x += sprite.X;
        y += sprite.Y;
        for (var sourceY = 0; sourceY < sprite.Height; sourceY++)
        {
            var destinationY = y + sourceY;
            if ((uint)destinationY >= Height)
            {
                continue;
            }
            for (var sourceX = 0; sourceX < sprite.Width; sourceX++)
            {
                var destinationX = x + sourceX;
                if ((uint)destinationX >= Width)
                {
                    continue;
                }

                var sourceIndex = sprite.Pixels[sourceY * sprite.Width + sourceX];
                if (sourceIndex == 0)
                {
                    continue;
                }
                var mappedIndex = sourceIndex == 2 && secondaryIndex.HasValue
                    ? secondaryIndex.Value
                    : foregroundIndex;
                argb[destinationY * Width + destinationX] = palette.Argb[mappedIndex];
            }
        }
    }

    public Bitmap Snapshot()
    {
        bitmap ??= new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(
            new Rectangle(0, 0, Width, Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            if (data.Stride == Width * sizeof(int))
            {
                Marshal.Copy(argb, 0, data.Scan0, argb.Length);
            }
            else
            {
                for (var row = 0; row < Height; row++)
                {
                    Marshal.Copy(argb, row * Width, data.Scan0 + row * data.Stride, Width);
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return bitmap;
    }

    public void Dispose() => bitmap?.Dispose();
}

internal static class OriginalGraphicChecks
{
    public const string MainMenuPixelsSha256 =
        "66A95F5D496FAAB8BC7F0C6DE15E7ECA4393851FA0E976B12B7A954346DB5310";

    public static string PixelHash(CrxImage image) =>
        Convert.ToHexString(SHA256.HashData(image.Pixels));
}
