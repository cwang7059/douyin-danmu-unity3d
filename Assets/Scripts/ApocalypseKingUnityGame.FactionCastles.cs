using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    // Layout: length = north-south (Z), width = east-west depth (X, gate on +X). Volume tuned via CastleVisualScale.
    private const float CastleVisualScale = 1.7f;
    private const int CastleLengthModules = 8;
    private const int CastleWidthModules = 3;
    private const float CastleKenneyTileSize = 2f;
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

        if (!HasRealisticCastleFortress() && !HasMedievalCastleAssets() && !HasKenneyCastleAssets() && !loggedCastleFallback)
        {
            loggedCastleFallback = true;
            Debug.LogWarning("[ApocalypseKing] Castle assets missing; run .\\tools\\import-realistic-castle.ps1");
        }

        CreateCastleFlankPads();
        CreateCastleGateRoads();
    }

    private void CreateCastleFlankPads()
    {
        Material grassMaterial = GetTexturedOpaqueMaterial(GrassTextureResourcePath, new Color(0.66f, 0.78f, 0.50f, 1f), new Vector2(8f, 10f), 0.08f);
        float padW = 11f * CastleVisualScale;
        float padH = 22f * CastleVisualScale;
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
        if (HasRealisticCastleFortress() && IsRealisticCastlePrefabViable())
        {
            return BuildRealisticCastleFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        if (HasMedievalCastleAssets())
        {
            return BuildMedievalGateFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        if (HasKenneyCastleAssets())
        {
            return BuildKenneyCastleFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        return BuildPrimitiveCastleFallback(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
    }

    private GameObject PlaceMedievalCastleModule(
        string assetName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        float floorLocalY = 0f)
    {
        GameObject instance = CreateMegaKitModule(assetName, parent, localPosition, localRotation, localScale);
        if (instance != null)
        {
            AlignCastleKitModuleToFloor(instance, floorLocalY);
        }

        return instance;
    }

    private float StackMedievalCastleModule(
        string assetName,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        float floorLocalY)
    {
        GameObject instance = PlaceMedievalCastleModule(assetName, parent, localPosition, localRotation, localScale, floorLocalY);
        if (instance == null)
        {
            return floorLocalY;
        }

        return GetCastleModuleStackTopY(instance, floorLocalY);
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

        float tile = CastleKenneyTileSize;
        float halfW = CastleWidthModules * tile * 0.5f;
        float halfD = CastleLengthModules * tile * 0.5f;
        float gateX = halfW;

        PlaceKenneyWallRun(root.transform, -halfW, halfW, -halfD, true, tile);
        PlaceKenneyWallRun(root.transform, -halfW, halfW, halfD, true, tile);
        PlaceKenneyWallRun(root.transform, -halfD, halfD, -halfW, false, tile);
        PlaceKenneyEastGateWalls(root.transform, halfW, halfD, tile);

        BuildKenneyCornerTower(root.transform, new Vector3(-halfW, 0f, -halfD));
        BuildKenneyCornerTower(root.transform, new Vector3(-halfW, 0f, halfD));
        BuildKenneyCornerTower(root.transform, new Vector3(gateX, 0f, -halfD));
        BuildKenneyCornerTower(root.transform, new Vector3(gateX, 0f, halfD));

        BuildKenneyGatehouse(root.transform, new Vector3(gateX - tile * 0.15f, 0f, 0f));

        PlaceCastleKitModuleOnFloor("stairs-stone-square", root.transform,
            new Vector3(gateX - tile * 1.1f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.9f);
        PlaceCastleKitModuleOnFloor("rocks-small", root.transform,
            new Vector3(-halfW + tile * 0.6f, 0f, halfD - tile * 0.55f), Quaternion.Euler(0f, 24f, 0f), Vector3.one);
        PlaceCastleKitModuleOnFloor("rocks-small", root.transform,
            new Vector3(-halfW + tile * 0.55f, 0f, -halfD + tile * 0.5f), Quaternion.Euler(0f, -38f, 0f), Vector3.one * 0.9f);

        float backdropZ = -halfD - tile * 2.2f;
        PlaceCastleKitModuleOnFloor("ground-hills", root.transform,
            new Vector3(-tile * 0.2f, 0f, backdropZ), Quaternion.identity, Vector3.one * 2.2f);
        PlaceCastleKitModuleOnFloor("rocks-large", root.transform,
            new Vector3(-halfW - tile * 0.35f, 0f, backdropZ - tile * 0.35f), Quaternion.Euler(0f, 15f, 0f), Vector3.one * 1.2f);

        float bannerTop = BuildKenneyCentralKeepTopY(root.transform, new Vector3(-tile * 0.35f, 0f, 0f));
        string banner = beastFaction ? "flag-banner-short" : "flag-banner-long";
        PlaceCastleKitModuleOnFloor(banner, root.transform,
            new Vector3(-tile * 0.35f, 0f, tile * 0.15f), Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f), Vector3.one * 0.95f, bannerTop);

        ApplyKenneyCastleFactionTint(root, beastFaction);
        float obstacleHalfWidth = 31f * CastleVisualScale;
        float obstacleHalfLength = 68f * CastleVisualScale;
        AddBuildingObstacle(root, name, centerLogicalX, centerZ, obstacleHalfWidth, obstacleHalfLength, 11f * CastleVisualScale, 18f, 520f);
        return root;
    }

    private void PlaceKenneyWallRun(Transform parent, float start, float end, float axis, bool alongX, float tile)
    {
        int segments = Mathf.Max(1, Mathf.RoundToInt((end - start) / tile));
        for (int i = 0; i < segments; i++)
        {
            float t = start + tile * 0.5f + i * tile;
            if (alongX)
            {
                PlaceCastleKitModuleOnFloor("wall", parent, new Vector3(t, 0f, axis), axis >= 0f ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity, Vector3.one);
            }
            else
            {
                PlaceCastleKitModuleOnFloor("wall", parent, new Vector3(axis, 0f, t), Quaternion.Euler(0f, axis >= 0f ? 90f : -90f, 0f), Vector3.one);
            }
        }
    }

    private void PlaceKenneyEastGateWalls(Transform parent, float gateX, float halfD, float tile)
    {
        for (float z = -halfD + tile * 0.5f; z <= halfD - tile * 0.25f; z += tile)
        {
            if (Mathf.Abs(z) <= tile * 0.65f)
            {
                continue;
            }

            PlaceCastleKitModuleOnFloor("wall", parent, new Vector3(gateX, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        }
    }

    private void BuildKenneyGatehouse(Transform parent, Vector3 localPosition)
    {
        float towerGap = CastleKenneyTileSize * 2.1f;
        BuildKenneyGateTower(parent, localPosition + new Vector3(0f, 0f, -towerGap * 0.5f));
        BuildKenneyGateTower(parent, localPosition + new Vector3(0f, 0f, towerGap * 0.5f));
        PlaceCastleKitModuleOnFloor("metal-gate", parent,
            localPosition + new Vector3(0.2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
    }

    private void BuildKenneyGateTower(Transform parent, Vector3 localPosition)
    {
        float floorY = 0f;
        floorY = StackCastleKitModule("tower-square-base-border", parent, localPosition, Quaternion.identity, Vector3.one, floorY);
        floorY = StackCastleKitModule("tower-square-mid-door", parent, localPosition, Quaternion.identity, Vector3.one, floorY);
        StackCastleKitModule("tower-square-top-roof-high", parent, localPosition, Quaternion.identity, Vector3.one, floorY);
    }

    private float BuildKenneyCentralKeepTopY(Transform parent, Vector3 localPosition)
    {
        Vector3 scale = Vector3.one * 1.05f;
        float floorY = 0f;
        floorY = StackCastleKitModule("tower-square-base-border", parent, localPosition, Quaternion.identity, scale, floorY);
        floorY = StackCastleKitModule("tower-square-mid-windows", parent, localPosition, Quaternion.identity, scale, floorY);
        floorY = StackCastleKitModule("tower-square-mid-door", parent, localPosition, Quaternion.identity, scale, floorY);
        floorY = StackCastleKitModule("tower-square-top-roof-high-windows", parent, localPosition, Quaternion.identity, scale, floorY);
        return StackCastleKitModule("tower-square-roof", parent, localPosition, Quaternion.identity, scale, floorY);
    }

    private void BuildKenneyCornerTower(Transform parent, Vector3 localPosition)
    {
        Vector3 scale = Vector3.one * 0.95f;
        float floorY = 0f;
        floorY = StackCastleKitModule("tower-square-base", parent, localPosition, Quaternion.identity, scale, floorY);
        floorY = StackCastleKitModule("tower-square-mid", parent, localPosition, Quaternion.identity, scale, floorY);
        StackCastleKitModule("tower-square-top-roof", parent, localPosition, Quaternion.identity, scale, floorY);
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

        int widthModules = CastleWidthModules;
        int depthModules = CastleLengthModules;
        const float wallLevelHeight = 3f;
        float module = CastleModuleSize;
        float halfW = widthModules * module * 0.5f;
        float halfD = depthModules * module * 0.5f;
        float gateX = halfW;
        string roofCourtyard = depthModules >= 8 ? "Roof_RoundTiles_8x12" : "Roof_RoundTiles_6x10";

        BuildMedievalCastleWallRing(root.transform, halfW, halfD, gateX, module, depthModules, widthModules, wall, door, window, 0f);
        BuildMedievalCastleWallRing(root.transform, halfW, halfD, gateX, module, depthModules, widthModules, wall, door, window, wallLevelHeight);

        PlaceMedievalCastleModule(corner, root.transform, new Vector3(-halfW, 0f, -halfD), Quaternion.identity, Vector3.one);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(gateX, 0f, -halfD), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(gateX, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(-halfW, 0f, halfD), Quaternion.Euler(0f, 270f, 0f), Vector3.one);

        PlaceMedievalCastleModule(corner, root.transform, new Vector3(-halfW, 0f, -halfD), Quaternion.identity, Vector3.one, wallLevelHeight);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(gateX, 0f, -halfD), Quaternion.Euler(0f, 90f, 0f), Vector3.one, wallLevelHeight);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(gateX, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one, wallLevelHeight);
        PlaceMedievalCastleModule(corner, root.transform, new Vector3(-halfW, 0f, halfD), Quaternion.Euler(0f, 270f, 0f), Vector3.one, wallLevelHeight);

        if (LoadMedievalVillagePrefab(roofCourtyard) != null)
        {
            PlaceMedievalCastleModule(roofCourtyard, root.transform, new Vector3(0f, 0f, 0f), Quaternion.identity, Vector3.one, wallLevelHeight);
        }

        BuildMedievalCornerTowerRoom(root.transform, new Vector3(-halfW, 0f, -halfD), brick, 0f);
        BuildMedievalCornerTowerRoom(root.transform, new Vector3(gateX, 0f, -halfD), brick, 90f);
        BuildMedievalCornerTowerRoom(root.transform, new Vector3(gateX, 0f, halfD), brick, 180f);
        BuildMedievalCornerTowerRoom(root.transform, new Vector3(-halfW, 0f, halfD), brick, 270f);

        PlaceMedievalCastleModule("Wall_Arch", root.transform, new Vector3(gateX + 0.12f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        PlaceMedievalCastleModule("Door_8_Flat", root.transform, new Vector3(gateX + 0.16f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);
        PlaceMedievalCastleModule("Stairs_Exterior_Straight_Center", root.transform, new Vector3(gateX + 0.82f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.82f);

        if (brick)
        {
            PlaceMedievalCastleModule("Prop_Brick3", root.transform, new Vector3(gateX + 1.1f, 0f, -1f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 0.85f);
        }
        else
        {
            PlaceMedievalCastleModule("Prop_Wagon", root.transform, new Vector3(gateX + 1f, 0f, 1.2f), Quaternion.Euler(0f, -70f, 0f), Vector3.one * 0.48f);
        }

        ApplyRealisticCastleStoneMaterials(root);
        ApplyMedievalCastleFactionTint(root, beastFaction);
        float obstacleHalfWidth = 31f * CastleVisualScale;
        float obstacleHalfLength = 68f * CastleVisualScale;
        AddBuildingObstacle(root, name, centerLogicalX, centerZ, obstacleHalfWidth, obstacleHalfLength, 11f * CastleVisualScale, 16f, 420f);
        return root;
    }

    private void BuildMedievalCastleWallRing(
        Transform parent,
        float halfW,
        float halfD,
        float gateX,
        float module,
        int depthModules,
        int widthModules,
        string wall,
        string door,
        string window,
        float floorY)
    {
        bool upper = floorY > 0.01f;
        string northSouth = upper ? window : wall;
        string eastWest = upper ? window : wall;

        for (int i = 0; i < depthModules; i++)
        {
            float z = -halfD + module * 0.5f + i * module;
            PlaceMedievalCastleModule(eastWest, parent, new Vector3(-halfW, 0f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one, floorY);
            string eastAsset = !upper && i == depthModules / 2 ? door : northSouth;
            PlaceMedievalCastleModule(eastAsset, parent, new Vector3(gateX, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one, floorY);
        }

        for (int i = 0; i < widthModules; i++)
        {
            float x = -halfW + module * 0.5f + i * module;
            PlaceMedievalCastleModule(northSouth, parent, new Vector3(x, 0f, -halfD), Quaternion.identity, Vector3.one, floorY);
            PlaceMedievalCastleModule(northSouth, parent, new Vector3(x, 0f, halfD), Quaternion.Euler(0f, 180f, 0f), Vector3.one, floorY);
        }
    }

    private void BuildMedievalCornerTowerRoom(Transform parent, Vector3 center, bool brick, float yawDegrees)
    {
        const float localSize = 3.6f;
        float half = localSize * 0.5f;
        Quaternion yaw = Quaternion.Euler(0f, yawDegrees, 0f);
        string front0 = brick ? "Wall_UnevenBrick_Door_Round" : "Wall_Plaster_Door_Round";
        string front1 = brick ? "Wall_UnevenBrick_Window_Thin_Round" : "Wall_Plaster_Window_Thin_Round";
        string side = brick ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string cornerAsset = brick ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";

        for (int level = 0; level < 2; level++)
        {
            float floorY = level * 3f;
            string front = level == 0 ? front0 : front1;
            PlaceMedievalCastleModule(front, parent, center + new Vector3(0f, 0f, -half), yaw, Vector3.one * 0.92f, floorY);
            PlaceMedievalCastleModule(side, parent, center + new Vector3(-half, 0f, 0f), yaw * Quaternion.Euler(0f, -90f, 0f), Vector3.one * 0.92f, floorY);
            PlaceMedievalCastleModule(side, parent, center + new Vector3(half, 0f, 0f), yaw * Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.92f, floorY);
            PlaceMedievalCastleModule(cornerAsset, parent, center + new Vector3(-half, 0f, -half), yaw, Vector3.one * 0.92f, floorY);
            PlaceMedievalCastleModule(cornerAsset, parent, center + new Vector3(half, 0f, -half), yaw * Quaternion.Euler(0f, 90f, 0f), Vector3.one * 0.92f, floorY);
        }

        PlaceMedievalCastleModule("Roof_Tower_RoundTiles", parent, center, yaw, Vector3.one * 0.95f, 6f);
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
