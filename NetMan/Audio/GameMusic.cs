namespace NetMan.Audio;

internal enum GameMusic
{
  None,
  Title,
  GameOver,
  Credits,
}

// Original scores rendered as layered FM lead, triangle bass, chords, and drums.
internal static class GameMusicTracks
{
  public static GameMusic ForScreen(bool titleActive, Game game)
  {
    if (titleActive)
      return GameMusic.Title;

    if (game.Lives == 0 && game.AwaitingRestart)
      return GameMusic.GameOver;

    if (game.Won && game.Credits is not null)
      return GameMusic.Credits;

    return GameMusic.None;
  }

  public static bool Loops(GameMusic music) => music is GameMusic.Title or GameMusic.Credits;

  public static short[] Create(GameMusic music)
  {
    bool march = music == GameMusic.GameOver;
    int[] melody;
    int[] roots;
    double tempo;
    switch (music)
    {
      case GameMusic.Title:
        tempo = 120;
        roots = [48, 53, 55, 48, 57, 53, 55, 48];
        melody = [72, 76, 79, 76, 74, 76, 79, 0, 77, 81, 84, 81, 79, 77, 76, 0,
          74, 79, 83, 79, 77, 74, 71, 74, 72, 76, 79, 84, 79, 76, 72, 0,
          76, 81, 84, 81, 79, 76, 72, 76, 77, 81, 84, 86, 84, 81, 77, 0,
          79, 83, 86, 83, 81, 79, 77, 74, 76, 79, 84, 79, 76, 74, 72, 0];
        break;

      case GameMusic.GameOver:
        tempo = 96;
        roots = [36, 31];
        // A slow minor-key march with repeated notes and a descending resolution.
        melody = [55, 0, 55, 55, 58, 0, 56, 55, 53, 0, 53, 53, 51, 50, 48, 0];
        break;

      case GameMusic.Credits:
        tempo = 132;
        roots = [50, 55, 57, 54, 59, 55, 57, 50];
        melody = [78, 81, 86, 85, 83, 81, 78, 0, 79, 83, 86, 88, 86, 83, 79, 0,
          81, 85, 88, 90, 88, 85, 81, 0, 78, 81, 85, 88, 85, 81, 78, 0,
          83, 86, 90, 86, 85, 83, 81, 78, 79, 83, 86, 88, 90, 88, 86, 83,
          81, 85, 88, 85, 83, 81, 79, 76, 78, 81, 86, 90, 86, 81, 78, 0];
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(music));
    }

    double beat = 60 / tempo;
    double eighth = beat / 2;
    double duration = roots.Length * 4 * beat;
    var samples = new short[(int)Math.Round(duration * GameSounds.SampleRate)];

    double[] notes = melody.Select(note => note == 0 ? 0 : Frequency(note)).ToArray();
    double[] bass = roots.Select(Frequency).ToArray();
    double[] thirds = roots.Select((root, bar) => Frequency(root +
      (march || (music == GameMusic.Title ? bar == 4 : bar is 3 or 4) ? 3 : 4))).ToArray();

    uint noiseState = 0x4D555349;

    for (int i = 0; i < samples.Length; i++)
    {
      double time = (double)i / GameSounds.SampleRate;
      int step = Math.Min(notes.Length - 1, (int)(time / eighth));
      int bar = Math.Min(roots.Length - 1, step / 8);
      double noteTime = time % eighth;
      double beatTime = time % beat;

      double leadEnvelope = Math.Min(1, noteTime / 0.006)
        * Math.Clamp((eighth * 0.88 - noteTime) / 0.025, 0, 1);

      double leadPhase = 2 * Math.PI * notes[step] * time;

      double lead = notes[step] == 0 ? 0 : Math.Sin(leadPhase +
        (march ? 0.5 : 1.1) * Math.Sin(2 * leadPhase)) * leadEnvelope * (march ? 0.20 : 0.16);

      double bassFrequency = bass[bar] * ((int)(time / beat) % 2 == 0 ? 0.5 : 0.75);
      double bassPhase = bassFrequency * time % 1;

      double low = (1 - 4 * Math.Abs(bassPhase - 0.5)) * Math.Exp(-4 * beatTime / beat)
        * Math.Min(1, beatTime / 0.004) * 0.12;

      double barTime = time % (4 * beat);
      double chordEnvelope = Math.Min(1, barTime / 0.01) * Math.Min(1, (4 * beat - barTime) / 0.03);
      double chord = (Math.Sin(2 * Math.PI * bass[bar] * time)
        + Math.Sin(2 * Math.PI * thirds[bar] * time)
        + Math.Sin(2 * Math.PI * bass[bar] * 1.5 * time)) * chordEnvelope * 0.025;

      noiseState ^= noiseState << 13;
      noiseState ^= noiseState >> 17;
      noiseState ^= noiseState << 5;

      double noise = (noiseState & 0xFFFF) / 32767.5 - 1;
      bool downbeat = (int)(time / beat) % 2 == 0;

      double drum = downbeat
        ? Math.Sin(2 * Math.PI * 65 * beatTime) * Math.Exp(-35 * beatTime) * 0.10
        : noise * Math.Exp(-45 * beatTime) * (march ? 0.05 : 0.08);

      double hat = march ? 0 : noise * Math.Exp(-130 * noteTime) * 0.025;
      double fade = Math.Min(1, time / 0.008) * Math.Min(1, (duration - time) / (march ? 0.35 : 0.008));
      samples[i] = (short)((lead + low + chord + drum + hat) * fade * short.MaxValue);
    }

    return samples;
  }

  private static double Frequency(int midi) => 440 * Math.Pow(2, (midi - 69) / 12.0);

}
