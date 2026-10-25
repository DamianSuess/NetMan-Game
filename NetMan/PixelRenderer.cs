namespace NetMan;

internal sealed class PixelRenderer
{
  public const int Height = 240;
  public const int TileSize = 16;
  public const int Width = 384;
  private const int FooterHeight = 14;
  private const uint Gold = 0xFFFFD56B;
  private const int HeaderHeight = 28;
  private const uint Ink = 0xFF17263F;
  private const uint White = 0xFFF6F4DF;
  private readonly Dictionary<string, Sprite> _sprites = [];
  private double _animationTime;
  private bool _facingLeft;

  public PixelRenderer(string assetsPath)
  {
    var palette = Sprite.LoadPalette(Path.Combine(assetsPath, "palette.txt"));

    foreach (string name in new[]
    {
      "player-idle",
      "player-run-1",
      "player-run-2",
      "player-jump",
      "player-splatter",
      "player-explosion-1",
      "player-explosion-2",
      "player-explosion-3",
      "player-explosion-4",
      "player-large-idle",
      "player-large-run-1",
      "player-large-run-2",
      "player-large-jump",
      "enemy-idle",
      "enemy-walk",
      "enemy-squashed",
      "grass",
      "dirt",
      "brick",
      "brick-rubble",
      "brick-dust",
      "bonus",
      "used",
      "pipe",
      "pipe-small-top",
      "pipe-small-body",
      "pipe-small-bottom",
      "pipe-top-left",
      "pipe-top-right",
      "pipe-body-left",
      "pipe-body-right",
      "pipe-bottom-left",
      "pipe-bottom-right",
      "coin",
      "spike",
      "goal",
      "reward-plus-one",
    })
      _sprites.Add(name, Sprite.Load(Path.Combine(assetsPath, name + ".sprite"), palette));
  }

  public PixelBuffer Frame { get; } = new(Width, Height);

  public void Draw(Game game, int direction, double elapsed = Game.Step)
  {
    bool moving = !game.Paused && !game.Won && !game.IsDying && game.Lives > 0;

    if (moving)
    {
      _animationTime += elapsed;

      if (direction != 0)
        _facingLeft = direction < 0;
    }

    int viewHeight = Height - HeaderHeight - FooterHeight;
    int cameraX = Math.Clamp((int)(game.X * TileSize) - (Width / 3), 0, Math.Max(0, (game.Level.Width * TileSize) - Width));
    int cameraY = Math.Clamp((int)(game.Y * TileSize) - ((viewHeight * 2) / 3), 0, Math.Max(0, (game.Level.Height * TileSize) - viewHeight));

    DrawBackground(cameraX);

    for (int y = cameraY / TileSize; y <= (cameraY + viewHeight) / TileSize; y++)
    {
      for (int x = cameraX / TileSize; x <= (cameraX + Width) / TileSize; x++)
      {
        string? name = game.Level.Tile(x, y) switch
        {
          '#' => game.Level.Tile(x, y - 1) == '#' ? "dirt" : "grass",
          'B' => "brick",
          '?' or 'M' => "bonus",
          'X' => "used",
          'P' => PipeSprite(game.Level, x, y),
          'p' => SkinnyPipeSprite(game.Level, x, y),
          'o' => "coin",
          '^' => "spike",
          'G' => "goal",
          _ => null,
        };

        if (name is not null)
          Frame.Draw(_sprites[name], (x * TileSize) - cameraX, HeaderHeight + (y * TileSize) - cameraY);
      }
    }

    foreach (var effect in game.BlockBreakEffects)
    {
      var sprite = _sprites[effect.FrameIndex == 0 ? "brick-rubble" : "brick-dust"];
      Frame.Draw(sprite, (effect.X * TileSize) - cameraX, HeaderHeight + (effect.Y * TileSize) - cameraY);
    }

    int animationFrame = (int)(_animationTime * 9) % 2;

    foreach (var enemy in game.Enemies.Where(enemy => enemy.Alive))
      DrawActor(_sprites[animationFrame == 0 ? "enemy-idle" : "enemy-walk"], enemy.X, enemy.Y, cameraX, cameraY, enemy.Direction > 0);

    foreach (var enemy in game.Enemies.Where(enemy => enemy.SquashTicksRemaining > 0))
      DrawActor(_sprites["enemy-squashed"], enemy.X, enemy.Y, cameraX, cameraY, enemy.Direction > 0);

    string playerFrame = !game.Grounded ? "player-jump"
      : moving && direction != 0 ? $"player-run-{animationFrame + 1}" : "player-idle";

    if (game.IsLarge)
      playerFrame = playerFrame.Replace("player-", "player-large-", StringComparison.Ordinal);

    if (game.IsExploding)
      DrawActor(_sprites[$"player-explosion-{game.ExplosionFrameIndex + 1}"], game.X, game.Y,
        cameraX, cameraY, _facingLeft, true, game.PlayerHeight);
    else if (game.IsDying)
      DrawActor(_sprites["player-splatter"], game.X, game.Y, cameraX, cameraY, _facingLeft, true);
    else if ((game.DamageTicksRemaining / 5) % 2 == 0)
      DrawActor(_sprites[playerFrame], game.X, game.Y, cameraX, cameraY, _facingLeft, bodyHeight: game.PlayerHeight);

    foreach (var popup in game.RewardPopups)
    {
      var rewardSprite = _sprites["reward-plus-one"];
      int x = (popup.X * TileSize) - cameraX + ((TileSize - rewardSprite.Width) / 2);
      int y = HeaderHeight
        + (popup.Y * TileSize)
        - cameraY
        - rewardSprite.Height - 2
        - ((popup.AgeTicks * 8) / CoinRewardPopup.DurationTicks);

      Frame.Draw(rewardSprite, x, y);
    }

    DrawHud(game);

    if (game.Paused || game.Won || (game.Lives == 0 && !game.IsDying))
    {
      string title = game.Won ? "LEVELS COMPLETE!" : game.Lives == 0 ? "GAME OVER" : "PAUSED";
      string hint = game.Won || game.Lives == 0 ? "R TO PLAY AGAIN" : "P TO RESUME";

      Frame.Fill(70, 90, 244, 60, Ink);
      Frame.Fill(72, 92, 240, 1, Gold);
      PixelFont.Draw(Frame, title, (Width - (title.Length * 8)) / 2, 105, Gold, 2);
      PixelFont.Draw(Frame, hint, (Width - (hint.Length * 4)) / 2, 130, White);
    }
  }

