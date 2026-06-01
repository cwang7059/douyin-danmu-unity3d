param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PackagePath)) {
    throw "Package not found: $PackagePath"
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("unitypkg-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    tar -xzf $PackagePath -C $tempRoot
    $folders = Get-ChildItem -LiteralPath $tempRoot -Directory
    $imported = 0
    foreach ($folder in $folders) {
        $pathnameFile = Join-Path $folder.FullName "pathname"
        if (-not (Test-Path -LiteralPath $pathnameFile)) {
            continue
        }

        $targetRelative = (Get-Content -LiteralPath $pathnameFile -Raw).Trim()
        if ([string]::IsNullOrWhiteSpace($targetRelative)) {
            continue
        }

        $targetRelative = $targetRelative -replace '/', [IO.Path]::DirectorySeparatorChar
        $assetSource = Join-Path $folder.FullName "asset"
        if (-not (Test-Path -LiteralPath $assetSource)) {
            continue
        }

        $destPath = Join-Path $ProjectRoot $targetRelative
        $destDir = Split-Path -Parent $destPath
        if (-not (Test-Path -LiteralPath $destDir)) {
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        }

        Copy-Item -LiteralPath $assetSource -Destination $destPath -Force
        $metaSource = Join-Path $folder.FullName "asset.meta"
        if (Test-Path -LiteralPath $metaSource) {
            Copy-Item -LiteralPath $metaSource -Destination ($destPath + ".meta") -Force
        }

        $imported++
    }

    Write-Host "[OK] Extracted $imported files from $(Split-Path -Leaf $PackagePath)"
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
