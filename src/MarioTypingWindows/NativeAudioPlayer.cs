using System.Media;
using System.Text;
using MarioTypingWindows.Original;

namespace MarioTypingWindows;

/// <summary>
/// Plays decoded original PCM directly through the Windows multimedia stack.
/// The generated WAVE stream remains in memory; no CD, mounted image, emulator,
/// or temporary audio file is involved.
/// </summary>
internal sealed class NativeAudioPlayer : IDisposable
{
    private readonly NativeMidiPlayer midi = new();
    private Stream? waveStream;
    private SoundPlayer? player;

    public bool MusicEnabled { get; set; } = true;

    public bool SoundEffectsEnabled { get; set; } = true;

    public bool SpeechEnabled { get; set; } = true;

    public bool Enabled
    {
        get => MusicEnabled || SoundEffectsEnabled || SpeechEnabled;
        set
        {
            MusicEnabled = value;
            SoundEffectsEnabled = value;
            SpeechEnabled = value;
        }
    }

    public void PlayAv(OriginalAvMovie movie, bool loop)
    {
        if (!SpeechEnabled)
        {
            StopWave();
            return;
        }
        PlayPcmCore(
            movie.UnsignedPcmAudio,
            OriginalAvMovie.AudioSampleRate,
            bitsPerSample: 8,
            channels: 1,
            loop);
    }

    public void PlayPcm(
        ReadOnlySpan<byte> pcm,
        int sampleRate,
        short bitsPerSample,
        short channels,
        bool loop)
    {
        StopWave();
        if (!SpeechEnabled || pcm.Length == 0)
        {
            return;
        }

        PlayPcmCore(pcm, sampleRate, bitsPerSample, channels, loop);
    }

    private void PlayPcmCore(
        ReadOnlySpan<byte> pcm,
        int sampleRate,
        short bitsPerSample,
        short channels,
        bool loop)
    {
        StopWave();
        if (pcm.Length == 0)
        {
            return;
        }

        waveStream = CreateWaveStream(pcm, sampleRate, bitsPerSample, channels);
        player = new SoundPlayer(waveStream);
        player.Load();
        if (loop)
        {
            player.PlayLooping();
        }
        else
        {
            player.Play();
        }
    }

    public void PlayMusicWave(Stream embeddedWave, bool loop)
    {
        StopWave();
        if (!MusicEnabled)
        {
            embeddedWave.Dispose();
            return;
        }

        waveStream = embeddedWave;
        player = new SoundPlayer(waveStream);
        player.Load();
        if (loop)
        {
            player.PlayLooping();
        }
        else
        {
            player.Play();
        }
    }

    public void PlayMusic(OriginalXmidiSequence sequence, bool loop)
    {
        midi.StopMusic();
        if (MusicEnabled)
        {
            midi.PlayMusic(sequence, loop);
        }
    }

    public void PlayEffect(OriginalXmidiSequence sequence)
    {
        if (SoundEffectsEnabled)
        {
            midi.PlayEffect(sequence);
        }
    }

    public void StopMusic() => midi.StopMusic();

    public void StopSoundEffects()
    {
        midi.StopEffects();
    }

    public void StopSpeech() => StopWave();

    public void Stop()
    {
        StopWave();
        midi.Stop();
    }

    private void StopWave()
    {
        player?.Stop();
        player?.Dispose();
        player = null;
        waveStream?.Dispose();
        waveStream = null;
    }

    public void Dispose()
    {
        StopWave();
        midi.Dispose();
    }

    private static MemoryStream CreateWaveStream(
        ReadOnlySpan<byte> pcm,
        int sampleRate,
        short bitsPerSample,
        short channels)
    {
        var bytesPerSample = bitsPerSample / 8;
        var blockAlign = checked((short)(channels * bytesPerSample));
        var bytesPerSecond = checked(sampleRate * blockAlign);
        var stream = new MemoryStream(44 + pcm.Length);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(checked(36 + pcm.Length));
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(bytesPerSecond);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(pcm.Length);
            writer.Write(pcm);
        }
        stream.Position = 0;
        return stream;
    }
}
