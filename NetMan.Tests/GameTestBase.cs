using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

public abstract class GameTestBase
{
  protected string LevelPath { get; private set; } = string.Empty;
  protected string NextPath { get; private set; } = string.Empty;

  [TestInitialize]
  public void CreateLevelPaths()
  {
    LevelPath = Path.Combine(Path.GetTempPath(), $"netman-test-{Guid.NewGuid():N}.txt");
    NextPath = Path.Combine(Path.GetTempPath(), $"netman-test-{Guid.NewGuid():N}.txt");
  }

  [TestCleanup]
  public void DeleteLevels()
  {
    File.Delete(LevelPath);
    File.Delete(NextPath);
  }

  protected Game Map(params string[] rows)
  {
    File.WriteAllLines(LevelPath, rows);
    return new Game([LevelPath]);
  }

  protected static void Tick(Game game, int count, int direction = 0, bool jump = false)
  {
    for (int i = 0; i < count; i++)
    {
      game.Update(direction, jump);
    }
  }
}
