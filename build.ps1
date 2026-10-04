<#
.SYNOPSIS
    Builds What Lies In The Depths for Web, Windows and macOS, then zips each for itch.io.

.DESCRIPTION
    Runs the Unity editor in batch mode once per target, using the Build Profiles in
    Assets/Settings/Build Profiles. Output goes where the manual builds already go:

        Builds/Web/                                   -> Builds/ItchBuild_Web.zip
        Builds/Windows/What Lies In The Depths/       -> Builds/ItchBuild_Windows.zip
        Builds/MacOS/What Lies In The Depths.app      -> Builds/ItchBuild_MacOS.zip

    Close the Unity editor first: Unity cannot open the same project twice.

.EXAMPLE
    .\build.ps1                       # all three
    .\build.ps1 -Targets Web          # just one
    .\build.ps1 -Targets Web,Windows -NoZip
    .\build.ps1 -Publish              # build all three, then upload them to itch.io
    .\build.ps1 -Publish -SkipBuild   # upload the zips that are already in Builds/

    If PowerShell refuses to run the script:
    powershell -ExecutionPolicy Bypass -File .\build.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Web', 'Windows', 'MacOS')]
    [string[]]$Targets = @('Web', 'MacOS', 'Windows'),   # Windows last, so the editor reopens on it

    # Full path to Unity.exe. Found automatically from ProjectVersion.txt when omitted.
    [string]$UnityPath,

    # Skip the ItchBuild_*.zip step.
    [switch]$NoZip,

    # Upload each ItchBuild_*.zip to itch.io with butler once it has built.
    [switch]$Publish,

    # With -Publish: upload the zips already in Builds/ without rebuilding anything.
    [switch]$SkipBuild,

    # itch.io target, "username/game-slug" (the slug is the last part of the game's page URL).
    [string]$ItchGame = 'drewthebear/wlitd'
)

$ErrorActionPreference = 'Stop'
$Root   = $PSScriptRoot
$Builds = Join-Path $Root 'Builds'
$Logs   = Join-Path $Root 'Logs'

# Profile = Build Profile asset. BuildPath = what Unity is told to build (-build).
# ZipSource = folder that gets zipped. ZipKeepFolder = whether that folder itself is the
# top-level entry in the zip (Web must have index.html at the root of the zip for itch).
$Config = @{
    Web = @{
        Profile       = 'Assets/Settings/Build Profiles/Web.asset'
        BuildPath     = Join-Path $Builds 'Web'
        Clean         = Join-Path $Builds 'Web'
        Expect        = Join-Path $Builds 'Web\index.html'
        ZipSource     = Join-Path $Builds 'Web'
        ZipKeepFolder = $false
    }
    Windows = @{
        Profile       = 'Assets/Settings/Build Profiles/Windows.asset'
        BuildPath     = Join-Path $Builds 'Windows\What Lies In The Depths\WhatLiesInTheDepths.exe'
        Clean         = Join-Path $Builds 'Windows'
        Expect        = Join-Path $Builds 'Windows\What Lies In The Depths\WhatLiesInTheDepths.exe'
        ZipSource     = Join-Path $Builds 'Windows\What Lies In The Depths'
        ZipKeepFolder = $true
    }
    MacOS = @{
        Profile       = 'Assets/Settings/Build Profiles/macOS.asset'
        BuildPath     = Join-Path $Builds 'MacOS\What Lies In The Depths.app'
        Clean         = Join-Path $Builds 'MacOS'
        Expect        = Join-Path $Builds 'MacOS\What Lies In The Depths.app\Contents\Info.plist'
        ZipSource     = Join-Path $Builds 'MacOS\What Lies In The Depths.app'
        ZipKeepFolder = $true
    }
}

# itch.io channel per target. Butler tags the platform from the channel name.
$Channels = @{ Web = 'html5'; Windows = 'windows'; MacOS = 'mac' }

function Find-Unity {
    $versionLine = Get-Content (Join-Path $Root 'ProjectSettings\ProjectVersion.txt') |
        Where-Object { $_ -match '^m_EditorVersion:\s*(\S+)' } | Select-Object -First 1
    if (-not $versionLine) { throw 'Could not read the editor version from ProjectSettings/ProjectVersion.txt.' }
    $version = $Matches[1]

    $installRoots = @("$env:ProgramFiles\Unity\Hub\Editor")
    # Unity Hub records a custom install location here, as a quoted path.
    $secondary = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $secondary) {
        $custom = (Get-Content $secondary -Raw).Trim().Trim('"') -replace '\\\\', '\'
        if ($custom) { $installRoots += $custom }
    }

    foreach ($dir in $installRoots) {
        $exe = Join-Path $dir "$version\Editor\Unity.exe"
        if (Test-Path $exe) { return $exe }
    }
    throw "Unity $version was not found under: $($installRoots -join ', '). Pass -UnityPath 'C:\path\to\Unity.exe'."
}

