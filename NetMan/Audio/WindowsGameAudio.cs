using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NetMan.Audio;

// Each voice owns its native buffers until WinMM returns them. No callbacks or
// background threads touch gameplay state, and overlapping effects can finish.
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsGameAudio : IDisposable
{
  private readonly Dictionary<GameSound, short[]> _samples = Enum.GetValues<GameSound>()
    .ToDictionary(sound => sound, GameSounds.Create);

  private readonly List<Voice> _voices = [];

  private bool _disposed;

  public void Dispose()
  {
    Stop();
    _disposed = true;
  }

  public void Play(GameSound sound)
  {
    if (_disposed)
      return;

    Update();
    if (sound == GameSound.Died)
      Stop();

    if (_voices.Count >= 8)
      return;

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
      return;

    var voice = new Voice(device, _samples[sound]);
    if (waveOutPrepareHeader(device, voice.Header, (uint)sizeof(WaveHeader)) != 0)
    {
      voice.Dispose();
      return;
    }

    if (waveOutWrite(device, voice.Header, (uint)sizeof(WaveHeader)) != 0)
    {
      voice.Dispose();
      return;
    }

    _voices.Add(voice);
  }

  public void Stop()
  {
    foreach (var voice in _voices)
      voice.Dispose();
    _voices.Clear();
  }

  public void Update()
  {
    for (int i = _voices.Count - 1; i >= 0; i--)
    {
      if ((_voices[i].Header->Flags & 1) == 0) // WHDR_DONE
        continue;
      _voices[i].Dispose();
      _voices.RemoveAt(i);
    }
  }

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutClose(nint device);

  [DllImport("winmm.dll", ExactSpelling = true)]
  private static extern uint waveOutOpen(out nint device, uint deviceId, ref WaveFormat format,
    nuint callback, nuint instance, uint flags);

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
