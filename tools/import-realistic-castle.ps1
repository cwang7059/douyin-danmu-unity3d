# Installs realistic faction castle: cohesive fortress GLB + Poly Haven stone PBR (CC0).
# Run: .\tools\import-realistic-castle.ps1
#      .\tools\import-realistic-castle.ps1 -GlbPath "C:\Downloads\medieval_castle.glb"
# Sketchfab (CC-BY): set SKETCHFAB_API_TOKEN and use -SketchfabUid or default OwenEarlNC castle.

param(
    [string]$GlbPath = "",
    [string]$FbxPath = "",
    [string]$SketchfabUid = "e21268ab4c644a439c6752a33c08cdec"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads\realistic-castle"
$ResourcesDir = Join-Path $Root "Assets\Resources\RealisticCastles"
$TexDir = Join-Path $ResourcesDir "Textures"
$FortressName = "CastleFortress"

New-Item -ItemType Directory -Force -Path $DownloadDir, $ResourcesDir, $TexDir | Out-Null

function Install-FortressFile {
    param([string]$Source)
    if (-not (Test-Path $Source)) { return $false }
    $ext = [System.IO.Path]::GetExtension($Source).ToLowerInvariant()
    if ($ext -ne ".glb" -and $ext -ne ".gltf" -and $ext -ne ".fbx") { return $false }
    $dest = Join-Path $ResourcesDir "$FortressName$ext"
    Copy-Item $Source $dest -Force
    Write-Host "[Castle] $FortressName$ext <- $Source"
    return $true
}

function Install-StoneTextures {
    $maps = @(
        @{
            Url  = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/castle_brick_01/castle_brick_01_diff_1k.jpg"
            Dest = "castle_brick_diff.jpg"
        },
        @{
            Url  = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/castle_brick_01/castle_brick_01_nor_gl_1k.jpg"
            Dest = "castle_brick_nrm.jpg"
        },
        @{
            Url  = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/castle_brick_01/castle_brick_01_rough_1k.jpg"
            Dest = "castle_brick_rough.jpg"
        },
        @{
            Url  = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rock_wall_10/rock_wall_10_diff_1k.jpg"
            Dest = "rock_wall_diff.jpg"
        },
        @{
            Url  = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rock_wall_10/rock_wall_10_nor_gl_1k.jpg"
            Dest = "rock_wall_nrm.jpg"
        }
    )

    foreach ($map in $maps) {
        $out = Join-Path $TexDir $map.Dest
        try {
            Invoke-WebRequest -Uri $map.Url -OutFile $out -UseBasicParsing -TimeoutSec 120
            Write-Host "[Castle] texture $($map.Dest) OK"
        }
        catch {
            Write-Host "[Castle] texture $($map.Dest) failed: $($_.Exception.Message)"
        }
    }
}

function Try-DownloadSketchfabCastleGlb {
    param([string]$Uid)
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-castle.glb"
    try {
        Write-Host "[Castle] Sketchfab API download ($Uid)..."
        $headers = @{ Authorization = "Token $token" }
        $meta = Invoke-RestMethod -Uri "https://api.sketchfab.com/v3/models/$Uid/download" -Headers $headers -UseBasicParsing
        $url = $null
        if ($meta.glb -and $meta.glb.url) { $url = $meta.glb.url }
        elseif ($meta.gltf -and $meta.gltf.url) { $url = $meta.gltf.url }
        if (-not $url) { return $null }
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 240
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 65536) { return $tmp }
    }
    catch {
        Write-Host "[Castle] Sketchfab API skipped: $($_.Exception.Message)"
    }
    return $null
}

function Try-DownloadQuaterniusCastleGlb {
    $url = "https://static.poly.pizza/dafe12fb-8ec0-4d2b-826a-4917d7ed78a3.glb"
    $tmp = Join-Path $DownloadDir "quaternius-castle.glb"
    try {
        Write-Host "[Castle] Downloading Quaternius Castle (CC0 via poly.pizza)..."
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 120
        if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 32768) { return $tmp }
    }
    catch {
        Write-Host "[Castle] Quaternius Castle download failed: $($_.Exception.Message)"
    }
    return $null
}

$installed = $false
Install-StoneTextures

if ($GlbPath -and (Test-Path $GlbPath)) {
    $installed = Install-FortressFile -Source $GlbPath
}
elseif ($FbxPath -and (Test-Path $FbxPath)) {
    $installed = Install-FortressFile -Source $FbxPath
}
else {
    $sf = Try-DownloadSketchfabCastleGlb -Uid $SketchfabUid
    if ($sf) { $installed = Install-FortressFile -Source $sf }
    if (-not $installed) {
        $q = Try-DownloadQuaterniusCastleGlb
        if ($q) { $installed = Install-FortressFile -Source $q }
    }
}

if ($installed) {
    Write-Host "[Castle] Realistic fortress ready in $ResourcesDir"
    Write-Host "[Castle] Rebuild player: .\build-and-start.ps1 -BuildOnly"
}
else {
    Write-Host "[Castle] No fortress mesh installed. Options:"
    Write-Host "  1) .\tools\import-realistic-castle.ps1 -GlbPath <path-to-castle.glb>"
    Write-Host "  2) Export Sketchfab Medieval Castle Environment (CC-BY) as GLB + set SKETCHFAB_API_TOKEN"
    Write-Host "  Game will use stone-textured MedievalVillageMegaKit walls until a GLB is present."
}
