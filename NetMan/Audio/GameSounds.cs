namespace NetMan;

internal enum GameSound
{
  Jump,
  PowerUp,
  PowerDown,
  Died,
}

// Original chiptune effects: pulse lead, triangle bass, and a quiet FM overtone.
// Samples are generated once at startup; playback never synthesizes on the game loop.
internal static class GameSounds
{
  public const int SampleRate = 44100;

  public static short[] Create(GameSound sound)
  {
    double duration = sound switch
    {
      GameSound.Jump => 0.18,
      GameSound.PowerUp => 0.48,
      GameSound.PowerDown => 0.42,
      GameSound.Died => 2.0,
      _ => throw new ArgumentOutOfRangeException(nameof(sound)),
    };

    var samples = new short[(int)(SampleRate * duration)];
    double phase = 0, bassPhase = 0;
    double[] rising = [523.25, 659.25, 783.99, 1046.50];
    double[] falling = [783.99, 659.25, 523.25, 329.63];
    double[] death = [659.25, 622.25, 587.33, 440, 349.23, 293.66, 220, 164.81];
    double noteLength = sound == GameSound.Died ? 0.25 : duration / 4;

    for (int i = 0; i < samples.Length; i++)
    {
      double time = (double)i / SampleRate;
      int note = Math.Min((int)(time / noteLength), sound == GameSound.Died ? 7 : 3);
      double frequency = sound switch
      {
        GameSound.Jump => 220 * Math.Pow(4, time / duration),
        GameSound.PowerUp => rising[note],
        GameSound.PowerDown => falling[note] * (1 - 0.15 * time / duration),
        _ => death[note],
      };

      phase = (phase + frequency / SampleRate) % 1;
      bassPhase = (bassPhase + frequency / (2 * SampleRate)) % 1;
      double pulse = phase < 0.25 ? 0.75 : -0.25;
      double triangle = 1 - 4 * Math.Abs(bassPhase - 0.5);
      double fm = Math.Sin(2 * Math.PI * phase + 1.5 * Math.Sin(4 * Math.PI * phase));
      double localTime = sound == GameSound.Jump ? time : time % noteLength;
      double localLength = sound == GameSound.Jump ? duration : noteLength;

      // Short attack/release ramps prevent clicks between notes and at either end.
      double envelope = Math.Min(1, localTime / 0.004)
        * Math.Min(1, (localLength - localTime) / 0.018)
        * Math.Min(1, (duration - time) / 0.08);
      double value = (pulse * 0.45 + triangle * 0.35 + fm * 0.20) * envelope;
      samples[i] = (short)(value * short.MaxValue * 0.45);
    }

    return samples;
  }
}
