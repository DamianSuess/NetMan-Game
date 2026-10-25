# NetBro

A small C# / .NET 10 console platformer inspired by classic side scrollers, with original ASCII visuals. Collect coins, stomp enemies, cross gaps, and reach the green `F` at the end of each level.

## Play

Install the .NET 10 SDK, open Windows Terminal in this folder, and run:

```powershell
dotnet run
```

Use a console at least 46 columns by 16 rows; about 100 by 28 works well. Windows Terminal is recommended for ANSI colors and smooth redraws.

| Key | Action |
| --- | --- |
| A / D or left / right | Move |
| Space / W / up | Jump; hold for a higher jump |
| P | Pause / resume |
| R | Restart the entire game |
| Esc | Quit |

You have three lives. Enemy side contact, spikes, and falling into a pit cost a life and reload the current level. Land on an enemy from above to stomp it. Hit `?` blocks from below for a coin; brick blocks break when hit from below. Scores and collected coin totals persist through deaths; the current map resets. Reach the goal in every level to win.

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
| `P` | Solid pipe (displayed as `H`) |
| `o` | Coin |
| `E` | Walking enemy (displayed as `m`) |
| `^` | Deadly spike |
| `G` | Goal (displayed as `F`); at least one per map |

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

Ship the entire `publish` folder, including its editable `Levels` directory. When running the published game, edit the maps beside `NetBro.exe` and relaunch it.

## Development

There are no NuGet package dependencies. `Game.cs` contains fixed-step physics and game rules, `Level.cs` parses text maps, `ConsoleScreen.cs` draws the scrolling view, and `Keyboard.cs` polls held Windows keys. Physics uses fractional positions and small collision steps to prevent movement through walls.

Run the headless checks (no interactive console required):

```powershell
dotnet run -- --self-test
```

This first version covers the core platformer loop. It does not include Mario artwork, music, power-up transformations, bosses, or a graphical level editor.
