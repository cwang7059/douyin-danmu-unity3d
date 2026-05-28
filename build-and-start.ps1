param(
    [string]$UnityExe = "",
    [int]$DanmuHttpPort = 8765,
    [switch]$DisableDanmuHttp,
    [switch]$KeepBuildLog,
    [switch]$KeepRunLog,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GameArgs
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameExe = Join-Path $Root "Builds\Windows\ApocalypseKingUnity3D.exe"

function Resolve-UnityExe {
    param([string]$RequestedUnityExe)

    if (-not [string]::IsNullOrWhiteSpace($RequestedUnityExe) -and (Test-Path -LiteralPath $RequestedUnityExe)) {
        return (Resolve-Path -LiteralPath $RequestedUnityExe).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE) -and (Test-Path -LiteralPath $env:UNITY_EXE)) {
        return (Resolve-Path -LiteralPath $env:UNITY_EXE).Path
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

$BuildLog = Join-Path ([IO.Path]::GetTempPath()) ("unity-build-danmu-" + [Guid]::NewGuid().ToString("N") + ".log")
$RunLog = Join-Path ([IO.Path]::GetTempPath()) ("danmu-player-" + [Guid]::NewGuid().ToString("N") + ".log")

try {
    $ResolvedUnityExe = Resolve-UnityExe $UnityExe
    Write-Host "[BUILD] Unity: $ResolvedUnityExe"

    $unityArgs = @(
        "-batchmode",
        "-quit",
        "-projectPath", $Root,
        "-executeMethod", "ApocalypseKingSceneBuilder.BuildWindowsPlayer",
        "-logFile", $BuildLog
    )

    $buildProcess = Start-Process -FilePath $ResolvedUnityExe -ArgumentList $unityArgs -Wait -PassThru -WindowStyle Hidden
    $summary = Select-String -Path $BuildLog -Pattern "Build Finished|Build result|error CS|Exception" -ErrorAction SilentlyContinue
    $summary | ForEach-Object { Write-Host $_.Line }

    $buildSucceeded = Select-String -Path $BuildLog -Pattern "Build result:\s*Succeeded|Build Finished,\s*Result:\s*Success" -Quiet -ErrorAction SilentlyContinue
    if ($buildProcess.ExitCode -ne 0 -or -not $buildSucceeded) {
        throw "Unity build failed."
    }

    if (-not (Test-Path -LiteralPath $GameExe)) {
        throw "Build succeeded, but game executable was not found: $GameExe"
    }

    $arguments = @()
    if ($DisableDanmuHttp) {
        $arguments += "-danmuHttpOff"
    }
    elseif ($DanmuHttpPort -gt 0) {
        $arguments += "-danmuHttpPort"
        $arguments += $DanmuHttpPort.ToString([Globalization.CultureInfo]::InvariantCulture)
    }

    $arguments += "-logFile"
    $arguments += $RunLog

    if ($GameArgs) {
        $arguments += $GameArgs
    }

    Write-Host "[RUN] $GameExe"
    $gameProcess = Start-Process -FilePath $GameExe -WorkingDirectory $Root -ArgumentList $arguments -PassThru

    if (-not $KeepRunLog) {
        $escapedRunLog = $RunLog.Replace("'", "''")
        $cleanupCommand = "Wait-Process -Id $($gameProcess.Id); Remove-Item -LiteralPath '$escapedRunLog' -Force -ErrorAction SilentlyContinue"
        Start-Process -FilePath powershell -ArgumentList @("-NoProfile", "-Command", $cleanupCommand) -WindowStyle Hidden
    }

    Write-Host "[OK] Game started. PID: $($gameProcess.Id)"
    if ($KeepRunLog) {
        Write-Host "[LOG] Run log: $RunLog"
    }
}
finally {
    if (-not $KeepBuildLog -and (Test-Path -LiteralPath $BuildLog)) {
        Remove-Item -LiteralPath $BuildLog -Force -ErrorAction SilentlyContinue
    }

    if ($KeepBuildLog) {
        Write-Host "[LOG] Build log: $BuildLog"
    }
}

