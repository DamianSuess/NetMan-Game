using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMan.Actors;

namespace NetMan.Tests;

[TestClass]
public sealed class TitleScreenTests
{
  [TestMethod]
  public void DeadbeefRequiresCorrectSequenceAndFocusAndRequestsOnlyOnce()
  {
    var title = new TitleScreen();
    int[] code = "DEADBEEF".Select(letter => (int)letter).ToArray();
    title.HandleInput(0, code, false);
    Assert.IsFalse(title.TakeLevelSelectRequest());

    title.HandleInput(0, "DEADBEAF".Select(letter => (int)letter).ToArray(), true);
    Assert.IsFalse(title.TakeLevelSelectRequest());

    title.HandleInput(0, code.Concat(new[] { 0x0D }).ToArray(), true);
    Assert.IsTrue(title.IsActive, "Completing the cheat must open selection before starting.");
    Assert.IsTrue(title.TakeLevelSelectRequest());
    Assert.IsFalse(title.TakeLevelSelectRequest());

    title.Update(0, true, true);
    title.HandleInput(0, code, true);
    Assert.IsFalse(title.TakeLevelSelectRequest());
  }

  [TestMethod]
  [DataRow(true)]
  [DataRow(false)]
  public void LevelSelectAndKonamiCodesCanBothBeUsed(bool konamiFirst)
  {
    var title = new TitleScreen();
    int[] debug = "DEADBEEF".Select(letter => (int)letter).ToArray();
    int[] konami = [0x26, 0x26, 0x28, 0x28, 0x25, 0x27, 0x25, 0x27, 0x42, 0x41];
    title.HandleInput(0, konamiFirst ? konami : debug, true);
    title.HandleInput(0, konamiFirst ? debug : konami, true);
    Assert.IsTrue(title.KonamiCodeEnabled);
    Assert.IsTrue(title.TakeLevelSelectRequest());
    Assert.IsTrue(title.IsActive);
  }

  [TestMethod]
  public void PlayerSelectionRespondsToDirectionAndLocksAfterStarting()
  {
    var title = new TitleScreen();
    Assert.AreEqual(PlayerCharacter.Sam, title.SelectedCharacter);
    title.Update(0, false, true, 1);
    Assert.AreEqual(PlayerCharacter.Ella, title.SelectedCharacter);
    title.Update(0, false, false, -1);
    Assert.AreEqual(PlayerCharacter.Ella, title.SelectedCharacter);
    title.Update(0, false, true, -1);
    Assert.AreEqual(PlayerCharacter.Sam, title.SelectedCharacter);
    title.Update(0, true, true, 1);
    Assert.AreEqual(PlayerCharacter.Ella, title.SelectedCharacter);
    title.Update(0, false, true, -1);
    Assert.AreEqual(PlayerCharacter.Ella, title.SelectedCharacter);
  }

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
