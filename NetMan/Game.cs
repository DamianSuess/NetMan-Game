namespace NetMan;

public sealed class Enemy(double x, double y)
{
  public const int SquashDurationTicks = 60;

  public int Direction = -1;
  public double X = x, Y = y, VelocityY;
  public bool Alive { get; private set; } = true;
  public int SquashTicksRemaining { get; private set; }

  internal void AdvanceSquash()
  {
    if (SquashTicksRemaining > 0)
      SquashTicksRemaining--;
  }

  internal void Squash()
  {
    if (!Alive)
      return;

    Alive = false;
    VelocityY = 0;
    SquashTicksRemaining = SquashDurationTicks;
  }
}

public sealed class Game
{
  public const double BodyWidth = 0.75, BodyHeight = 0.9;
  public const int DeathDurationTicks = 120;
  public const int DamageProtectionTicks = 90;
  public const double LargeBodyHeight = 1.8;
  public const double OverloadedBodyHeight = 2.7;
  // 100 ms is exactly six simulation ticks at 60 Hz.
  public const int ExplosionFrameTicks = 6;
  public const int ExplosionFrameCount = 4;
  public const int ExplosionDurationTicks = ExplosionFrameTicks * ExplosionFrameCount;
  public const double Step = 1.0 / 60;

  // The spike artwork's tip begins three pixels below the top of its 16-pixel tile.
  private const double SpikeTipOffset = 3.0 / 16;

  private readonly List<BlockBreakEffect> _blockBreakEffects = [];
  private readonly string[] _files;
  private readonly string[] _deathQuotes;
  private readonly List<CoinRewardPopup> _rewardPopups = [];
  private double _jumpBuffer, _coyoteTime, _transition;
  private int _levelIndex;
  private bool _growthPending;
  private bool _overloadPending;

  public Game(string[] files, string? deathQuotesPath = null)
  {
    this._files = files;
    deathQuotesPath ??= Path.Combine(AppContext.BaseDirectory, "Assets", "death-quotes.txt");
    _deathQuotes = File.Exists(deathQuotesPath)
      ? File.ReadAllLines(deathQuotesPath).Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith(';')).ToArray()
      : [];
    if (_deathQuotes.Length == 0)
      _deathQuotes = ["Gravity wins again."];

