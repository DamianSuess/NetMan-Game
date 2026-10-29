using NetMan.Actors;
using NetMan.Audio;
using NetMan.Effects;
using NetMan.Items;
using NetMan.Screens;

namespace NetMan;

public sealed class Game
{
  public const double BodyWidth = 0.75, BodyHeight = 0.9;
  public const string BoomStickPickupQuote = "This here is my BOOM STICK!";
  public const int BoomStickQuoteDurationTicks = 180;
  public const int BurnDurationTicks = 180;
  public const int DamageProtectionTicks = 90;
  public const int DeathDurationTicks = 120;
  public const int ExplosionDurationTicks = ExplosionFrameTicks * ExplosionFrameCount;
  public const int ExplosionFrameCount = 4;

  // 100 ms is exactly six simulation ticks at 60 Hz.
  public const int ExplosionFrameTicks = 6;

  public const int FlameDamageIntervalTicks = 18;
  public const int FlameMaxDurationTicks = 120;
  public const double FlameRange = 3;
  public const int FlamethrowerQuoteDurationTicks = 180;
  public const double LargeBodyHeight = 1.8;
  public const double OverloadedBodyHeight = 2.7;
  public const int PoopSlowDurationTicks = 300;
  public const double PoopSpeedMultiplier = 0.5;
  public const int PoopSplatterDurationTicks = 120;
  public const double ShotgunRange = 3;
  public const int ShotgunReloadTicks = 30;
  public const double Step = 1.0 / 60;

  // The spike artwork's tip begins three pixels below the top of its 16-pixel tile.
  private const double SpikeTipOffset = 3.0 / 16;

  private readonly List<BlockBreakEffect> _blockBreakEffects = [];
  private readonly HashSet<(int Level, int X, int Y)> _collectedItems = [];
  private readonly string _creditsAssetsPath;
  private readonly string[] _deathQuotes;
  private readonly EnemyConfiguration _enemyConfiguration;
  private readonly string[] _files;
  private readonly string[] _flamethrowerQuotes;
  private readonly string[] _gameOverQuotes;
  private readonly List<GrowthHeart> _growthHearts = [];
  private readonly HashSet<(int X, int Y)> _levelItems = [];
  private readonly List<PoopPile> _poopPiles = [];
  private readonly List<CoinRewardPopup> _rewardPopups = [];
  private int _facingDirection = 1;
  private bool _fireWasHeld;
  private int _flameAnimationTicks;
  private int _flameDamageTicks;
  private bool _growthPending;
  private double _jumpBuffer, _coyoteTime, _transition;
  private int _levelIndex;
  private int _shotgunReloadTicks;

  public Game(string[] files, string? deathQuotesPath = null, string? creditsAssetsPath = null, string? enemySettingsPath = null,
    string? flamethrowerQuotesPath = null, bool konamiCheat = false, string? gameOverQuotesPath = null,
    int startingLevelIndex = 0)
  {
    if ((uint)startingLevelIndex >= (uint)files.Length)
      throw new ArgumentOutOfRangeException(nameof(startingLevelIndex));

    this._files = files;

    _levelIndex = startingLevelIndex;
    KonamiCheatEnabled = konamiCheat;

    _enemyConfiguration = EnemyConfiguration.Load(enemySettingsPath ?? Path.Combine(AppContext.BaseDirectory, "Assets", "enemies.txt"));
    _creditsAssetsPath = creditsAssetsPath ?? Path.Combine(AppContext.BaseDirectory, "Assets");
    deathQuotesPath ??= Path.Combine(AppContext.BaseDirectory, "Assets", "death-quotes.txt");
    _deathQuotes = File.Exists(deathQuotesPath)
      ? File.ReadAllLines(deathQuotesPath).Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith(';')).ToArray()
      : [];
    if (_deathQuotes.Length == 0)
      _deathQuotes = ["Gravity wins again."];

