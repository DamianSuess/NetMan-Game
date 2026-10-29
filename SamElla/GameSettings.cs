using SamElla.Audio;

namespace SamElla;

internal sealed class GameSettings
{
  public AudioSettings Audio { get; private init; } = new();

  public PlayerRecords Records { get; private init; } = new();

  public static GameSettings Load(string path) => new()
  {
    Audio = AudioSettings.Load(path),
    Records = PlayerRecords.Load(path),
  };

  public void Save(string path)
  {
    string temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
      File.WriteAllLines(temporaryPath, Audio.ToLines().Concat(Records.ToLines()));
      File.Move(temporaryPath, path, overwrite: true);
    }

    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
      // Keep playing with session settings if preferences cannot be written.
    }

    finally
    {
      try
      {
        if (File.Exists(temporaryPath))
          File.Delete(temporaryPath);
      }

      catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
      {
        // A locked temporary file must not interrupt the game.
      }
    }
  }

  public bool RecordProgress(Game game, DateTimeOffset recordedAt)
  {
    return Records.RecordProgress(game.Score, game.Won ? game.CompletionTime : null,
      game.Lives == 0 ? game.CompletionTime : null, recordedAt);
  }
}
