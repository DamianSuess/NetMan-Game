# NetMan

A C# / .NET 10 pixel-art platformer with its own software renderer and native Windows game window. The game requires no third-party engine, graphics library, or NuGet packages. The separate MSTest project restores Microsoft test packages during development. Collect coins, stomp enemies, cross gaps, and reach the green flag at the end of each level.

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

You have three lives. Enemy side contact, landing on a spike tip from above, and falling into a pit cost a life. Spikes block sideways movement like pipes without causing side-contact damage. Touching them from below is safe. The player becomes a blood-and-clothing splatter while gameplay freezes for two seconds of game time, then shows PRESS ANY KEY TO CONTINUE. A fresh keypress reloads the current level; holding a key does not dismiss the screen. After the last life, the same prompt restarts from level one with three lives and cleared rewards. Pausing freezes the death timers. Land on an enemy from above to stomp it. Dead enemies become a flattened splatter of blood and their original colors for one second, then disappear. Enemy and player splatters can appear together and expire independently. Hit `?` blocks from below for a coin; brick blocks break when hit from below. Scores and collected coin totals persist through level respawns; the current map resets. Reach the goal in every level to win.

## Title screen

The animated title screen opens before Level 1. Choose Sam or Ella with A/D or the Left/Right arrow keys. Both previews show the powered-up size; Ella wears purple and has a ponytail. Press Enter or Space to start, or Esc to quit. Gameplay begins at normal size; the selection preview does not grant a power-up. The selected character remains active through growth, shrinking, deaths, level changes, and restarts. Both characters use the same movement and power settings. Characters move along the bottom beneath the selection, and the copyright appears in the bottom-right corner. Losing window focus freezes the title animation.

## Make levels

Edit `Levels/*.txt` in any text editor, then restart the game. `dotnet run` copies them into the output directory. Files play in alphabetical order: use names such as `01-meadow.txt`, `02-ruins.txt`, `03-your-level.txt`. The game discovers additional `.txt` files automatically on startup.

Each character is one tile. Rows run from top to bottom; columns run left to right. No header or comments are used inside a map.

| Character | Meaning |
| --- | --- |
| `.` or space | Empty air |
| `S` | Player spawn; exactly one per map |
| `#` | Solid ground / wall |
| `B` | Breakable brick; animates into rubble and dust over 0.5 seconds |
| `?` | One-coin bonus block |
| `M` | Growth bonus block; looks like `?`, releases a collectible beating heart, then becomes a used block |
| `F` | Flamethrower bonus block; looks like `?`, releases a moving weapon pickup |
| `P` | Solid pipe |
| `p` | Skinny solid pipe; stacks vertically and stays separate beside `PP` pipes |
| `o` | Coin |
| `E` | Walking enemy |
| `C` | Toaster Cat; runs faster and hops |
| `Z` | Tall, slow-walking zombie; place in its bottom tile with clear space above |
| `D` | Dog; patrols and leaves poop piles on solid ground |
| `^` | Spike; blocks sideways movement and is deadly only when landing on its tip from above |
| `G` | Green goal flag; at least one per map |

Toaster Cats appear from Level 2 onward. Their metal toaster bodies have toast, cat faces, paws, and tails. They run a little faster than ordinary enemies and hop between short ground runs. Edit `Assets/enemies.txt` and relaunch to balance both types independently: `Speed` is tiles per second (0-30), `PowerLevel` is enemy health (1-10), and `HopVelocity` is hop strength (0-20; zero disables hopping). Defaults are speed 3 / health 1 / no hopping for walkers and speed 6 / health 2 / hop strength 7 for Toaster Cats. A nonfatal stomp briefly stuns the enemy and bounces the player safely away. Defeating an enemy by stomping awards 200 points per power level; pit falls defeat enemies regardless of power. Enemies regain full health when a level reloads. Side contact keeps the existing shrink-or-die behavior.

Zombies appear from Level 4 onward. They match the powered-up player in height, shuffle with outstretched arms, and have torn clothing and a dangling eyeball. Put `Z` in the bottom tile of their two-tile space, leaving the tile above clear. Their editable `[Zombie]` settings default to speed 1 tile per second, health 3, and no hopping. That means three small-player stomps or two large-player stomps by default. Their blood, skin, and clothing splatter lasts one second.

Dogs appear from Level 5 onward. Put `D` in a level to spawn one. They leave smiling poop emoji piles behind them every three seconds of grounded walking. Stepping in a pile removes it and halves running speed for five seconds, with a translucent screen splatter for two seconds. Jump over piles to avoid them. Another pile refreshes both timers without stacking the slowdown. Jump strength remains unchanged. Pausing freezes the dog and both effect timers; reloading a level clears the piles and effects. Dog speed, health, and hopping are configurable under `[Dog]` in `Assets/enemies.txt` (defaults: speed 2, health 1, no hopping). Stomps and side contact follow the existing enemy rules.

Player settings are in the same file under `[Player.PowerLevel1]` (small) and `[Player.PowerLevel2]` (large). For each form, `Speed` controls running, `HopVelocity` controls jump takeoff, and `PowerLevel` controls stomp damage (1-10). Defaults are speed 12 and jump velocity 15 for both forms, with power 1 when small and power 2 when large. Each stomp subtracts the current player power from enemy health, so a default Toaster Cat needs two small-player stomps or one large-player stomp. Growing and shrinking immediately switch the active settings. The growth-heart overload limit remains unchanged.

