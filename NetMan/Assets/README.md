# NetMan pixel art

Each `.sprite` file is a separate image used by the game. Normal characters and tiles are 16 x 16 pixels; enlarged player frames are 16 x 32. Player frames face right; the renderer flips them when moving left. Character sprites are anchored at their feet and centered over their collision body.

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

Colors use six hexadecimal RGB digits, such as `A=57C4B0`. The shared palette has 18 colors. Editing it changes every sprite that uses that symbol. The loader reports invalid dimensions, missing rows, and unknown colors at startup. Sprites may be up to 128 x 128, although tiles should remain 16 x 16 to match the map grid. Normal character and tile checks expect 16 x 16, enlarged player frames expect 16 x 32, overload explosion frames expect 64 x 64, both player and enemy death effects are 24 x 16, and the reward popup can use other dimensions.

Player files: `player-idle.sprite`, `player-run-1.sprite`, `player-run-2.sprite`, `player-jump.sprite`.

Enlarged player files: `player-large-idle.sprite`, `player-large-run-1.sprite`, `player-large-run-2.sprite`, and `player-large-jump.sprite` are editable 16 x 32 sprites. The renderer anchors both sizes at the player's feet. Put `M` in a level to give a question block a growth reward; it uses the same `bonus.sprite` artwork as coin blocks (`?`). Enlarged players shrink when damaged and flash during their brief protection period. Their collision body grows along with the artwork, and growth waits for sufficient headroom.

Power overload: `player-explosion-1.sprite` through `player-explosion-4.sprite` are editable 64 x 64 frames, displayed in order for 100 ms each. The first frame shows the player swelling to a 24 x 48 body and rupturing; the remaining frames scatter blood, guts, and clothing, then leave a final splatter. Use `E`/`F` for blood, `H`/`S` for organ fragments, and `A`/`T`/`B`/`N` for clothing. All frames share the same canvas and foot anchor and remain inside the visible play area. An extra growth reward beyond the enlarged form triggers this fatal 0.4-second animation, after which the player disappears until the two-second death delay ends; pausing freezes both timers. The usual player-death and enemy-death splatter sprites remain separate.

Player death effect: `player-splatter.sprite` uses bright/dark blood reds (`E`/`F`), teal clothing (`A`/`T`), blue clothing (`B`/`N`), and gold trim (`Y`). Edit this file to customize the splatter, then restart the game. The effect replaces the living player during the two-second wait before a level respawn or automatic game restart. Pit deaths keep the splatter within the visible play area so it does not disappear below the screen.

Enemy files: `enemy-idle.sprite`, `enemy-walk.sprite`.

Enemy death effect: `enemy-squashed.sprite` is a 24 x 16 flattened enemy and blood splatter, using the enemy's original yellow, orange, brown, and blue colors (`Y`/`O`/`R`/`D`/`N`/`W`) mixed with blood reds (`E`/`F`). It remains anchored at the enemy's feet for one second after a stomp or pit death. Enemy and player death effects run independently and can appear together; pausing freezes both timers. Edit either death sprite and restart the game to customize its appearance.

Tile files: `grass.sprite`, `dirt.sprite`, `brick.sprite`, `bonus.sprite`, `used.sprite`, `pipe.sprite`, `coin.sprite`, `spike.sprite`, `goal.sprite`.

Pipes can be one or two tiles wide and any number of tiles tall. Stack `P` tiles vertically, keeping the same width on every row. A stack has one top rim, a continuous stem, and a bottom edge. The renderer automatically chooses the editable 16 x 16 pieces. Use lowercase `p` for an explicitly skinny pipe: its vertical stack stays separate even when touching a wide `PP` pipe. Both `p` and `P` are solid, so a shorter skinny pipe can act as a step beside a tall wide pipe. Keep uppercase-only pipe groups separated if they should not join.

This one-tile skinny step touches a three-tile-tall wide pipe. Jump onto the step, then jump again to clear the wide pipe:

```text
.PP
.PP
pPP
###
```

For example, these are a small pipe three tiles tall and a large pipe four tiles tall:

```text
....PP
P...PP
P...PP
P...PP
######
```

Small stacks use `pipe-small-top.sprite`, `pipe-small-body.sprite`, and `pipe-small-bottom.sprite`. Large stacks use `pipe-top-left.sprite`, `pipe-top-right.sprite`, `pipe-body-left.sprite`, `pipe-body-right.sprite`, `pipe-bottom-left.sprite`, and `pipe-bottom-right.sprite`. Body pieces repeat for extra height; two-tile-tall pipes only need top and bottom pieces. Single-tile pipes, one-row groups, and irregular shapes retain the unchanged original `pipe.sprite` artwork. A large pipe begins at 2 x 2 tiles (32 x 32 pixels); each extra row adds 16 pixels of height. To restore the standard artwork everywhere, copy `pipe.sprite` over the stack-piece sprite files.

Brick break animation: `brick-rubble.sprite` shows broken chunks for 0.25 seconds, followed by `brick-dust.sprite` showing sparse dust for 0.25 seconds. The effect disappears after 0.5 seconds of game time. Both frames are editable 16 x 16 sprites and use the shared brick colors. Pausing freezes the animation. Each broken brick animates independently, and its collision disappears as soon as it breaks. Coin blocks keep their existing reward and used-block behavior.

Reward popup: `reward-plus-one.sprite` is an 8 x 6 pixel image containing the gold `+1` and its shadow. Edit its pixel grid to customize the reward artwork. You can also change its width and height; it stays centered above the rewarded block and floats upward for one second of game time. The popup uses the shared palette, including `Y` for gold and `K` for its shadow. Its dimensions are independent of the 16 x 16 map tiles.

Restart `dotnet run` after editing source art. Running the project copies assets into its build output. For a published game, edit the `.sprite` files and palette inside the `Assets` directory beside `NetMan.exe`, then relaunch.

## PNG previews

`Previews` contains standard transparent PNG exports for inspecting or sharing the art. They are generated from the `.sprite` sources; editing a preview PNG does not change the game. From the NetMan project directory, regenerate them with:

```powershell
dotnet run -- --export-art Assets/Previews
```

`sprite-sheet.png` shows all sprites in alphabetical filename order, numbered from 1. `game-preview.png` is a frame produced by the same renderer used in the game window. PNG exports use only .NET compression, with no image library dependency.
