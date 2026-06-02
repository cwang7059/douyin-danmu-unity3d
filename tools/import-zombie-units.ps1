# Installs zombie character assets for UnitKind.Giant (丧尸).
# Primary: OpenGameArt Pixelhouse zombie (FBX + textures, auto-download).
# Fallback: Kenney Animated Characters 3 (CC0).
# Optional: Quaternius GLB via Poly Pizza when CDN allows.
# Run from repo root: .\tools\import-zombie-units.ps1
# Guide: doc/丧尸写实素材选型与导入.md

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads"
$PixelhouseZip = Join-Path $DownloadDir "pixelhouse-zombie.zip"
$PixelhouseZipUrl = "https://opengameart.org/sites/default/files/zombie.zip"
$PixelhouseDir = Join-Path $Root "Assets\Resources\RealisticZombies\Pixelhouse"
$PixelhouseStreaming = Join-Path $Root "Assets\StreamingAssets\RealisticZombies\Pixelhouse"
$KenneyResources = Join-Path $Root "Assets\Resources\Kenney\ZombieCharacters"
$KenneyThirdParty = Join-Path $Root "Assets\ThirdParty\Kenney\AnimatedCharacters3"
$QuaterniusPolyDir = Join-Path $Root "Assets\Resources\Quaternius\ZombieUnits"
$QuaterniusStreaming = Join-Path $Root "Assets\StreamingAssets\Quaternius\ZombieUnits"

New-Item -ItemType Directory -Force -Path $DownloadDir, $PixelhouseDir, $PixelhouseStreaming, $QuaterniusPolyDir, $QuaterniusStreaming | Out-Null

function Install-PixelhouseZombie {
    if (-not (Test-Path $PixelhouseZip)) {
        Write-Host "[Zombie] Downloading OpenGameArt Pixelhouse zombie (rigged FBX + textures)..."
        Invoke-WebRequest -Uri $PixelhouseZipUrl -OutFile $PixelhouseZip -UseBasicParsing -TimeoutSec 120
    }

    $extract = Join-Path $DownloadDir "pixelhouse-zombie-extract"
    if (Test-Path $extract) {
        Remove-Item $extract -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $extract | Out-Null
    tar -xf $PixelhouseZip -C $extract

    $map = @{
        "walk.FBX"  = "Zombie.fbx"
        "fury.FBX"  = "ZombieFury.fbx"
        "dead.FBX"  = "ZombieDead.fbx"
        "difusse.jpg" = "ZombieDiffuse.jpg"
        "especular.jpg" = "ZombieSpecular.jpg"
        "normal.JPG" = "ZombieNormal.jpg"
    }

    $copied = 0
    foreach ($entry in $map.GetEnumerator()) {
        $src = Get-ChildItem -Path $extract -Recurse -Filter $entry.Key -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $src) {
            Write-Host "[Zombie] Missing in zip: $($entry.Key)"
            continue
        }
        $dest = Join-Path $PixelhouseDir $entry.Value
        Copy-Item $src.FullName $dest -Force
        $streamDest = Join-Path $PixelhouseStreaming $entry.Value
        Copy-Item $src.FullName $streamDest -Force
        $copied++
    }

    Write-Host "[Zombie] Pixelhouse zombie -> $PixelhouseDir ($copied files)"
    return $copied -gt 0
}

function Try-DownloadPolyZombie {
    param([string]$Id, [string]$Name)
    $tmp = Join-Path $DownloadDir "zombie-units\$Name.glb"
    $dest = Join-Path $QuaterniusPolyDir "$Name.glb"
    if (Test-Path $dest) { return $true }
    $urls = @(
        "https://static.poly.pizza/glTF/$Id.glb",
        "https://static.poly.pizza/$Id.glb"
    )
    foreach ($url in $urls) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 60
            if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 8192) {
                Copy-Item $tmp $dest -Force
                Copy-Item $tmp (Join-Path $QuaterniusStreaming "$Name.glb") -Force
                Write-Host "[Zombie] Poly Pizza $Name installed."
                return $true
            }
        }
        catch { }
    }
    return $false
}

function Install-KenneyFallback {
    $srcRoot = $KenneyThirdParty
    if (-not (Test-Path (Join-Path $srcRoot "Model\characterMedium.fbx"))) {
        $KenneyZip = Join-Path $DownloadDir "kenney_animated-characters-3.zip"
        $KenneyExtract = Join-Path $DownloadDir "kenney_ac3_extract"
        if (-not (Test-Path $KenneyZip)) {
            Write-Host "[Zombie] Downloading Kenney Animated Characters 3 (CC0 fallback)..."
            try {
                Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/kenney_animated-characters-3.zip" `
                    -OutFile $KenneyZip -UseBasicParsing -TimeoutSec 120
            }
            catch {
                Write-Host "[Zombie] Kenney download failed."
                return
            }
        }
        if (Test-Path $KenneyZip) {
            New-Item -ItemType Directory -Force -Path $KenneyExtract | Out-Null
            Expand-Archive -Path $KenneyZip -DestinationPath $KenneyExtract -Force
            $inner = Get-ChildItem -Path $KenneyExtract -Recurse -Filter "characterMedium.fbx" | Select-Object -First 1
            if ($inner) { $srcRoot = $inner.Directory.Parent.Parent.FullName }
        }
    }

    if (-not (Test-Path (Join-Path $srcRoot "Model\characterMedium.fbx"))) {
        return
    }

    foreach ($dir in @("Model", "Animations", "Skins")) {
        $from = Join-Path $srcRoot $dir
        $to = Join-Path $KenneyResources $dir
        if (Test-Path $from) {
            New-Item -ItemType Directory -Force -Path $to | Out-Null
            Copy-Item -Path (Join-Path $from "*") -Destination $to -Recurse -Force
        }
    }
    Write-Host "[Zombie] Kenney stylized fallback -> $KenneyResources"
}

$pixelOk = Install-PixelhouseZombie
Install-KenneyFallback

$polyModels = @(
    @{ Id = "VlXjG0N8Eg"; Name = "ZombieA" },
    @{ Id = "JoBvxIUpZP"; Name = "ZombieB" }
)
$polyInstalled = 0
foreach ($entry in $polyModels) {
    if (Try-DownloadPolyZombie $entry.Id $entry.Name) { $polyInstalled++ }
}

Write-Host ""
if ($pixelOk) {
    Write-Host "[Zombie] Done. Primary: RealisticZombies/Pixelhouse/Zombie.fbx (OpenGameArt)."
    Write-Host "[Zombie] Reimport in Unity, then build."
}
else {
    Write-Host "[Zombie] ERROR: Pixelhouse download failed. Check network."
    exit 1
}
