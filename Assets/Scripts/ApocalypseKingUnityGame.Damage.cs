using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void ApplyGiantContactDamage(BattleUnit giant)
    {
        DamageResolver.ApplyGiantContactDamage(giant);
    }

    private void ApplyAreaDamageToHumans(float x, float z, float radius, float damage, bool groundOnly, float knockback)
    {
        DamageResolver.ApplyAreaDamageToHumans(x, z, radius, damage, groundOnly, knockback);
    }

    private void DamageGiantAt(float x, float z, float amount)
    {
        DamageResolver.DamageGiantAt(x, z, amount);
    }

    private void DamageGiantsInArea(float x, float z, float radius, float amount)
    {
        DamageResolver.DamageGiantsInArea(x, z, radius, amount);
    }

    private void ApplyDirectUnitDamage(BattleUnit attacker, BattleUnit target, float amount)
    {
        DamageResolver.ApplyDirectUnitDamage(attacker, target, ScaleOutgoingDamage(attacker, target, amount));
    }

    private sealed class DamageSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public DamageSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void ApplyGiantContactDamage(BattleUnit giant)
        {
            DamageGiantContactGroup(giant, game.soldiers);
            DamageGiantContactGroup(giant, game.tanks);
            DamageGiantContactGroup(giant, game.aircraft);
        }

        public void ApplyAreaDamageToHumans(float x, float z, float radius, float damage, bool groundOnly, float knockback)
        {
            float radiusSq = radius * radius;
            DamageHumanGroup(game.soldiers, x, z, radius, radiusSq, damage, groundOnly, knockback);
            DamageHumanGroup(game.tanks, x, z, radius, radiusSq, damage, groundOnly, knockback);
            DamageHumanGroup(game.aircraft, x, z, radius, radiusSq, damage, groundOnly, knockback);
        }

        public void DamageGiantAt(float x, float z, float amount)
        {
            var giant = game.FindNearestActiveGiant(x, z);
            if (giant == null)
            {
                return;
            }

            var result = ApplyDamage(giant, amount, 0.055f);
            if (result.Defeated)
            {
                game.DefeatGiant(giant);
            }
        }

        public void DamageGiantsInArea(float x, float z, float radius, float amount)
        {
            float radiusSq = radius * radius;
            for (int i = 0; i < game.giants.Count; i++)
            {
                var giant = game.giants[i];
                if (giant == null || !giant.active)
                {
                    continue;
                }

                float distanceSq = game.DistanceSq(x, z, giant.x, giant.z);
                if (distanceSq > radiusSq)
                {
                    continue;
                }

                float pct = 1f - Mathf.Clamp01(Mathf.Sqrt(distanceSq) / Mathf.Max(1f, radius));
                var result = ApplyDamage(giant, amount * (0.55f + pct * 0.65f), 0.09f);
                if (result.Defeated)
                {
                    game.DefeatGiant(giant);
                }
            }
        }

        private void DamageGiantContactGroup(BattleUnit giant, List<BattleUnit> units)
        {
            if (giant == null || !giant.active)
            {
                return;
            }

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.active || !game.IsTargetInGiantMeleeRange(giant, unit, true))
                {
                    continue;
                }

                if (!game.IsHostileUnit(giant, unit))
                {
                    continue;
                }

                float reach = Mathf.Max(1f, game.GiantMeleeDistance(unit.kind, true));
                float pct = 1f - Mathf.Clamp01(game.Distance(giant.x, giant.z, unit.x, unit.z) / reach);
                float damage = unit.kind == UnitKind.Aircraft ? giant.damage * 0.88f : giant.damage;
                damage = game.ScaleOutgoingDamage(giant, unit, damage);
                var result = ApplyDamage(unit, damage * (0.76f + pct * 0.45f), 0f);
                game.TryApplyInfection(giant, unit);

                if (result.Defeated)
                {
                    game.DeactivateHumanUnit(unit);
                }
            }
        }

        private void DamageHumanGroup(List<BattleUnit> units, float x, float z, float radius, float radiusSq, float damage, bool groundOnly, float knockback)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null || !unit.active)
                {
                    continue;
                }

                if (groundOnly && unit.kind == UnitKind.Aircraft)
                {
                    continue;
                }

                float dSq = game.DistanceSq(unit.x, unit.z, x, z);
                if (dSq > radiusSq)
                {
                    continue;
                }

                float pct = 1f - Mathf.Sqrt(dSq) / Mathf.Max(1f, radius);
                var result = ApplyDamage(unit, damage * (0.62f + pct * 0.55f), 0f);
                unit.x -= knockback * (0.35f + pct);

                if (result.Defeated)
                {
                    game.DeactivateHumanUnit(unit);
                }
            }
        }

        private static DamageResult ApplyDamage(BattleUnit target, float amount, float hitFlashTime)
        {
            if (target == null || !target.active || amount <= 0f)
            {
                return DamageResult.None;
            }

            float previousHp = Mathf.Max(0f, target.hp);
            target.hp = Mathf.Max(0f, previousHp - amount);
            if (hitFlashTime > 0f)
            {
                target.hitFlashTimer = hitFlashTime;
            }

            return new DamageResult(target.hp <= 0f);
        }

        public void ApplyDirectUnitDamage(BattleUnit attacker, BattleUnit target, float amount)
        {
            if (target == null || !target.active || amount <= 0f)
            {
                return;
            }

            var result = ApplyDamage(target, amount, 0.04f);
            if (result.Defeated)
            {
                if (target.kind == UnitKind.Giant)
                {
                    game.DefeatGiant(target);
                }
                else
                {
                    game.DeactivateHumanUnit(target);
                }
            }
        }

        private readonly struct DamageResult
        {
            public static readonly DamageResult None = new DamageResult(false);

            public readonly bool Defeated;

            public DamageResult(bool defeated)
            {
                Defeated = defeated;
            }
        }
    }
}
