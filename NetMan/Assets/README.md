# NetMan pixel art

Each `.sprite` file is a separate image used by the game. The bundled characters and tiles are 16 x 16 pixels. Player frames face right; the renderer flips them when moving left. Character sprites are anchored at their feet and centered over their collision body.

## Editing a sprite

The first non-comment line contains `width height`. Exactly `height` pixel rows follow, each containing exactly `width` symbols. `.` means transparent. Other symbols reference `palette.txt`. Blank lines and lines beginning with `;` are ignored.

For example, this is a valid 4 x 4 image:

```text
4 4
.KK.
KAAK
KTTK
.KK.
```

Colors use six hexadecimal RGB digits, such as `A=57C4B0`. The shared palette has 18 colors. Editing it changes every sprite that uses that symbol. The loader reports invalid dimensions, missing rows, and unknown colors at startup. Sprites may be up to 128 x 128, although tiles should remain 16 x 16 to match the map grid. Bundled character and tile checks expect 16 x 16; the player death effect is 24 x 16, and the reward popup can use other dimensions.

Player files: `player-idle.sprite`, `player-run-1.sprite`, `player-run-2.sprite`, `player-jump.sprite`.

Player death effect: `player-splatter.sprite` uses bright/dark blood reds (`E`/`F`), teal clothing (`A`/`T`), blue clothing (`B`/`N`), and gold trim (`Y`). Edit this file to customize the splatter, then restart the game. The effect replaces the living player for one second before a level respawn or automatic game restart. Pit deaths keep the splatter within the visible play area so it does not disappear below the screen.

Enemy files: `enemy-idle.sprite`, `enemy-walk.sprite`.

Enemy death effect: `enemy-squashed.sprite` is a 24 x 16 flattened enemy and blood splatter, using the enemy's original yellow, orange, brown, and blue colors mixed with blood reds (`E`/`F`). It remains for one second after a stomp or pit death. Enemy and player death effects run independently and can appear together; pausing freezes both timers. Edit either death sprite and restart the game to customize its appearance.

Tile files: `grass.sprite`, `dirt.sprite`, `brick.sprite`, `bonus.sprite`, `used.sprite`, `pipe.sprite`, `coin.sprite`, `spike.sprite`, `goal.sprite`.

Reward popup: `reward-plus-one.sprite` is an 8 x 6 pixel image containing the gold `+1` and its shadow. Edit its pixel grid to customize the reward artwork. You can also change its width and height; it stays centered above the rewarded block and floats upward for one second of game time. The popup uses the shared palette, including `Y` for gold and `K` for its shadow. Its dimensions are independent of the 16 x 16 map tiles.

Restart `dotnet run` after editing source art. Running the project copies assets into its build output. For a published game, edit the `.sprite` files and palette inside the `Assets` directory beside `NetMan.exe`, then relaunch.

## PNG previews

`Previews` contains standard transparent PNG exports for inspecting or sharing the art. They are generated from the `.sprite` sources; editing a preview PNG does not change the game. From the NetMan project directory, regenerate them with:

```powershell
dotnet run -- --export-art Assets/Previews
```

`sprite-sheet.png` shows all sprites in alphabetical filename order, numbered from 1. `game-preview.png` is a frame produced by the same renderer used in the game window. PNG exports use only .NET compression, with no image library dependency.
