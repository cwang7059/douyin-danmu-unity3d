using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void ResolveUnitOverlaps()
    {
        UnitSeparation.ResolveUnitOverlaps();
    }

    private float SeparationRadius(BattleUnit unit)
    {
        return UnitSeparation.SeparationRadius(unit);
    }

    private void ClampUnitPosition(BattleUnit unit)
    {
        UnitSeparation.ClampUnitPosition(unit);
    }

    private sealed class UnitSeparationSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public UnitSeparationSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void ResolveUnitOverlaps()
        {
            bool changed = false;
            for (int pass = 0; pass < 4; pass++)
            {
                changed |= ResolveWithinGroup(game.giants, 1.05f);
                changed |= ResolveWithinGroup(game.tanks, 1.18f);
                changed |= ResolveBetweenGroups(game.tanks, game.soldiers, 1.10f);
                changed |= ResolveAwayFromGiant(game.tanks, 1.12f);
                changed |= ResolveAwayFromGiant(game.soldiers, 1.02f);
                changed |= ResolveAwayFromBuildings(game.soldiers);
                changed |= ResolveAwayFromBuildings(game.tanks);
                changed |= ResolveAwayFromBuildings(game.aircraft);
                changed |= ResolveAwayFromBuildings(game.giants);
            }

            if (!changed)
            {
                return;
            }

            UpdateActiveTransforms(game.tanks);
            UpdateActiveTransforms(game.soldiers);
            UpdateActiveTransforms(game.aircraft);
            UpdateActiveTransforms(game.giants);
        }

        public float SeparationRadius(BattleUnit unit)
        {
            switch (unit.kind)
            {
                case UnitKind.Tank:
                    return 56f;
                case UnitKind.Giant:
                    return 96f;
                case UnitKind.Soldier:
                    return 18f;
                default:
                    return unit.radius;
            }
        }

        public void ClampUnitPosition(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            float minX = unit.kind == UnitKind.Tank ? ApocalypseKingUnityGame.Left - 76f : unit.kind == UnitKind.Giant ? ApocalypseKingUnityGame.Left - 180f : ApocalypseKingUnityGame.Left - 150f;
            float maxX = unit.kind == UnitKind.Tank ? ApocalypseKingUnityGame.Right - 160f : unit.kind == UnitKind.Giant ? ApocalypseKingUnityGame.Right + 260f : ApocalypseKingUnityGame.Right - 48f;
            unit.x = Mathf.Clamp(unit.x, minX, maxX);
            unit.z = Mathf.Clamp(unit.z, ApocalypseKingUnityGame.Bottom + 44f, ApocalypseKingUnityGame.Top - 70f);
            game.ResolveBuildingCollision(unit);
            unit.x = Mathf.Clamp(unit.x, minX, maxX);
            unit.z = Mathf.Clamp(unit.z, ApocalypseKingUnityGame.Bottom + 44f, ApocalypseKingUnityGame.Top - 70f);
        }

        private bool ResolveWithinGroup(List<BattleUnit> units, float padding)
        {
            bool changed = false;
            BuildSeparationGrid(units);

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!CanResolveSeparation(unit))
                {
                    continue;
                }

                int cellX = SeparationCell(unit.x);
                int cellZ = SeparationCell(unit.z);
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        List<BattleUnit> bucket;
                        if (!game.separationGrid.TryGetValue(SeparationCellKey(cellX + dx, cellZ + dz), out bucket))
                        {
                            continue;
                        }

                        for (int b = 0; b < bucket.Count; b++)
                        {
                            var other = bucket[b];
                            if (other.id <= unit.id)
                            {
                                continue;
                            }

                            changed |= ResolvePair(unit, other, padding);
                        }
                    }
                }
            }

            return changed;
        }

        private bool ResolveBetweenGroups(List<BattleUnit> first, List<BattleUnit> second, float padding)
        {
            bool changed = false;
            BuildSeparationGrid(second);

            for (int i = 0; i < first.Count; i++)
            {
                var unit = first[i];
                if (!CanResolveSeparation(unit))
                {
                    continue;
                }

                int cellX = SeparationCell(unit.x);
                int cellZ = SeparationCell(unit.z);
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        List<BattleUnit> bucket;
                        if (!game.separationGrid.TryGetValue(SeparationCellKey(cellX + dx, cellZ + dz), out bucket))
                        {
                            continue;
                        }

                        for (int b = 0; b < bucket.Count; b++)
                        {
                            changed |= ResolvePair(unit, bucket[b], padding);
                        }
                    }
                }
            }

            return changed;
        }

        private void BuildSeparationGrid(List<BattleUnit> units)
        {
            ClearSeparationGrid();
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!CanResolveSeparation(unit))
                {
                    continue;
                }

                long key = SeparationCellKey(SeparationCell(unit.x), SeparationCell(unit.z));
                List<BattleUnit> bucket;
                if (!game.separationGrid.TryGetValue(key, out bucket))
                {
                    bucket = GetSeparationGridBucket();
                    game.separationGrid[key] = bucket;
                }

                bucket.Add(unit);
            }
        }

        private void ClearSeparationGrid()
        {
            for (int i = 0; i < game.separationGridBuckets.Count; i++)
            {
                var bucket = game.separationGridBuckets[i];
                bucket.Clear();
                game.separationGridBucketPool.Add(bucket);
            }

            game.separationGridBuckets.Clear();
            game.separationGrid.Clear();
        }

        private List<BattleUnit> GetSeparationGridBucket()
        {
            int last = game.separationGridBucketPool.Count - 1;
            List<BattleUnit> bucket;
            if (last >= 0)
            {
                bucket = game.separationGridBucketPool[last];
                game.separationGridBucketPool.RemoveAt(last);
            }
            else
            {
                bucket = new List<BattleUnit>(8);
            }

            game.separationGridBuckets.Add(bucket);
            return bucket;
        }

        private bool ResolveAwayFromGiant(List<BattleUnit> units, float padding)
        {
            bool changed = false;
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.active || unit.kind == UnitKind.Aircraft)
                {
                    continue;
                }

                for (int g = 0; g < game.giants.Count; g++)
                {
                    var giant = game.giants[g];
                    if (giant == null || !giant.active)
                    {
                        continue;
                    }

                    float zReach = game.GiantMeleeZReach(unit.kind, false) * padding;
                    if (Mathf.Abs(unit.z - giant.z) > zReach)
                    {
                        continue;
                    }

                    float stopX = game.HumanHoldX(unit, giant);
                    float guard = 4f + padding * 2f;
                    if (unit.x > stopX + guard)
                    {
                        unit.x = stopX + guard;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private bool ResolveAwayFromBuildings(List<BattleUnit> units)
        {
            bool changed = false;
            for (int i = 0; i < units.Count; i++)
            {
                changed |= game.ResolveBuildingCollision(units[i]);
            }

            return changed;
        }

        private bool ResolvePair(BattleUnit first, BattleUnit second, float padding)
        {
            if (first == null || second == null || !first.active || !second.active || first == second)
            {
                return false;
            }

            if (first.kind == UnitKind.Aircraft || second.kind == UnitKind.Aircraft)
            {
                return false;
            }

            float dx = first.x - second.x;
            float dz = first.z - second.z;
            float distanceSq = dx * dx + dz * dz;
            float minimum = (SeparationRadius(first) + SeparationRadius(second)) * padding;
            if (distanceSq >= minimum * minimum)
            {
                return false;
            }

            float distance = Mathf.Sqrt(Mathf.Max(0.0001f, distanceSq));
            if (distance < 0.1f)
            {
                float angle = game.Noise(first.id * 23.7f + second.id * 11.9f) * Mathf.PI * 2f;
                dx = Mathf.Cos(angle);
                dz = Mathf.Sin(angle);
                distance = 1f;
            }

            float nx = dx / distance;
            float nz = dz / distance;
            float push = (minimum - distance) + 0.25f;
            float firstWeight = PushWeight(first);
            float secondWeight = PushWeight(second);
            float totalWeight = firstWeight + secondWeight;
            if (totalWeight <= 0.001f)
            {
                return false;
            }

            float firstPush = push * (firstWeight / totalWeight);
            float secondPush = push * (secondWeight / totalWeight);

            first.x += nx * firstPush;
            first.z += nz * firstPush;
            second.x -= nx * secondPush;
            second.z -= nz * secondPush;
            ClampUnitPosition(first);
            ClampUnitPosition(second);
            return true;
        }

        private float PushWeight(BattleUnit unit)
        {
            switch (unit.kind)
            {
                case UnitKind.Giant:
                    return 0.65f;
                case UnitKind.Tank:
                    return 0.55f;
                case UnitKind.Soldier:
                    return 1.4f;
                default:
                    return 1f;
            }
        }

        private void UpdateActiveTransforms(List<BattleUnit> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].active)
                {
                    game.UpdateUnitTransform(units[i], 0f);
                }
            }
        }

        private static bool CanResolveSeparation(BattleUnit unit)
        {
            return unit != null && unit.active && unit.kind != UnitKind.Aircraft;
        }

        private static int SeparationCell(float value)
        {
            return Mathf.FloorToInt(value / ApocalypseKingUnityGame.SeparationGridCellSize);
        }

        private static long SeparationCellKey(int cellX, int cellZ)
        {
            return ((long)cellX << 32) ^ (uint)cellZ;
        }
    }
}
