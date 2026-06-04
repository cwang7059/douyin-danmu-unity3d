using UnityEngine;

/// <summary>测试：单位阵亡后从原 rank 对应出生点立即补回同型单位，便于持续观战与特效验证。</summary>
public sealed partial class ApocalypseKingUnityGame
{
    private const bool EnableTestRespawnOnUnitDeath = true;

    private int testBattleDeathCount;

    private void ResetTestBattleDeathCounter()
    {
        testBattleDeathCount = 0;
    }

    private void TryTestRespawnAtSpawn(BattleUnit unit)
    {
        if (!EnableTestRespawnOnUnitDeath
            || unit == null
            || matchPhase != MatchPhase.Battle
            || battleTime <= 0f)
        {
            return;
        }

        switch (unit.kind)
        {
            case UnitKind.Soldier:
                TestRespawnSoldier(unit);
                break;
            case UnitKind.Tank:
                TestRespawnTank(unit);
                break;
            case UnitKind.Aircraft:
                TestRespawnAircraft(unit);
                break;
            case UnitKind.Giant:
                TestRespawnGiant(unit);
                break;
        }
    }

    private void TestRespawnSoldier(BattleUnit unit)
    {
        if (unit.rank < 0 || unit.rank >= SoldierCount)
        {
            return;
        }

        int i = unit.rank;
        GetHumanSoldierMassSpawn(i, out float x, out float z);
        unit.combatVariant = UnitCombatVariant.Standard;
        unit.kind = UnitKind.Soldier;
        ActivateUnit(
            unit,
            x,
            z,
            soldierConfig.MaxHp,
            soldierConfig.Damage,
            soldierConfig.MoveSpeed + Noise(i + 73f) * 8f,
            soldierConfig.Radius,
            soldierConfig.AttackRange + Noise(i + 101f) * 34f,
            soldierConfig.AttackInterval + Noise(i + 131f) * 0.22f,
            i,
            1,
            0f);
        unit.headingDegrees = DirectionYawDegrees(
            BeastCastleGateX - x,
            BeastCastleCenterZ - z,
            unit.headingDegrees);
        unit.turretYawDegrees = unit.headingDegrees;
        EnsureUnitModelAttached(unit);
    }

    private void TestRespawnTank(BattleUnit unit)
    {
        bool rocketTruck = unit.combatVariant == UnitCombatVariant.RocketTruck;
        int spawnIndex = rocketTruck ? unit.rank - TankCount : unit.rank;
        if (spawnIndex < 0)
        {
            return;
        }

        if (rocketTruck)
        {
            if (unit.rank < TankCount || unit.rank >= TankCount + RocketTruckCount)
            {
                return;
            }

            GetHumanRocketTruckMassSpawn(spawnIndex, out float rx, out float rz);
            ActivateUnit(
                unit,
                rx,
                rz,
                tankConfig.MaxHp * 0.92f,
                tankConfig.Damage * 1.35f,
                tankConfig.MoveSpeed * RocketTruckMoveSpeedRatio + Noise(unit.rank + 401f) * 2f,
                tankConfig.Radius * 1.05f,
                tankConfig.AttackRange + RocketTruckAttackRangeBonus,
                tankConfig.AttackInterval * 1.15f + Noise(unit.rank + 503f) * 0.25f,
                unit.rank,
                1,
                0f);
            unit.modelYawOffset = RocketTruckMeshYawOffset;
        }
        else
        {
            if (unit.rank < 0 || unit.rank >= TankCount)
            {
                return;
            }

            GetHumanTankMassSpawn(spawnIndex, out float tx, out float tz);
            ActivateUnit(
                unit,
                tx,
                tz,
                tankConfig.MaxHp,
                tankConfig.Damage,
                tankConfig.MoveSpeed + Noise(unit.rank + 401f) * 6f,
                tankConfig.Radius,
                tankConfig.AttackRange,
                tankConfig.AttackInterval + Noise(unit.rank + 503f) * 0.3f,
                unit.rank,
                1,
                0f);
            unit.modelYawOffset = 0f;
        }

        unit.headingDegrees = DirectionYawDegrees(
            BeastCastleGateX - unit.x,
            BeastCastleCenterZ - unit.z,
            unit.headingDegrees);
        unit.turretYawDegrees = unit.headingDegrees;
        EnsureUnitModelAttached(unit);
    }

    private void TestRespawnAircraft(BattleUnit unit)
    {
        if (unit.rank < 0 || unit.rank >= AircraftCount)
        {
            return;
        }

        int i = unit.rank;
        GetHumanAircraftMassSpawn(i, out float x, out float z);
        unit.combatVariant = UnitCombatVariant.Standard;
        unit.kind = UnitKind.Aircraft;
        ActivateUnit(
            unit,
            x,
            z,
            aircraftConfig.MaxHp,
            aircraftConfig.Damage,
            aircraftConfig.MoveSpeed + i * 7f,
            aircraftConfig.Radius,
            aircraftConfig.AttackRange,
            aircraftConfig.AttackInterval + i * 0.12f,
            i,
            1,
            AircraftDefaultAltitude);
    }

    private void TestRespawnGiant(BattleUnit unit)
    {
        if (unit.combatVariant == UnitCombatVariant.RocketGiant)
        {
            if (unit.rank < BaseGiantCount || unit.rank >= GiantCount)
            {
                return;
            }

            int rocketIndex = unit.rank - BaseGiantCount;
            GetRocketGiantMassSpawn(rocketIndex, out float rx, out float rz);
            float range = giantConfig.AttackRange + 240f;
            ActivateUnit(
                unit,
                rx,
                rz,
                giantConfig.MaxHp * 1.05f,
                giantConfig.Damage * 1.2f,
                giantConfig.MoveSpeed + Noise(rocketIndex + 811f) * 5f,
                giantConfig.Radius * 1.08f,
                range,
                giantConfig.AttackInterval * 1.02f + Noise(rocketIndex + 911f) * 0.12f,
                unit.rank,
                -1,
                0f);
            unit.attackCooldown = 1.1f + Noise(rocketIndex + 1011f);
            EnsureUnitModelAttached(unit);
            if (unit.modelInstance != null)
            {
                AttachGiantRocketLauncher(unit.modelInstance);
            }

            return;
        }

        if (unit.rank < 0 || unit.rank >= BaseGiantCount)
        {
            return;
        }

        int i = unit.rank;
        GetGiantMassSpawn(i, out float x, out float z);
        unit.combatVariant = UnitCombatVariant.Standard;
        ActivateUnit(
            unit,
            x,
            z,
            giantConfig.MaxHp,
            giantConfig.Damage,
            giantConfig.MoveSpeed + Noise(i + 207f) * 4f,
            giantConfig.Radius,
            giantConfig.AttackRange,
            giantConfig.AttackInterval + Noise(i + 307f) * 0.18f,
            i,
            -1,
            0f);
        unit.attackCooldown = 2.2f + Noise(i + 907f) * 1.4f;
        EnsureUnitModelAttached(unit);
    }
}
