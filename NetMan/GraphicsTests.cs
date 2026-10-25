namespace NetMan;

internal static class GraphicsTests
{
  public static void Run()
  {
    int passed = 0;

    void Check(bool condition, string name)
    {
      if (!condition)
        throw new InvalidOperationException($"FAIL: {name}");

      Console.WriteLine($"PASS: {name}");
      passed++;
    }

    var palette = new Dictionary<char, uint> { ['.'] = 0, ['A'] = 0xFFFF0000, ['B'] = 0xFF00FF00 };
    var sprite = Sprite.Parse("test", ["2 2", "A.", ".B"], palette);
    var frame = new PixelBuffer(3, 3);
    frame.Clear(0xFF0000FF);
    frame.Draw(sprite, 0, 0);
    Check(frame.Pixels[0] == palette['A'] && frame.Pixels[4] == palette['B'], "Sprite colors reach correct pixels");
    Check(frame.Pixels[1] == 0xFF0000FF, "Transparent pixels preserve the background");
    frame.Clear(0);
    frame.Draw(sprite, 0, 0, true);
    Check(frame.Pixels[1] == palette['A'] && frame.Pixels[3] == palette['B'], "Sprite can face the opposite direction");
    frame.Clear(0);
    frame.Draw(sprite, -1, -1);
    Check(frame.Pixels[0] == palette['B'] && frame.Pixels.Count(pixel => pixel != 0) == 1, "Sprites clip at top and left edges");
    frame.Clear(0);
    frame.Draw(sprite, 2, 2);
    Check(frame.Pixels[8] == palette['A'], "Sprites clip at bottom and right edges");
    frame.Fill(-3, -3, 4, 4, 0xFF112233);
    Check(frame.Pixels[0] == 0xFF112233, "Filled rectangles clip to the framebuffer");
    frame.Fill(100, 100, 5, 5, 0);

    bool Rejects(string[] lines)
    {
      try
      {
        Sprite.Parse("bad", lines, palette);
        return false;
      }
      catch (InvalidDataException)
      {
        return true;
      }
    }

    Check(Rejects(["2 2", "A."]), "Missing sprite rows rejected");
    Check(Rejects(["2 2", "A", ".B"]), "Unequal sprite row widths rejected");
    Check(Rejects(["2 2", "AZ", ".B"]), "Undefined sprite colors rejected");
    Check(Rejects(["99999 99999"]), "Oversized sprite dimensions rejected");
    var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
    var gamePalette = Sprite.LoadPalette(Path.Combine(assets, "palette.txt"));

    foreach (string file in Directory.GetFiles(assets, "*.sprite"))
    {
      var asset = Sprite.Load(file, gamePalette);
      bool validSize = Path.GetFileName(file) switch
      {
        "player-splatter.sprite" => asset.Width == 24 && asset.Height == 16,
        "enemy-squashed.sprite" => asset.Width == 24 && asset.Height == 16,
        "reward-plus-one.sprite" => true,
        _ => asset.Width == 16 && asset.Height == 16,
      };
      Check(validSize, $"Bundled pixel art validates: {Path.GetFileName(file)}");
    }

    var renderer = new PixelRenderer(assets);
    var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Levels"), "*.txt").Order().ToArray();
    var game = new Game(files);
    renderer.Draw(game, 0);
    Check(renderer.Frame.Pixels.Distinct().Count() > 16, "Complete scene renders with sprites and background");
    uint[] beforePause = (uint[])renderer.Frame.Pixels.Clone();
    game.Paused = true;
    renderer.Draw(game, 0);
    Check(!renderer.Frame.Pixels.SequenceEqual(beforePause), "Pause overlay renders over the game");
    uint[] paused = (uint[])renderer.Frame.Pixels.Clone();
    renderer.Draw(game, 0, 1);
    Check(renderer.Frame.Pixels.SequenceEqual(paused), "Animation freezes while paused");

    var deathLevelPath = Path.Combine(Path.GetTempPath(), $"netman-death-{Guid.NewGuid():N}.txt");

    try
    {
      File.WriteAllLines(deathLevelPath,
        ["....................", "....................", "....................", "....................", ".S.^..............G.", "####################"]);
      var deathGame = new Game([deathLevelPath]);

      for (int i = 0; i < 60 && !deathGame.IsDying; i++)
        deathGame.Update(1, false);

      renderer.Draw(deathGame, 0);
      Check(renderer.Frame.Pixels.Contains(gamePalette['E']) && renderer.Frame.Pixels.Contains(gamePalette['F']),
        "Dead player renders bright and dark blood from the editable splatter sprite");
      Check(renderer.Frame.Pixels.Contains(gamePalette['T']) && renderer.Frame.Pixels.Contains(gamePalette['B']),
        "Player splatter retains teal and blue clothing colors");

      for (int i = 0; i < Game.DeathDurationTicks; i++)
        deathGame.Update(0, false);

      renderer.Draw(deathGame, 0);
      Check(!renderer.Frame.Pixels.Contains(gamePalette['E']) && !renderer.Frame.Pixels.Contains(gamePalette['F']),
        "Player splatter disappears when the level respawns");

      File.WriteAllLines(deathLevelPath,
        ["....................", "....................", "....................", ".S................G.", "...................."]);
      deathGame = new Game([deathLevelPath]);

      for (int life = 0; life < 3; life++)
      {
        for (int i = 0; i < 120 && !deathGame.IsDying; i++)
          deathGame.Update(0, false);

        renderer.Draw(deathGame, 0);
        Check(renderer.Frame.Pixels.Contains(gamePalette['E']),
          "Pit death splatter stays visible, including on the last life");

        for (int i = 0; i < Game.DeathDurationTicks; i++)
          deathGame.Update(0, false);
      }
    }
    finally
    {
      File.Delete(deathLevelPath);
    }

    var combinedLevelPath = Path.Combine(Path.GetTempPath(), $"netman-both-splatters-{Guid.NewGuid():N}.txt");

    try
    {
      File.WriteAllLines(combinedLevelPath,
        ["....................", "....................", ".S.^................", "...^................", ".E.^..............G.", "####################"]);
      var combinedGame = new Game([combinedLevelPath]);
      combinedGame.Enemies[0].Direction = 0;

      for (int i = 0; i < 60 && combinedGame.Enemies[0].Alive; i++)
        combinedGame.Update(0, false);

      renderer.Draw(combinedGame, 0);
      Check(renderer.Frame.Pixels[(107 * PixelRenderer.Width) + 13] == gamePalette['F'],
        "Enemy splatter sprite renders at the stomp location");

      for (int i = 0; i < 60 && !combinedGame.IsDying; i++)
        combinedGame.Update(1, false);

      renderer.Draw(combinedGame, 0);
      Check(combinedGame.IsDying && combinedGame.Enemies[0].SquashTicksRemaining > 0
        && renderer.Frame.Pixels[(107 * PixelRenderer.Width) + 13] == gamePalette['F']
        && renderer.Frame.Pixels.Contains(gamePalette['T']) && renderer.Frame.Pixels.Contains(gamePalette['B']),
        "Enemy and player splatter sprites render together");

      int enemyTicks = combinedGame.Enemies[0].SquashTicksRemaining;

      for (int i = 0; i < enemyTicks; i++)
        combinedGame.Update(0, false);

      renderer.Draw(combinedGame, 0);
      Check(combinedGame.IsDying && renderer.Frame.Pixels[(107 * PixelRenderer.Width) + 13] != gamePalette['F']
        && renderer.Frame.Pixels.Contains(gamePalette['T']),
        "Expired enemy splatter is removed without removing player splatter");
    }
    finally
    {
      File.Delete(combinedLevelPath);
    }

    var rewardLevelPath = Path.Combine(Path.GetTempPath(), $"netman-reward-{Guid.NewGuid():N}.txt");

    try
    {
      File.WriteAllLines(rewardLevelPath,
        ["....................", "....................", ".?..................", "....................", ".S................G.", "####################"]);
      var rewardGame = new Game([rewardLevelPath]);

      for (int i = 0; i < 10; i++)
        rewardGame.Update(0, false);

      rewardGame.QueueJump();

      for (int i = 0; i < 30 && rewardGame.RewardPopups.Count == 0; i++)
        rewardGame.Update(0, true);

      renderer.Draw(rewardGame, 0);
      Check(renderer.Frame.Pixels[(53 * PixelRenderer.Width) + 21] == gamePalette['Y']
        && renderer.Frame.Pixels[(52 * PixelRenderer.Width) + 25] == gamePalette['Y'],
        "Coin reward renders the editable sprite above the rewarded block");

      for (int i = 0; i < 60; i++)
        rewardGame.Update(0, false);

      renderer.Draw(rewardGame, 0);
      Check(renderer.Frame.Pixels[(53 * PixelRenderer.Width) + 21] != gamePalette['Y']
        && renderer.Frame.Pixels[(52 * PixelRenderer.Width) + 25] != gamePalette['Y'],
        "Expired reward text is removed from the rendered scene");
    }
    finally
    {
      File.Delete(rewardLevelPath);
    }

    Console.WriteLine($"All {passed} graphics checks passed.");
  }
}
