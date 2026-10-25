using System.Globalization;

namespace NetMan;

internal sealed class EnemyConfiguration
{
  public static EnemySettings DefaultWalker { get; } = new(3, 1, 0);
  public static EnemySettings DefaultToasterCat { get; } = new(4, 2, 7);

  public EnemySettings Walker { get; private set; } = DefaultWalker;
  public EnemySettings ToasterCat { get; private set; } = DefaultToasterCat;

  public static EnemyConfiguration Load(string path)
  {
    var config = new EnemyConfiguration();
    if (!File.Exists(path))
      return config;

    string section = string.Empty;
    int lineNumber = 0;
    foreach (string raw in File.ReadLines(path))
    {
      lineNumber++;
      string line = raw.Trim();
      if (line.Length == 0 || line.StartsWith(';'))
        continue;

      if (line is "[Walker]" or "[ToasterCat]")
      {
        section = line[1..^1];
        continue;
      }

      string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
      if (section.Length == 0 || parts.Length != 2)
        throw new InvalidDataException($"{path}:{lineNumber}: expected [Walker], [ToasterCat], or Setting=Value.");

      var current = section == "Walker" ? config.Walker : config.ToasterCat;
      try
      {
        EnemySettings updated = parts[0] switch
        {
          "Speed" => new(double.Parse(parts[1], CultureInfo.InvariantCulture), current.PowerLevel, current.HopVelocity),
          "PowerLevel" => new(current.Speed, int.Parse(parts[1], CultureInfo.InvariantCulture), current.HopVelocity),
          "HopVelocity" => new(current.Speed, current.PowerLevel, double.Parse(parts[1], CultureInfo.InvariantCulture)),
          _ => throw new InvalidDataException($"{path}:{lineNumber}: unknown enemy setting '{parts[0]}'."),
        };
        if (section == "Walker")
          config.Walker = updated;
        else
          config.ToasterCat = updated;
      }
      catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentOutOfRangeException)
      {
        throw new InvalidDataException($"{path}:{lineNumber}: invalid {parts[0]} value '{parts[1]}'. {exception.Message}", exception);
      }
    }

    return config;
  }
}
