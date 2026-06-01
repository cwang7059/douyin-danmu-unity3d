# Imports free CC0 assets for Apocalypse King (Kenney packs + VFX).
# Run from repo root: .\tools\import-apocalypse-assets.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

Write-Host "[Apocalypse] Importing VFX (Kenney particles)..."
& "$PSScriptRoot\import-free-vfx.ps1"

$ThirdParty = Join-Path $Root "Assets\ThirdParty\Kenney"
New-Item -ItemType Directory -Force -Path $ThirdParty | Out-Null

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
