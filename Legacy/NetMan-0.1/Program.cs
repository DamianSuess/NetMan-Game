using System.Diagnostics;
using NetBro;

if (args.Contains("--self-test"))
{
  SelfTests.Run();
  return;
}

if (!OperatingSystem.IsWindows() || Console.IsInputRedirected || Console.IsOutputRedirected)
{
  Console.Error.WriteLine("NetBro requires an interactive Windows console. Run dotnet run in Windows Terminal.");
  return;
}

try
{
  var levelsPath = Path.Combine(AppContext.BaseDirectory, "Levels");
  var option = Array.IndexOf(args, "--levels");

  if (option >= 0)
  {
    if (option + 1 >= args.Length)
      throw new ArgumentException("Usage: --levels <directory>");

    levelsPath = Path.GetFullPath(args[option + 1]);
  }

  var files = Directory.GetFiles(levelsPath, "*.txt").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray();

  if (files.Length == 0)
    throw new InvalidDataException($"No .txt levels found in {levelsPath}");

  // Validate every map up front, including levels reached later in the game.
  foreach (var file in files)
    Level.Load(file);

  using var screen = new ConsoleScreen();
  var game = new Game(files);
  var clock = Stopwatch.StartNew();
  double previous = clock.Elapsed.TotalSeconds, accumulator = 0;
  bool wasJump = false, wasRestart = false, wasPause = false;

  while (!Keyboard.Down(Keyboard.Escape))
  {
    var now = clock.Elapsed.TotalSeconds;
    accumulator += Math.Min(now - previous, 0.1);
    previous = now;

    var jump = Keyboard.Down(Keyboard.Space) || Keyboard.Down(Keyboard.Up) || Keyboard.Down(Keyboard.W);
    var restart = Keyboard.Down(Keyboard.R);
    var pause = Keyboard.Down(Keyboard.P);

    if (restart && !wasRestart)
      game.Restart();

    if (pause && !wasPause)
      game.Paused = !game.Paused;

    if (jump && !wasJump)
      game.QueueJump();

    wasJump = jump;
    wasRestart = restart;
    wasPause = pause;

    var direction = (Keyboard.Down(Keyboard.Right) || Keyboard.Down(Keyboard.D) ? 1 : 0)
                  - (Keyboard.Down(Keyboard.Left) || Keyboard.Down(Keyboard.A) ? 1 : 0);

    while (accumulator >= Game.Step)
    {
      game.Update(direction, jump);
      accumulator -= Game.Step;
    }

    screen.Draw(game);
    Thread.Sleep(16);
  }
}
catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException)
{
  Console.Error.WriteLine($"NetBro: {exception.Message}");
  Environment.ExitCode = 1;
}
