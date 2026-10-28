using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMan.Audio;

namespace NetMan.Tests;

[TestClass]
public sealed class GameSoundTests : GameTestBase
{
  [TestMethod]
  public void EffectsHaveDistinctNonSilentPcmAndDeathLastsExactlyTwoSeconds()
  {
    var effects = Enum.GetValues<GameSound>().Select(GameSounds.Create).ToArray();
    foreach (var samples in effects)
    {
      Assert.IsTrue(samples.Length > 0);
      Assert.IsTrue(samples.Any(sample => Math.Abs((int)sample) > 1000));
      Assert.IsTrue(samples.All(sample => Math.Abs((int)sample) < short.MaxValue));
      Assert.AreEqual(0, samples[0]);
      Assert.IsTrue(Math.Abs((int)samples[^1]) < 100, "Release must end near silence.");
    }

    Assert.AreEqual(GameSounds.SampleRate * 2, GameSounds.Create(GameSound.Died).Length);
    for (int i = 0; i < effects.Length; i++)
      for (int j = i + 1; j < effects.Length; j++)
        Assert.IsFalse(effects[i].Take(4000).SequenceEqual(effects[j].Take(4000)));
  }

  [TestMethod]
  [DataRow('F')]
  [DataRow('Q')]
  public void GrowthAndWeaponPickupPlayPowerUpAndDamagePlaysPowerDown(char weapon)
  {
    var game = Map(
      "........................",
      "........................",
      "........................",
      "........................",
      "........................",
      $".{weapon}..M...................",
      "........................",
      ".S....................G.",
      "########################");

    var sounds = Listen(game);
    Tick(game, 10);
    Tick(game, 15, 1);
    game.QueueJump();
    Tick(game, 30, jump: true);
    TouchHeart(game);
    Tick(game, 60);
    Assert.IsTrue(game.IsLarge);
    Assert.AreEqual(1, sounds.Count(sound => sound == GameSound.PowerUp));

    Tick(game, 15, -1);
    game.QueueJump();
    Tick(game, 30, jump: true);
    TouchHeart(game);
    Tick(game, 60);
    Assert.IsTrue(game.HasFlamethrower || game.HasBoomStick);
    Assert.AreEqual(2, sounds.Count(sound => sound == GameSound.PowerUp));

    sounds.Clear();
    Hit(game);
    CollectionAssert.AreEqual(new[] { GameSound.PowerDown }, sounds);
    Assert.IsTrue(game.IsLarge);

    game.Update(0, false);
    Assert.HasCount(1, sounds, "Invulnerability must not repeat the loss sound.");

    game.Enemies.Clear();
    Tick(game, Game.DamageProtectionTicks);
    Hit(game);
    Assert.IsFalse(game.IsLarge);

    CollectionAssert.AreEqual(new[] { GameSound.PowerDown, GameSound.PowerDown }, sounds);
    game.Enemies.Clear();
    Tick(game, Game.DamageProtectionTicks);
    Hit(game);

    Assert.IsTrue(game.IsDying);
    Tick(game, Game.DeathDurationTicks + 10);
    CollectionAssert.AreEqual(new[] { GameSound.PowerDown, GameSound.PowerDown, GameSound.Died }, sounds);
  }

  [TestMethod]
  public void JumpPlaysOnTakeoffWithoutRepeatingInAirOrWhilePaused()
  {
    var game = Map(
      "....................",
      "....................",
      "....................",
      "....................",
      ".S................G.",
      "####################");

    Tick(game, 10);
    var sounds = Listen(game);
    game.Paused = true;
    game.QueueJump();
    Tick(game, 2, jump: true);
    Assert.IsEmpty(sounds);

    game.Paused = false;
    game.QueueJump();
    game.Update(0, true);
    CollectionAssert.AreEqual(new[] { GameSound.Jump }, sounds);

    game.QueueJump();
    Tick(game, 8, jump: true);
    CollectionAssert.AreEqual(new[] { GameSound.Jump }, sounds);
  }

  [TestMethod]
  public void OverheatingFlamethrowerPlaysPowerDown()
  {
    Map(
      "....................",
      "....................",
      ".F..................",
      "....................",
      ".S................G.",
      "####################");

    var game = new Game([LevelPath], konamiCheat: true);
    var sounds = Listen(game);
    Tick(game, 10);
    game.QueueJump();
    Tick(game, 30, jump: true);
    TouchHeart(game);

    Assert.IsTrue(game.IsBurning);
    Assert.AreEqual(1, sounds.Count(sound => sound == GameSound.PowerDown));
    Assert.IsFalse(sounds.Contains(GameSound.PowerUp));
  }

  private static void Hit(Game game)
  {
    game.Enemies.Add(new Enemy(game.X + 0.4, game.Y) { Direction = 0 });
    game.Update(0, false);
  }

  private static List<GameSound> Listen(Game game)
  {
    var sounds = new List<GameSound>();
    game.SoundRequested += sounds.Add;
    return sounds;
  }
}
