using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const string CastleKitResourceFolderPath = "Kenney/CastleKit";
    private const float CastleVisualScale = 2f;
    private const float RomanFortModuleScale = 2.35f;

    private static readonly Color RomanStoneHuman = new Color(0.74f, 0.70f, 0.60f, 1f);
    private static readonly Color RomanStoneBeast = new Color(0.70f, 0.58f, 0.48f, 1f);
    private static readonly Color RomanTrimHuman = new Color(0.42f, 0.50f, 0.62f, 1f);
    private static readonly Color RomanTrimBeast = new Color(0.62f, 0.36f, 0.28f, 1f);

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

    public static float HumanCastleMinUnitX => HumanCastleGateX - 72f * CastleVisualScale;
    public static float BeastCastleMaxUnitX => BeastCastleGateX + 72f * CastleVisualScale;

    private static readonly float[] CastleSpawnLanes = { -168f, -108f, -48f, 12f, 72f, 132f, -228f, 192f };

    private Transform humanCastleRoot;
    private Transform beastCastleRoot;

    private static float WorldToLogicalX(float worldX)
    {
        return worldX / LogicalToWorld;
    }

    private static Vector3 CastleWorldPoint(float worldX, float logicalZ, float height = 0f)
    {
        return new Vector3(worldX, height, logicalZ * LogicalToWorld);
    }

    private void CreateFactionCastles()
    {
        humanCastleRoot = CreateRomanFortressCastle("HumanCastle", HumanCastleWorldX, HumanCastleCenterZ, false).transform;
        beastCastleRoot = CreateRomanFortressCastle("BeastCastle", BeastCastleWorldX, BeastCastleCenterZ, true).transform;
        CreateCastleFlankPads();
        CreateCastleGateRoads();
    }

    private void CreateCastleFlankPads()
    {
        Material grassMaterial = GetTexturedOpaqueMaterial(GrassTextureResourcePath, new Color(0.66f, 0.78f, 0.50f, 1f), new Vector2(8f, 10f), 0.08f);
        float padW = 16f * CastleVisualScale;
        float padH = 20f * CastleVisualScale;
        CreateBattlefieldPlane("HumanCastlePad", CastleWorldPoint(HumanCastleWorldX, 0f, 0.034f), new Vector2(padW, padH), grassMaterial);
        CreateBattlefieldPlane("BeastCastlePad", CastleWorldPoint(BeastCastleWorldX, 0f, 0.034f), new Vector2(padW, padH), grassMaterial);
    }

    private void CreateCastleGateRoads()
    {
        Material roadMaterial = GetOpaqueMaterial(RoadColor);
        float roadX = HumanCastleGateWorldX + 8f;
        CreateBattlefieldPlane("HumanCastleGateRoad", new Vector3(roadX, 0.038f, -1.2f), new Vector2(6f, 2.8f), roadMaterial, -4f);
        CreateBattlefieldPlane("HumanCastleGateRoad2", new Vector3(roadX, 0.038f, 1.2f), new Vector2(6f, 2.8f), roadMaterial, 4f);

        roadX = BeastCastleGateWorldX - 8f;
        CreateBattlefieldPlane("BeastCastleGateRoad", new Vector3(roadX, 0.038f, -1.2f), new Vector2(6f, 2.8f), roadMaterial, 4f);
        CreateBattlefieldPlane("BeastCastleGateRoad2", new Vector3(roadX, 0.038f, 1.2f), new Vector2(6f, 2.8f), roadMaterial, -4f);
    }

    private GameObject CreateRomanFortressCastle(string name, float centerWorldX, float centerZ, bool beastFaction)
    {
        float centerLogicalX = WorldToLogicalX(centerWorldX);
        if (HasKenneyRomanFortAssets())
        {
            return BuildKenneyRomanFort(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        return BuildPrimitiveRomanFort(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
    }

    private static bool HasKenneyRomanFortAssets()
    {
        return LoadCastleKitPrefab("tower-square-base-border") != null
            && LoadCastleKitPrefab("tower-square-mid-door") != null
            && LoadCastleKitPrefab("wall") != null;
    }

    private GameObject BuildKenneyRomanFort(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);

        float m = RomanFortModuleScale * CastleVisualScale;
        float gateSign = beastFaction ? -1f : 1f;
        Color stone = beastFaction ? RomanStoneBeast : RomanStoneHuman;
        Color trim = beastFaction ? RomanTrimBeast : RomanTrimHuman;

        PlaceCastleModule(root.transform, "tower-square-base-border", Vector3.zero, Quaternion.identity, m * 2.05f);
        PlaceCastleModule(root.transform, "tower-square-mid-door", new Vector3(0f, 2.55f * m, 0f), Quaternion.identity, m * 2.05f);
        PlaceCastleModule(root.transform, "tower-square-top-roof-high", new Vector3(0f, 5.35f * m, 0f), Quaternion.identity, m * 1.95f);

        float wallSpan = 3.6f * m;
        float wallStep = 2.05f * m;
        for (int i = -1; i <= 1; i++)
        {
            float z = i * wallStep;
            PlaceCastleModule(root.transform, "wall", new Vector3(0f, 0.15f * m, z), Quaternion.identity, m * 1.85f);
            PlaceCastleModule(root.transform, "wall", new Vector3(-wallSpan, 0.15f * m, z), Quaternion.Euler(0f, 90f, 0f), m * 1.85f);
            PlaceCastleModule(root.transform, "wall", new Vector3(wallSpan, 0.15f * m, z), Quaternion.Euler(0f, 90f, 0f), m * 1.85f);
        }

        PlaceCastleModule(root.transform, "wall-corner", new Vector3(-wallSpan, 0.15f * m, -wallStep), Quaternion.identity, m * 1.9f);
        PlaceCastleModule(root.transform, "wall-corner", new Vector3(wallSpan, 0.15f * m, -wallStep), Quaternion.Euler(0f, 90f, 0f), m * 1.9f);
        PlaceCastleModule(root.transform, "wall-corner", new Vector3(wallSpan, 0.15f * m, wallStep), Quaternion.Euler(0f, 180f, 0f), m * 1.9f);
        PlaceCastleModule(root.transform, "wall-corner", new Vector3(-wallSpan, 0.15f * m, wallStep), Quaternion.Euler(0f, 270f, 0f), m * 1.9f);

        PlaceCastleModule(root.transform, "tower-square-mid-door", new Vector3(-wallSpan * 0.92f, 0f, -wallStep * 0.92f), Quaternion.identity, m * 1.35f);
        PlaceCastleModule(root.transform, "tower-square-mid-door", new Vector3(wallSpan * 0.92f, 0f, -wallStep * 0.92f), Quaternion.identity, m * 1.35f);
        PlaceCastleModule(root.transform, "tower-square-mid-door", new Vector3(-wallSpan * 0.92f, 0f, wallStep * 0.92f), Quaternion.identity, m * 1.35f);
        PlaceCastleModule(root.transform, "tower-square-mid-door", new Vector3(wallSpan * 0.92f, 0f, wallStep * 0.92f), Quaternion.identity, m * 1.35f);

        string gateAsset = LoadCastleKitPrefab("metal-gate") != null ? "metal-gate" : "gate";
        PlaceCastleModule(root.transform, gateAsset, new Vector3(gateSign * 4.15f * m, 0.35f * m, 0f), Quaternion.Euler(0f, gateSign > 0 ? 90f : 270f, 0f), m * 1.75f);
        PlaceCastleModule(root.transform, "stairs-stone", new Vector3(gateSign * 5.1f * m, 0f, 0f), Quaternion.Euler(0f, gateSign > 0 ? 90f : 270f, 0f), m * 1.55f);

        PlaceCastleModule(root.transform, "bridge-straight-pillar", new Vector3(gateSign * 3.5f * m, 0f, -1.55f * m), Quaternion.identity, m * 1.45f);
        PlaceCastleModule(root.transform, "bridge-straight-pillar", new Vector3(gateSign * 3.5f * m, 0f, 1.55f * m), Quaternion.identity, m * 1.45f);
        PlaceCastleModule(root.transform, "wall-pillar", new Vector3(gateSign * 3.2f * m, 0f, -2.35f * m), Quaternion.identity, m * 1.25f);
        PlaceCastleModule(root.transform, "wall-pillar", new Vector3(gateSign * 3.2f * m, 0f, 2.35f * m), Quaternion.identity, m * 1.25f);

        if (LoadCastleKitPrefab("flag-pennant") != null)
        {
            PlaceCastleModule(root.transform, "flag-pennant", new Vector3(0f, 6.2f * m, 0f), Quaternion.identity, m * 1.6f);
        }
        else
        {
            PlaceCastleModule(root.transform, "flag", new Vector3(0f, 6.2f * m, 0f), Quaternion.identity, m * 1.5f);
        }

        ApplyRomanFortTint(root, stone, trim);
        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 62f * CastleVisualScale, 78f * CastleVisualScale, 10f * CastleVisualScale, 18f, 420f);
        return root;
    }

    private GameObject BuildPrimitiveRomanFort(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        Color stone = beastFaction ? RomanStoneBeast : RomanStoneHuman;
        Color trim = beastFaction ? RomanTrimBeast : RomanTrimHuman;
        float gateSign = beastFaction ? -1f : 1f;

        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);
        root.transform.localScale = Vector3.one * CastleVisualScale;

        var podium = CreatePrimitive(PrimitiveType.Cylinder, $"{name}_Podium", root.transform);
        podium.transform.localScale = new Vector3(14f, 1.2f, 14f);
        podium.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        podium.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(stone);

        var keep = CreatePrimitive(PrimitiveType.Cube, $"{name}_Keep", root.transform);
        keep.transform.localScale = new Vector3(11f, 9f, 11f);
        keep.transform.localPosition = new Vector3(0f, 5.5f, 0f);
        keep.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(stone);

        var battlement = CreatePrimitive(PrimitiveType.Cube, $"{name}_Battlement", root.transform);
        battlement.transform.localScale = new Vector3(12f, 1.2f, 12f);
        battlement.transform.localPosition = new Vector3(0f, 10.8f, 0f);
        battlement.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(trim);

        for (int i = -1; i <= 1; i += 2)
        {
            var column = CreatePrimitive(PrimitiveType.Cylinder, $"{name}_Column_{i}", root.transform);
            column.transform.localScale = new Vector3(1.4f, 5.5f, 1.4f);
            column.transform.localPosition = new Vector3(gateSign * 5.8f, 2.8f, i * 4.2f);
            column.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(trim);
        }

        var gate = CreatePrimitive(PrimitiveType.Cube, $"{name}_Gate", root.transform);
        gate.transform.localScale = new Vector3(0.8f, 5f, 5f);
        gate.transform.localPosition = new Vector3(gateSign * 6.2f, 2.5f, 0f);
        gate.GetComponent<Renderer>().sharedMaterial = GetOpaqueMaterial(trim);

        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 62f * CastleVisualScale, 78f * CastleVisualScale, 10f * CastleVisualScale, 16f, 420f);
        return root;
    }

    private void PlaceCastleModule(Transform parent, string assetName, Vector3 localPosition, Quaternion localRotation, float scale)
    {
        var prefab = LoadCastleKitPrefab(assetName);
        if (prefab == null || parent == null)
        {
            return;
        }

        var instance = Instantiate(prefab, parent, false);
        instance.name = assetName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one * scale;
        ConfigureCastleKitInstance(instance);
    }

    private void ConfigureCastleKitInstance(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                ApplyOpaqueDoubleSided(materials[m]);
            }
        }
    }

    private void ApplyRomanFortTint(GameObject root, Color stone, Color trim)
    {
        if (root == null)
        {
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool accent = renderer.name.IndexOf("flag", System.StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("metal", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Color target = accent ? trim : stone;
            var materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null && materials[m].HasProperty("_Color"))
                {
                    materials[m].color = Color.Lerp(materials[m].color, target, accent ? 0.55f : 0.42f);
                }
            }
        }
    }

    private static GameObject LoadCastleKitPrefab(string assetName)
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return null;
        }

        return Resources.Load<GameObject>(CastleKitResourceFolderPath + "/" + assetName);
    }

    private void GetHumanCastleSpawn(int seed, out float x, out float z)
    {
        int lane = Mathf.Abs(seed) % CastleSpawnLanes.Length;
        int rank = (Mathf.Abs(seed) / CastleSpawnLanes.Length) % 8;
        x = HumanCastleGateX + 8f + rank * 10f;
        z = CastleSpawnLanes[lane] + (Noise(seed * 1.73f) - 0.5f) * 12f;
    }

    private void GetBeastCastleSpawn(int seed, out float x, out float z)
    {
        int lane = Mathf.Abs(seed) % CastleSpawnLanes.Length;
        int rank = (Mathf.Abs(seed) / CastleSpawnLanes.Length) % 8;
        x = BeastCastleGateX - 8f - rank * 10f;
        z = CastleSpawnLanes[lane] + (Noise(seed * 2.11f) - 0.5f) * 14f;
    }

    private void GetFactionCastleSpawn(FactionId faction, int seed, out float x, out float z, out int facing)
    {
        bool fromBeastCastle = faction == FactionId.Green || faction == FactionId.Zombie;
        if (fromBeastCastle)
        {
            GetBeastCastleSpawn(seed, out x, out z);
            facing = -1;
            return;
        }

        GetHumanCastleSpawn(seed, out x, out z);
        facing = 1;
    }
}
