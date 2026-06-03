using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void UpdateHumans(float dt)
    {
        for (int i = 0; i < soldiers.Count; i++)
        {
            UpdateHumanUnit(soldiers[i], dt);
        }

        for (int i = 0; i < tanks.Count; i++)
        {
            UpdateHumanUnit(tanks[i], dt);
        }

        for (int i = 0; i < aircraft.Count; i++)
        {
            UpdateHumanUnit(aircraft[i], dt);
        }
    }

    private void UpdateHumanUnit(BattleUnit unit, float dt)
    {
        if (!unit.active)
        {
            return;
        }

        var target = FindNearestEnemy(unit, true);
        bool siegeCastle = IsEnemyCastleInSiegeRange(unit);
        bool engageEnemy = CanHumanUnitEngageEnemy(unit, target);

        unit.animTimer += dt;
        unit.attackCooldown = Mathf.Max(0f, unit.attackCooldown - dt);
        unit.attackVisualTimer = Mathf.Max(0f, unit.attackVisualTimer - dt);

        float previousX = unit.x;
        float previousZ = unit.z;

        if (engageEnemy && target != null)
        {
            float dx = target.x - unit.x;
            float dz = target.z - unit.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            bool canFire = distance <= unit.attackRange + target.radius * 0.55f;
            if (unit.kind == UnitKind.Aircraft)
            {
                canFire = canFire && distance <= AircraftBombDropRadius;
            }

            if (canFire && unit.attackCooldown <= 0f)
            {
                FireHumanWeapon(unit, target);
            }
        }
        else if (siegeCastle)
        {
            unit.runtimeState = UnitRuntimeState.Attacking;
        }

        bool tankAnchored = unit.kind == UnitKind.Tank && engageEnemy && target != null
            && Mathf.Sqrt(DistanceSq(unit.x, unit.z, target.x, target.z)) <= unit.attackRange + target.radius * 0.55f;
        float nextX = unit.x;
        float nextZ = unit.z;
        bool aircraftHoveringTarget = unit.kind == UnitKind.Aircraft && engageEnemy && target != null;
        if (aircraftHoveringTarget)
        {
            float jitterX = (Noise(unit.id * 0.37f + unit.rank * 1.9f) - 0.5f) * AircraftBombHoverJitterX;
            float jitterZ = (Noise(unit.id * 0.43f + unit.rank * 2.1f) - 0.5f) * AircraftBombHoverJitterZ;
            float desiredX = target.x + jitterX;
            float desiredZ = target.z + jitterZ;
            float step = unit.speed * dt;
            nextX = unit.x + Mathf.Sign(desiredX - unit.x) * Mathf.Min(step, Mathf.Abs(desiredX - unit.x));
            nextZ = unit.z + Mathf.Sign(desiredZ - unit.z) * Mathf.Min(step, Mathf.Abs(desiredZ - unit.z));
        }
        else if (!tankAnchored)
        {
            float desiredX = HumanHoldX(unit, target, engageEnemy);
            float holdDeltaX = desiredX - unit.x;
            if (Mathf.Abs(holdDeltaX) > 0.5f)
            {
                float stepX = unit.speed * dt;
                nextX = unit.x + Mathf.Sign(holdDeltaX) * Mathf.Min(stepX, Mathf.Abs(holdDeltaX));
            }
        }

        if (!aircraftHoveringTarget && unit.kind == UnitKind.Aircraft)
        {
            float desiredZ = HumanHoldZ(unit);
            nextZ = desiredZ + Mathf.Sin(battleTime * 2.1f + unit.seed * 9f) * 13f;
        }
        else if (!aircraftHoveringTarget && !tankAnchored)
        {
            float desiredZ = HumanHoldZ(unit);
            nextZ = unit.z + (desiredZ - unit.z) * dt * 0.45f;
        }

        float maxMoveStep = unit.speed * dt * 1.35f;
        MoveUnitToAvoidingBuildings(unit, nextX, nextZ, maxMoveStep);
        unit.x = Mathf.Clamp(unit.x, HumanCastleMinUnitX - 40f, BeastCastleMaxUnitX + 40f);

        RecordUnitMovement(unit, previousX, previousZ, dt);
        if (engageEnemy && target != null)
        {
            UpdateHumanFacing(unit, target, dt);
        }
        else
        {
            AimUnitTowardCastle(unit, dt);
        }

        RefreshRuntimeStateFromMovement(unit);
        UpdateUnitTransform(unit, dt);
    }

    private float HumanHoldX(BattleUnit unit, BattleUnit target, bool engageEnemy)
    {
        float stagger = (Noise(unit.id * 0.37f + unit.rank * 1.9f) - 0.5f) * 40f;
        bool hasSiegePoint = TryGetEnemyCastleSiegePoint(unit, out float siegeX, out _);
        float desiredX = hasSiegePoint ? siegeX + stagger : unit.x + stagger;

        if (target != null && engageEnemy)
        {
            float gap = HumanEngagementGap(unit.kind);
            float combatX = (unit.facing >= 0 ? target.x - gap : target.x + gap) + stagger;
            if (hasSiegePoint)
            {
                // 只向城堡方向推进：交战站位不得落在身后，避免双方在中场对顶不前
                if (unit.facing >= 0)
                {
                    desiredX = Mathf.Min(Mathf.Max(combatX, unit.x), siegeX + stagger);
                }
                else
                {
                    desiredX = Mathf.Max(Mathf.Min(combatX, unit.x), siegeX + stagger);
                }
            }
            else
            {
                desiredX = combatX;
            }
        }

        float minX = Left + 58f;
        float maxX = Right - 48f;
        if (unit.kind == UnitKind.Tank)
        {
            minX = Left - 76f;
            maxX = Right - 160f;
        }

        return Mathf.Clamp(desiredX, minX, maxX);
    }

    private void AimUnitTowardCastle(BattleUnit unit, float dt)
    {
        if (unit == null || !TryGetEnemyCastleSiegePoint(unit, out float siegeX, out float siegeZ))
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(siegeX - unit.x, siegeZ - unit.z, unit.headingDegrees);
        float turnRate = unit.kind == UnitKind.Tank ? 8.5f : unit.kind == UnitKind.Aircraft ? 5.8f : 7.5f;
        unit.headingDegrees = Mathf.LerpAngle(unit.headingDegrees, aimYaw, Mathf.Clamp01(dt * turnRate));
        unit.turretYawDegrees = unit.headingDegrees;
        unit.facing = DirectionFromYaw(unit.headingDegrees).x >= 0f ? 1 : -1;
    }

    private float HumanEngagementGap(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return GiantMeleeOffset(kind);
            case UnitKind.Tank:
                return 96f;
            case UnitKind.Soldier:
                return 142f;
            default:
                return GiantMeleeOffset(kind);
        }
    }

    private float HumanHoldZ(BattleUnit unit)
    {
        float wave = unit.kind == UnitKind.Aircraft ? 0f : Mathf.Sin(battleTime * 1.7f + unit.seed * 6f) * 5f;
        if (TryGetEnemyCastleSiegePoint(unit, out _, out float siegeZ))
        {
            float anchor = unit.kind == UnitKind.Aircraft ? unit.baseZ : unit.baseZ + wave;
            return Mathf.Lerp(anchor, siegeZ, unit.kind == UnitKind.Aircraft ? 0.22f : 0.38f);
        }

        return unit.baseZ + wave;
    }

    private void UpdateHumanFacing(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        switch (unit.kind)
        {
            case UnitKind.Tank:
                UpdateTankAiming(unit, target, dt);
                break;
            case UnitKind.Soldier:
                UpdateSoldierAiming(unit, target, dt);
                break;
            case UnitKind.Aircraft:
                UpdateAircraftAiming(unit, target, dt);
                break;
        }
    }

    private void UpdateTankAiming(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.headingDegrees);
        bool anchored = unit.moveSpeed < 0.45f;
        float hullTurnRate = Mathf.Clamp01(dt * (anchored ? 10.5f : 7.2f));
        float turretTurnRate = Mathf.Clamp01(dt * (anchored ? 11.5f : 8.4f));
        unit.headingDegrees = Mathf.LerpAngle(unit.headingDegrees, aimYaw, hullTurnRate);
        unit.turretYawDegrees = Mathf.LerpAngle(unit.turretYawDegrees, aimYaw, turretTurnRate);
        unit.facing = DirectionFromYaw(unit.headingDegrees).x >= 0f ? 1 : -1;
    }

    private void UpdateSoldierAiming(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.headingDegrees);
        float turnRate = unit.attackVisualTimer > 0f ? 13f : 8.5f;
        unit.headingDegrees = Mathf.LerpAngle(unit.headingDegrees, aimYaw, Mathf.Clamp01(dt * turnRate));
        unit.turretYawDegrees = unit.headingDegrees;
        unit.facing = DirectionFromYaw(unit.headingDegrees).x >= 0f ? 1 : -1;
    }

    private void UpdateAircraftAiming(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.headingDegrees);
        unit.headingDegrees = Mathf.LerpAngle(unit.headingDegrees, aimYaw, Mathf.Clamp01(dt * 5.8f));
        unit.turretYawDegrees = unit.headingDegrees;
        unit.facing = DirectionFromYaw(unit.headingDegrees).x >= 0f ? 1 : -1;
    }

    private void FireHumanWeapon(BattleUnit unit, BattleUnit target)
    {
        unit.runtimeState = UnitRuntimeState.Attacking;
        unit.attackCooldown = unit.attackInterval * (0.9f + Noise(battleTime * 31f + unit.id) * 0.22f);
        unit.attackVisualTimer = unit.kind == UnitKind.Soldier ? 0.18f : 0.42f;

        if (target.kind != UnitKind.Giant)
        {
            ApplyDirectUnitDamage(unit, target, unit.damage);
            return;
        }

        float scaledDamage = ScaleOutgoingDamage(unit, target, unit.damage);
        Vector2 aim = DirectionTo(unit.x, unit.z, target.x, target.z, unit.turretYawDegrees);

        if (unit.kind == UnitKind.Soldier)
        {
            Vector2 muzzleAim = DirectionFromYaw(unit.turretYawDegrees);
            Vector2 muzzle = SoldierMuzzlePoint(unit, muzzleAim);
            PlayBattleEffect(BattleEffectId.MuzzleRifle, muzzle.x, muzzle.y, 1.04f, 0.55f, RotationFromDirection(muzzleAim));
            PlayBattleAudio(BattleAudioCueId.RifleShot, muzzle.x, muzzle.y, 1.02f);
            SpawnProjectile(ProjectileKind.Bullet, ProjectileTarget.Giant, muzzle.x, muzzle.y, 1.05f, target.x - aim.x * 24f, target.z - aim.y * 24f, 1.9f, scaledDamage, 0f, 760f, new Color(1f, 0.82f, 0.32f, 1f));
            return;
        }

        if (unit.kind == UnitKind.Tank)
        {
            Vector2 muzzle = TankMuzzlePoint(unit);
            Vector2 barrelAim = DirectionFromYaw(unit.turretYawDegrees);
            PlayBattleEffect(BattleEffectId.MuzzleTank, muzzle.x, muzzle.y, 0.78f, 0.40f, RotationFromDirection(barrelAim));
            PlayBattleEffect(BattleEffectId.ShellLaunchSmoke, muzzle.x, muzzle.y, 0.72f, 0.34f, RotationFromDirection(barrelAim));
            PlayBattleAudio(BattleAudioCueId.TankShot, muzzle.x, muzzle.y, 0.82f);
            TriggerCameraShake(0.08f, 0.035f);
            SpawnProjectile(ProjectileKind.Shell, ProjectileTarget.Giant, muzzle.x, muzzle.y, 0.82f, target.x - barrelAim.x * 24f, target.z - barrelAim.y * 24f, 2.35f, scaledDamage, 52f, 520f, new Color(0.58f, 0.56f, 0.52f, 0.9f));
            return;
        }

        TryGetUnitBodyLaunchLogical(unit, out _, out _, out float launchHeight);
        launchHeight = Mathf.Max(AircraftDefaultAltitude, launchHeight);
        float dropHeight = Mathf.Max(2.5f, launchHeight - 0.15f);
        SpawnProjectile(
            ProjectileKind.Bomb,
            ProjectileTarget.Giant,
            target.x,
            target.z,
            dropHeight,
            target.x,
            target.z,
            0.18f,
            scaledDamage,
            76f,
            AircraftBombProjectileSpeed,
            AircraftBombVisualColor);
    }

    private bool CanHumanUnitEngageEnemy(BattleUnit unit, BattleUnit target)
    {
        if (unit == null || target == null)
        {
            return false;
        }

        if (unit.kind == UnitKind.Aircraft)
        {
            float weaponRange = unit.attackRange + target.radius * 0.55f;
            return DistanceSq(unit.x, unit.z, target.x, target.z) <= weaponRange * weaponRange;
        }

        return IsUnitInCastleAggro(unit, target);
    }

    private void UpdateGiants(float dt)
    {
        for (int i = 0; i < giants.Count; i++)
        {
            UpdateGiantUnit(giants[i], dt);
        }
    }

    private void UpdateGiantUnit(BattleUnit giant, float dt)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        giant.animTimer += dt;
        giant.attackCooldown = Mathf.Max(0f, giant.attackCooldown - dt);
        giant.attackVisualTimer = Mathf.Max(0f, giant.attackVisualTimer - dt);
        giant.hitFlashTimer = Mathf.Max(0f, giant.hitFlashTimer - dt);

        var chaseTarget = FindNearestHumanGroundEnemy(giant);
        var contactTarget = FindGiantContactTarget(giant);
        var engagementTarget = contactTarget ?? FindGiantEngagementTarget(giant);
        float rage = giant.hp / giant.maxHp < 0.45f ? 1.22f : 1f;
        float configuredSpeed = giantConfig != null ? giantConfig.MoveSpeed : 42f;
        float baseGiantSpeed = Mathf.Max(configuredSpeed, giant.baseSpeed);
        giant.speed = baseGiantSpeed * rage;
        float previousX = giant.x;
        float previousZ = giant.z;

        float dx = 0f;
        float dz = 0f;
        var faceTarget = ResolveGiantFaceTarget(contactTarget, engagementTarget, chaseTarget);
        if (contactTarget == null)
        {
            float goalX = giant.x;
            float goalZ = giant.z;
            bool marchCastle = TryGetEnemyCastleSiegePoint(giant, out float siegeX, out float siegeZ);
            if (marchCastle)
            {
                goalX = siegeX;
                goalZ = siegeZ;
                if (giant.facing >= 0)
                {
                    goalX = Mathf.Min(Mathf.Max(goalX, giant.x), siegeX);
                }
                else
                {
                    goalX = Mathf.Max(Mathf.Min(goalX, giant.x), siegeX);
                }
            }
            else if (chaseTarget != null)
            {
                goalX = chaseTarget.x;
                goalZ = chaseTarget.z;
            }

            if (marchCastle || chaseTarget != null)
            {
                float formationZ = marchCastle
                    ? Mathf.Clamp(goalZ, Bottom + 62f, Top - 88f)
                    : Mathf.Clamp(goalZ + GiantFormationZOffset(giant), Bottom + 62f, Top - 88f);
                Vector2 chase = DirectionTo(giant.x, giant.z, goalX, formationZ, giant.headingDegrees);
                float nextX = giant.x + chase.x * giant.speed * dt;
                float nextZ = giant.z + chase.y * giant.speed * dt;
                dx = nextX - giant.x;
                dz = nextZ - giant.z;
                MoveUnitToAvoidingBuildings(giant, nextX, nextZ);
            }
        }

        UpdateGiantHeadingTowardHumans(giant, faceTarget, dt);

        if (engagementTarget != null && giant.attackCooldown <= 0f)
        {
            PerformGiantMeleeAttack(giant, engagementTarget);
        }

        RecordUnitMovement(giant, previousX, previousZ, dt);
        RefreshRuntimeStateFromMovement(giant);
        UpdateUnitTransform(giant, dt);
    }

    private void UpdateGiantHeadingTowardHumans(BattleUnit giant, BattleUnit faceTarget, float dt)
    {
        if (giant == null)
        {
            return;
        }

        float aimYaw;
        if (faceTarget != null)
        {
            aimYaw = DirectionYawDegrees(faceTarget.x - giant.x, faceTarget.z - giant.z, giant.headingDegrees);
        }
        else if (TryGetEnemyCastleSiegePoint(giant, out float siegeX, out float siegeZ))
        {
            aimYaw = DirectionYawDegrees(siegeX - giant.x, siegeZ - giant.z, giant.headingDegrees);
        }
        else
        {
            aimYaw = DirectionYawDegrees(HumanCastleGateX - giant.x, HumanCastleCenterZ - giant.z, giant.headingDegrees);
        }

        giant.headingDegrees = Mathf.LerpAngle(giant.headingDegrees, aimYaw, Mathf.Clamp01(dt * 9f));
        Vector2 facingDir = DirectionFromYaw(giant.headingDegrees);
        giant.facing = facingDir.x >= 0f ? 1 : -1;
    }

    private float GiantFormationZOffset(BattleUnit giant)
    {
        if (giant == null)
        {
            return 0f;
        }

        int lane = giant.rank % BeastGiantLanesPerRow;
        int rank = giant.rank / BeastGiantLanesPerRow;
        return (lane - 1) * 12f + rank * 8f;
    }

    private float GiantMeleeOffset(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return 76f;
            case UnitKind.Tank:
                return 104f;
            default:
                return 82f;
        }
    }

    private float GiantMeleeXReach(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 26f : 42f;
            case UnitKind.Tank:
                return contactOnly ? 24f : 40f;
            default:
                return contactOnly ? 18f : 32f;
        }
    }

    private float GiantMeleeZReach(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 760f : 800f;
            case UnitKind.Tank:
                return contactOnly ? 360f : 400f;
            default:
                return contactOnly ? 180f : 220f;
        }
    }

    private float GiantMeleeDistance(UnitKind kind, bool contactOnly)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return contactOnly ? 168f : 214f;
            case UnitKind.Tank:
                return contactOnly ? 212f : 252f;
            default:
                return contactOnly ? 98f : 132f;
        }
    }

    private void PerformGiantSmash(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.runtimeState = UnitRuntimeState.Attacking;
        giant.attackCooldown = giant.attackInterval * (giant.hp / giant.maxHp < 0.45f ? 0.78f : 1f);
        giant.attackVisualTimer = 0.58f;

        float impactX = Mathf.Min(giant.x - 62f, target.x + 16f);
        float impactZ = target.z;
        PlayBattleEffect(BattleEffectId.MonsterHammerImpact, impactX, impactZ, 0.18f, 1.05f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterShockwave, impactX, impactZ, 0.08f, 0.72f, Quaternion.identity);
        ApplyAreaDamageToHumans(impactX, impactZ, 162f, giant.damage, true, 44f);
        ShowBanner("丧尸猛击", true, 0.95f);
    }

    private void PerformGiantMeleeAttack(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.runtimeState = UnitRuntimeState.Attacking;
        giant.attackCooldown = giant.attackInterval * (giant.hp / giant.maxHp < 0.45f ? 0.72f : 0.92f);
        giant.attackVisualTimer = 0.66f;

        var contactTarget = FindGiantContactTarget(giant);
        var visualTarget = contactTarget ?? target;
        Vector2 attackDir = DirectionTo(giant.x, giant.z, target.x, target.z, giant.headingDegrees);
        float impactX;
        float impactZ;
        if (contactTarget != null)
        {
            impactX = visualTarget.x - attackDir.x * 10f;
            impactZ = visualTarget.z - attackDir.y * 10f;
        }
        else
        {
            float whiffDistance = GiantMeleeDistance(target.kind, true) * 0.92f;
            impactX = giant.x + attackDir.x * whiffDistance;
            impactZ = giant.z + attackDir.y * whiffDistance;
        }

        BattleEffectId impactEffect = target.kind == UnitKind.Tank
            ? BattleEffectId.MonsterHammerImpact
            : target.kind == UnitKind.Aircraft
                ? BattleEffectId.ClawHit
                : BattleEffectId.MonsterStompDust;
        PlayBattleEffect(impactEffect, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.4f : 0.16f, target.kind == UnitKind.Tank ? 1.05f : 0.82f, Quaternion.identity);
        if (BattleEffectTuning.ShouldPlayMeleeShockwave(target.kind))
        {
            PlayBattleEffect(BattleEffectId.MonsterShockwave, impactX, impactZ, 0.08f, 0.68f, Quaternion.identity);
        }

        PlayBattleAudio(BattleAudioCueId.CreatureHit, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.2f : 0.2f);
        TriggerCameraShake(target.kind == UnitKind.Tank ? 0.20f : 0.14f, target.kind == UnitKind.Tank ? 0.13f : 0.08f);
        ApplyGiantContactDamage(giant);
        ShowBanner(target.kind == UnitKind.Aircraft ? "丧尸拍落" : target.kind == UnitKind.Tank ? "丧尸重击" : "丧尸践踏", true, 0.85f);
    }

    private void ThrowGiantRock(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.runtimeState = UnitRuntimeState.Attacking;
        giant.attackCooldown = giant.attackInterval * 1.15f;
        giant.attackVisualTimer = 0.45f;
        SpawnProjectile(ProjectileKind.Rock, ProjectileTarget.Human, giant.x - 70f, giant.z + 128f, 4.6f, target.x, target.z + 8f, 0.75f, 116f, 76f, 470f, new Color(0.72f, 1f, 0.52f, 1f));
    }

    private void DamageBuildingsInArea(float x, float z, float radius, float damage)
    {
        if (buildingObstacles.Count == 0 || radius <= 0f || damage <= 0f)
        {
            return;
        }

        for (int i = 0; i < buildingObstacles.Count; i++)
        {
            var obstacle = buildingObstacles[i];
            if (obstacle == null || obstacle.Destroyed)
            {
                continue;
            }

            float dx = Mathf.Max(0f, Mathf.Abs(x - obstacle.CenterX) - obstacle.HalfX);
            float dz = Mathf.Max(0f, Mathf.Abs(z - obstacle.CenterZ) - obstacle.HalfZ);
            float distanceSq = dx * dx + dz * dz;
            if (distanceSq > radius * radius)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distanceSq);
            float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, radius));
            obstacle.Hp -= damage * Mathf.Lerp(0.45f, 1.15f, falloff);
            if (obstacle.Hp <= 0f)
            {
                DestroyBuildingObstacle(obstacle);
            }
        }
    }

    private void DestroyBuildingObstacle(BuildingObstacle obstacle)
    {
        if (obstacle == null || obstacle.Destroyed)
        {
            return;
        }

        obstacle.Destroyed = true;
        if (obstacle.Root != null)
        {
            obstacle.Root.SetActive(false);
        }

        float rubbleWidth = Mathf.Max(0.55f, obstacle.HalfX * 2f * LogicalToWorld * 0.58f);
        float rubbleDepth = Mathf.Max(0.55f, obstacle.HalfZ * 2f * LogicalToWorld * 0.58f);
        var rubble = CreatePrimitive(PrimitiveType.Cube, obstacle.Name + "_Rubble", decorRoot);
        rubble.transform.localPosition = ToWorldPoint(obstacle.CenterX, obstacle.CenterZ, 0.05f);
        rubble.transform.localScale = new Vector3(rubbleWidth, 0.10f, rubbleDepth);
        rubble.transform.localRotation = Quaternion.Euler(0f, Noise(obstacle.CenterX * 0.17f + obstacle.CenterZ * 0.31f) * 180f - 90f, 0f);
        rubble.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(new Color(0.20f, 0.18f, 0.15f, 1f));

        float effectScale = Mathf.Clamp((obstacle.HalfX + obstacle.HalfZ) / 95f, 0.75f, 1.15f);
        PlayBattleEffect(BattleEffectId.ShellExplosionLarge, obstacle.CenterX, obstacle.CenterZ, 0.28f, effectScale, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.TankWreckSmoke, obstacle.CenterX, obstacle.CenterZ, 0.22f, effectScale * 0.72f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, obstacle.CenterX, obstacle.CenterZ, 0.18f);
        TriggerCameraShake(0.16f, 0.08f);
    }

    private void DeactivateHumanUnit(BattleUnit unit)
    {
        if (!unit.active)
        {
            return;
        }

        unit.active = false;
        unit.runtimeState = UnitRuntimeState.Dead;
        unit.root.SetActive(false);
        humanLosses++;

        switch (unit.kind)
        {
            case UnitKind.Tank:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.TankDeathExplosion, unit.x, unit.z, 0.35f, 1.05f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.TankWreckSmoke, unit.x, unit.z, 0.25f, 0.82f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 0.35f);
                TriggerCameraShake(0.22f, 0.15f);
                break;
            case UnitKind.Aircraft:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.AircraftDeathExplosion, unit.x, unit.z, 2.45f, 1.12f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.AircraftCrashSmoke, unit.x, unit.z, 1.2f, 0.78f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 2.2f);
                TriggerCameraShake(0.18f, 0.12f);
                break;
            default:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.SoldierDeath, unit.x, unit.z, 0.08f, 0.58f, Quaternion.identity);
                break;
        }
    }

    private void DefeatGiant(BattleUnit giant)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        giant.hp = 0f;
        giant.active = false;
        giant.runtimeState = UnitRuntimeState.Dead;
        giant.root.SetActive(false);
        SpawnDeathVisual(giant);
        PlayBattleEffect(BattleEffectId.MonsterDeathExplosion, giant.x, giant.z + 48f, 0.55f, 1.22f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterDeathDust, giant.x, giant.z + 12f, 0.22f, 0.95f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, giant.x, giant.z, 0.4f);
        TriggerCameraShake(0.32f, 0.24f);
        if (CountActive(giants) <= 0)
        {
            ended = true;
            ShowBanner("Humans win", true, 4f);
        }

        RefreshHud();
    }

    private int CountUnitsInRuntimeState(UnitRuntimeState state)
    {
        return CountRuntimeStateInList(soldiers, state)
            + CountRuntimeStateInList(tanks, state)
            + CountRuntimeStateInList(aircraft, state)
            + CountRuntimeStateInList(giants, state);
    }

    private static int CountRuntimeStateInList(List<BattleUnit> units, UnitRuntimeState state)
    {
        if (units == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < units.Count; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null && unit.active && unit.runtimeState == state)
            {
                total++;
            }
        }

        return total;
    }

    private static bool ShouldPresentUnitAsAttacking(BattleUnit unit)
    {
        return unit != null && unit.active && unit.runtimeState == UnitRuntimeState.Attacking;
    }

    private static void RefreshRuntimeStateFromMovement(BattleUnit unit)
    {
        if (unit == null || !unit.active || unit.runtimeState == UnitRuntimeState.Dead)
        {
            return;
        }

        if (unit.attackVisualTimer > 0f)
        {
            unit.runtimeState = UnitRuntimeState.Attacking;
            return;
        }

        float moveThreshold = unit.kind == UnitKind.Giant ? 0.12f : 0.5f;
        unit.runtimeState = unit.moveSpeed > moveThreshold ? UnitRuntimeState.Moving : UnitRuntimeState.Idle;
    }
}

