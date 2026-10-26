namespace NetMan;

public sealed class Enemy
{
  public const int SquashDurationTicks = 60;
  public const int HopIntervalTicks = 24;
  public const int StompStunTicks = 20;

  public Enemy(double x, double y, EnemyKind kind = EnemyKind.Walker, EnemySettings? settings = null)
  {
    X = x;
    Y = y;
    Kind = kind;
    Settings = settings ?? (kind switch
    {
      EnemyKind.ToasterCat => EnemyConfiguration.DefaultToasterCat,
      EnemyKind.Zombie => EnemyConfiguration.DefaultZombie,
      _ => EnemyConfiguration.DefaultWalker,
    });
    Health = Settings.PowerLevel;
  }

  public int Direction = -1;
  public double X, Y, VelocityY;
  public EnemyKind Kind { get; }
  public EnemySettings Settings { get; }
  public double BodyHeight => Kind == EnemyKind.Zombie ? Game.LargeBodyHeight : Game.BodyHeight;
  public int Health { get; private set; }
  public bool Alive { get; private set; } = true;
  public int SquashTicksRemaining { get; private set; }
  internal int HopTicksRemaining { get; set; }
  internal int StunTicksRemaining { get; set; }

  internal void AdvanceSquash()
  {
    if (SquashTicksRemaining > 0)
      SquashTicksRemaining--;
  }

  internal bool Stomp(int damage = 1)
  {
    if (damage is < 1 or > 10)
      throw new ArgumentOutOfRangeException(nameof(damage));

    if (!Alive)
      return false;

    Health = Math.Max(0, Health - damage);
    if (Health == 0)
    {
      Squash();
      return true;
    }

    VelocityY = 0;
    StunTicksRemaining = StompStunTicks;
    return false;
  }

  internal void Squash()
  {
    if (!Alive)
      return;

    Alive = false;
    Health = 0;
    VelocityY = 0;
    SquashTicksRemaining = SquashDurationTicks;
  }
}
