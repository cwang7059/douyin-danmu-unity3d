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
            danmuQueue.EnqueueRawMessage("local-blue", "Local Blue", "1");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            danmuQueue.EnqueueRawMessage("local-green", "Local Green", "2");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            danmuQueue.EnqueueRawMessage("local-zombie", "Local Zombie", "3");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (matchSettings != null)
            {
                matchSettings.RageLikeEnabled = !matchSettings.RageLikeEnabled;
                ShowBanner(matchSettings.RageLikeEnabled ? "狂暴点赞 ON" : "狂暴点赞 OFF", true, 1.2f);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            danmuQueue.EnqueueRawMessage("local-gift", "Local Gift", "仙女棒");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            danmuQueue.EnqueueRawMessage("local-skill", "Local Skill", "超能喷射");
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
        if (matchPhase == MatchPhase.Battle || command.type == DanmuCommandType.JoinFaction)
        {
            ApplyApocalypseDanmuCommand(command);
            if (command.type == DanmuCommandType.JoinFaction || matchPhase == MatchPhase.Battle)
            {
                return;
            }
        }

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
            bool spawned = ApplyHumanDanmuSpawnAction(ResolveHumanDanmuSpawnAction(command.key), command);

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
            ShowBanner(CountActive(giants) >= GiantCount ? "丧尸已满" : "弹幕增援丧尸", true, 0.85f);
            return;
        }

        ShowBanner("弹幕增援丧尸", true, 0.85f);
    }

    private DanmuHumanSpawnAction ResolveHumanDanmuSpawnAction(string key)
    {
        if (danmuSpawnMappingConfig != null)
        {
            if (danmuSpawnMappingConfig.TryResolveHumanAction(key, out DanmuHumanSpawnAction action))
            {
                return action;
            }

            return danmuSpawnMappingConfig.DefaultHumanAction;
        }

        return DanmuSpawnMappingConfig.ResolveDefaultHumanAction(key);
    }

    private bool ApplyHumanDanmuSpawnAction(DanmuHumanSpawnAction action, DanmuCommand command)
    {
        switch (action)
        {
            case DanmuHumanSpawnAction.Tank:
                return ReviveTankFromDanmu(command);
            case DanmuHumanSpawnAction.Aircraft:
                return ReviveAircraftFromDanmu(command);
            case DanmuHumanSpawnAction.Heal:
                return HealHumanForces(22f);
            default:
                return ReviveSoldierFromDanmu(command);
        }
    }

    private void ApplyDanmuSkill(DanmuCommand command)
    {
        if (command.team == BattleTeam.Human)
        {
            Vector2 center = GetActiveGiantCenter();
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.HumanAirStrikeWarning, ToWorldPoint(center.x, center.y, 0.05f), Quaternion.identity, null, 1.45f));
                EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.ExplosionLarge, ToWorldPoint(center.x, center.y, 0.35f), Quaternion.identity, null, 1.15f));
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
            EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.OrcRageBuff, ToWorldPoint(center.x, center.y, 0.25f), Quaternion.identity, null, 1.35f));
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
