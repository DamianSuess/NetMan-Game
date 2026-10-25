namespace NetBro;

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
      Check(asset.Width == 16 && asset.Height == 16, $"Bundled pixel art validates: {Path.GetFileName(file)}");
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
    Console.WriteLine($"All {passed} graphics checks passed.");
  }
}
