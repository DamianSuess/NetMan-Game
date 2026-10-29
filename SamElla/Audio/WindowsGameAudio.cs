using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SamElla.Audio;

// Native audio resources belong to a dedicated worker. Device operations may
// block, so they must never run on the gameplay and input thread.
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsGameAudio : IDisposable
{
  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutClose(nint device);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutOpen(out nint device, uint deviceId, ref WaveFormat format,
    nuint callback, nuint instance, uint flags);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutPause(nint device);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutRestart(nint device);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutPrepareHeader(nint device, WaveHeader* header, uint size);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutReset(nint device);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutUnprepareHeader(nint device, WaveHeader* header, uint size);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutWrite(nint device, WaveHeader* header, uint size);

  [StructLayout(LayoutKind.Sequential, Pack = 2)]
  private struct WaveFormat
  {
    public ushort FormatTag, Channels;
    public uint SamplesPerSecond, AverageBytesPerSecond;
    public ushort BlockAlign, BitsPerSample, ExtraSize;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct WaveHeader
  {
    public nint Data;
    public uint Length, BytesRecorded;
    public nuint User;
    public uint Flags, Loops;
    public nint Next;
    public nuint Reserved;
  }

  private readonly Dictionary<GameSound, short[]> _samples = Enum.GetValues<GameSound>()
    .ToDictionary(sound => sound, GameSounds.Create);

  private readonly List<Voice> _voices = [];
  private readonly Dictionary<GameMusic, short[]> _musicSamples = Enum.GetValues<GameMusic>()
    .Where(music => music != GameMusic.None).ToDictionary(music => music, GameMusicTracks.Create);

  private bool _disposed;
  private Voice? _flamethrower;
  private bool _flamethrowerRequested;
  private Voice? _music;
  private GameMusic _requestedMusic;
  private readonly MusicPauseState _musicPause = new();

  private readonly AudioWorker _worker;
  private GameMusic _queuedMusic;
  private bool _queuedMusicPaused;
  private bool _queuedFlamethrower;
  private bool _queuedMusicEnabled = true;
  private bool _queuedEffectsEnabled = true;
  private bool _effectsEnabled = true;

  public WindowsGameAudio()
  {
    _worker = new AudioWorker(UpdateCore, StopCore);
  }

  public void Dispose()
  {
    _worker.Dispose();
    _disposed = true;
  }

  public void Play(GameSound sound)
  {
    if (sound == GameSound.Died)
      ResetQueuedState();

    _worker.Enqueue(() => PlayCore(sound));
  }

  public void SetMusic(GameMusic music)
  {
    if (_queuedMusic == music)
      return;

    _queuedMusic = music;
    _worker.Enqueue(() => SetMusicCore(music));
  }

  public void SetMusicPaused(bool paused)
  {
    if (_queuedMusicPaused == paused)
      return;

    _queuedMusicPaused = paused;
    _worker.Enqueue(() => SetMusicPausedCore(paused));
  }

  public void SetFlamethrowerActive(bool active)
  {
    if (_queuedFlamethrower == active)
      return;

    _queuedFlamethrower = active;
    _worker.Enqueue(() => SetFlamethrowerActiveCore(active));
  }

  public void Stop()
  {
    ResetQueuedState();
    _worker.Enqueue(StopCore);
  }

  public void SetEnabled(bool musicEnabled, bool effectsEnabled)
  {
    if (_queuedMusicEnabled == musicEnabled && _queuedEffectsEnabled == effectsEnabled)
      return;

    _queuedMusicEnabled = musicEnabled;
    _queuedEffectsEnabled = effectsEnabled;
    _worker.Enqueue(() => SetEnabledCore(musicEnabled, effectsEnabled));
  }

  private void SetEnabledCore(bool musicEnabled, bool effectsEnabled)
  {
    _musicPause.Enabled = musicEnabled;
    _effectsEnabled = effectsEnabled;
    if (!_effectsEnabled)
    {
      foreach (var voice in _voices)
        voice.Dispose();

      _voices.Clear();
      _musicPause.ClearNotifications();
      _flamethrower?.Dispose();
      _flamethrower = null;
    }
    else if (_flamethrowerRequested && _flamethrower is null)
      _flamethrower = CreateVoice(GameSound.Flamethrower, loop: true);

    ApplyMusicPause();
  }

  private void ResetQueuedState()
  {
    _queuedMusic = GameMusic.None;
    _queuedFlamethrower = false;
  }

  private void PlayCore(GameSound sound)
  {
    if (_disposed)
      return;

    UpdateCore();
    if (sound == GameSound.Died)
      StopCore();

    if (!_effectsEnabled || _voices.Count >= 8)
      return;

    bool notification = sound == GameSound.PowerUp;
    if (notification)
      _music?.SetPaused(true);

    var voice = CreateVoice(sound);
    if (voice is not null)
    {
      _voices.Add(voice);
      if (notification)
        _musicPause.StartNotification(voice);
    }
    else if (notification)
      ApplyMusicPause();
  }

  private void SetFlamethrowerActiveCore(bool active)
  {
    if (_disposed || active == _flamethrowerRequested)
      return;

    _flamethrowerRequested = active;
    if (active && _effectsEnabled)
      _flamethrower = CreateVoice(GameSound.Flamethrower, loop: true);
    else
    {
      _flamethrower?.Dispose();
      _flamethrower = null;
    }

  }

  private void SetMusicCore(GameMusic music)
  {
    if (_disposed || music == _requestedMusic)
      return;

    _music?.Dispose();
    _music = null;
    _requestedMusic = music;
    if (music != GameMusic.None)
    {
      _music = CreateVoice(_musicSamples[music], GameMusicTracks.Loops(music),
        paused: _musicPause.ShouldPause);
    }

  }

  private void SetMusicPausedCore(bool paused)
  {
    if (_disposed || paused == _musicPause.GamePaused)
      return;

    _musicPause.GamePaused = paused;
    ApplyMusicPause();
  }

  private void ApplyMusicPause()
  {
    _music?.SetPaused(_musicPause.ShouldPause);
  }

  private Voice? CreateVoice(GameSound sound, bool loop = false) => CreateVoice(_samples[sound], loop);

  private Voice? CreateVoice(short[] samples, bool loop = false, bool paused = false)
  {
    var format = new WaveFormat
    {
      FormatTag = 1,
      Channels = 1,
      SamplesPerSecond = GameSounds.SampleRate,
      AverageBytesPerSecond = GameSounds.SampleRate * 2,
      BlockAlign = 2,
      BitsPerSample = 16,
    };

    // Audio is optional: missing/disabled devices must not prevent playing the game.
    if (waveOutOpen(out nint device, uint.MaxValue, ref format, 0, 0, 0) != 0)
      return null;

    var voice = new Voice(device, samples);
    if (paused)
      voice.SetPaused(true);

    if (waveOutPrepareHeader(device, voice.Header, (uint)sizeof(WaveHeader)) != 0)
    {
      voice.Dispose();
      return null;
    }

    if (loop)
    {
      voice.Header->Flags |= 4 | 8; // WHDR_BEGINLOOP | WHDR_ENDLOOP
      voice.Header->Loops = uint.MaxValue;
    }

    if (waveOutWrite(device, voice.Header, (uint)sizeof(WaveHeader)) != 0)
    {
      voice.Dispose();
      return null;
    }

    return voice;
  }

  private void StopCore()
  {
    SetFlamethrowerActiveCore(false);
    SetMusicCore(GameMusic.None);
    foreach (var voice in _voices)
      voice.Dispose();
    _voices.Clear();
    _musicPause.ClearNotifications();
  }

  private void UpdateCore()
  {
    if (_music is not null && (_music.Header->Flags & 1) != 0)
    {
      _music.Dispose();
      _music = null;
      // Keep the requested track so Game Over cannot restart after completion.
    }

    for (int i = _voices.Count - 1; i >= 0; i--)
    {
      if ((_voices[i].Header->Flags & 1) == 0) // WHDR_DONE
        continue;

      bool notificationFinished = _musicPause.CompleteNotification(_voices[i]);
      _voices[i].Dispose();
      _voices.RemoveAt(i);
      if (notificationFinished)
        ApplyMusicPause();
    }

  }

  private sealed class Voice : IDisposable
  {
    private readonly nint _device;

    public Voice(nint device, short[] samples)
    {
      _device = device;
      Header = (WaveHeader*)NativeMemory.AllocZeroed((nuint)sizeof(WaveHeader));
      Header->Length = (uint)(samples.Length * sizeof(short));
      Header->Data = (nint)NativeMemory.Alloc(Header->Length);
      Marshal.Copy(samples, 0, Header->Data, samples.Length);
    }

    public WaveHeader* Header { get; }

    public void SetPaused(bool paused)
    {
      if (paused)
        waveOutPause(_device);
      else
        waveOutRestart(_device);
    }

    public void Dispose()
    {
      waveOutReset(_device);
      if ((Header->Flags & 2) != 0) // WHDR_PREPARED
        waveOutUnprepareHeader(_device, Header, (uint)sizeof(WaveHeader));
      waveOutClose(_device);
      NativeMemory.Free((void*)Header->Data);
      NativeMemory.Free(Header);
    }

  }

}
