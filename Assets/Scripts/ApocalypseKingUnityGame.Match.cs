using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const string MatchSettingsResourcesPath = "Apocalypse/ApocalypseMatchSettings";
    private const string GiftCatalogResourcesPath = "Apocalypse/ApocalypseGiftCatalog";

    [SerializeField] internal ApocalypseMatchSettings matchSettings;
    [SerializeField] internal ApocalypseGiftCatalog giftCatalog;

    internal MatchPhase matchPhase = MatchPhase.ModeSelect;
    private ApocalypseBaseState blueBase;
    private ApocalypseBaseState greenBase;
    private ApocalypseBaseState zombieBase;
    private bool betrayalActive;
    private FactionId betrayalAlly = FactionId.Neutral;
    private float globalAttackBuffTimer;
    private float globalAttackBuffMultiplier = 1f;
    private float nuclearTimer;
    private readonly System.Collections.Generic.Dictionary<string, FactionId> viewerFactions =
        new System.Collections.Generic.Dictionary<string, FactionId>();

    public MatchPhase DiagnosticsMatchPhase => matchPhase;

    private void EnsureApocalypseConfigs()
    {
        if (matchSettings == null)
        {
            matchSettings = Resources.Load<ApocalypseMatchSettings>(MatchSettingsResourcesPath);
        }

        if (matchSettings == null)
        {
            matchSettings = ApocalypseMatchSettings.CreateRuntimeDefault();
        }

        if (giftCatalog == null)
        {
            giftCatalog = Resources.Load<ApocalypseGiftCatalog>(GiftCatalogResourcesPath);
        }

        if (giftCatalog == null)
        {
            giftCatalog = ScriptableObject.CreateInstance<ApocalypseGiftCatalog>();
            giftCatalog.Entries = ApocalypseGiftCatalog.CreateDefaultEntries();
        }
    }

    private void InitApocalypseBases()
    {
        float hp = matchSettings != null ? matchSettings.BaseMaxHp : 100000f;
        blueBase = new ApocalypseBaseState(FactionId.Blue, hp, HumanCastleGateX - 18f, HumanCastleCenterZ);
        greenBase = new ApocalypseBaseState(FactionId.Green, hp, BeastCastleGateX + 18f, BeastCastleCenterZ);
        zombieBase = new ApocalypseBaseState(FactionId.Zombie, hp, BeastCastleGateX + 42f, -96f);
        betrayalActive = false;
        betrayalAlly = FactionId.Neutral;
        nuclearTimer = matchSettings != null ? matchSettings.NuclearCountdownSeconds : 90f;
    }

    private void BeginApocalypseBattle()
    {
        matchPhase = MatchPhase.Battle;
        ended = false;
        paused = false;
        battleTime = 0f;
        factionInteractionScores.Clear();
        pendingSpawnQueue.Clear();
        lastSettlementSummary = string.Empty;
        InitApocalypseBases();
        ResetBattle();
        EnsureBaseMarkers();
        ShowBanner("末日之王 — 摧毁敌方基地获胜 | F1-F4 镜头", false, 2.4f);
    }

    private void HandleMatchPhaseInput()
    {
        HandleModeSelectSettingsInput();

        if (Input.GetKeyDown(KeyCode.Return) && matchPhase == MatchPhase.ModeSelect)
        {
            ApplyModeSettingsToMatch();
            BeginApocalypseBattle();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) && matchPhase == MatchPhase.Result)
        {
            matchPhase = MatchPhase.ModeSelect;
            ended = false;
            ShowBanner("模式选择：Enter 开战", false, 2f);
        }
    }

    private float MatchDurationSeconds()
    {
        return matchSettings != null ? matchSettings.MatchDurationSeconds : 600f;
    }

    private void UpdateApocalypseMatch(float dt)
    {
        if (matchPhase != MatchPhase.Battle || ended)
        {
            return;
        }

        float nuclearBefore = nuclearTimer;
        nuclearTimer = Mathf.Max(0f, nuclearTimer - dt);
        if (nuclearBefore > 0f && nuclearTimer <= 0f)
        {
            TryBeginNuclearCountdownStrike();
        }

        UpdateNuclearStrikeSequence(dt);
        if (globalAttackBuffTimer > 0f)
        {
            globalAttackBuffTimer -= dt;
            if (globalAttackBuffTimer <= 0f)
            {
                globalAttackBuffMultiplier = 1f;
            }
        }

        ApplyBaseSiegeDamage(dt);
        TickInfectionTimers(dt);
        TickBurnStatuses(dt);
        RefreshBaseMarkers();
        CheckApocalypseMatchEnd();
    }

    private void ApplyBaseSiegeDamage(float dt)
    {
        SiegeBase(soldiers, dt);
        SiegeBase(tanks, dt);
        SiegeBase(aircraft, dt);
        SiegeBase(giants, dt);
    }

    private void SiegeBase(System.Collections.Generic.List<BattleUnit> units, float dt)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.active)
            {
                continue;
            }

            ApocalypseBaseState target = GetEnemyBaseForUnit(unit);
            if (target == null || target.Destroyed)
            {
                continue;
            }

            float dx = unit.x - target.WorldX;
            float dz = unit.z - target.WorldZ;
            if (dx * dx + dz * dz > CastleSiegeDamageRadius * CastleSiegeDamageRadius)
            {
                continue;
            }

            float dps = unit.kind == UnitKind.Giant ? 520f : unit.kind == UnitKind.Tank ? 220f : 58f;
            target.ApplyDamage(dps * dt);
        }
    }

    private const float CastleSiegeStandoffX = 48f;
    private const float CastleSiegeAggroRadius = 300f;
    private const float CastleSiegeDamageRadius = 168f;

    private ApocalypseBaseState GetEnemyBaseForUnit(BattleUnit unit)
    {
        FactionId faction = GetEffectiveFaction(unit);
        if (faction == FactionId.Zombie)
        {
            if (betrayalActive)
            {
                return betrayalAlly == FactionId.Blue ? greenBase : blueBase;
            }

            return blueBase.Hp >= greenBase.Hp ? blueBase : greenBase;
        }

        if (faction == FactionId.Blue || faction == FactionId.Green)
        {
            if (betrayalActive && faction == betrayalAlly)
            {
                return faction == FactionId.Blue ? greenBase : blueBase;
            }

            return zombieBase;
        }

        return zombieBase;
    }

    private bool TryGetEnemyCastleSiegePoint(BattleUnit unit, out float siegeX, out float siegeZ)
    {
        siegeX = 0f;
        siegeZ = 0f;
        if (unit == null)
        {
            return false;
        }

        if (matchPhase == MatchPhase.Battle)
        {
            ApocalypseBaseState enemyBase = GetEnemyBaseForUnit(unit);
            if (enemyBase == null || enemyBase.Destroyed)
            {
                return false;
            }

            siegeX = enemyBase.WorldX - unit.facing * CastleSiegeStandoffX;
            siegeZ = enemyBase.WorldZ;
            return true;
        }

        FactionId faction = GetEffectiveFaction(unit);
        if (faction == FactionId.Zombie || unit.team == TeamKind.Giant)
        {
            siegeX = HumanCastleGateX + CastleSiegeStandoffX;
            siegeZ = HumanCastleCenterZ;
            return true;
        }

        siegeX = BeastCastleGateX - CastleSiegeStandoffX;
        siegeZ = BeastCastleCenterZ;
        return true;
    }

    private bool IsEnemyCastleInSiegeRange(BattleUnit unit)
    {
        if (unit == null || matchPhase != MatchPhase.Battle)
        {
            return false;
        }

        ApocalypseBaseState enemyBase = GetEnemyBaseForUnit(unit);
        if (enemyBase == null || enemyBase.Destroyed)
        {
            return false;
        }

        float dx = unit.x - enemyBase.WorldX;
        float dz = unit.z - enemyBase.WorldZ;
        return dx * dx + dz * dz <= CastleSiegeDamageRadius * CastleSiegeDamageRadius;
    }

    private bool IsUnitInCastleAggro(BattleUnit unit, BattleUnit enemy)
    {
        if (unit == null || enemy == null)
        {
            return false;
        }

        float radiusSq = CastleSiegeAggroRadius * CastleSiegeAggroRadius;
        return DistanceSq(unit.x, unit.z, enemy.x, enemy.z) <= radiusSq;
    }

    private void CheckApocalypseMatchEnd()
    {
        if (ended)
        {
            return;
        }

        float duration = MatchDurationSeconds();
        bool timeUp = battleTime >= duration;

        if (!timeUp)
        {
            if (zombieBase.Destroyed && !betrayalActive && matchSettings != null && matchSettings.BetrayalEnabled)
            {
                TryStartBetrayal();
                return;
            }

            if (blueBase.Destroyed || greenBase.Destroyed)
            {
                EndApocalypseMatch("基地被摧毁");
                return;
            }

            if (zombieBase.Destroyed && betrayalActive)
            {
                EndApocalypseMatch("丧尸基地覆灭 — 人类胜利");
                return;
            }

            return;
        }

        EndApocalypseMatch(ResolveTimedWinnerLabel());
    }

    private void TryStartBetrayal()
    {
        betrayalActive = true;
        betrayalAlly = Random.value < (matchSettings != null ? matchSettings.BetrayalChance : 0.35f)
            ? FactionId.Green
            : FactionId.Blue;
        zombieBase.RestorePercent(matchSettings != null ? matchSettings.BetrayalBaseRestorePercent : 0.01f);
        float extra = matchSettings != null ? matchSettings.BetrayalExtraSeconds : 120f;
        if (matchSettings != null)
        {
            matchSettings.MatchDurationSeconds += extra;
        }

        if (EffectManager.Instance != null && zombieBase != null)
        {
            EffectManager.Instance.Play(EffectPlayback.Create(
                BattleEffectId.OrcRageBuff,
                ToWorldPoint(zombieBase.WorldX, zombieBase.WorldZ, 0.4f),
                Quaternion.identity,
                null,
                1.6f));
        }

        ShowBanner($"叛变！{FactionLabel(betrayalAlly)} 联手丧尸", true, 4f);
    }

    private string ResolveTimedWinnerLabel()
    {
        float b = blueBase.Hp;
        float g = greenBase.Hp;
        float z = zombieBase.Hp;
        if (b >= g && b >= z)
        {
            return "时间到 — 蓝军基地领先";
        }

        if (g >= b && g >= z)
        {
            return "时间到 — 绿军基地领先";
        }

        return "时间到 — 丧尸基地领先";
    }

    private void EndApocalypseMatch(string reason)
    {
        ended = true;
        matchPhase = MatchPhase.Result;
        BuildSettlementSummary();
        ShowBanner($"{reason}\n{lastSettlementSummary}", true, 6f);
    }

    private static string FactionLabel(FactionId faction)
    {
        switch (faction)
        {
            case FactionId.Blue:
                return "蓝军";
            case FactionId.Green:
                return "绿军";
            case FactionId.Zombie:
                return "丧尸";
            default:
                return "中立";
        }
    }

    private void RegisterViewerFaction(string userId, FactionId faction)
    {
        if (string.IsNullOrEmpty(userId) || faction == FactionId.Neutral)
        {
            return;
        }

        viewerFactions[userId] = faction;
    }

    private FactionId ResolveCommandFaction(DanmuCommand command)
    {
        if (command.faction != FactionId.Neutral)
        {
            return command.faction;
        }

        if (!string.IsNullOrEmpty(command.userId) && viewerFactions.TryGetValue(command.userId, out FactionId saved))
        {
            return saved;
        }

        return FactionId.Blue;
    }

    public float GetBlueBaseHpPercent() => blueBase != null ? blueBase.Hp / Mathf.Max(1f, blueBase.MaxHp) : 1f;
    public float GetGreenBaseHpPercent() => greenBase != null ? greenBase.Hp / Mathf.Max(1f, greenBase.MaxHp) : 1f;
    public float GetZombieBaseHpPercent() => zombieBase != null ? zombieBase.Hp / Mathf.Max(1f, zombieBase.MaxHp) : 1f;
    public float GetNuclearTimer() => nuclearTimer;
    public bool IsBetrayalActive() => betrayalActive;
}
