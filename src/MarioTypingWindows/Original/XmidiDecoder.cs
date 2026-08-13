namespace MarioTypingWindows.Original;

internal sealed record OriginalXmidiEvent(int Tick, byte Status, byte Data1, byte Data2);

internal sealed record OriginalXmidiSequence(
    IReadOnlyList<OriginalXmidiEvent> Events,
    int DurationTicks);

/// <summary>
/// Decodes the Miles Sound System XMIDI event stream used by the original game.
/// XMIDI runs at 60 PPQN with a fixed 500,000 microsecond tempo (120 ticks/sec),
/// and note-on messages carry their own variable-length duration.
/// </summary>
internal static class XmidiDecoder
{
    public const int TicksPerSecond = 120;

    public static IReadOnlyList<OriginalXmidiSequence> Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var eventChunks = new List<(int Offset, int Length)>();
        FindEventChunks(data, 0, data.Length, eventChunks);
        if (eventChunks.Count == 0)
        {
            throw new InvalidDataException("XMIDI file contains no EVNT chunks.");
        }

        return eventChunks
            .Select(chunk => DecodeEvents(data.AsSpan(chunk.Offset, chunk.Length)))
            .ToArray();
    }

    private static void FindEventChunks(
        ReadOnlySpan<byte> data,
        int start,
        int end,
        List<(int Offset, int Length)> chunks)
    {
        var position = start;
        while (position + 8 <= end)
        {
            var id = ReadFourCc(data, position);
            var length = ReadBigEndianInt32(data, position + 4);
            var contentStart = position + 8;
            var contentEnd = checked(contentStart + length);
            if (length < 0 || contentEnd > end)
            {
                throw new InvalidDataException($"Invalid XMIDI {id} chunk length {length}.");
            }

            if (id == "EVNT")
            {
                chunks.Add((contentStart, length));
            }
            else if (id is "FORM" or "CAT " or "LIST")
            {
                if (length < 4)
                {
                    throw new InvalidDataException($"XMIDI {id} container is missing its type code.");
                }
                FindEventChunks(data, contentStart + 4, contentEnd, chunks);
            }

            position = contentStart + ((length + 1) & ~1);
        }
    }

    private static OriginalXmidiSequence DecodeEvents(ReadOnlySpan<byte> data)
    {
        var events = new List<OriginalXmidiEvent>();
        var position = 0;
        var tick = 0;
        var maximumTick = 0;

        while (position < data.Length)
        {
            while (position < data.Length && data[position] < 0x80)
            {
                tick = checked(tick + data[position++]);
            }
            if (position >= data.Length)
            {
                break;
            }

            var status = data[position++];
            var messageType = status & 0xF0;
            switch (messageType)
            {
                case 0x80:
                case 0xA0:
                case 0xB0:
                case 0xE0:
                {
                    var data1 = ReadDataByte(data, ref position);
                    var data2 = ReadDataByte(data, ref position);
                    events.Add(new OriginalXmidiEvent(tick, status, data1, data2));
                    maximumTick = Math.Max(maximumTick, tick);
                    break;
                }
                case 0x90:
                {
                    var note = ReadDataByte(data, ref position);
                    var velocity = ReadDataByte(data, ref position);
                    var duration = ReadVariableLength(data, ref position);
                    if (velocity == 0)
                    {
                        events.Add(new OriginalXmidiEvent(tick, (byte)(0x80 | (status & 0x0F)), note, 0));
                        maximumTick = Math.Max(maximumTick, tick);
                        break;
                    }

                    var noteOffTick = checked(tick + Math.Max(1, duration));
                    events.Add(new OriginalXmidiEvent(tick, status, note, velocity));
                    events.Add(new OriginalXmidiEvent(noteOffTick, (byte)(0x80 | (status & 0x0F)), note, 0));
                    maximumTick = Math.Max(maximumTick, noteOffTick);
                    break;
                }
                case 0xC0:
                case 0xD0:
                {
                    var data1 = ReadDataByte(data, ref position);
                    events.Add(new OriginalXmidiEvent(tick, status, data1, 0));
                    maximumTick = Math.Max(maximumTick, tick);
                    break;
                }
                case 0xF0:
                    if (!SkipSystemEvent(data, ref position, status))
                    {
                        return Finish(events, maximumTick);
                    }
                    break;
                default:
                    throw new InvalidDataException($"Unsupported XMIDI status 0x{status:X2}.");
            }
        }

        return Finish(events, maximumTick);
    }

    private static bool SkipSystemEvent(ReadOnlySpan<byte> data, ref int position, byte status)
    {
        switch (status)
        {
            case 0xFF:
            {
                var type = ReadDataByte(data, ref position);
                var length = ReadVariableLength(data, ref position);
                Skip(data, ref position, length);
                return type != 0x2F;
            }
            case 0xF0:
            case 0xF7:
                Skip(data, ref position, ReadVariableLength(data, ref position));
                return true;
            case 0xF2:
                Skip(data, ref position, 2);
                return true;
            case 0xF3:
                Skip(data, ref position, 1);
                return true;
            case 0xF6:
            case 0xF8:
            case 0xFA:
            case 0xFB:
            case 0xFC:
            case 0xFE:
                return true;
            default:
                throw new InvalidDataException($"Unsupported XMIDI system status 0x{status:X2}.");
        }
    }

    private static OriginalXmidiSequence Finish(List<OriginalXmidiEvent> events, int maximumTick)
    {
        var ordered = events
            .OrderBy(item => item.Tick)
            .ThenBy(item => IsNoteOff(item) ? 0 : 1)
            .ToArray();
        if (ordered.Length == 0)
        {
            throw new InvalidDataException("XMIDI EVNT chunk contains no playable events.");
        }
        return new OriginalXmidiSequence(ordered, checked(maximumTick + 1));
    }

    private static bool IsNoteOff(OriginalXmidiEvent item) =>
        (item.Status & 0xF0) == 0x80 ||
        (item.Status & 0xF0) == 0x90 && item.Data2 == 0;

    private static byte ReadDataByte(ReadOnlySpan<byte> data, ref int position)
    {
        if (position >= data.Length)
        {
            throw new InvalidDataException("Unexpected end of XMIDI event data.");
        }
        return data[position++];
    }

    private static int ReadVariableLength(ReadOnlySpan<byte> data, ref int position)
    {
        var value = 0;
        byte current;
        do
        {
            current = ReadDataByte(data, ref position);
            value = checked((value << 7) | (current & 0x7F));
        } while ((current & 0x80) != 0);
        return value;
    }

    private static void Skip(ReadOnlySpan<byte> data, ref int position, int length)
    {
        if (length < 0 || position + length > data.Length)
        {
            throw new InvalidDataException("XMIDI event payload extends past its EVNT chunk.");
        }
        position += length;
    }

    private static string ReadFourCc(ReadOnlySpan<byte> data, int offset) =>
        new([(char)data[offset], (char)data[offset + 1], (char)data[offset + 2], (char)data[offset + 3]]);

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> data, int offset) =>
        checked((int)(((uint)data[offset] << 24) |
                      ((uint)data[offset + 1] << 16) |
                      ((uint)data[offset + 2] << 8) |
                      data[offset + 3]));
}
