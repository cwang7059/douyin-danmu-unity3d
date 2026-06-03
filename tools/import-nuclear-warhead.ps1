# Realistic cruise missile for nuclear strike: AGM-114 Hellfire GLB (CC-BY, GitHub mirror) + Cherry Blast FBX fallback.
# Run: .\tools\import-nuclear-warhead.ps1
# Optional: $env:SKETCHFAB_API_TOKEN or -GlbPath for Tomahawk override.

param(
    [string]$UnityExe = "",
    [string]$GlbPath = "",
    [string]$FbxPath = "",
    [switch]$SkipUnitySetup
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
Set-Location $ProjectRoot

$DownloadDir = Join-Path $ProjectRoot "_downloads\nuclear-missile"
$CherryZip = Join-Path $ProjectRoot "_downloads\cherry-blast-missile.zip"
$CherryExtract = Join-Path $ProjectRoot "_downloads\cherry-blast"
$ResourcesDir = Join-Path $ProjectRoot "Assets\Resources\Nuclear"
$StreamingDir = Join-Path $ProjectRoot "Assets\StreamingAssets\Nuclear"
$TexDir = Join-Path $ResourcesDir "Textures"
$OgaUrl = "https://opengameart.org/sites/default/files/Cherry_Blast.zip"
$HellfireGlbUrl = "https://github.com/ku6ryo/GuidedMissile/raw/master/Assets/missile.glb"
$SketchfabTomahawkUid = "7f2cda9c50864d8ab136a63659ca0658"
$PrimaryGlbName = "CruiseMissile.glb"
$FallbackFbxName = "TacticalMissile.fbx"

New-Item -ItemType Directory -Force -Path $DownloadDir, $ResourcesDir, $StreamingDir, $TexDir | Out-Null

function Install-MissileGlb {
    param([string]$Source, [string]$Label)
    if (-not (Test-Path $Source)) { return $false }
    $destRes = Join-Path $ResourcesDir $PrimaryGlbName
    $destStream = Join-Path $StreamingDir $PrimaryGlbName
    Copy-Item $Source $destRes -Force
    Copy-Item $Source $destStream -Force
    Write-Host "[Nuclear] Installed $Label -> $destRes"
    return $true
}

function Install-MissileFbx {
    param([string]$Source, [string]$Diffuse, [string]$Ao)
    if (-not (Test-Path $Source)) { return $false }
    Copy-Item $Source (Join-Path $ResourcesDir $FallbackFbxName) -Force
    Copy-Item $Source (Join-Path $StreamingDir $FallbackFbxName) -Force
    if ($Diffuse -and (Test-Path $Diffuse)) {
        Copy-Item $Diffuse (Join-Path $TexDir "missile01_Diff.png") -Force
    }
    if ($Ao -and (Test-Path $Ao)) {
        Copy-Item $Ao (Join-Path $TexDir "AO.png") -Force
    }
    Write-Host "[Nuclear] Installed fallback FBX -> Assets/Resources/Nuclear/$FallbackFbxName"
    return $true
}

function Try-DownloadHellfireGlb {
    $tmp = Join-Path $DownloadDir "hellfire-missile.glb"
    if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 500000) {
        return $tmp
    }
    try {
        Write-Host "[Nuclear] Downloading AGM-114 Hellfire GLB (CC-BY xephoney / Sketchfab mirror)..."
        Invoke-WebRequest -Uri $HellfireGlbUrl -OutFile $tmp -UseBasicParsing -TimeoutSec 180
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 500000) { return $tmp }
    }
    catch {
        Write-Host "[Nuclear] Hellfire download failed: $($_.Exception.Message)"
    }
    return $null
}

