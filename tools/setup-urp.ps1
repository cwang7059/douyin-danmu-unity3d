param(
    [string]$UnityExe = "",
    [switch]$SkipMaterialUpgrade,
    [switch]$InstallFireballIfPresent
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
$LogFile = Join-Path ([IO.Path]::GetTempPath()) ("unity-urp-setup-" + [Guid]::NewGuid().ToString("N") + ".log")

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
        $latest = Get-ChildItem -LiteralPath $hubRoot -Directory | Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) {
            $candidate = Join-Path $latest.FullName "Editor\Unity.exe"
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    throw "Unity.exe not found. Pass -UnityExe or set UNITY_EXE."
}

$unity = Resolve-UnityExe -RequestedUnityExe $UnityExe
Write-Host "[ApocalypseKing] URP setup via: $unity"
Write-Host "[ApocalypseKing] Log: $LogFile"

$methods = @("ApocalypseKingUrpSetup.CreateAndAssignUrpPipeline")
if (-not $SkipMaterialUpgrade) {
    $methods += "ApocalypseKingUrpSetup.UpgradeProjectMaterialsToUrp"
}
if ($InstallFireballIfPresent) {
    $methods += "ApocalypseKingUrpSetup.InstallPterosaurFireballToResources"
}

foreach ($method in $methods) {
    Write-Host "[ApocalypseKing] Running $method ..."
    & $unity -batchmode -nographics -quit `
        -projectPath $ProjectRoot `
        -executeMethod $method `
        -logFile $LogFile
    if ($LASTEXITCODE -ne 0) {
        $errors = Select-String -Path $LogFile -Pattern "error CS|Error|Exception" -ErrorAction SilentlyContinue | Select-Object -Last 20
        if ($errors) {
            $errors | ForEach-Object { Write-Host $_.Line }
        }
        throw "Unity failed ($method), exit $LASTEXITCODE. See $LogFile"
    }
}

Write-Host "[ApocalypseKing] URP pipeline assigned. Re-open Unity Editor and enter Play Mode."
Write-Host "  Next: import Fireball Pack, then run menu URP/3 or: .\tools\setup-urp.ps1 -InstallFireballIfPresent"
