using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const float CastleVisualScale = 1.55f;
    private const float CastleModuleSize = 2f;

    private const float GrassHalfWidthWorld = 75f;
    private const float HumanCastleWorldX = -GrassHalfWidthWorld + 17f;
    private const float HumanCastleGateWorldX = -GrassHalfWidthWorld + 27f;
    private const float BeastCastleWorldX = GrassHalfWidthWorld - 17f;
    private const float BeastCastleGateWorldX = GrassHalfWidthWorld - 27f;

    private static float HumanCastleCenterX => WorldToLogicalX(HumanCastleWorldX);
    private static float HumanCastleGateX => WorldToLogicalX(HumanCastleGateWorldX);
    private static float HumanCastleCenterZ => 0f;
    private static float BeastCastleCenterX => WorldToLogicalX(BeastCastleWorldX);
    private static float BeastCastleGateX => WorldToLogicalX(BeastCastleGateWorldX);
    private static float BeastCastleCenterZ => 0f;

    public static float HumanCastleMinUnitX => HumanCastleGateX - 68f * CastleVisualScale;
    public static float BeastCastleMaxUnitX => BeastCastleGateX + 68f * CastleVisualScale;

    private Transform humanCastleRoot;
    private Transform beastCastleRoot;
    private bool loggedCastleFallback;

    private static float WorldToLogicalX(float worldX)
    {
        return worldX / LogicalToWorld;
    }

    private Vector3 CastleWorldPoint(float worldX, float logicalZ, float heightOffset = 0f)
    {
        float worldZ = logicalZ * LogicalToWorld;
        return new Vector3(worldX, SampleBattlefieldGroundHeightWorld(worldX, worldZ) + heightOffset, worldZ);
    }

    private void CreateFactionCastles()
    {
        CacheMedievalVillagePrefabs();
        humanCastleRoot = CreateFactionCastle("HumanCastle", HumanCastleWorldX, HumanCastleCenterZ, false).transform;
        beastCastleRoot = CreateFactionCastle("BeastCastle", BeastCastleWorldX, BeastCastleCenterZ, true).transform;

        if (!HasMedievalCastleAssets() && !loggedCastleFallback)
        {
            loggedCastleFallback = true;
            Debug.LogWarning("[ApocalypseKing] Medieval village castle modules missing; using simple placeholder forts.");
        }

        CreateCastleFlankPads();
        CreateCastleGateRoads();
    }

    private void CreateCastleFlankPads()
    {
        Material grassMaterial = GetTexturedOpaqueMaterial(GrassTextureResourcePath, new Color(0.66f, 0.78f, 0.50f, 1f), new Vector2(8f, 10f), 0.08f);
        float padW = 15f * CastleVisualScale;
        float padH = 18f * CastleVisualScale;
        CreateBattlefieldPlane("HumanCastlePad", CastleWorldPoint(HumanCastleWorldX, 0f, 0.034f), new Vector2(padW, padH), grassMaterial);
        CreateBattlefieldPlane("BeastCastlePad", CastleWorldPoint(BeastCastleWorldX, 0f, 0.034f), new Vector2(padW, padH), grassMaterial);
    }

    private void CreateCastleGateRoads()
    {
        Material roadMaterial = GetOpaqueMaterial(RoadColor);
        float roadX = HumanCastleGateWorldX + 8f;
        PlaceCastleGateRoad("HumanCastleGateRoad", roadX, -1.2f, roadMaterial, -4f);
        PlaceCastleGateRoad("HumanCastleGateRoad2", roadX, 1.2f, roadMaterial, 4f);

        roadX = BeastCastleGateWorldX - 8f;
        PlaceCastleGateRoad("BeastCastleGateRoad", roadX, -1.2f, roadMaterial, 4f);
        PlaceCastleGateRoad("BeastCastleGateRoad2", roadX, 1.2f, roadMaterial, -4f);
    }

    private void PlaceCastleGateRoad(string name, float worldX, float worldZ, Material material, float yawDegrees)
    {
        float groundY = SampleBattlefieldGroundHeightWorld(worldX, worldZ);
        CreateBattlefieldPlane(name, new Vector3(worldX, groundY + 0.038f, worldZ), new Vector2(6f, 2.8f), material, yawDegrees);
    }

    private GameObject CreateFactionCastle(string name, float centerWorldX, float centerZ, bool beastFaction)
    {
        float centerLogicalX = WorldToLogicalX(centerWorldX);
        if (HasMedievalCastleAssets())
        {
            return BuildMedievalGateFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        return BuildPrimitiveCastleFallback(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
    }

    private bool HasMedievalCastleAssets()
    {
        return LoadMedievalVillagePrefab("Wall_Plaster_Straight") != null
            && LoadMedievalVillagePrefab("Roof_RoundTiles_6x6") != null
            && LoadMedievalVillagePrefab("Wall_UnevenBrick_Door_Flat") != null;
    }

    private GameObject BuildMedievalGateFortress(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);
        root.transform.localScale = Vector3.one * CastleVisualScale;

        bool brick = beastFaction;
        string wall = brick ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string door = brick ? "Wall_UnevenBrick_Door_Flat" : "Wall_Plaster_Door_Flat";
        string window = brick ? "Wall_UnevenBrick_Window_Wide_Flat" : "Wall_Plaster_Window_Wide_Flat";
        string corner = brick ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";
        string roofMain = "Roof_RoundTiles_6x10";
        string roofFront = brick ? "Roof_Front_Brick8" : "Roof_Front_Brick6";
        string overhang = brick ? "Overhang_UnevenBrick_Long" : "Overhang_Plaster_Long";

        const int widthModules = 5;
        const int depthModules = 4;
        float localWidth = widthModules * CastleModuleSize;
        float localDepth = depthModules * CastleModuleSize;
        float halfW = localWidth * 0.5f;
        float halfD = localDepth * 0.5f;
        float module = CastleModuleSize;
        float gateX = halfW;

        for (int i = 0; i < depthModules; i++)
        {
            float z = -halfD + module * 0.5f + i * module;
            CreateMegaKitModule(wall, root.transform, new Vector3(-halfW, 0f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            string eastAsset = i == depthModules / 2 ? door : window;
            CreateMegaKitModule(eastAsset, root.transform, new Vector3(halfW, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }

        for (int i = 0; i < widthModules; i++)
        {
            float x = -halfW + module * 0.5f + i * module;
            if (Mathf.Abs(x) < module * 0.6f)
            {
                continue;
            }

            CreateMegaKitModule(window, root.transform, new Vector3(x, 0f, -halfD), Quaternion.identity, Vector3.one);
            CreateMegaKitModule(window, root.transform, new Vector3(x, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        }

        CreateMegaKitModule(corner, root.transform, new Vector3(-halfW, 0f, -halfD), Quaternion.identity, Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(halfW, 0f, -halfD), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(halfW, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        CreateMegaKitModule(corner, root.transform, new Vector3(-halfW, 0f, halfD), Quaternion.Euler(0f, 270f, 0f), Vector3.one);

        CreateMegaKitModule(roofMain, root.transform, new Vector3(0f, 3.05f, 0f), Quaternion.identity, Vector3.one);
        CreateMegaKitModule(roofFront, root.transform, new Vector3(0f, 3.05f, gateX + 0.04f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateMegaKitModule(overhang, root.transform, new Vector3(gateX + 0.12f, 2.4f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateMegaKitModule("Wall_Arch", root.transform, new Vector3(gateX + 0.18f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f);
        CreateMegaKitModule("Door_8_Flat", root.transform, new Vector3(gateX + 0.22f, 0.02f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateMegaKitModule("Stairs_Exterior_Straight_Center", root.transform, new Vector3(gateX + 0.95f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.82f);

        BuildMedievalCornerTower(root.transform, new Vector3(-halfW, 0f, -halfD), brick, false);
        BuildMedievalCornerTower(root.transform, new Vector3(-halfW, 0f, halfD), brick, true);
        BuildMedievalCornerTower(root.transform, new Vector3(halfW * 0.72f, 0f, -halfD * 0.82f), brick, false);
        BuildMedievalCornerTower(root.transform, new Vector3(halfW * 0.72f, 0f, halfD * 0.82f), brick, true);

        if (brick)
        {
            CreateMegaKitModule("Prop_Chimney", root.transform, new Vector3(-halfW * 0.35f, 3.5f, halfD * 0.2f), Quaternion.Euler(0f, -18f, 0f), Vector3.one);
            CreateMegaKitModule("Prop_Brick3", root.transform, new Vector3(gateX + 1.35f, 0f, -1.1f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 0.9f);
        }
        else
        {
            CreateMegaKitModule("Prop_Wagon", root.transform, new Vector3(gateX + 1.2f, 0f, 1.4f), Quaternion.Euler(0f, -70f, 0f), Vector3.one * 0.5f);
            CreateMegaKitModule("Prop_Crate", root.transform, new Vector3(gateX + 1.05f, 0f, -1.3f), Quaternion.identity, Vector3.one * 0.48f);
        }

        CreateMegaKitModule("Prop_MetalFence_Ornament", root.transform, new Vector3(0f, 0f, -halfD - 0.55f), Quaternion.identity, Vector3.one * 0.95f);
        CreateMegaKitModule("Prop_MetalFence_Ornament", root.transform, new Vector3(0f, 0f, halfD + 0.55f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 0.95f);

        ApplyMedievalCastleFactionTint(root, beastFaction);
        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 58f * CastleVisualScale, 72f * CastleVisualScale, 9.5f * CastleVisualScale, 16f, 420f);
        return root;
    }

    private void BuildMedievalCornerTower(Transform parent, Vector3 localPosition, bool brick, bool mirrorYaw)
    {
        const float size = 3.2f;
        float half = size * 0.5f;
        float yawOffset = mirrorYaw ? 180f : 0f;
        string front = brick ? "Wall_UnevenBrick_Window_Thin_Round" : "Wall_Plaster_Window_Thin_Round";
        string side = brick ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string cornerAsset = brick ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";

        for (int level = 0; level < 2; level++)
        {
            float y = level * 2.85f;
            Quaternion baseYaw = Quaternion.Euler(0f, yawOffset, 0f);
            CreateMegaKitModule(level == 0 ? (brick ? "Wall_UnevenBrick_Door_Round" : "Wall_Plaster_Door_Round") : front, parent,
                localPosition + new Vector3(0f, y, -half), baseYaw, Vector3.one * 0.92f);
            CreateMegaKitModule(side, parent, localPosition + new Vector3(-half, y, 0f), baseYaw * Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.92f);
            CreateMegaKitModule(side, parent, localPosition + new Vector3(half, y, 0f), baseYaw * Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.92f);
            CreateMegaKitModule(cornerAsset, parent, localPosition + new Vector3(-half, y, -half), baseYaw, Vector3.one * 0.92f);
            CreateMegaKitModule(cornerAsset, parent, localPosition + new Vector3(half, y, -half), baseYaw * Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.92f);
        }

        CreateMegaKitModule("Roof_Tower_RoundTiles", parent, localPosition + new Vector3(0f, 5.7f, 0f), Quaternion.Euler(0f, yawOffset, 0f), Vector3.one * 0.95f);
    }

    private void ApplyMedievalCastleFactionTint(GameObject root, bool beastFaction)
    {
        Color accent = beastFaction
            ? new Color(0.82f, 0.42f, 0.30f, 1f)
            : new Color(0.36f, 0.52f, 0.78f, 1f);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool tintable = renderer.name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("Fence", System.StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("Overhang", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!tintable)
            {
                continue;
            }

            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null && materials[m].HasProperty("_Color"))
                {
                    materials[m].color = Color.Lerp(materials[m].color, accent, 0.22f);
                }
            }
        }
    }

    private GameObject BuildPrimitiveCastleFallback(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        Color stone = beastFaction ? new Color(0.64f, 0.50f, 0.42f, 1f) : new Color(0.68f, 0.66f, 0.58f, 1f);
        Color trim = beastFaction ? new Color(0.78f, 0.32f, 0.22f, 1f) : new Color(0.28f, 0.46f, 0.72f, 1f);
        float gateSign = beastFaction ? -1f : 1f;

        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);
        root.transform.localScale = Vector3.one * CastleVisualScale;

        var podium = CreatePrimitive(PrimitiveType.Cylinder, $"{name}_Podium", root.transform);
        podium.transform.localScale = new Vector3(12f, 1f, 12f);
        podium.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        podium.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(stone);

        var keep = CreatePrimitive(PrimitiveType.Cube, $"{name}_Keep", root.transform);
        keep.transform.localScale = new Vector3(9f, 7f, 9f);
        keep.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        keep.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(stone);

        var gate = CreatePrimitive(PrimitiveType.Cube, $"{name}_Gate", root.transform);
        gate.transform.localScale = new Vector3(0.7f, 4.2f, 4.2f);
        gate.transform.localPosition = new Vector3(gateSign * 5.4f, 2.2f, 0f);
        gate.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(trim);

        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 58f * CastleVisualScale, 72f * CastleVisualScale, 9f * CastleVisualScale, 16f, 420f);
        return root;
    }

    private const float FormationBlockGapX = 56f;
    private const float HumanSoldierFormationOffsetX = 10f;
    private const float BeastSoldierFormationOffsetX = 10f;
    private const float SoldierFormationRankSpacingX = 44f;
    private const float SoldierFormationLaneSpacingZ = 44f;
    private const float TankFormationRankSpacingX = 80f;
    private const float TankFormationLaneSpacingZ = 82f;
    private const float GiantFormationRankSpacingX = 176f;
    private const float GiantFormationLaneSpacingZ = 176f;
    private const int BeastFormationLanesPerRow = 4;

    private static int FormationRowCount(int unitCount, int lanesPerRow)
    {
        if (unitCount <= 0)
        {
            return 0;
        }

        return (unitCount - 1) / Mathf.Max(1, lanesPerRow) + 1;
    }

    private static float FormationLaneZ(int columnIndex, int columnsPerRow, float laneSpacing)
    {
        int cols = Mathf.Max(1, columnsPerRow);
        int col = Mathf.Clamp(columnIndex, 0, cols - 1);
        float span = (cols - 1) * laneSpacing;
        return -span * 0.5f + col * laneSpacing;
    }

    private static float HumanSoldierBlockFrontX()
    {
        int rows = FormationRowCount(SoldierCount, HumanFormationLanesPerRow);
        return HumanCastleGateX + HumanSoldierFormationOffsetX + Mathf.Max(0, rows - 1) * SoldierFormationRankSpacingX;
    }

    private static float HumanTankFormationBaseX()
    {
        return HumanSoldierBlockFrontX() + FormationBlockGapX;
    }

    private static float HumanTankBlockFrontX()
    {
        int rows = FormationRowCount(TankCount, HumanFormationTanksPerRow);
        return HumanTankFormationBaseX() + Mathf.Max(0, rows - 1) * TankFormationRankSpacingX;
    }

    private static float HumanAircraftFormationX()
    {
        return HumanTankBlockFrontX() + FormationBlockGapX;
    }

    private static float BeastSoldierBlockFrontX()
    {
        int rows = FormationRowCount(SoldierCount, BeastFormationLanesPerRow);
        return BeastCastleGateX - BeastSoldierFormationOffsetX - Mathf.Max(0, rows - 1) * SoldierFormationRankSpacingX;
    }

    private static float BeastGiantFormationBaseX()
    {
        return BeastSoldierBlockFrontX() - FormationBlockGapX;
    }

    private static float BeastTankFormationBaseX()
    {
        int giantRows = FormationRowCount(GiantCount, BeastFormationLanesPerRow);
        float giantFront = BeastGiantFormationBaseX() - Mathf.Max(0, giantRows - 1) * GiantFormationRankSpacingX;
        return giantFront - FormationBlockGapX;
    }

    private static float BeastTankBlockFrontX()
    {
        int rows = FormationRowCount(TankCount, HumanFormationTanksPerRow);
        return BeastTankFormationBaseX() - Mathf.Max(0, rows - 1) * TankFormationRankSpacingX;
    }

    private static float BeastAircraftFormationX()
    {
        return BeastTankBlockFrontX() - FormationBlockGapX;
    }

    private void GetHumanFormationSpawn(UnitKind kind, int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int lanesPerRow = kind == UnitKind.Tank ? HumanFormationTanksPerRow : HumanFormationLanesPerRow;
        float laneSpacing = kind == UnitKind.Tank ? TankFormationLaneSpacingZ : SoldierFormationLaneSpacingZ;
        int col = unitIndex % lanesPerRow;
        int rank = unitIndex / lanesPerRow;
        z = FormationLaneZ(col, lanesPerRow, laneSpacing);
        if (kind == UnitKind.Tank)
        {
            x = HumanTankFormationBaseX() + rank * TankFormationRankSpacingX;
        }
        else
        {
            x = HumanCastleGateX + HumanSoldierFormationOffsetX + rank * SoldierFormationRankSpacingX;
        }
    }

    private void GetHumanAircraftFormationSpawn(int laneIndex, out float x, out float z)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, AirLanes.Length - 1);
        x = HumanAircraftFormationX();
        z = AirLanes[laneIndex];
    }

    private void GetBeastFormationSpawn(UnitKind kind, int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int lanesPerRow = kind == UnitKind.Tank ? HumanFormationTanksPerRow : BeastFormationLanesPerRow;
        float laneSpacing = kind == UnitKind.Tank ? TankFormationLaneSpacingZ
            : kind == UnitKind.Giant ? GiantFormationLaneSpacingZ
            : SoldierFormationLaneSpacingZ;
        float rankSpacing = kind == UnitKind.Tank ? TankFormationRankSpacingX
            : kind == UnitKind.Giant ? GiantFormationRankSpacingX
            : SoldierFormationRankSpacingX;
        int col = unitIndex % lanesPerRow;
        int rank = unitIndex / lanesPerRow;
        z = FormationLaneZ(col, lanesPerRow, laneSpacing);
        if (kind == UnitKind.Tank)
        {
            x = BeastTankFormationBaseX() - rank * rankSpacing;
        }
        else if (kind == UnitKind.Giant)
        {
            x = BeastGiantFormationBaseX() - rank * rankSpacing;
        }
        else
        {
            x = BeastCastleGateX - BeastSoldierFormationOffsetX - rank * rankSpacing;
        }
    }

    private void GetBeastAircraftFormationSpawn(int laneIndex, out float x, out float z)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, AirLanes.Length - 1);
        x = BeastAircraftFormationX();
        z = AirLanes[laneIndex];
    }

    private void GetFactionCastleSpawn(FactionId faction, UnitKind kind, int slotIndex, out float x, out float z, out int facing)
    {
        bool fromBeastCastle = faction == FactionId.Green || faction == FactionId.Zombie;
        if (fromBeastCastle)
        {
            if (kind == UnitKind.Aircraft)
            {
                GetBeastAircraftFormationSpawn(slotIndex, out x, out z);
            }
            else
            {
                GetBeastFormationSpawn(kind, slotIndex, out x, out z);
            }

            facing = -1;
            return;
        }

        if (kind == UnitKind.Aircraft)
        {
            GetHumanAircraftFormationSpawn(slotIndex, out x, out z);
        }
        else
        {
            GetHumanFormationSpawn(kind, slotIndex, out x, out z);
        }

        facing = 1;
    }
}
