using UnityEngine;

/// <summary>Clamps per-effect playback scale so pooled particles stay readable in the logical battlefield.</summary>
public static class BattleEffectTuning
{
    public static float NormalizeScale(BattleEffectId id, float scale)
    {
        scale = Mathf.Max(0.05f, scale);
        float min;
        float max;
        GetScaleLimits(id, out min, out max);
        return Mathf.Clamp(scale, min, max);
    }

    public static bool ShouldPlayMeleeShockwave(UnitKind targetKind)
    {
        return targetKind == UnitKind.Tank;
    }

    private static void GetScaleLimits(BattleEffectId id, out float min, out float max)
    {
        switch (id)
        {
            case BattleEffectId.MuzzleRifle:
                min = 0.35f;
                max = 0.58f;
                return;
            case BattleEffectId.MuzzleTank:
                min = 0.28f;
                max = 0.48f;
                return;
            case BattleEffectId.MuzzleAircraft:
                min = 0.22f;
                max = 0.42f;
                return;
            case BattleEffectId.PterosaurFireBreath:
                min = 0.42f;
                max = 0.88f;
                return;
            case BattleEffectId.ShellLaunchSmoke:
                min = 0.24f;
                max = 0.42f;
                return;
            case BattleEffectId.BombDropTrail:
                min = 0.12f;
                max = 0.28f;
                return;
            case BattleEffectId.BulletHitMetal:
            case BattleEffectId.BulletHitDirt:
            case BattleEffectId.ClawHit:
                min = 0.35f;
                max = 0.78f;
                return;
            case BattleEffectId.SoldierDeath:
                min = 0.40f;
                max = 0.72f;
                return;
            case BattleEffectId.ShellExplosionSmall:
            case BattleEffectId.ExplosionSmall:
                min = 0.55f;
                max = 1.05f;
                return;
            case BattleEffectId.ShellImpactMonster:
                min = 0.75f;
                max = 1.25f;
                return;
            case BattleEffectId.MonsterHammerImpact:
            case BattleEffectId.MonsterStompDust:
                min = 0.70f;
                max = 1.15f;
                return;
            case BattleEffectId.MonsterShockwave:
                min = 0.45f;
                max = 0.85f;
                return;
            case BattleEffectId.HumanSummon:
                min = 0.70f;
                max = 1.15f;
                return;
            case BattleEffectId.OrcSummon:
                min = 0.85f;
                max = 1.35f;
                return;
            case BattleEffectId.TankWreckSmoke:
            case BattleEffectId.AircraftCrashSmoke:
                min = 0.55f;
                max = 1.0f;
                return;
            case BattleEffectId.TankDeathExplosion:
                min = 0.85f;
                max = 1.20f;
                return;
            case BattleEffectId.AircraftDeathExplosion:
            case BattleEffectId.AirCrashExplosion:
                min = 0.90f;
                max = 1.25f;
                return;
            case BattleEffectId.BombExplosion:
            case BattleEffectId.ShellExplosionLarge:
            case BattleEffectId.ExplosionLarge:
                min = 0.80f;
                max = 1.30f;
                return;
            case BattleEffectId.MonsterDeathExplosion:
                min = 0.95f;
                max = 1.40f;
                return;
            case BattleEffectId.MonsterDeathDust:
                min = 0.75f;
                max = 1.20f;
                return;
            case BattleEffectId.HumanAirStrikeWarning:
                min = 1.0f;
                max = 1.65f;
                return;
            case BattleEffectId.OrcRageBuff:
                min = 1.0f;
                max = 1.55f;
                return;
            case BattleEffectId.NuclearStrikeWarning:
                min = 1.35f;
                max = 2.2f;
                return;
            case BattleEffectId.NuclearDetonation:
                min = 2.8f;
                max = 5.2f;
                return;
            default:
                min = 0.40f;
                max = 1.20f;
                return;
        }
    }
}
