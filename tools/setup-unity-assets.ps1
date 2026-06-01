param(
    [string]$UnityExe = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
$LogFile = Join-Path ([IO.Path]::GetTempPath()) ("unity-setup-danmu-" + [Guid]::NewGuid().ToString("N") + ".log")

function Resolve-UnityExe {
    param([string]$RequestedUnityExe)

    if (-not [string]::IsNullOrWhiteSpace($RequestedUnityExe) -and (Test-Path -LiteralPath $RequestedUnityExe)) {
        return (Resolve-Path -LiteralPath $RequestedUnityExe).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE) -and (Test-Path -LiteralPath $env:UNITY_EXE)) {
        return (Resolve-Path -LiteralPath $env:UNITY_EXE).Path
    }

    $projectVersionPath = Join-Path $ProjectRoot "ProjectSettings\ProjectVersion.txt"
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

    $hubRoot = Join-Path $env:ProgramFiles "Unity\Hub\Editor"
    if (Test-Path -LiteralPath $hubRoot) {
        $candidate = Get-ChildItem -LiteralPath $hubRoot -Recurse -Filter Unity.exe -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    throw "Cannot find Unity.exe. Pass -UnityExe or set UNITY_EXE."
}

$ResolvedUnityExe = Resolve-UnityExe $UnityExe
Write-Host "[SETUP] Unity: $ResolvedUnityExe"
Write-Host "[SETUP] Project: $ProjectRoot"

$unityArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectRoot,
    "-executeMethod", "ApocalypseKingProjectSetup.SetupProjectAssetsForBatchMode",
    "-logFile", $LogFile
)

$process = Start-Process -FilePath $ResolvedUnityExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
if (-not (Test-Path -LiteralPath $LogFile)) {
    throw "Unity exited without creating a setup log."
}

$setupSucceeded = Select-String -Path $LogFile -Pattern "Batch setup complete" -Quiet -ErrorAction SilentlyContinue
$compileErrors = Select-String -Path $LogFile -Pattern "error CS" -ErrorAction SilentlyContinue
if ($compileErrors) {
    $compileErrors | ForEach-Object { Write-Host $_.Line }
}

if ($process.ExitCode -ne 0 -or -not $setupSucceeded) {
    Write-Host "[LOG] $LogFile"
    throw "Unity asset setup failed. Close the Unity Editor if the project is open, then retry."
}

Write-Host "[OK] HUD, danmu mapping, battle effect/audio assets, and scene references are ready."
Write-Host "[LOG] $LogFile"
