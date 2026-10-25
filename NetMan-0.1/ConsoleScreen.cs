using System.Runtime.Versioning;
using System.Text;

namespace NetBro;

[SupportedOSPlatform("windows")]
internal sealed class ConsoleScreen : IDisposable
{
  private readonly bool _cursorVisible;
  private readonly ConsoleColor _foreground = Console.ForegroundColor;
  private readonly ConsoleColor _background = Console.BackgroundColor;

  public ConsoleScreen()
  {
    _cursorVisible = Console.CursorVisible;
    Console.CursorVisible = false;
    Console.Write("\x1b[?1049h\x1b[2J");
  }

  public void Draw(Game game)
  {
    int width = Math.Clamp(Console.WindowWidth - 1, 1, 140);
    int height = Math.Clamp(Console.WindowHeight - 1, 1, 45);
    var output = new StringBuilder("\x1b[H\x1b[0m");

    if (width < 45 || height < 15)
    {
      output.Append("Enlarge console to at least 46 x 16.\x1b[J");
      Console.Write(output); return;
    }

    int viewHeight = height - 4;
    int cameraX = Math.Clamp((int)game.X - width / 3, 0, Math.Max(0, game.Level.Width - width));
    int cameraY = Math.Clamp((int)game.Y - viewHeight / 2, 0, Math.Max(0, game.Level.Height - viewHeight));

    void Line(string text)
    {
      output.Append(text[..Math.Min(text.Length, width)].PadRight(width)).Append("\x1b[0m\r\n");
    }

    Line($"NETBRO   Score {game.Score:000000}   Coins {game.Coins:00}   Lives {game.Lives}   {game.LevelNumber}/{game.LevelCount}");
    Line($"{game.Level.Name}  |  {(game.Paused ? "PAUSED - P to resume" : game.Message)}");

    var enemies = game.Enemies.Where(e => e.Alive).Select(e => ((int)Math.Floor(e.X), (int)Math.Floor(e.Y))).ToHashSet();

    for (int row = 0; row < viewHeight; row++)
    {
      int lastColor = -1;

      for (int col = 0; col < width; col++)
      {
        int x = cameraX + col, y = cameraY + row;
        char tile = game.Level.Tile(x, y);

        (char glyph, int color) = tile switch
        {
          '#' => ('#', 33),
          'B' => ('=', 31),
          '?' => ('?', 93),
          'X' => ('=', 90),
          'P' => ('H', 92),
          'o' => ('o', 93),
          '^' => ('^', 91),
          'G' => ('F', 92),
          _ => (' ', 37)
        };

        if (enemies.Contains((x, y)))
        {
          glyph = 'm';
          color = 91;
        }

        if ((int)Math.Floor(game.X) == x && (int)Math.Floor(game.Y) == y)
        {
          glyph = '@';
          color = 97;
        }

        if (color != lastColor)
        {
          output.Append($"\x1b[{color}m");
          lastColor = color;
        }

        output.Append(glyph);
      }

      output.Append("\x1b[0m\r\n");
    }

    Line("A/D or arrows: move   Space/W/Up: jump");
    output.Append("P: pause   R: restart   Esc: quit".PadRight(width)[..width]).Append("\x1b[J");
    Console.Write(output);
  }

  public void Dispose()
  {
    Console.Write("\x1b[0m\x1b[?1049l");
    Console.ForegroundColor = _foreground; Console.BackgroundColor = _background;
    Console.CursorVisible = _cursorVisible;
  }
}
