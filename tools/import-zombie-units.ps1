# Installs online zombie character assets for UnitKind.Giant (丧尸).
# Primary: Kenney Animated Characters 3 (CC0, opengameart) — zombie male/female skins + run/idle.
# Optional: Quaternius zombie GLBs from Poly Pizza when CDN allows download.
# Run from repo root: .\tools\import-zombie-units.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$KenneyThirdParty = Join-Path $Root "Assets\ThirdParty\Kenney\AnimatedCharacters3"
$KenneyResources = Join-Path $Root "Assets\Resources\Kenney\ZombieCharacters"
$DownloadDir = Join-Path $Root "_downloads"
$KenneyZip = Join-Path $DownloadDir "kenney_animated-characters-3.zip"
$KenneyExtract = Join-Path $DownloadDir "kenney_ac3_extract"

function Install-KenneyZombieCharacters {
    $srcRoot = $KenneyThirdParty
    if (-not (Test-Path (Join-Path $srcRoot "Model\characterMedium.fbx"))) {
        New-Item -ItemType Directory -Force -Path $DownloadDir | Out-Null
        if (-not (Test-Path $KenneyZip)) {
            Write-Host "[Zombie] Downloading Kenney Animated Characters 3 (CC0)..."
            Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/kenney_animated-characters-3.zip" `
                -OutFile $KenneyZip -UseBasicParsing -TimeoutSec 120
        }
        if (Test-Path $KenneyZip) {
            New-Item -ItemType Directory -Force -Path $KenneyExtract | Out-Null
            Expand-Archive -Path $KenneyZip -DestinationPath $KenneyExtract -Force
            $inner = Get-ChildItem -Path $KenneyExtract -Recurse -Filter "characterMedium.fbx" | Select-Object -First 1
            if ($inner) {
                $srcRoot = $inner.Directory.Parent.Parent.FullName
            }
        }
    }

    if (-not (Test-Path (Join-Path $srcRoot "Model\characterMedium.fbx"))) {
        throw "[Zombie] Kenney characterMedium.fbx not found. Check $KenneyThirdParty or network."
    }

    $dirs = @("Model", "Animations", "Skins")
    foreach ($dir in $dirs) {
        $from = Join-Path $srcRoot $dir
        $to = Join-Path $KenneyResources $dir
        if (Test-Path $from) {
            New-Item -ItemType Directory -Force -Path $to | Out-Null
            Copy-Item -Path (Join-Path $from "*") -Destination $to -Recurse -Force
        }
    }

    Write-Host "[Zombie] Kenney zombie characters -> $KenneyResources"
}

Install-KenneyZombieCharacters

# Optional Quaternius GLBs (Poly Pizza); skip quietly when blocked (403).
$PolyDir = Join-Path $Root "Assets\Resources\Quaternius\ZombieUnits"
$PolyStreaming = Join-Path $Root "Assets\StreamingAssets\Quaternius\ZombieUnits"
New-Item -ItemType Directory -Force -Path (Join-Path $Root "_downloads\zombie-units"), $PolyDir, $PolyStreaming | Out-Null

$PolyModels = @(
    @{ Id = "VlXjG0N8Eg"; Name = "ZombieA" },
    @{ Id = "JoBvxIUpZP"; Name = "ZombieB" },
    @{ Id = "22K0aSZkHV"; Name = "ZombieC" },
    @{ Id = "jkrEvQZb8J"; Name = "ZombieD" }
)

$polyInstalled = 0
foreach ($entry in $PolyModels) {
    $tmp = Join-Path $Root "_downloads\zombie-units\$($entry.Name).glb"
    $dest = Join-Path $PolyDir "$($entry.Name).glb"
    if (Test-Path $dest) {
        $polyInstalled++
        continue
    }
    $urls = @(
        "https://static.poly.pizza/glTF/$($entry.Id).glb",
        "https://static.poly.pizza/$($entry.Id).glb"
    )
    $ok = $false
    foreach ($url in $urls) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing -TimeoutSec 60
            if ((Test-Path $tmp) -and (Get-Item $tmp).Length -gt 8192) {
                Copy-Item $tmp $dest -Force
                Copy-Item $tmp (Join-Path $PolyStreaming "$($entry.Name).glb") -Force
                $polyInstalled++
                $ok = $true
                Write-Host "[Zombie] Poly Pizza $($entry.Name) installed."
                break
            }
        }
        catch { }
    }
    if (-not $ok) {
        Write-Host "[Zombie] Poly Pizza $($entry.Name) skipped (CDN blocked or offline)."
    }
}

Write-Host "[Zombie] Done. Kenney required; Quaternius optional ($polyInstalled/4)."
Write-Host "[Zombie] Reimport in Unity (setup-unity-assets.ps1) before building."
