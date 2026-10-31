namespace SamElla;

public sealed class Level
{
  public const int DefaultTimeLimitSeconds = 100;
  public const int MaxNameLength = 32;

  private readonly char[,] _tiles;

  public string Name { get; }
  public string DisplayName { get; }
  public int? Section { get; }
  public int? SectionLevel { get; }
  public string? SectionLevelNumber => Section.HasValue ? $"{Section:00}-{SectionLevel:00}" : null;

  public BackgroundTheme Background { get; }
  public bool EnemiesAvoidLedges { get; }
  public int TimeLimitSeconds { get; }

  public int Width => _tiles.GetLength(0);

  public int Height => _tiles.GetLength(1);

  public (int X, int Y) Spawn { get; private set; }

  public List<(int X, int Y)> EnemySpawns { get; } = [];
  public List<(int X, int Y)> ToasterCatSpawns { get; } = [];
  public List<(int X, int Y)> ZombieSpawns { get; } = [];
  public List<(int X, int Y)> DogSpawns { get; } = [];
  public List<(int X, int Y)> CreepyCrawlerSpawns { get; } = [];

  private Level(string name, char[,] tiles, BackgroundTheme background, bool enemiesAvoidLedges, int timeLimitSeconds, string displayName)
  {
    Name = name;
    DisplayName = displayName;
    string[] parts = name.Split('-', 3);
    if (parts.Length == 3 && parts[0].Length == 2 && parts[1].Length == 2
      && parts[0].All(char.IsAsciiDigit) && parts[1].All(char.IsAsciiDigit)
      && int.TryParse(parts[0], out int section) && section > 0
      && int.TryParse(parts[1], out int sectionLevel) && sectionLevel > 0)
    {
      Section = section;
      SectionLevel = sectionLevel;
    }

    Background = background;
    EnemiesAvoidLedges = enemiesAvoidLedges;
    TimeLimitSeconds = timeLimitSeconds;
    this._tiles = tiles;
  }

