namespace NetMan;

public sealed class Level
{
  private readonly char[,] _tiles;

  public string Name { get; }

  public int Width => _tiles.GetLength(0);

  public int Height => _tiles.GetLength(1);

  public (int X, int Y) Spawn { get; private set; }

  public List<(int X, int Y)> EnemySpawns { get; } = [];
  public List<(int X, int Y)> ToasterCatSpawns { get; } = [];
  public List<(int X, int Y)> ZombieSpawns { get; } = [];
  public List<(int X, int Y)> DogSpawns { get; } = [];

  private Level(string name, char[,] tiles)
  {
    Name = name;
    this._tiles = tiles;
  }

  public static Level Load(string path) => Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllLines(path));

  public static Level Parse(string name, string[] lines)
  {
    if (lines.Length < 3 || lines.Length > 100 || lines.Max(line => line.Length) is < 10 or > 10000)
      throw new InvalidDataException($"{name}: maps must be 10–10000 columns wide and 3–100 rows tall.");

    var map = new Level(name, new char[lines.Max(line => line.Length), lines.Length]);
    int starts = 0, goals = 0;

    for (int y = 0; y < map.Height; y++)
    {
      for (int x = 0; x < map.Width; x++)
      {
        char tile = x < lines[y].Length ? lines[y][x] : '.';
        if (!". #B?Mo^PpSGECZD".Contains(tile))
          throw new InvalidDataException($"{name}: unknown tile '{tile}' at column {x + 1}, row {y + 1}.");

        if (tile == 'S')
        {
          map.Spawn = (x, y);
          starts++;
          tile = '.';
        }

        if (tile is 'E' or 'C' or 'Z' or 'D')
        {
          var spawns = tile switch
          {
            'C' => map.ToasterCatSpawns,
            'Z' => map.ZombieSpawns,
            'D' => map.DogSpawns,
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

    return map;
  }

  public char Tile(int x, int y) => x < 0 || x >= Width || y < 0 || y >= Height ? '.' : _tiles[x, y];

  public bool Solid(int x, int y) => x < 0 || x >= Width || y < 0 || "#B?MPpX".Contains(Tile(x, y));

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
