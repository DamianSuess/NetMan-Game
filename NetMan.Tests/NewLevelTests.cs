using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class NewLevelTests
{
  [TestMethod]
  [DataRow("03-pipe-gardens.txt")]
  [DataRow("04-brick-foothills.txt")]
  [DataRow("05-pipe-terraces.txt")]
  [DataRow("06-ruins-crossing.txt")]
  [DataRow("07-brick-summit.txt")]
  public void NewLevelCanBeCompletedWithRunningJumps(string name)
  {
    var path = Path.Combine(AppContext.BaseDirectory, "Levels", name);
    var game = new Game([path]);
    int groundRow = game.Level.Height - 2;
    int goalX = Enumerable.Range(0, game.Level.Width).Single(x => game.Level.Tile(x, groundRow - 1) == 'G');

    int[] takeoffs = Enumerable.Range(1, game.Level.Width - 1)
      .Where(x => game.Level.Tile(x, groundRow) == '.' && game.Level.Tile(x - 1, groundRow) == '#')
      .Select(x => x - 2).ToArray();

    // Patrol speed is configurable. Try a few launch timings instead of requiring
    // every level to match one particular enemy phase at the start of the run.
    for (int startDelay = 0; startDelay <= 60 && !game.Won; startDelay += 5)
    {
      if (startDelay > 0)
        game.Restart();

      for (int tick = 0; tick < startDelay; tick++)
        game.Update(0, false);

      int nextTakeoff = 0;
      for (int tick = 0; tick < 6000 && !game.Won && !game.IsDying; tick++)
      {
        int direction = game.X >= goalX - 0.2 ? 0 : 1;
        bool atTakeoff = nextTakeoff < takeoffs.Length && game.X >= takeoffs[nextTakeoff];

        // Land before each pit, then make a full jump from its safe takeoff point.
        if (atTakeoff && !game.Grounded)
          direction = 0;

        if (game.Grounded && direction != 0)
        {
          game.QueueJump();

          if (atTakeoff)
            nextTakeoff++;
        }

        game.Update(direction, true);
      }
    }

    Assert.IsTrue(game.Won, $"{name}: the route must reach the goal; stopped at ({game.X:F2}, {game.Y:F2}), dying={game.IsDying}.");
    Assert.AreEqual(3, game.Lives, "The route should be completable without losing a life.");
  }
}
