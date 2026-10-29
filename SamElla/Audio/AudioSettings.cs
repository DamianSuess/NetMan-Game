namespace SamElla.Audio;

internal sealed class AudioSettings
{
  private int _musicVolume = 100;
  private int _soundEffectsVolume = 100;

  // Volume changes last for this session; only the existing on/off flags are saved.
  public int MusicVolume
  {
    get => _musicVolume;
    set => _musicVolume = Math.Clamp(value, 0, 100);
  }

  public int SoundEffectsVolume
  {
    get => _soundEffectsVolume;
    set => _soundEffectsVolume = Math.Clamp(value, 0, 100);
  }

  public bool MusicEnabled { get; set; } = true;
  public bool SoundEffectsEnabled { get; set; } = true;

  public static AudioSettings Load(string path)
  {
    var settings = new AudioSettings();

    try
    {
      foreach (string line in File.ReadAllLines(path))
      {
        string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !bool.TryParse(parts[1], out bool enabled))
          continue;

        if (parts[0] == nameof(MusicEnabled))
          settings.MusicEnabled = enabled;
        else if (parts[0] == nameof(SoundEffectsEnabled))
          settings.SoundEffectsEnabled = enabled;
      }
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // Missing or unavailable preferences fall back to audible defaults.
    }

    return settings;
  }

  public void Save(string path)
  {
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
      File.WriteAllLines(path,
        [$"{nameof(MusicEnabled)}={MusicEnabled}", $"{nameof(SoundEffectsEnabled)}={SoundEffectsEnabled}"]);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // Preferences still apply to this session if the file cannot be written.
    }
  }
}