The Level 2 power-up is a handheld flamethrower. Put `F` in a level for a question block that releases the weapon; chase and collect it to gain 100 points and equip it. A weapon block is included in each bundled level. Only a fully grown player can collect the flamethrower. Small players leave the pickup untouched and receive no growth or points from it. Sam and Ella can both use it. Hold X or Ctrl to fire in the direction you face. Four editable fire frames animate every 100 ms, reaching up to three tiles from the player and stopping at solid walls, pipes, and blocks. The stream deals current stomp damage plus one (normally 3 when large), at most once every 0.3 seconds. Fire kills award the same points and one-second enemy splatter as stomps. It does not break terrain. Taking damage removes the weapon first while keeping the large form and granting the usual protection; the next unprotected hit shrinks the player. The weapon carries into the next level and is cleared by death or restarting. Collecting another flamethrower while already equipped causes the existing overload explosion. Growth hearts keep their existing overload behavior.

Use `M` for selected question blocks that release growth hearts and `?` for coin blocks. Hit an `M` from below to release a beating human heart above it. Chase the heart and touch it to earn 100 points and grow; opening the block alone awards neither growth nor points. Hearts move in the direction the player was facing, hop along the ground, and reverse at solid walls and pipes. They can fall into pits, and uncollected hearts are cleared when the level reloads. Movement and heartbeat freeze while paused or dead. Growth makes the player two tiles tall with a matching collision body. If there is not enough headroom after collecting a heart, growth waits until the player reaches open space. An enemy hit or spike landing shrinks the enlarged player and gives 1.5 seconds of flashing protection; another hit after protection expires costs a life. Stomping enemies remains safe. Falling into a pit still costs a life. Growth carries into the next level, while death or restarting returns the player to normal size.

The growth-heart limit is one growth reward at a time; the separate flamethrower provides the second power-up tier. Collecting another heart while enlarged (or while growth is waiting for headroom) causes an overload: the player grows one extra size and explodes, losing one life. Four editable animation frames show the oversized rupture, body fragments, spreading blood and guts, and final splatter. Each frame lasts 100 ms, for a 0.4-second animation. After the final explosion frame, the player disappears while the two-second death delay finishes, then waits for a fresh keypress to restart the level. Pausing freezes the animation. Coin blocks (`?`) do not count toward this limit, and shrinking frees the growth slot again.

Every death displays a large `YOU DIED!` title with a randomly chosen quote underneath during the two-second death delay and while waiting for a keypress to continue. Edit `Assets/death-quotes.txt` to customize the messages: one quote per line, without surrounding quotation marks. Blank lines and lines starting with `;` are ignored. The file includes 42 quotes; add, remove, or replace lines as desired, then relaunch the game. Longer messages wrap to three lines and are shortened with an ellipsis if needed. A missing or empty file uses a fallback quote.

Completing the final level starts scrolling end credits over a translucent black overlay, with characters running across the bottom. The credits open with a random dark-humored victory quote, credit Damian Suess, Suess Labs, and Xeno Innovations, and give special thanks to Little Stinker. Your run statistics show completion time, deaths, points, and items missed. Completion time includes active gameplay, level transitions, and death animations, while excluding pauses, waiting for a respawn key, and the credits themselves. Missed items are coins and reward-block contents never collected during the run; hearts only count as collected when touched, and items found before a death are remembered across retries. Brick obstacles and enemies are not counted as missed items.

The credits end on another random play-again quote. Edit `Assets/credits-opening-quotes.txt` and `Assets/credits-closing-quotes.txt` to customize these lines, using the same one-quote-per-line format as death quotes. Press `P` to pause the credits, `R` to start a new run, or `Esc` to quit. Starting a new run resets the statistics.

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

Character artwork lives in separate files under `Assets`: `player-idle.sprite`, `player-run-1.sprite`, `player-run-2.sprite`, `player-jump.sprite`, `enemy-idle.sprite`, and `enemy-walk.sprite`. Tiles and objects also have their own sprite files. Characters and tiles are 16 x 16 pixels; `player-splatter.sprite` and `enemy-squashed.sprite` are wider 24 x 16 death effects, and the coin-block reward popup uses the editable 8 x 6 `reward-plus-one.sprite`. Sprites share an 22-color palette for a retro look; the framebuffer uses 32-bit color.

The `.sprite` files are editable pixel grids. Each symbol represents one pixel, `.` is transparent, and colors are defined in `Assets/palette.txt`. Edit the source files and restart the game to see changes. See [Assets/README.md](Assets/README.md) for the format.

`Assets/Previews` contains transparent PNG copies of every sprite, a contact sheet, and a scene preview. These are exported previews; the game reads `.sprite` files. Regenerate the PNGs after editing artwork:

```powershell
dotnet run -- --export-art Assets/Previews
```

## Development

The game project has no NuGet package dependencies. `Game.cs` contains fixed-step physics and game rules, `Level.cs` parses text maps, `PixelRenderer.cs` composes the scrolling scene, and `PixelBuffer.cs` draws sprites and exports PNGs. `WindowsGameWindow.cs` owns the Win32 window, event-driven keyboard input, and integer-scaled GDI presentation. All native declarations live in `NativeMethods.cs`. Physics uses fractional positions and small collision steps to prevent movement through walls.

Run the MSTest suite from this directory (no game window required):

```powershell
dotnet test --solution .\NetMan.slnx
```

The tests live in `../NetMan.Tests`, use MSTest with Microsoft.Testing.Platform, and can also run in Visual Studio Test Explorer. They cover gameplay, sprite validation, transparency, flipping, clipping, scene composition, and paused animation. They run without creating a window.

This version includes animated walking, a jumping pose, growth power-ups, shaded tiles, a pixel HUD, parallax scenery, and pause/win/game-over overlays. It does not include sound, bosses, or a graphical level editor.

Native backend references: [CreateWindowExW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createwindowexw), [StretchDIBits](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-stretchdibits).
