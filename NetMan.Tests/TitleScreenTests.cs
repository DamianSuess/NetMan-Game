using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetMan.Tests;

[TestClass]
public sealed class TitleScreenTests
{
  [TestMethod]
  public void TitleWaitsForStartAndFreezesWhenUnfocused()
  {
    var title = new TitleScreen();
    Assert.IsTrue(title.IsActive);
    title.Update(1, false, true);
    Assert.AreEqual(1, title.Elapsed);
    title.Update(1, true, false);
    Assert.IsTrue(title.IsActive);
    Assert.AreEqual(1, title.Elapsed);
    title.Update(0, true, true);
    Assert.IsFalse(title.IsActive);
    title.Update(1, false, true);
    Assert.AreEqual(1, title.Elapsed);
  }

  [TestMethod]
  public void EnterStartsOnlyOnFreshKeypress()
  {
    var keyboard = new GameKeyboard();
    keyboard.KeyDown(0x0D);
    Assert.IsTrue(keyboard.Pressed(GameKey.Start));
    keyboard.BeginFrame();
    keyboard.KeyDown(0x0D, isRepeat: true);
    Assert.IsFalse(keyboard.Pressed(GameKey.Start));
    keyboard.KeyUp(0x0D);
    keyboard.KeyDown(0x0D);
    Assert.IsTrue(keyboard.Pressed(GameKey.Start));
  }

  [TestMethod]
  public void TitleAnimatesCharactersWhileKeepingCopyrightFixed()
  {
    var title = new TitleScreen();
    var renderer = new PixelRenderer(Path.Combine(AppContext.BaseDirectory, "Assets"));
    renderer.DrawTitle(title);
    uint[] first = (uint[])renderer.Frame.Pixels.Clone();
    title.Update(1, false, true);
    renderer.DrawTitle(title);
    int laneStart = 192 * PixelRenderer.Width;
    int footerStart = 226 * PixelRenderer.Width;
    Assert.IsFalse(first.Skip(laneStart).Take(footerStart - laneStart)
      .SequenceEqual(renderer.Frame.Pixels.Skip(laneStart).Take(footerStart - laneStart)));
    Assert.IsTrue(first.Skip(footerStart).SequenceEqual(renderer.Frame.Pixels.Skip(footerStart)));
    Assert.IsTrue(renderer.Frame.Pixels.Skip(footerStart).Any(pixel => pixel == 0xFFF6F4DF));
  }
}
