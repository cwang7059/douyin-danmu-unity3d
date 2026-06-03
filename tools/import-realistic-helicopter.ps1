# Realistic helicopter for UnitKind.Aircraft (replaces KumaSousa low-poly when present).
# Sources (in order): -GlbPath / -FbxPath, Sketchfab API (CC-BY UH-60), bundled OpenGameArt Attack Chopper.
# Run: .\tools\import-realistic-helicopter.ps1
#      .\tools\import-realistic-helicopter.ps1 -GlbPath "D:\Downloads\scene.glb"

param(
    [string]$GlbPath = "",
    [string]$FbxPath = "",
    [string]$SourceDir = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads\realistic-helicopter"
$OgaZip = Join-Path $Root "_downloads\attackchopper.zip"
$OgaUrl = "https://opengameart.org/sites/default/files/attackchopper.zip"
$ResourcesDir = Join-Path $Root "Assets\Resources\RealisticAircraft"
$StreamingDir = Join-Path $Root "Assets\StreamingAssets\RealisticAircraft"
$SketchfabUid = "172e790b08ec436da02b0816cd0a09e1"

New-Item -ItemType Directory -Force -Path $DownloadDir, $ResourcesDir, $StreamingDir | Out-Null

function Install-HelicopterMesh {
    param([string]$Source)
    if (-not (Test-Path $Source)) { return $false }
    $ext = [System.IO.Path]::GetExtension($Source).ToLowerInvariant()
    if ($ext -ne ".glb" -and $ext -ne ".fbx") { return $false }
    $destName = "BlackHawk$ext"
    $dest = Join-Path $ResourcesDir $destName
    $stream = Join-Path $StreamingDir $destName
    Copy-Item $Source $dest -Force
    Copy-Item $Source $stream -Force
    Write-Host "[Helicopter] $destName <- $Source"
    return $true
}

function Install-AttackChopperBundle {
    if (-not (Test-Path $OgaZip) -or (Get-Item $OgaZip).Length -lt 1000000) {
        Write-Host "[Helicopter] Downloading Attack Chopper (OpenGameArt / Cheese Animal)..."
        Invoke-WebRequest -Uri $OgaUrl -OutFile $OgaZip -UseBasicParsing -TimeoutSec 180
    }

    python -c @"
import os, shutil, zipfile
root = r'$($Root -replace "\\","\\\\")'
zip_path = r'$($OgaZip -replace "\\","\\\\")'
res = r'$($ResourcesDir -replace "\\","\\\\")'
stream = r'$($StreamingDir -replace "\\","\\\\")'
os.makedirs(res, exist_ok=True)
os.makedirs(stream, exist_ok=True)
with zipfile.ZipFile(zip_path) as z:
    z.extract('FBX/Attack_Helicopter.fbx', root + '/_downloads/realistic-helicopter')
    for name in z.namelist():
        if name.startswith('Textures/') and name.lower().endswith('.png'):
            z.extract(name, root + '/_downloads/realistic-helicopter')
fbx_src = os.path.join(root, '_downloads', 'realistic-helicopter', 'FBX', 'Attack_Helicopter.fbx')
fbx_dst = os.path.join(res, 'BlackHawk.fbx')
shutil.copy2(fbx_src, fbx_dst)
shutil.copy2(fbx_src, os.path.join(stream, 'BlackHawk.fbx'))
tex_src = os.path.join(root, '_downloads', 'realistic-helicopter', 'Textures')
for fn in os.listdir(tex_src):
    if not fn.lower().endswith('.png'):
        continue
    shutil.copy2(os.path.join(tex_src, fn), os.path.join(res, fn))
    shutil.copy2(os.path.join(tex_src, fn), os.path.join(stream, fn))
print('[Helicopter] BlackHawk.fbx + textures from Attack Chopper')
"@ | Out-Null

    return (Test-Path (Join-Path $ResourcesDir "BlackHawk.fbx"))
}

function Try-DownloadSketchfabGlb {
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-blackhawk.glb"
    try {
        Write-Host "[Helicopter] Sketchfab API download (UH-60 Black Hawk, CC-BY)..."
        $headers = @{ Authorization = "Token $token" }
        $meta = Invoke-RestMethod -Uri "https://api.sketchfab.com/v3/models/$SketchfabUid/download" -Headers $headers -UseBasicParsing
        $url = $null
        if ($meta.glb -and $meta.glb.url) { $url = $meta.glb.url }
        elseif ($meta.gltf -and $meta.gltf.url) { $url = $meta.gltf.url }
        if (-not $url) { return $null }
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 180
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 65536) { return $tmp }
    }
    catch {
        Write-Host "[Helicopter] Sketchfab API skipped: $($_.Exception.Message)"
    }
    return $null
}

$installed = $false

if ($GlbPath -and (Test-Path $GlbPath)) {
    $installed = Install-HelicopterMesh -Source $GlbPath
}
elseif ($FbxPath -and (Test-Path $FbxPath)) {
    $installed = Install-HelicopterMesh -Source $FbxPath
}
elseif ($SourceDir -and (Test-Path $SourceDir)) {
    $pick = Get-ChildItem -Path $SourceDir -Recurse -Include *.glb,*.fbx -File | Sort-Object Length -Descending | Select-Object -First 1
    if ($pick) { $installed = Install-HelicopterMesh -Source $pick.FullName }
}
else {
    $glb = Try-DownloadSketchfabGlb
    if ($glb) { $installed = Install-HelicopterMesh -Source $glb }

    if (-not $installed) {
        $pick = Get-ChildItem -Path $DownloadDir -Recurse -Include *.glb,*.fbx -File -ErrorAction SilentlyContinue |
            Sort-Object Length -Descending | Select-Object -First 1
        if ($pick) { $installed = Install-HelicopterMesh -Source $pick.FullName }
    }

    if (-not $installed) {
        $installed = Install-AttackChopperBundle
    }
}

if (-not $installed) {
    Write-Host ""
    Write-Host "[Helicopter] No helicopter model installed."
    Write-Host "  .\tools\import-realistic-helicopter.ps1 -GlbPath `"path\to\uh60.glb`""
    Write-Host "  Optional: set SKETCHFAB_API_TOKEN for Sketchfab UH-60 (CC-BY)."
    exit 1
}

Write-Host "[Helicopter] Installed to $ResourcesDir"
Write-Host "[Helicopter] Game prefers RealisticAircraft over KumaSousa LowPolyHelicopter."
Write-Host "[Helicopter] Unity: Reimport RealisticAircraft, then Stop/Play."
