# Installs Kenney Castle Kit (CC0) for faction bases — stone fortress with keep, walls, gate towers.
# Run: .\tools\import-castle-environment.ps1
# Guide: doc/城堡素材选型与导入.md

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$CastleZipUrl = "https://opengameart.org/sites/default/files/kenney_castle-kit.zip"
$CastleZip = Join-Path $Root "_downloads\kenney_castle-kit.zip"
$CastleExtract = Join-Path $Root "_downloads\kenney_castle-kit-extract"
$CastleResources = Join-Path $Root "Assets\Resources\Kenney\CastleKit"
$CastleThirdParty = Join-Path $Root "Assets\ThirdParty\Kenney\CastleKit"

New-Item -ItemType Directory -Force -Path (Split-Path $CastleZip), $CastleResources | Out-Null

if (-not (Test-Path $CastleZip)) {
    Write-Host "[Castle] Downloading Kenney Castle Kit (CC0)..."
    Invoke-WebRequest -Uri $CastleZipUrl -OutFile $CastleZip -UseBasicParsing -TimeoutSec 120
}

if (Test-Path $CastleExtract) { Remove-Item $CastleExtract -Recurse -Force }
Expand-Archive -Path $CastleZip -DestinationPath $CastleExtract -Force

$glbSrc = Get-ChildItem -Path $CastleExtract -Recurse -Filter "*.glb" -File | Where-Object { $_.DirectoryName -match "GLB" }
if (-not $glbSrc) {
    $glbSrc = Get-ChildItem -Path $CastleExtract -Recurse -Filter "*.glb" -File
}

$copied = 0
foreach ($file in $glbSrc) {
    $dest = Join-Path $CastleResources $file.Name
    Copy-Item $file.FullName $dest -Force
    $copied++
}

Write-Host "[Castle] Copied $copied GLB -> $CastleResources"
Write-Host "[Castle] Open Unity: Apocalypse King > Bake Castle Kit Prefabs (or batch build)."
Write-Host "[Castle] License: CC0 https://kenney.nl/assets/castle-kit"
