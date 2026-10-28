using System.Diagnostics;
using NetMan;
using NetMan.Actors;
using NetMan.Audio;
using NetMan.Screens;
using NetMan.Updater;
using Velopack;

try
{
  VelopackApp.Build().Run();

#if !NETMAN_DISTRIBUTION
  int exportOption = Array.IndexOf(args, "--export-art");

  if (exportOption >= 0)
  {
    if (exportOption + 1 >= args.Length)
      throw new ArgumentException("Usage: --export-art <output-directory>");

    ArtExporter.Export(Path.Combine(AppContext.BaseDirectory, "Assets"), Path.GetFullPath(args[exportOption + 1]));
    return;
  }

#endif

  if (!OperatingSystem.IsWindows())
    throw new PlatformNotSupportedException("NetMan currently has a Windows backend. Its game and pixel renderer are portable; other window backends can be added later.");

  using var updates = new AppUpdates(new VelopackUpdateBackend(), exception =>
  {
    try
    {
      string logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetMan", "Logs");
      Directory.CreateDirectory(logs);
      File.AppendAllText(Path.Combine(logs, "updates.log"), $"{DateTimeOffset.UtcNow:u} {exception}\n");
    }
    catch (Exception logError) when (logError is IOException or UnauthorizedAccessException)
    {
      // An unavailable log folder must not prevent offline play.
    }
  });

  string contentRoot = AppContext.BaseDirectory;
  if (updates.IsInstalled)
  {
    contentRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetMan", "Content");
    UserContent.Synchronize(AppContext.BaseDirectory, contentRoot);
  }

  string assetsPath = Path.Combine(contentRoot, "Assets");
  var levelsPath = Path.Combine(contentRoot, "Levels");
  var option = Array.IndexOf(args, "--levels");

  if (option >= 0)
  {
    if (option + 1 >= args.Length)
      throw new ArgumentException("Usage: --levels <directory>");

    levelsPath = Path.GetFullPath(args[option + 1]);
  }

  var files = LevelSelectScreen.Discover(levelsPath);

  if (files.Length == 0)
    throw new InvalidDataException($"No .txt levels found in {levelsPath}");

  foreach (var file in files)
    Level.Load(file);

  using var audio = new WindowsGameAudio();
  Game CreateGame(bool cheat = false, PlayerCharacter character = PlayerCharacter.Sam, int startingLevelIndex = 0)
  {
    var created = new Game(files,
      deathQuotesPath: Path.Combine(assetsPath, "death-quotes.txt"), creditsAssetsPath: assetsPath,
      enemySettingsPath: Path.Combine(assetsPath, "enemies.txt"),
      flamethrowerQuotesPath: Path.Combine(assetsPath, "flamethrower-quotes.txt"), konamiCheat: cheat,
      gameOverQuotesPath: Path.Combine(assetsPath, "game-over-quotes.txt"), startingLevelIndex: startingLevelIndex)
    { Character = character };
    created.SoundRequested += audio.Play;
    return created;
  }

  var renderer = new PixelRenderer(assetsPath);
  var game = CreateGame();
  var titleScreen = new TitleScreen();
  LevelSelectScreen? levelSelect = null;
  var levelStart = new LevelStartScreen(Path.Combine(assetsPath, "uncle-quotes.txt"));
  Level? presentedLevel = null;

  using IGameWindow window = new WindowsGameWindow(PixelRenderer.Width, PixelRenderer.Height);
  _ = Task.Run(updates.CheckAsync);

  var clock = Stopwatch.StartNew();
  double previous = clock.Elapsed.TotalSeconds;
  double accumulator = 0;

  while (!window.Closed)
  {
    window.PumpEvents();
    audio.Update();
    audio.SetMusicPaused(!window.Active || (!titleScreen.IsActive && game.Paused));
    audio.SetMusic(GameMusicTracks.ForScreen(titleScreen.IsActive, game));
    audio.SetFlamethrowerActive(window.Active && !titleScreen.IsActive && !levelStart.IsActive
      && !game.Paused && !game.IsDying && !game.Won && game.Flame is not null);

    if (window.Closed)
      break;

    var now = clock.Elapsed.TotalSeconds;
    double elapsed = Math.Min(now - previous, 0.1);
    accumulator += elapsed;
    previous = now;

    if (titleScreen.IsActive)
    {
      if (window.Active && updates.TryScheduleApply(restart: true, args))
        break;
      int startingLevelIndex = 0;
      if (levelSelect is null)
      {
        if (window.Active && window.Pressed(GameKey.Escape))
          break;

        titleScreen.HandleInput(elapsed, window.KeyPresses, window.Active);
        if (titleScreen.TakeLevelSelectRequest())
        {
          files = LevelSelectScreen.Discover(levelsPath);
          levelSelect = new LevelSelectScreen(files);
        }
      }
      else
      {
        levelSelect.HandleInput(window.KeyPresses, window.Active);
        if (!levelSelect.IsActive)
        {
          if (levelSelect.Confirmed)
          {
            startingLevelIndex = levelSelect.SelectedIndex;
            titleScreen.Update(0, startPressed: true, windowActive: true);
          }

          levelSelect = null;
        }
      }

      if (!titleScreen.IsActive)
      {
        game = CreateGame(titleScreen.KonamiCodeEnabled, titleScreen.SelectedCharacter, startingLevelIndex);
        audio.SetMusic(GameMusic.None);
        audio.Play(GameSound.PowerUp);
      }

      accumulator = 0;
      if (levelSelect is null)
        renderer.DrawTitle(titleScreen, updates.Status);
      else
        renderer.DrawLevelSelect(levelSelect);

      window.Present(renderer.Frame);
      Thread.Sleep(16);
      continue;
    }

    if (!ReferenceEquals(presentedLevel, game.Level))
    {
      presentedLevel = game.Level;
      levelStart.Show(game);
    }

    if (levelStart.IsActive)
    {
      levelStart.Update(elapsed, window.AnyKeyPressed, window.Active);
      if (!levelStart.IsActive)
        game.CompleteLevelStart();

      accumulator = 0;
      renderer.DrawLevelStart(levelStart);
      window.Present(renderer.Frame);
      Thread.Sleep(16);
      continue;
    }

    if (!window.Active)
    {
      game.Paused = true;
      accumulator = 0;
      renderer.Draw(game, 0, 0);
      window.Present(renderer.Frame);
      Thread.Sleep(20);
      continue;
    }

    bool wasDying = game.IsDying;
    if (wasDying)
    {
      game.Paused = false;
      if (window.AnyKeyPressed && game.ContinueAfterDeath())
        accumulator = 0;
    }
    else
    {
      if (window.Pressed(GameKey.Escape))
        break;

      if (window.Pressed(GameKey.Restart))
        game.Restart();

      if (window.Pressed(GameKey.Pause))
        game.Paused = !game.Paused;

      if (window.Pressed(GameKey.Jump))
        game.QueueJump();
    }

    if (!ReferenceEquals(presentedLevel, game.Level))
    {
      accumulator = 0;
      continue;
    }

    int direction = wasDying ? 0 : (window.Down(GameKey.Right) ? 1 : 0) - (window.Down(GameKey.Left) ? 1 : 0);

    while (accumulator >= Game.Step)
    {
      game.Update(direction, !wasDying && window.Down(GameKey.Jump), !wasDying && window.Down(GameKey.Fire));
      accumulator -= Game.Step;
      if (!ReferenceEquals(presentedLevel, game.Level))
      {
        accumulator = 0;
        break;
      }
    }

    renderer.Draw(game, direction, elapsed);
    window.Present(renderer.Frame);
    Thread.Sleep(16);
  }

  updates.TryScheduleApply(restart: false, args);
}
catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException
  or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
{
#if !NETMAN_DISTRIBUTION
  Console.Error.WriteLine($"NetMan: {exception.Message}");
#endif

  if (OperatingSystem.IsWindows() && !args.Contains("--export-art"))
    WindowsGameWindow.ShowError(exception.Message);

  Environment.ExitCode = 1;
}
