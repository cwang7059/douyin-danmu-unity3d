# Pterosaur (Pteranodon) + rocket truck for Apocalypse King special units.
# Run from repo root: .\tools\import-apocalypse-special-units.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

& (Join-Path $PSScriptRoot "import-pterosaur-pteranodon.ps1")

$RocketTruckDir = Join-Path $Root "Assets\Resources\Vehicles\RocketTruck"
$KenneyCatapult = Join-Path $Root "Assets\Resources\Kenney\CastleKit\siege-catapult.glb"
$DownloadDir = Join-Path $Root "_downloads"

New-Item -ItemType Directory -Force -Path $DownloadDir, $RocketTruckDir | Out-Null

function Try-DownloadPoly {
    param([string]$Id, [string]$Dest)
    if ((Test-Path $Dest) -and (Get-Item $Dest).Length -gt 8192) { return $true }
    $tmp = Join-Path $DownloadDir "$Id.glb"
    foreach ($url in @(
        "https://static.poly.pizza/glTF/$Id.glb",
        "https://static.poly.pizza/$Id.glb"
    )) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 60
            if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 8192) {
                Copy-Item $tmp $Dest -Force
                return $true
            }
        }
        catch { }
    }
    return $false
}

$RocketTruckDest = Join-Path $RocketTruckDir "RocketTruck.glb"
if ((Test-Path $KenneyCatapult) -and (Get-Item $KenneyCatapult).Length -gt 4096) {
    Copy-Item $KenneyCatapult $RocketTruckDest -Force
    Write-Host "[Special] RocketTruck <- Kenney siege-catapult"
}
elseif (-not (Try-DownloadPoly -Id "lNGPW2NGPZ" -Dest $RocketTruckDest)) {
    Write-Host "[Special] Rocket truck: ensure Kenney CastleKit is imported."
}

Write-Host "[Special] Done."
