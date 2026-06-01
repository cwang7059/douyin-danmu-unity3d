using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private static readonly float[] GreenSoldierLanes = { 180f, 228f, 276f, 132f, 84f, 36f, -12f, -60f, -108f, -156f };

    private void ApplyApocalypseDanmuCommand(DanmuCommand command)
    {
        FactionId faction = ResolveCommandFaction(command);
        command.faction = faction;
        command.team = DanmuCommand.TeamFromFaction(faction);

        switch (command.type)
        {
            case DanmuCommandType.JoinFaction:
                RegisterViewerFaction(command.userId, faction);
                ShowBanner($"加入{FactionLabel(faction)}", faction == FactionId.Zombie, 1.2f);
                SpawnApocalypseLike(faction, 5);
                break;
            case DanmuCommandType.Like:
                int likeCount = matchSettings != null && matchSettings.RageLikeEnabled
                    ? 10 * Mathf.Max(1, matchSettings.RageLikeMultiplier)
                    : 10;
                SpawnApocalypseLike(faction, likeCount);
                break;
            case DanmuCommandType.SpawnUnit:
                ApplyApocalypseSpawn(command, faction);
                break;
            case DanmuCommandType.CastSkill:
                ApplyApocalypseSkill(command, faction);
                break;
            default:
                ApplyDanmuCommand(command);
                break;
        }
    }

    private void ApplyApocalypseSpawn(DanmuCommand command, FactionId faction)
    {
        if (giftCatalog != null && giftCatalog.TryResolve(command.key, out ApocalypseGiftEntry entry))
        {
            var batches = giftCatalog.GetSpawns(faction, entry);
            int mult = Mathf.Max(1, command.value);
            for (int i = 0; i < batches.Length; i++)
            {
                SpawnRoleBatch(faction, batches[i].Role, batches[i].Count * mult);
            }

            ShowBanner($"{FactionLabel(faction)} — {command.key}", faction == FactionId.Zombie, 0.9f);
            return;
        }

        if (string.Equals(command.key, "666", System.StringComparison.OrdinalIgnoreCase))
        {
            SpawnRoleBatch(faction, faction == FactionId.Zombie ? ApocalypseUnitRole.ZombieGrunt : ApocalypseUnitRole.Survivor, 100);
            return;
        }

        ApplyDanmuSpawn(command);
    }

    private void ApplyApocalypseSkill(DanmuCommand command, FactionId faction)
    {
        if (string.Equals(command.key, "superjet", System.StringComparison.OrdinalIgnoreCase))
        {
            TriggerSuperJetStrike();
            globalAttackBuffMultiplier = 1.15f + 0.1f * Mathf.Clamp(command.value, 1, 4);
            globalAttackBuffTimer = 45f;
            nuclearTimer = matchSettings != null ? matchSettings.NuclearCountdownSeconds : 90f;
            ShowBanner("超能喷射 — 全线轰炸 + 全军强化", true, 2.5f);
            return;
        }

        ApplyDanmuSkill(command);
    }

    private void TriggerSuperJetStrike()
    {
        Vector2 center = GetActiveGiantCenter();
        if (center == Vector2.zero)
        {
            center = new Vector2(Left - 80f, 0f);
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.HumanAirStrikeWarning, ToWorldPoint(center.x, center.y, 0.05f), Quaternion.identity, null, 1.45f));
            EffectManager.Instance.Play(EffectPlayback.Create(BattleEffectId.ExplosionLarge, ToWorldPoint(center.x, center.y, 0.35f), Quaternion.identity, null, 1.15f));
        }

        DamageGiantsInArea(center.x, center.y, 320f, 380f);
        if (zombieBase != null)
        {
            zombieBase.ApplyDamage(8000f);
        }
    }

    private void SpawnApocalypseLike(FactionId faction, int count)
    {
        ApocalypseUnitRole role = faction == FactionId.Zombie ? ApocalypseUnitRole.ZombieGrunt : ApocalypseUnitRole.Survivor;
        SpawnRoleBatch(faction, role, count);
    }

    private void SpawnRoleBatch(FactionId faction, ApocalypseUnitRole role, int count)
    {
        int maxPerFrame = matchSettings != null ? matchSettings.MaxSpawnsPerFrame : 24;
        int spawned = 0;
        for (int i = 0; i < count && spawned < maxPerFrame; i++)
        {
            if (TrySpawnApocalypseRole(faction, role))
            {
                spawned++;
            }
        }

        pendingSpawnBudget += Mathf.Max(0, count - spawned);
    }

    private void DrainPendingSpawns()
    {
        if (pendingSpawnBudget <= 0)
        {
            return;
        }

        int maxPerFrame = matchSettings != null ? matchSettings.MaxSpawnsPerFrame : 24;
        int n = Mathf.Min(pendingSpawnBudget, maxPerFrame);
        pendingSpawnBudget -= n;
        for (int i = 0; i < n; i++)
        {
            TrySpawnApocalypseRole(FactionId.Blue, ApocalypseUnitRole.Survivor);
        }
    }

    private bool TrySpawnApocalypseRole(FactionId faction, ApocalypseUnitRole role)
    {
        switch (role)
        {
            case ApocalypseUnitRole.AirUnit:
                return ReviveApocalypseAircraft(faction);
            case ApocalypseUnitRole.RushVehicle:
            case ApocalypseUnitRole.ShieldTank:
            case ApocalypseUnitRole.Artillery:
                return ReviveApocalypseTank(faction, role);
            case ApocalypseUnitRole.SuperHeavy:
            case ApocalypseUnitRole.ZombieGiant:
                return ReviveApocalypseGiant(faction);
            case ApocalypseUnitRole.ZombieGrunt:
            case ApocalypseUnitRole.ZombieHound:
                return ReviveApocalypseGiant(faction) || ReviveApocalypseSoldier(faction, role);
            default:
                return ReviveApocalypseSoldier(faction, role);
        }
    }

    private bool ReviveApocalypseSoldier(FactionId faction, ApocalypseUnitRole role)
    {
        var unit = FindInactiveUnit(soldiers);
        if (unit == null)
        {
            return false;
        }

        float[] lanes = faction == FactionId.Green ? GreenSoldierLanes : SoldierLanes;
        int lane = processedDanmuCommandCount % lanes.Length;
        int rank = CountActiveFaction(soldiers, faction) / lanes.Length;
        float x = faction == FactionId.Green ? Right - 190f - rank * 38f : Left + 190f + rank * 38f;
        float z = lanes[lane] + (Noise(processedDanmuCommandCount + 29f) - 0.5f) * 12f;
        int facing = faction == FactionId.Green ? -1 : 1;
        float hpMul = role == ApocalypseUnitRole.MeleeGrunt ? 1.1f : role == ApocalypseUnitRole.RangedGrunt ? 0.95f : 1f;
        float dmgMul = role == ApocalypseUnitRole.RangedGrunt ? 1.2f : 1f;
        ActivateUnit(unit, x, z,
            soldierConfig.MaxHp * hpMul,
            soldierConfig.Damage * dmgMul * globalAttackBuffMultiplier,
            soldierConfig.MoveSpeed + Noise(processedDanmuCommandCount) * 12f,
            soldierConfig.Radius,
            soldierConfig.AttackRange,
            soldierConfig.AttackInterval,
            rank, facing, 0f);
        unit.faction = faction;
        unit.apocalypseRole = role;
        unit.team = TeamKind.Human;
        TintUnitFaction(unit, faction);
        PlayDanmuSpawnEffect(
            faction == FactionId.Zombie ? BattleEffectId.OrcSummon : BattleEffectId.HumanSummon,
            unit.x,
            unit.z,
            0.92f);
        return true;
    }

    private bool ReviveApocalypseTank(FactionId faction, ApocalypseUnitRole role)
    {
        var unit = FindInactiveUnit(tanks);
        if (unit == null)
        {
            return ReviveApocalypseSoldier(faction, role);
        }

        float x = faction == FactionId.Green ? Right - 60f : Left + 40f;
        float z = TankLanes[processedDanmuCommandCount % TankLanes.Length];
        float hpMul = role == ApocalypseUnitRole.ShieldTank ? 1.8f : role == ApocalypseUnitRole.Artillery ? 0.85f : 1.2f;
        ActivateUnit(unit, x, z,
            tankConfig.MaxHp * hpMul,
            tankConfig.Damage * globalAttackBuffMultiplier,
            tankConfig.MoveSpeed * (role == ApocalypseUnitRole.RushVehicle ? 1.35f : 1f),
            tankConfig.Radius,
            tankConfig.AttackRange + (role == ApocalypseUnitRole.Artillery ? 80f : 0f),
            tankConfig.AttackInterval,
            0, faction == FactionId.Green ? -1 : 1, 0f);
        unit.faction = faction;
        unit.apocalypseRole = role;
        unit.team = TeamKind.Human;
        TintUnitFaction(unit, faction);
        return true;
    }

    private bool ReviveApocalypseAircraft(FactionId faction)
    {
        var unit = FindInactiveUnit(aircraft);
        if (unit == null)
        {
            return ReviveApocalypseTank(faction, ApocalypseUnitRole.AirUnit);
        }

        float x = faction == FactionId.Green ? Right - 80f : Left + 52f;
        float z = AirLanes[processedDanmuCommandCount % AirLanes.Length];
        ActivateUnit(unit, x, z,
            aircraftConfig.MaxHp,
            aircraftConfig.Damage * globalAttackBuffMultiplier,
            aircraftConfig.MoveSpeed,
            aircraftConfig.Radius,
            aircraftConfig.AttackRange,
            aircraftConfig.AttackInterval,
            0, faction == FactionId.Green ? -1 : 1, 2.5f);
        unit.faction = faction;
        unit.apocalypseRole = ApocalypseUnitRole.AirUnit;
        unit.team = TeamKind.Human;
        TintUnitFaction(unit, faction);
        return true;
    }

    private bool ReviveApocalypseGiant(FactionId faction)
    {
        var unit = FindInactiveUnit(giants);
        if (unit == null)
        {
            return false;
        }

        float x = Left - 120f - CountActiveFaction(giants, FactionId.Zombie) * 42f;
        float z = -460f + (processedDanmuCommandCount % 5) * 230f;
        ActivateUnit(unit, x, z,
            giantConfig.MaxHp * 1.4f,
            giantConfig.Damage * globalAttackBuffMultiplier,
            giantConfig.MoveSpeed,
            giantConfig.Radius,
            giantConfig.AttackRange,
            giantConfig.AttackInterval,
            0, -1, 0f);
        unit.faction = FactionId.Zombie;
        unit.apocalypseRole = ApocalypseUnitRole.SuperHeavy;
        unit.team = TeamKind.Giant;
        TintUnitFaction(unit, FactionId.Zombie);
        return true;
    }

    private int CountActiveFaction(System.Collections.Generic.List<BattleUnit> units, FactionId faction)
    {
        int n = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].active && units[i].faction == faction)
            {
                n++;
            }
        }

        return n;
    }

    private void TintUnitFaction(BattleUnit unit, FactionId faction)
    {
        if (unit.modelInstance == null)
        {
            return;
        }

        Color tint = faction == FactionId.Blue
            ? new Color(0.55f, 0.75f, 1f)
            : faction == FactionId.Green
                ? new Color(0.55f, 1f, 0.65f)
                : new Color(0.85f, 0.45f, 0.95f);
        var renderers = unit.modelInstance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null && renderers[i].material.HasProperty("_Color"))
            {
                renderers[i].material.color = Color.Lerp(renderers[i].material.color, tint, 0.35f);
            }
        }
    }
}
