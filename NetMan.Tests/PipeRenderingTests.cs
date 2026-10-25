using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class PipeRenderingTests : GameTestBase
{
  [TestMethod]
  [DataRow("PP/PP", 5, 2, "top-left,top-right/bottom-left,bottom-right")]
  [DataRow("PP/PP", 0, 0, "top-left,top-right/bottom-left,bottom-right")]
  [DataRow("PP/PP/PP", 5, 2, "top-left,top-right/body-left,body-right/bottom-left,bottom-right")]
  [DataRow("PP/PP/PP/PP", 5, 1, "top-left,top-right/body-left,body-right/body-left,body-right/bottom-left,bottom-right")]
  [DataRow("PP/PP/PP", 18, 0, "top-left,top-right/body-left,body-right/bottom-left,bottom-right")]
  [DataRow("P/P", 5, 2, "small-top/small-bottom")]
  [DataRow("P/P/P", 5, 2, "small-top/small-body/small-bottom")]
  [DataRow("P/P/P/P/P", 0, 0, "small-top/small-body/small-body/small-body/small-bottom")]
  [DataRow("P", 5, 2, "standard")]
  [DataRow("PP", 5, 2, "standard")]
  [DataRow("PPP/PPP", 5, 2, "standard")]
  [DataRow("PP/P.", 5, 2, "standard")]
  [DataRow("P./PP", 5, 2, "standard")]
  [DataRow("PP/PPP/PP", 5, 2, "standard")]
  [DataRow("PP/../PP", 5, 2, "standard")]
  public void PipeGroupsRenderMatchingArtwork(string layout, int left, int top, string expectedLayout)
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
    string[][] expectedPieces = expectedLayout.Split('/').Select(row => row.Split(',')).ToArray();

    for (int y = 0; y < pieces.Length; y++)
    {
      char[] row = rows[top + y].ToCharArray();

      for (int x = 0; x < pieces[y].Length; x++)
      {
        if (pieces[y][x] != 'P')
          continue;

        row[left + x] = 'P';
        string name = expectedLayout == "standard" ? "pipe.sprite" : $"pipe-{expectedPieces[y][x]}.sprite";
        var sprite = Sprite.Load(Path.Combine(assets, name), palette);
        expected.Frame.Draw(sprite, (left + x) * PixelRenderer.TileSize, 28 + ((top + y) * PixelRenderer.TileSize));
      }

      rows[top + y] = new string(row);
    }

    var actual = new PixelRenderer(assets);
    actual.Draw(Map(rows), 0, 0);

    CollectionAssert.AreEqual(expected.Frame.Pixels, actual.Frame.Pixels,
      "Rectangular stacks should have one top rim, continuous body pieces, and a single bottom; irregular groups retain the standard sprite.");
  }
}