    LoadLevel();
  }

  public IReadOnlyList<BlockBreakEffect> BlockBreakEffects => _blockBreakEffects;
  public int Coins { get; private set; }
  public int DeathTicksRemaining { get; private set; }
  public string DeathQuote { get; private set; } = string.Empty;
  public int DamageTicksRemaining { get; private set; }
  public List<Enemy> Enemies { get; private set; } = [];
  public bool Grounded { get; private set; }
  public bool IsDying => DeathTicksRemaining > 0;
  public bool IsLarge { get; private set; }
  public bool IsExploding { get; private set; }
  public bool IsExplosionVisible => IsExploding && DeathDurationTicks - DeathTicksRemaining < ExplosionDurationTicks;
  public int ExplosionFrameIndex => IsExploding
    ? Math.Min(ExplosionFrameCount - 1, (DeathDurationTicks - DeathTicksRemaining) / ExplosionFrameTicks) : 0;
  public double PlayerHeight => IsExploding ? OverloadedBodyHeight : IsLarge ? LargeBodyHeight : BodyHeight;
  public Level Level { get; private set; } = null!;
  public int LevelCount => _files.Length;
  public int LevelNumber => _levelIndex + 1;
  public int Lives { get; private set; } = 3;
  public string Message { get; private set; } = "Let's go!";
  public bool Paused { get; set; }
  public IReadOnlyList<CoinRewardPopup> RewardPopups => _rewardPopups;
  public int Score { get; private set; }
  public double VelocityY { get; private set; }
  public bool Won { get; private set; }
  public double X { get; private set; }
  public double Y { get; private set; }

  public void QueueJump()
  {
    if (!Paused && !Won && !IsDying && Lives > 0)
      _jumpBuffer = 0.12;
  }

  public void Restart()
  {
    _levelIndex = 0;
    Lives = 3;
    Coins = Score = 0;
    Won = Paused = false;
    _transition = 0;

    Message = "Let's go!";
    LoadLevel();
  }

  public void Update(int direction, bool jumpHeld)
  {
    if (Paused)
      return;

    for (int i = _blockBreakEffects.Count - 1; i >= 0; i--)
    {
      var effect = _blockBreakEffects[i] with { AgeTicks = _blockBreakEffects[i].AgeTicks + 1 };

      if (effect.AgeTicks >= BlockBreakEffect.DurationTicks)
        _blockBreakEffects.RemoveAt(i);
      else
        _blockBreakEffects[i] = effect;
    }

    foreach (var enemy in Enemies)
      enemy.AdvanceSquash();

    for (int i = _rewardPopups.Count - 1; i >= 0; i--)
    {
      var popup = _rewardPopups[i] with { AgeTicks = _rewardPopups[i].AgeTicks + 1 };

      if (popup.AgeTicks >= CoinRewardPopup.DurationTicks)
        _rewardPopups.RemoveAt(i);
      else
        _rewardPopups[i] = popup;
    }

    if (Won)
      return;

    if (IsDying)
    {
      DeathTicksRemaining--;

      if (!IsDying)
      {
        if (Lives > 0)
        {
          LoadLevel();
          Message = "Try again!";
        }
        else
          Restart();
      }

      return;
    }

    if (Lives == 0)
      return;

    if (_transition > 0)
    {
      _transition -= Step;
      return;
    }

    _jumpBuffer -= Step;
    if (DamageTicksRemaining > 0)
      DamageTicksRemaining--;

    _coyoteTime = Grounded ? 0.1 : _coyoteTime - Step;

    if (_jumpBuffer > 0 && _coyoteTime > 0)
    {
      VelocityY = -15;
      Grounded = false;
      _jumpBuffer = _coyoteTime = 0;
    }

    // Releasing jump early produces a shorter hop.
    VelocityY = Math.Min(VelocityY + (VelocityY < 0 && !jumpHeld ? 75 : 40) * Step, 28);

    double x = X, y = Y;
    double previousFeet = Y + PlayerHeight;
    Move(ref x, ref y, Math.Clamp(direction, -1, 1) * 12 * Step, true, false, out _, PlayerHeight);

    var vertical = VelocityY * Step;
    Move(ref x, ref y, vertical, false, true, out bool hitY, PlayerHeight);

    Grounded = hitY && vertical > 0;

    if (hitY)
      VelocityY = 0;

    X = x;
    Y = y;

    if (_overloadPending)
    {
      Explode();
      return;
    }

    if (vertical > 0 && TryFindSpikeLanding(previousFeet, out double spikeTop))
    {
      Y = spikeTop - PlayerHeight;
      TakeDamage();
      if (IsDying)
        return;

      Grounded = true;
      VelocityY = 0;
    }

    TryGrow();

    foreach (var (tx, ty) in Cells(X, Y, PlayerHeight))
    {
      switch (Level.Tile(tx, ty))
      {
        case 'o':
          Level.Clear(tx, ty);
          Coins++;
          Score += 100;
          break;

        case 'G':
          Score += 1000;
          if (_levelIndex + 1 == _files.Length)
          {
            Won = true;
            Message = "YOU WIN! R to play again";
          }
          else
          {
            _levelIndex++;
            LoadLevel(preserveGrowth: true);
            _transition = 0.6;
            Message = "Next level!";
          }

          return;
      }
    }

    if (Y > Level.Height + 2)
    {
      Die();
      return;
    }

    foreach (var enemy in Enemies.Where(enemy => enemy.Alive))
    {
      double ex = enemy.X, ey = enemy.Y;
      Move(ref ex, ref ey, enemy.Direction * 3 * Step, true, false, out bool wall);

      if (wall)
        enemy.Direction *= -1;

      enemy.VelocityY = Math.Min(enemy.VelocityY + 40 * Step, 28);
      Move(ref ex, ref ey, enemy.VelocityY * Step, false, false, out bool floor);

      if (floor)
        enemy.VelocityY = 0;

      enemy.X = ex; enemy.Y = ey;

      if (ey > Level.Height + 2)
      {
        enemy.Y = Level.Height - BodyHeight;
        enemy.Squash();
      }

      if (!enemy.Alive || !Overlaps(X, Y, PlayerHeight, ex, ey))
        continue;

      if (vertical > 0 && Y + PlayerHeight - vertical <= ey + 0.25)
      {
        enemy.Squash();
        VelocityY = -10;
        Score += 200;
        Message = "Stomp!";
      }
      else
      {
        TakeDamage();
        if (IsDying)
          return;
      }
    }
  }

  private static IEnumerable<(int X, int Y)> Cells(double x, double y, double height = BodyHeight)
  {
    for (int ty = (int)Math.Floor(y + 0.00001);
      ty <= (int)Math.Floor(y + height - 0.00001); ty++)
    {
      for (int tx = (int)Math.Floor(x + 0.00001); tx <= (int)Math.Floor(x + BodyWidth - 0.00001); tx++)
        yield return (tx, ty);
    }
  }

  private static bool Overlaps(double ax, double ay, double height, double bx, double by) =>
    ax < bx + BodyWidth && ax + BodyWidth > bx && ay < by + BodyHeight && ay + height > by;

  private void TryGrow()
  {
    if (!_growthPending || IsLarge)
      return;

    double grownY = Y + BodyHeight - LargeBodyHeight;

    // Wait for headroom instead of growing into the block that awarded the power.
    if (grownY < 0 || Cells(X, grownY, LargeBodyHeight).Any(cell => Level.Solid(cell.X, cell.Y)))
      return;

    Y = grownY;
    IsLarge = true;
    _growthPending = false;
    Message = "Powered up!";
  }

  private void TakeDamage()
  {
    if (DamageTicksRemaining > 0)
      return;

    if (!IsLarge)
    {
      Die();
      return;
    }

    Y += LargeBodyHeight - BodyHeight;
    IsLarge = false;
    _growthPending = false;
    DamageTicksRemaining = DamageProtectionTicks;
    Message = "Shrunk! Keep moving!";
  }

  private void Explode()
  {
    double feet = Y + PlayerHeight;
    Die();
    IsExploding = true;
    Y = feet - OverloadedBodyHeight;
    Message = "Power overload!";
  }

  private void Die()
  {
    if (IsDying || Lives == 0)
      return;

    Lives--;
    DeathQuote = _deathQuotes[Random.Shared.Next(_deathQuotes.Length)];
    Y += PlayerHeight - BodyHeight;
    IsExploding = false;
    IsLarge = false;
    _growthPending = false;
    _overloadPending = false;
    DamageTicksRemaining = 0;
    DeathTicksRemaining = DeathDurationTicks;
    VelocityY = 0;
    Grounded = false;
    _jumpBuffer = _coyoteTime = 0;

    Message = Lives == 0 ? "Ouch! Restarting game..." : "Ouch!";
  }

  private void LoadLevel(bool preserveGrowth = false)
  {
    bool keepGrowth = preserveGrowth && (IsLarge || _growthPending);
    IsExploding = false;
    IsLarge = false;
    _growthPending = keepGrowth;
    _overloadPending = false;
    DamageTicksRemaining = 0;
    Level = Level.Load(_files[_levelIndex]);
    _rewardPopups.Clear();
    _blockBreakEffects.Clear();

    X = Level.Spawn.X;
    Y = Level.Spawn.Y;
    VelocityY = 0;
    DeathTicksRemaining = 0;
    DeathQuote = string.Empty;

    Grounded = false;
    _jumpBuffer = _coyoteTime = 0;

    Enemies = Level.EnemySpawns.Select(spawn => new Enemy(spawn.X, spawn.Y)).ToList();
    TryGrow();
  }

  private void Move(ref double x, ref double y, double distance, bool horizontal, bool bump, out bool hit, double height = BodyHeight)
  {
    hit = false;
    int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / 0.15));

    for (int i = 0; i < steps; i++)
    {
      double nx = x + (horizontal ? distance / steps : 0);
      double ny = y + (horizontal ? 0 : distance / steps);
      double previousX = x;
      double bodyBottom = y + height;

      // Spike sides block entry like a pipe. Vertical contact is handled separately at the tip.
      var collisions = Cells(nx, ny, height).Where(
        cell => Level.Solid(cell.X, cell.Y) ||
        (horizontal
        && Level.Tile(cell.X, cell.Y) == '^'
        && bodyBottom > cell.Y + SpikeTipOffset + 0.00001
        && ((distance > 0 && previousX + BodyWidth <= cell.X + 0.00001) || (distance < 0 && previousX >= cell.X + 1 - 0.00001))
        )).ToArray();

      if (collisions.Length == 0)
      {
        x = nx;
        y = ny;
        continue;
      }

      hit = true;

      foreach (var cell in collisions)
      {
        if (bump && distance < 0)
        {
          if (Level.Tile(cell.X, cell.Y) == 'M')
          {
            Level.UseBlock(cell.X, cell.Y);
            Score += 100;
            if (IsLarge || _growthPending)
              _overloadPending = true;
            else
              _growthPending = true;

            Message = "Growth power!";
          }
          else if (Level.Tile(cell.X, cell.Y) == '?')
          {
            Level.UseBlock(cell.X, cell.Y);
            Coins++;
            Score += 100;
            Message = "Bonus coin!";

            _rewardPopups.Add(new CoinRewardPopup(cell.X, cell.Y));
          }
          else if (Level.Tile(cell.X, cell.Y) == 'B')
          {
            Level.Clear(cell.X, cell.Y);
            Score += 50;
            _blockBreakEffects.Add(new BlockBreakEffect(cell.X, cell.Y));
          }
        }
      }

      if (horizontal)
        x = distance > 0 ? collisions.Min(c => c.X) - BodyWidth : collisions.Max(c => c.X) + 1;
      else
        y = distance > 0 ? collisions.Min(c => c.Y) - height : collisions.Max(c => c.Y) + 1;
      break;
    }
  }

  private bool TryFindSpikeLanding(double previousFeet, out double spikeTop)
  {
    double currentFeet = Y + PlayerHeight;
    spikeTop = 0;

    if (currentFeet <= previousFeet + 0.00001)
      return false;

    // Sweep the feet across the whole downward movement so fast falls cannot skip a tip.
    for (int ty = (int)Math.Floor(previousFeet);
         ty <= (int)Math.Floor(currentFeet);
         ty++)
    {
      double tip = ty + SpikeTipOffset;

      if (previousFeet > tip + 0.00001 || currentFeet < tip)
        continue;

      for (int tx = (int)Math.Floor(X + 0.00001);
           tx <= (int)Math.Floor(X + BodyWidth - 0.00001);
           tx++)
      {
        if (Level.Tile(tx, ty) != '^')
          continue;

        spikeTop = tip;
        return true;
      }
    }

    return false;
  }
}
