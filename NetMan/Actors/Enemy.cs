namespace NetMan.Actors;

public sealed class Enemy
{
  public const int SquashDurationTicks = 60;
  public const int HopIntervalTicks = 24;
  public const int StompStunTicks = 20;
  public const int PoopIntervalTicks = 180;
  public const int CrawlerEmergenceDurationTicks = 60;
  public const double CrawlerBodyHeight = 0.5;
  public const double CrawlerWakeDistance = 12;

  public Enemy(double x, double y, EnemyKind kind = EnemyKind.Walker, EnemySettings? settings = null)
  {
    X = x;
    Y = y;
    Kind = kind;
    Settings = settings ?? (kind switch
    {
      EnemyKind.ToasterCat => EnemyConfiguration.DefaultToasterCat,
      EnemyKind.Zombie => EnemyConfiguration.DefaultZombie,
      EnemyKind.Dog => EnemyConfiguration.DefaultDog,
      EnemyKind.CreepyCrawler => EnemyConfiguration.DefaultCreepyCrawler,
      _ => EnemyConfiguration.DefaultWalker,
    });

    Health = Settings.PowerLevel;
    IsBuried = kind == EnemyKind.CreepyCrawler;
  }

  public int Direction = -1;

  public double X, Y, VelocityY;

  public EnemyKind Kind { get; }

  public EnemySettings Settings { get; }

  public double BodyHeight => Kind switch
  {
    EnemyKind.Zombie => Game.LargeBodyHeight,
    EnemyKind.CreepyCrawler => CrawlerBodyHeight,
    _ => Game.BodyHeight,
  };

  public bool IsBuried { get; private set; }
  public int EmergenceTicksRemaining { get; private set; }
  public bool IsEmerging => EmergenceTicksRemaining > 0;
  public int EmergenceFrameIndex => Math.Min(2, (CrawlerEmergenceDurationTicks - EmergenceTicksRemaining) / 20);
  public bool CanInteract => Alive && !IsBuried && !IsEmerging;

  public int Health { get; private set; }

  public bool Alive { get; private set; } = true;

  public int SquashTicksRemaining { get; private set; }

  internal int HopTicksRemaining { get; set; }

  internal int StunTicksRemaining { get; set; }

  internal int PoopTicksRemaining { get; set; } = PoopIntervalTicks;

  internal bool AdvanceEmergence(double playerX, double playerY)
  {
    if (Kind != EnemyKind.CreepyCrawler)
      return false;

    if (IsBuried)
    {
      if (Math.Abs(X - playerX) <= CrawlerWakeDistance && Math.Abs(Y - playerY) <= 4)
      {
        IsBuried = false;
        EmergenceTicksRemaining = CrawlerEmergenceDurationTicks;
        Direction = playerX < X ? -1 : 1;
      }

      return true;
    }

    if (IsEmerging)
    {
      EmergenceTicksRemaining--;
      return true;
    }

    return false;
  }

  internal void AdvanceSquash()
  {
    if (SquashTicksRemaining > 0)
      SquashTicksRemaining--;
  }

  internal bool Stomp(int damage = 1)
  {
    if (damage is < 1 or > 10)
      throw new ArgumentOutOfRangeException(nameof(damage));

    return Hit(damage);
  }

  internal bool Hit(int damage)
  {
    if (damage is < 1 or > 12)
      throw new ArgumentOutOfRangeException(nameof(damage));

    if (!CanInteract)
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