  public static Level Load(string path) => Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllLines(path));

  public static Level Parse(string name, string[] lines)
  {
    string fallbackName = name;
    for (int prefix = 0; prefix < 2; prefix++)
    {
      int separator = fallbackName.IndexOf('-');
      if (separator <= 0 || !fallbackName[..separator].All(char.IsAsciiDigit))
        break;

      fallbackName = fallbackName[(separator + 1)..];
    }

    string displayName = fallbackName.Replace('-', ' ').Replace('_', ' ');
    if (displayName.Length > MaxNameLength)
      displayName = displayName[..MaxNameLength];

    var background = BackgroundTheme.Day;
    bool enemiesAvoidLedges = false;
    int timeLimitSeconds = DefaultTimeLimitSeconds;
    int mapStart = 0;
    while (mapStart < lines.Length && lines[mapStart].Contains('='))
    {
      string[] setting = lines[mapStart].Split('=', 2, StringSplitOptions.TrimEntries);
      if (setting[0].Equals("Name", StringComparison.OrdinalIgnoreCase))
      {
        if (setting[1].Length is < 1 or > MaxNameLength || setting[1].Any(char.IsControl))
          throw new InvalidDataException($"{name}: line {mapStart + 1}: Name must contain 1..{MaxNameLength} display characters.");

        displayName = setting[1];
      }
      else if (setting[0].Equals("Background", StringComparison.OrdinalIgnoreCase)
        && (setting[1].Equals("Day", StringComparison.OrdinalIgnoreCase) || setting[1].Equals("Night", StringComparison.OrdinalIgnoreCase)))
        background = setting[1].Equals("Night", StringComparison.OrdinalIgnoreCase) ? BackgroundTheme.Night : BackgroundTheme.Day;
      else if (setting[0].Equals("EnemyLedges", StringComparison.OrdinalIgnoreCase)
        && (setting[1].Equals("Turn", StringComparison.OrdinalIgnoreCase) || setting[1].Equals("Fall", StringComparison.OrdinalIgnoreCase)))
        enemiesAvoidLedges = setting[1].Equals("Turn", StringComparison.OrdinalIgnoreCase);
      else if (setting[0].Equals("TimeLimit", StringComparison.OrdinalIgnoreCase)
        && int.TryParse(setting[1], out int seconds) && seconds is >= 1 and <= 9999)
        timeLimitSeconds = seconds;
      else
        throw new InvalidDataException($"{name}: line {mapStart + 1}: expected Name=1..32 display characters, Background=Day or Background=Night, EnemyLedges=Turn/Fall, or TimeLimit=1..9999 seconds.");

      mapStart++;
      while (mapStart < lines.Length && string.IsNullOrWhiteSpace(lines[mapStart]))
        mapStart++;
    }

    lines = lines[mapStart..];
    if (lines.Length < 3 || lines.Length > 100 || lines.Max(line => line.Length) is < 10 or > 10000)
      throw new InvalidDataException($"{name}: maps must be 10–10000 columns wide and 3–100 rows tall.");

    var map = new Level(name, new char[lines.Max(line => line.Length), lines.Length], background, enemiesAvoidLedges, timeLimitSeconds, displayName);
    int starts = 0, goals = 0;

    for (int y = 0; y < map.Height; y++)
    {
      for (int x = 0; x < map.Width; x++)
      {
        char tile = x < lines[y].Length ? lines[y][x] : '.';
        if (!". #B?MFQo^PpSGECZDR".Contains(tile))
          throw new InvalidDataException($"{name}: unknown tile '{tile}' at column {x + 1}, row {y + 1}.");

        if (tile == 'S')
        {
          map.Spawn = (x, y);
          starts++;
          tile = '.';
        }

        if (tile is 'E' or 'C' or 'Z' or 'D' or 'R')
        {
          var spawns = tile switch
          {
            'C' => map.ToasterCatSpawns,
            'Z' => map.ZombieSpawns,
            'D' => map.DogSpawns,
            'R' => map.CreepyCrawlerSpawns,
            _ => map.EnemySpawns,
          };

          spawns.Add((x, y));
          tile = '.';
        }

        if (tile == 'G')
          goals++;

        map._tiles[x, y] = tile;
      }
    }

    if (starts != 1 || goals == 0)
      throw new InvalidDataException($"{name}: use exactly one S (start) and at least one G (goal).");

    foreach (var spawn in map.ZombieSpawns)
    {
      if (spawn.Y == 0 || map.Solid(spawn.X, spawn.Y - 1))
        throw new InvalidDataException($"{name}: zombie at column {spawn.X + 1}, row {spawn.Y + 1} needs an empty tile above it.");
    }

    foreach (var spawn in map.CreepyCrawlerSpawns)
    {
      if (background != BackgroundTheme.Night)
        throw new InvalidDataException($"{name}: Creepy-Crawler at column {spawn.X + 1}, row {spawn.Y + 1} requires Background=Night.");

      if (spawn.Y + 1 >= map.Height || !map.Solid(spawn.X, spawn.Y + 1))
        throw new InvalidDataException($"{name}: Creepy-Crawler at column {spawn.X + 1}, row {spawn.Y + 1} needs solid terrain directly below it.");
    }

    return map;
  }

  public char Tile(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? '.' : _tiles[x, y];

  public bool Solid(int x, int y) => x < 0 || x >= Width || y < 0 || "#B?MFQPpX".Contains(Tile(x, y));

  public void Clear(int x, int y)
  {
    if (Inside(x, y))
      _tiles[x, y] = '.';
  }

  public void UseBlock(int x, int y)
  {
    if (Inside(x, y))
      _tiles[x, y] = 'X';
  }

  private bool Inside(int x, int y) =>
    x >= 0 && x < Width && y >= 0 && y < Height;
}
