# Learn.DosGame

Active development lives in [NetMan](NetMan/README.md), a .NET pixel-art platformer with a native Windows game window and editable text levels. [NetMan-0.1](NetMan-0.1/README.md) preserves the original console version.

![](screen-shot.png)

![Player collecting an extra heart and exploding](Docs/Player-Explosion.gif)

From this repository folder, run:

```powershell
dotnet run --project .\NetMan\NetMan.csproj
```

See the game README for controls, editable text levels, and development checks.

## Build a release

From the repository root, run:

```powershell
.\build.ps1
```

Project and output paths resolve relative to the script, so it also works when invoked by absolute path from another working directory.

To publish and launch the game, double-click `build-exec.bat` or run:

```powershell
.\build-exec.bat
```

This runs the same Release build, tests, Native AOT publish, and ZIP packaging, then opens the newly published game. If any step fails, the game is not launched. Build options are forwarded, for example `.\build-exec.bat -SkipTests`. The PowerShell equivalent is `.\build.ps1 -Run`.
