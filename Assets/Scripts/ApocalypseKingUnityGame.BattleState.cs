using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void CheckBattleEnd()
    {
        BattleState.CheckBattleEnd();
    }

    private int CountHumans()
    {
        return BattleState.CountHumans();
    }

    private int CountActive(List<BattleUnit> units)
    {
        return BattleState.CountActive(units);
    }

    private Vector2 GetActiveGiantCenter()
    {
        return BattleState.GetActiveGiantCenter();
    }

    private float GetGiantHpTotal()
    {
        return BattleState.GetGiantHpTotal();
    }

    private float GetGiantMaxHpTotal()
    {
        return BattleState.GetGiantMaxHpTotal();
    }

    private sealed class BattleStateSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public BattleStateSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void CheckBattleEnd()
        {
            if (game.ended)
            {
                return;
            }

            for (int i = 0; i < game.giants.Count; i++)
            {
                var unit = game.giants[i];
                if (unit != null && unit.active && unit.x < ApocalypseKingUnityGame.Left + 46f)
                {
                    game.ended = true;
                    game.ShowBanner("Human line broken", true, 4f);
                    return;
                }
            }

            if (CountActive(game.giants) <= 0)
            {
                game.ended = true;
                game.ShowBanner("Humans win", true, 4f);
                return;
            }

            if (CountHumans() <= 0)
            {
                game.ended = true;
                game.ShowBanner("All human forces lost", true, 4f);
            }
        }

        public int CountHumans()
        {
            int alive = 0;
            CountActive(game.soldiers, ref alive);
            CountActive(game.tanks, ref alive);
            CountActive(game.aircraft, ref alive);
            return alive;
        }

        public int CountActive(List<BattleUnit> units)
        {
            int total = 0;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].active)
                {
                    total++;
                }
            }
            return total;
        }

        public Vector2 GetActiveGiantCenter()
        {
            float x = 0f;
            float z = 0f;
            int active = 0;

            for (int i = 0; i < game.giants.Count; i++)
            {
                var unit = game.giants[i];
                if (unit == null || !unit.active)
                {
                    continue;
                }

                x += unit.x;
                z += unit.z;
                active++;
            }

            return active > 0 ? new Vector2(x / active, z / active) : Vector2.zero;
        }

        public float GetGiantHpTotal()
        {
            float total = 0f;
            for (int i = 0; i < game.giants.Count; i++)
            {
                var unit = game.giants[i];
                if (unit != null && unit.maxHp > 0f)
                {
                    total += Mathf.Max(0f, unit.hp);
                }
            }

            return total;
        }

        public float GetGiantMaxHpTotal()
        {
            float total = 0f;
            for (int i = 0; i < game.giants.Count; i++)
            {
                var unit = game.giants[i];
                if (unit != null && unit.maxHp > 0f)
                {
                    total += Mathf.Max(0f, unit.maxHp);
                }
            }

            return total > 0f ? total : (game.giantConfig != null ? game.giantConfig.MaxHp : 2600f) * ApocalypseKingUnityGame.GiantCount;
        }

        private static void CountActive(List<BattleUnit> units, ref int alive)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].active)
                {
                    alive++;
                }
            }
        }
    }
}
