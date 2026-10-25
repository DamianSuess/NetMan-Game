#requires -Version 5.1

<#
.SYNOPSIS
Builds and packages NetMan as a size-optimized, trimmed Native AOT executable.
.DESCRIPTION
Run on Windows with the .NET 10 SDK and Visual Studio's Desktop development
with C++ workload installed. Paths are based on this script, not the current
directory. The first three FileVersion components determine the archive name.
Output: Publish/NetMan-vX.Y.Z/ and Publish/NetMan-vX.Y.Z.zip.
.EXAMPLE
.\build.ps1
.EXAMPLE
.\build.ps1 -Runtime win-arm64 -SkipTests
#>

[CmdletBinding()]
param(
  [ValidateSet('win-x64', 'win-arm64')]
  [string]$Runtime = 'win-x64',

  [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet
{
  param([string[]]$Arguments)

  & dotnet @Arguments | Out-Host

  if ($LASTEXITCODE -ne 0)
  {
    throw "dotnet $($Arguments[0]) failed (exit code $LASTEXITCODE). No new release was packaged; see the build output above."
  }
}

function Assert-PublishChild
{
  param([string]$Path)

  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $prefix = $publishRoot + [System.IO.Path]::DirectorySeparatorChar

  if (-not $fullPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase))
  {
    throw "Refusing to modify a path outside Publish: $fullPath"
  }

  # Do not follow directory links when replacing or cleaning generated output.
  $candidate = $fullPath

  while ($candidate.Length -ge $publishRoot.Length)
  {
    if (Test-Path -LiteralPath $candidate)
    {
      $item = Get-Item -LiteralPath $candidate -Force

      if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)
      {
        throw "Refusing to modify linked output: $candidate"
      }
    }

    $candidate = [System.IO.Path]::GetDirectoryName($candidate)
  }
}

if ($env:OS -ne 'Windows_NT')
{
  throw 'NetMan currently has a Windows backend. Build this Native AOT release on Windows.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue))
{
  throw 'The .NET 10 SDK is required. Install it and ensure dotnet is on PATH.'
}

$projectPath = Join-Path $PSScriptRoot 'NetMan/NetMan.csproj'
$testProjectPath = Join-Path $PSScriptRoot 'NetMan.Tests/NetMan.Tests.csproj'
$publishRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Publish'))

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf))
{
  throw "Project not found: $projectPath"
}

# Ask MSBuild so imported properties and future version changes are respected.
$metadataOutput = & dotnet msbuild $projectPath -nologo '-getProperty:FileVersion,AssemblyName'

if ($LASTEXITCODE -ne 0)
{
  throw 'Could not read the project version with MSBuild.'
}

$metadata = ($metadataOutput -join [Environment]::NewLine) | ConvertFrom-Json
$fileVersion = [string]$metadata.Properties.FileVersion

if ($fileVersion -notmatch '^\d+\.\d+\.\d+(\.\d+)?$')
{
  throw "FileVersion must contain Major.Minor.Revision, optionally followed by a fourth component. Found: $fileVersion"
}

$versionParts = $fileVersion.Split('.')
$releaseName = 'NetMan-v' + ($versionParts[0..2] -join '.')
$releasePath = Join-Path $publishRoot $releaseName
$archivePath = Join-Path $publishRoot ($releaseName + '.zip')
$stagingRoot = Join-Path $publishRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))
$payloadPath = Join-Path $stagingRoot $releaseName
$stagedArchive = Join-Path $stagingRoot ($releaseName + '.zip')
$executableName = [string]$metadata.Properties.AssemblyName + '.exe'
Assert-PublishChild $releasePath
Assert-PublishChild $archivePath
Assert-PublishChild $stagingRoot

Write-Host "Building $releaseName for $Runtime..."
Invoke-DotNet -Arguments @('build', $projectPath, '--configuration', 'Release', '--nologo')

if (-not $SkipTests)
{
  Push-Location $PSScriptRoot
  try
  {
    Invoke-DotNet -Arguments @('test', '--project', $testProjectPath, '--configuration', 'Release')
  }
  finally
  {
    Pop-Location
  }
}

New-Item -ItemType Directory -Path $payloadPath -Force | Out-Null

try
{
  $publishArguments = @(
    'publish', $projectPath,
    '--configuration', 'Release',
    '--runtime', $Runtime,
    '--self-contained', 'true',
    '--output', $payloadPath,
    '--nologo',
    '-p:NetManDistribution=true',
    '-p:PublishAot=true',
    '-p:PublishTrimmed=true',
    '-p:TrimMode=full',
    '-p:OptimizationPreference=Size',
    '-p:InvariantGlobalization=true',
    '-p:UseSizeOptimizedLinq=true',
    '-p:UseSystemResourceKeys=true',
    '-p:DebuggerSupport=false',
    '-p:EventSourceSupport=false',
    '-p:StackTraceSupport=false',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '-p:StripSymbols=true',
    '-p:CopyOutputSymbolsToPublishDirectory=false',
    '-p:TrimmerRemoveSymbols=true',
    '-p:TreatWarningsAsErrors=true'
  )
  Invoke-DotNet -Arguments $publishArguments

  if (-not (Test-Path -LiteralPath (Join-Path $payloadPath $executableName) -PathType Leaf))
  {
    throw 'Publish completed without producing the expected native executable.'
  }

  if (-not (Test-Path -LiteralPath (Join-Path $payloadPath 'Assets/palette.txt') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $payloadPath 'Assets/player-splatter.sprite') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $payloadPath 'Assets/enemy-squashed.sprite') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $payloadPath 'Assets/brick-rubble.sprite') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $payloadPath 'Assets/brick-dust.sprite') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $payloadPath 'Levels') -PathType Container))
  {
    throw 'Publish is missing required game assets or levels.'
  }

  $licensePath = Join-Path $PSScriptRoot 'license.md'

  if (Test-Path -LiteralPath $licensePath -PathType Leaf)
  {
    Copy-Item -LiteralPath $licensePath -Destination $payloadPath
  }

  # Native AOT can emit a separate PDB even when managed symbols are disabled.
  foreach ($symbol in Get-ChildItem -LiteralPath $payloadPath -Recurse -File -Filter '*.pdb')
  {
    Assert-PublishChild $symbol.FullName
    Remove-Item -LiteralPath $symbol.FullName -Force
  }

  # Include one top-level versioned directory, ready to extract and run.
  Compress-Archive -LiteralPath $payloadPath -DestinationPath $stagedArchive -CompressionLevel Optimal

  # Existing releases survive build/publish/ZIP failures; replace only after success.
  if (Test-Path -LiteralPath $releasePath)
  {
    Assert-PublishChild $releasePath
    Remove-Item -LiteralPath $releasePath -Recurse -Force
  }

  if (Test-Path -LiteralPath $archivePath)
  {
    Assert-PublishChild $archivePath
    Remove-Item -LiteralPath $archivePath -Force
  }

  Move-Item -LiteralPath $payloadPath -Destination $releasePath
  Move-Item -LiteralPath $stagedArchive -Destination $archivePath
  $archive = Get-Item -LiteralPath $archivePath
  $executable = Get-Item -LiteralPath (Join-Path $releasePath $executableName)
  Write-Host ("Native executable: {0:N2} MB" -f ($executable.Length / 1MB))
  Write-Host ("ZIP: {0} ({1:N2} MB)" -f $archive.FullName, ($archive.Length / 1MB))
  Write-Host "Release: $releasePath"
}
finally
{
  if (Test-Path -LiteralPath $stagingRoot)
  {
    Assert-PublishChild $stagingRoot
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
  }
}
