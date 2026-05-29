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
        var target = FindNearestGiant(unit);
        if (!unit.active || target == null)
        {
            return;
        }

        unit.animTimer += dt;
        unit.attackCooldown = Mathf.Max(0f, unit.attackCooldown - dt);
        unit.attackVisualTimer = Mathf.Max(0f, unit.attackVisualTimer - dt);

        float previousX = unit.x;
        float previousZ = unit.z;
        if (unit.kind == UnitKind.Tank)
        {
            UpdateTankAiming(unit, target, dt);
        }
        else if (unit.kind == UnitKind.Soldier)
        {
            UpdateSoldierAiming(unit, target, dt);
        }
        else if (unit.kind == UnitKind.Aircraft)
        {
            UpdateAircraftAiming(unit, target, dt);
        }

        float dx = target.x - unit.x;
        float dz = target.z - unit.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        bool canFire = distance <= unit.attackRange + target.radius * 0.55f;

        if (canFire && unit.attackCooldown <= 0f)
        {
            FireHumanWeapon(unit, target);
        }

        float desiredX = HumanHoldX(unit, target);
        float nextX = unit.x;
        if (unit.x < desiredX)
        {
            nextX = Mathf.Min(desiredX, unit.x + unit.speed * dt);
        }

        float desiredZ = HumanHoldZ(unit);
        float nextZ = unit.z + (desiredZ - unit.z) * dt * 0.45f;

        if (unit.kind == UnitKind.Aircraft)
        {
            nextZ = desiredZ + Mathf.Sin(battleTime * 2.1f + unit.seed * 9f) * 13f;
        }

        MoveUnitToAvoidingBuildings(unit, nextX, nextZ);
        unit.x = Mathf.Clamp(unit.x, Left - 190f, Right - 48f);
        if (unit.kind == UnitKind.Tank)
        {
            float moveX = unit.x - previousX;
            float moveZ = unit.z - previousZ;
            if (Mathf.Abs(moveX) + Mathf.Abs(moveZ) > 0.01f)
            {
                unit.headingDegrees = DirectionYawDegrees(moveX, moveZ, unit.headingDegrees);
            }
        }

        RecordUnitMovement(unit, previousX, previousZ, dt);
        UpdateUnitTransform(unit, dt);
    }

    private float HumanHoldX(BattleUnit unit, BattleUnit target)
    {
        float gap = HumanEngagementGap(unit.kind);
        return Mathf.Clamp(target.x - gap, Left + 58f, Right - 48f);
    }

    private float HumanEngagementGap(UnitKind kind)
    {
        switch (kind)
        {
            case UnitKind.Aircraft:
                return GiantMeleeOffset(kind);
            case UnitKind.Tank:
                return GiantMeleeOffset(kind);
            default:
                return GiantMeleeOffset(kind);
        }
    }

    private float HumanHoldZ(BattleUnit unit)
    {
        if (unit.kind == UnitKind.Aircraft)
        {
            return unit.baseZ;
        }

        return unit.baseZ + Mathf.Sin(battleTime * 1.7f + unit.seed * 6f) * 5f;
    }

    private void UpdateTankAiming(BattleUnit unit, BattleUnit target, float dt)
    {
        if (unit == null || target == null)
        {
            return;
        }

        float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.turretYawDegrees);
        unit.turretYawDegrees = Mathf.LerpAngle(unit.turretYawDegrees, aimYaw, Mathf.Clamp01(dt * 7.2f));
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
        unit.attackCooldown = unit.attackInterval * (0.9f + Noise(battleTime * 31f + unit.id) * 0.22f);
        unit.attackVisualTimer = unit.kind == UnitKind.Soldier ? 0.18f : 0.42f;

        Vector2 aim = DirectionTo(unit.x, unit.z, target.x, target.z, unit.turretYawDegrees);

        if (unit.kind == UnitKind.Soldier)
        {
            Vector2 muzzleAim = DirectionFromYaw(unit.turretYawDegrees);
            Vector2 muzzle = SoldierMuzzlePoint(unit, muzzleAim);
            PlayBattleEffect(BattleEffectId.MuzzleRifle, muzzle.x, muzzle.y, 1.04f, 1.08f, RotationFromDirection(muzzleAim));
            PlayBattleAudio(BattleAudioCueId.RifleShot, muzzle.x, muzzle.y, 1.02f);
            SpawnProjectile(ProjectileKind.Bullet, ProjectileTarget.Giant, muzzle.x, muzzle.y, 1.05f, target.x - aim.x * 24f, target.z - aim.y * 24f, 1.9f, unit.damage, 0f, 760f, new Color(1f, 0.82f, 0.32f, 1f));
            return;
        }

        if (unit.kind == UnitKind.Tank)
        {
            Vector2 muzzle = TankMuzzlePoint(unit);
            Vector2 barrelAim = DirectionFromYaw(unit.turretYawDegrees);
            PlayBattleEffect(BattleEffectId.MuzzleTank, muzzle.x, muzzle.y, 0.78f, 1.0f, RotationFromDirection(barrelAim));
            PlayBattleEffect(BattleEffectId.ShellLaunchSmoke, muzzle.x, muzzle.y, 0.72f, 1.0f, RotationFromDirection(barrelAim));
            PlayBattleAudio(BattleAudioCueId.TankShot, muzzle.x, muzzle.y, 0.82f);
            TriggerCameraShake(0.08f, 0.035f);
            SpawnProjectile(ProjectileKind.Shell, ProjectileTarget.Giant, muzzle.x, muzzle.y, 0.82f, target.x - barrelAim.x * 24f, target.z - barrelAim.y * 24f, 2.35f, unit.damage, 52f, 520f, new Color(1f, 0.76f, 0.42f, 1f));
            return;
        }

        float bombX = unit.x + aim.x * 22f;
        float bombZ = unit.z + aim.y * 22f;
        PlayBattleEffect(BattleEffectId.MuzzleAircraft, bombX, bombZ, 2.35f, 0.85f, RotationFromDirection(aim));
        PlayBattleEffect(BattleEffectId.BombDropTrail, bombX, bombZ, 2.3f, 0.75f, Quaternion.identity);
        SpawnProjectile(ProjectileKind.Bomb, ProjectileTarget.Giant, bombX, bombZ, 2.35f, target.x, target.z, 0.18f, unit.damage, 76f, 430f, new Color(0.42f, 0.50f, 0.48f, 1f));
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

        var chaseTarget = FindNearestHuman(giant, true);
        var contactTarget = FindGiantContactTarget(giant);
        var engagementTarget = contactTarget ?? FindGiantEngagementTarget(giant);
        float rage = giant.hp / giant.maxHp < 0.45f ? 1.22f : 1f;
        float configuredSpeed = giantConfig != null ? giantConfig.MoveSpeed : 42f;
        float baseGiantSpeed = Mathf.Max(configuredSpeed, giant.baseSpeed);
        giant.speed = baseGiantSpeed * rage;
        float previousX = giant.x;
        float previousZ = giant.z;

        var faceTarget = contactTarget ?? engagementTarget ?? chaseTarget;
        if (faceTarget != null)
        {
            float targetYaw = DirectionYawDegrees(faceTarget.x - giant.x, faceTarget.z - giant.z, giant.headingDegrees);
            giant.headingDegrees = Mathf.LerpAngle(giant.headingDegrees, targetYaw, Mathf.Clamp01(dt * 4.6f));
        }

        if (engagementTarget != null && giant.attackCooldown <= 0f)
        {
            PerformGiantMeleeAttack(giant, engagementTarget);
        }

        if (contactTarget == null && chaseTarget != null)
        {
            float formationZ = Mathf.Clamp(chaseTarget.z + GiantFormationZOffset(giant), Bottom + 62f, Top - 88f);
            Vector2 chase = DirectionTo(giant.x, giant.z, chaseTarget.x, formationZ, giant.headingDegrees);
            MoveUnitToAvoidingBuildings(giant, giant.x + chase.x * giant.speed * dt, giant.z + chase.y * giant.speed * dt);
        }

        RecordUnitMovement(giant, previousX, previousZ, dt);
        UpdateUnitTransform(giant, dt);
    }

    private float GiantFormationZOffset(BattleUnit giant)
    {
        if (giant == null)
        {
            return 0f;
        }

        int lane = giant.rank % 5;
        int rank = giant.rank / 5;
        return (lane - 2) * 42f + rank * 18f;
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

        giant.attackCooldown = giant.attackInterval * (giant.hp / giant.maxHp < 0.45f ? 0.78f : 1f);
        giant.attackVisualTimer = 0.58f;

        float impactX = Mathf.Min(giant.x - 62f, target.x + 16f);
        float impactZ = target.z;
        PlayBattleEffect(BattleEffectId.MonsterHammerImpact, impactX, impactZ + 24f, 0.18f, 1.45f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterShockwave, impactX, impactZ + 24f, 0.08f, 1.1f, Quaternion.identity);
        ApplyAreaDamageToHumans(impactX, impactZ, 162f, giant.damage, true, 44f);
        ShowBanner("Giant smash", true, 0.95f);
    }

    private void PerformGiantMeleeAttack(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

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
        PlayBattleEffect(impactEffect, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.4f : 0.16f, target.kind == UnitKind.Tank ? 1.55f : 1.15f, Quaternion.identity);
        if (target.kind != UnitKind.Aircraft)
        {
            PlayBattleEffect(BattleEffectId.MonsterShockwave, impactX, impactZ, 0.08f, target.kind == UnitKind.Tank ? 1.2f : 0.9f, Quaternion.identity);
        }

        PlayBattleAudio(BattleAudioCueId.CreatureHit, impactX, impactZ, target.kind == UnitKind.Aircraft ? 2.2f : 0.2f);
        TriggerCameraShake(target.kind == UnitKind.Tank ? 0.20f : 0.14f, target.kind == UnitKind.Tank ? 0.13f : 0.08f);
        ApplyGiantContactDamage(giant);
        ShowBanner(target.kind == UnitKind.Aircraft ? "Giant swat" : target.kind == UnitKind.Tank ? "Giant hammer" : "Giant stomp", true, 0.85f);
    }

    private void ThrowGiantRock(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

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

        float effectScale = Mathf.Clamp((obstacle.HalfX + obstacle.HalfZ) / 95f, 0.85f, 1.65f);
        PlayBattleEffect(BattleEffectId.ShellExplosionLarge, obstacle.CenterX, obstacle.CenterZ, 0.28f, effectScale, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.TankWreckSmoke, obstacle.CenterX, obstacle.CenterZ, 0.22f, effectScale * 0.95f, Quaternion.identity);
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
        unit.root.SetActive(false);
        humanLosses++;

        switch (unit.kind)
        {
            case UnitKind.Tank:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.TankDeathExplosion, unit.x, unit.z, 0.35f, 1.45f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.TankWreckSmoke, unit.x, unit.z, 0.25f, 1.1f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 0.35f);
                TriggerCameraShake(0.22f, 0.15f);
                break;
            case UnitKind.Aircraft:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.AircraftDeathExplosion, unit.x, unit.z, 2.45f, 1.5f, Quaternion.identity);
                PlayBattleEffect(BattleEffectId.AircraftCrashSmoke, unit.x, unit.z, 1.2f, 1.0f, Quaternion.identity);
                PlayBattleAudio(BattleAudioCueId.ExplosionLarge, unit.x, unit.z, 2.2f);
                TriggerCameraShake(0.18f, 0.12f);
                break;
            default:
                SpawnDeathVisual(unit);
                PlayBattleEffect(BattleEffectId.SoldierDeath, unit.x, unit.z, 0.08f, 0.75f, Quaternion.identity);
                break;
        }
    }

    private void DefeatGiant(BattleUnit giant)
    {
        if (giant == null || !giant.active)
        {
            return;
        }

        giant.active = false;
        giant.root.SetActive(false);
        SpawnDeathVisual(giant);
        PlayBattleEffect(BattleEffectId.MonsterDeathExplosion, giant.x - 38f, giant.z + 80f, 0.8f, 1.5f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterDeathDust, giant.x + 32f, giant.z + 128f, 0.18f, 1.55f, Quaternion.identity);
        PlayBattleEffect(BattleEffectId.MonsterDeathExplosion, giant.x - 6f, giant.z + 34f, 0.45f, 1.35f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, giant.x, giant.z, 0.4f);
        TriggerCameraShake(0.32f, 0.24f);
        if (CountActive(giants) <= 0)
        {
            ended = true;
            ShowBanner("Humans win", true, 4f);
        }

        RefreshHud();
    }
}
