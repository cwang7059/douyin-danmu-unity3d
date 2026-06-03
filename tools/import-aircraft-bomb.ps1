# MK-82 500lb 航空炸弹（OpenGameArt GPL-2，Mike Hosker）→ Resources/AircraftBomb
# Run: .\tools\import-aircraft-bomb.ps1

param(
    [string]$ObjPath = "",
    [string]$TexturePath = "",
    [string]$UnityExe = "",
    [switch]$SkipUnitySetup
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
Set-Location $ProjectRoot

$OgaZipUrl = "https://opengameart.org/sites/default/files/ldgp.zip"
$DownloadZip = Join-Path $ProjectRoot "_downloads\ldgp.zip"
$DownloadDir = Join-Path $ProjectRoot "_downloads\ldgp-full"
$DestDir = Join-Path $ProjectRoot "Assets\Resources\AircraftBomb"

New-Item -ItemType Directory -Force -Path (Split-Path $DownloadZip), $DestDir | Out-Null

if ($ObjPath -and (Test-Path $ObjPath)) {
    Copy-Item $ObjPath (Join-Path $DestDir "Mk82Bomb.obj") -Force
}
else {
    if (-not (Test-Path $DownloadZip) -or (Get-Item $DownloadZip).Length -lt 50000) {
        Write-Host "[AircraftBomb] Downloading MK-82 bomb (OpenGameArt GPL-2)..."
        Invoke-WebRequest -Uri $OgaZipUrl -OutFile $DownloadZip -UseBasicParsing -TimeoutSec 120
    }
    if (Test-Path $DownloadDir) { Remove-Item $DownloadDir -Recurse -Force }
    Expand-Archive -Path $DownloadZip -DestinationPath $DownloadDir -Force
    $srcObj = Join-Path $DownloadDir "ldgp\mk82.obj"
    if (-not (Test-Path $srcObj)) { throw "mk82.obj not found in ldgp.zip" }
    Copy-Item $srcObj (Join-Path $DestDir "Mk82Bomb.obj") -Force
    $srcTex = Join-Path $DownloadDir "ldgp\skin.png"
    if (Test-Path $srcTex) {
        Copy-Item $srcTex (Join-Path $DestDir "skin.png") -Force
    }
}

if ($TexturePath -and (Test-Path $TexturePath)) {
    Copy-Item $TexturePath (Join-Path $DestDir "skin.png") -Force
}

$mtl = @"
newmtl Material_skin.png
Ka 0.2 0.2 0.2
Kd 0.8 0.8 0.8
map_Kd skin.png
"@
Set-Content -Path (Join-Path $DestDir "Mk82Bomb.mtl") -Value $mtl -Encoding UTF8

Write-Host "[AircraftBomb] Installed -> $DestDir"
Write-Host "[AircraftBomb] License: doc/许可证/OpenGameArt_MK82_LDGP_GPL2.md"

if (-not $SkipUnitySetup) {
    Write-Host "[AircraftBomb] Unity batch import + prefab bake..."
    & (Join-Path $Root "setup-unity-assets.ps1") -UnityExe $UnityExe
}
