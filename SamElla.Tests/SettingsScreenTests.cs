using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamElla.Audio;
using SamElla.Screens;

namespace SamElla.Tests;

[TestClass]
public sealed class SettingsScreenTests
{
  [TestMethod]
  public void TogglesAreIndependentAndDisabledVolumesAreSkipped()
  {
    var audio = new AudioSettings();
    var screen = new SettingsScreen(audio);
    screen.Open();
    screen.HandleInput([0x25], true);
    Assert.IsFalse(audio.MusicEnabled);
    Assert.IsTrue(audio.SoundEffectsEnabled);
    screen.HandleInput([0x28, 0x20], true);
    Assert.AreEqual(1, screen.SelectedIndex);
    Assert.IsFalse(audio.SoundEffectsEnabled);
    screen.HandleInput([0x27], true);
    Assert.IsTrue(audio.SoundEffectsEnabled);
    Assert.IsFalse(audio.MusicEnabled);
    screen.HandleInput([0x28], true);
    Assert.AreEqual(2, screen.SelectedIndex, "Navigation skips the disabled volume sliders.");
    screen.HandleInput([0x25, 0x27], true);
    Assert.IsTrue(screen.IsActive);
    screen.HandleInput([0x0D], true);
    Assert.IsFalse(screen.IsActive);
  }

  [TestMethod]
  public void UnfocusedAndClosedSettingsIgnoreInputAndReopeningPreservesPreferences()
  {
    var screen = new SettingsScreen(new AudioSettings());
    screen.HandleInput([0x20], true);
    screen.Open();
    screen.HandleInput([0x25, 0x28, 0x1B], false);
    Assert.IsTrue(screen.IsActive);
    Assert.IsTrue(screen.Audio.MusicEnabled);
    Assert.AreEqual(0, screen.SelectedIndex);
    screen.HandleInput([0x20, 0x28, 0x1B], true);
    Assert.IsFalse(screen.Audio.MusicEnabled);
    Assert.IsFalse(screen.IsActive);
    screen.Open();
    Assert.IsFalse(screen.Audio.MusicEnabled);
    Assert.AreEqual(0, screen.SelectedIndex);
    screen.HandleInput([0x26], true);
    Assert.AreEqual(2, screen.SelectedIndex);
  }

  [TestMethod]
  [DataRow(0x1B)]
  [DataRow(0x53)]
  [DataRow(0x70)]
  public void ClosingConsumesRemainingKeys(int closeKey)
  {
    var screen = new SettingsScreen(new AudioSettings());
    screen.Open();
    screen.HandleInput([closeKey, 0x20], true);
    Assert.IsFalse(screen.IsActive);
    Assert.IsTrue(screen.Audio.MusicEnabled);
  }

  [TestMethod]
  public void PreferencesRoundTripAndInvalidValuesRetainDefaults()
  {
    string path = Path.Combine(Path.GetTempPath(), $"samella-audio-{Guid.NewGuid():N}.txt");
    try
    {
      var defaults = AudioSettings.Load(path);
      Assert.IsTrue(defaults.MusicEnabled && defaults.SoundEffectsEnabled);
      new AudioSettings { MusicEnabled = false, SoundEffectsEnabled = true }.Save(path);
      var restored = AudioSettings.Load(path);
      Assert.IsFalse(restored.MusicEnabled);
      Assert.IsTrue(restored.SoundEffectsEnabled);
      File.WriteAllLines(path, ["MusicEnabled=invalid", "SoundEffectsEnabled=false", "garbage"]);
      restored = AudioSettings.Load(path);
      Assert.IsTrue(restored.MusicEnabled);
      Assert.IsFalse(restored.SoundEffectsEnabled);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [TestMethod]
  public void OverlayBlendsWithUnderlyingScreenAndUsesGrayVolumeControls()
  {
    var renderer = new PixelRenderer(Path.Combine(AppContext.BaseDirectory, "Assets"));
    var screen = new SettingsScreen(new AudioSettings());
    screen.Open();
    renderer.Frame.Clear(0xFFFFFFFF);
    renderer.DrawSettings(screen);
    uint lightPanel = renderer.Frame.Pixels[(26 * PixelRenderer.Width) + 26];
    Assert.AreNotEqual(0xFF17263Fu, lightPanel, "The panel must allow the background to show through.");
    Assert.AreEqual(0xFF838B98u, renderer.Frame.Pixels[(99 * PixelRenderer.Width) + 308]);
    renderer.Frame.Clear(0xFF000000);
    renderer.DrawSettings(screen);
    Assert.AreNotEqual(lightPanel, renderer.Frame.Pixels[(26 * PixelRenderer.Width) + 26]);
  }
}
