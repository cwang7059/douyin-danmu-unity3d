using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const string RealisticCastleResourceFolderPath = "RealisticCastles";
    private const string RealisticCastleFortressResourcePath = RealisticCastleResourceFolderPath + "/CastleFortress";
    private const string RealisticCastleBrickDiffusePath = RealisticCastleResourceFolderPath + "/Textures/castle_brick_diff";
    private const string RealisticCastleBrickNormalPath = RealisticCastleResourceFolderPath + "/Textures/castle_brick_nrm";
    private const string RealisticCastleBrickRoughPath = RealisticCastleResourceFolderPath + "/Textures/castle_brick_rough";
    private const string RealisticCastleRockDiffusePath = RealisticCastleResourceFolderPath + "/Textures/rock_wall_diff";
    private const string RealisticCastleRockNormalPath = RealisticCastleResourceFolderPath + "/Textures/rock_wall_nrm";

    private const float RealisticCastleTargetWidth = 14f;
    private const float RealisticCastleTargetLength = 27f;
    private const float RealisticCastleMaxHeight = 14f;
    // Quaternius Castle GLB: Y-up, gate roughly faces +Z; root handles faction flip on Y.
    private const float RealisticCastleModelYawOffset = 0f;

    private Material realisticCastleStoneMaterial;
    private Material realisticCastleRockMaterial;
    private bool? realisticCastlePrefabViable;

    private bool HasRealisticCastleFortress()
    {
        return Resources.Load<GameObject>(RealisticCastleFortressResourcePath + ".glb") != null
            || Resources.Load<GameObject>(RealisticCastleFortressResourcePath + ".gltf") != null
            || Resources.Load<GameObject>(RealisticCastleFortressResourcePath) != null;
    }

    private GameObject LoadRealisticCastleFortressPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(RealisticCastleFortressResourcePath);
        if (prefab != null)
        {
            return prefab;
        }

        prefab = Resources.Load<GameObject>(RealisticCastleFortressResourcePath + ".glb");
        if (prefab != null)
        {
            return prefab;
        }

        return Resources.Load<GameObject>(RealisticCastleFortressResourcePath + ".gltf");
    }

    private bool IsRealisticCastlePrefabViable()
    {
        if (realisticCastlePrefabViable.HasValue)
        {
            return realisticCastlePrefabViable.Value;
        }

        GameObject prefab = LoadRealisticCastleFortressPrefab();
        if (prefab == null)
        {
            realisticCastlePrefabViable = false;
            return false;
        }

        GameObject probe = Instantiate(prefab);
        probe.transform.localRotation = Quaternion.Euler(0f, RealisticCastleModelYawOffset, 0f);
        OrientCastleFortressStanding(probe);
        realisticCastlePrefabViable = IsCastleFortressPlausible(probe);
        UnityEngine.Object.Destroy(probe);
        return realisticCastlePrefabViable.Value;
    }

    private static bool IsCastleFortressPlausible(GameObject model)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            return false;
        }

        float height = bounds.size.y;
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (height < 1.5f || footprint < 2f)
        {
            return false;
        }

        if (height < footprint * 0.42f)
        {
            return false;
        }

        float minAxis = Mathf.Min(bounds.size.x, bounds.size.z);
        return minAxis >= height * 0.22f;
    }

    private GameObject BuildRealisticCastleFortress(string name, float centerWorldX, float centerZ, float centerLogicalX, bool beastFaction)
    {
        var root = new GameObject(name);
        root.transform.SetParent(decorRoot, false);
        root.transform.localPosition = CastleWorldPoint(centerWorldX, centerZ, 0f);
        root.transform.localRotation = Quaternion.Euler(0f, beastFaction ? 180f : 0f, 0f);
        root.transform.localScale = Vector3.one;

        GameObject prefab = LoadRealisticCastleFortressPrefab();
        if (prefab == null)
        {
            UnityEngine.Object.Destroy(root);
            return BuildMedievalGateFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        GameObject fortress = Instantiate(prefab, root.transform, false);
        fortress.name = "CastleFortress";
        fortress.transform.localPosition = Vector3.zero;
        fortress.transform.localRotation = Quaternion.Euler(0f, RealisticCastleModelYawOffset, 0f);
        fortress.transform.localScale = Vector3.one;

        OrientCastleFortressStanding(fortress);
        FaceCastleFortressGateTowardBattlefield(fortress, beastFaction);
        if (!IsCastleFortressPlausible(fortress))
        {
            UnityEngine.Object.Destroy(fortress);
            UnityEngine.Object.Destroy(root);
            return BuildMedievalGateFortress(name, centerWorldX, centerZ, centerLogicalX, beastFaction);
        }

        FitCastleFortressToFootprint(fortress, RealisticCastleTargetWidth, RealisticCastleTargetLength, RealisticCastleMaxHeight);
        ApplyRealisticCastleStoneMaterials(fortress);
        ApplyRealisticCastleFactionTint(fortress, beastFaction);

        AddBuildingObstacle(root, name, centerLogicalX, centerZ, 40f, 34f, 11f, 18f, 480f);
        return root;
    }

    private static void OrientCastleFortressStanding(GameObject model)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            return;
        }

        Vector3 size = bounds.size;
        if (size.y >= size.x * 0.85f && size.y >= size.z * 0.85f)
        {
            return;
        }

        if (size.x >= size.z)
        {
            model.transform.localRotation *= Quaternion.Euler(0f, 0f, 90f);
        }
        else
        {
            model.transform.localRotation *= Quaternion.Euler(-90f, 0f, 0f);
        }
    }

    private static void FaceCastleFortressGateTowardBattlefield(GameObject model, bool beastFaction)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            return;
        }

        Quaternion upright = model.transform.localRotation;
        float bestScore = float.MinValue;
        Quaternion bestRotation = upright;
        for (int step = 0; step < 4; step++)
        {
            float yaw = step * 90f;
            model.transform.localRotation = upright * Quaternion.Euler(0f, RealisticCastleModelYawOffset + yaw, 0f);
            if (!TryGetCastleModuleLocalBounds(model, out bounds))
            {
                continue;
            }

            float gateSign = beastFaction ? -1f : 1f;
            float forwardScore = bounds.max.x * gateSign - bounds.min.x * gateSign;
            float uprightScore = bounds.size.y * 4f;
            float score = forwardScore + uprightScore;
            if (score > bestScore)
            {
                bestScore = score;
                bestRotation = model.transform.localRotation;
            }
        }

        model.transform.localRotation = bestRotation;
    }

    private static void FitCastleFortressToFootprint(GameObject model, float targetWidth, float targetLength, float maxHeight)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            return;
        }

        float sizeX = Mathf.Max(0.01f, bounds.size.x);
        float sizeZ = Mathf.Max(0.01f, bounds.size.z);
        float height = Mathf.Max(0.01f, bounds.size.y);
        float scaleX = targetWidth / sizeX;
        float scaleZ = targetLength / sizeZ;
        float scale = Mathf.Min(scaleX, scaleZ);
        if (height * scale > maxHeight)
        {
            scale = maxHeight / height;
        }

        model.transform.localScale = Vector3.one * scale;
        AlignCastleKitModuleToFloor(model, 0f);
    }

    private Material GetRealisticCastleStoneMaterial()
    {
        if (realisticCastleStoneMaterial != null)
        {
            return realisticCastleStoneMaterial;
        }

        Texture2D diffuse = Resources.Load<Texture2D>(RealisticCastleBrickDiffusePath);
        Texture2D normal = Resources.Load<Texture2D>(RealisticCastleBrickNormalPath);
        Texture2D rough = Resources.Load<Texture2D>(RealisticCastleBrickRoughPath);
        if (diffuse == null)
        {
            diffuse = Resources.Load<Texture2D>(RealisticCastleRockDiffusePath);
            normal = Resources.Load<Texture2D>(RealisticCastleRockNormalPath);
        }

        if (diffuse == null)
        {
            realisticCastleStoneMaterial = GetOpaqueMaterial(new Color(0.52f, 0.48f, 0.42f, 1f));
            return realisticCastleStoneMaterial;
        }

        realisticCastleStoneMaterial = CreateCastlePbrMaterial(diffuse, normal, rough, new Color(0.92f, 0.88f, 0.82f, 1f), new Vector2(2.4f, 2.4f));
        return realisticCastleStoneMaterial;
    }

    private Material GetRealisticCastleRockMaterial()
    {
        if (realisticCastleRockMaterial != null)
        {
            return realisticCastleRockMaterial;
        }

        Texture2D diffuse = Resources.Load<Texture2D>(RealisticCastleRockDiffusePath);
        Texture2D normal = Resources.Load<Texture2D>(RealisticCastleRockNormalPath);
        if (diffuse == null)
        {
            return GetRealisticCastleStoneMaterial();
        }

        realisticCastleRockMaterial = CreateCastlePbrMaterial(diffuse, normal, null, new Color(0.78f, 0.74f, 0.68f, 1f), new Vector2(3f, 3f));
        return realisticCastleRockMaterial;
    }

    private Material CreateCastlePbrMaterial(Texture2D albedo, Texture2D normal, Texture2D roughness, Color tint, Vector2 tiling)
    {
        Material material = new Material(FindRuntimeShader(null, "Standard", "Legacy Shaders/Diffuse", "Unlit/Texture"));

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", albedo);
            material.SetTextureScale("_MainTex", tiling);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", albedo);
            material.SetTextureScale("_BaseMap", tiling);
        }

        if (normal != null)
        {
            if (material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_NormalMap"))
            {
                material.SetTexture("_NormalMap", normal);
            }
        }

        if (roughness != null && material.HasProperty("_MetallicGlossMap"))
        {
            material.SetTexture("_MetallicGlossMap", roughness);
        }
        else if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.12f);
        }

        if (material.HasProperty("_Color"))
        {
            material.color = tint;
        }

        ApplyOpaqueDoubleSided(material);
        return material;
    }

    private void ApplyRealisticCastleStoneMaterials(GameObject root)
    {
        Material stone = GetRealisticCastleStoneMaterial();
        Material rock = GetRealisticCastleRockMaterial();
        if (stone == null)
        {
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;

            bool useRock = renderer.name.IndexOf("rock", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0
                || renderer.name.IndexOf("hill", StringComparison.OrdinalIgnoreCase) >= 0;
            Material target = useRock && rock != null ? rock : stone;

            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = target;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void ApplyRealisticCastleFactionTint(GameObject root, bool beastFaction)
    {
        Color accent = beastFaction
            ? new Color(0.78f, 0.32f, 0.24f, 1f)
            : new Color(0.28f, 0.46f, 0.72f, 1f);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
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

            Material[] materials = renderer.materials;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null && materials[m].HasProperty("_Color"))
                {
                    materials[m].color = Color.Lerp(materials[m].color, accent, 0.28f);
                }
            }
        }
    }
}
