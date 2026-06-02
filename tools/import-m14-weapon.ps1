# Downloads / installs M14 rifle GLB for US Army soldiers.
# Run from repo root: .\tools\import-m14-weapon.ps1
# If network is blocked, run: python tools/generate-m14-glb.py

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads"
$ResourcesDir = Join-Path $Root "Assets\Resources\Weapons\M14Rifle"
New-Item -ItemType Directory -Force -Path $DownloadDir, $ResourcesDir | Out-Null

$DestGlb = Join-Path $ResourcesDir "M14Rifle.glb"
$PolyModelId = "lNGPW2NGPZ"
$ManualSources = @(
    (Join-Path $DownloadDir "M14Rifle_PolyPizza.glb"),
    (Join-Path $DownloadDir "MK14.glb"),
    (Join-Path $DownloadDir "M14Rifle.glb")
)

function Try-DownloadM14 {
    $urls = @(
        "https://static.poly.pizza/glTF/$PolyModelId.glb",
        "https://static.poly.pizza/$PolyModelId.glb",
        "https://poly.pizza/m/$PolyModelId/download"
    )
    foreach ($url in $urls) {
        $tmp = Join-Path $DownloadDir "M14Rifle_PolyPizza.glb"
        try {
            Write-Host "[M14] Trying $url ..."
            Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 60
            if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 4096) {
                return $tmp
            }
        }
        catch {
            Write-Host "[M14] Skipped: $($_.Exception.Message)"
        }
    }
    return $null
}

$source = Try-DownloadM14
if (-not $source) {
    foreach ($candidate in $ManualSources) {
        if ((Test-Path $candidate) -and (Get-Item $candidate).Length -gt 4096) {
            $source = $candidate
            Write-Host "[M14] Using cached download: $candidate"
            break
        }
    }
}

if ($source) {
    Copy-Item $source $DestGlb -Force
    Write-Host "[M14] Installed downloaded model -> $DestGlb"
}
else {
    Write-Host "[M14] Web download failed. Generating fallback GLB..."
    python (Join-Path $PSScriptRoot "generate-m14-glb.py")
    if (-not (Test-Path $DestGlb)) {
        throw "M14 install failed. Place a rifle GLB at _downloads/M14Rifle.glb or fix network, then re-run."
    }
}

Write-Host "[M14] Done. Reimport in Unity (or run setup-unity-assets.ps1) before building."
Write-Host "[M14] Preferred web source (CC-BY): https://poly.pizza/m/$PolyModelId"