function Try-DownloadSketchfabTomahawkGlb {
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-tomahawk.glb"
    try {
        Write-Host "[Nuclear] Sketchfab API: Tomahawk (optional override)..."
        $headers = @{ Authorization = "Token $token" }
        $meta = Invoke-RestMethod -Uri "https://api.sketchfab.com/v3/models/$SketchfabTomahawkUid/download" -Headers $headers -UseBasicParsing
        $url = $null
        if ($meta.glb -and $meta.glb.url) { $url = $meta.glb.url }
        elseif ($meta.gltf -and $meta.gltf.url) { $url = $meta.gltf.url }
        if (-not $url) { return $null }
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 180
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 4096) { return $tmp }
    }
    catch {
        Write-Host "[Nuclear] Sketchfab API skipped: $($_.Exception.Message)"
    }
    return $null
}

function Ensure-CherryBlastAssets {
    New-Item -ItemType Directory -Force -Path (Split-Path $CherryZip) | Out-Null
    if (-not (Test-Path $CherryZip) -or (Get-Item $CherryZip).Length -lt 100000) {
        Write-Host "[Nuclear] Downloading Cherry Blast fallback (CC-BY PolygonDan)..."
        Invoke-WebRequest -Uri $OgaUrl -OutFile $CherryZip -UseBasicParsing -TimeoutSec 120
    }
    if (-not (Test-Path (Join-Path $CherryExtract "obj fbx\missile02.fbx"))) {
        if (Test-Path $CherryExtract) { Remove-Item $CherryExtract -Recurse -Force }
        Expand-Archive -Path $CherryZip -DestinationPath $CherryExtract -Force
    }
    return @{
        Fbx = Join-Path $CherryExtract "obj fbx\missile02.fbx"
        Diff = Join-Path $CherryExtract "Texture\missile01_Diff.png"
        Ao = Join-Path $CherryExtract "Texture\AO.png"
    }
}

$installedRealistic = $false
$installedFallback = $false
$realisticLabel = ""

if ($GlbPath -and (Test-Path $GlbPath)) {
    $installedRealistic = Install-MissileGlb -Source $GlbPath -Label "custom GLB"
    $realisticLabel = "custom GLB"
}
elseif ($FbxPath -and (Test-Path $FbxPath)) {
    $installedFallback = Install-MissileFbx -Source $FbxPath -Diffuse "" -Ao ""
}
else {
    $tomahawk = Try-DownloadSketchfabTomahawkGlb
    if ($tomahawk) {
        $installedRealistic = Install-MissileGlb -Source $tomahawk -Label "Sketchfab Tomahawk"
        $realisticLabel = "Tomahawk (Sketchfab)"
    }

    if (-not $installedRealistic) {
        $hellfire = Try-DownloadHellfireGlb
        if ($hellfire) {
            $installedRealistic = Install-MissileGlb -Source $hellfire -Label "AGM-114 Hellfire"
            $realisticLabel = "Hellfire (CC-BY xephoney)"
        }
    }

    if (-not $installedRealistic) {
        $cached = Get-ChildItem -Path $DownloadDir -Recurse -Filter "*.glb" -File -ErrorAction SilentlyContinue |
            Sort-Object Length -Descending | Select-Object -First 1
        if ($cached -and $cached.Length -gt 500000) {
            $installedRealistic = Install-MissileGlb -Source $cached.FullName -Label "cached GLB"
            $realisticLabel = "cached GLB"
        }
    }

    $cherry = Ensure-CherryBlastAssets
    if (Test-Path $cherry.Fbx) {
        $installedFallback = Install-MissileFbx -Source $cherry.Fbx -Diffuse $cherry.Diff -Ao $cherry.Ao
    }
}

if (-not $installedRealistic -and -not $installedFallback) {
    throw "[Nuclear] No missile model installed."
}

if ($installedRealistic) {
    Write-Host "[Nuclear] Primary realistic model: $realisticLabel"
    Write-Host "[Nuclear] License: doc/许可证/Sketchfab_Hellfire_CC-BY.md (Hellfire) or Tomahawk doc if overridden"
}
if ($installedFallback) {
    Write-Host "[Nuclear] Fallback: Cherry Blast FBX + textures (CC-BY PolygonDan)"
}

Write-Host "[Nuclear] Unity batch import + prefab bake..."
& (Join-Path $Root "setup-unity-assets.ps1") -UnityExe $UnityExe

Write-Host "[Nuclear] Done."
