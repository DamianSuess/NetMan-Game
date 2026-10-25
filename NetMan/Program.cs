using System.Diagnostics;
using NetMan;

try
{
#if !NETMAN_DISTRIBUTION
  if (args.Contains("--self-test"))
  {
    SelfTests.Run();
    GraphicsTests.Run();
    return;
  }

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

  var levelsPath = Path.Combine(AppContext.BaseDirectory, "Levels");
  var option = Array.IndexOf(args, "--levels");

  if (option >= 0)
  {
    if (option + 1 >= args.Length)
      throw new ArgumentException("Usage: --levels <directory>");

    levelsPath = Path.GetFullPath(args[option + 1]);
  }

  var files = Directory.GetFiles(levelsPath, "*.txt")
    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();

  if (files.Length == 0)
    throw new InvalidDataException($"No .txt levels found in {levelsPath}");

  foreach (var file in files)
    Level.Load(file);

  var renderer = new PixelRenderer(Path.Combine(AppContext.BaseDirectory, "Assets"));
  var game = new Game(files);
  using IGameWindow window = new WindowsGameWindow(PixelRenderer.Width, PixelRenderer.Height);
  var clock = Stopwatch.StartNew();
  double previous = clock.Elapsed.TotalSeconds;
  double accumulator = 0;

  while (!window.Closed)
  {
    window.PumpEvents();

    if (window.Closed || window.Pressed(GameKey.Escape))
      break;

    var now = clock.Elapsed.TotalSeconds;
    double elapsed = Math.Min(now - previous, 0.1);
    accumulator += elapsed;
    previous = now;

    if (!window.Active)
    {
      game.Paused = true;
      accumulator = 0;
      renderer.Draw(game, 0, 0);
      window.Present(renderer.Frame);
      Thread.Sleep(20);
      continue;
    }

    if (window.Pressed(GameKey.Restart))
      game.Restart();

    if (window.Pressed(GameKey.Pause))
      game.Paused = !game.Paused;

    if (window.Pressed(GameKey.Jump))
      game.QueueJump();

    int direction = (window.Down(GameKey.Right) ? 1 : 0) - (window.Down(GameKey.Left) ? 1 : 0);

    while (accumulator >= Game.Step)
    {
      game.Update(direction, window.Down(GameKey.Jump));
      accumulator -= Game.Step;
    }

    renderer.Draw(game, direction, elapsed);
    window.Present(renderer.Frame);
    Thread.Sleep(16);
  }
}
catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException
  or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
{
#if !NETMAN_DISTRIBUTION
  Console.Error.WriteLine($"NetMan: {exception.Message}");
#endif

  if (OperatingSystem.IsWindows() && !args.Contains("--self-test") && !args.Contains("--export-art"))
    WindowsGameWindow.ShowError(exception.Message);

  Environment.ExitCode = 1;
}
