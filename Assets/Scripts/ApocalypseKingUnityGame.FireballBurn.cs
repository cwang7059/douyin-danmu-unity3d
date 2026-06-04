using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const float PterosaurFireballBurnDuration = 4.5f;
    private const float PterosaurFireballBurnTickInterval = 0.55f;
    private const float PterosaurFireballBurnDamageRatio = 0.22f;
    private const float PterosaurFireballMinFlightSeconds = 20f;
    private const float PterosaurFireballMaxFlightSecondsCap = 32f;
    private const float PterosaurFireballFlightSpeed = 165f;
    private const float PterosaurFireballHitRadius = 28f;
    private const float PterosaurFireballRetargetRadius = 480f;
    private const float PterosaurFireballNearGoalDetonateSeconds = 0.28f;

    private BattleUnit FindBattleUnitById(int unitId)
    {
        if (unitId < 0)
        {
            return null;
        }

        BattleUnit unit = FindUnitByIdInList(soldiers, unitId);
        if (unit != null)
        {
            return unit;
        }

        unit = FindUnitByIdInList(tanks, unitId);
        if (unit != null)
        {
            return unit;
        }

        return FindUnitByIdInList(aircraft, unitId);
    }

    private static BattleUnit FindUnitByIdInList(List<BattleUnit> units, int unitId)
    {
        if (units == null)
        {
            return null;
        }

        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null && unit.id == unitId)
            {
                return unit;
            }
        }

        return null;
    }

    private void TickBurnStatuses(float dt)
    {
        TickGroupBurn(soldiers, dt);
        TickGroupBurn(tanks, dt);
        TickGroupBurn(aircraft, dt);
    }

    private void TickGroupBurn(List<BattleUnit> units, float dt)
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.active || unit.burnTimer <= 0f)
            {
                continue;
            }

            unit.burnTimer -= dt;
            unit.burnTickTimer -= dt;
            unit.hitFlashTimer = Mathf.Max(unit.hitFlashTimer, 0.1f);
            if (unit.burnTickTimer <= 0f && unit.burnDamagePerTick > 0f)
            {
                unit.burnTickTimer = PterosaurFireballBurnTickInterval;
                ApplyBurnTickDamage(unit, unit.burnDamagePerTick);
                if (unit.id % 2 == 0)
                {
                    PlayBattleEffect(
                        BattleEffectId.PterosaurFireballBurn,
                        unit.x,
                        unit.z,
                        unit.kind == UnitKind.Aircraft ? Mathf.Max(1.8f, unit.altitude * 0.45f) : 0.35f,
                        unit.kind == UnitKind.Tank ? 0.55f : 0.45f,
                        Quaternion.identity);
                }
            }

            if (unit.burnTimer <= 0f)
            {
                unit.burnDamagePerTick = 0f;
                unit.burnTickTimer = 0f;
            }
        }
    }

    private void ApplyFireballBurn(BattleUnit unit, float impactDamage)
    {
        if (unit == null || !unit.active || unit.kind == UnitKind.Giant)
        {
            return;
        }

        float tickDamage = Mathf.Max(2f, impactDamage * PterosaurFireballBurnDamageRatio);
        unit.burnTimer = Mathf.Max(unit.burnTimer, PterosaurFireballBurnDuration);
        unit.burnDamagePerTick = Mathf.Max(unit.burnDamagePerTick, tickDamage);
        unit.burnTickTimer = Mathf.Min(unit.burnTickTimer, 0.12f);
        unit.hitFlashTimer = Mathf.Max(unit.hitFlashTimer, 0.18f);
        PlayBattleEffect(
            BattleEffectId.PterosaurFireballBurn,
            unit.x,
            unit.z,
            unit.kind == UnitKind.Aircraft ? Mathf.Max(2f, unit.altitude * 0.5f) : 0.42f,
            0.85f,
            Quaternion.identity);
    }

    private void ApplyFireballBurnAt(float x, float z, float impactDamage, int directHitUnitId)
    {
        if (directHitUnitId >= 0)
        {
            BattleUnit direct = FindBattleUnitById(directHitUnitId);
            if (direct != null && direct.active)
            {
                ApplyFireballBurn(direct, impactDamage);
            }
        }

        ApplyFireballBurnToHumansInRadius(x, z, PterosaurFireballHitRadius * 0.85f, impactDamage, directHitUnitId);
    }

    private void ApplyFireballBurnToHumansInRadius(float x, float z, float radius, float impactDamage, int skipUnitId)
    {
        float radiusSq = radius * radius;
        ApplyFireballBurnInList(soldiers, x, z, radiusSq, impactDamage, skipUnitId);
        ApplyFireballBurnInList(tanks, x, z, radiusSq, impactDamage, skipUnitId);
        ApplyFireballBurnInList(aircraft, x, z, radiusSq, impactDamage, skipUnitId);
    }

    private void ApplyFireballBurnInList(List<BattleUnit> units, float x, float z, float radiusSq, float impactDamage, int skipUnitId)
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.active || unit.id == skipUnitId)
            {
                continue;
            }

            if (DistanceSq(x, z, unit.x, unit.z) <= radiusSq)
            {
                ApplyFireballBurn(unit, impactDamage);
            }
        }
    }

    private float ComputeFireballMaxFlightSeconds(float fromX, float fromZ, float goalX, float goalZ)
    {
        float travel = Distance(fromX, fromZ, goalX, goalZ);
        float seconds = travel / PterosaurFireballFlightSpeed + 12f;
        return Mathf.Clamp(seconds, PterosaurFireballMinFlightSeconds, PterosaurFireballMaxFlightSecondsCap);
    }

    private void InitializeFireballFlight(ProjectileView shot, float goalX, float goalZ, float goalHeight)
    {
        shot.fireballGoalX = goalX;
        shot.fireballGoalZ = goalZ;
        shot.fireballGoalHeight = goalHeight;
        shot.fireballNearGoalTime = 0f;
        shot.fireballMaxFlightTime = ComputeFireballMaxFlightSeconds(shot.fromX, shot.fromZ, goalX, goalZ);
    }

    private bool TryResolveFireballHomingTarget(ProjectileView shot, out float goalX, out float goalZ, out float goalHeight)
    {
        BattleUnit target = FindBattleUnitById(shot.homingTargetId);
        if (target != null && target.active)
        {
            shot.fireballGoalX = target.x;
            shot.fireballGoalZ = target.z;
            shot.fireballGoalHeight = Mathf.Max(1.2f, target.altitude * 0.55f);
            shot.fireballMaxFlightTime = Mathf.Max(
                shot.fireballMaxFlightTime,
                ComputeFireballMaxFlightSeconds(shot.fromX, shot.fromZ, shot.fireballGoalX, shot.fireballGoalZ));
            goalX = shot.fireballGoalX;
            goalZ = shot.fireballGoalZ;
            goalHeight = shot.fireballGoalHeight;
            return true;
        }

        if (TryFindNearestHumanForFireballRetarget(
                shot.fireballLogicalX,
                shot.fireballLogicalZ,
                PterosaurFireballRetargetRadius,
                shot.homingTargetId,
                out target))
        {
            shot.homingTargetId = target.id;
            shot.fireballGoalX = target.x;
            shot.fireballGoalZ = target.z;
            shot.fireballGoalHeight = Mathf.Max(1.2f, target.altitude * 0.55f);
            shot.fireballMaxFlightTime = Mathf.Max(
                shot.fireballMaxFlightTime,
                ComputeFireballMaxFlightSeconds(shot.fromX, shot.fromZ, shot.fireballGoalX, shot.fireballGoalZ));
            goalX = shot.fireballGoalX;
            goalZ = shot.fireballGoalZ;
            goalHeight = shot.fireballGoalHeight;
            return true;
        }

        goalX = shot.fireballGoalX;
        goalZ = shot.fireballGoalZ;
        goalHeight = shot.fireballGoalHeight;
        return true;
    }

    private bool TryFindNearestHumanForFireballRetarget(
        float x,
        float z,
        float radius,
        int skipUnitId,
        out BattleUnit nearest)
    {
        nearest = null;
        float radiusSq = radius * radius;
        float bestDistSq = radiusSq;
        TryFindNearestHumanForFireballInList(soldiers, x, z, skipUnitId, ref nearest, ref bestDistSq);
        TryFindNearestHumanForFireballInList(tanks, x, z, skipUnitId, ref nearest, ref bestDistSq);
        TryFindNearestHumanForFireballInList(aircraft, x, z, skipUnitId, ref nearest, ref bestDistSq);
        return nearest != null;
    }

    private static void TryFindNearestHumanForFireballInList(
        List<BattleUnit> units,
        float x,
        float z,
        int skipUnitId,
        ref BattleUnit nearest,
        ref float bestDistSq)
    {
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.active || unit.id == skipUnitId)
            {
                continue;
            }

            float dx = unit.x - x;
            float dz = unit.z - z;
            float distSq = dx * dx + dz * dz;
            if (distSq >= bestDistSq)
            {
                continue;
            }

            bestDistSq = distSq;
            nearest = unit;
        }
    }

    private bool TryFindFireballHumanImpact(Vector2 from, Vector2 to, float radius, out BattleUnit hitUnit, out Vector2 impactPoint)
    {
        hitUnit = null;
        impactPoint = to;
        float bestDistSq = float.PositiveInfinity;
        float hitRadius = Mathf.Max(12f, radius);

        TryFindFireballHumanImpactInList(soldiers, from, to, hitRadius, ref hitUnit, ref impactPoint, ref bestDistSq);
        TryFindFireballHumanImpactInList(tanks, from, to, hitRadius, ref hitUnit, ref impactPoint, ref bestDistSq);
        TryFindFireballHumanImpactInList(aircraft, from, to, hitRadius, ref hitUnit, ref impactPoint, ref bestDistSq);
        return hitUnit != null;
    }

    private void TryFindFireballHumanImpactInList(
        List<BattleUnit> units,
        Vector2 from,
        Vector2 to,
        float hitRadius,
        ref BattleUnit hitUnit,
        ref Vector2 impactPoint,
        ref float bestDistSq)
    {
        float hitRadiusSq = hitRadius * hitRadius;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            Vector2 center = new Vector2(unit.x, unit.z);
            float distSq = DistancePointToSegmentSq(center, from, to);
            if (distSq > hitRadiusSq || distSq >= bestDistSq)
            {
                continue;
            }

            bestDistSq = distSq;
            hitUnit = unit;
            impactPoint = center;
        }
    }

    private void ApplyBurnTickDamage(BattleUnit unit, float amount)
    {
        if (unit == null || !unit.active || amount <= 0f)
        {
            return;
        }

        unit.hp = Mathf.Max(0f, unit.hp - amount);
        unit.hitFlashTimer = Mathf.Max(unit.hitFlashTimer, 0.14f);
        if (unit.hp <= 0f)
        {
            DeactivateHumanUnit(unit);
        }
    }

    private static float DistancePointToSegmentSq(Vector2 point, Vector2 segStart, Vector2 segEnd)
    {
        Vector2 ab = segEnd - segStart;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq <= 0.0001f)
        {
            return (point - segStart).sqrMagnitude;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segStart, ab) / abLenSq);
        Vector2 closest = segStart + ab * t;
        return (point - closest).sqrMagnitude;
    }
}
