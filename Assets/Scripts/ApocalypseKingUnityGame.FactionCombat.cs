using System.Collections.Generic;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private struct PendingSpawnRequest
    {
        public FactionId Faction;
        public ApocalypseUnitRole Role;
    }

    private readonly Queue<PendingSpawnRequest> pendingSpawnQueue = new Queue<PendingSpawnRequest>();
    private readonly Dictionary<FactionId, float> factionInteractionScores = new Dictionary<FactionId, float>();
    private string lastSettlementSummary = string.Empty;

    private Transform blueBaseMarker;
    private Transform greenBaseMarker;
    private Transform zombieBaseMarker;

    private static readonly Color BlueBaseColor = new Color(0.28f, 0.55f, 1f, 0.85f);
    private static readonly Color GreenBaseColor = new Color(0.35f, 0.95f, 0.45f, 0.85f);
    private static readonly Color ZombieBaseColor = new Color(0.85f, 0.32f, 0.95f, 0.85f);

    public string DiagnosticsSettlementSummary => lastSettlementSummary;

    private FactionId GetEffectiveFaction(BattleUnit unit)
    {
        if (unit == null)
        {
            return FactionId.Neutral;
        }

        if (unit.infectionTimer > 0f)
        {
            return FactionId.Zombie;
        }

        return unit.faction != FactionId.Neutral ? unit.faction : FactionId.Blue;
    }

    private bool AreFactionsHostile(FactionId a, FactionId b)
    {
        if (a == FactionId.Neutral || b == FactionId.Neutral || a == b)
        {
            return false;
        }

        if (!betrayalActive)
        {
            bool aHuman = a == FactionId.Blue || a == FactionId.Green;
            bool bHuman = b == FactionId.Blue || b == FactionId.Green;
            if (aHuman && bHuman)
            {
                return false;
            }

            return aHuman != bHuman;
        }

        bool aRebel = a == betrayalAlly || a == FactionId.Zombie;
        bool bRebel = b == betrayalAlly || b == FactionId.Zombie;
        if (aRebel && bRebel)
        {
            return false;
        }

        return aRebel != bRebel;
    }

    private bool IsHostileUnit(BattleUnit a, BattleUnit b)
    {
        if (a == null || b == null || !a.active || !b.active)
        {
            return false;
        }

        return AreFactionsHostile(GetEffectiveFaction(a), GetEffectiveFaction(b));
    }

    private BattleUnit FindNearestEnemy(BattleUnit origin, bool includeAircraft)
    {
        if (origin == null || !origin.active)
        {
            return null;
        }

        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;
        ConsiderEnemyPool(soldiers, origin, includeAircraft, ref best, ref bestScore);
        ConsiderEnemyPool(tanks, origin, includeAircraft, ref best, ref bestScore);
        ConsiderEnemyPool(aircraft, origin, includeAircraft, ref best, ref bestScore);
        ConsiderEnemyPool(giants, origin, includeAircraft, ref best, ref bestScore);
        return best;
    }

    private void ConsiderEnemyPool(List<BattleUnit> units, BattleUnit origin, bool includeAircraft, ref BattleUnit best, ref float bestScore)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var candidate = units[i];
            if (candidate == null || !candidate.active || candidate.id == origin.id)
            {
                continue;
            }

            if (!includeAircraft && candidate.kind == UnitKind.Aircraft)
            {
                continue;
            }

            if (!IsHostileUnit(origin, candidate))
            {
                continue;
            }

            float priority = candidate.apocalypseRole == ApocalypseUnitRole.SuperHeavy ? 0.82f : 1f;
            float score = DistanceSq(origin.x, origin.z, candidate.x, candidate.z) * priority;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
    }

    private float ScaleOutgoingDamage(BattleUnit attacker, BattleUnit defender, float amount)
    {
        if (attacker == null || defender == null || amount <= 0f)
        {
            return amount;
        }

        return amount * ApocalypseCombatMatrix.GetDamageMultiplier(attacker.apocalypseRole, defender.apocalypseRole);
    }

    private void TickInfectionTimers(float dt)
    {
        TickGroupInfection(soldiers, dt);
        TickGroupInfection(tanks, dt);
        TickGroupInfection(aircraft, dt);
    }

    private void TickGroupInfection(List<BattleUnit> units, float dt)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.active || unit.infectionTimer <= 0f)
            {
                continue;
            }

            unit.infectionTimer -= dt;
            if (unit.infectionTimer <= 0f)
            {
                unit.faction = unit.preInfectionFaction != FactionId.Neutral ? unit.preInfectionFaction : unit.faction;
                unit.preInfectionFaction = FactionId.Neutral;
                TintUnitFaction(unit, unit.faction);
            }
        }
    }

    private void TryApplyInfection(BattleUnit giant, BattleUnit victim)
    {
        if (giant == null || victim == null || !victim.active)
        {
            return;
        }

        if (victim.kind == UnitKind.Aircraft || GetEffectiveFaction(victim) == FactionId.Zombie)
        {
            return;
        }

        float chance = matchSettings != null ? matchSettings.InfectionChance : 0.12f;
        if (Random.value > chance)
        {
            return;
        }

        float duration = matchSettings != null ? matchSettings.InfectionDurationSeconds : 3f;
        victim.preInfectionFaction = victim.faction != FactionId.Neutral ? victim.faction : FactionId.Blue;
        victim.infectionTimer = duration;
        TintUnitFaction(victim, FactionId.Zombie);
        ShowBanner("丧尸感染！", true, 1.1f);
        TrySpawnApocalypseRole(FactionId.Zombie, ApocalypseUnitRole.ZombieGrunt);
    }

    private void RecordInteractionScore(FactionId faction, int amount)
    {
        if (faction == FactionId.Neutral || amount <= 0)
        {
            return;
        }

        if (!factionInteractionScores.ContainsKey(faction))
        {
            factionInteractionScores[faction] = 0f;
        }

        factionInteractionScores[faction] += amount;
    }

    private void RecordDanmuEconomy(DanmuCommand command)
    {
        FactionId faction = ResolveCommandFaction(command);
        int score = command.value;
        if (command.type == DanmuCommandType.Like)
        {
            score = 50;
        }
        else if (command.type == DanmuCommandType.JoinFaction)
        {
            score = 20;
        }
        else if (giftCatalog != null && giftCatalog.TryResolve(command.key, out ApocalypseGiftEntry entry))
        {
            score = Mathf.Max(10, entry.CoinValue * Mathf.Max(1, command.value));
        }

        RecordInteractionScore(faction, score);
    }

    private void BuildSettlementSummary()
    {
        float blue = GetFactionScore(FactionId.Blue);
        float green = GetFactionScore(FactionId.Green);
        float zombie = GetFactionScore(FactionId.Zombie);
        float pool = GetPointPoolTotal();
        float carry = pool * 0.3f;
        float split = pool * 0.7f;

        FactionId winner = FactionId.Blue;
        float topBase = GetBlueBaseHpPercent();
        if (GetGreenBaseHpPercent() > topBase)
        {
            winner = FactionId.Green;
            topBase = GetGreenBaseHpPercent();
        }

        if (GetZombieBaseHpPercent() > topBase)
        {
            winner = FactionId.Zombie;
        }

        lastSettlementSummary =
            $"胜方 {FactionLabel(winner)} | 池 {pool:N0} 瓜分 {split:N0} 留存 {carry:N0}\n" +
            $"互动分 蓝 {blue:N0} 绿 {green:N0} 尸 {zombie:N0}";
    }

    private float GetFactionScore(FactionId faction)
    {
        return factionInteractionScores.TryGetValue(faction, out float score) ? score : 0f;
    }

    internal float GetPointPoolTotal()
    {
        float basePool = matchSettings != null ? matchSettings.PointPoolBase : 380000f;
        float perSec = matchSettings != null ? matchSettings.PointPoolPerSecond : 8200f;
        return basePool + battleTime * perSec + humanLosses * 2600f;
    }

    private bool ShowWorldBaseMarkersEnabled()
    {
        return matchSettings != null && matchSettings.ShowWorldBaseMarkers;
    }

    private void ClearWorldBaseMarkers()
    {
        DestroyMarker(ref blueBaseMarker);
        DestroyMarker(ref greenBaseMarker);
        DestroyMarker(ref zombieBaseMarker);
    }

    private static void DestroyMarker(ref Transform marker)
    {
        if (marker == null)
        {
            return;
        }

        Object.Destroy(marker.gameObject);
        marker = null;
    }

    private void EnsureBaseMarkers()
    {
        if (!ShowWorldBaseMarkersEnabled())
        {
            ClearWorldBaseMarkers();
            return;
        }

        if (blueBaseMarker != null)
        {
            return;
        }

        blueBaseMarker = CreateBaseMarker("BlueBase", BlueBaseColor, blueBase);
        greenBaseMarker = CreateBaseMarker("GreenBase", GreenBaseColor, greenBase);
        zombieBaseMarker = CreateBaseMarker("ZombieBase", ZombieBaseColor, zombieBase);
    }

    private Transform CreateBaseMarker(string name, Color color, ApocalypseBaseState state)
    {
        if (state == null || decorRoot == null)
        {
            return null;
        }

        var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        root.name = name;
        root.transform.SetParent(decorRoot, false);
        root.transform.position = ToWorldPoint(state.WorldX, state.WorldZ, 1.2f);
        root.transform.localScale = new Vector3(14f, 0.35f, 14f);
        var renderer = root.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color padColor = new Color(color.r, color.g, color.b, 0.22f);
            renderer.sharedMaterial = GetTransparentMaterial(padColor);
        }

        var collider = root.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = name + "_Beacon";
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        beacon.transform.localScale = new Vector3(0.22f, 2.4f, 0.22f);
        var beaconRenderer = beacon.GetComponent<Renderer>();
        if (beaconRenderer != null)
        {
            beaconRenderer.sharedMaterial = GetTransparentMaterial(new Color(color.r, color.g, color.b, 0.55f));
        }

        var beaconCollider = beacon.GetComponent<Collider>();
        if (beaconCollider != null)
        {
            Object.Destroy(beaconCollider);
        }

        return root.transform;
    }

    private void RefreshBaseMarkers()
    {
        if (!ShowWorldBaseMarkersEnabled() || blueBase == null)
        {
            return;
        }

        UpdateBaseMarkerScale(blueBaseMarker, GetBlueBaseHpPercent());
        UpdateBaseMarkerScale(greenBaseMarker, GetGreenBaseHpPercent());
        UpdateBaseMarkerScale(zombieBaseMarker, GetZombieBaseHpPercent());
    }

    private static void UpdateBaseMarkerScale(Transform marker, float hpPercent)
    {
        if (marker == null)
        {
            return;
        }

        float h = Mathf.Lerp(0.2f, 0.45f, Mathf.Clamp01(hpPercent));
        marker.localScale = new Vector3(14f, h, 14f);
        Transform beacon = marker.Find(marker.name + "_Beacon");
        if (beacon != null)
        {
            float beam = Mathf.Lerp(1.2f, 3.2f, Mathf.Clamp01(hpPercent));
            beacon.localScale = new Vector3(0.22f, beam, 0.22f);
        }
    }

    private void ApplyCameraPreset(int index)
    {
        if (orbitCamera == null || cameraTarget == null)
        {
            return;
        }

        switch (index)
        {
            case 0:
                orbitCamera.yaw = -12f;
                orbitCamera.pitch = 26f;
                orbitCamera.distance = 92f;
                cameraTarget.localPosition = new Vector3(0f, 0f, -2.4f);
                break;
            case 1:
                orbitCamera.yaw = -34f;
                orbitCamera.pitch = 30f;
                orbitCamera.distance = 98f;
                cameraTarget.localPosition = new Vector3(HumanCastleWorldX + 4f, 0f, -1.2f);
                break;
            case 2:
                orbitCamera.yaw = 16f;
                orbitCamera.pitch = 30f;
                orbitCamera.distance = 98f;
                cameraTarget.localPosition = new Vector3(BeastCastleWorldX - 4f, 0f, -1f);
                break;
            default:
                orbitCamera.yaw = -6f;
                orbitCamera.pitch = 34f;
                orbitCamera.distance = 64f;
                cameraTarget.localPosition = new Vector3(0f, 0f, -3.2f);
                break;
        }

        cameraYaw = orbitCamera.yaw;
        cameraPitch = orbitCamera.pitch;
        cameraDistance = orbitCamera.distance;
    }

    private void HandleCameraPresetInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ApplyCameraPreset(0);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            ApplyCameraPreset(1);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            ApplyCameraPreset(2);
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            ApplyCameraPreset(3);
        }

        if (Input.GetKeyDown(KeyCode.Space) && matchPhase == MatchPhase.Battle)
        {
            ApplyCameraPreset(0);
        }
    }
}
