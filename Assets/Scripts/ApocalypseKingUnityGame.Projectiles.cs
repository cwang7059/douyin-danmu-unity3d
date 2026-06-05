using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void PrewarmProjectiles(ProjectileKind kind, int count, Color color)
    {
        ProjectileResolver.PrewarmProjectiles(kind, count, color);
    }

    private void SpawnProjectile(ProjectileKind kind, ProjectileTarget target, float fromX, float fromZ, float fromHeight, float toX, float toZ, float toHeight, float damage, float radius, float speed, Color color, int homingTargetId = -1)
    {
        ProjectileResolver.SpawnProjectile(kind, target, fromX, fromZ, fromHeight, toX, toZ, toHeight, damage, radius, speed, color, homingTargetId);
    }

    private void UpdateProjectiles(float dt)
    {
        ProjectileResolver.UpdateProjectiles(dt);
    }

    private sealed class ProjectileSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public ProjectileSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void PrewarmProjectiles(ProjectileKind kind, int count, Color color)
        {
            for (int i = 0; i < count && game.projectiles.Count < ApocalypseKingUnityGame.MaxProjectiles; i++)
            {
                game.projectiles.Add(CreateProjectileView(kind, color));
            }
        }

        public void SpawnProjectile(ProjectileKind kind, ProjectileTarget target, float fromX, float fromZ, float fromHeight, float toX, float toZ, float toHeight, float damage, float radius, float speed, Color color, int homingTargetId = -1)
        {
            if (game.projectiles.Count >= ApocalypseKingUnityGame.MaxProjectiles)
            {
                return;
            }

            ProjectileView projectile = null;
            for (int i = 0; i < game.projectiles.Count; i++)
            {
                ProjectileView candidate = game.projectiles[i];
                if (!candidate.active && candidate.kind == kind)
                {
                    projectile = candidate;
                    break;
                }
            }

            if (projectile == null)
            {
                projectile = CreateProjectileView(kind, color);
                game.projectiles.Add(projectile);
            }

            projectile.kind = kind;
            projectile.target = target;
            projectile.fromX = fromX;
            projectile.fromZ = fromZ;
            projectile.toX = toX;
            projectile.toZ = toZ;
            projectile.fromHeight = fromHeight;
            projectile.toHeight = toHeight;
            projectile.damage = damage;
            projectile.radius = radius;
            projectile.speed = speed;
            projectile.color = color;
            if (kind == ProjectileKind.Bomb)
            {
                game.EnsureBombProjectileMeshVisual(projectile);
            }
            else if (kind == ProjectileKind.Rocket)
            {
                game.EnsureRocketProjectileMeshVisual(projectile);
            }
            else if (kind == ProjectileKind.Fireball)
            {
                game.EnsureFireballProjectileVisual(projectile);
            }

            ConfigureProjectileVisual(projectile, kind, color);
            float flightSeconds = Mathf.Max(0.04f, game.Distance(fromX, fromZ, toX, toZ) / speed);
            if (kind == ProjectileKind.Bomb)
            {
                flightSeconds = Mathf.Max(AircraftBombMinFlightSeconds, flightSeconds);
                float dx = toX - fromX;
                float dz = toZ - fromZ;
                projectile.bombHeadingYawDegrees = dx * dx + dz * dz > 0.04f
                    ? Mathf.Atan2(dx, dz) * Mathf.Rad2Deg
                    : 0f;
            }
            else if (kind == ProjectileKind.Fireball)
            {
                projectile.homingTargetId = homingTargetId;
                projectile.fireballLogicalX = fromX;
                projectile.fireballLogicalZ = fromZ;
                projectile.fireballLogicalHeight = fromHeight;
                projectile.fireballFlightTime = 0f;
                projectile.speed = PterosaurFireballFlightSpeed;
                projectile.radius = PterosaurFireballHitRadius;
                game.InitializeFireballFlight(projectile, toX, toZ, toHeight);
                flightSeconds = projectile.fireballMaxFlightTime;
            }

            projectile.duration = flightSeconds;
            projectile.progress = 0f;
            projectile.trailTimer = 0f;
            projectile.target = target;
            projectile.lastWorldPosition = game.ToWorldPoint(fromX, fromZ, fromHeight);
            projectile.worldPosition = projectile.lastWorldPosition;
            projectile.active = true;
            projectile.root.SetActive(true);
            UpdateProjectileVisual(projectile, 0f);
        }

        public void UpdateProjectiles(float dt)
        {
            for (int i = 0; i < game.projectiles.Count; i++)
            {
                var shot = game.projectiles[i];
                if (!shot.active)
                {
                    continue;
                }

                bool impactedBuilding = false;
                Vector2 previousLogical;
                Vector2 currentLogical;
                float t;
                if (shot.kind == ProjectileKind.Fireball)
                {
                    shot.fireballFlightTime += dt;
                    previousLogical = new Vector2(shot.fireballLogicalX, shot.fireballLogicalZ);
                    game.TryResolveFireballHomingTarget(shot, out float goalX, out float goalZ, out float goalHeight);
                    Vector2 toGoal = new Vector2(goalX - shot.fireballLogicalX, goalZ - shot.fireballLogicalZ);
                    float dist = toGoal.magnitude;
                    float step = Mathf.Max(0.01f, shot.speed * dt);
                    if (dist <= Mathf.Max(8f, shot.radius * 0.45f))
                    {
                        currentLogical = new Vector2(goalX, goalZ);
                        shot.fireballLogicalHeight = goalHeight;
                    }
                    else
                    {
                        Vector2 delta = toGoal * (step / dist);
                        currentLogical = previousLogical + delta;
                        shot.fireballLogicalHeight = Mathf.Lerp(
                            shot.fireballLogicalHeight,
                            goalHeight,
                            Mathf.Clamp01(step / Mathf.Max(dist, 1f)));
                    }

                    shot.fireballLogicalX = currentLogical.x;
                    shot.fireballLogicalZ = currentLogical.y;
                    float totalSpan = Mathf.Max(1f, game.Distance(shot.fromX, shot.fromZ, goalX, goalZ));
                    t = Mathf.Clamp01(game.Distance(shot.fromX, shot.fromZ, shot.fireballLogicalX, shot.fireballLogicalZ) / totalSpan);
                    shot.progress = t;
                    float arc = Mathf.Sin(t * Mathf.PI) * 0.35f;
                    shot.lastWorldPosition = shot.worldPosition;
                    shot.worldPosition = game.ToWorldPoint(
                        currentLogical.x,
                        currentLogical.y,
                        shot.fireballLogicalHeight + arc);

                    BattleUnit hitUnit;
                    Vector2 humanImpactPoint;
                    if (game.TryFindFireballHumanImpact(previousLogical, currentLogical, shot.radius, out hitUnit, out humanImpactPoint))
                    {
                        impactedBuilding = true;
                        shot.toX = humanImpactPoint.x;
                        shot.toZ = humanImpactPoint.y;
                        shot.toHeight = shot.fireballLogicalHeight;
                        shot.homingTargetId = hitUnit.id;
                        shot.worldPosition = game.ToWorldPoint(humanImpactPoint.x, humanImpactPoint.y, shot.fireballLogicalHeight + arc);
                    }
                    else if (dist <= Mathf.Max(10f, shot.radius * 0.5f))
                    {
                        shot.fireballNearGoalTime += dt;
                        if (shot.fireballNearGoalTime >= PterosaurFireballNearGoalDetonateSeconds)
                        {
                            impactedBuilding = true;
                            shot.toX = currentLogical.x;
                            shot.toZ = currentLogical.y;
                            shot.toHeight = shot.fireballLogicalHeight;
                        }
                    }
                    else
                    {
                        shot.fireballNearGoalTime = 0f;
                    }

                    if (!impactedBuilding && shot.fireballFlightTime >= shot.fireballMaxFlightTime)
                    {
                        impactedBuilding = true;
                        shot.toX = currentLogical.x;
                        shot.toZ = currentLogical.y;
                        shot.toHeight = shot.fireballLogicalHeight;
                    }
                }
                else
                {
                    float deltaProgress = dt / Mathf.Max(0.04f, shot.duration);
                    float previousT = Mathf.Clamp01(shot.progress);
                    shot.progress += deltaProgress;
                    t = Mathf.Clamp01(shot.progress);
                    float arc = shot.kind == ProjectileKind.Bomb
                        ? Mathf.Sin(t * Mathf.PI) * 1.35f
                        : shot.kind == ProjectileKind.Shell
                            || shot.kind == ProjectileKind.Rock
                            ? Mathf.Sin(t * Mathf.PI) * 1.45f
                            : 0f;
                    shot.lastWorldPosition = shot.worldPosition;
                    previousLogical = new Vector2(Mathf.Lerp(shot.fromX, shot.toX, previousT), Mathf.Lerp(shot.fromZ, shot.toZ, previousT));
                    currentLogical = new Vector2(Mathf.Lerp(shot.fromX, shot.toX, t), Mathf.Lerp(shot.fromZ, shot.toZ, t));
                    shot.worldPosition = game.ToWorldPoint(currentLogical.x, currentLogical.y, Mathf.Lerp(shot.fromHeight, shot.toHeight, t) + arc);

                    Vector2 buildingImpactPoint;
                    if (CanProjectileHitBuildingsInFlight(shot.kind)
                        && TryFindProjectileBuildingImpact(previousLogical, currentLogical, ProjectileBuildingImpactRadius(shot.kind), out _, out buildingImpactPoint))
                    {
                        impactedBuilding = true;
                        shot.toX = buildingImpactPoint.x;
                        shot.toZ = buildingImpactPoint.y;
                        shot.worldPosition = game.ToWorldPoint(buildingImpactPoint.x, buildingImpactPoint.y, Mathf.Lerp(shot.fromHeight, shot.toHeight, t) + arc);
                    }
                }

                if (shot.kind == ProjectileKind.Bomb)
                {
                    shot.trailTimer -= dt;
                    if (shot.trailTimer <= 0f)
                    {
                        shot.trailTimer = 0.22f;
                        game.PlayBattleEffect(
                            BattleEffectId.BombDropTrail,
                            shot.worldPosition,
                            AircraftBombDropTrailScale * 0.65f,
                            Quaternion.identity);
                    }
                }
                UpdateProjectileVisual(shot, t);

                if (impactedBuilding || (shot.kind != ProjectileKind.Fireball && t >= 1f))
                {
                    ResolveProjectileImpact(shot);
                }
            }
        }

        private ProjectileView CreateProjectileView(ProjectileKind kind, Color color)
        {
            var root = new GameObject($"{kind}_Projectile");
            root.transform.SetParent(game.projectileRoot, false);

            var lineObject = new GameObject("Trail");
            lineObject.transform.SetParent(root.transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
        line.startWidth = kind == ProjectileKind.Bullet ? 0.015f : 0.08f;
        line.endWidth = kind == ProjectileKind.Bullet ? 0.01f : 0.06f;
            line.material = game.GetOpaqueMaterial(color);
            line.startColor = color;
            line.endColor = color;

            Transform headTransform;
            bool usesBombMesh = false;
            bool usesRocketMesh = false;
            if (kind == ProjectileKind.Rocket)
            {
                if (line != null)
                {
                    line.enabled = true;
                    line.startWidth = 0.05f;
                    line.endWidth = 0.02f;
                }

                headTransform = new GameObject("RocketBody").transform;
                headTransform.SetParent(root.transform, false);
                headTransform.localPosition = Vector3.zero;
                headTransform.localRotation = Quaternion.identity;
                headTransform.localScale = Vector3.one;
                var rocketProjectile = new ProjectileView
                {
                    root = root,
                    line = line,
                    head = headTransform,
                    active = false,
                    kind = ProjectileKind.Rocket,
                    usesRocketMesh = false,
                };
                game.EnsureRocketProjectileMeshVisual(rocketProjectile);
                usesRocketMesh = rocketProjectile.usesRocketMesh;
                headTransform = rocketProjectile.head;
            }
            else if (kind == ProjectileKind.Fireball)
            {
                if (line != null)
                {
                    line.enabled = true;
                    line.startWidth = 0.04f;
                    line.endWidth = 0.01f;
                }

                headTransform = new GameObject("FireballBody").transform;
                headTransform.SetParent(root.transform, false);
                headTransform.localPosition = Vector3.zero;
                headTransform.localRotation = Quaternion.identity;
                headTransform.localScale = Vector3.one;
                var fireballProjectile = new ProjectileView
                {
                    root = root,
                    line = line,
                    head = headTransform,
                    active = false,
                    kind = ProjectileKind.Fireball,
                    usesFireballParticleVisual = false,
                };
                game.EnsureFireballProjectileVisual(fireballProjectile);
                headTransform = fireballProjectile.head;
                root.SetActive(false);
                return new ProjectileView
                {
                    kind = ProjectileKind.Fireball,
                    root = root,
                    line = line,
                    head = headTransform,
                    active = false,
                    usesFireballParticleVisual = fireballProjectile.usesFireballParticleVisual,
                };
            }
            else if (kind == ProjectileKind.Bomb)
            {
                if (line != null)
                {
                    line.enabled = false;
                }

                headTransform = new GameObject("BombBody").transform;
                headTransform.SetParent(root.transform, false);
                headTransform.localPosition = Vector3.zero;
                headTransform.localRotation = Quaternion.identity;
                headTransform.localScale = Vector3.one;
                var bombProjectile = new ProjectileView
                {
                    root = root,
                    line = line,
                    head = headTransform,
                    active = false,
                    kind = ProjectileKind.Bomb,
                    usesBombMesh = false,
                };
                game.EnsureBombProjectileMeshVisual(bombProjectile);
                usesBombMesh = bombProjectile.usesBombMesh;
                headTransform = bombProjectile.head;
            }
            else
            {
                GameObject headObject = game.CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
                headObject.transform.localScale = Vector3.one * (kind == ProjectileKind.Bullet ? 0.08f : 0.18f);
                headObject.GetComponent<Renderer>().sharedMaterial = game.GetOpaqueMaterial(color);
                headTransform = headObject.transform;
            }

            root.SetActive(false);
            return new ProjectileView
            {
                kind = kind,
                root = root,
                line = line,
                head = headTransform,
                active = false,
                usesBombMesh = usesBombMesh,
                usesRocketMesh = usesRocketMesh,
            };
        }

        private void ConfigureProjectileVisual(ProjectileView projectile, ProjectileKind kind, Color color)
        {
            if (projectile == null)
            {
                return;
            }

            if (projectile.line != null)
            {
                if ((kind == ProjectileKind.Bomb && projectile.usesBombMesh)
                    || (kind == ProjectileKind.Rocket && projectile.usesRocketMesh)
                    || (kind == ProjectileKind.Fireball && projectile.usesFireballParticleVisual))
                {
                    projectile.line.enabled = false;
                }
                else
                {
                    projectile.line.enabled = true;
                }

                float startWidth;
                float endWidth;
                Color lineColor = color;
                if (kind == ProjectileKind.Shell)
                {
                    startWidth = 0.028f;
                    endWidth = 0.018f;
                    lineColor = new Color(0.52f, 0.50f, 0.46f, 0.75f);
                }
                else if (kind == ProjectileKind.Rocket)
                {
                    startWidth = 0.06f;
                    endWidth = 0.02f;
                    lineColor = new Color(1f, 0.62f, 0.22f, 0.82f);
                }
                else if (kind == ProjectileKind.Fireball)
                {
                    startWidth = 0.05f;
                    endWidth = 0.015f;
                    lineColor = new Color(color.r, color.g, color.b, 0.55f);
                }
                else
                {
                    startWidth = kind == ProjectileKind.Bullet ? 0.015f : kind == ProjectileKind.Bomb ? 0.10f : 0.08f;
                    endWidth = kind == ProjectileKind.Bullet ? 0.01f : kind == ProjectileKind.Bomb ? 0.14f : 0.06f;
                    if (kind == ProjectileKind.Bomb)
                    {
                        lineColor = new Color(0.72f, 0.74f, 0.70f, 0.75f);
                    }
                }

                projectile.line.startWidth = startWidth;
                projectile.line.endWidth = endWidth;
                projectile.line.material = game.GetOpaqueMaterial(lineColor);
                projectile.line.startColor = lineColor;
                projectile.line.endColor = lineColor;
            }

            if (projectile.head != null
                && !(kind == ProjectileKind.Bomb && projectile.usesBombMesh)
                && !(kind == ProjectileKind.Rocket && projectile.usesRocketMesh)
                && !(kind == ProjectileKind.Fireball && projectile.usesFireballParticleVisual))
            {
                float scale = ProjectileHeadScale(kind);
                if (kind == ProjectileKind.Shell)
                {
                    projectile.head.localScale = new Vector3(scale * 0.55f, scale * 0.55f, scale * 1.1f);
                }
                else if (kind == ProjectileKind.Bomb)
                {
                    projectile.head.localScale = Vector3.one * scale;
                }
                else if (kind == ProjectileKind.Rocket)
                {
                    projectile.head.localScale = new Vector3(scale * 0.35f, scale * 1.4f, scale * 0.35f);
                }
                else
                {
                    projectile.head.localScale = new Vector3(scale * 0.7f, scale * 1.2f, scale * 0.7f);
                }
                var renderer = projectile.head.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color headColor = kind == ProjectileKind.Shell
                        ? new Color(0.42f, 0.40f, 0.36f, 1f)
                        : kind == ProjectileKind.Bomb
                            ? AircraftBombVisualColor
                            : kind == ProjectileKind.Rocket
                                ? TacticalRocketVisualColor
                                : color;
                    renderer.sharedMaterial = game.GetOpaqueMaterial(headColor);
                }
            }
        }

        private void UpdateProjectileVisual(ProjectileView shot, float t)
        {
            if (!shot.active || shot.root == null || shot.head == null)
            {
                return;
            }

            if (shot.line != null && shot.line.enabled)
            {
                shot.line.SetPosition(0, shot.lastWorldPosition);
                shot.line.SetPosition(1, shot.worldPosition);
            }

            shot.root.transform.position = shot.worldPosition;
            shot.head.localPosition = Vector3.zero;
            shot.head.localRotation = Quaternion.identity;
            if (shot.kind == ProjectileKind.Bomb)
            {
                // 航空炸弹下落时保持水平，仅按投放时的水平航向偏航（勿随竖直速度低头）
                shot.head.rotation = Quaternion.Euler(0f, shot.bombHeadingYawDegrees, 0f);
            }
            else
            {
                Vector3 direction = shot.worldPosition - shot.lastWorldPosition;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    shot.head.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            float pulse = shot.kind == ProjectileKind.Bullet ? 1f : 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            float scale = ProjectileHeadScale(shot.kind) * pulse;
            if ((shot.kind == ProjectileKind.Bomb && shot.usesBombMesh)
                || (shot.kind == ProjectileKind.Rocket && shot.usesRocketMesh))
            {
                shot.head.localScale = Vector3.one;
            }
            else if (shot.kind == ProjectileKind.Fireball && shot.usesFireballParticleVisual)
            {
                shot.head.localScale = Vector3.one * game.GetPterosaurFireballVisualScale();
            }
            else if (shot.kind == ProjectileKind.Bomb && !shot.usesBombMesh)
            {
                shot.head.localScale = Vector3.one * scale;
            }
            else if (shot.kind == ProjectileKind.Shell)
            {
                shot.head.localScale = new Vector3(scale * 0.55f, scale * 0.55f, scale * 1.1f);
            }
            else
            {
                shot.head.localScale = Vector3.one * scale;
            }
        }

        private void ResolveProjectileImpact(ProjectileView shot)
        {
            if (shot.kind == ProjectileKind.Fireball && shot.root != null)
            {
                TrailRenderer trail = shot.root.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.emitting = false;
                }
            }

            shot.active = false;
            shot.root.SetActive(false);

            if (CanProjectileDamageBuildings(shot.kind))
            {
                game.DamageBuildingsInArea(shot.toX, shot.toZ, ProjectileBuildingBlastRadius(shot), ProjectileBuildingDamage(shot));
            }

            if (shot.target == ProjectileTarget.Giant)
            {
                if (shot.radius > 0f)
                {
                    game.DamageGiantsInArea(shot.toX, shot.toZ, shot.radius, shot.damage);
                }
                else
                {
                    game.DamageGiantAt(shot.toX, shot.toZ, shot.damage);
                }

                if (shot.kind == ProjectileKind.Shell || shot.kind == ProjectileKind.Rocket || shot.kind == ProjectileKind.Bomb)
                {
                    BattleEffectId impact = shot.kind == ProjectileKind.Bomb ? BattleEffectId.BombExplosion : BattleEffectId.ShellImpactMonster;
                    float scale = shot.kind == ProjectileKind.Bomb ? 1.22f : shot.kind == ProjectileKind.Rocket ? 1.12f : 1.0f;
                    float impactHeight = shot.kind == ProjectileKind.Bomb ? 0.18f : 1.72f;
                    game.PlayBattleEffect(impact, shot.toX, shot.toZ, impactHeight, scale, Quaternion.identity);
                    game.PlayBattleAudio(shot.kind == ProjectileKind.Bomb ? BattleAudioCueId.ExplosionLarge : BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.2f);
                    game.TriggerCameraShake(shot.kind == ProjectileKind.Bomb ? 0.18f : 0.12f, shot.kind == ProjectileKind.Bomb ? 0.11f : 0.07f);
                }
                else if (game.Noise(game.battleTime * 25f + shot.toX) > 0.68f)
                {
                    game.PlayBattleEffect(BattleEffectId.BulletHitMetal, shot.toX, shot.toZ, 1.7f, 0.52f, Quaternion.identity);
                }

                return;
            }

            if (shot.kind == ProjectileKind.Fireball)
            {
                game.PlayBattleEffect(
                    BattleEffectId.PterosaurFireballImpact,
                    shot.toX,
                    shot.toZ,
                    Mathf.Max(0.35f, shot.toHeight * 0.55f),
                    0.92f,
                    Quaternion.identity);
                game.PlayBattleAudio(BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.22f);
                game.TriggerCameraShake(0.14f, 0.09f);
                game.ApplyFireballBurnAt(shot.toX, shot.toZ, shot.damage, shot.homingTargetId);
            }
            else
            {
                game.PlayBattleEffect(
                    shot.kind == ProjectileKind.Rock ? BattleEffectId.MonsterHammerImpact : BattleEffectId.ShellExplosionSmall,
                    shot.toX,
                    shot.toZ,
                    0.12f,
                    shot.kind == ProjectileKind.Rock ? 0.95f : 0.82f,
                    Quaternion.identity);
                game.PlayBattleAudio(BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.12f);
                game.TriggerCameraShake(0.12f, 0.08f);
            }

            game.ApplyAreaDamageToHumans(shot.toX, shot.toZ, shot.radius, shot.damage, false, 36f);
        }

        private bool TryFindProjectileBuildingImpact(Vector2 from, Vector2 to, float radius, out BuildingObstacle obstacle, out Vector2 impact)
        {
            obstacle = null;
            impact = to;
            float bestT = float.PositiveInfinity;

            for (int i = 0; i < game.buildingObstacles.Count; i++)
            {
                float t;
                var candidate = game.buildingObstacles[i];
                if (ApocalypseKingUnityGame.SegmentIntersectsBuilding(from, to, candidate, radius, out t) && t < bestT)
                {
                    bestT = t;
                    obstacle = candidate;
                }
            }

            if (obstacle == null)
            {
                return false;
            }

            impact = Vector2.Lerp(from, to, Mathf.Clamp01(bestT));
            return true;
        }

        private static bool CanProjectileHitBuildingsInFlight(ProjectileKind kind)
        {
            return kind == ProjectileKind.Shell || kind == ProjectileKind.Rocket;
        }

        private static bool CanProjectileDamageBuildings(ProjectileKind kind)
        {
            return kind == ProjectileKind.Shell || kind == ProjectileKind.Rocket || kind == ProjectileKind.Bomb;
        }

        private static float ProjectileBuildingImpactRadius(ProjectileKind kind)
        {
            switch (kind)
            {
                case ProjectileKind.Rocket:
                    return 8f;
                case ProjectileKind.Shell:
                    return 7f;
                default:
                    return 0f;
            }
        }

        private static float ProjectileBuildingBlastRadius(ProjectileView shot)
        {
            switch (shot.kind)
            {
                case ProjectileKind.Bomb:
                    return Mathf.Max(shot.radius, 104f);
                case ProjectileKind.Rocket:
                    return Mathf.Max(shot.radius, 78f);
                case ProjectileKind.Shell:
                    return Mathf.Max(shot.radius, 68f);
                default:
                    return shot.radius;
            }
        }

        private static float ProjectileBuildingDamage(ProjectileView shot)
        {
            switch (shot.kind)
            {
                case ProjectileKind.Bomb:
                    return Mathf.Max(shot.damage * 2.7f, 190f);
                case ProjectileKind.Rocket:
                    return Mathf.Max(shot.damage * 2.5f, 170f);
                case ProjectileKind.Shell:
                    return Mathf.Max(shot.damage * 2.2f, 155f);
                default:
                    return shot.damage;
            }
        }

        private static float ProjectileHeadScale(ProjectileKind kind)
        {
            switch (kind)
            {
                case ProjectileKind.Bullet:
                    return 0.08f;
                case ProjectileKind.Bomb:
                    return Mathf.Max(0.18f, AircraftModelTargetHeight * 0.42f);
                case ProjectileKind.Rock:
                    return 0.26f;
                case ProjectileKind.Fireball:
                    return 0.34f;
                case ProjectileKind.Rocket:
                    return 0.22f;
                case ProjectileKind.Shell:
                    return 0.11f;
                default:
                    return 0.18f;
            }
        }
    }
}