# Zips a folder with forward-slash entry names. Windows PowerShell's built-in zipping writes
# backslashes, which breaks the zip on macOS and stops itch.io finding Build/ in the web build.
function New-Zip([string]$Source, [string]$Zip, [bool]$KeepFolder) {
    $base = if ($KeepFolder) { Split-Path $Source -Parent } else { $Source }
    $skip = $base.TrimEnd('\').Length + 1
    $archive = [IO.Compression.ZipFile]::Open($Zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem $Source -Recurse -File -Force | ForEach-Object {
            $entry = $_.FullName.Substring($skip).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $_.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
}

function Invoke-UnityBuild([string]$Name) {
    $c   = $Config[$Name]
    $log = Join-Path $Logs "build-$Name.log"

    # Start from an empty output folder so files from an older build cannot linger.
    if (Test-Path $c.Clean) { Remove-Item $c.Clean -Recurse -Force }

    $unityArgs = @(
        '-batchmode', '-quit',
        '-projectPath', "`"$Root`"",
        '-activeBuildProfile', "`"$($c.Profile)`"",
        '-build', "`"$($c.BuildPath)`"",
        '-logFile', "`"$log`""
    ) -join ' '

    Write-Host "==> Building $Name ..." -ForegroundColor Cyan
    $timer = [Diagnostics.Stopwatch]::StartNew()
    # Unity.exe is a GUI program, so PowerShell only waits for it (and gets its exit code) this way.
    $proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru

    # Unity 6000.6 can crash while opening the project when it has to switch platform on the way
    # in ("manager 'PlayerSettings' is NULL"), before any building starts. The switch itself
    # sticks, so a second launch opens on the right platform and builds normally.
    $crashedOnOpen = $proc.ExitCode -ne 0 -and (Test-Path $log) -and
        -not (Select-String -Path $log -Pattern 'Build Finished, Result' -Quiet)
    if ($crashedOnOpen) {
        Write-Host '    Unity crashed while opening the project; trying once more ...' -ForegroundColor Yellow
        Copy-Item $log (Join-Path $Logs "build-$Name.crash.log") -Force
        $proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
    }
    $timer.Stop()

    if ($proc.ExitCode -ne 0 -or -not (Test-Path $c.Expect)) {
        Write-Host "    FAILED (exit code $($proc.ExitCode)). Last lines of $log :" -ForegroundColor Red
        if (Test-Path $log) { Get-Content $log -Tail 30 | ForEach-Object { Write-Host "    $_" } }
        return $false
    }
    Write-Host ("    done in {0:mm\:ss}" -f $timer.Elapsed) -ForegroundColor Green

    # Unity drops debug-symbol folders next to the player; their names say not to ship them.
    # Move them out of the build folder so they stay out of the zip.
    $keep = Join-Path $Builds "DoNotShip\$Name"
    if (Test-Path $keep) { Remove-Item $keep -Recurse -Force }
    Get-ChildItem $c.ZipSource -Directory |
        Where-Object { $_.Name -match '_BackUpThisFolder_ButDontShipItWithYourGame$|_DoNotShip$' } |
        ForEach-Object {
            New-Item -ItemType Directory -Force -Path $keep | Out-Null
            Move-Item $_.FullName $keep
        }

    if (-not $NoZip) {
        $zip = Join-Path $Builds "ItchBuild_$Name.zip"
        if (Test-Path $zip) { Remove-Item $zip -Force }
        Write-Host "    zipping -> $zip"
        New-Zip $c.ZipSource $zip $c.ZipKeepFolder
    }
    return $true
}

# ---------------------------------------------------------------------------------------

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

if ($Publish) {
    if ($NoZip) { throw '-Publish uploads the zips, so it cannot be combined with -NoZip.' }
    if (-not (Get-Command butler -ErrorAction SilentlyContinue)) {
        throw 'butler was not found on PATH. Install it from https://itch.io/docs/butler/ and run "butler login" once.'
    }
} elseif ($SkipBuild) { throw '-SkipBuild only makes sense together with -Publish.' }

if (-not $SkipBuild) {
if (-not $UnityPath) { $UnityPath = Find-Unity }
if (-not (Test-Path $UnityPath)) { throw "Unity was not found at '$UnityPath'." }

# The editor holds this file open for as long as it has the project open.
$lock = Join-Path $Root 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Dispose() }
    catch { throw 'This project is open in the Unity editor. Close Unity and run the script again.' }
}

New-Item -ItemType Directory -Force -Path $Builds, $Logs | Out-Null
Write-Host "Unity:   $UnityPath"
}
Write-Host "Targets: $($Targets -join ', ')"

$results = [ordered]@{}
$uploadFailed = @()
foreach ($t in $Targets) {
    if ($SkipBuild) {
        $results[$t] = Test-Path (Join-Path $Builds "ItchBuild_$t.zip")
        if (-not $results[$t]) { Write-Host "==> $t : Builds\ItchBuild_$t.zip does not exist" -ForegroundColor Red }
    } else {
        $results[$t] = Invoke-UnityBuild $t
    }

    if ($Publish -and $results[$t]) {
        $version = (Get-Content (Join-Path $Root 'version.json') -Raw | ConvertFrom-Json).version
        $dest    = "${ItchGame}:$($Channels[$t])"
        Write-Host "==> Uploading $t to $dest (v$version) ..." -ForegroundColor Cyan
        & butler push (Join-Path $Builds "ItchBuild_$t.zip") $dest --userversion $version
        if ($LASTEXITCODE -ne 0) { $results[$t] = $false; $uploadFailed += $t }
    }
}

Write-Host ''
Write-Host 'Summary' -ForegroundColor Cyan
foreach ($t in $results.Keys) {
    if ($results[$t]) { Write-Host "  $t : OK" -ForegroundColor Green }
    elseif ($uploadFailed -contains $t) { Write-Host "  $t : built, but the UPLOAD FAILED (see butler's message above)" -ForegroundColor Red }
    else              { Write-Host "  $t : FAILED (see the output above, or Logs\build-$t.log)" -ForegroundColor Red }
}
if ($results.Values -contains $false) { exit 1 }
