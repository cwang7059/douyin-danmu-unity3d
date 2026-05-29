using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void MoveUnitToAvoidingBuildings(BattleUnit unit, float targetX, float targetZ)
    {
        BuildingAvoidance.MoveUnitToAvoidingBuildings(unit, targetX, targetZ);
    }

    private bool ResolveBuildingCollision(BattleUnit unit)
    {
        return BuildingAvoidance.ResolveBuildingCollision(unit);
    }

    private bool AvoidsBuildings(BattleUnit unit)
    {
        return BuildingAvoidanceSystem.AvoidsBuildings(unit);
    }

    private float BuildingAvoidanceRadius(BattleUnit unit)
    {
        return BuildingAvoidanceSystem.BuildingAvoidanceRadius(unit);
    }

    private static bool SegmentIntersectsBuilding(Vector2 from, Vector2 to, BuildingObstacle obstacle, float radius, out float t)
    {
        return BuildingAvoidanceSystem.SegmentIntersectsBuilding(from, to, obstacle, radius, out t);
    }

    private sealed class BuildingAvoidanceSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public BuildingAvoidanceSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void MoveUnitToAvoidingBuildings(BattleUnit unit, float targetX, float targetZ)
        {
            if (unit == null)
            {
                return;
            }

            Vector2 steered = ApplyBuildingAvoidanceSteering(unit, new Vector2(targetX, targetZ));
            float dx = steered.x - unit.x;
            float dz = steered.y - unit.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            if (distance <= 0.001f)
            {
                game.ClampUnitPosition(unit);
                return;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(distance / 28f), 1, 8);
            float stepX = dx / steps;
            float stepZ = dz / steps;
            for (int i = 0; i < steps; i++)
            {
                float nextX = unit.x + stepX;
                float nextZ = unit.z + stepZ;
                if (WouldOverlapBuilding(unit, nextX, nextZ, BuildingAvoidanceRadius(unit)))
                {
                    bool movedOnX = TryMoveUnitAroundBuilding(unit, nextX, unit.z);
                    bool movedOnZ = TryMoveUnitAroundBuilding(unit, unit.x, nextZ);
                    if (!movedOnX && !movedOnZ)
                    {
                        break;
                    }
                }
                else
                {
                    unit.x = nextX;
                    unit.z = nextZ;
                }

                game.ClampUnitPosition(unit);
            }
        }

        public bool ResolveBuildingCollision(BattleUnit unit)
        {
            if (!AvoidsBuildings(unit) || game.buildingObstacles.Count == 0)
            {
                return false;
            }

            bool changed = false;
            float radius = BuildingAvoidanceRadius(unit);
            for (int i = 0; i < game.buildingObstacles.Count; i++)
            {
                var obstacle = game.buildingObstacles[i];
                if (obstacle.Destroyed)
                {
                    continue;
                }

                float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
                float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
                float dx = unit.x - obstacle.CenterX;
                float dz = unit.z - obstacle.CenterZ;
                float absX = Mathf.Abs(dx);
                float absZ = Mathf.Abs(dz);
                if (absX >= expandedHalfX || absZ >= expandedHalfZ)
                {
                    continue;
                }

                float signX = absX > 0.001f ? Mathf.Sign(dx) : (unit.facing >= 0 ? -1f : 1f);
                float signZ = absZ > 0.001f ? Mathf.Sign(dz) : BuildingBypassSide(unit, obstacle, new Vector2(unit.x, unit.z));
                float penetrationX = expandedHalfX - absX;
                float penetrationZ = expandedHalfZ - absZ;
                if (penetrationX < penetrationZ)
                {
                    unit.x = obstacle.CenterX + signX * expandedHalfX;
                }
                else
                {
                    unit.z = obstacle.CenterZ + signZ * expandedHalfZ;
                }

                changed = true;
            }

            return changed;
        }

        public static bool AvoidsBuildings(BattleUnit unit)
        {
            return unit != null
                && (unit.kind == UnitKind.Soldier
                    || unit.kind == UnitKind.Tank
                    || unit.kind == UnitKind.Aircraft
                    || unit.kind == UnitKind.Giant);
        }

        public static float BuildingAvoidanceRadius(BattleUnit unit)
        {
            switch (unit.kind)
            {
                case UnitKind.Soldier:
                    return 16f;
                case UnitKind.Tank:
                    return 48f;
                case UnitKind.Aircraft:
                    return 52f;
                case UnitKind.Giant:
                    return 76f;
                default:
                    return Mathf.Max(10f, unit.radius);
            }
        }

        public static bool SegmentIntersectsBuilding(Vector2 from, Vector2 to, BuildingObstacle obstacle, float radius, out float t)
        {
            if (obstacle == null || obstacle.Destroyed)
            {
                t = 0f;
                return false;
            }

            float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
            float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
            float minX = obstacle.CenterX - expandedHalfX;
            float maxX = obstacle.CenterX + expandedHalfX;
            float minZ = obstacle.CenterZ - expandedHalfZ;
            float maxZ = obstacle.CenterZ + expandedHalfZ;

            float tMin = 0f;
            float tMax = 1f;
            if (!IntersectSegmentAxis(from.x, to.x, minX, maxX, ref tMin, ref tMax)
                || !IntersectSegmentAxis(from.y, to.y, minZ, maxZ, ref tMin, ref tMax))
            {
                t = 0f;
                return false;
            }

            t = tMin;
            return true;
        }

        private Vector2 ApplyBuildingAvoidanceSteering(BattleUnit unit, Vector2 target)
        {
            if (!AvoidsBuildings(unit) || game.buildingObstacles.Count == 0)
            {
                return target;
            }

            Vector2 from = new Vector2(unit.x, unit.z);
            Vector2 travel = target - from;
            if (travel.sqrMagnitude <= 0.01f)
            {
                return target;
            }

            float bestT = float.PositiveInfinity;
            BuildingObstacle best = null;
            float radius = BuildingAvoidanceRadius(unit);
            for (int i = 0; i < game.buildingObstacles.Count; i++)
            {
                float t;
                if (SegmentIntersectsBuilding(from, target, game.buildingObstacles[i], radius, out t) && t < bestT)
                {
                    bestT = t;
                    best = game.buildingObstacles[i];
                }
            }

            if (best == null)
            {
                return target;
            }

            Vector2 roadTarget;
            if (TryGetRoadBypassTarget(unit, from, best, target, radius, out roadTarget))
            {
                if (!SegmentIntersectsAnyBuilding(from, roadTarget, radius * 0.85f))
                {
                    return roadTarget;
                }

                return BuildBuildingBypassTarget(unit, from, best, roadTarget, radius);
            }

            return BuildBuildingBypassTarget(unit, from, best, target, radius);
        }

        private bool TryMoveUnitAroundBuilding(BattleUnit unit, float candidateX, float candidateZ)
        {
            float radius = BuildingAvoidanceRadius(unit);
            if (WouldOverlapBuilding(unit, candidateX, candidateZ, radius))
            {
                return false;
            }

            unit.x = candidateX;
            unit.z = candidateZ;
            return true;
        }

        private Vector2 BuildBuildingBypassTarget(BattleUnit unit, Vector2 from, BuildingObstacle obstacle, Vector2 target, float radius)
        {
            float extraClearance = unit.kind == UnitKind.Giant ? 34f : unit.kind == UnitKind.Tank ? 24f : 18f;
            float sideZ = BuildingBypassSide(unit, obstacle, target);
            float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius + extraClearance * 0.35f;
            float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius + extraClearance;
            float bypassZ = obstacle.CenterZ + sideZ * expandedHalfZ;

            float sideX = Mathf.Abs(from.x - obstacle.CenterX) > 3f
                ? Mathf.Sign(from.x - obstacle.CenterX)
                : Mathf.Sign(target.x - obstacle.CenterX);
            if (Mathf.Abs(sideX) < 0.1f)
            {
                sideX = unit.facing >= 0 ? -1f : 1f;
            }

            if (Mathf.Abs(from.y - bypassZ) > 8f)
            {
                float safeX = Mathf.Abs(from.x - obstacle.CenterX) < expandedHalfX + 4f
                    ? obstacle.CenterX + sideX * expandedHalfX
                    : from.x;
                return new Vector2(safeX, bypassZ);
            }

            return new Vector2(target.x, bypassZ);
        }

        private bool SegmentIntersectsAnyBuilding(Vector2 from, Vector2 target, float radius)
        {
            for (int i = 0; i < game.buildingObstacles.Count; i++)
            {
                float t;
                if (SegmentIntersectsBuilding(from, target, game.buildingObstacles[i], radius, out t))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetRoadBypassTarget(BattleUnit unit, Vector2 from, BuildingObstacle obstacle, Vector2 target, float radius, out Vector2 roadTarget)
        {
            roadTarget = target;
            if (game.roadCorridors.Count == 0)
            {
                return false;
            }

            RoadCorridor best = null;
            float bestScore = float.PositiveInfinity;
            Vector2 obstacleCenter = new Vector2(obstacle.CenterX, obstacle.CenterZ);

            for (int i = 0; i < game.roadCorridors.Count; i++)
            {
                var corridor = game.roadCorridors[i];
                float score =
                    corridor.DistanceToPoint(obstacleCenter) * 0.72f
                    + corridor.DistanceToPoint(from) * 0.18f
                    + corridor.DistanceToPoint(target) * 0.10f
                    + corridor.Priority;

                if (unit.kind == UnitKind.Tank && corridor.CenterZ > -320f)
                {
                    score += 90f;
                }
                else if (unit.kind == UnitKind.Giant && corridor.CenterX < -180f)
                {
                    score += 70f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = corridor;
                }
            }

            if (best == null)
            {
                return false;
            }

            roadTarget = target;
            if (best.Horizontal)
            {
                float laneOffset = unit.kind == UnitKind.Soldier
                    ? Mathf.Clamp(unit.baseZ - best.CenterZ, -best.HalfZ * 0.45f, best.HalfZ * 0.45f)
                    : 0f;
                roadTarget.x = ClampRoadCoordinate(target.x, best.CenterX, best.HalfX, radius);
                roadTarget.y = ClampRoadCoordinate(best.CenterZ + laneOffset, best.CenterZ, best.HalfZ, radius);
            }
            else
            {
                roadTarget.x = ClampRoadCoordinate(best.CenterX, best.CenterX, best.HalfX, radius);
                roadTarget.y = ClampRoadCoordinate(target.y, best.CenterZ, best.HalfZ, radius);
            }

            return true;
        }

        private float BuildingBypassSide(BattleUnit unit, BuildingObstacle obstacle, Vector2 target)
        {
            float[] preferences =
            {
                unit.z - obstacle.CenterZ,
                unit.baseZ - obstacle.CenterZ,
                target.y - obstacle.CenterZ,
            };

            for (int i = 0; i < preferences.Length; i++)
            {
                if (Mathf.Abs(preferences[i]) > 3f)
                {
                    return Mathf.Sign(preferences[i]);
                }
            }

            return game.Noise(unit.id * 19.7f + obstacle.CenterX * 0.031f + obstacle.CenterZ * 0.017f) > 0.5f ? 1f : -1f;
        }

        private bool WouldOverlapBuilding(BattleUnit unit, float x, float z, float radius)
        {
            if (!AvoidsBuildings(unit) || game.buildingObstacles.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < game.buildingObstacles.Count; i++)
            {
                var obstacle = game.buildingObstacles[i];
                if (obstacle.Destroyed)
                {
                    continue;
                }

                float expandedHalfX = obstacle.HalfX + obstacle.Padding + radius;
                float expandedHalfZ = obstacle.HalfZ + obstacle.Padding + radius;
                if (Mathf.Abs(x - obstacle.CenterX) < expandedHalfX
                    && Mathf.Abs(z - obstacle.CenterZ) < expandedHalfZ)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ClampRoadCoordinate(float value, float center, float half, float radius)
        {
            float innerHalf = Mathf.Max(4f, half - radius * 0.25f);
            return Mathf.Clamp(value, center - innerHalf, center + innerHalf);
        }

        private static bool IntersectSegmentAxis(float from, float to, float min, float max, ref float tMin, ref float tMax)
        {
            float delta = to - from;
            if (Mathf.Abs(delta) <= 0.001f)
            {
                return from >= min && from <= max;
            }

            float inv = 1f / delta;
            float t1 = (min - from) * inv;
            float t2 = (max - from) * inv;
            if (t1 > t2)
            {
                float swap = t1;
                t1 = t2;
                t2 = swap;
            }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMin <= tMax;
        }
    }
}
