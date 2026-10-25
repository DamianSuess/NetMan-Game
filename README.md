# Learn.DosGame

Active development lives in [NetMan](NetMan/README.md), a .NET pixel-art platformer with a native Windows game window and editable text levels. [NetMan-0.1](NetMan-0.1/README.md) preserves the original console version.

![](screen-shot.png)

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

It builds Release, runs the MSTest suite, and publishes a self-contained, trimmed Native AOT Windows x64 executable optimized for size. It then creates `Publish/NetMan-vX.Y.Z/` and `Publish/NetMan-vX.Y.Z.zip`. The ZIP contains that versioned directory with `NetMan.exe`, runtime assets, editable levels, and the license. No .NET installation is needed on the player's machine.

The name comes from the first three components of `FileVersion` in `NetMan/NetMan.csproj` (Major.Minor.Revision). Change that property to change the package version. Rebuilding the same version replaces its generated folder and ZIP after publishing and compression succeed; older versions are retained. `Publish` is ignored by Git.

Requirements: Windows, PowerShell 5.1 or later, the .NET 10 SDK, and Visual Studio 2022 or later with **Desktop development with C++**, including its default MSVC and Windows SDK components. The first Native AOT build also restores compiler/runtime packages from NuGet. See Microsoft's [Native AOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

Optional arguments:

```powershell
.\build.ps1 -SkipTests
.\build.ps1 -Runtime win-arm64
```

ARM64 publishing needs the corresponding C++ ARM64 build tools. Both architectures use the requested version-only ZIP name, so publishing another architecture replaces the same version's previous output.

To reduce size, tests live in a separate project, and the distribution excludes PNG-export tooling, preview images, asset documentation, and debug symbols. Runtime `.sprite` files, the palette, and levels remain editable. Publishing favors size over speed, uses invariant globalization, and removes runtime stack traces, debugger support, diagnostic events, and full framework exception messages. Normal development builds retain their existing behavior. See Microsoft's [AOT optimization](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/optimizing) and [trimming options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options).

## Tests

`NetMan.Tests` uses MSTest.Sdk with Microsoft.Testing.Platform. From the repository root:

```powershell
dotnet test --solution .\NetMan\NetMan.slnx
dotnet test --project .\NetMan.Tests\NetMan.Tests.csproj --configuration Release
```

Open `NetMan/NetMan.slnx` in Visual Studio to discover and run the tests in Test Explorer. Tests cover gameplay, collisions, reward and animation timing, both splatter effects, asset validation, and rendering without opening a game window. Each scenario has its own temporary level files, so tests can run independently and in parallel.

`global.json` selects the .NET 10 SDK family and the MTP runner. The first test run restores Microsoft's test packages from NuGet. These packages belong only to the test project and are not included in the game or release ZIP. `build.ps1` (and `build.bat`) runs Release tests before publishing and stops if any fail; `-SkipTests` explicitly bypasses them.
