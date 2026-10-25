namespace NetBro;

public sealed class Enemy(double x, double y)
{
  public double X = x, Y = y, VelocityY;
  public int Direction = -1;
  public bool Alive = true;
}

public sealed class Game
{
  public const double Step = 1.0 / 60;
  public const double BodyWidth = 0.75, BodyHeight = 0.9;

  private readonly string[] _files;

  private int _levelIndex;
  private double _jumpBuffer, _coyoteTime, _transition;

  public Level Level { get; private set; } = null!;
  public List<Enemy> Enemies { get; private set; } = [];
  public double X { get; private set; }
  public double Y { get; private set; }
  public double VelocityY { get; private set; }
  public bool Grounded { get; private set; }
  public int Lives { get; private set; } = 3;
  public int Coins { get; private set; }
  public int Score { get; private set; }
  public bool Won { get; private set; }
  public bool Paused { get; set; }
  public string Message { get; private set; } = "Let's go!";
  public int LevelNumber => _levelIndex + 1;
  public int LevelCount => _files.Length;

  public Game(string[] files)
  {
    this._files = files;
    LoadLevel();
  }

  private void LoadLevel()
  {
    Level = Level.Load(_files[_levelIndex]);

    X = Level.Spawn.X;
    Y = Level.Spawn.Y;
    VelocityY = 0;

    Grounded = false;
    _jumpBuffer = _coyoteTime = 0;

    Enemies = Level.EnemySpawns.Select(spawn => new Enemy(spawn.X, spawn.Y)).ToList();
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

  public void QueueJump()
  {
    if (!Paused && !Won && Lives > 0)
      _jumpBuffer = 0.12;
  }

  public void Update(int direction, bool jumpHeld)
  {
    if (Paused || Won || Lives == 0)
      return;

    if (_transition > 0)
    {
      _transition -= Step;
      return;
    }

    _jumpBuffer -= Step;
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
    Move(ref x, ref y, Math.Clamp(direction, -1, 1) * 12 * Step, true, false, out _);

    var vertical = VelocityY * Step;
    Move(ref x, ref y, vertical, false, true, out bool hitY);

    Grounded = hitY && vertical > 0;

    if (hitY)
      VelocityY = 0;

    X = x;
    Y = y;

    foreach (var (tx, ty) in Cells(X, Y))
    {
      switch (Level.Tile(tx, ty))
      {
        case 'o':
          Level.Clear(tx, ty);
          Coins++;
          Score += 100;
          break;

        case '^':
          Die();
          return;

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
            LoadLevel();
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
      if (wall) enemy.Direction *= -1;
      enemy.VelocityY = Math.Min(enemy.VelocityY + 40 * Step, 28);
      Move(ref ex, ref ey, enemy.VelocityY * Step, false, false, out bool floor);
      if (floor) enemy.VelocityY = 0;
      enemy.X = ex; enemy.Y = ey;
      if (ey > Level.Height + 2) enemy.Alive = false;
      if (!enemy.Alive || !Overlaps(X, Y, ex, ey)) continue;
      if (vertical > 0 && Y + BodyHeight - vertical <= ey + 0.25)
      {
        enemy.Alive = false; VelocityY = -10; Score += 200; Message = "Stomp!";
      }
      else { Die(); return; }
    }
  }

  private void Die()
  {
    Lives--;
    Message = Lives == 0 ? "GAME OVER - R to restart" : "Try again!";
    if (Lives > 0) { LoadLevel(); _transition = 0.7; }
  }

  private void Move(ref double x, ref double y, double distance, bool horizontal, bool bump, out bool hit)
  {
    hit = false;
    int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / 0.15));
    for (int i = 0; i < steps; i++)
    {
      double nx = x + (horizontal ? distance / steps : 0);
      double ny = y + (horizontal ? 0 : distance / steps);
      var collisions = Cells(nx, ny).Where(cell => Level.Solid(cell.X, cell.Y)).ToArray();
      if (collisions.Length == 0) { x = nx; y = ny; continue; }
      hit = true;
      foreach (var cell in collisions)
      {
        if (bump && distance < 0)
        {
          if (Level.Tile(cell.X, cell.Y) == '?')
          { Level.UseBlock(cell.X, cell.Y); Coins++; Score += 100; Message = "Bonus coin!"; }
          else if (Level.Tile(cell.X, cell.Y) == 'B')
          { Level.Clear(cell.X, cell.Y); Score += 50; }
        }
      }
      if (horizontal)
        x = distance > 0 ? collisions.Min(c => c.X) - BodyWidth : collisions.Max(c => c.X) + 1;
      else
        y = distance > 0 ? collisions.Min(c => c.Y) - BodyHeight : collisions.Max(c => c.Y) + 1;
      break;
    }
  }

  private static IEnumerable<(int X, int Y)> Cells(double x, double y)
  {
    for (int ty = (int)Math.Floor(y + 0.00001);
      ty <= (int)Math.Floor(y + BodyHeight - 0.00001); ty++)
    {

      for (int tx = (int)Math.Floor(x + 0.00001); tx <= (int)Math.Floor(x + BodyWidth - 0.00001); tx++)
        yield return (tx, ty);
    }
  }

  private static bool Overlaps(double ax, double ay, double bx, double by) =>
    ax < bx + BodyWidth && ax + BodyWidth > bx && ay < by + BodyHeight && ay + BodyHeight > by;
}
