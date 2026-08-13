namespace MarioTypingWindows.Original;

internal static class LzssDecoder
{
    private const int RingSize = 4096;
    private const int InitialCursor = 0xFEE;

    public static byte[] Decode(ReadOnlySpan<byte> source, int expectedLength, out int consumed)
    {
        if (expectedLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedLength));
        }

        var ring = new byte[RingSize];
        Array.Fill(ring, (byte)0x20);
        var output = new byte[expectedLength];
        var sourceIndex = 0;
        var outputIndex = 0;
        var cursor = InitialCursor;
        var flags = 0;

        while (outputIndex < output.Length)
        {
            flags >>= 1;
            if ((flags & 0x100) == 0)
            {
                EnsureAvailable(source, sourceIndex, 1);
                flags = source[sourceIndex++] | 0xFF00;
            }

            if ((flags & 1) != 0)
            {
                EnsureAvailable(source, sourceIndex, 1);
                var value = source[sourceIndex++];
                output[outputIndex++] = value;
                ring[cursor] = value;
                cursor = (cursor + 1) & 0xFFF;
                continue;
            }

            EnsureAvailable(source, sourceIndex, 2);
            var low = source[sourceIndex++];
            var high = source[sourceIndex++];
            var offset = low | ((high & 0xF0) << 4);
            var length = (high & 0x0F) + 3;
            for (var index = 0; index < length && outputIndex < output.Length; index++)
            {
                var value = ring[(offset + index) & 0xFFF];
                output[outputIndex++] = value;
                ring[cursor] = value;
                cursor = (cursor + 1) & 0xFFF;
            }
        }

        consumed = sourceIndex;
        return output;
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> source, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > source.Length)
        {
            throw new InvalidDataException("Truncated Interplay LZSS stream.");
        }
    }
}
