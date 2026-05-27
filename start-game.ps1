param(
    [switch]$Probe,
    [switch]$ProbeDanmu,
    [int]$DanmuHttpPort = 8765,
    [switch]$DisableDanmuHttp,
    [double]$ProbeDelay = 2.0,
    [double]$ProbeTimeScale = 1.0,
    [string]$ProbeOutput = "preview-manual-probe.png",
    [string]$LogFile = "player-manual.log"
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

if (-not [string]::IsNullOrWhiteSpace($LogFile)) {
    $arguments += "-logFile"
    $arguments += (Join-Path $Root $LogFile)
}

Start-Process -FilePath $GameExe -WorkingDirectory $Root -ArgumentList $arguments