    flamethrowerQuotesPath ??= Path.Combine(AppContext.BaseDirectory, "Assets", "flamethrower-quotes.txt");
    _flamethrowerQuotes = File.Exists(flamethrowerQuotesPath)
      ? File.ReadAllLines(flamethrowerQuotesPath).Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith(';')).ToArray()
      : [];
    if (_flamethrowerQuotes.Length == 0)
      _flamethrowerQuotes = ["Light Em' If you Got Em!"];

    gameOverQuotesPath ??= Path.Combine(AppContext.BaseDirectory, "Assets", "game-over-quotes.txt");
    _gameOverQuotes = File.Exists(gameOverQuotesPath)
      ? File.ReadAllLines(gameOverQuotesPath).Select(line => line.Trim())
        .Where(line => line.Length > 0 && !line.StartsWith(';')).ToArray()
      : [];
    if (_gameOverQuotes.Length == 0)
      _gameOverQuotes = ["Bet you wish you could rewind? Nope."];

    LoadLevel();
    ApplyCheatStart();
  }

  internal event Action<GameSound>? SoundRequested;

  public bool AwaitingRestart { get; private set; }
  public IReadOnlyList<BlockBreakEffect> BlockBreakEffects => _blockBreakEffects;
  public string BoomStickQuote => BoomStickQuoteTicksRemaining > 0 ? BoomStickPickupQuote : string.Empty;
  public int BoomStickQuoteTicksRemaining { get; private set; }
  public int BurnFrameIndex => (BurnDurationTicks - BurnTicksRemaining) / 6 % 4;
  public int BurnTicksRemaining { get; private set; }
  public PlayerCharacter Character { get; init; } = PlayerCharacter.Sam;
  public int Coins { get; private set; }
  public TimeSpan CompletionTime => TimeSpan.FromTicks(RunTicks * TimeSpan.TicksPerSecond / 60);
  public long CreditsTicks { get; private set; }
  public int DamageTicksRemaining { get; private set; }
  public string DeathQuote { get; private set; } = string.Empty;
  public int DeathTicksRemaining { get; private set; }
  public List<Enemy> Enemies { get; private set; } = [];

  public int ExplosionFrameIndex => IsExploding
    ? Math.Min(ExplosionFrameCount - 1, (DeathDurationTicks - DeathTicksRemaining) / ExplosionFrameTicks) : 0;

  public int FacingDirection => _facingDirection;
  public FlameStream? Flame { get; private set; }
  public int FlameDamage => PlayerPowerLevel + 1;
  public string FlamethrowerQuote { get; private set; } = string.Empty;
  public int FlamethrowerQuoteTicksRemaining { get; private set; }
  public string GameOverQuote { get; private set; } = string.Empty;
  public bool Grounded { get; private set; }
  public IReadOnlyList<GrowthHeart> GrowthHearts => _growthHearts;
  public bool HasBoomStick { get; private set; }
  public bool HasFlamethrower { get; private set; }
  public bool IsBurning => BurnTicksRemaining > 0;
  public bool IsDying => DeathTicksRemaining > 0 || AwaitingRestart;
  public bool IsExploding { get; private set; }
  public bool IsExplosionVisible => IsExploding && DeathDurationTicks - DeathTicksRemaining < ExplosionDurationTicks;
  public bool IsLarge { get; private set; }
  public int ItemsMissed { get; private set; }
  public bool KonamiCheatEnabled { get; }
  public Level Level { get; private set; } = null!;
  public int LevelCount => _files.Length;
  public int LevelNumber => _levelIndex + 1;
  internal int ThemeLevelNumber { get; private set; }
  public int Lives { get; private set; } = 3;
  public string Message { get; private set; } = "Let's effn' go!";
  public bool Paused { get; set; }
  public double PlayerHeight => IsExploding ? OverloadedBodyHeight : IsLarge ? LargeBodyHeight : BodyHeight;
  public int PlayerPowerLevel => PlayerSettings.PowerLevel;
  public PlayerSettings PlayerSettings => IsLarge ? _enemyConfiguration.PlayerPowerLevel2 : _enemyConfiguration.PlayerPowerLevel1;
  public double PlayerSpeed => PlayerSettings.Speed * (SlowTicksRemaining > 0 ? PoopSpeedMultiplier : 1);
  public IReadOnlyList<PoopPile> PoopPiles => _poopPiles;
  public int PoopSplatterTicksRemaining { get; private set; }
  public int PowerUpLevel => HasBoomStick ? 3 : HasFlamethrower ? 2 : IsLarge || _growthPending ? 1 : 0;
  public IReadOnlyList<CoinRewardPopup> RewardPopups => _rewardPopups;
  public long RunTicks { get; private set; }
  public int Score { get; private set; }
  public ShotgunBlast? ShotgunBlast { get; private set; }
  public int ShotgunDamage => FlameDamage + 1;
  public int SlowTicksRemaining { get; private set; }
  public int TotalDeaths { get; private set; }
  public double VelocityY { get; private set; }
  public bool Won { get; private set; }
  public double X { get; private set; }
  public double Y { get; private set; }
  internal EndCreditsScreen? Credits { get; private set; }

  public bool ContinueAfterDeath()
  {
    if (!IsDying)
      return false;

    if (Lives > 0)
    {
      Paused = false;
      LoadLevel();
      Message = "Try again!";
    }
    else
      Restart();

    return true;
  }

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
    RunTicks = CreditsTicks = 0;
    TotalDeaths = ItemsMissed = 0;
    Credits = null;
    _collectedItems.Clear();
    Won = Paused = false;
    _transition = 0;

    Message = "Let's go!";
    LoadLevel();
    ApplyCheatStart();
  }

  public void Update(int direction, bool jumpHeld, bool fireHeld = false)
  {
    if (Paused)
      return;

    Flame = null;

    if (Won)
    {
      ShotgunBlast = null;
      CreditsTicks++;
      return;
    }

    if (!AwaitingRestart)
      RunTicks++;

    if (PoopSplatterTicksRemaining > 0)
      PoopSplatterTicksRemaining--;

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

    if (IsDying)
    {
      if (DeathTicksRemaining > 0)
      {
        DeathTicksRemaining--;
        AwaitingRestart = DeathTicksRemaining == 0;
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

    if (ShotgunBlast is { } blast)
      ShotgunBlast = blast.AgeTicks + 1 >= ShotgunBlast.DurationTicks ? null : blast with { AgeTicks = blast.AgeTicks + 1 };

    if (_shotgunReloadTicks > 0)
      _shotgunReloadTicks--;

    _jumpBuffer -= Step;
    if (BoomStickQuoteTicksRemaining > 0)
      BoomStickQuoteTicksRemaining--;

    if (FlamethrowerQuoteTicksRemaining > 0 && --FlamethrowerQuoteTicksRemaining == 0)
      FlamethrowerQuote = string.Empty;

    if (BurnTicksRemaining > 0)
      BurnTicksRemaining--;

    if (direction != 0)
      _facingDirection = Math.Sign(direction);

    AdvanceGrowthHearts();
    if (DamageTicksRemaining > 0)
      DamageTicksRemaining--;

    _coyoteTime = Grounded ? 0.1 : _coyoteTime - Step;

    if (_jumpBuffer > 0 && _coyoteTime > 0)
    {
      VelocityY = -PlayerSettings.HopVelocity;
      Grounded = false;
      _jumpBuffer = _coyoteTime = 0;
      SoundRequested?.Invoke(GameSound.Jump);
    }

    // Releasing jump early produces a shorter hop.
    VelocityY = Math.Min(VelocityY + (VelocityY < 0 && !jumpHeld ? 75 : 40) * Step, 28);

    double x = X, y = Y;
    double previousFeet = Y + PlayerHeight;
    Move(ref x, ref y, Math.Clamp(direction, -1, 1) * PlayerSpeed * Step, true, false, out _, PlayerHeight);
    if (SlowTicksRemaining > 0)
    {
      SlowTicksRemaining--;
      if (SlowTicksRemaining == 0)
        Message = "Back to full speed!";
    }

    var vertical = VelocityY * Step;
    Move(ref x, ref y, vertical, false, true, out bool hitY, PlayerHeight);

    Grounded = hitY && vertical > 0;

    if (hitY)
      VelocityY = 0;

    X = x;
    Y = y;

    if (vertical > 0 && TryFindSpikeLanding(previousFeet, out double spikeTop))
    {
      Y = spikeTop - PlayerHeight;
      TakeDamage();
      if (IsDying)
        return;

      Grounded = true;
      VelocityY = 0;
    }

    if (CollectGrowthHearts())
      return;

    TryGrow();
    if (HasBoomStick)
    {
      if (IsLarge && fireHeld && !_fireWasHeld && _shotgunReloadTicks == 0)
        FireShotgun();
    }
    else
      UpdateFlame(fireHeld);
    _fireWasHeld = fireHeld;

    foreach (var (tx, ty) in Cells(X, Y, PlayerHeight))
    {
      switch (Level.Tile(tx, ty))
      {
        case 'o':
          Level.Clear(tx, ty);
          _collectedItems.Add((_levelIndex, tx, ty));
          Coins++;
          Score += 100;
          SoundRequested?.Invoke(GameSound.Coin);
          break;

        case 'G':
          Score += 1000;
          ItemsMissed += _levelItems.Count(item => !_collectedItems.Contains((_levelIndex, item.X, item.Y)));
          if (_levelIndex + 1 == _files.Length)
          {
            Won = true;
            CreditsTicks = 0;
            Credits = new EndCreditsScreen(_creditsAssetsPath, CompletionTime, TotalDeaths, Score, ItemsMissed);
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
      bool stunned = enemy.StunTicksRemaining > 0;
      if (stunned)
        enemy.StunTicksRemaining--;
      Move(ref ex, ref ey, stunned ? 0 : enemy.Direction * enemy.Settings.Speed * Step, true, false, out bool wall, enemy.BodyHeight);

      if (wall)
        enemy.Direction *= -1;

      enemy.VelocityY = Math.Min(enemy.VelocityY + 40 * Step, 28);
      double enemyVertical = enemy.VelocityY * Step;
      Move(ref ex, ref ey, enemyVertical, false, false, out bool floor, enemy.BodyHeight);

      if (floor)
      {
        enemy.VelocityY = 0;
        if (!stunned && enemy.Settings.HopVelocity > 0 && enemyVertical > 0)
        {
          if (enemy.HopTicksRemaining > 0)
            enemy.HopTicksRemaining--;
          else
          {
            enemy.VelocityY = -enemy.Settings.HopVelocity;
            enemy.HopTicksRemaining = Enemy.HopIntervalTicks;
          }
        }
      }

      enemy.X = ex; enemy.Y = ey;
      if (enemy.Kind == EnemyKind.Dog && floor && enemyVertical > 0 && !stunned)
        LeavePoop(enemy);

      if (ey > Level.Height + 2)
      {
        enemy.Y = Level.Height - enemy.BodyHeight;
        enemy.Squash();
      }

      if (!enemy.Alive || !Overlaps(X, Y, PlayerHeight, ex, ey, enemy.BodyHeight))
        continue;

      if (vertical > 0 && Y + PlayerHeight - vertical <= ey + 0.25)
      {
        bool defeated = enemy.Stomp(PlayerPowerLevel);
        Y = ey - PlayerHeight;
        VelocityY = -10;

        if (defeated)
        {
          Score += 200 * enemy.Settings.PowerLevel;
        }

        SoundRequested?.Invoke(defeated ? GameSound.EnemyDied : GameSound.EnemySmash);
        Message = defeated ? "Stomp!" : $"Still kicking! {enemy.Health} hits left";
      }
      else
      {
        TakeDamage();
        if (IsDying)
          return;
      }
    }

    StepInPoop();
  }

  // The level-start page replaces the short in-game transition delay.
  internal void CompleteLevelStart() => _transition = 0;

  private static IEnumerable<(int X, int Y)> Cells(double x, double y, double height = BodyHeight)
  {
    for (int ty = (int)Math.Floor(y + 0.00001);
      ty <= (int)Math.Floor(y + height - 0.00001); ty++)
    {
      for (int tx = (int)Math.Floor(x + 0.00001); tx <= (int)Math.Floor(x + BodyWidth - 0.00001); tx++)
        yield return (tx, ty);
    }
  }

  private static bool Overlaps(double ax, double ay, double height, double bx, double by, double otherHeight = BodyHeight) =>
    ax < bx + BodyWidth && ax + BodyWidth > bx && ay < by + otherHeight && ay + height > by;

  private void AdvanceGrowthHearts()
  {
    for (int i = _growthHearts.Count - 1; i >= 0; i--)
    {
      var heart = _growthHearts[i];
      heart.AgeTicks++;
      double x = heart.X, y = heart.Y;
      Move(ref x, ref y, heart.Direction * GrowthHeart.Speed * Step, true, false, out bool wall);
      if (wall)
        heart.Direction *= -1;

      heart.VelocityY = Math.Min(heart.VelocityY + 40 * Step, 28);
      Move(ref x, ref y, heart.VelocityY * Step, false, false, out bool floor);
      if (floor)
        heart.VelocityY = heart.VelocityY > 0 ? -5 : 0;

      heart.X = x;
      heart.Y = y;
      if (heart.Y > Level.Height + 2)
        _growthHearts.RemoveAt(i);
    }
  }

  private void ApplyCheatStart()
  {
    if (!KonamiCheatEnabled)
      return;

    Lives = 30;
    HasFlamethrower = true;
    _growthPending = true;
    TryGrow();
    Message = "Konami code! 30 lives and fire!";
  }

  private void BreakBrick(int x, int y)
  {
    if (Level.Tile(x, y) != 'B')
      return;
    Level.Clear(x, y);
    Score += 50;
    _blockBreakEffects.Add(new BlockBreakEffect(x, y));
    SoundRequested?.Invoke(GameSound.BlockBreak);
  }

  private void ClearFlamethrowerQuote()
  {
    FlamethrowerQuote = string.Empty;
    FlamethrowerQuoteTicksRemaining = 0;
  }

  private bool CollectGrowthHearts()
  {
    for (int i = _growthHearts.Count - 1; i >= 0; i--)
    {
      var heart = _growthHearts[i];
      if (!Overlaps(X, Y, PlayerHeight, heart.X, heart.Y))
        continue;

      if ((heart.IsFlamethrower || heart.IsBoomStick) && (!IsLarge || IsBurning || HasBoomStick))
        continue;

      _growthHearts.RemoveAt(i);
      if (heart.SourceX >= 0 && heart.SourceY >= 0)
        _collectedItems.Add((_levelIndex, heart.SourceX, heart.SourceY));

      Score += 100;
      if (heart.IsBoomStick)
      {
        HasBoomStick = true;
        BoomStickQuoteTicksRemaining = BoomStickQuoteDurationTicks;
        HasFlamethrower = false;
        Flame = null;
        ClearFlamethrowerQuote();
        Message = "Boom-Stick! X or CTRL to blast!";
        SoundRequested?.Invoke(GameSound.PowerUp);
        continue;
      }

      if (heart.IsFlamethrower)
      {
        if (HasFlamethrower)
        {
          HasFlamethrower = false;
          Flame = null;
          BurnTicksRemaining = BurnDurationTicks;
          ClearFlamethrowerQuote();
          Message = "On fire! Lost flamethrower!";
          SoundRequested?.Invoke(GameSound.PowerDown);
          continue;
        }

        HasFlamethrower = true;
        FlamethrowerQuote = _flamethrowerQuotes[Random.Shared.Next(_flamethrowerQuotes.Length)];
        FlamethrowerQuoteTicksRemaining = FlamethrowerQuoteDurationTicks;
        Message = "Flamethrower! Hold X or CTRL";
        SoundRequested?.Invoke(GameSound.PowerUp);
        continue;
      }

      if (IsLarge || _growthPending)
      {
        Explode();
        return true;
      }

      _growthPending = true;
      Message = "Heart collected!";
      SoundRequested?.Invoke(GameSound.PowerUp);
    }

    return false;
  }

  private void Die()
  {
    if (IsDying || Lives == 0)
      return;

    Lives--;
    if (Lives == 0)
      GameOverQuote = _gameOverQuotes[Random.Shared.Next(_gameOverQuotes.Length)];
    HasFlamethrower = false;
    HasBoomStick = false;
    BoomStickQuoteTicksRemaining = 0;
    ShotgunBlast = null;
    Flame = null;
    BurnTicksRemaining = 0;
    ClearFlamethrowerQuote();
    TotalDeaths++;
    AwaitingRestart = false;
    DeathQuote = _deathQuotes[Random.Shared.Next(_deathQuotes.Length)];
    Y += PlayerHeight - BodyHeight;
    IsExploding = false;
    IsLarge = false;
    _growthPending = false;
    DamageTicksRemaining = 0;
    DeathTicksRemaining = DeathDurationTicks;
    VelocityY = 0;
    Grounded = false;
    _jumpBuffer = _coyoteTime = 0;

    Message = Lives == 0 ? "Ouch! Restarting game..." : "Ouch!";
    SoundRequested?.Invoke(GameSound.Died);
  }

  private void Explode()
  {
    double feet = Y + PlayerHeight;
    Die();
    IsExploding = true;
    Y = feet - OverloadedBodyHeight;
    Message = "Power overload!";
  }

  private void FireShotgun()
  {
    double originX = _facingDirection > 0 ? X + BodyWidth : X;
    double originY = Y + PlayerHeight - 0.65;
    double length = 0;
    for (int pixel = 0; pixel < (int)(ShotgunRange * 16); pixel++)
    {
      int tileX = (int)Math.Floor(originX + (_facingDirection * ((pixel + 0.5) / 16)));
      int top = (int)Math.Floor(originY - 0.25);
      int bottom = (int)Math.Floor(originY + 0.25);
      // A brick breaks and lets the rest of this shot continue through it.
      for (int tileY = top; tileY <= bottom; tileY++)
        BreakBrick(tileX, tileY);
      if (Level.Solid(tileX, top) || Level.Solid(tileX, bottom))
      {
        double distance = _facingDirection > 0 ? tileX - originX : originX - (tileX + 1);
        length = Math.Clamp(distance, 0, length);
        break;
      }

      length = (pixel + 1) / 16.0;
    }

    ShotgunBlast = new ShotgunBlast(originX, originY, _facingDirection, length);
    _shotgunReloadTicks = ShotgunReloadTicks;
    double left = _facingDirection > 0 ? originX : originX - length;
    double right = _facingDirection > 0 ? originX + length : originX;
    foreach (var enemy in Enemies.Where(enemy => enemy.Alive))
    {
      if (enemy.X >= right || enemy.X + BodyWidth <= left
        || enemy.Y >= originY + 0.25 || enemy.Y + enemy.BodyHeight <= originY - 0.25)
        continue;

      if (enemy.Hit(ShotgunDamage))
      {
        Score += 200 * enemy.Settings.PowerLevel;
        SoundRequested?.Invoke(GameSound.EnemyDied);
      }

    }

    Message = "BOOM-STICK!";
    SoundRequested?.Invoke(GameSound.Shotgun);
  }

  private void LeavePoop(Enemy dog)
  {
    if (--dog.PoopTicksRemaining > 0)
      return;

    dog.PoopTicksRemaining = Enemy.PoopIntervalTicks;
    int x = (int)Math.Floor(dog.X + (BodyWidth / 2) - (dog.Direction * 0.65));
    int y = (int)Math.Round(dog.Y + dog.BodyHeight);
    if (x >= 0 && x < Level.Width && Level.Solid(x, y) && !Level.Solid(x, y - 1))
    {
      var pile = new PoopPile(x, y);
      if (!_poopPiles.Contains(pile))
        _poopPiles.Add(pile);
    }
  }

  private void LoadLevel(bool preserveGrowth = false)
  {
    bool keepGrowth = preserveGrowth && (IsLarge || _growthPending);
    HasFlamethrower = preserveGrowth && HasFlamethrower;
    HasBoomStick = preserveGrowth && HasBoomStick;
    BoomStickQuoteTicksRemaining = 0;
    ShotgunBlast = null;
    _shotgunReloadTicks = 0;
    _fireWasHeld = false;
    ClearFlamethrowerQuote();
    BurnTicksRemaining = 0;
    Flame = null;
    _flameDamageTicks = _flameAnimationTicks = 0;
    IsExploding = false;
    IsLarge = false;
    _growthPending = keepGrowth;
    _growthHearts.Clear();
    _poopPiles.Clear();
    SlowTicksRemaining = PoopSplatterTicksRemaining = 0;
    _facingDirection = 1;
    DamageTicksRemaining = 0;
    Level = Level.Load(_files[_levelIndex]);
    ThemeLevelNumber = 1;

    for (int index = 0; index < _levelIndex; index++)
    {
      if (Level.Load(_files[index]).Background == Level.Background)
        ThemeLevelNumber++;
    }

    _levelItems.Clear();
    for (int y = 0; y < Level.Height; y++)
    {
      for (int x = 0; x < Level.Width; x++)
      {
        if (Level.Tile(x, y) is 'o' or '?' or 'M' or 'F' or 'Q')
          _levelItems.Add((x, y));
      }
    }

    _rewardPopups.Clear();
    _blockBreakEffects.Clear();

    X = Level.Spawn.X;
    Y = Level.Spawn.Y;
    VelocityY = 0;
    DeathTicksRemaining = 0;
    AwaitingRestart = false;
    DeathQuote = string.Empty;
    GameOverQuote = string.Empty;

    Grounded = false;
    _jumpBuffer = _coyoteTime = 0;

    Enemies = Level.EnemySpawns.Select(spawn => new Enemy(spawn.X, spawn.Y, EnemyKind.Walker, _enemyConfiguration.Walker))
      .Concat(Level.ToasterCatSpawns.Select(spawn => new Enemy(spawn.X, spawn.Y, EnemyKind.ToasterCat, _enemyConfiguration.ToasterCat)))
      .Concat(Level.ZombieSpawns.Select(spawn => new Enemy(spawn.X, spawn.Y - (LargeBodyHeight - BodyHeight), EnemyKind.Zombie, _enemyConfiguration.Zombie)))
      .Concat(Level.DogSpawns.Select(spawn => new Enemy(spawn.X, spawn.Y, EnemyKind.Dog, _enemyConfiguration.Dog)))
      .ToList();
    TryGrow();
  }

  private void Move(ref double x, ref double y, double distance, bool horizontal, bool bump, out bool hit, double height = BodyHeight)
  {
    hit = false;
    // Snapshot contacts before bumping tiles, without allocating per body or substep.
    Span<(int X, int Y)> collisions = stackalloc (int, int)[2 * ((int)Math.Ceiling(height) + 1)];
    int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / 0.15));

    for (int i = 0; i < steps; i++)
    {
      double nx = x + (horizontal ? distance / steps : 0);
      double ny = y + (horizontal ? 0 : distance / steps);
      double previousX = x;
      double bodyBottom = y + height;

      // Spike sides block entry like a pipe. Vertical contact is handled separately at the tip.
      int count = 0;
      for (int ty = (int)Math.Floor(ny + 0.00001); ty <= (int)Math.Floor(ny + height - 0.00001); ty++)
      {
        for (int tx = (int)Math.Floor(nx + 0.00001); tx <= (int)Math.Floor(nx + BodyWidth - 0.00001); tx++)
        {
          if (Level.Solid(tx, ty) || (horizontal && Level.Tile(tx, ty) == '^'
            && bodyBottom > ty + SpikeTipOffset + 0.00001
            && ((distance > 0 && previousX + BodyWidth <= tx + 0.00001)
              || (distance < 0 && previousX >= tx + 1 - 0.00001))))
            collisions[count++] = (tx, ty);
        }
      }

      if (count == 0)
      {
        x = nx;
        y = ny;
        continue;
      }

      hit = true;

      foreach (var cell in collisions[..count])
      {
        if (bump && distance < 0)
        {
          if (Level.Tile(cell.X, cell.Y) is 'M' or 'F' or 'Q')
          {
            bool flamethrower = Level.Tile(cell.X, cell.Y) == 'F';
            bool shotgun = Level.Tile(cell.X, cell.Y) == 'Q';
            Level.UseBlock(cell.X, cell.Y);
            _growthHearts.Add(new GrowthHeart(cell.X + 0.125, cell.Y - BodyHeight, _facingDirection, cell.X, cell.Y, flamethrower, shotgun));
            Message = shotgun ? "Catch the Boom-Stick!" : flamethrower ? "Catch the flamethrower!" : "Catch the heart!";
          }
          else if (Level.Tile(cell.X, cell.Y) == '?')
          {
            Level.UseBlock(cell.X, cell.Y);
            _collectedItems.Add((_levelIndex, cell.X, cell.Y));
            Coins++;
            Score += 100;
            Message = "Bonus coin!";
            SoundRequested?.Invoke(GameSound.Coin);

            _rewardPopups.Add(new CoinRewardPopup(cell.X, cell.Y));
          }
          else if (Level.Tile(cell.X, cell.Y) == 'B')
          {
            BreakBrick(cell.X, cell.Y);
          }
        }
      }

      int boundary = horizontal ? collisions[0].X : collisions[0].Y;
      for (int contact = 1; contact < count; contact++)
      {
        int coordinate = horizontal ? collisions[contact].X : collisions[contact].Y;
        boundary = distance > 0 ? Math.Min(boundary, coordinate) : Math.Max(boundary, coordinate);
      }

      if (horizontal)
        x = distance > 0 ? boundary - BodyWidth : boundary + 1;
      else
        y = distance > 0 ? boundary - height : boundary + 1;

      break;
    }
  }

  private void StepInPoop()
  {
    for (int i = _poopPiles.Count - 1; i >= 0; i--)
    {
      var pile = _poopPiles[i];
      if (!Level.Solid(pile.X, pile.Y))
      {
        _poopPiles.RemoveAt(i);
        continue;
      }

      if (!Grounded || Math.Abs(Y + PlayerHeight - pile.Y) > 0.05
        || X + BodyWidth <= pile.X + 0.15 || X >= pile.X + 0.85)
        continue;

      _poopPiles.RemoveAt(i);
      SlowTicksRemaining = PoopSlowDurationTicks;
      PoopSplatterTicksRemaining = PoopSplatterDurationTicks;
      Message = "Oh crap! Slowed for 5 seconds";
    }
  }

  private void TakeDamage()
  {
    if (DamageTicksRemaining > 0)
      return;

    if (HasBoomStick)
    {
      HasBoomStick = false;
      BoomStickQuoteTicksRemaining = 0;
      HasFlamethrower = false;
      ShotgunBlast = null;
      DamageTicksRemaining = DamageProtectionTicks;
      Message = "Lost Boom-Stick! Keep moving!";
      SoundRequested?.Invoke(GameSound.PowerDown);
      return;
    }

    if (HasFlamethrower)
    {
      HasFlamethrower = false;
      Flame = null;
      DamageTicksRemaining = DamageProtectionTicks;
      Message = "Lost flamethrower!";
      SoundRequested?.Invoke(GameSound.PowerDown);
      ClearFlamethrowerQuote();
      return;
    }

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
    SoundRequested?.Invoke(GameSound.PowerDown);
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

    Message = HasFlamethrower ? "Flamethrower! Hold X or CTRL" : "Powered up!";
  }

  private void UpdateFlame(bool fireHeld)
  {
    if (_flameDamageTicks > 0)
      _flameDamageTicks--;
    if (!HasFlamethrower || !IsLarge || !fireHeld)
    {
      _flameAnimationTicks = 0;
      return;
    }

    if (_flameAnimationTicks >= FlameMaxDurationTicks)
      return;

    double originX = _facingDirection > 0 ? X + BodyWidth : X;
    double originY = Y + PlayerHeight - 0.65;
    double length = 0;

    // Trace the full flame thickness so it cannot pass through a wall or a pipe.
    for (int pixel = 0; pixel < (int)(FlameRange * 16); pixel++)
    {
      double checkX = originX + (_facingDirection * ((pixel + 0.5) / 16));
      int tileX = (int)Math.Floor(checkX);

      if (Level.Solid(tileX, (int)Math.Floor(originY - 0.25))
        || Level.Solid(tileX, (int)Math.Floor(originY + 0.25)))
      {
        double boundaryDistance = _facingDirection > 0 ? tileX - originX : originX - (tileX + 1);
        length = Math.Clamp(boundaryDistance, 0, length);
        break;
      }

      length = (pixel + 1) / 16.0;
    }

    Flame = new FlameStream(originX, originY, _facingDirection, length, _flameAnimationTicks / 6 % 4);
    _flameAnimationTicks++;

    if (_flameDamageTicks > 0 || length == 0)
      return;

    _flameDamageTicks = FlameDamageIntervalTicks;

    double left = _facingDirection > 0 ? originX : originX - length;
    double right = _facingDirection > 0 ? originX + length : originX;

    foreach (var enemy in Enemies.Where(enemy => enemy.Alive))
    {
      if (enemy.X >= right || enemy.X + BodyWidth <= left
        || enemy.Y >= originY + 0.25 || enemy.Y + enemy.BodyHeight <= originY - 0.25)
        continue;

      if (enemy.Hit(FlameDamage))
      {
        Score += 200 * enemy.Settings.PowerLevel;
        SoundRequested?.Invoke(GameSound.EnemyDied);
      }
    }
  }
}
