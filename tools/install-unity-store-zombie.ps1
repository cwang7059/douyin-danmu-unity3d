# Guides Unity login (Hub / Editor) and installs Asset Store Zombie (30232).
# Does NOT ask for or store passwords — sign in only in official Unity UI.
#
# Usage:
#   .\tools\install-unity-store-zombie.ps1              # Open Hub + Unity + guide
#   .\tools\install-unity-store-zombie.ps1 -Finalize      # After PM Import, copy to Resources
#
# From repo root.

param(
    [switch]$Finalize,
    [switch]$OpenOnly,
    [string]$UnityExe = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Resolve-UnityExe {
    param([string]$RequestedUnityExe)
    if (-not [string]::IsNullOrWhiteSpace($RequestedUnityExe) -and (Test-Path -LiteralPath $RequestedUnityExe)) {
        return (Resolve-Path -LiteralPath $RequestedUnityExe).Path
    }
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE) -and (Test-Path -LiteralPath $env:UNITY_EXE)) {
        return (Resolve-Path -LiteralPath $env:UNITY_EXE).Path
    }
    $projectVersionPath = Join-Path $Root "ProjectSettings\ProjectVersion.txt"
    if (Test-Path -LiteralPath $projectVersionPath) {
        $versionLine = Select-String -Path $projectVersionPath -Pattern "m_EditorVersion:\s*(.+)$" | Select-Object -First 1
        if ($versionLine) {
            $version = $versionLine.Matches[0].Groups[1].Value.Trim()
            $candidate = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$version\Editor\Unity.exe"
            if (Test-Path -LiteralPath $candidate) { return $candidate }
        }
    }
    $hubRoot = Join-Path $env:ProgramFiles "Unity\Hub\Editor"
    if (Test-Path -LiteralPath $hubRoot) {
        $candidate = Get-ChildItem -LiteralPath $hubRoot -Recurse -Filter Unity.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
    }
    throw "Cannot find Unity.exe. Pass -UnityExe or set UNITY_EXE."
}

function Test-ZombieAlreadyInstalled {
    $targets = @(
        (Join-Path $Root "Assets\Resources\RealisticZombies\UnityStore\Zombie1.fbx"),
        (Join-Path $Root "Assets\Resources\RealisticZombies\UnityStore\Zombie1.glb")
    )
    foreach ($t in $targets) {
        if (Test-Path -LiteralPath $t) { return $true }
    }
    return $false
}

function Invoke-UnityFinalize {
    param([string]$ResolvedUnityExe)
    $LogFile = Join-Path ([IO.Path]::GetTempPath()) ("unity-zombie-finalize-" + [Guid]::NewGuid().ToString("N") + ".log")
    Write-Host "[Zombie] Copying imported models to Resources (batchmode)..."
    $unityArgs = @(
        "-batchmode", "-quit", "-nographics",
        "-projectPath", $Root,
        "-executeMethod", "ApocalypseKingZombieAssetImport.FinalizeUnityStoreZombieForBatchMode",
        "-logFile", $LogFile
    )
    $process = Start-Process -FilePath $ResolvedUnityExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $LogFile) {
            Select-String -Path $LogFile -Pattern "error|Error|Zombie" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host $_.Line }
            Write-Host "[LOG] $LogFile"
        }
        throw "Finalize failed. Complete Package Manager Import first, then retry -Finalize."
    }
    Write-Host "[OK] Models copied to Assets/Resources/RealisticZombies/UnityStore/"
}

if ($Finalize) {
    if (Test-ZombieAlreadyInstalled) {
        Write-Host "[OK] RealisticZombies/UnityStore/Zombie1 already present."
        exit 0
    }
    $unity = Resolve-UnityExe $UnityExe
    Invoke-UnityFinalize $unity
    & "$PSScriptRoot\import-zombie-units.ps1"
    exit 0
}

# --- Guided install (no password prompt) ---
$hubExe = Join-Path ${env:ProgramFiles} "Unity Hub\Unity Hub.exe"
if (Test-Path -LiteralPath $hubExe) {
    Write-Host "[Zombie] Opening Unity Hub (sign in here with Unity ID)..."
    Start-Process -FilePath $hubExe
}
else {
    Write-Host "[Zombie] Unity Hub not found at default path. Sign in at https://id.unity.com/"
    Start-Process "https://id.unity.com/"
}

Write-Host ""
Write-Host "========== Unity login + Zombie package =========="
Write-Host ""
Write-Host "Security: never send your password in chat or scripts. Use Unity Hub / Editor only."
Write-Host ""
Write-Host "Step 1 - Sign in"
Write-Host "  - Unity Hub: top-right Sign in"
Write-Host "  - Unity ID email + password, or Google / Apple"
Write-Host "  - Forgot password: https://id.unity.com/"
Write-Host ""
Write-Host "Step 2 - Open this project"
Write-Host "  - In Hub: douyin-danmu-Unity3D (Unity 2022.3)"
Write-Host ""

if (-not $OpenOnly) {
    try {
        $unity = Resolve-UnityExe $UnityExe
        Write-Host "[Zombie] Opening Unity project..."
        Start-Process -FilePath $unity -ArgumentList @("-projectPath", $Root)
    }
    catch {
        Write-Host "[Zombie] Could not auto-launch Unity. Open the project manually from Hub."
    }
}

Start-Process "https://assetstore.unity.com/packages/3d/characters/humanoids/zombie-30232"

Write-Host "Step 3 - Download and Import in Unity Editor"
Write-Host "  - Menu: Apocalypse King > Install Unity Store Zombie > 2. Open Package Manager"
Write-Host "  - Or: Window > Package Manager, filter My Assets"
Write-Host "  - Search Zombie, Download, Import into project"
Write-Host "  - If prompted, sign in with the same Unity ID as Hub"
Write-Host ""
Write-Host "Step 4 - After Import, run in project root:"
Write-Host "  .\tools\install-unity-store-zombie.ps1 -Finalize"
Write-Host ""
Write-Host "Or Unity menu: Apocalypse King > Install Unity Store Zombie > 3. Copy Zombie1-3..."
Write-Host "=================================================="
Write-Host ""

if (Test-ZombieAlreadyInstalled) {
    Write-Host "[OK] Zombie1 already in RealisticZombies. Run build-and-start.bat to test."
}
