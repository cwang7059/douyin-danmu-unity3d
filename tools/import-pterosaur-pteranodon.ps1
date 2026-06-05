# Pteranodon (pterosaur special unit) — download + install to Resources.
# Run from repo root:
#   .\tools\import-pterosaur-pteranodon.ps1
#   .\tools\import-pterosaur-pteranodon.ps1 -GlbPath "D:\Downloads\scene.glb"
#   $env:SKETCHFAB_API_TOKEN = "..." ; .\tools\import-pterosaur-pteranodon.ps1
#
# Primary (CC-BY): JW Primal Ops Pteranodon
#   https://sketchfab.com/3d-models/jw-primal-ops-pteranodon-c9d423e6e27d4334963c6abe86bbf85d
# Alternate animated (CC-BY): Pteranodon (Animated)
#   https://sketchfab.com/3d-models/pteranodon-animated-7d7683df41d1405283f160e81a5dff1b

param(
    [string]$GlbPath = "",
    [string]$SourceDir = "",
    [string]$SketchfabUid = "c9d423e6e27d4334963c6abe86bbf85d",
    [switch]$PreferAnimated,
    [switch]$PreferStatic
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads\pterosaur"
$PterosaurDir = Join-Path $Root "Assets\Resources\Monsters\Pterosaur"
$ManualDrop = Join-Path $PterosaurDir "Pteranodon.glb"
$PterosaurDest = Join-Path $PterosaurDir "Pterosaur.glb"
$IncomingDrop = Join-Path $PterosaurDir "Incoming\Pteranodon.glb"
$AnimatedUid = "7d7683df41d1405283f160e81a5dff1b"
$OgaZipUrl = "https://opengameart.org/sites/default/files/low_spec_pterosaur_oga.zip"
$OgaZip = Join-Path $Root "_downloads\low_spec_pterosaur_oga.zip"

New-Item -ItemType Directory -Force -Path $DownloadDir, $PterosaurDir, (Split-Path $IncomingDrop) | Out-Null

function Test-GlbReady {
    param([string]$Path)
    return (Test-Path $Path) -and (Get-Item $Path).Length -gt 32768
}

function Install-Glb {
    param([string]$Source, [string]$Label)
    if (-not (Test-GlbReady $Source)) { return $false }
    Copy-Item $Source $ManualDrop -Force
    Copy-Item $Source $PterosaurDest -Force
    Export-PterosaurTexturesFromGlb $ManualDrop
    $bytes = (Get-Item $ManualDrop).Length
    Write-Host "[Pteranodon] Installed $Label ($bytes bytes)"
    Write-Host "  -> Assets/Resources/Monsters/Pterosaur/Pteranodon.glb"
    Write-Host "  -> Assets/Resources/Monsters/Pterosaur/Pterosaur.glb"
    return $true
}

function Export-PterosaurTexturesFromGlb {
    param([string]$GlbPath)
    $texDir = Join-Path $Root "Assets\Resources\Monsters\Pterosaur\Textures"
    $extractScript = Join-Path $DownloadDir "extract_pterosaur_textures.py"
    @"
import struct, json
from pathlib import Path
glb = Path(r'$($GlbPath.Replace("'", "''"))')
out = Path(r'$($texDir.Replace("'", "''"))')
out.mkdir(parents=True, exist_ok=True)
with open(glb, 'rb') as f:
    f.read(12)
    jl, jt = struct.unpack('<I4s', f.read(8))
    json_data = f.read(jl)
    bl, bt = struct.unpack('<I4s', f.read(8))
    bin_data = f.read(bl)
j = json.loads(json_data.decode())
for i, img in enumerate(j.get('images', [])):
    bv = img.get('bufferView')
    if bv is None:
        continue
    bv_info = j['bufferViews'][bv]
    start = bv_info.get('byteOffset', 0)
    length = bv_info['byteLength']
    chunk = bin_data[start:start + length]
    ext = 'png' if 'png' in img.get('mimeType', '') else 'jpg'
    (out / ('Pteranodon_%d.%s' % (i, ext))).write_bytes(chunk)
print('textures exported to', out)
"@ | Set-Content -Path $extractScript -Encoding UTF8
    python $extractScript 2>&1 | Out-Host
}

function Try-DownloadSketchfabGlb {
    param([string]$Uid, [string]$Tag)
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-$Uid.glb"
    try {
        Write-Host "[Pteranodon] Sketchfab API ($Tag) uid=$Uid ..."
        $headers = @{ Authorization = "Token $token" }
        $meta = Invoke-RestMethod -Uri "https://api.sketchfab.com/v3/models/$Uid/download" -Headers $headers -UseBasicParsing
        $url = $null
        if ($meta.glb -and $meta.glb.url) { $url = $meta.glb.url }
        elseif ($meta.gltf -and $meta.gltf.url) { $url = $meta.gltf.url }
        if (-not $url) { return $null }
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 240
        if (Test-GlbReady $tmp) { return $tmp }
    }
    catch {
        Write-Host "[Pteranodon] Sketchfab API skipped ($Tag): $($_.Exception.Message)"
    }
    return $null
}

# --- explicit file ---
if ($GlbPath -and (Test-Path $GlbPath)) {
    if (Install-Glb $GlbPath "manual -GlbPath") { exit 0 }
}

if ($SourceDir -and (Test-Path $SourceDir)) {
    $pick = Get-ChildItem -Path $SourceDir -Recurse -Filter "*.glb" -File -ErrorAction SilentlyContinue |
        Sort-Object Length -Descending | Select-Object -First 1
    if ($pick -and (Install-Glb $pick.FullName "manual -SourceDir")) { exit 0 }
}

# --- incoming drop folder ---
if (Test-GlbReady $IncomingDrop) {
    if (Install-Glb $IncomingDrop "Incoming/Pteranodon.glb") { exit 0 }
}

$dropCandidates = @(
    (Join-Path $DownloadDir "Pteranodon.glb"),
    (Join-Path $DownloadDir "pteranodon.glb"),
    (Join-Path $Root "_downloads\Pteranodon.glb")
)
foreach ($c in $dropCandidates) {
    if (Test-GlbReady $c) {
        if (Install-Glb $c "downloads folder") { exit 0 }
    }
}

# --- Sketchfab API (default: animated wing-flap model first) ---
$preferAnimatedFirst = $PreferAnimated -or -not $PreferStatic
$uidOrder = if ($preferAnimatedFirst) { @($AnimatedUid, $SketchfabUid) } else { @($SketchfabUid, $AnimatedUid) }
foreach ($uid in $uidOrder) {
    $tag = if ($uid -eq $AnimatedUid) { "Pteranodon Animated" } else { "JW Primal Ops" }
    $glb = Try-DownloadSketchfabGlb -Uid $uid -Tag $tag
    if ($glb -and (Install-Glb $glb "Sketchfab $tag")) { exit 0 }
}

# --- keep existing Resources copy ---
if (Test-GlbReady $ManualDrop) {
    Copy-Item $ManualDrop $PterosaurDest -Force
    Write-Host "[Pteranodon] Keeping existing Pteranodon.glb ($((Get-Item $ManualDrop).Length) bytes)"
    exit 0
}

# --- OpenGameArt + Blender ---
if (-not (Test-Path $OgaZip) -or (Get-Item $OgaZip).Length -lt 4096) {
    Write-Host "[Pteranodon] Downloading OpenGameArt low-spec pterosaur (CC0)..."
    Invoke-WebRequest -Uri $OgaZipUrl -OutFile $OgaZip -UseBasicParsing -TimeoutSec 120
}

$blender = Get-Command blender -ErrorAction SilentlyContinue
if ($blender -and (Test-Path $OgaZip)) {
    $extract = Join-Path $DownloadDir "oga-export"
    if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
    Expand-Archive -Path $OgaZip -DestinationPath $extract -Force
    $blend = Get-ChildItem -Path $extract -Recurse -Filter "*.blend" | Select-Object -First 1
    if ($blend) {
        $exportGlb = Join-Path $DownloadDir "oga_pterosaur.glb"
        $py = Join-Path $DownloadDir "export_oga.py"
        @"
import bpy
bpy.ops.wm.open_mainfile(filepath=r'$($blend.FullName)')
bpy.ops.export_scene.gltf(filepath=r'$exportGlb', export_format='GLB')
"@ | Set-Content -Path $py -Encoding UTF8
        Write-Host "[Pteranodon] Blender export from OGA..."
        & $blender.Source --background --python $py 2>&1 | Out-Host
        if (Test-GlbReady $exportGlb) {
            if (Install-Glb $exportGlb "OpenGameArt CC0") { exit 0 }
        }
    }
}

# --- procedural fallback ---
Write-Host "[Pteranodon] Generating procedural GLB..."
python (Join-Path $PSScriptRoot "generate-pteranodon-glb.py")

Write-Host ""
Write-Host "[Pteranodon] For your custom fierce model (black/red wings):"
Write-Host "  Save GLB to: Assets/Resources/Monsters/Pterosaur/Incoming/Pteranodon.glb"
Write-Host "  Then re-run: .\tools\import-pterosaur-pteranodon.ps1"
Write-Host ""
Write-Host "[Pteranodon] Or set SKETCHFAB_API_TOKEN and re-run for auto download."
Write-Host "  JW Primal Ops: https://sketchfab.com/3d-models/jw-primal-ops-pteranodon-c9d423e6e27d4334963c6abe86bbf85d"
Write-Host "[Pteranodon] Unity: Reimport Monsters/Pterosaur, Stop -> Play."
