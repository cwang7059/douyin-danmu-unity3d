using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const float CastleVisualScale = 2.05f;
    private const float CastleKenneyModuleSpacing = 3.35f;
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
        CacheCastleKitPrefabs();
        humanCastleRoot = CreateFactionCastle("HumanCastle", HumanCastleWorldX, HumanCastleCenterZ, false).transform;
        beastCastleRoot = CreateFactionCastle("BeastCastle", BeastCastleWorldX, BeastCastleCenterZ, true).transform;

        if (!HasKenneyCastleAssets() && !loggedCastleFallback)
        {
            loggedCastleFallback = true;
            Debug.LogWarning("[ApocalypseKing] Kenney Castle Kit missing; run .\\tools\\import-castle-environment.ps1 then bake prefabs.");
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
        if (HasKenneyCastleAssets())
        {
            return BuildKenneyCastleFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

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

    private GameObject BuildKenneyCastleFortress(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);
        root.transform.localScale = Vector3.one * CastleVisualScale;

        const int segmentsX = 8;
        const int segmentsZ = 7;
        float spacing = CastleKenneyModuleSpacing;
        float halfW = segmentsX * spacing * 0.5f;
        float halfD = segmentsZ * spacing * 0.5f;
        float gateX = halfW;

        for (int i = 0; i < segmentsX; i++)
        {
            float x = -halfW + spacing * 0.5f + i * spacing;
            CreateCastleKitModule("wall", root.transform, new Vector3(x, 0f, -halfD), Quaternion.identity, Vector3.one);
            CreateCastleKitModule("wall", root.transform, new Vector3(x, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        }

        for (int i = 0; i < segmentsZ; i++)
        {
            float z = -halfD + spacing * 0.5f + i * spacing;
            CreateCastleKitModule("wall", root.transform, new Vector3(-halfW, 0f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
            if (Mathf.Abs(z) > spacing * 0.55f)
            {
                CreateCastleKitModule("wall", root.transform, new Vector3(gateX, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
            }
        }

        CreateCastleKitModule("wall-corner", root.transform, new Vector3(-halfW, 0f, -halfD), Quaternion.identity, Vector3.one);
        CreateCastleKitModule("wall-corner", root.transform, new Vector3(-halfW, 0f, halfD), Quaternion.Euler(0f, -90f, 0f), Vector3.one);
        CreateCastleKitModule("wall-corner", root.transform, new Vector3(gateX, 0f, -halfD), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        CreateCastleKitModule("wall-corner", root.transform, new Vector3(gateX, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one);

        BuildKenneyGatehouse(root.transform, new Vector3(gateX + 0.15f, 0f, 0f));
        BuildKenneyCentralKeep(root.transform, Vector3.zero);
        BuildKenneyCornerTower(root.transform, new Vector3(-halfW, 0f, -halfD));
        BuildKenneyCornerTower(root.transform, new Vector3(-halfW, 0f, halfD));
        BuildKenneyCornerTower(root.transform, new Vector3(gateX * 0.82f, 0f, -halfD * 0.9f));
        BuildKenneyCornerTower(root.transform, new Vector3(gateX * 0.82f, 0f, halfD * 0.9f));

        CreateCastleKitModule("stairs-stone-square", root.transform, new Vector3(gateX - 2.2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.9f);
        CreateCastleKitModule("wall-pillar", root.transform, new Vector3(0f, 0f, 0f), Quaternion.identity, Vector3.one * 0.85f);
        CreateCastleKitModule("rocks-small", root.transform, new Vector3(-2.5f, 0f, 2.2f), Quaternion.Euler(0f, 24f, 0f), Vector3.one * 1.4f);
        CreateCastleKitModule("rocks-small", root.transform, new Vector3(1.8f, 0f, -2.4f), Quaternion.Euler(0f, -38f, 0f), Vector3.one * 1.2f);
        CreateCastleKitModule("tree-trunk", root.transform, new Vector3(-3.8f, 0f, -1.2f), Quaternion.identity, Vector3.one * 0.75f);

        float backdropZ = -halfD - 5.5f;
        CreateCastleKitModule("ground-hills", root.transform, new Vector3(0f, -0.2f, backdropZ), Quaternion.identity, Vector3.one * 2.8f);
        CreateCastleKitModule("rocks-large", root.transform, new Vector3(-5f, 0f, backdropZ - 1.5f), Quaternion.Euler(0f, 15f, 0f), Vector3.one * 1.6f);
        CreateCastleKitModule("rocks-large", root.transform, new Vector3(5.5f, 0f, backdropZ - 2f), Quaternion.Euler(0f, -22f, 0f), Vector3.one * 1.4f);

        string banner = beastFaction ? "flag-banner-short" : "flag-banner-long";
        CreateCastleKitModule(banner, root.transform, new Vector3(0f, 9.8f, 0.5f), Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f), Vector3.one * 0.95f);

        ApplyKenneyCastleFactionTint(root, beastFaction);
        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 78f * CastleVisualScale, 92f * CastleVisualScale, 12f * CastleVisualScale, 18f, 520f);
        return root;
    }

    private void BuildKenneyGatehouse(Transform parent, Vector3 localPosition)
    {
        float towerGap = 4.2f;
        BuildKenneyGateTower(parent, localPosition + new Vector3(0f, 0f, -towerGap * 0.5f));
        BuildKenneyGateTower(parent, localPosition + new Vector3(0f, 0f, towerGap * 0.5f));
        CreateCastleKitModule("metal-gate", parent, localPosition + new Vector3(0.35f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.05f);
        CreateCastleKitModule("bridge-straight-pillar", parent, localPosition + new Vector3(0.9f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.75f);
    }

    private void BuildKenneyGateTower(Transform parent, Vector3 localPosition)
    {
        CreateCastleKitModule("tower-square-base-border", parent, localPosition, Quaternion.identity, Vector3.one);
        CreateCastleKitModule("tower-square-mid-door", parent, localPosition + new Vector3(0f, 2.75f, 0f), Quaternion.identity, Vector3.one);
        CreateCastleKitModule("tower-square-top-roof-high", parent, localPosition + new Vector3(0f, 5.5f, 0f), Quaternion.identity, Vector3.one);
    }

    private void BuildKenneyCentralKeep(Transform parent, Vector3 localPosition)
    {
        CreateCastleKitModule("tower-square-base-border", parent, localPosition, Quaternion.identity, Vector3.one * 1.08f);
        CreateCastleKitModule("tower-square-mid-windows", parent, localPosition + new Vector3(0f, 2.85f, 0f), Quaternion.identity, Vector3.one * 1.08f);
        CreateCastleKitModule("tower-square-mid-door", parent, localPosition + new Vector3(0f, 5.7f, 0f), Quaternion.identity, Vector3.one * 1.08f);
        CreateCastleKitModule("tower-square-top-roof-high-windows", parent, localPosition + new Vector3(0f, 8.55f, 0f), Quaternion.identity, Vector3.one * 1.12f);
        CreateCastleKitModule("tower-square-roof", parent, localPosition + new Vector3(0f, 10.2f, 0f), Quaternion.identity, Vector3.one * 1.05f);
    }

    private void BuildKenneyCornerTower(Transform parent, Vector3 localPosition)
    {
        CreateCastleKitModule("tower-square-base", parent, localPosition, Quaternion.identity, Vector3.one * 0.92f);
        CreateCastleKitModule("tower-square-mid", parent, localPosition + new Vector3(0f, 2.5f, 0f), Quaternion.identity, Vector3.one * 0.92f);
        CreateCastleKitModule("tower-square-top-roof", parent, localPosition + new Vector3(0f, 5f, 0f), Quaternion.identity, Vector3.one * 0.92f);
    }

    private void ApplyKenneyCastleFactionTint(GameObject root, bool beastFaction)
    {
        Color accent = beastFaction
            ? new Color(0.86f, 0.38f, 0.28f, 1f)
            : new Color(0.34f, 0.56f, 0.82f, 1f);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool tintable = renderer.name.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("banner", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("door", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!tintable)
            {
                continue;
            }

            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null && materials[m].HasProperty("_Color"))
                {
                    materials[m].color = Color.Lerp(materials[m].color, accent, 0.35f);
                }
            }
        }
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
    private const float GiantFormationRankSpacingX = 48f;
    private const float GiantFormationLaneSpacingZ = 34f;
    private const int BeastGiantLanesPerRow = 3;
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

    private const int HumanFormationLayoutCap = 16;
    private const int HumanSoldierMassSpawnColumns = 25;
    private const int HumanTankMassSpawnColumns = 20;
    private const float HumanSoldierMassSpawnSpacingX = 20f;
    private const float HumanSoldierMassSpawnSpacingZ = 22f;
    private const float HumanTankMassSpawnSpacingX = 24f;
    private const float HumanTankMassSpawnSpacingZ = 26f;
    private const int HumanAircraftMassSpawnColumns = 10;
    private const float HumanAircraftMassSpawnSpacingX = 26f;
    private const float HumanAircraftMassSpawnSpacingZ = 28f;

    private static float HumanSoldierBlockFrontX()
    {
        int rows = FormationRowCount(HumanFormationLayoutCap, HumanFormationLanesPerRow);
        return HumanCastleGateX + HumanSoldierFormationOffsetX + Mathf.Max(0, rows - 1) * SoldierFormationRankSpacingX;
    }

    private static float HumanTankFormationBaseX()
    {
        return HumanSoldierBlockFrontX() + FormationBlockGapX;
    }

    private static float HumanTankBlockFrontX()
    {
        int rows = FormationRowCount(HumanFormationLayoutCap, HumanFormationTanksPerRow);
        return HumanTankFormationBaseX() + Mathf.Max(0, rows - 1) * TankFormationRankSpacingX;
    }

    private static float HumanAircraftFormationX()
    {
        return HumanTankBlockFrontX() + FormationBlockGapX;
    }

    private static float BeastSoldierBlockFrontX()
    {
        int rows = FormationRowCount(HumanFormationLayoutCap, BeastFormationLanesPerRow);
        return BeastCastleGateX - BeastSoldierFormationOffsetX - Mathf.Max(0, rows - 1) * SoldierFormationRankSpacingX;
    }

    private static float BeastGiantFormationBaseX()
    {
        return BeastSoldierBlockFrontX() - FormationBlockGapX;
    }

    private const int GiantFormationLayoutCap = 12;
    private const int GiantMassSpawnColumns = 100;
    private const float GiantMassSpawnSpacingX = 5.5f;
    private const float GiantMassSpawnSpacingZ = 6f;

    private static float BeastTankFormationBaseX()
    {
        int giantRows = FormationRowCount(GiantFormationLayoutCap, BeastGiantLanesPerRow);
        float giantFront = BeastGiantFormationBaseX() - Mathf.Max(0, giantRows - 1) * GiantFormationRankSpacingX;
        return giantFront - FormationBlockGapX;
    }

    private void GetGiantMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % GiantMassSpawnColumns;
        int row = unitIndex / GiantMassSpawnColumns;
        float anchorX = BeastGiantFormationBaseX() - 8f;
        z = (col - (GiantMassSpawnColumns - 1) * 0.5f) * GiantMassSpawnSpacingZ;
        x = anchorX - row * GiantMassSpawnSpacingX;
    }

    private static float BeastTankBlockFrontX()
    {
        int rows = FormationRowCount(HumanFormationLayoutCap, HumanFormationTanksPerRow);
        return BeastTankFormationBaseX() - Mathf.Max(0, rows - 1) * TankFormationRankSpacingX;
    }

    private static float BeastAircraftFormationX()
    {
        return BeastTankBlockFrontX() - FormationBlockGapX;
    }

    private void GetHumanSoldierMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % HumanSoldierMassSpawnColumns;
        int row = unitIndex / HumanSoldierMassSpawnColumns;
        float anchorX = HumanCastleGateX + HumanSoldierFormationOffsetX + 6f;
        z = (col - (HumanSoldierMassSpawnColumns - 1) * 0.5f) * HumanSoldierMassSpawnSpacingZ;
        x = anchorX + row * HumanSoldierMassSpawnSpacingX;
    }

    private void GetHumanTankMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % HumanTankMassSpawnColumns;
        int row = unitIndex / HumanTankMassSpawnColumns;
        float anchorX = HumanTankFormationBaseX() + 10f;
        z = (col - (HumanTankMassSpawnColumns - 1) * 0.5f) * HumanTankMassSpawnSpacingZ;
        x = anchorX + row * HumanTankMassSpawnSpacingX;
    }

    private void GetHumanFormationSpawn(UnitKind kind, int unitIndex, out float x, out float z)
    {
        if (kind == UnitKind.Tank)
        {
            GetHumanTankMassSpawn(unitIndex, out x, out z);
            return;
        }

        if (kind == UnitKind.Soldier)
        {
            GetHumanSoldierMassSpawn(unitIndex, out x, out z);
            return;
        }

        unitIndex = Mathf.Max(0, unitIndex);
        int lanesPerRow = HumanFormationLanesPerRow;
        float laneSpacing = SoldierFormationLaneSpacingZ;
        int col = unitIndex % lanesPerRow;
        int rank = unitIndex / lanesPerRow;
        z = FormationLaneZ(col, lanesPerRow, laneSpacing);
        x = HumanCastleGateX + HumanSoldierFormationOffsetX + rank * SoldierFormationRankSpacingX;
    }

    private void GetHumanAircraftMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % HumanAircraftMassSpawnColumns;
        int row = unitIndex / HumanAircraftMassSpawnColumns;
        float anchorX = HumanAircraftFormationX() + 12f;
        z = (col - (HumanAircraftMassSpawnColumns - 1) * 0.5f) * HumanAircraftMassSpawnSpacingZ;
        x = anchorX + row * HumanAircraftMassSpawnSpacingX;
    }

    private void GetHumanAircraftFormationSpawn(int unitIndex, out float x, out float z)
    {
        GetHumanAircraftMassSpawn(unitIndex, out x, out z);
    }

    private void GetBeastFormationSpawn(UnitKind kind, int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int lanesPerRow = kind == UnitKind.Tank
            ? HumanFormationTanksPerRow
            : kind == UnitKind.Giant ? BeastGiantLanesPerRow : BeastFormationLanesPerRow;
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
            GetGiantMassSpawn(unitIndex, out x, out z);
            return;
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
