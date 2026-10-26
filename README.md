# Learn.DosGame

Active development lives in [NetMan](NetMan/README.md), a .NET pixel-art platformer with a native Windows game window and editable text levels. [NetMan-0.1](NetMan-0.1/README.md) preserves the original console version.

![](screen-shot.png)

![Player collecting an extra heart and exploding](Docs/Player-Explosion.gif)

![Graveyard Shift](Docs/Level-Graveyard.png)

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

## Automatic build versions

`.github/workflows/increment-develop-build.yml` increments the fourth version component after a pull request is merged into `develop`. For example, `0.4.0.60` becomes `0.4.0.61`. The workflow updates `FileVersion` and `AssemblyVersion` together in `NetMan/NetMan.csproj` and commits the change back to `develop` as `github-actions[bot]`. The first three components remain under your control. Opening a PR, adding commits to it, closing it without merging, and directly pushing a normal commit do not increment the version.

Each version commit records the PR number and merge commit. Rerunning a completed workflow does not increment again. Simultaneous merges use normal pushes with retries against the latest `develop`, so both merges receive an increment; the workflow never force-pushes. After a successful run, pull the latest `develop` to use its updated version locally. ZIP names keep their existing three-part format, such as `NetMan-v0.4.0.zip`.

Enable GitHub Actions for the repository and allow this workflow's `contents: write` permission. The repository's branch rules must permit the automation identity to push the version commit to `develop`. If your rules require all changes through PRs, signed commits, or restricted push identities, configure an approved automation identity and its credentials rather than disabling those rules. The workflow uses the repository's built-in `GITHUB_TOKEN` by default. If your approved automation account needs a different token, store it as the repository secret `VERSION_BUMP_TOKEN`; checkout will use that token instead. Signing requirements need a signing step configured for the automation identity. The workflow does not change repository permissions or branch rules. A rejected push fails with an explanation and can be rerun after the repository policy is configured.

PRs from branches in this repository use the `pull_request` merged/closed event. Fork and Dependabot PRs use `pull_request_target` because ordinary fork PR tokens are read-only. GitHub loads target-event workflows from the repository's **default branch**, so also include this workflow on that branch when enabling fork/Dependabot support (`master` if that is your default branch). Both routes require an already-merged PR targeting `develop`; only the trusted, merged `develop` checkout is used, never the unmerged PR head. The PR marker protects against duplicate processing. See GitHub's [PR event documentation](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows) and [token permission documentation](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#permissions).

At build number `65534`, automation stops instead of overflowing the assembly-version limit. Manually advance the release prefix and reset the fourth component in both version properties before the next merge. Keep those properties synchronized and do not manually update the build component in ordinary feature PRs.

To validate the automation locally with PowerShell 7 and Git:

```powershell
pwsh -NoProfile -File .github/scripts/Test-BuildVersion.ps1
```

This creates disposable repositories under the ignored `output/VersioningChecks` folder. It tests increments, reruns, simultaneous merges, formatting preservation, dirty checkout rejection, unmerged commits, overflow, release-prefix changes, and server-side write rejection without contacting GitHub or modifying the current checkout.
