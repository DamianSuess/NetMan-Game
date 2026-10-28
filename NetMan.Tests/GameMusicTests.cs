using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMan.Audio;

namespace NetMan.Tests;

[TestClass]
public sealed class GameMusicTests : GameTestBase
{
  [TestMethod]
  [DataRow("Title", 16.0, true)]
  [DataRow("GameOver", 5.0, false)]
  [DataRow("Credits", 32 * 60.0 / 132, true)]
  public void TracksHaveExpectedDurationLoopPolicyAndCleanPcm(string name, double seconds, bool loops)
  {
    GameMusic music = Enum.Parse<GameMusic>(name);
    short[] samples = GameMusicTracks.Create(music);
    Assert.AreEqual((int)Math.Round(seconds * GameSounds.SampleRate), samples.Length);
    Assert.AreEqual(loops, GameMusicTracks.Loops(music));
    Assert.AreEqual(0, samples[0]);
    Assert.IsTrue(Math.Abs((int)samples[^1]) < 100);
    Assert.IsTrue(samples.Any(sample => Math.Abs((int)sample) > 2000));
    Assert.IsTrue(samples.All(sample => Math.Abs((int)sample) < short.MaxValue));
  }

  [TestMethod]
  public void GameOverMusicStartsAfterFinalDeathAnimationAndClearsOnRestart()
  {
    var game = Map("..........", ".S.....G..", "##########");
    Assert.AreEqual(GameMusic.Title, GameMusicTracks.ForScreen(true, game));
    Assert.AreEqual(GameMusic.None, GameMusicTracks.ForScreen(false, game));
    for (int life = 0; life < 3; life++)
    {
      for (int x = 0; x < game.Level.Width; x++)
        game.Level.Clear(x, 2);

      for (int tick = 0; tick < 180 && !game.IsDying; tick++)
        game.Update(0, false);

      Assert.IsTrue(game.IsDying);
      Assert.AreEqual(GameMusic.None, GameMusicTracks.ForScreen(false, game));
      Tick(game, Game.DeathDurationTicks);
      Assert.AreEqual(life == 2 ? GameMusic.GameOver : GameMusic.None, GameMusicTracks.ForScreen(false, game));
      if (life < 2)
        Assert.IsTrue(game.ContinueAfterDeath());
    }

    Tick(game, 360);
    Assert.AreEqual(GameMusic.GameOver, GameMusicTracks.ForScreen(false, game));
    game.Restart();
    Assert.AreEqual(GameMusic.None, GameMusicTracks.ForScreen(false, game));
  }

  [TestMethod]
  public void VictorySelectsCreditsMusicAndRestartStopsIt()
  {
    var game = Map("..........", ".SG.......", "##########");
    Tick(game, 60, 1);
    Assert.IsTrue(game.Won);
    Assert.AreEqual(GameMusic.Credits, GameMusicTracks.ForScreen(false, game));
    Tick(game, 180);
    Assert.AreEqual(GameMusic.Credits, GameMusicTracks.ForScreen(false, game));
    game.Restart();
    Assert.AreEqual(GameMusic.None, GameMusicTracks.ForScreen(false, game));
  }

}
