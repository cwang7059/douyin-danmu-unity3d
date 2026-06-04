# Installs pterosaur fireball VFX into Resources for runtime loading.
# Prefers official Cartoon FX Remaster fireball prefabs when present (paid CFXR 1/2/3 packs).
# Falls back to CFXR4 Sun from the Free sampler when no fireball prefab exists.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dst = Join-Path $root "Assets\Resources\Battle\VFX"
$cfxrRoot = Join-Path $root "Assets\JMO Assets\Cartoon FX Remaster"

$fireballPriority = @(
    "CFXR Fireball + Fire Trail.prefab",
    "CFXR Fireball.prefab",
    "CFXR2 Fireball + Screen Distortion.prefab",
    "CFXR2 Fireball.prefab",
    "CFXR3 Fireball A.prefab",
    "CFXR3 Fireball B.prefab",
    "CFXR4 Sun.prefab"
)

$hitPriority = @(
    "CFXR3 Hit Fire B (Air).prefab",
    "CFXR3 Hit Fire A (Air).prefab",
    "CFXR2 Hit Fire B (Air).prefab",
    "CFXR Hit Fire B (Air).prefab"
)

function Find-CfxrPrefab([string[]]$names) {
    if (-not (Test-Path $cfxrRoot)) {
        return $null
    }
    foreach ($name in $names) {
        $hits = Get-ChildItem -Path $cfxrRoot -Recurse -Filter $name -File -ErrorAction SilentlyContinue
        if ($hits) {
            return ($hits | Select-Object -First 1).FullName
        }
    }
    return $null
}

New-Item -ItemType Directory -Force -Path $dst | Out-Null

$fireballSrc = Find-CfxrPrefab $fireballPriority
$hitSrc = Find-CfxrPrefab $hitPriority
$fireballDst = Join-Path $dst "PterosaurCfxrFireball.prefab"
$hitDst = Join-Path $dst "PterosaurCfxrFireballHit.prefab"

if (-not $fireballSrc) {
    Write-Error @"
No CFXR fireball prefab found under:
  $cfxrRoot

Import Cartoon FX Remaster Free (minimum) or Cartoon FX 1 Remaster for official
  'CFXR Fireball + Fire Trail', then re-run this script.
"@
}
if (-not $hitSrc) {
    Write-Error "No CFXR fire hit prefab found under $cfxrRoot"
}

Copy-Item -Force $fireballSrc $fireballDst
Copy-Item -Force $hitSrc $hitDst

$fireballName = [System.IO.Path]::GetFileNameWithoutExtension($fireballSrc)
Write-Host "Installed pterosaur CFXR fireball VFX:"
Write-Host "  projectile: $fireballDst  (from $fireballName)"
Write-Host "  impact:     $hitDst"
if ($fireballName -notmatch "Fireball") {
    Write-Host ""
    Write-Host "NOTE: Using free-pack fallback ($fireballName), not official Fireball+Fire Trail."
    Write-Host "      Import Cartoon FX 1 Remaster from Asset Store, then re-run this script."
}
Write-Host ""
Write-Host "Reimport Assets/Resources/Battle/VFX in Unity, then Play."
