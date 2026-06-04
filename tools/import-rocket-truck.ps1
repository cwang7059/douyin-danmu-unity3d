# Red Alert-style rocket launcher truck (BM-21 Grad / V3 MLRS silhouette).
# Run from repo root:
#   .\tools\import-rocket-truck.ps1
#   .\tools\import-rocket-truck.ps1 -GlbPath "D:\Downloads\bm21.glb"
#   $env:SKETCHFAB_API_TOKEN = "..." ; .\tools\import-rocket-truck.ps1
#
# Primary (CC-BY): BM-21 Grad — Soviet truck MLRS, closest to 红警火箭车
#   https://sketchfab.com/3d-models/21-bm-21-grad-c90559a30e6a414d993a0d3bdf6c5ff8
# Alternates (Sketchfab, need token):
#   Katyusha BM-13  bb46c77dc61b46a0be797fe12aa9a36e
#   V3 launcher     8820550131e54ec8ac0f0712a85bc1b9
#   MLRS truck      303c46b5ede748288ae6ca6d085aae79

param(
    [string]$GlbPath = "",
    [string]$SourceDir = "",
    [string]$SketchfabUid = "c90559a30e6a414d993a0d3bdf6c5ff8"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads\rocket-truck"
$RocketTruckDir = Join-Path $Root "Assets\Resources\Vehicles\RocketTruck"
$Dest = Join-Path $RocketTruckDir "RocketTruck.glb"
$IncomingDrop = Join-Path $RocketTruckDir "Incoming\RocketTruck.glb"

$SketchfabCandidates = @(
    @{ Uid = "c90559a30e6a414d993a0d3bdf6c5ff8"; Tag = "BM-21 Grad (CC-BY)" },
    @{ Uid = "bb46c77dc61b46a0be797fe12aa9a36e"; Tag = "Katyusha BM-13" },
    @{ Uid = "8820550131e54ec8ac0f0712a85bc1b9"; Tag = "V3 rocket launcher" },
    @{ Uid = "303c46b5ede748288ae6ca6d085aae79"; Tag = "MLRS military truck" },
    @{ Uid = "03accd98d1b7421c82d266dbb670babe"; Tag = "BM-21 Grad alt" }
)

if (-not [string]::IsNullOrWhiteSpace($SketchfabUid)) {
    $preferred = $SketchfabCandidates | Where-Object { $_.Uid -eq $SketchfabUid } | Select-Object -First 1
    $rest = $SketchfabCandidates | Where-Object { $_.Uid -ne $SketchfabUid }
    if ($preferred) {
        $SketchfabCandidates = @($preferred) + @($rest)
    }
}

New-Item -ItemType Directory -Force -Path $DownloadDir, $RocketTruckDir, (Split-Path $IncomingDrop) | Out-Null

function Test-GlbReady {
    param([string]$Path)
    return (Test-Path $Path) -and (Get-Item $Path).Length -gt 16384
}

function Install-Glb {
    param([string]$Source, [string]$Label)
    if (-not (Test-GlbReady $Source)) { return $false }
    Copy-Item $Source $Dest -Force
    $bytes = (Get-Item $Dest).Length
    Write-Host "[RocketTruck] Installed $Label ($bytes bytes)"
    Write-Host "  -> Assets/Resources/Vehicles/RocketTruck/RocketTruck.glb"
    return $true
}

function Try-DownloadSketchfabGlb {
    param([string]$Uid, [string]$Tag)
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-$Uid.glb"
    try {
        Write-Host "[RocketTruck] Sketchfab API ($Tag) uid=$Uid ..."
        $headers = @{ Authorization = "Token $token" }
        $meta = Invoke-RestMethod -Uri "https://api.sketchfab.com/v3/models/$Uid/download" -Headers $headers -UseBasicParsing
        $url = $null
        if ($meta.glb -and $meta.glb.url) { $url = $meta.glb.url }
        elseif ($meta.gltf -and $meta.gltf.url) { $url = $meta.gltf.url }
        if (-not $url) { return $null }
        Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 300
        if (Test-GlbReady $tmp) { return $tmp }
    }
    catch {
        Write-Host "[RocketTruck] Sketchfab skipped ($Tag): $($_.Exception.Message)"
    }
    return $null
}

if ($GlbPath -and (Test-Path $GlbPath)) {
    if (Install-Glb $GlbPath "manual -GlbPath") { exit 0 }
}

if ($SourceDir -and (Test-Path $SourceDir)) {
    $pick = Get-ChildItem -Path $SourceDir -Recurse -Filter "*.glb" -File -ErrorAction SilentlyContinue |
        Sort-Object Length -Descending | Select-Object -First 1
    if ($pick -and (Install-Glb $pick.FullName "manual -SourceDir")) { exit 0 }
}

if (Test-GlbReady $IncomingDrop) {
    if (Install-Glb $IncomingDrop "Incoming/RocketTruck.glb") { exit 0 }
}

$dropCandidates = @(
    (Join-Path $DownloadDir "RocketTruck.glb"),
    (Join-Path $Root "_downloads\RocketTruck.glb"),
    (Join-Path $Root "_downloads\bm21-grad.glb")
)
foreach ($c in $dropCandidates) {
    if (Test-GlbReady $c) {
        if (Install-Glb $c "downloads folder") { exit 0 }
    }
}

foreach ($entry in $SketchfabCandidates) {
    $glb = Try-DownloadSketchfabGlb -Uid $entry.Uid -Tag $entry.Tag
    if ($glb -and (Install-Glb $glb "Sketchfab $($entry.Tag)")) { exit 0 }
}

if (Test-GlbReady $Dest) {
    Write-Host "[RocketTruck] Keeping existing RocketTruck.glb ($((Get-Item $Dest).Length) bytes)"
    exit 0
}

Write-Host "[RocketTruck] Generating procedural MLRS truck GLB (红警火箭车轮廓)..."
python (Join-Path $PSScriptRoot "generate-rocket-truck-glb.py")
if (Test-GlbReady $Dest) {
    Write-Host "[RocketTruck] Procedural GLB ready."
    exit 0
}

Write-Host ""
Write-Host "[RocketTruck] For high-quality BM-21 / V3 mesh:"
Write-Host "  1) Download GLB from Sketchfab (links in script header)"
Write-Host "  2) Save to: Assets/Resources/Vehicles/RocketTruck/Incoming/RocketTruck.glb"
Write-Host "  3) Re-run: .\tools\import-rocket-truck.ps1"
Write-Host "  Or: `$env:SKETCHFAB_API_TOKEN = '...' ; .\tools\import-rocket-truck.ps1"
Write-Host "[RocketTruck] Unity: Reimport Vehicles/RocketTruck, Stop -> Play."
exit 1
