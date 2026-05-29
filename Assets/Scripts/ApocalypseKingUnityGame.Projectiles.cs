using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void PrewarmProjectiles(ProjectileKind kind, int count, Color color)
    {
        ProjectileResolver.PrewarmProjectiles(kind, count, color);
    }

    private void SpawnProjectile(ProjectileKind kind, ProjectileTarget target, float fromX, float fromZ, float fromHeight, float toX, float toZ, float toHeight, float damage, float radius, float speed, Color color)
    {
        ProjectileResolver.SpawnProjectile(kind, target, fromX, fromZ, fromHeight, toX, toZ, toHeight, damage, radius, speed, color);
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

        public void SpawnProjectile(ProjectileKind kind, ProjectileTarget target, float fromX, float fromZ, float fromHeight, float toX, float toZ, float toHeight, float damage, float radius, float speed, Color color)
        {
            if (game.projectiles.Count >= ApocalypseKingUnityGame.MaxProjectiles)
            {
                return;
            }

            ProjectileView projectile = null;
            for (int i = 0; i < game.projectiles.Count; i++)
            {
                if (!game.projectiles[i].active)
                {
                    projectile = game.projectiles[i];
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
            ConfigureProjectileVisual(projectile, kind, color);
            projectile.duration = Mathf.Max(0.04f, game.Distance(fromX, fromZ, toX, toZ) / speed);
            projectile.progress = 0f;
            projectile.trailTimer = 0f;
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

                float deltaProgress = dt / Mathf.Max(0.04f, shot.duration);
                float previousT = Mathf.Clamp01(shot.progress);
                shot.progress += deltaProgress;
                float t = Mathf.Clamp01(shot.progress);
                float arc = shot.kind == ProjectileKind.Bomb
                    ? Mathf.Sin(t * Mathf.PI) * 0.35f - t * t * 1.45f
                    : shot.kind == ProjectileKind.Shell || shot.kind == ProjectileKind.Rock
                        ? Mathf.Sin(t * Mathf.PI) * 1.45f
                        : 0f;
                shot.lastWorldPosition = shot.worldPosition;
                Vector2 previousLogical = new Vector2(Mathf.Lerp(shot.fromX, shot.toX, previousT), Mathf.Lerp(shot.fromZ, shot.toZ, previousT));
                Vector2 currentLogical = new Vector2(Mathf.Lerp(shot.fromX, shot.toX, t), Mathf.Lerp(shot.fromZ, shot.toZ, t));
                shot.worldPosition = game.ToWorldPoint(currentLogical.x, currentLogical.y, Mathf.Lerp(shot.fromHeight, shot.toHeight, t) + arc);

                bool impactedBuilding = false;
                Vector2 buildingImpactPoint;
                if (CanProjectileHitBuildingsInFlight(shot.kind)
                    && TryFindProjectileBuildingImpact(previousLogical, currentLogical, ProjectileBuildingImpactRadius(shot.kind), out _, out buildingImpactPoint))
                {
                    impactedBuilding = true;
                    shot.toX = buildingImpactPoint.x;
                    shot.toZ = buildingImpactPoint.y;
                    shot.worldPosition = game.ToWorldPoint(buildingImpactPoint.x, buildingImpactPoint.y, Mathf.Lerp(shot.fromHeight, shot.toHeight, t) + arc);
                }

                if (shot.kind == ProjectileKind.Bomb)
                {
                    shot.trailTimer -= dt;
                    if (shot.trailTimer <= 0f)
                    {
                        shot.trailTimer = 0.08f;
                        game.PlayBattleEffect(BattleEffectId.BombDropTrail, shot.worldPosition, 0.42f, Quaternion.identity);
                    }
                }
                UpdateProjectileVisual(shot, t);

                if (impactedBuilding || t >= 1f)
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
            line.startWidth = kind == ProjectileKind.Bullet ? 0.03f : 0.08f;
            line.endWidth = kind == ProjectileKind.Bullet ? 0.03f : 0.06f;
            line.material = game.GetOpaqueMaterial(color);
            line.startColor = color;
            line.endColor = color;

            var head = game.CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
            head.transform.localScale = Vector3.one * (kind == ProjectileKind.Bullet ? 0.08f : 0.18f);
            head.GetComponent<Renderer>().sharedMaterial = game.GetOpaqueMaterial(color);

            root.SetActive(false);
            return new ProjectileView
            {
                root = root,
                line = line,
                head = head.transform,
                active = false,
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
                float startWidth = kind == ProjectileKind.Bullet ? 0.03f : kind == ProjectileKind.Bomb ? 0.10f : 0.08f;
                float endWidth = kind == ProjectileKind.Bullet ? 0.03f : kind == ProjectileKind.Bomb ? 0.12f : 0.06f;
                projectile.line.startWidth = startWidth;
                projectile.line.endWidth = endWidth;
                projectile.line.material = game.GetOpaqueMaterial(color);
                projectile.line.startColor = color;
                projectile.line.endColor = color;
            }

            if (projectile.head != null)
            {
                float scale = ProjectileHeadScale(kind);
                projectile.head.localScale = new Vector3(scale * 0.7f, scale * 1.2f, scale * 0.7f);
                var renderer = projectile.head.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = game.GetOpaqueMaterial(color);
                }
            }
        }

        private void UpdateProjectileVisual(ProjectileView shot, float t)
        {
            if (!shot.active || shot.root == null)
            {
                return;
            }

            shot.line.SetPosition(0, shot.lastWorldPosition);
            shot.line.SetPosition(1, shot.worldPosition);
            shot.head.position = shot.worldPosition;
            Vector3 direction = shot.worldPosition - shot.lastWorldPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                shot.head.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            float pulse = shot.kind == ProjectileKind.Bullet ? 1f : 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            float scale = ProjectileHeadScale(shot.kind) * pulse;
            shot.head.localScale = shot.kind == ProjectileKind.Bomb
                ? new Vector3(scale * 0.75f, scale * 1.55f, scale * 0.75f)
                : Vector3.one * scale;
        }

        private void ResolveProjectileImpact(ProjectileView shot)
        {
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
                    float scale = shot.kind == ProjectileKind.Bomb ? 1.7f : shot.kind == ProjectileKind.Rocket ? 1.55f : 1.25f;
                    float impactHeight = shot.kind == ProjectileKind.Bomb ? 0.18f : 1.72f;
                    game.PlayBattleEffect(impact, shot.toX, shot.toZ, impactHeight, scale, Quaternion.identity);
                    game.PlayBattleAudio(shot.kind == ProjectileKind.Bomb ? BattleAudioCueId.ExplosionLarge : BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.2f);
                    game.TriggerCameraShake(shot.kind == ProjectileKind.Bomb ? 0.18f : 0.12f, shot.kind == ProjectileKind.Bomb ? 0.11f : 0.07f);
                }
                else if (game.Noise(game.battleTime * 25f + shot.toX) > 0.68f)
                {
                    game.PlayBattleEffect(BattleEffectId.BulletHitMetal, shot.toX, shot.toZ, 1.7f, 0.65f, Quaternion.identity);
                }

                return;
            }

            game.PlayBattleEffect(shot.kind == ProjectileKind.Rock ? BattleEffectId.MonsterHammerImpact : BattleEffectId.ShellExplosionSmall, shot.toX, shot.toZ, 0.12f, shot.kind == ProjectileKind.Rock ? 1.35f : 1.0f, Quaternion.identity);
            game.PlayBattleAudio(BattleAudioCueId.ExplosionSmall, shot.toX, shot.toZ, 0.12f);
            game.TriggerCameraShake(0.12f, 0.08f);
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
                    return 0.24f;
                case ProjectileKind.Rock:
                    return 0.26f;
                default:
                    return 0.18f;
            }
        }
    }
}
