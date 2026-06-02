# Build + run automated tank visual probe; exits non-zero if tanks fail acceptance checks.
param(
    [double]$ProbeDelay = 4.0,
    [string]$ProbeOutput = "artifacts\tank-visual-probe.png",
    [string]$LogFile = "artifacts\tank-visual-probe.log"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$artifacts = Join-Path $Root "artifacts"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$probePng = if ([IO.Path]::IsPathRooted($ProbeOutput)) { $ProbeOutput } else { Join-Path $Root $ProbeOutput }
$probeLog = if ([IO.Path]::IsPathRooted($LogFile)) { $LogFile } else { Join-Path $Root $LogFile }
if (Test-Path $probeLog) { Remove-Item $probeLog -Force }
if (Test-Path $probePng) { Remove-Item $probePng -Force }

Write-Host "[Probe] Building Windows player..."
& "$Root\build-and-start.ps1" -BuildOnly -KeepBuildLog
$GameExe = Join-Path $Root "Builds\Windows\ApocalypseKingUnity3D.exe"
if (-not (Test-Path -LiteralPath $GameExe)) { throw "Build failed: missing $GameExe" }
$args = @(
    "-apocalypseProbe",
    "-danmuHttpOff",
    "-danmuWsOff",
    "-probeDelay", $ProbeDelay.ToString("0.###", [Globalization.CultureInfo]::InvariantCulture),
    "-probeOutput", $probePng,
    "-logFile", $probeLog
)

Write-Host "[Probe] Running visual check..."
$proc = Start-Process -FilePath $GameExe -WorkingDirectory $Root -ArgumentList $args -PassThru -Wait
if (-not (Test-Path $probeLog)) { throw "Probe log not created: $probeLog" }

$probeLine = Select-String -Path $probeLog -Pattern "\[ApocalypseProbe\]" | Select-Object -Last 1
if ($probeLine) { Write-Host $probeLine.Line }

if (-not (Test-Path $probePng)) { throw "Screenshot missing: $probePng" }
$pngKb = [math]::Round((Get-Item $probePng).Length / 1KB, 1)
Write-Host "[Probe] Screenshot: $probePng ($pngKb KB)"

if ($proc.ExitCode -ne 0) {
    Write-Host "[Probe] FAILED (exit $($proc.ExitCode)). See log: $probeLog" -ForegroundColor Red
    exit $proc.ExitCode
}

Write-Host "[Probe] PASSED tank visual checks." -ForegroundColor Green
exit 0
