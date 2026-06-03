# Imports free CC0 assets for Apocalypse King (Kenney packs + VFX).
# Run from repo root: .\tools\import-apocalypse-assets.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "[Apocalypse] Importing VFX (Kenney particles)..."
& "$PSScriptRoot\import-free-vfx.ps1"

Write-Host "[Apocalypse] Installing nuclear warhead mesh (Kenney Blaster Kit CC0)..."
& "$PSScriptRoot\import-nuclear-warhead.ps1"

Write-Host "[Apocalypse] Importing US Army tactical soldier..."
& "$PSScriptRoot\import-us-soldier.ps1"

Write-Host "[Apocalypse] Importing M14 rifle for soldiers..."
& "$PSScriptRoot\import-m14-weapon.ps1"

Write-Host "[Apocalypse] Importing zombie unit models (Quaternius / Poly Pizza)..."
& "$PSScriptRoot\import-zombie-units.ps1"

Write-Host "[Apocalypse] Installing Kenney Castle Kit (legacy fallback)..."
& "$PSScriptRoot\import-castle-environment.ps1"

Write-Host "[Apocalypse] Installing realistic castle (fortress GLB + stone PBR)..."
& "$PSScriptRoot\import-realistic-castle.ps1"

Write-Host "[Apocalypse] Installing realistic tank (optional; needs Sketchfab GLB — see doc/坦克写实素材选型与导入.md)..."
$ErrorActionPreference = "Continue"
& "$PSScriptRoot\import-realistic-tank.ps1"
if (-not $?) {
    Write-Host "[Apocalypse] Realistic tank skipped (place GLB in _downloads/realistic-tank or use -GlbPath)."
}
$ErrorActionPreference = "Stop"

$ThirdParty = Join-Path $Root "Assets\ThirdParty\Kenney"
New-Item -ItemType Directory -Force -Path $ThirdParty | Out-Null

$CastleZip = Join-Path $ThirdParty "kenney_castle-kit.zip"
$CastleExtract = Join-Path $ThirdParty "CastleKit"
$CastleResources = Join-Path $Root "Assets\Resources\Kenney\CastleKit"
if (-not (Test-Path $CastleZip)) {
    try {
        Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/kenney_castle-kit.zip" -OutFile $CastleZip -UseBasicParsing
        Write-Host "[Apocalypse] Downloaded Kenney Castle Kit (CC0)."
    }
    catch {
        Write-Host "[Apocalypse] Castle kit download skipped (manual: https://kenney.nl/assets/castle-kit)"
    }
}

if (Test-Path $CastleZip) {
    New-Item -ItemType Directory -Force -Path $CastleExtract | Out-Null
    Expand-Archive -Path $CastleZip -DestinationPath $CastleExtract -Force
    $glbSrc = Join-Path $CastleExtract "Models\GLB format"
    if (Test-Path $glbSrc) {
        New-Item -ItemType Directory -Force -Path $CastleResources | Out-Null
        @(
            "gate", "metal-gate", "wall", "wall-corner", "wall-pillar", "wall-doorway",
            "tower-square-base-border", "tower-square-mid-door", "tower-square-top-roof-high",
            "stairs-stone", "bridge-straight-pillar", "flag", "flag-pennant"
        ) | ForEach-Object {
            $file = Join-Path $glbSrc "$_.glb"
            if (Test-Path $file) { Copy-Item $file (Join-Path $CastleResources "$_.glb") -Force }
        }
        Write-Host "[Apocalypse] Castle GLB copied to Assets/Resources/Kenney/CastleKit"
    }
}

$Readme = @"
# Apocalypse King — Third-Party Unit Assets

Run ``tools/import-apocalypse-assets.ps1`` to refresh VFX.

## Manual downloads (CC0)

- Platformer Characters: https://kenney.nl/assets/platformer-characters
- Tank Pack: https://kenney.nl/assets/tank-pack
- City Kit Suburban: https://kenney.nl/assets/city-kit-suburban

Extract ZIP contents under this folder, then in Unity assign meshes to UnitConfig variants.

License: CC0 (Kenney.nl)
"@

Set-Content -Path (Join-Path $ThirdParty "README_APOCALYPSE_UNITS.md") -Value $Readme -Encoding UTF8
Write-Host "[Apocalypse] Wrote $ThirdParty\README_APOCALYPSE_UNITS.md"
Write-Host "[Apocalypse] Done. Import character/tank ZIPs manually if needed."
