param(
    [switch]$Probe,
    [switch]$ProbeDanmu,
    [int]$DanmuHttpPort = 8765,
    [switch]$DisableDanmuHttp,
    [double]$ProbeDelay = 2.0,
    [double]$ProbeTimeScale = 1.0,
    [string]$ProbeOutput = "preview-manual-probe.png",
    [string]$LogFile = ""
)

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameExe = Join-Path $Root "Builds\Windows\ApocalypseKingUnity3D.exe"

if (-not (Test-Path -LiteralPath $GameExe)) {
    Write-Host "[ERROR] Cannot find game executable:" -ForegroundColor Red
    Write-Host $GameExe
    Write-Host ""
    Write-Host "Please build it from Unity first: Apocalypse King / Build Windows Player"
    exit 1
}

$arguments = @()
if ($Probe) {
    $arguments += "-apocalypseProbe"
    $arguments += "-probeDelay"
    $arguments += $ProbeDelay.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)
    $arguments += "-probeTimeScale"
    $arguments += $ProbeTimeScale.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture)
    $arguments += "-probeOutput"
    $arguments += (Join-Path $Root $ProbeOutput)
}

if ($ProbeDanmu) {
    $arguments += "-probeDanmu"
}

if ($DisableDanmuHttp) {
    $arguments += "-danmuHttpOff"
}
elseif ($DanmuHttpPort -gt 0) {
    $arguments += "-danmuHttpPort"
    $arguments += $DanmuHttpPort.ToString([Globalization.CultureInfo]::InvariantCulture)
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = Join-Path ([IO.Path]::GetTempPath()) ("danmu-player-" + [Guid]::NewGuid().ToString("N") + ".log")
    $arguments += "-logFile"
    $arguments += $LogFile
    $deleteLogWhenDone = $true
}
else {
    $arguments += "-logFile"
    $arguments += $(if ([IO.Path]::IsPathRooted($LogFile)) { $LogFile } else { Join-Path $Root $LogFile })
    $deleteLogWhenDone = $false
}

$process = Start-Process -FilePath $GameExe -WorkingDirectory $Root -ArgumentList $arguments -PassThru

if ($deleteLogWhenDone) {
    $escapedLogFile = $LogFile.Replace("'", "''")
    $cleanupCommand = "Wait-Process -Id $($process.Id); Remove-Item -LiteralPath '$escapedLogFile' -Force -ErrorAction SilentlyContinue"
    Start-Process -FilePath powershell -ArgumentList @("-NoProfile", "-Command", $cleanupCommand) -WindowStyle Hidden
}
