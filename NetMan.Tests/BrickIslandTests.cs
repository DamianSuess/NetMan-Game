using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class BrickIslandTests
{
  [TestMethod]
  [DataRow("08-moonlit-stepping-stones.txt")]
  [DataRow("09-midnight-brickfall.txt")]
  public void MissingTheFirstJumpFallsIntoTheVoid(string name)
  {
    var game = new Game([Path.Combine(AppContext.BaseDirectory, "Levels", name)]);
    game.CompleteLevelStart();
    for (int tick = 0; tick < 180 && !game.IsDying; tick++)
      game.Update(1, false);

    Assert.IsTrue(game.IsDying);
    Assert.IsFalse(game.Won);
    Assert.AreEqual(2, game.Lives);
  }

  [TestMethod]
  [DataRow("08-moonlit-stepping-stones.txt", false)]
  [DataRow("08-moonlit-stepping-stones.txt", true)]
  [DataRow("09-midnight-brickfall.txt", false)]
  [DataRow("09-midnight-brickfall.txt", true)]
  public void NighttimeBrickIslandsCanBeCrossedWithNormalRunningJumps(string name, bool large)
  {
    var game = new Game([Path.Combine(AppContext.BaseDirectory, "Levels", name)], konamiCheat: large);
    game.CompleteLevelStart();
    var islands = new List<(int Left, int Right, int Top)>();
    for (int y = 0; y < game.Level.Height; y++)
    {
      for (int x = 0; x < game.Level.Width; x++)
      {
        Assert.AreNotEqual('#', game.Level.Tile(x, y), "These levels must have no floor.");
        if (game.Level.Tile(x, y) != 'B')
          continue;

        int left = x;
        while (game.Level.Tile(x, y) == 'B')
          x++;

        islands.Add((left, x, y));
      }
    }

    islands = islands.OrderBy(island => island.Left).ToList();
    Assert.AreEqual(BackgroundTheme.Night, game.Level.Background);
    Assert.IsGreaterThanOrEqualTo(16, islands.Count);
    Assert.IsGreaterThanOrEqualTo(6, islands.Select(island => island.Top).Distinct().Count());
    for (int tick = 0; tick < 60 && !game.Grounded; tick++)
      game.Update(0, false);

    for (int index = 1; index < islands.Count; index++)
    {
      var previous = islands[index - 1];
      var target = islands[index];
      Assert.IsGreaterThan(previous.Right, target.Left, "Every island must have a bottomless gap before it.");
      double takeoff = previous.Right - 1;
      for (int tick = 0; tick < 90 && game.X < takeoff && !game.IsDying; tick++)
        game.Update(1, false);

      Assert.IsTrue(game.Grounded, $"{name}: safe takeoff required on island {index}.");
      game.QueueJump();
      bool landed = false;
      for (int tick = 0; tick < 120 && !game.IsDying; tick++)
      {
        game.Update(game.X < target.Left + 0.5 ? 1 : 0, true);
        if (game.Grounded && Math.Abs(game.Y + game.PlayerHeight - target.Top) < 0.001
          && game.X >= target.Left)
        {
          landed = true;
          break;
        }
      }

      Assert.IsTrue(landed,
        $"{name}: jump {index} to ({target.Left}, {target.Top}) failed at ({game.X:F2}, {game.Y:F2}).");
    }

    for (int tick = 0; tick < 60 && !game.Won && !game.IsDying; tick++)
      game.Update(1, false);

    Assert.IsTrue(game.Won, $"{name}: the last brick island must lead to the goal.");
    Assert.AreEqual(large ? 30 : 3, game.Lives);
  }
}
