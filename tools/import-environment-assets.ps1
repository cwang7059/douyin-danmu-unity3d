# Downloads CC0 environment textures (Poly Haven grass + Kenney mountains) into Resources.
# Run from repo root: .\tools\import-environment-assets.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Online = Join-Path $Root "Assets\Resources\Environment\Online"
New-Item -ItemType Directory -Force -Path $Online | Out-Null

Write-Host "[Environment] Downloading grass detail (Poly Haven CC0)..."
try {
    Invoke-WebRequest -Uri "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/aerial_grass_rock/aerial_grass_rock_diff_1k.jpg" `
        -OutFile (Join-Path $Online "grass_detail.jpg") -UseBasicParsing -TimeoutSec 90
    Write-Host "[Environment] grass_detail.jpg OK"
}
catch {
    Write-Host "[Environment] grass_detail download failed: $_"
}

$kenneyZip = Join-Path $Root "tools\_downloads\kenney_background_elements.zip"
$kenneyExtract = Join-Path $Root "tools\_downloads\kenney_background_elements"
if (-not (Test-Path $kenneyZip)) {
    Write-Host "[Environment] Downloading Kenney Background Elements Redux (CC0)..."
    New-Item -ItemType Directory -Force -Path (Split-Path $kenneyZip) | Out-Null
    Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/Background%20Elements%20Redux.zip" `
        -OutFile $kenneyZip -UseBasicParsing -TimeoutSec 120
}

if (Test-Path $kenneyZip) {
    New-Item -ItemType Directory -Force -Path $kenneyExtract | Out-Null
    Expand-Archive -Path $kenneyZip -DestinationPath $kenneyExtract -Force
    $elementDir = Join-Path $kenneyExtract "Backgrounds\Elements"
    if (Test-Path $elementDir) {
        $pairs = @(
            ,("mountain_ridge.png", "mountains.png")
            ,("mountain_layer_a.png", "mountainA.png")
            ,("mountain_layer_b.png", "mountainB.png")
            ,("mountain_layer_c.png", "mountainC.png")
            ,("hills_large.png", "hillsLarge.png")
        )
        foreach ($pair in $pairs) {
            $dest = Join-Path $Online $pair[0]
            $src = Join-Path $elementDir $pair[1]
            if (Test-Path $src) {
                Copy-Item $src $dest -Force
                Write-Host "[Environment] Copied $($pair[0])"
            }
        }
    }
}

Write-Host "[Environment] Done. Open Unity to import new textures."