  private static string SkinnyPipeSprite(Level level, int x, int y)
  {
    bool above = level.Tile(x, y - 1) == 'p';
    bool below = level.Tile(x, y + 1) == 'p';

    return (above, below) switch
    {
      (false, false) => "pipe",
      (false, true) => "pipe-small-top",
      (true, false) => "pipe-small-bottom",
      _ => "pipe-small-body",
    };
  }

  private static string PipeSprite(Level level, int x, int y)
  {
    int left = x;
    int right = x;

    while (level.Tile(left - 1, y) == 'P')
      left--;

    while (level.Tile(right + 1, y) == 'P')
      right++;

    int width = right - left + 1;

    if (width > 2)
      return "pipe";

    int top = y;
    int bottom = y;

    while (IsPipeRow(level, left, width, top - 1))
      top--;

    while (IsPipeRow(level, left, width, bottom + 1))
      bottom++;

    // Irregular groups retain the original artwork rather than mismatched pipe pieces.
    for (int offset = 0; offset < width; offset++)
    {
      if (level.Tile(left + offset, top - 1) == 'P' || level.Tile(left + offset, bottom + 1) == 'P')
        return "pipe";
    }

    if (top == bottom)
      return "pipe";

    string section = y == top ? "top" : y == bottom ? "bottom" : "body";

    return width == 1
      ? $"pipe-small-{section}"
      : $"pipe-{section}-{(x == left ? "left" : "right")}";
  }

  private static bool IsPipeRow(Level level, int left, int width, int y)
  {
    if (level.Tile(left - 1, y) == 'P' || level.Tile(left + width, y) == 'P')
      return false;

    for (int offset = 0; offset < width; offset++)
    {
      if (level.Tile(left + offset, y) != 'P')
        return false;
    }

    return true;
  }

  private void DrawActor(Sprite sprite, double x, double y, int cameraX, int cameraY, bool flip, bool keepInView = false, double bodyHeight = Game.BodyHeight)
  {
    int px = (int)Math.Round((x + (Game.BodyWidth / 2)) * TileSize) - (sprite.Width / 2) - cameraX;
    int py = HeaderHeight + (int)Math.Round((y + bodyHeight) * TileSize) - sprite.Height - cameraY;

    if (keepInView)
    {
      px = Math.Clamp(px, 0, Width - sprite.Width);
      py = Math.Clamp(py, HeaderHeight, Height - FooterHeight - sprite.Height);
    }

    Frame.Draw(sprite, px, py, flip);
  }

  private void DrawBackground(int cameraX)
  {
    for (int y = 0; y < Height; y++)
    {
      uint color = y < 70 ? 0xFF608CC8 : y < 125 ? 0xFF79ADD7 : 0xFF9BCEE1;
      Frame.Fill(0, y, Width, 1, color);
    }

    Frame.Fill(301, 45, 20, 20, 0xFFFFDF8A);
    Frame.Fill(297, 49, 28, 12, 0xFFFFDF8A);

    for (int i = -1; i < 5; i++)
    {
      int x = (i * 112) - ((cameraX / 5) % 112);
      int y = 55 + ((i & 1) * 17);
      Frame.Fill(x + 8, y, 25, 6, 0xFFDEEFF4);
      Frame.Fill(x, y + 6, 47, 9, 0xFFDEEFF4);
      Frame.Fill(x + 5, y + 15, 35, 3, 0xFFB6DCEB);
    }

    for (int x = 0; x < Width; x++)
    {
      int far = Math.Abs(((x + (cameraX / 6)) % 180) - 90);
      int near = Math.Abs(((x + (cameraX / 3) + 60) % 150) - 75);
      int farTop = 110 + (far / 2);
      int nearTop = 150 + (near / 2);
      Frame.Fill(x, farTop, 1, Height - farTop, 0xFF679DA5);
      Frame.Fill(x, nearTop, 1, Height - nearTop, 0xFF4A887D);
    }
  }

  private void DrawHud(Game game)
  {
    Frame.Fill(0, 0, Width, HeaderHeight, Ink);
    Frame.Fill(0, HeaderHeight - 2, Width, 2, 0xFF355575);
    PixelFont.Draw(Frame, "NETMAN", 8, 5, Gold, 2);
    PixelFont.Draw(Frame, $"SCORE {game.Score:000000}   COINS {game.Coins:00}   LIVES {game.Lives}   {game.LevelNumber}/{game.LevelCount}", 76, 8, White);
    PixelFont.Draw(Frame, game.Message, 8, 19, 0xFFA7CBDC);

    Frame.Fill(0, Height - FooterHeight, Width, FooterHeight, Ink);
    PixelFont.Draw(Frame, "A/D MOVE  SPACE JUMP  P PAUSE  R RESTART  ESC QUIT", 8, Height - 9, White);
  }
}
