param(
    [string]$UnityExe = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
$LogFile = Join-Path ([IO.Path]::GetTempPath()) ("unity-bind-vfx-" + [Guid]::NewGuid().ToString("N") + ".log")

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
Write-Host "[BIND] Unity: $ResolvedUnityExe"
Write-Host "[BIND] Project: $ProjectRoot"

$unityArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectRoot,
    "-executeMethod", "ApocalypseKingVfxPrefabBinder.BindAllForBatchMode",
    "-logFile", $LogFile
)

$process = Start-Process -FilePath $ResolvedUnityExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
if (-not (Test-Path -LiteralPath $LogFile)) {
    throw "Unity exited without creating a bind log."
}

$bindSucceeded = Select-String -Path $LogFile -Pattern "VFX bind batch complete" -Quiet -ErrorAction SilentlyContinue
$nuclearOk = Select-String -Path $LogFile -Pattern "NuclearDetonation OK" -Quiet -ErrorAction SilentlyContinue
$compileErrors = Select-String -Path $LogFile -Pattern "error CS" -ErrorAction SilentlyContinue
if ($compileErrors) {
    $compileErrors | ForEach-Object { Write-Host $_.Line }
}

Select-String -Path $LogFile -Pattern "\[ApocalypseKing\].*Nuclear|VFX bind batch|No store VFX" -ErrorAction SilentlyContinue |
    ForEach-Object { Write-Host $_.Line }

if ($process.ExitCode -ne 0 -or -not $bindSucceeded) {
    $projectOpen = Select-String -Path $LogFile -Pattern "another Unity instance|already open" -Quiet -ErrorAction SilentlyContinue
    Write-Host "[LOG] $LogFile"
    if ($projectOpen) {
        throw "Unity project is open. Close the Unity Editor and rerun: .\tools\bind-vfx-prefabs.ps1"
    }

    throw "Unity VFX bind failed."
}

if (-not $nuclearOk) {
    Write-Warning "NuclearDetonation validation line not found in log. Check Effect_NuclearDetonation.asset prefab."
}

Write-Host "[OK] VFX prefabs bound. Log: $LogFile"
