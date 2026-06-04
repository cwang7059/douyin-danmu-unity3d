# Pterosaur (Pteranodon) + rocket truck for Apocalypse King special units.
# Run from repo root: .\tools\import-apocalypse-special-units.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

& (Join-Path $PSScriptRoot "import-pterosaur-pteranodon.ps1")
& (Join-Path $PSScriptRoot "import-rocket-truck.ps1")

& (Join-Path $PSScriptRoot "import-rocket-projectile.ps1")

Write-Host "[Special] Done."
