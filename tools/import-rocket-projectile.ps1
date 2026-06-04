# Tactical rocket projectile mesh (CruiseMissile from nuclear pack) -> Resources/Projectiles/TacticalRocket
# License: same as project nuclear assets / OpenGameArt where applicable.
# Run from repo root: .\tools\import-rocket-projectile.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DestDir = Join-Path $Root "Assets\Resources\Projectiles\TacticalRocket"
New-Item -ItemType Directory -Force -Path $DestDir | Out-Null

$sources = @(
    (Join-Path $Root "Assets\Resources\Nuclear\CruiseMissile.glb"),
    (Join-Path $Root "Assets\StreamingAssets\Nuclear\CruiseMissile.glb"),
    (Join-Path $Root "Assets\Resources\Nuclear\TacticalMissile.fbx"),
    (Join-Path $Root "Assets\StreamingAssets\Nuclear\TacticalMissile.fbx")
)

$DestGlb = Join-Path $DestDir "TacticalRocket.glb"
$copied = $false
foreach ($src in $sources) {
    if ((Test-Path $src) -and (Get-Item $src).Length -gt 4096) {
        Copy-Item $src $DestGlb -Force
        Write-Host "[RocketProjectile] Installed $DestGlb <- $src"
        $copied = $true
        break
    }
}

if (-not $copied) {
    Write-Host "[RocketProjectile] WARNING: No CruiseMissile source found. Run tools/import-nuclear-warhead.ps1 first."
    exit 1
}

$texSrc = Join-Path $Root "Assets\Resources\Nuclear\Textures\missile01_Diff.png"
if (Test-Path $texSrc) {
    Copy-Item $texSrc (Join-Path $DestDir "missile01_Diff.png") -Force
    Write-Host "[RocketProjectile] Copied diffuse texture."
}

Write-Host "[RocketProjectile] Done. Reimport in Unity if needed."
