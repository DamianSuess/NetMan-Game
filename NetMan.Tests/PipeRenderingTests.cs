using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class PipeRenderingTests : GameTestBase
{
  [TestMethod]
  [DataRow("PP/PP", 5, 2, true)]
  [DataRow("PP/PP", 0, 0, true)]
  [DataRow("P", 5, 2, false)]
  [DataRow("PP", 5, 2, false)]
  [DataRow("P/P", 5, 2, false)]
  [DataRow("PPP/PPP", 5, 2, false)]
  [DataRow("PP/P.", 5, 2, false)]
  public void PipeGroupsRenderMatchingArtwork(string layout, int left, int top, bool large)
  {
    string[] rows =
    [
      "....................",
      "....................",
      "....................",
      "....................",
      "....................",
      ".S................G.",
      "####################",
    ];
    var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
    var palette = Sprite.LoadPalette(Path.Combine(assets, "palette.txt"));
    var expected = new PixelRenderer(assets);
    expected.Draw(Map(rows), 0, 0);

    string[] pieces = layout.Split('/');

    for (int y = 0; y < pieces.Length; y++)
    {
      char[] row = rows[top + y].ToCharArray();

      for (int x = 0; x < pieces[y].Length; x++)
      {
        if (pieces[y][x] != 'P')
          continue;

        row[left + x] = 'P';
        string name = large
          ? $"pipe-{(y == 0 ? "top" : "bottom")}-{(x == 0 ? "left" : "right")}.sprite"
          : "pipe.sprite";
        var sprite = Sprite.Load(Path.Combine(assets, name), palette);
        expected.Frame.Draw(sprite, (left + x) * PixelRenderer.TileSize, 28 + ((top + y) * PixelRenderer.TileSize));
      }

      rows[top + y] = new string(row);
    }

    var actual = new PixelRenderer(assets);
    actual.Draw(Map(rows), 0, 0);

    CollectionAssert.AreEqual(expected.Frame.Pixels, actual.Frame.Pixels,
      "Only complete 2 x 2 groups should use the four joined pipe pieces; other layouts retain the standard sprite.");
  }
}
