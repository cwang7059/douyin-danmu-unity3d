# Downloads the Mixamo Vanguard tactical soldier (helmet, vest, rifle mesh) with Run/Idle/Walk.
# Run from repo root: .\tools\import-us-soldier.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads"
New-Item -ItemType Directory -Force -Path $DownloadDir | Out-Null

$SourceUrl = "https://threejs.org/examples/models/gltf/Soldier.glb"
$DownloadPath = Join-Path $DownloadDir "USArmySoldier_MixamoVanguard.glb"

if (-not (Test-Path $DownloadPath)) {
    Write-Host "[Soldier] Downloading tactical soldier GLB..."
    Invoke-WebRequest -Uri $SourceUrl -OutFile $DownloadPath -UseBasicParsing
}

$ResourcesDir = Join-Path $Root "Assets\Resources\Soldiers\USArmyTacticalVanguard"
$StreamingDir = Join-Path $Root "Assets\StreamingAssets\Soldiers"
New-Item -ItemType Directory -Force -Path $ResourcesDir, $StreamingDir | Out-Null

Copy-Item $DownloadPath (Join-Path $ResourcesDir "USArmySoldier.glb") -Force
Copy-Item $DownloadPath (Join-Path $StreamingDir "us_army_soldier.glb") -Force

Write-Host "[Soldier] Installed to Resources and StreamingAssets."
Write-Host "[Soldier] Reimport in Unity (or run setup-unity-assets.ps1) before building."
