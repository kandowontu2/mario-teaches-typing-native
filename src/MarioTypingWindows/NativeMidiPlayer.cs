using System.Diagnostics;
using System.Runtime.InteropServices;
using MarioTypingWindows.Original;

namespace MarioTypingWindows;

/// <summary>
/// Sends decoded original XMIDI events directly to the Windows MIDI mapper.
/// No emulator, external player, extracted MIDI file, or temporary file is used.
/// </summary>
internal sealed class NativeMidiPlayer : IDisposable
{
    private const uint MidiMapper = unchecked((uint)-1);
    private readonly object gate = new();
    private IntPtr output;
    private System.Threading.Timer? timer;
    private TrackState? music;
    private TrackState? effect;

    public void PlayMusic(OriginalXmidiSequence sequence, bool loop)
    {
        lock (gate)
        {
            if (!EnsureOutput())
            {
                return;
            }
            StopTrack(music);
            music = new TrackState(sequence, loop);
        }
    }

    public void PlayEffect(OriginalXmidiSequence sequence)
    {
        lock (gate)
        {
            if (!EnsureOutput())
            {
                return;
            }
            StopTrack(effect);
            effect = new TrackState(sequence, loop: false);
        }
    }

    public void StopMusic()
    {
        lock (gate)
        {
            StopTrack(music);
            music = null;
        }
    }

    public void StopEffects()
    {
        lock (gate)
        {
            StopTrack(effect);
            effect = null;
        }
    }

    public void Stop()
    {
        lock (gate)
        {
            music = null;
            effect = null;
            if (output != IntPtr.Zero)
            {
                midiOutReset(output);
            }
        }
    }

    private bool EnsureOutput()
    {
        if (output != IntPtr.Zero)
        {
            return true;
        }
        if (midiOutOpen(out output, MidiMapper, IntPtr.Zero, IntPtr.Zero, 0) != 0)
        {
            output = IntPtr.Zero;
            return false;
        }
        timer = new System.Threading.Timer(Process, null, dueTime: 0, period: 4);
        return true;
    }

    private void Process(object? state)
    {
        lock (gate)
        {
            if (output == IntPtr.Zero)
            {
                return;
            }
            var now = Stopwatch.GetTimestamp();
            Advance(ref music, now);
            Advance(ref effect, now);
        }
    }

    private void Advance(ref TrackState? track, long now)
    {
        if (track is null)
        {
            return;
        }

        var elapsedTicks = checked((int)Math.Min(
            int.MaxValue,
            (now - track.StartedAt) * XmidiDecoder.TicksPerSecond / Stopwatch.Frequency));
        while (track.EventIndex < track.Sequence.Events.Count &&
               track.Sequence.Events[track.EventIndex].Tick <= elapsedTicks)
        {
            Send(track, track.Sequence.Events[track.EventIndex++]);
        }

        if (track.EventIndex < track.Sequence.Events.Count)
        {
            return;
        }

        if (!track.Loop)
        {
            StopTrack(track);
            track = null;
            return;
        }

        StopTrack(track);
        var completedLoops = Math.Max(1, elapsedTicks / Math.Max(1, track.Sequence.DurationTicks));
        track.StartedAt += checked(
            completedLoops * track.Sequence.DurationTicks * Stopwatch.Frequency /
            XmidiDecoder.TicksPerSecond);
        track.EventIndex = 0;
    }

    private void Send(TrackState track, OriginalXmidiEvent item)
    {
        var messageType = item.Status & 0xF0;
        var key = ((item.Status & 0x0F) << 8) | item.Data1;
        if (messageType == 0x90 && item.Data2 != 0)
        {
            track.ActiveNotes.Add(key);
        }
        else if (messageType == 0x80 || messageType == 0x90)
        {
            track.ActiveNotes.Remove(key);
        }

        var message = (uint)(item.Status | item.Data1 << 8 | item.Data2 << 16);
        midiOutShortMsg(output, message);
    }

    private void StopTrack(TrackState? track)
    {
        if (track is null || output == IntPtr.Zero)
        {
            return;
        }
        foreach (var key in track.ActiveNotes)
        {
            var channel = (byte)(key >> 8);
            var note = (byte)(key & 0xFF);
            midiOutShortMsg(output, (uint)(0x80 | channel | note << 8));
        }
        track.ActiveNotes.Clear();
    }

    public void Dispose()
    {
        lock (gate)
        {
            music = null;
            effect = null;
            timer?.Dispose();
            timer = null;
            if (output != IntPtr.Zero)
            {
                midiOutReset(output);
                midiOutClose(output);
                output = IntPtr.Zero;
            }
        }
    }

    private sealed class TrackState(OriginalXmidiSequence sequence, bool loop)
    {
        public OriginalXmidiSequence Sequence { get; } = sequence;
        public bool Loop { get; } = loop;
        public long StartedAt { get; set; } = Stopwatch.GetTimestamp();
        public int EventIndex { get; set; }
        public HashSet<int> ActiveNotes { get; } = [];
    }

    [DllImport("winmm.dll")]
    private static extern uint midiOutOpen(
        out IntPtr handle,
        uint deviceId,
        IntPtr callback,
        IntPtr instance,
        uint flags);

    [DllImport("winmm.dll")]
    private static extern uint midiOutShortMsg(IntPtr handle, uint message);

    [DllImport("winmm.dll")]
    private static extern uint midiOutReset(IntPtr handle);

    [DllImport("winmm.dll")]
    private static extern uint midiOutClose(IntPtr handle);
}
