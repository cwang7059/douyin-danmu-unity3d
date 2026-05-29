using System.Collections.Generic;

public sealed partial class ApocalypseKingUnityGame
{
    private BattleUnit FindNearestGiant(BattleUnit origin)
    {
        return Targeting.FindNearestGiant(origin);
    }

    private BattleUnit FindNearestHuman(BattleUnit origin, bool includeAircraft)
    {
        return Targeting.FindNearestHuman(origin, includeAircraft);
    }

    private BattleUnit FindGiantContactTarget()
    {
        return Targeting.FindGiantContactTarget();
    }

    private BattleUnit FindGiantContactTarget(BattleUnit giant)
    {
        return Targeting.FindGiantContactTarget(giant);
    }

    private BattleUnit FindGiantEngagementTarget()
    {
        return Targeting.FindGiantEngagementTarget();
    }

    private BattleUnit FindGiantEngagementTarget(BattleUnit giant)
    {
        return Targeting.FindGiantEngagementTarget(giant);
    }

    private bool IsTargetInGiantMeleeRange(BattleUnit giant, BattleUnit target, bool contactOnly)
    {
        return Targeting.IsTargetInGiantMeleeRange(giant, target, contactOnly);
    }

    private BattleUnit FindNearestActiveGiant(float x, float z)
    {
        return Targeting.FindNearestActiveGiant(x, z);
    }

    private sealed class TargetingSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public TargetingSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public BattleUnit FindNearestGiant(BattleUnit origin)
        {
            if (origin == null)
            {
                return null;
            }

            BattleUnit best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < game.giants.Count; i++)
            {
                var candidate = game.giants[i];
                if (candidate == null || !candidate.active)
                {
                    continue;
                }

                float score = game.DistanceSq(origin.x, origin.z, candidate.x, candidate.z);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        public BattleUnit FindNearestHuman(BattleUnit origin, bool includeAircraft)
        {
            if (origin == null)
            {
                return null;
            }

            BattleUnit best = null;
            float bestScore = float.PositiveInfinity;

            ConsiderNearest(game.soldiers, origin, includeAircraft, ref best, ref bestScore);
            ConsiderNearest(game.tanks, origin, includeAircraft, ref best, ref bestScore);
            ConsiderNearest(game.aircraft, origin, includeAircraft, ref best, ref bestScore);
            return best;
        }

        public BattleUnit FindGiantContactTarget()
        {
            for (int i = 0; i < game.giants.Count; i++)
            {
                var target = FindGiantContactTarget(game.giants[i]);
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        public BattleUnit FindGiantContactTarget(BattleUnit giant)
        {
            BattleUnit best = null;
            float bestScore = float.PositiveInfinity;
            ConsiderGiantContact(giant, game.soldiers, true, ref best, ref bestScore);
            ConsiderGiantContact(giant, game.tanks, true, ref best, ref bestScore);
            ConsiderGiantContact(giant, game.aircraft, true, ref best, ref bestScore);
            return best;
        }

        public BattleUnit FindGiantEngagementTarget()
        {
            for (int i = 0; i < game.giants.Count; i++)
            {
                var target = FindGiantEngagementTarget(game.giants[i]);
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        public BattleUnit FindGiantEngagementTarget(BattleUnit giant)
        {
            BattleUnit best = null;
            float bestScore = float.PositiveInfinity;
            ConsiderGiantContact(giant, game.soldiers, false, ref best, ref bestScore);
            ConsiderGiantContact(giant, game.tanks, false, ref best, ref bestScore);
            ConsiderGiantContact(giant, game.aircraft, false, ref best, ref bestScore);
            return best;
        }

        public bool IsTargetInGiantMeleeRange(BattleUnit giant, BattleUnit target, bool contactOnly)
        {
            if (target == null || giant == null || !target.active || !giant.active)
            {
                return false;
            }

            float reach = game.GiantMeleeDistance(target.kind, contactOnly);
            return game.DistanceSq(giant.x, giant.z, target.x, target.z) <= reach * reach;
        }

        public BattleUnit FindNearestActiveGiant(float x, float z)
        {
            BattleUnit best = null;
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < game.giants.Count; i++)
            {
                var candidate = game.giants[i];
                if (candidate == null || !candidate.active)
                {
                    continue;
                }

                float score = game.DistanceSq(x, z, candidate.x, candidate.z);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private void ConsiderGiantContact(BattleUnit giant, List<BattleUnit> units, bool contactOnly, ref BattleUnit best, ref float bestScore)
        {
            if (giant == null || !giant.active)
            {
                return;
            }

            for (int i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.active || !IsTargetInGiantMeleeRange(giant, candidate, contactOnly))
                {
                    continue;
                }

                float score = game.DistanceSq(giant.x, giant.z, candidate.x, candidate.z);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
        }

        private void ConsiderNearest(List<BattleUnit> units, BattleUnit origin, bool includeAircraft, ref BattleUnit best, ref float bestScore)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (candidate == null || !candidate.active)
                {
                    continue;
                }

                if (!includeAircraft && candidate.kind == UnitKind.Aircraft)
                {
                    continue;
                }

                float airPenalty = candidate.kind == UnitKind.Aircraft ? 1.35f : 1f;
                float score = game.DistanceSq(origin.x, origin.z, candidate.x, candidate.z) * airPenalty;
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
        }
    }
}
