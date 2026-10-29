# Sam n' Ella - _Brutal Pixel Art Game_

> In the words of Ivan Drago,\
> &nbsp; &nbsp;"_If he dies, he dies._"

![](Docs/Sam-and-Ella.png)

This aint ya mamma's side scroller. Sam n' Ella (_previously, NetMan_) is a pixel-art platformer with a native game window and editable Sprites, Text levels, and really bad uncle quotes. Written 100% in C# and not forgiving like modern "Mario" games. _Saves? NO! Surprises? YES!_

![](screen-shot.png)

![Player collecting an extra heart and exploding](Docs/Player-Explosion.gif)

![Graveyard Shift](Docs/Level-Graveyard.png)

From this repository folder, run:

```powershell
dotnet run --project .\SamElla\SamElla.csproj
```

See the game README for controls, editable text levels, and development checks.

## Build a release

From the repository root, run:

```powershell
.\build.ps1
```

The build also produces a Velopack installer and automatic-update packages in
`Publish/Velopack/win-x64/`. The .NET SDK restores the pinned local packaging tool;
no global Velopack installation is required. Use `-SkipInstaller` to produce only
the original Native AOT folder and ZIP. `-Runtime win-arm64` produces a separate
ARM64 update channel when the required native toolchain is installed.

Project and output paths resolve relative to the script, so it also works when invoked by absolute path from another working directory.

To publish and launch the game, double-click `build-exec.bat` or run:

```powershell
.\build-exec.bat
```

This runs the same Release build, tests, Native AOT publish, and ZIP packaging, then opens the newly published game. If any step fails, the game is not launched. Build options are forwarded, for example `.\build-exec.bat -SkipTests`. The PowerShell equivalent is `.\build.ps1 -Run`.

## Install and automatically update

Install `SuessLabs.SamElla-win-x64-Setup.exe` from the project's
[GitHub Releases](https://github.com/SuessLabs/Sam-n-Ella-Game/releases).
Velopack-managed installations and its generated portable ZIP check that repository's
stable releases at launch. Updates download without blocking the game. If a download
finishes while the title screen is open, the app exits cleanly and restarts updated;
otherwise it installs when the player exits. A pending downloaded update can also
be applied automatically at the next launch. Offline play remains available, and
update failures are logged to `%LocalAppData%/SamElla/Logs/updates.log`.

The Velopack package ID is `SuessLabs.SamElla`. This is a new installation identity:
users of the former `SuessLabs.NetMan` package must install the new setup EXE once.
This release does not provide an automatic migration between those package IDs.
The desktop and Start menu shortcuts are named `Sam-Ella`.
User content, audio settings, and update logs now live under `%LocalAppData%/SamElla`.
To preserve previous custom content and settings, close the game and copy the
`Content` folder and `audio-settings.txt` from `%LocalAppData%/NetMan` into
`%LocalAppData%/SamElla` before launching the new version.

The original `SamElla-vX.Y.Z.zip` remains available as an unmanaged portable build;
it does not self-update. Existing ZIP users must install the setup EXE or use the
Velopack-generated portable ZIP once to enable automatic updates.

For each release, increment the project FileVersion and build, then upload **all files**
from `Publish/Velopack/win-x64/` as assets of the same GitHub Release. The full `.nupkg`
and `releases.win-x64.json` are required; uploading only a ZIP is insufficient.
The new `publish-release.yml` workflow performs the build, tests, and upload automatically
when a GitHub Release is published from a tag containing this integration. It also
attaches the original ZIP. The workflow uses GitHub's built-in release token; client
applications contain no GitHub credentials. Draft and prerelease releases are not
offered by the app. If using immutable GitHub releases, build locally and attach the
assets while the release is still a draft, before publishing it.

The four-part Windows FileVersion is preserved. Velopack uses a three-part package
version: `Major.Minor.(Revision * 65536 + Build)`. For example, `0.5.0.71` becomes
`0.5.71`, and `0.5.1.0` becomes `0.5.65536`. This preserves ordering when either
Revision or Build increases. Verify the mapping with `./scripts/Test-PackageVersion.ps1`.

For managed installations, editable levels and assets live in
`%LocalAppData%/SamElla/Content/Levels` and `.../Assets`, outside the replaceable app
directory. New defaults are copied forward only if the old copy was not edited;
custom files and intentionally deleted defaults are preserved. Unmodified retired
defaults are removed. The existing `--levels <directory>` override still works.

## Hubmle Beginnings

NetMan started out as a side-scrolling DOS pixle art game. The orginal code can be found in the Legacy folder [NetMan-0.1](Legacy/NetMan-0.1/README.md) which preserves the original console version.

## Developer Notes

* Active development lives in [SamElla](SamElla/README.md)
* More [developer notes](Docs/dev-notes.md)
