# NetMan

A C# / .NET 10 pixel-art platformer with its own software renderer and native Windows game window. No third-party engine, graphics library, or NuGet packages are required. Collect coins, stomp enemies, cross gaps, and reach the green flag at the end of each level.

## Play

Install the .NET 10 SDK, open Windows Terminal in this folder, and run:

```powershell
dotnet run
```

The game opens a separate resizable window. It renders at 384 x 240 pixels and starts at 3x scale (1152 x 720 client area). Resizing uses whole-number scaling and letterboxing to keep the pixel art crisp. Keyboard input is received only by the game window. Switching to another app automatically pauses the game; press P to resume after returning.

This version runs on Windows. The gameplay, text-level loader, sprite loader, pixel renderer, and PNG exporter use portable .NET code. `IGameWindow` separates those components from the Win32 backend; Linux and macOS would need their own window/input/presentation implementations and testing.

| Key | Action |
| --- | --- |
| A / D or left / right | Move |
| Space / W / up | Jump; hold for a higher jump |
| P | Pause / resume |
| R | Restart the entire game |
| Esc | Quit |

You have three lives. Enemy side contact, spikes, and falling into a pit cost a life and reload the current level. Land on an enemy from above to stomp it. Stomped enemies flatten into a blood splatter mixed with their original colors, visible for one second of game time before disappearing. Hit `?` blocks from below for a coin; brick blocks break when hit from below. Scores and collected coin totals persist through deaths; the current map resets. Reach the goal in every level to win.

## Make levels

Edit `Levels/*.txt` in any text editor, then restart the game. `dotnet run` copies them into the output directory. Files play in alphabetical order: use names such as `01-meadow.txt`, `02-ruins.txt`, `03-your-level.txt`. The game discovers additional `.txt` files automatically on startup.

Each character is one tile. Rows run from top to bottom; columns run left to right. No header or comments are used inside a map.

| Character | Meaning |
| --- | --- |
| `.` or space | Empty air |
| `S` | Player spawn; exactly one per map |
| `#` | Solid ground / wall |
| `B` | Breakable brick |
| `?` | One-coin bonus block |
| `P` | Solid pipe |
| `o` | Coin |
| `E` | Walking enemy |
| `^` | Deadly spike |
| `G` | Green goal flag; at least one per map |

Example:

```text
..............................
..............................
..........o...................
.........B?B..................
..............................
..S...........E.............G.
##############################
```

Maps support 10–10000 columns and 3–100 rows. Shorter rows are padded with air. Place the spawn and enemies in empty space above solid ground. Leave openings in the bottom ground rows to make pits. The camera follows horizontally and vertically, so maps can exceed the visible screen. A full jump rises roughly 2.8 tiles; start with obstacles up to two tiles tall and gaps up to four tiles wide. Test every map to ensure the goal is reachable. The loader validates map syntax and reports the file and position of unknown characters.

To load maps directly from another folder:

```powershell
dotnet run -- --levels "C:\path\to\maps"
```

To distribute a Windows x64 executable with its own runtime:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

Ship the entire `publish` folder, including its `Assets` and editable `Levels` directories. When running the published game, edit the maps beside `NetMan.exe` and relaunch it.

## Pixel art

Character artwork lives in separate files under `Assets`: `player-idle.sprite`, `player-run-1.sprite`, `player-run-2.sprite`, `player-jump.sprite`, `enemy-idle.sprite`, and `enemy-walk.sprite`. Tiles and objects also have their own sprite files. Characters and tiles are 16 x 16 pixels; the editable `enemy-squashed.sprite` effect is 24 x 16, and the coin-block reward popup uses the editable 8 x 6 `reward-plus-one.sprite`. Sprites share an 18-color palette for a retro look; the framebuffer uses 32-bit color.

The `.sprite` files are editable pixel grids. Each symbol represents one pixel, `.` is transparent, and colors are defined in `Assets/palette.txt`. Edit the source files and restart the game to see changes. See [Assets/README.md](Assets/README.md) for the format.

`Assets/Previews` contains transparent PNG copies of every sprite, a contact sheet, and a scene preview. These are exported previews; the game reads `.sprite` files. Regenerate the PNGs after editing artwork:

```powershell
dotnet run -- --export-art Assets/Previews
```

## Development

There are no NuGet package dependencies. `Game.cs` contains fixed-step physics and game rules, `Level.cs` parses text maps, `PixelRenderer.cs` composes the scrolling scene, and `PixelBuffer.cs` draws sprites and exports PNGs. `WindowsGameWindow.cs` owns the Win32 window, event-driven keyboard input, and integer-scaled GDI presentation. All native declarations live in `NativeMethods.cs`. Physics uses fractional positions and small collision steps to prevent movement through walls.

Run the headless checks (no interactive console required):

```powershell
dotnet run -- --self-test
```

The checks cover gameplay, sprite validation, transparency, flipping, clipping, scene composition, and paused animation. They run without creating a window.

This version includes animated walking, a jumping pose, shaded tiles, a pixel HUD, parallax scenery, and pause/win/game-over overlays. It does not include sound, power-up transformations, bosses, or a graphical level editor.

Native backend references: [CreateWindowExW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createwindowexw), [StretchDIBits](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-stretchdibits).
