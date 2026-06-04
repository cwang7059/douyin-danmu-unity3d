# 提示：Fireball Pack 需在 Unity Editor 内从 Asset Store 导入；本脚本仅打开说明与商店页。
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$doc = Join-Path $repoRoot "doc\URP迁移与火球特效.md"
$url = "https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-asset-vfx-particles-fireball-pack-263814"

Write-Host "[ApocalypseKing] URP + Fireball Pack setup"
Write-Host "  1. Open Unity project (URP packages resolve from manifest.json)"
Write-Host "  2. Menu: Apocalypse King > URP > Run Full URP + Fireball Setup"
Write-Host "  3. Import Fireball Pack from Asset Store, then URP > 3. Install Pterosaur Fireball Prefab to Resources"
Write-Host "  Doc: $doc"
Start-Process $url
