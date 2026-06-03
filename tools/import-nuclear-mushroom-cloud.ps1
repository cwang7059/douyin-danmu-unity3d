# 写实核爆蘑菇云贴图（维基共享 / OpenGameArt 公版照片 + Kenney CC0 烟雾）
# Run: .\tools\import-nuclear-mushroom-cloud.ps1
# 构建时若 hero 贴图已存在则跳过网络请求（避免 404/429 刷屏）

param(
    [switch]$SkipKenneySmoke,
    [switch]$Force,
    [switch]$VerboseDownload
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $Root
Set-Location $ProjectRoot

$DownloadDir = Join-Path $ProjectRoot "_downloads\nuclear-mushroom"
$DestDir = Join-Path $ProjectRoot "Assets\Resources\VFX\Nuclear"
$HeroPng = Join-Path $DestDir "mushroom_cloud_hero.png"
New-Item -ItemType Directory -Force -Path $DownloadDir, $DestDir | Out-Null

if (-not $Force -and (Test-Path $HeroPng) -and (Get-Item $HeroPng).Length -gt 80000) {
    Write-Host "[MushroomCloud] OK (cached) -> $DestDir"
    exit 0
}

$UserAgent = 'ApocalypseKingUnity3D/1.0 (asset-import; contact: build@local)'
function Get-RemoteFile {
    param(
        [string[]]$Urls,
        [string]$OutPath,
        [int]$MinBytes = 20000,
        [bool]$Required = $true
    )
    if ((Test-Path $OutPath) -and (Get-Item $OutPath).Length -ge $MinBytes) { return $true }
    foreach ($url in $Urls) {
        try {
            if ($VerboseDownload) { Write-Host "[MushroomCloud] GET $url" }
            Invoke-WebRequest -Uri $url -OutFile $OutPath -UseBasicParsing -TimeoutSec 180 -Headers @{ "User-Agent" = $UserAgent }
            if ((Test-Path $OutPath) -and (Get-Item $OutPath).Length -ge $MinBytes) { return $true }
        }
        catch {
            if ($VerboseDownload) { Write-Host "[MushroomCloud]   failed: $($_.Exception.Message)" }
        }
        Start-Sleep -Seconds 2
    }
    if ($Required) { throw "Download failed for $OutPath" }
    return $false
}

# Required: Castle Romeo (OGA) + Crossroads Baker (Wikimedia)
$RequiredSources = @(
    ,@{ Name = "castle_romeo.jpg"; Urls = @("https://opengameart.org/sites/default/files/Castle_Romeo.jpg") }
    ,@{ Name = "crossroads_baker.jpg"; Urls = @("https://upload.wikimedia.org/wikipedia/commons/e/e8/Baker_nuclear_test_blast_at_Bikini_atoll_1946.jpg", "https://upload.wikimedia.org/wikipedia/commons/1/1c/Baker_Test_atomic_explosion_during_Operation_Crossroads_25_July_1946.jpg") }
)

$OptionalSources = @(
    ,@{ Name = "trinity.jpg"; Urls = @("https://upload.wikimedia.org/wikipedia/commons/6/6f/Trinity_shot_color.jpg") }
    ,@{ Name = "ivy_mike.jpg"; Urls = @("https://upload.wikimedia.org/wikipedia/commons/1/1b/Ivy_Mike_-_mushroom_cloud.jpg", "https://upload.wikimedia.org/wikipedia/commons/3/3b/Ivy_Mike_mushroom_cloud.jpg") }
)

foreach ($src in $RequiredSources) {
    $out = Join-Path $DownloadDir $src.Name
    Get-RemoteFile -Urls $src.Urls -OutPath $out -Required $true | Out-Null
    Start-Sleep -Seconds 2
}

foreach ($src in $OptionalSources) {
    $out = Join-Path $DownloadDir $src.Name
    $ok = Get-RemoteFile -Urls $src.Urls -OutPath $out -Required $false
    if (-not $ok) {
        Write-Host "[MushroomCloud] optional $($src.Name) skipped (fallback atlas)"
    }
    Start-Sleep -Seconds 2
}

Add-Type -AssemblyName System.Drawing

function Convert-MushroomPhotoToPng {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$Size = 512,
        [string]$CropMode = "mushroom"
    )
    $img = [System.Drawing.Image]::FromFile($InputPath)
    $bmp = New-Object System.Drawing.Bitmap $Size, $Size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    $srcX = 0
    $srcY = 0
    $srcW = $img.Width
    $srcH = $img.Height
    if ($CropMode -eq "mushroom") {
        $srcW = [int]($img.Width * 0.78)
        $srcH = [int]($img.Height * 0.62)
        $srcX = [int](($img.Width - $srcW) * 0.5)
        $srcY = [int]($img.Height * 0.04)
    }
    elseif ($CropMode -eq "baker") {
        $srcW = [int]($img.Width * 0.72)
        $srcH = [int]($img.Height * 0.48)
        $srcX = [int](($img.Width - $srcW) * 0.5)
        $srcY = [int]($img.Height * 0.02)
    }

    $srcRect = New-Object System.Drawing.Rectangle $srcX, $srcY, $srcW, $srcH
    $destRect = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
    $g.DrawImage($img, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $img.Dispose()

    $step = if ($Size -ge 640) { 2 } else { 1 }
    for ($py = 0; $py -lt $Size; $py += $step) {
        for ($px = 0; $px -lt $Size; $px += $step) {
            $c = $bmp.GetPixel($px, $py)
            $lum = ($c.R * 0.299 + $c.G * 0.587 + $c.B * 0.114)
            $edgeDist = [Math]::Min([Math]::Min($px, $py), [Math]::Min($Size - 1 - $px, $Size - 1 - $py))
            $edge = [Math]::Min(1.0, $edgeDist / [double]($Size * 0.08))
            $alpha = 255
            if ($lum -lt 18) { $alpha = 0 }
            elseif ($lum -lt 42) { $alpha = [int](($lum - 18) / 24.0 * 200) }
            $alpha = [int]($alpha * $edge)
            $outColor = if ($alpha -lt 10) { [System.Drawing.Color]::FromArgb(0, 0, 0, 0) } else { [System.Drawing.Color]::FromArgb($alpha, $c.R, $c.G, $c.B) }
            for ($dy = 0; $dy -lt $step -and ($py + $dy) -lt $Size; $dy++) {
                for ($dx = 0; $dx -lt $step -and ($px + $dx) -lt $Size; $dx++) {
                    $bmp.SetPixel($px + $dx, $py + $dy, $outColor)
                }
            }
        }
    }

    $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function New-AtlasPng {
    param(
        [string[]]$InputPaths,
        [string]$OutputPath,
        [int]$Cell = 512,
        [int]$Cols = 2
    )
    $rows = [int][Math]::Ceiling($InputPaths.Length / [double]$Cols)
    $bmp = New-Object System.Drawing.Bitmap ($Cols * $Cell), ($rows * $Cell)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    for ($i = 0; $i -lt $InputPaths.Length; $i++) {
        $tmp = Join-Path $DownloadDir ("_cell_$i.png")
        Convert-MushroomPhotoToPng -InputPath $InputPaths[$i] -OutputPath $tmp -Size $Cell
        $cellImg = [System.Drawing.Image]::FromFile($tmp)
        $fx = ($i % $Cols) * $Cell
        $fy = [int]($i / $Cols) * $Cell
        $g.DrawImage($cellImg, $fx, $fy, $Cell, $Cell)
        $cellImg.Dispose()
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
    $g.Dispose()
    $bmp.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$heroSrc = $null
foreach ($prefer in @("castle_romeo.jpg", "crossroads_baker.jpg", "trinity.jpg", "ivy_mike.jpg")) {
    $candidate = Join-Path $DownloadDir $prefer
    if ((Test-Path $candidate) -and (Get-Item $candidate).Length -ge 20000) { $heroSrc = $candidate; break }
}
if (-not $heroSrc) { throw "No hero mushroom photo available." }
$heroCrop = if ($heroSrc -like "*baker*") { "baker" } else { "mushroom" }
Convert-MushroomPhotoToPng -InputPath $heroSrc -OutputPath $HeroPng -Size 512 -CropMode $heroCrop

$evoPaths = New-Object System.Collections.Generic.List[string]
Get-ChildItem -Path $DownloadDir -Filter "*.jpg" | Where-Object { $_.Length -ge 20000 } | Sort-Object Name | ForEach-Object { [void]$evoPaths.Add($_.FullName) }
if ($evoPaths.Count -eq 0) { throw "No mushroom cloud photos downloaded." }
while ($evoPaths.Count -lt 4) { [void]$evoPaths.Add($evoPaths[$evoPaths.Count % $evoPaths.Count]) }
if ($evoPaths.Count -gt 4) {
    $trimmed = $evoPaths.GetRange(0, 4)
    $evoPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($p in $trimmed) { [void]$evoPaths.Add($p) }
}
New-AtlasPng -InputPaths $evoPaths.ToArray() -OutputPath (Join-Path $DestDir "mushroom_cloud_evolution.png") -Cell 512 -Cols 2

if (-not $SkipKenneySmoke) {
    $KenneyZipUrl = "https://opengameart.org/sites/default/files/smokeParticleAssets.zip"
    $KenneyZip = Join-Path $DownloadDir "smokeParticleAssets.zip"
    $KenneyExtract = Join-Path $DownloadDir "kenney_smoke"
    $SmokeAtlas = Join-Path $DestDir "mushroom_smoke_atlas.png"
    if (-not (Test-Path $SmokeAtlas) -or $Force) {
        if (-not (Test-Path $KenneyZip) -or (Get-Item $KenneyZip).Length -lt 500000) {
            Write-Host "[MushroomCloud] Download Kenney smoke (CC0)..."
            Invoke-WebRequest -Uri $KenneyZipUrl -OutFile $KenneyZip -UseBasicParsing -TimeoutSec 240 -Headers @{ "User-Agent" = $UserAgent }
        }
        if (Test-Path $KenneyExtract) { Remove-Item $KenneyExtract -Recurse -Force }
        Expand-Archive -Path $KenneyZip -DestinationPath $KenneyExtract -Force
        $explosionDir = Get-ChildItem -Path $KenneyExtract -Recurse -Directory -Filter "explosion" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($null -eq $explosionDir) {
            $explosionDir = Get-ChildItem -Path $KenneyExtract -Recurse -Directory | Where-Object { $_.Name -match "explosion|Explosion" } | Select-Object -First 1
        }
        $frames = @()
        if ($explosionDir) {
            $frames = Get-ChildItem -Path $explosionDir.FullName -Filter "*.png" | Sort-Object Name | Select-Object -First 16 -ExpandProperty FullName
        }
        if ($frames.Count -lt 4) {
            $blackDir = Get-ChildItem -Path $KenneyExtract -Recurse -Directory | Where-Object { $_.Name -match "black" } | Select-Object -First 1
            if ($blackDir) {
                $frames = Get-ChildItem -Path $blackDir.FullName -Filter "*.png" | Sort-Object Name | Select-Object -First 16 -ExpandProperty FullName
            }
        }
        if ($frames.Count -ge 4) {
            $cell = 128
            $cols = 4
            $rows = [int][Math]::Ceiling($frames.Count / [double]$cols)
            $atlas = New-Object System.Drawing.Bitmap ($cols * $cell), ($rows * $cell)
            $g = [System.Drawing.Graphics]::FromImage($atlas)
            $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            for ($i = 0; $i -lt $frames.Count; $i++) {
                $frame = [System.Drawing.Image]::FromFile($frames[$i])
                $fx = ($i % $cols) * $cell
                $fy = [int]($i / $cols) * $cell
                $g.DrawImage($frame, $fx, $fy, $cell, $cell)
                $frame.Dispose()
            }
            $g.Dispose()
            $atlas.Save($SmokeAtlas, [System.Drawing.Imaging.ImageFormat]::Png)
            $atlas.Dispose()
        }
    }
}

Write-Host "[MushroomCloud] Installed -> $DestDir"
Write-Host "[MushroomCloud] License: doc/许可证/Wikimedia_Nuclear_Mushroom_Cloud_PublicDomain.md"
