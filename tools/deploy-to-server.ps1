param(
    [string]$Server = "10.100.20.25",
    [string]$RemoteShare = "rkx-remote",
    [string]$RemoteFolder = "douyin-danmu-Unity3D",
    [int]$DanmuHttpPort = 8765,
    [string]$DanmuHttpHost = "+",
    [switch]$BuildOnly,
    [switch]$SkipBuild,
    [switch]$RunUnityEditor,
    [switch]$RunPlayer,
    [string]$UnityExe = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$RemoteRoot = "\\$Server\$RemoteShare\$RemoteFolder"
$GameExe = Join-Path $Root "Builds\Windows\ApocalypseKingUnity3D.exe"

function Resolve-UnityExe {
    param([string]$RequestedUnityExe)

    if (-not [string]::IsNullOrWhiteSpace($RequestedUnityExe) -and (Test-Path -LiteralPath $RequestedUnityExe)) {
        return (Resolve-Path -LiteralPath $RequestedUnityExe).Path
    }

    $projectVersionPath = Join-Path $Root "ProjectSettings\ProjectVersion.txt"
    $versions = @()
    if (Test-Path -LiteralPath $projectVersionPath) {
        $versionLine = Select-String -Path $projectVersionPath -Pattern "m_EditorVersion:\s*(.+)$" | Select-Object -First 1
        if ($versionLine) {
            $version = $versionLine.Matches[0].Groups[1].Value.Trim()
            $versions += $version
            $versions += ($version -replace "c\d+$", "")
        }
    }

    foreach ($version in ($versions | Select-Object -Unique)) {
        $candidate = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Cannot find Unity.exe. Pass -UnityExe or install Unity 2022.3.50f1."
}

function Get-RemoteProjectPath {
    param([string]$TargetServer, [string]$ShareName, [string]$FolderName)
    return "C:\Users\rkx\$ShareName\$FolderName"
}

Write-Host "[DEPLOY] Target: $RemoteRoot"

if (-not (Test-Path -LiteralPath "\\$Server\$RemoteShare")) {
    throw "Cannot access remote share \\$Server\$RemoteShare"
}

if (-not $SkipBuild) {
    if (Test-Path -LiteralPath (Join-Path $Root "Temp\UnityLockfile")) {
        Write-Host "[WARN] Unity Editor has this project open. Close it before batch build, or pass -SkipBuild to deploy source only." -ForegroundColor Yellow
        if (-not $RunUnityEditor) {
            throw "Unity project is open. Close Unity Editor or use -SkipBuild -RunUnityEditor."
        }
    }
    else {
        $resolvedUnity = Resolve-UnityExe $UnityExe
        Write-Host "[BUILD] Building Windows player with $resolvedUnity ..."
        & (Join-Path $Root "build-and-start.ps1") -UnityExe $resolvedUnity -BuildOnly -DanmuHttpPort $DanmuHttpPort
    }
}

Write-Host "[SYNC] Copying project to remote share (excluding Library/Temp/Builds cache) ..."
New-Item -ItemType Directory -Force -Path $RemoteRoot | Out-Null

$robocopyArgs = @(
    $Root,
    $RemoteRoot,
    "/MIR",
    "/XD", "Library", "Temp", "Logs", "UserSettings", ".git", "_downloads",
    "/XF", "*.csproj", "*.sln",
    "/NFL", "/NDL", "/NJH", "/NJS", "/NC", "/NS"
)
$null = & robocopy @robocopyArgs
if ($LASTEXITCODE -ge 8) {
    throw "Robocopy failed with exit code $LASTEXITCODE"
}

if (Test-Path -LiteralPath $GameExe) {
    $remoteBuildDir = Join-Path $RemoteRoot "Builds\Windows"
    New-Item -ItemType Directory -Force -Path $remoteBuildDir | Out-Null
    Copy-Item -LiteralPath $GameExe -Destination (Join-Path $remoteBuildDir "ApocalypseKingUnity3D.exe") -Force
    $dataDir = Join-Path $Root "Builds\Windows\ApocalypseKingUnity3D_Data"
    if (Test-Path -LiteralPath $dataDir) {
        robocopy $dataDir (Join-Path $remoteBuildDir "ApocalypseKingUnity3D_Data") /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
    }
}

$remoteProjectPath = Get-RemoteProjectPath -TargetServer $Server -ShareName $RemoteShare -FolderName $RemoteFolder

$remoteStartScript = @"
@echo off
setlocal
cd /d "$remoteProjectPath"
set GAME=$remoteProjectPath\Builds\Windows\ApocalypseKingUnity3D.exe
if not exist "%GAME%" (
  echo [ERROR] Game not built. Open Unity and use Apocalypse King / Build Windows Player.
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File "$remoteProjectPath\tools\ensure-http-urlacl.ps1" -Port $DanmuHttpPort
start "" "%GAME%" -danmuHttpHost $DanmuHttpHost -danmuHttpPort $DanmuHttpPort -logFile "$remoteProjectPath\Logs\game-run.log"
echo [OK] Started danmu game. HTTP http://${Server}:$DanmuHttpPort/
"@

$remoteStartScript | Set-Content -LiteralPath (Join-Path $RemoteRoot "start-game-remote.bat") -Encoding ASCII

$remoteUnityScript = @"
@echo off
setlocal
set PROJECT=$remoteProjectPath
set UNITY=
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.50f1c1\Editor\Unity.exe" set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.50f1c1\Editor\Unity.exe
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe" set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe
if "%UNITY%"=="" (
  echo [ERROR] Unity 2022.3.50f1 not found on this machine.
  exit /b 1
)
start "" "%UNITY%" -projectPath "%PROJECT%"
echo [OK] Opening Unity project: %PROJECT%
"@

$remoteUnityScript | Set-Content -LiteralPath (Join-Path $RemoteRoot "open-unity-remote.bat") -Encoding ASCII

$remoteBuildScript = @"
@echo off
setlocal
set PROJECT=$remoteProjectPath
set UNITY=
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.50f1c1\Editor\Unity.exe" set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.50f1c1\Editor\Unity.exe
if exist "C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe" set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe
if "%UNITY%"=="" (
  echo [ERROR] Unity 2022.3.50f1 not found on this machine.
  exit /b 1
)
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod ApocalypseKingSceneBuilder.BuildWindowsPlayer -logFile "%PROJECT%\Logs\unity-build.log"
if errorlevel 1 (
  echo [ERROR] Unity build failed. See Logs\unity-build.log
  exit /b 1
)
echo [OK] Build complete: %PROJECT%\Builds\Windows\ApocalypseKingUnity3D.exe
"@

$remoteBuildScript | Set-Content -LiteralPath (Join-Path $RemoteRoot "build-remote.bat") -Encoding ASCII

$urlAclScript = @'
param([int]$Port = 8765)
$prefix = "http://+:$Port/"
$existing = netsh http show urlacl url=$prefix 2>$null
if ($LASTEXITCODE -ne 0) {
    netsh http add urlacl url=$prefix user=Everyone | Out-Null
    "Added urlacl $prefix" | Out-File -FilePath (Join-Path $PSScriptRoot "..\Logs\urlacl.log") -Append
}
'@
$urlAclScript | Set-Content -LiteralPath (Join-Path $RemoteRoot "tools\ensure-http-urlacl.ps1") -Encoding UTF8
New-Item -ItemType Directory -Force -Path (Join-Path $RemoteRoot "Logs") | Out-Null

if ($BuildOnly) {
    Write-Host "[OK] Deploy sync complete. Remote path: $RemoteRoot"
    exit 0
}

Write-Host "[OK] Deployed to $RemoteRoot"
Write-Host "On server $Server (VNC/RDP/local), run:"
Write-Host "  $remoteProjectPath\open-unity-remote.bat     # Open Unity Editor"
Write-Host "  $remoteProjectPath\build-remote.bat          # Batch build Windows player"
Write-Host "  $remoteProjectPath\start-game-remote.bat     # Run game + danmu HTTP on LAN"
Write-Host ""
Write-Host "From this machine after game is running:"
Write-Host "  Invoke-RestMethod http://${Server}:$DanmuHttpPort/health"
Write-Host "  .\tools\send-danmu-test.ps1 -HostUrl http://${Server}:$DanmuHttpPort"
