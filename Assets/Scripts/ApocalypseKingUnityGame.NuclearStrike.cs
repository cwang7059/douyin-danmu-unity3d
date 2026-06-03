using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const float NuclearStrikeRadius = 620f;
    private const float NuclearStrikeUnitDamage = 680f;
    private const float NuclearStrikeGiantDamage = 1200f;
    private const float NuclearStrikeGiantMinHpFraction = 0.38f;
    private const float NuclearStrikeGiantMaxHpFraction = 0.92f;

    private static readonly float[] NuclearRingOffsetX = { 0f, -200f, 200f, -120f, 120f };
    private static readonly float[] NuclearRingOffsetZ = { 0f, 140f, -140f, -95f, 95f };

    private float nuclearStrikeSequenceTimer;
    private float nuclearStrikeCenterX;
    private float nuclearStrikeCenterZ;
    private bool nuclearStrikeDetonated;

    private void UpdateNuclearStrikeSequence(float dt)
    {
        if (!IsNuclearStrikeSequenceActive)
        {
            return;
        }

        if (nuclearStrikePhase == NuclearStrikePhase.InFlight)
        {
            UpdateNuclearWarheadFlight(dt);
            return;
        }

        if (nuclearStrikePhase == NuclearStrikePhase.PostDetonation)
        {
            nuclearWarheadPostVfxTimer = Mathf.Max(0f, nuclearWarheadPostVfxTimer - dt);
            if (nuclearWarheadPostVfxTimer <= 0f)
            {
                ResetNuclearStrikeSequence();
            }
        }
    }

    private void TryBeginNuclearCountdownStrike()
    {
        if (matchPhase != MatchPhase.Battle || ended || IsNuclearStrikeSequenceActive)
        {
            return;
        }

        Vector2 center = GetNuclearStrikeCenter();
        nuclearStrikeCenterX = center.x;
        nuclearStrikeCenterZ = center.y;

        PlayBattleAudio(BattleAudioCueId.ExplosionSmall, nuclearStrikeCenterX, nuclearStrikeCenterZ, 0.12f);
        TriggerCameraShake(0.35f, 0.12f);
        ShowBanner("核武倒计时归零 — 人族城堡发射战术核弹", true, 1.6f);
        BeginNuclearWarheadFlight();
    }

    private void DetonateScheduledNuclearStrike()
    {
        Vector2 center = GetNuclearStrikeCenter();
        nuclearStrikeCenterX = center.x;
        nuclearStrikeCenterZ = center.y;
        int defeated = ExecuteNuclearDetonation(nuclearStrikeCenterX, nuclearStrikeCenterZ, 1f, true);
        ResetNuclearCountdown();
        ShowBanner(defeated > 0 ? $"核武降临 — 重创丧尸 {defeated} 只" : "核武降临 — 战场震颤", true, 3.2f);
    }

    private void ResetNuclearCountdown()
    {
        nuclearTimer = matchSettings != null ? matchSettings.NuclearCountdownSeconds : 10f;
    }

    private Vector2 GetNuclearStrikeCenter()
    {
        Vector2 giantCenter = GetActiveGiantCenter();
        if (giantCenter != Vector2.zero)
        {
            return giantCenter;
        }

        return new Vector2((Left + Right) * 0.5f, 0f);
    }

    private void TriggerTacticalAirStrike(bool resetNuclearCountdown, string bannerText)
    {
        Vector2 center = GetNuclearStrikeCenter();
        PlayBattleEffect(BattleEffectId.HumanAirStrikeWarning, center.x, center.y, 0.05f, 1.45f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.ExplosionLarge, center.x, center.y, 0.35f, 1.15f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, center.x, center.y, 0.35f);
        TriggerCameraShake(0.85f, 0.22f);

        DamageGiantsInNuclearStrike(center.x, center.y, 360f, 520f);
        if (zombieBase != null)
        {
            zombieBase.ApplyDamage(8000f);
        }

        if (resetNuclearCountdown)
        {
            ResetNuclearCountdown();
        }

        if (!string.IsNullOrEmpty(bannerText))
        {
            ShowBanner(bannerText, true, 2.5f);
        }
    }

    private int ExecuteNuclearDetonation(float centerX, float centerZ, float intensity, bool fullStrike)
    {
        float effectScale = Mathf.Lerp(2.2f, 3.1f, Mathf.Clamp01(intensity));
        TriggerNuclearFlash(1.1f);
        TriggerCameraShake(3.2f, 0.58f);

        PlayBattleEffect(BattleEffectId.NuclearDetonation, centerX, centerZ, 0.42f, effectScale, Quaternion.identity);
        for (int i = 1; i < NuclearRingOffsetX.Length; i++)
        {
            float x = centerX + NuclearRingOffsetX[i];
            float z = centerZ + NuclearRingOffsetZ[i];
            PlayBattleEffect(BattleEffectId.ExplosionLarge, x, z, 0.28f, effectScale * 0.52f, Quaternion.identity);
        }

        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, centerX, centerZ, 0.45f);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, centerX + 40f, centerZ, 0.35f);

        float radius = fullStrike ? NuclearStrikeRadius : 360f;
        float unitDamage = fullStrike ? NuclearStrikeUnitDamage : 380f;
        float giantDamage = fullStrike ? NuclearStrikeGiantDamage : 520f;
        int defeatedGiants = 0;
        defeatedGiants += DamageGiantsInNuclearStrike(centerX, centerZ, radius, giantDamage);
        for (int i = 1; i < NuclearRingOffsetX.Length; i++)
        {
            defeatedGiants += DamageGiantsInNuclearStrike(
                centerX + NuclearRingOffsetX[i],
                centerZ + NuclearRingOffsetZ[i],
                radius * 0.72f,
                giantDamage * 0.82f);
        }

        DamageResolver.DamageAllUnitsInArea(centerX, centerZ, radius, unitDamage);
        ApplyNuclearBaseDamage(centerX, centerZ, fullStrike);
        RefreshHud();
        return defeatedGiants;
    }

    private int DamageGiantsInNuclearStrike(float centerX, float centerZ, float radius, float baseDamage)
    {
        if (radius <= 0f || baseDamage <= 0f)
        {
            return 0;
        }

        int defeated = 0;
        float radiusSq = radius * radius;
        for (int i = 0; i < giants.Count; i++)
        {
            var unit = giants[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            float distanceSq = DistanceSq(centerX, centerZ, unit.x, unit.z);
            if (distanceSq > radiusSq)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distanceSq);
            float pct = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, radius));
            float hpFraction = Mathf.Lerp(NuclearStrikeGiantMinHpFraction, NuclearStrikeGiantMaxHpFraction, pct);
            float scaled = Mathf.Max(baseDamage * (0.55f + pct * 0.65f), unit.maxHp * hpFraction);
            float previousHp = unit.hp;
            unit.hp = Mathf.Max(0f, previousHp - scaled);
            unit.hitFlashTimer = Mathf.Max(unit.hitFlashTimer, 0.14f);
            if (unit.hp > 0f)
            {
                continue;
            }

            DefeatGiant(unit);
            defeated++;
        }

        return defeated;
    }

    private void ApplyNuclearBaseDamage(float centerX, float centerZ, bool fullStrike)
    {
        if (!fullStrike)
        {
            return;
        }

        if (zombieBase != null)
        {
            zombieBase.ApplyDamage(18000f);
        }

        if (greenBase != null)
        {
            greenBase.ApplyDamage(9000f);
        }

        if (blueBase != null)
        {
            float dist = Distance(centerX, centerZ, blueBase.WorldX, blueBase.WorldZ);
            float falloff = Mathf.Clamp01(1f - dist / 900f);
            blueBase.ApplyDamage(3500f * falloff);
        }
    }
}
