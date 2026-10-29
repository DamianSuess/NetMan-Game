using NetMan.Actors;
using NetMan.Items;
using NetMan.Screens;

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
  private static readonly string VersionLabel = "v" + System.Reflection.CustomAttributeExtensions
    .GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>(typeof(PixelRenderer).Assembly)!.Version;
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
      "toaster-cat-run-1",
      "toaster-cat-run-2",
      "toaster-cat-jump",
      "toaster-cat-squashed",
      "zombie-walk-1",
      "zombie-walk-2",
      "zombie-squashed",
      "creepy-crawler-buried",
      "creepy-crawler-emerge-1",
      "creepy-crawler-emerge-2",
      "creepy-crawler-emerge-3",
      "creepy-crawler-crawl-1",
      "creepy-crawler-crawl-2",
      "creepy-crawler-squashed",
      "dog-walk-1",
      "dog-walk-2",
      "dog-squashed",
      "poop",
      "poop-screen-splatter",
      "grass",
      "dirt",
      "brick",
      "brick-rubble",
      "brick-dust",
      "bonus",
      "heart-1",
      "heart-2",
      "flamethrower",
      "flamethrower-pickup",
      "boom-stick",
      "boom-stick-pickup",
      "shotgun-blast-1",
      "shotgun-blast-2",
      "flame-1",
      "flame-2",
      "flame-3",
      "flame-4",
      "player-burning-1",
      "player-burning-2",
      "player-burning-3",
      "player-burning-4",
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
      "moon",
      "tombstone",
      "tombstone-cross",
    })
      _sprites.Add(name, Sprite.Load(Path.Combine(assetsPath, name + ".sprite"), palette));

    foreach (string name in _sprites.Keys.Where(name => name.StartsWith("player-", StringComparison.Ordinal)
      && !name.StartsWith("player-burning-", StringComparison.Ordinal)).ToArray())
    {
      string ellaName = PlayerSprite(PlayerCharacter.Ella, name);
      _sprites.Add(ellaName, Sprite.Load(Path.Combine(assetsPath, ellaName + ".sprite"), palette));
    }
  }

  public PixelBuffer Frame { get; } = new(Width, Height);

  public void DrawTitle(TitleScreen title, string updateStatus = "")
  {
    DrawBackground((int)(title.Elapsed * 10) % (Width * 4));
    Frame.OverlayBlack(100);
    DrawTitleText(updateStatus, 12, White, 1);
    DrawTitleText("Sam n' Ella", 34, Gold, 5, shadow: true);
    if (title.KonamiCodeEnabled)
      DrawTitleText("KONAMI CODE ACTIVATED - 30 LIVES", 72, Gold, 1);
    DrawTitleText("PLAYER SELECT", 86, White, 2);

    int animationFrame = (int)(title.Elapsed * 6) % 2 + 1;
    foreach (PlayerCharacter character in Enum.GetValues<PlayerCharacter>())
    {
      int left = character == PlayerCharacter.Sam ? 76 : 202;
      bool selected = title.SelectedCharacter == character;
      Frame.Fill(left, 97, 106, 69, selected ? Gold : 0xFF527787);
      Frame.Fill(left + 2, 99, 102, 65, Ink);
      Frame.Draw(_sprites[PlayerSprite(character, $"player-large-run-{animationFrame}")], left + 45, 108);
      string name = character.ToString().ToUpperInvariant();
      PixelFont.Draw(Frame, name, left + (106 - ((name.Length * 4) - 1)) / 2, 149, selected ? Gold : White);
      if (selected)
        PixelFont.Draw(Frame, ">", left + 10, 126, Gold);
    }
    DrawTitleText("LEFT/RIGHT PLAYER  UP/DOWN MENU  ENTER SELECT", 173, White, 1);
    DrawMenuChoice("START GAME", 186, title.SelectedMenuIndex == 0);
    DrawMenuChoice("SETTINGS", 200, title.SelectedMenuIndex == 1);

    long ticks = (long)(title.Elapsed * 60);
    DrawCreditsRunner($"player-run-{animationFrame}", ticks, 0, 38, false, 224);
    DrawCreditsRunner($"ella-run-{animationFrame}", ticks, 215, 38, false, 224);
    DrawCreditsRunner($"toaster-cat-run-{animationFrame}", ticks, 140, -45, false, 224);
    DrawCreditsRunner((int)(title.Elapsed * 4) % 2 == 0 ? "enemy-idle" : "enemy-walk", ticks, 290, -24, false, 224);
    DrawCreditsRunner($"zombie-walk-{(int)(title.Elapsed * 3) % 2 + 1}", ticks, 380, -12, false, 224);
    DrawCreditsRunner($"dog-walk-{animationFrame}", ticks, 110, -28, false, 224);
    Frame.Fill(0, 224, Width, 2, Gold);
    Frame.Fill(0, 226, Width, 14, Ink);

    PixelFont.Draw(Frame, VersionLabel, 6, Height - 9, White, preserveCase: true);

    const string Copyright = "© 2026 XENO INNOVATIONS, INC.";
    PixelFont.Draw(Frame, Copyright, Width - (Copyright.Length * 4) - 5, Height - 9, White);
  }

  private void DrawTitleText(string text, int y, uint color, int scale, bool shadow = false)
  {
    int x = (Width - (((text.Length * 4) - 1) * scale)) / 2;
    if (shadow)
      PixelFont.Draw(Frame, text, x + 2, y + 3, Ink, scale);

    PixelFont.Draw(Frame, text, x, y, color, scale);
  }

  private void DrawMenuChoice(string text, int y, bool selected)
  {
    DrawTitleText(selected ? $"> {text} <" : text, y, selected ? Gold : White, 1);
  }

  public void DrawSettings(SettingsScreen screen)
  {
    const uint Disabled = 0xFF838B98;
    Frame.OverlayBlack(80);
    Frame.FillTranslucent(24, 24, Width - 48, 214, Ink, 225);
    Frame.FillTranslucent(26, 28, Width - 52, 32, Ink, 210);
    Frame.Fill(24, 24, Width - 48, 1, Gold);
    Frame.Fill(24, 237, Width - 48, 1, 0xFF527787);
    DrawTitleText("SETTINGS", 36, Gold, 3);

    DrawAudioToggle("BACKGROUND MUSIC", 68, screen.Audio.MusicEnabled, screen.SelectedIndex == 0);
    DrawVolumePlaceholder("MUSIC VOLUME", 99, Disabled);
    DrawAudioToggle("SOUND FX", 129, screen.Audio.SoundEffectsEnabled, screen.SelectedIndex == 1);
    DrawVolumePlaceholder("SOUND FX VOLUME", 160, Disabled);

    Frame.FillTranslucent(144, 184, 96, 24, Ink, 180);
    if (screen.SelectedIndex == 2)
      Frame.FillTranslucent(144, 187, 96, 17, Gold, 45);

    DrawTitleText(screen.SelectedIndex == 2 ? "> BACK <" : "BACK", 192,
      screen.SelectedIndex == 2 ? Gold : White, 1);

    DrawTitleText("UP/DOWN SELECT  LEFT/RIGHT OFF/ON", 220, White, 1);
    DrawTitleText("ENTER/SPACE TOGGLE  ESC BACK", 230, White, 1);
  }

  private void DrawAudioToggle(string label, int y, bool enabled, bool selected)
  {
    if (selected)
      Frame.FillTranslucent(36, y - 5, Width - 72, 24, Gold, 40);

    PixelFont.Draw(Frame, selected ? ">" : " ", 40, y + 3, Gold);
    PixelFont.Draw(Frame, label, 52, y + 3, selected ? Gold : White);
    const int SwitchX = 272;
    Frame.Fill(SwitchX, y - 1, 64, 16, enabled ? 0xFF3A806C : 0xFF535D70);
    Frame.Fill(SwitchX + (enabled ? 47 : 2), y + 1, 15, 12, enabled ? White : 0xFFAFB4BF);
    PixelFont.Draw(Frame, enabled ? "ON" : "OFF", SwitchX + (enabled ? 10 : 27), y + 4, White);
  }

  private void DrawVolumePlaceholder(string label, int y, uint color)
  {
    PixelFont.Draw(Frame, label, 52, y, color);
    Frame.Fill(244, y + 2, 92, 3, 0xFF525D70);
    Frame.Fill(308, y - 1, 7, 9, color);
    PixelFont.Draw(Frame, "COMING SOON", 52, y + 10, color);
  }

  private static string PlayerSprite(PlayerCharacter character, string name) =>
    character == PlayerCharacter.Ella ? name.Replace("player-", "ella-", StringComparison.Ordinal) : name;

  public void DrawLevelSelect(LevelSelectScreen screen)
  {
    Frame.Clear(Ink);
    DrawTitleText("LEVEL SELECT", 12, Gold, 3);
    PixelFont.Draw(Frame, "NO.", 24, 44, Gold);
    PixelFont.Draw(Frame, "NAME", 48, 44, Gold);
    PixelFont.Draw(Frame, "THEME", 300, 44, Gold);
    Frame.Fill(10, 54, Width - 20, 1, Gold);
    int first = screen.Page * LevelSelectScreen.PageSize;

    for (int row = 0; row < LevelSelectScreen.PageSize && first + row < screen.Levels.Count; row++)
    {
      int index = first + row;
      var level = screen.Levels[index];
      int y = 64 + row * 16;
      bool selected = index == screen.SelectedIndex;

      if (selected)
      {
        Frame.Fill(10, y - 4, Width - 20, 13, 0xFF355575);
        PixelFont.Draw(Frame, ">", 13, y, Gold);
      }

      uint color = selected ? Gold : White;
      string name = level.Name.Length > 56 ? level.Name[..53] + "..." : level.Name;
      PixelFont.Draw(Frame, level.Number.ToString(), 24, y, color);
      PixelFont.Draw(Frame, name, 48, y, color);
      PixelFont.Draw(Frame, level.Theme == BackgroundTheme.Night ? "NIGHTTIME" : "DAYTIME", 300, y, color);
    }

    DrawTitleText($"PAGE {screen.Page + 1}/{screen.PageCount}  LEVELS {screen.Levels.Count}", 196, Gold, 1);
    DrawTitleText("UP/DOWN SELECT  LEFT/RIGHT OR PGUP/PGDN PAGE", 213, White, 1);
    DrawTitleText("ENTER PLAY  ESC BACK", 227, White, 1);
  }

  public void DrawLevelStart(LevelStartScreen screen)
  {
    Frame.Clear(Ink);
    Frame.Fill(12, 18, Width - 24, 1, Gold);
    DrawTitleText($"LEVEL {screen.LevelNumber}", 32, Gold, 4);
    DrawWrappedCentered(screen.LevelName, 66, White, maxLines: 2);
    string pose = screen.IsLarge ? "player-large-idle" : "player-idle";
    var player = _sprites[PlayerSprite(screen.Character, pose)];
    Frame.Draw(player, 148, 142 - player.Height);
    PixelFont.Draw(Frame, $"LIVES {screen.Lives}", 180, 125, White, 2);
    DrawTitleText("UNCLE QUOTE", 155, Gold, 1);
    DrawWrappedCentered(screen.Quote, 170, White);
    Frame.Fill(12, 218, Width - 24, 1, 0xFF355575);
    DrawTitleText("PRESS ANY KEY TO BEGIN", 227, Gold, 1);
  }

  private void DrawGameOver(Game game)
  {
    Frame.Clear(Ink);
    Frame.Fill(12, 22, Width - 24, 1, 0xFFFF6B6B);
    DrawTitleText("GAME OVER", 38, 0xFFFF6B6B, 4);
    DrawTitleText("OUT OF LIVES. OUT OF EXCUSES.", 70, Gold, 1);
    DrawWrappedCentered(game.GameOverQuote, 96, White, maxLines: 4);
    DrawTitleText($"LEVEL {game.LevelNumber}  SCORE {game.Score:000000}  DEATHS {game.TotalDeaths}", 177, White, 1);
    Frame.Fill(12, 198, Width - 24, 1, 0xFF355575);
    DrawTitleText("PRESS ANY KEY TO TRY AGAIN", 213, Gold, 1);
  }

  private void DrawWrappedCentered(string text, int y, uint color, int maxLines = 3)
  {
    const int MaxCharacters = 42;
    string remaining = text.Trim();
    for (int index = 0; index < maxLines && remaining.Length > 0; index++)
    {
      int length = Math.Min(MaxCharacters, remaining.Length);
      if (length < remaining.Length)
      {
        int space = remaining.LastIndexOf(' ', length - 1, length);
        if (space > 0)
          length = space;
      }
      string line = remaining[..length].Trim();
      remaining = remaining[length..].TrimStart();
      if (index == maxLines - 1 && remaining.Length > 0)
        line = line[..Math.Min(line.Length, MaxCharacters - 3)] + "...";
      DrawTitleText(line, y + (index * 12), color, 2);
    }
  }

  public void Draw(Game game, int direction, double elapsed = Game.Step, int pauseSelection = 0)
  {
    if (game.Lives == 0 && game.AwaitingRestart)
    {
      DrawGameOver(game);
      return;
    }

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

    bool night = game.Level.Background == BackgroundTheme.Night;
    DrawBackground(cameraX, night);

    if (night)
      DrawGraveyard(cameraX, cameraY, game.Level.Height);

    for (int y = cameraY / TileSize; y <= (cameraY + viewHeight) / TileSize; y++)
    {
      for (int x = cameraX / TileSize; x <= (cameraX + Width) / TileSize; x++)
      {
        string? name = game.Level.Tile(x, y) switch
        {
          '#' => game.Level.Tile(x, y - 1) == '#' ? "dirt" : "grass",
          'B' => "brick",
          '?' or 'M' or 'F' or 'Q' => "bonus",
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

    foreach (var pile in game.PoopPiles)
      Frame.Draw(_sprites["poop"], (pile.X * TileSize) - cameraX, HeaderHeight + (pile.Y * TileSize) - TileSize - cameraY);

    foreach (var effect in game.BlockBreakEffects)
    {
      var sprite = _sprites[effect.FrameIndex == 0 ? "brick-rubble" : "brick-dust"];
      Frame.Draw(sprite, (effect.X * TileSize) - cameraX, HeaderHeight + (effect.Y * TileSize) - cameraY);
    }

    foreach (var heart in game.GrowthHearts)
      DrawActor(_sprites[heart.IsBoomStick ? "boom-stick-pickup" : heart.IsFlamethrower ? "flamethrower-pickup" : $"heart-{heart.FrameIndex + 1}"], heart.X, heart.Y, cameraX, cameraY, heart.Direction < 0);

    int animationFrame = (int)(_animationTime * 9) % 2;

    foreach (var enemy in game.Enemies.Where(enemy => enemy.Alive))
    {
      if (enemy.IsBuried)
        continue;

      if (enemy.IsEmerging)
      {
        var sprite = _sprites[$"creepy-crawler-emerge-{enemy.EmergenceFrameIndex + 1}"];
        int groundY = HeaderHeight + (int)Math.Round((enemy.Y + enemy.BodyHeight) * TileSize) - cameraY;
        int left = (int)Math.Round((enemy.X + (Game.BodyWidth / 2)) * TileSize) - (sprite.Width / 2) - cameraX;
        int undergroundOffset = (enemy.EmergenceTicksRemaining * (TileSize / 2)) / Enemy.CrawlerEmergenceDurationTicks;
        Frame.Draw(sprite, left, groundY - sprite.Height + undergroundOffset,
          enemy.Direction > 0, clipBottom: groundY);
        continue;
      }

      string name = enemy.Kind switch
      {
        EnemyKind.CreepyCrawler => $"creepy-crawler-crawl-{(int)(_animationTime * 4) % 2 + 1}",
        EnemyKind.Zombie => $"zombie-walk-{(int)(_animationTime * 3) % 2 + 1}",
        EnemyKind.Dog => $"dog-walk-{(int)(_animationTime * 6) % 2 + 1}",
        EnemyKind.ToasterCat => Math.Abs(enemy.VelocityY) > 0.01 ? "toaster-cat-jump" : $"toaster-cat-run-{animationFrame + 1}",
        _ => animationFrame == 0 ? "enemy-idle" : "enemy-walk",
      };

      DrawActor(_sprites[name], enemy.X, enemy.Y, cameraX, cameraY, enemy.Direction > 0, bodyHeight: enemy.BodyHeight);
    }

    foreach (var enemy in game.Enemies.Where(enemy => enemy.SquashTicksRemaining > 0))
    {
      string name = enemy.Kind switch
      {
        EnemyKind.CreepyCrawler => "creepy-crawler-squashed",
        EnemyKind.Zombie => "zombie-squashed",
        EnemyKind.Dog => "dog-squashed",
        EnemyKind.ToasterCat => "toaster-cat-squashed",
        _ => "enemy-squashed",
      };

      DrawActor(_sprites[name], enemy.X, enemy.Y, cameraX, cameraY, enemy.Direction > 0, bodyHeight: enemy.BodyHeight);
    }

    string playerFrame = !game.Grounded ? "player-jump"
      : moving && direction != 0 ? $"player-run-{animationFrame + 1}" : "player-idle";

    if (game.IsLarge)
      playerFrame = playerFrame.Replace("player-", "player-large-", StringComparison.Ordinal);

    if (game.IsExploding)
    {
      if (game.IsExplosionVisible)
        DrawActor(_sprites[PlayerSprite(game.Character, $"player-explosion-{game.ExplosionFrameIndex + 1}")], game.X, game.Y,
          cameraX, cameraY, _facingLeft, true, game.PlayerHeight);
    }
    else if (game.IsDying)
      DrawActor(_sprites[PlayerSprite(game.Character, "player-splatter")], game.X, game.Y, cameraX, cameraY, _facingLeft, true);
    else if ((game.DamageTicksRemaining / 5) % 2 == 0)
      DrawActor(_sprites[PlayerSprite(game.Character, playerFrame)], game.X, game.Y, cameraX, cameraY, _facingLeft, bodyHeight: game.PlayerHeight);

    if ((game.HasFlamethrower || game.HasBoomStick) && !game.IsDying && (game.DamageTicksRemaining / 5) % 2 == 0)
    {
      int weaponX = (int)Math.Round((game.X + (Game.BodyWidth / 2)) * TileSize) - 8 - cameraX;
      int weaponY = HeaderHeight + (int)Math.Round((game.Y + game.PlayerHeight - 0.65) * TileSize) - 8 - cameraY;
      Frame.Draw(_sprites[game.HasBoomStick ? "boom-stick" : "flamethrower"], weaponX, weaponY, game.FacingDirection < 0);
    }

    if (game.IsBurning && !game.IsDying)
    {
      var burning = _sprites[$"player-burning-{game.BurnFrameIndex + 1}"];
      int burnX = (int)Math.Round((game.X + (Game.BodyWidth / 2)) * TileSize) - (burning.Width / 2) - cameraX;
      int burnY = HeaderHeight + (int)Math.Round((game.Y + game.PlayerHeight) * TileSize) - burning.Height - cameraY;
      Frame.DrawTranslucent(burning, burnX, burnY, 200);
    }

    if (game.Flame is { } flame)
    {
      var sprite = _sprites[$"flame-{flame.FrameIndex + 1}"];
      int originX = (int)Math.Round(flame.X * TileSize) - cameraX;
      int originY = HeaderHeight + (int)Math.Round(flame.Y * TileSize) - 8 - cameraY;
      for (int distance = 0; distance < (int)(flame.Length * TileSize); distance++)
      {
        for (int y = 0; y < sprite.Height; y++)
        {
          uint color = sprite.Pixel(distance % TileSize, y);
          if (color != 0)
            Frame.Fill(originX + (flame.Direction > 0 ? distance : -distance - 1), originY + y, 1, 1, color);
        }
      }
    }

    if (game.ShotgunBlast is { } blast)
    {
      var sprite = _sprites[$"shotgun-blast-{blast.FrameIndex + 1}"];
      int originX = (int)Math.Round(blast.X * TileSize) - cameraX;
      int originY = HeaderHeight + (int)Math.Round(blast.Y * TileSize) - 8 - cameraY;
      for (int distance = 0; distance < (int)(blast.Length * TileSize); distance++)
      {
        for (int y = 0; y < sprite.Height; y++)
        {
          uint color = sprite.Pixel(distance % TileSize, y);
          if (color != 0)
            Frame.Fill(originX + (blast.Direction > 0 ? distance : -distance - 1), originY + y, 1, 1, color);
        }
      }
    }

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

    if (game.Won && game.Credits is { } credits)
    {
      DrawCredits(game, credits);
      if (game.Paused)
        DrawPauseMenu(pauseSelection);

      return;
    }

    DrawHud(game);

    if (game.PoopSplatterTicksRemaining > 0)
      Frame.DrawTranslucent(_sprites["poop-screen-splatter"], (Width - 128) / 2, 64, 140);

    if (!game.IsDying && game.FlamethrowerQuoteTicksRemaining > 0)
      DrawWeaponQuote(game.FlamethrowerQuote, "FLAMETHROWER!");
    if (!game.IsDying && game.BoomStickQuoteTicksRemaining > 0)
      DrawWeaponQuote(game.BoomStickQuote, "BOOM-STICK!");

    if (game.IsDying)
    {
      DrawDeathMessage(game.DeathQuote);
      const string Prompt = "PRESS ANY KEY TO CONTINUE";
      int promptScale = game.AwaitingRestart ? 2 : 1;
      int promptY = game.AwaitingRestart ? 112 : Height - 9;
      if (!game.AwaitingRestart)
        Frame.Fill(0, Height - FooterHeight, Width, FooterHeight, Ink);
      int promptX = (Width - (((Prompt.Length * 4) - 1) * promptScale)) / 2;
      PixelFont.Draw(Frame, Prompt, promptX + 1, promptY + 1, Ink, promptScale);
      PixelFont.Draw(Frame, Prompt, promptX, promptY, Gold, promptScale);
    }
    else if (game.Paused && !game.Won && game.Lives > 0)
      DrawPauseMenu(pauseSelection);
    else if (game.Won || game.Lives == 0)
    {
      string title = game.Won ? "LEVELS COMPLETE!" : game.Lives == 0 ? "GAME OVER" : "PAUSED";
      const string hint = "R TO PLAY AGAIN";

      Frame.Fill(70, 90, 244, 60, Ink);
      Frame.Fill(72, 92, 240, 1, Gold);
      PixelFont.Draw(Frame, title, (Width - (title.Length * 8)) / 2, 105, Gold, 2);
      PixelFont.Draw(Frame, hint, (Width - (hint.Length * 4)) / 2, 130, White);
    }
  }

  private void DrawPauseMenu(int selection)
  {
    Frame.FillTranslucent(70, 76, 244, 104, Ink, 230);
    Frame.Fill(72, 78, 240, 1, Gold);
    DrawTitleText("PAUSED", 89, Gold, 2);
    DrawMenuChoice("RESUME GAME", 116, selection == 0);
    DrawMenuChoice("SETTINGS", 135, selection == 1);
    DrawTitleText("UP/DOWN SELECT  ENTER CONFIRM", 161, White, 1);
  }

  private void DrawWeaponQuote(string quote, string title)
  {
    const int MaxCharacters = 42;
    var lines = new List<string>();
    string remaining = quote.Trim();
    while (remaining.Length > 0 && lines.Count < 3)
    {
      int length = Math.Min(MaxCharacters, remaining.Length);
      if (length < remaining.Length)
      {
        int space = remaining.LastIndexOf(' ', length - 1, length);
        if (space > 0)
          length = space;
      }
      string line = remaining[..length].Trim();
      remaining = remaining[length..].TrimStart();
      if (lines.Count == 2 && remaining.Length > 0)
        line = line[..Math.Min(line.Length, MaxCharacters - 3)] + "...";
      lines.Add(line);
    }

    Frame.Fill(12, 42, Width - 24, 22 + (lines.Count * 12), Ink);
    Frame.Fill(12, 42, Width - 24, 1, Gold);
    DrawTitleText(title, 48, Gold, 1);
    for (int index = 0; index < lines.Count; index++)
      DrawTitleText(lines[index], 60 + (index * 12), White, 2);
  }

  private void DrawCredits(Game game, EndCreditsScreen credits)
  {
    const int ViewTop = 12;
    const int ViewBottom = 198;
    const int ScrollSpeed = 30;
    Frame.OverlayBlack(180);
    long maximumOffset = ViewBottom + credits.ClosingOffset - 72;
    int offset = (int)Math.Min(maximumOffset, game.CreditsTicks * ScrollSpeed / 60);
    foreach (var line in credits.Lines)
    {
      int y = ViewBottom + line.Y - offset;
      if (y >= ViewBottom || y + (5 * line.Scale) <= ViewTop)
        continue;

      int x = (Width - (((line.Text.Length * 4) - 1) * line.Scale)) / 2;
      PixelFont.Draw(Frame, line.Text, x + 1, y + 1, Ink, line.Scale, ViewTop, ViewBottom, line.PreserveCase);
      PixelFont.Draw(Frame, line.Text, x, y, line.Highlight ? Gold : White, line.Scale, ViewTop, ViewBottom, line.PreserveCase);
    }

    int animationFrame = (int)(game.CreditsTicks / 6 % 2);
    Frame.Fill(0, 239, Width, 1, 0xFF355575);
    DrawCreditsRunner($"player-run-{animationFrame + 1}", game.CreditsTicks, 0, 46, false);
    DrawCreditsRunner($"player-large-run-{animationFrame + 1}", game.CreditsTicks, 160, 38, false);
    DrawCreditsRunner($"ella-large-run-{animationFrame + 1}", game.CreditsTicks, 340, 38, false);
    DrawCreditsRunner(animationFrame == 0 ? "enemy-idle" : "enemy-walk", game.CreditsTicks, 290, -32, false);
    DrawCreditsRunner(animationFrame == 0 ? "enemy-walk" : "enemy-idle", game.CreditsTicks, 80, 54, true);
    DrawCreditsRunner($"toaster-cat-run-{animationFrame + 1}", game.CreditsTicks, 380, -50, false);
    DrawCreditsRunner($"zombie-walk-{game.CreditsTicks / 20 % 2 + 1}", game.CreditsTicks, 220, -20, false);
    DrawCreditsRunner($"dog-walk-{animationFrame + 1}", game.CreditsTicks, 110, -28, false);

    if (game.Paused)
      PixelFont.Draw(Frame, "PAUSED", Width - 30, 4, Gold);
  }

  private void DrawCreditsRunner(string name, long ticks, int phase, int speed, bool flip, int bottom = 238)
  {
    const int WrapWidth = Width + 64;
    long distance = (ticks * speed / 60) + phase;
    int x = (int)(((distance % WrapWidth) + WrapWidth) % WrapWidth) - 32;
    var sprite = _sprites[name];
    Frame.Draw(sprite, x, bottom - sprite.Height, flip);
  }

  private void DrawDeathMessage(string quote)
  {
    const string Title = "YOU DIED!";
    const int TitleScale = 4;
    const int QuoteScale = 2;
    const int MaxQuoteCharacters = 42;
    const int MaxQuoteLines = 3;
    const uint TitleColor = 0xFFFF6B6B;
    int titleX = (Width - (((Title.Length * 4) - 1) * TitleScale)) / 2;
    PixelFont.Draw(Frame, Title, titleX + 2, 40, Ink, TitleScale);
    PixelFont.Draw(Frame, Title, titleX, 38, TitleColor, TitleScale);

    string remaining = quote.Trim();
    for (int lineIndex = 0; lineIndex < MaxQuoteLines && remaining.Length > 0; lineIndex++)
    {
      int length = Math.Min(MaxQuoteCharacters, remaining.Length);
      if (length < remaining.Length)
      {
        int space = remaining.LastIndexOf(' ', length - 1, length);
        if (space > 0)
          length = space;
      }

      string line = remaining[..length].Trim();
      remaining = remaining[length..].TrimStart();
      if (lineIndex == MaxQuoteLines - 1 && remaining.Length > 0)
        line = line[..Math.Min(line.Length, MaxQuoteCharacters - 3)] + "...";

      int x = (Width - (((line.Length * 4) - 1) * QuoteScale)) / 2;
      int y = 66 + (lineIndex * 12);
      PixelFont.Draw(Frame, line, x + 1, y + 1, Ink, QuoteScale);
      PixelFont.Draw(Frame, line, x, y, White, QuoteScale);
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

  private void DrawGraveyard(int cameraX, int cameraY, int levelHeight)
  {
    int groundY = HeaderHeight + ((levelHeight - 2) * TileSize) - cameraY;
    for (int index = -1; index < 7; index++)
    {
      int farX = (index * 80) + 48 - ((cameraX / 2) % 80);
      Frame.DrawTranslucent(_sprites["tombstone-cross"], farX, groundY - 32, 150);
      int nearX = (index * 80) + 16 - (((cameraX * 3) / 4) % 80);
      var stone = _sprites[index % 2 == 0 ? "tombstone" : "tombstone-cross"];
      Frame.Draw(stone, nearX, groundY - stone.Height);
    }
  }

  private void DrawBackground(int cameraX, bool night = false)
  {
    for (int y = 0; y < Height; y++)
    {
      uint color = night
        ? y < 70 ? 0xFF101B35 : y < 125 ? 0xFF1A2946 : 0xFF243A50
        : y < 70 ? 0xFF608CC8 : y < 125 ? 0xFF79ADD7 : 0xFF9BCEE1;
      Frame.Fill(0, y, Width, 1, color);
    }

    if (night)
    {
      for (int index = 0; index < 22; index++)
      {
        int x = ((index * 53) + 17) % Width;
        int y = 34 + ((index * 29) % 66);
        Frame.Fill(x, y, 1, 1, 0xFFBFD7E8);
      }
      Frame.Draw(_sprites["moon"], 295, 38);
    }
    else
    {
      Frame.Fill(301, 45, 20, 20, 0xFFFFDF8A);
      Frame.Fill(297, 49, 28, 12, 0xFFFFDF8A);
    }

    for (int i = -1; i < 5; i++)
    {
      int x = (i * 112) - ((cameraX / 5) % 112);
      int y = 55 + ((i & 1) * 17);
      Frame.Fill(x + 8, y, 25, 6, night ? 0xFF2D405A : 0xFFDEEFF4);
      Frame.Fill(x, y + 6, 47, 9, night ? 0xFF2D405A : 0xFFDEEFF4);
      Frame.Fill(x + 5, y + 15, 35, 3, night ? 0xFF23344E : 0xFFB6DCEB);
    }

    for (int x = 0; x < Width; x++)
    {
      int far = Math.Abs(((x + (cameraX / 6)) % 180) - 90);
      int near = Math.Abs(((x + (cameraX / 3) + 60) % 150) - 75);
      int farTop = 110 + (far / 2);
      int nearTop = 150 + (near / 2);
      Frame.Fill(x, farTop, 1, Height - farTop, night ? 0xFF253854 : 0xFF679DA5);
      Frame.Fill(x, nearTop, 1, Height - nearTop, night ? 0xFF1B3041 : 0xFF4A887D);
    }
  }

  private void DrawHud(Game game)
  {
    Frame.Fill(0, 0, Width, HeaderHeight, Ink);
    Frame.Fill(0, HeaderHeight - 2, Width, 2, 0xFF355575);
    PixelFont.Draw(Frame, "Sam n' Ella", 8, 8, Gold);
    PixelFont.Draw(Frame, $"SCORE {game.Score:000000}   COINS {game.Coins:00}   LIVES {game.Lives}   {game.LevelNumber}/{game.LevelCount}", 76, 8, White);
    string seconds = game.LevelSecondsRemaining.ToString("000");
    int timeX = Width - (((seconds.Length * 4) - 1) * 2) - 8;
    uint timeColor = game.LevelSecondsRemaining <= 10 ? 0xFFFF6B6B : Gold;
    PixelFont.Draw(Frame, "TIME", timeX - 23, 8, timeColor);
    PixelFont.Draw(Frame, seconds, timeX, 5, timeColor, 2);
    PixelFont.Draw(Frame, game.Message, 8, 19, 0xFFA7CBDC);

    Frame.Fill(0, Height - FooterHeight, Width, FooterHeight, Ink);
    PixelFont.Draw(Frame, "A/D MOVE  SPACE JUMP  X/CTRL FIRE  ENTER PAUSE  R RESTART  ESC QUIT", 8, Height - 9, White);
  }
}
