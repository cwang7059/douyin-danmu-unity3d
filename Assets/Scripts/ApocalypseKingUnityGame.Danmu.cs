using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void EnqueueLocalDanmuShortcuts()
    {
        if (danmuQueue == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            danmuQueue.EnqueueRawMessage("local-human", "Local Human", "人族步兵");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            danmuQueue.EnqueueRawMessage("local-orc", "Local Orc", "兽族地狱犬");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            danmuQueue.EnqueueRawMessage("local-skill", "Local Skill", "人族空袭");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            danmuQueue.EnqueueRawMessage("local-rage", "Local Rage", "兽族狂暴");
        }
    }

    private void ProcessDanmuCommands()
    {
        if (danmuQueue == null)
        {
            return;
        }

        int limit = Mathf.Max(1, danmuQueue.MaxCommandsPerFrame);
        for (int i = 0; i < limit; i++)
        {
            DanmuCommand command;
            if (!danmuQueue.TryDequeue(out command))
            {
                return;
            }

            ApplyDanmuCommand(command);
            processedDanmuCommandCount++;
        }
    }

    private void ApplyDanmuCommand(DanmuCommand command)
    {
        switch (command.type)
        {
            case DanmuCommandType.SpawnUnit:
                ApplyDanmuSpawn(command);
                break;
            case DanmuCommandType.CastSkill:
                ApplyDanmuSkill(command);
                break;
            case DanmuCommandType.Heal:
                ApplyDanmuHeal(command);
                break;
            case DanmuCommandType.Buff:
            case DanmuCommandType.AddEnergy:
                ApplyDanmuBuff(command);
                break;
        }
    }

    private void ApplyDanmuSpawn(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            string key = NormalizeDanmuKey(command.key);
            bool spawned;
            if (key == "tank")
            {
                spawned = ReviveTankFromDanmu(command);
            }
            else if (key == "aircraft" || key == "plane" || key == "helicopter")
            {
                spawned = ReviveAircraftFromDanmu(command);
            }
            else if (key == "medic")
            {
                spawned = HealHumanForces(22f);
            }
            else
            {
                spawned = ReviveSoldierFromDanmu(command);
            }

            if (!spawned)
            {
                HealHumanForces(10f);
            }

            ShowBanner("Danmu human reinforce", false, 0.85f);
            return;
        }

        bool revived = ReviveGiantFromDanmu(command);
        if (!revived)
        {
            HealGiants(90f);
            HastenGiants(0.2f);
        }

        ShowBanner("Danmu monster reinforce", true, 0.85f);
    }

    private static string NormalizeDanmuKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }

    private void ApplyDanmuSkill(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            Vector2 center = GetActiveGiantCenter();
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.HumanAirStrikeWarning, ToWorldPoint(center.x, center.y, 0.05f), Quaternion.identity, null, 2.2f));
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.ExplosionLarge, ToWorldPoint(center.x, center.y, 0.35f), Quaternion.identity, null, 2.6f));
            }

            DamageGiantsInArea(center.x, center.y, 290f, 330f);
            ShowBanner("Danmu air strike", true, 1.1f);
            return;
        }

        HealGiants(170f);
        HastenGiants(0.08f);
        if (EffectManager.Instance != null)
        {
            Vector2 center = GetActiveGiantCenter();
            EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.OrcRageBuff, ToWorldPoint(center.x, center.y, 0.25f), Quaternion.identity, null, 2.8f));
        }

        ShowBanner("Danmu monster rage", true, 1.1f);
    }

    private void ApplyDanmuHeal(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            HealHumanForces(36f + command.value * 0.35f);
            ShowBanner("Danmu human heal", false, 0.85f);
            return;
        }

        HealGiants(110f + command.value * 0.6f);
        ShowBanner("Danmu monster heal", true, 0.85f);
    }

    private void ApplyDanmuBuff(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            ReduceHumanCooldowns(0.18f);
            ShowBanner("Danmu focus fire", false, 0.85f);
            return;
        }

        HastenGiants(0.18f);
        ShowBanner("Danmu monster haste", true, 0.85f);
    }
}
