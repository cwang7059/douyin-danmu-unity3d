param(
    [string]$UnityExe = "",
    [switch]$SkipUnitySetup
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
$DownloadDir = Join-Path $Root "_downloads"
$KenneyZipUrl = "https://opengameart.org/sites/default/files/kenney_particlePack.zip"
$KenneyZip = Join-Path $DownloadDir "kenney_particlePack.zip"
$KenneyExtract = Join-Path $DownloadDir "kenney_particlePack"
$KenneyPackage = Join-Path $KenneyExtract "Unity samples/particlePack_samples.unitypackage"
$VfxSelected = Join-Path $ProjectRoot "Assets/Resources/VFX/Online/Selected"
$KenneyPng = Join-Path $KenneyExtract "PNG"

New-Item -ItemType Directory -Force -Path $DownloadDir, $VfxSelected | Out-Null

if (-not (Test-Path -LiteralPath $KenneyZip) -or (Get-Item -LiteralPath $KenneyZip).Length -lt 1000000) {
    Write-Host "[DOWNLOAD] Kenney Particle Pack (CC0)..."
    Invoke-WebRequest -Uri $KenneyZipUrl -OutFile $KenneyZip -UseBasicParsing
}

if (-not (Test-Path -LiteralPath $KenneyPackage)) {
    Write-Host "[EXTRACT] Kenney zip..."
    if (Test-Path -LiteralPath $KenneyExtract) {
        Remove-Item -LiteralPath $KenneyExtract -Recurse -Force
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($KenneyZip, $KenneyExtract)
}

Write-Host "[IMPORT] Kenney Unity samples into project..."
& (Join-Path $Root "extract-unitypackage.ps1") -PackagePath $KenneyPackage -ProjectRoot $ProjectRoot

# Map Kenney PNG -> runtime Resources names (CC0)
$textureMap = @{
    "flash_kenney.png" = "flare_01.png"
    "smoke_white.png" = "smoke_05.png"
    "smoke_black.png" = "smoke_01.png"
    "muzzle_rifle.png" = "muzzle_01.png"
    "muzzle_tank.png" = "muzzle_04.png"
    "explosion_kenney.png" = "fire_02.png"
    "explosion_fireball.png" = "flame_04.png"
    "explosion_bomb.png" = "magic_05.png"
    "shockwave_ring.png" = "circle_03.png"
}

if (Test-Path -LiteralPath $KenneyPng) {
    Write-Host "[COPY] VFX textures to Resources..."
    foreach ($pair in $textureMap.GetEnumerator()) {
        $src = Join-Path $KenneyPng $pair.Value
        $dst = Join-Path $VfxSelected $pair.Key
        if (Test-Path -LiteralPath $src) {
            Copy-Item -LiteralPath $src -Destination $dst -Force
            Write-Host "  $($pair.Key) <- $($pair.Value)"
        }
    }

    # 8x8 flipbook sheets from Kenney fire/smoke sequence
    function New-FlipbookPng([string]$outName, [string[]]$frames, [int]$cell = 64) {
        Add-Type -AssemblyName System.Drawing
        $cols = [int][Math]::Ceiling([Math]::Sqrt($frames.Length))
        $rows = [int][Math]::Ceiling($frames.Length / [double]$cols)
        $bmp = New-Object System.Drawing.Bitmap ($cols * $cell), ($rows * $cell)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        for ($i = 0; $i -lt $frames.Length; $i++) {
            $srcPath = Join-Path $KenneyPng $frames[$i]
            if (-not (Test-Path -LiteralPath $srcPath)) { continue }
            $frame = [System.Drawing.Image]::FromFile($srcPath)
            $fx = ($i % $cols) * $cell
            $fy = [int]($i / $cols) * $cell
            $g.DrawImage($frame, $fx, $fy, $cell, $cell)
            $frame.Dispose()
        }
        $g.Dispose()
        $outPath = Join-Path $VfxSelected $outName
        $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "  $outName (flipbook $($frames.Length) frames)"
    }

    $smallFrames = @("fire_01.png", "fire_02.png", "flame_01.png", "flame_02.png", "flame_03.png", "flame_04.png", "scorch_01.png", "scorch_02.png", "scorch_03.png", "spark_01.png", "spark_02.png", "spark_03.png", "spark_04.png", "spark_05.png", "spark_06.png", "spark_07.png")
    $largeFrames = @("fire_01.png", "fire_02.png", "flame_03.png", "flame_04.png", "flame_05.png", "flame_06.png", "scorch_01.png", "scorch_02.png", "scorch_03.png", "smoke_06.png", "smoke_07.png", "smoke_08.png", "smoke_09.png", "smoke_10.png", "spark_05.png", "spark_06.png")
    $bombFrames = @("magic_01.png", "magic_02.png", "magic_03.png", "magic_04.png", "magic_05.png", "fire_02.png", "flame_05.png", "flame_06.png", "scorch_03.png", "smoke_08.png", "smoke_09.png", "smoke_10.png", "spark_06.png", "spark_07.png", "light_02.png", "light_03.png")

    New-FlipbookPng "explosion_sinestesia_small.png" $smallFrames
    New-FlipbookPng "explosion_sinestesia_large.png" $largeFrames
    New-FlipbookPng "explosion_sinestesia_bomb.png" $bombFrames
    $nuclearFrames = @("magic_03.png", "magic_04.png", "magic_05.png", "fire_02.png", "flame_05.png", "flame_06.png", "scorch_03.png", "smoke_09.png", "smoke_10.png", "smoke_08.png", "light_02.png", "light_03.png", "spark_07.png", "spark_06.png", "flame_04.png", "fire_01.png")
    New-FlipbookPng "explosion_nuclear.png" $nuclearFrames
}

if (-not $SkipUnitySetup) {
    Write-Host "[UNITY] Bind VFX prefabs + full project setup..."
    & (Join-Path $Root "bind-vfx-prefabs.ps1") -UnityExe $UnityExe
    & (Join-Path $Root "setup-unity-assets.ps1") -UnityExe $UnityExe
}

Write-Host ""
Write-Host "Kenney VFX imported under Assets/Kenney/Particle samples/"
Write-Host "Unity Asset Store (War FX / Cartoon FX Remaster Free) still require login in Unity:"
Write-Host "  Window > Package Manager > My Assets > Download > Import"
Write-Host "Then: Apocalypse King > Bind Store VFX Prefabs to Effect Configs"
