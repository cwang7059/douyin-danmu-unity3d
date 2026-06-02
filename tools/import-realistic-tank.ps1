# Installs realistic tank OBJ/GLB/FBX for UnitKind.Tank (replaces low-poly Quaternius when present).
# Auto-sources: bundled Sketchfab 7z in StreamingAssets, manual GLB, or SKETCHFAB_API_TOKEN download.
# Run: .\tools\import-realistic-tank.ps1
#      .\tools\import-realistic-tank.ps1 -GlbPath "C:\Downloads\scene.glb"

param(
    [string]$GlbPath = "",
    [string]$FbxPath = "",
    [string]$SourceDir = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

$DownloadDir = Join-Path $Root "_downloads\realistic-tank"
$ExtractDir = Join-Path $Root "_downloads\t55a-extract"
$ResourcesDir = Join-Path $Root "Assets\Resources\RealisticTanks"
$StreamingDir = Join-Path $Root "Assets\StreamingAssets\RealisticTanks"
$Sketchfab7z = Join-Path $Root "Assets\StreamingAssets\Sketchfab\t55a_download\source\T-55A 1963 Soviet Tank Edited by Alex_Ka.7z"
$SketchfabUid = "ff908961f49a46218e6f4a5b8e84f3f8"

New-Item -ItemType Directory -Force -Path $DownloadDir, $ResourcesDir, $StreamingDir | Out-Null

function Install-TankFile {
    param([string]$Source)
    if (-not (Test-Path $Source)) { return $false }
    $ext = [System.IO.Path]::GetExtension($Source).ToLowerInvariant()
    if ($ext -ne ".glb" -and $ext -ne ".fbx") { return $false }
    foreach ($name in @("T55A", "T55AK")) {
        $dest = Join-Path $ResourcesDir "$name$ext"
        $stream = Join-Path $StreamingDir "$name$ext"
        Copy-Item $Source $dest -Force
        Copy-Item $Source $stream -Force
        Write-Host "[Tank] $name$ext <- $Source"
    }
    return $true
}

function Copy-TankSupportFiles {
    param([string]$SourceFolder, [string]$DestFolder)
    $keep = @("tex1.jpg", "tex2.jpg", "tex3.jpg", "tex1 - nrm.jpg", "tex2 - nrm.jpg", "tex3 - nrm.jpg")
    foreach ($name in $keep) {
        $src = Join-Path $SourceFolder $name
        if (Test-Path $src) {
            Copy-Item $src (Join-Path $DestFolder $name) -Force
        }
    }
}

function Remove-SketchfabDisplayAssets {
    param([string]$Folder)
    Get-ChildItem -Path $Folder -File -ErrorAction SilentlyContinue | ForEach-Object {
        $n = $_.Name
        if ($n -match '^\d{3}\s' -or $n -match '^(Floor Circle|logo|shadow|Smoke)\.' -or $n -match 'ground nrm') {
            Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        }
    }
}

function Install-TankObjBundle {
    param([string]$ObjSource, [string]$MtlSource)
    if (-not (Test-Path $ObjSource)) { return $false }
    $sourceFolder = Split-Path -Parent $ObjSource
    $stagingMtl = Join-Path $DownloadDir "_tank_source.mtl"
    if (Test-Path $MtlSource) {
        Copy-Item $MtlSource $stagingMtl -Force
    }
    else {
        $autoMtl = Get-ChildItem -Path $sourceFolder -Filter "*.mtl" -File | Select-Object -First 1
        if ($autoMtl) { Copy-Item $autoMtl.FullName $stagingMtl -Force }
    }

    Copy-TankSupportFiles -SourceFolder $sourceFolder -DestFolder $ResourcesDir
    Copy-TankSupportFiles -SourceFolder $sourceFolder -DestFolder $StreamingDir
    Remove-SketchfabDisplayAssets -Folder $ResourcesDir
    Remove-SketchfabDisplayAssets -Folder $StreamingDir

    $stagingObj = Join-Path $DownloadDir "_tank_source.obj"
    Copy-Item $ObjSource $stagingObj -Force

    foreach ($name in @("T55A", "T55AK")) {
        $outObj = Join-Path $ResourcesDir "$name.obj"
        $outMtl = Join-Path $ResourcesDir "$name.mtl"
        $objForVariant = (Get-Content -Path $stagingObj -Raw -Encoding UTF8) -replace '(?m)^mtllib\s+.*$', "mtllib $name.mtl"
        Set-Content -Path $outObj -Value $objForVariant -Encoding UTF8 -NoNewline
        if (Test-Path $stagingMtl) {
            Copy-Item $stagingMtl $outMtl -Force
        }
        python "$PSScriptRoot\clean-t55-obj.py" $outObj $outMtl
        Copy-Item $outObj (Join-Path $StreamingDir "$name.obj") -Force
        if (Test-Path $outMtl) {
            Copy-Item $outMtl (Join-Path $StreamingDir "$name.mtl") -Force
        }
        Write-Host "[Tank] $name.obj (+ textures, display meshes stripped) <- $ObjSource"
    }
    return $true
}

function Ensure-Sketchfab7zExtracted {
    if (-not (Test-Path $Sketchfab7z)) { return $null }
    $obj = Get-ChildItem -Path $ExtractDir -Recurse -Filter "*.obj" -File -ErrorAction SilentlyContinue |
        Sort-Object Length -Descending | Select-Object -First 1
    if ($obj) { return $obj.FullName }

    Write-Host "[Tank] Extracting bundled Sketchfab T-55A archive..."
    if (Test-Path $ExtractDir) { Remove-Item $ExtractDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $ExtractDir | Out-Null

    $seven = @("C:\Program Files\7-Zip\7z.exe", "C:\Program Files (x86)\7-Zip\7z.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($seven) {
        & $seven x $Sketchfab7z "-o$ExtractDir" -y | Out-Null
    }
    else {
        python -c @"
import os, shutil
import py7zr
archive = r'$($Sketchfab7z -replace "\\","\\")'
out = r'$($ExtractDir -replace "\\","\\")'
os.makedirs(out, exist_ok=True)
with py7zr.SevenZipFile(archive, mode='r') as z:
    z.extractall(path=out)
"@ | Out-Null
        if ($LASTEXITCODE -ne 0) {
            pip install py7zr -q 2>$null
            python -c "import os,py7zr; z=py7zr.SevenZipFile(r'$Sketchfab7z','r'); z.extractall(r'$ExtractDir')"
        }
    }

    $obj = Get-ChildItem -Path $ExtractDir -Recurse -Filter "*.obj" -File -ErrorAction SilentlyContinue |
        Sort-Object Length -Descending | Select-Object -First 1
    if ($obj) { return $obj.FullName }
    return $null
}

function Try-DownloadSketchfabGlb {
    $token = $env:SKETCHFAB_API_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) { return $null }

    $tmp = Join-Path $DownloadDir "sketchfab-t55.glb"
    try {
        Write-Host "[Tank] Sketchfab API download (Ukrainian T-55AGM)..."
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
        Write-Host "[Tank] Sketchfab API skipped: $($_.Exception.Message)"
    }
    return $null
}

$installed = $false

if ($GlbPath -and (Test-Path $GlbPath)) {
    $installed = Install-TankFile -Source $GlbPath
}
elseif ($FbxPath -and (Test-Path $FbxPath)) {
    $installed = Install-TankFile -Source $FbxPath
}
elseif ($SourceDir -and (Test-Path $SourceDir)) {
    $pick = Get-ChildItem -Path $SourceDir -Recurse -Include *.glb,*.fbx,*.obj -File | Sort-Object Length -Descending | Select-Object -First 1
    if ($pick) {
        if ($pick.Extension -eq ".obj") {
            $mtl = Get-ChildItem -Path $SourceDir -Recurse -Filter "*.mtl" -File | Select-Object -First 1
            $installed = Install-TankObjBundle -ObjSource $pick.FullName -MtlSource $(if ($mtl) { $mtl.FullName } else { "" })
        }
        else {
            $installed = Install-TankFile -Source $pick.FullName
        }
    }
}
else {
    $glb = Try-DownloadSketchfabGlb
    if ($glb) { $installed = Install-TankFile -Source $glb }

    if (-not $installed) {
        $pick = Get-ChildItem -Path $DownloadDir -Recurse -Include *.glb,*.fbx,*.obj -File -ErrorAction SilentlyContinue |
            Sort-Object Length -Descending | Select-Object -First 1
        if ($pick) {
            if ($pick.Extension -eq ".obj") {
                $installed = Install-TankObjBundle -ObjSource $pick.FullName -MtlSource ""
            }
            else {
                $installed = Install-TankFile -Source $pick.FullName
            }
        }
    }

    if (-not $installed) {
        $objPath = Ensure-Sketchfab7zExtracted
        if ($objPath) {
            $mtl = Get-ChildItem -Path (Split-Path $objPath) -Filter "*.mtl" -File | Select-Object -First 1
            $installed = Install-TankObjBundle -ObjSource $objPath -MtlSource $(if ($mtl) { $mtl.FullName } else { "" })
        }
    }
}

if (-not $installed) {
    Write-Host ""
    Write-Host "[Tank] No tank model installed."
    Write-Host "  Bundled 7z missing or extract failed. Place GLB in _downloads\realistic-tank or run:"
    Write-Host "  .\tools\import-realistic-tank.ps1 -GlbPath `"path\to\scene.glb`""
    Write-Host "  Optional: set SKETCHFAB_API_TOKEN for auto GLB download."
    exit 1
}

Write-Host "[Tank] Installed to $ResourcesDir (Unity will import on next build)."
Write-Host "[Tank] Game prefers RealisticTanks over Quaternius AnimatedTankPack."
