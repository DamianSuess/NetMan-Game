namespace NetMan;

internal static class SelfTests
{
  public static void Run()
  {
    int passed = 0;
    void Check(bool condition, string name)
    {
      if (!condition) throw new InvalidOperationException($"FAIL: {name}");
      Console.WriteLine($"PASS: {name}"); passed++;
    }

    bool Rejects(string[] rows)
    {
      try { Level.Parse("test", rows); return false; }
      catch (InvalidDataException) { return true; }
    }

    Check(Rejects(["..........", ".........G", "##########"]), "Missing spawn rejected");
    Check(Rejects(["S.........", "........ZG", "##########"]), "Unknown tile rejected");
    Check(Rejects(["SS........", ".........G", "##########"]), "Duplicate spawn rejected");
    Check(Level.Parse("ragged", ["S.........G", ".", "###########"]).Tile(5, 1) == '.', "Short rows padded");

    var path = Path.Combine(Path.GetTempPath(), $"netbro-test-{Guid.NewGuid():N}.txt");
    var nextPath = Path.Combine(Path.GetTempPath(), $"netbro-test-{Guid.NewGuid():N}.txt");

    try
    {
      Game Map(params string[] rows)
      {
        File.WriteAllLines(path, rows);
        return new Game([path]);
      }

      void Tick(Game game, int count, int direction = 0, bool jump = false)
      {
        for (int i = 0; i < count; i++)
          game.Update(direction, jump);
      }

      var game = Map("....................", "....................", "....................", "....................", "....................", ".S.....o.........G..", "####################");
      Tick(game, 30);
      Check(game.Grounded && Math.Abs(game.Y - 5.1) < 0.001, "Player lands precisely on floor");
      game.QueueJump(); Tick(game, 10, jump: true);
      Check(game.Y < 4 && game.VelocityY < 0, "Jump rises above floor");
      Tick(game, 100);
      Check(game.Grounded && game.Lives == 3, "Jump returns safely to floor");
      Tick(game, 30, 1);
      Check(game.Coins == 1 && game.Score == 100, "Coin collected once");
      game.Paused = true; double previousX = game.X; Tick(game, 20, 1);
      Check(game.X == previousX, "Pause stops simulation"); game.Paused = false;
      Tick(game, 70, 1);
      Check(game.Won && game.Score == 1100, "Goal completes final level");
      game.Restart();
      Check(!game.Won && game.Lives == 3 && game.Score == 0, "Restart resets game");

      game = Map("....................", "....................", "....................", "....................", ".S.#..............G.", "####################");
      Tick(game, 50, 1);
      Check(game.X + Game.BodyWidth <= 3.00001, "Wall blocks horizontal movement");

      game = Map("....................", "....................", ".?..................", "....................", ".S................G.", "####################");
      Tick(game, 10); game.QueueJump(); Tick(game, 20, jump: true);
      Check(game.Coins == 1 && game.Level.Tile(1, 2) == 'X', "Head bump dispenses bonus coin");
      Check(game.RewardPopups.Count == 1 && game.RewardPopups[0].X == 1 && game.RewardPopups[0].Y == 2,
        "Coin block creates a reward popup at the block");

      game = Map("....................", "....................", ".?..................", "....................", ".S................G.", "####################");
      Tick(game, 10);
      game.QueueJump();

      for (int i = 0; i < 30 && game.RewardPopups.Count == 0; i++)
        game.Update(0, true);

      Check(game.RewardPopups.Count == 1 && game.RewardPopups[0].AgeTicks == 0,
        "Reward popup appears immediately on the rewarding hit");
      game.Paused = true;
      Tick(game, 120);
      Check(game.RewardPopups.Count == 1 && game.RewardPopups[0].AgeTicks == 0,
        "Pausing freezes the reward popup timer");
      game.Paused = false;
      Tick(game, 59);
      Check(game.RewardPopups.Count == 1, "Reward popup remains visible until one second has elapsed");
      Tick(game, 1);
      Check(game.RewardPopups.Count == 0, "Reward popup disappears after exactly one second");
      game.QueueJump();
      Tick(game, 60, jump: true);
      Check(game.Coins == 1 && game.RewardPopups.Count == 0, "Used block gives no additional reward popup");

      game.Restart();
      Tick(game, 10);
      game.QueueJump();
      Tick(game, 10, jump: true);
      Check(game.RewardPopups.Count == 1, "A reloaded bonus block can show a new reward popup");
      game.Restart();
      Check(game.RewardPopups.Count == 0, "Restart clears old reward popups");

      game = Map("....................", "....................", "....................", "....................", ".So^..............G.", "####################");

      for (int i = 0; i < 60 && !game.IsDying; i++)
        game.Update(1, false);

      Check(game.Lives == 2 && game.DeathTicksRemaining == Game.DeathDurationTicks,
        "Hazard costs one life and immediately starts the death splatter");
      double deathX = game.X;
      double deathY = game.Y;
      var dyingLevel = game.Level;
      game.Paused = true;
      Tick(game, 120, 1, true);
      Check(game.DeathTicksRemaining == Game.DeathDurationTicks, "Pause freezes the death timer");
      game.Paused = false;
      game.QueueJump();
      Tick(game, 59, 1, true);
      Check(game.IsDying && game.DeathTicksRemaining == 1 && game.X == deathX && game.Y == deathY
        && ReferenceEquals(game.Level, dyingLevel) && game.Lives == 2,
        "Death holds position and level for a full second without repeated damage");
      Tick(game, 1);
      Check(!game.IsDying && game.X == game.Level.Spawn.X && game.Y == game.Level.Spawn.Y
        && !ReferenceEquals(game.Level, dyingLevel) && game.Lives == 2,
        "Level reloads exactly one second after death");
      Check(game.Coins == 1 && game.Score == 100, "Level respawn preserves earned coins and score");

      game = Map("....................", "....................", "....................", "....................", ".S.E..............G.", "####################");
      Tick(game, 15, 1);
      Check(game.Lives == 2 && game.IsDying, "Enemy side contact costs a life and starts death splatter");
      game.Restart();
      Check(!game.IsDying && game.Lives == 3, "Manual restart clears the death state");

      game = Map("....................", "....................", ".S..................", "....................", ".E................G.", "####################");
      // Keep enemy at the left boundary beneath the falling player.
      game.Enemies[0].Direction = 0;
      for (int i = 0; i < 60 && game.Enemies[0].Alive; i++)
        game.Update(0, false);
      Check(!game.Enemies[0].Alive && game.Score == 200 && game.Lives == 3, "Falling player stomps enemy");
      Check(game.Enemies[0].SquashTicksRemaining == Enemy.SquashDurationTicks,
        "Enemy death immediately starts a one-second splatter");
      double enemyDeathX = game.Enemies[0].X;
      double enemyDeathY = game.Enemies[0].Y;
      Tick(game, Enemy.SquashDurationTicks - 1);
      Check(game.Enemies[0].SquashTicksRemaining == 1 && game.Enemies[0].X == enemyDeathX
        && game.Enemies[0].Y == enemyDeathY && game.Score == 200 && game.Lives == 3,
        "Enemy splatter remains in place and cannot cause damage or extra points");
      Tick(game, 1);
      Check(!game.Enemies[0].Alive && game.Enemies[0].SquashTicksRemaining == 0,
        "Enemy splatter disappears after exactly one second");

      game = Map("....................", "....................", ".S.^................", "...^................", ".E.^..............G.", "####################");
      game.Enemies[0].Direction = 0;

      for (int i = 0; i < 60 && game.Enemies[0].Alive; i++)
        game.Update(0, false);

      for (int i = 0; i < 60 && !game.IsDying; i++)
        game.Update(1, false);

      Check(game.IsDying && !game.Enemies[0].Alive && game.Enemies[0].SquashTicksRemaining > 0,
        "Enemy splatter and player splatter coexist in the same game");
      int enemyTicks = game.Enemies[0].SquashTicksRemaining;
      int playerTicks = game.DeathTicksRemaining;
      game.Paused = true;
      Tick(game, 120);
      Check(game.Enemies[0].SquashTicksRemaining == enemyTicks && game.DeathTicksRemaining == playerTicks,
        "Pause freezes both splatter effects together");
      game.Paused = false;
      Tick(game, enemyTicks);
      Check(game.Enemies[0].SquashTicksRemaining == 0 && game.IsDying,
        "Enemy splatter expires independently while player splatter remains");
      Tick(game, game.DeathTicksRemaining);
      Check(!game.IsDying && game.Enemies[0].Alive && game.Enemies[0].SquashTicksRemaining == 0,
        "Player respawn clears both death effects and reloads live enemies");

      game = Map("....................", "....................", "....................", "....................", ".S...E............G.", "####.....###########");
      game.Enemies[0].Direction = 0;

      for (int i = 0; i < 120 && game.Enemies[0].Alive; i++)
        game.Update(0, false);

      Check(!game.Enemies[0].Alive && game.Enemies[0].SquashTicksRemaining == Enemy.SquashDurationTicks
        && game.Enemies[0].Y + Game.BodyHeight <= game.Level.Height,
        "Enemy pit deaths also create splatter at the bottom of the level");

      game = Map("....................", "....................", ".B..................", "....................", ".S................G.", "####################");
      Tick(game, 10); game.QueueJump(); Tick(game, 20, jump: true);
      Check(game.Level.Tile(1, 2) == '.' && game.Score == 50, "Head bump breaks a brick");

      game = Map("....................", "....................", "....................", ".S................G.", "....................");

      for (int remainingLives = 2; remainingLives >= 0; remainingLives--)
      {
        for (int i = 0; i < 120 && !game.IsDying; i++)
          game.Update(0, false);

        Check(game.IsDying && game.Lives == remainingLives
          && game.DeathTicksRemaining == Game.DeathDurationTicks, "Pit fall starts a timed death");
        Tick(game, Game.DeathDurationTicks - 1);
        Check(game.IsDying && game.Lives == remainingLives, "Pit splatter remains for the full death delay");
        Tick(game, 1);
        Check(!game.IsDying && game.Lives == (remainingLives == 0 ? 3 : remainingLives),
          "Death respawns the level or restarts the game after the last life");
      }

      Check(game.LevelNumber == 1 && game.Score == 0 && game.Coins == 0,
        "Automatic game restart resets progress and rewards");

      File.WriteAllLines(path, ["....................", "....................", ".S.G................", "####################"]);
      File.WriteAllLines(nextPath, ["....................", "....................", ".S...............G..", "####################"]);
      game = new Game([path, nextPath]);
      Tick(game, 15, 1);
      Check(game.LevelNumber == 2 && !game.Won && game.Score == 1000, "Goal loads the next level");
      Tick(game, 140, 1);
      Check(game.Won && game.Score == 2000, "Final goal wins a multiple-level game");
    }
    finally
    {
      File.Delete(path);
      File.Delete(nextPath);
    }

    foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Levels"), "*.txt"))
      Check(Level.Load(file).Width > 0, $"Bundled level validates: {Path.GetFileName(file)}");

    Console.WriteLine($"All {passed} checks passed.");
  }
}
