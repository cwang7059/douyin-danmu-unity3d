using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private static readonly string[] AircraftBombResourceCandidates =
    {
        "AircraftBomb/Mk82BombPrefab",
        "AircraftBomb/Mk82Bomb",
    };

    private const string AircraftBombDiffusePath = "AircraftBomb/skin";
    private const float AircraftBombTargetLength = AircraftModelTargetHeight * 1.35f;
    /// <summary>MK-82 长轴 +X；绑定到弹体节点 +Z 后由水平偏航控制朝向，下落全程保持横置。</summary>
    private static readonly Quaternion AircraftBombModelBindRotation = Quaternion.Euler(0f, -90f, 0f);

    private GameObject aircraftBombMeshPrototype;
    private Material aircraftBombMaterial;

    private void RebuildBombProjectilePool()
    {
        if (projectiles == null || projectiles.Count == 0)
        {
            return;
        }

        int removed = 0;
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileView projectile = projectiles[i];
            if (projectile?.root == null || !projectile.root.name.StartsWith("Bomb_", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (projectile.root != null)
            {
                Destroy(projectile.root);
            }

            projectiles.RemoveAt(i);
            removed++;
        }

        if (removed > 0)
        {
            if (aircraftBombMeshPrototype != null)
            {
                Destroy(aircraftBombMeshPrototype);
                aircraftBombMeshPrototype = null;
            }

            PrewarmAircraftBombMesh();
            PrewarmProjectiles(ProjectileKind.Bomb, PrewarmBombProjectiles, AircraftBombVisualColor);
            Debug.Log($"[AircraftBomb] Rebuilt bomb projectile pool ({removed} slots) with MK-82 mesh.");
        }
    }

    private void PrewarmAircraftBombMesh()
    {
        if (aircraftBombMeshPrototype != null)
        {
            return;
        }

        GameObject source = LoadAircraftBombResourceRoot();
        if (source != null)
        {
            aircraftBombMeshPrototype = CacheAircraftBombTemplate(source);
            Debug.Log("[AircraftBomb] Prewarm: MK-82 mesh from Resources.");
            return;
        }

        aircraftBombMeshPrototype = CreateAircraftBombMeshPrototype();
        if (aircraftBombMeshPrototype != null)
        {
            Debug.Log("[AircraftBomb] Prewarm: MK-82 mesh fallback from Resources meshes.");
            return;
        }

        Debug.LogWarning("[AircraftBomb] Prewarm failed — 运行 tools/import-aircraft-bomb.ps1");
    }

    private bool TryInstantiateAircraftBombVisual(Transform parent, out Transform bombTransform)
    {
        bombTransform = null;
        if (parent == null)
        {
            return false;
        }

        PrewarmAircraftBombMesh();
        if (aircraftBombMeshPrototype == null)
        {
            return false;
        }

        GameObject instance = Instantiate(aircraftBombMeshPrototype, parent, false);
        instance.name = "Mk82Bomb";
        instance.SetActive(true);
        ClearHideFlagsInHierarchy(instance);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        AlignBombLongAxisToForward(instance);
        ApplyAircraftBombMaterials(instance);
        bombTransform = instance.transform;
        return true;
    }

    private GameObject CacheAircraftBombTemplate(GameObject source)
    {
        if (source == null)
        {
            return null;
        }

        var template = Instantiate(source, modelCacheRoot, false);
        template.name = "AircraftBomb_Template";
        template.SetActive(false);
        FitAircraftBombMesh(template);
        AlignBombLongAxisToForward(template);
        ApplyAircraftBombMaterials(template);
        return template;
    }

    private static GameObject LoadAircraftBombResourceRoot()
    {
        for (int i = 0; i < AircraftBombResourceCandidates.Length; i++)
        {
            string resourcePath = AircraftBombResourceCandidates[i];
            GameObject source = Resources.Load<GameObject>(resourcePath);
            if (source != null && source.GetComponentsInChildren<Renderer>(true).Length > 0)
            {
                return source;
            }

            Object[] gameObjects = Resources.LoadAll(resourcePath, typeof(GameObject));
            GameObject best = PickBestRenderableRoot(gameObjects);
            if (best != null)
            {
                return best;
            }
        }

        Object[] folderAssets = Resources.LoadAll("AircraftBomb", typeof(GameObject));
        return PickBestRenderableRoot(folderAssets);
    }

    private void EnsureBombProjectileHeadAnchor(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        bool needsFreshAnchor = projectile.head == null
            || projectile.head.GetComponent<Renderer>() != null
            || !string.Equals(projectile.head.name, "BombBody", System.StringComparison.Ordinal);

        if (!needsFreshAnchor)
        {
            return;
        }

        if (projectile.head != null)
        {
            Object.Destroy(projectile.head.gameObject);
        }

        var headAnchor = new GameObject("BombBody");
        headAnchor.transform.SetParent(projectile.root.transform, false);
        headAnchor.transform.localPosition = Vector3.zero;
        headAnchor.transform.localRotation = Quaternion.identity;
        headAnchor.transform.localScale = Vector3.one;
        projectile.head = headAnchor.transform;
    }

    private void EnsureBombProjectileMeshVisual(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        EnsureBombProjectileHeadAnchor(projectile);

        PrewarmAircraftBombMesh();
        if (aircraftBombMeshPrototype == null)
        {
            projectile.usesBombMesh = false;
            return;
        }

        for (int i = projectile.head.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(projectile.head.GetChild(i).gameObject);
        }

        if (TryInstantiateAircraftBombVisual(projectile.head, out _))
        {
            projectile.usesBombMesh = true;
            projectile.head.localScale = Vector3.one;
            if (projectile.line != null)
            {
                projectile.line.enabled = false;
            }

            return;
        }

        projectile.usesBombMesh = false;
        GameObject fallback = CreatePrimitive(PrimitiveType.Capsule, "BombFallback", projectile.head);
        fallback.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
        Renderer renderer = fallback.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetOpaqueMaterial(new Color(0.35f, 0.38f, 0.32f, 1f));
        }
    }

    private GameObject CreateAircraftBombMeshPrototype()
    {
        Mesh bestMesh = null;
        int bestVertexCount = 0;
        for (int i = 0; i < AircraftBombResourceCandidates.Length; i++)
        {
            Object[] assets = Resources.LoadAll(AircraftBombResourceCandidates[i]);
            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is not Mesh mesh || mesh.vertexCount <= 0)
                {
                    continue;
                }

                if (mesh.vertexCount > bestVertexCount)
                {
                    bestMesh = mesh;
                    bestVertexCount = mesh.vertexCount;
                }
            }
        }

        if (bestMesh == null)
        {
            return null;
        }

        var root = new GameObject("AircraftBomb_MeshTemplate");
        root.transform.SetParent(modelCacheRoot, false);
        root.SetActive(false);
        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = bestMesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        ApplyAircraftBombMaterials(root);
        FitAircraftBombMesh(root);
        AlignBombLongAxisToForward(root);
        return root;
    }

    private void FitAircraftBombMesh(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            model.transform.localScale = Vector3.one * AircraftBombTargetLength;
            return;
        }

        float length = Mathf.Max(0.05f, bounds.size.x, bounds.size.y, bounds.size.z);
        float scale = AircraftBombTargetLength / length;
        model.transform.localScale = Vector3.one * scale;
    }

    private static void AlignBombLongAxisToForward(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            model.transform.localRotation = AircraftBombModelBindRotation;
            return;
        }

        Vector3 size = bounds.size;
        int axis = 0;
        float maxSize = size.x;
        if (size.y > maxSize)
        {
            axis = 1;
            maxSize = size.y;
        }

        if (size.z > maxSize)
        {
            axis = 2;
        }

        switch (axis)
        {
            case 1:
                model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                break;
            case 2:
                model.transform.localRotation = Quaternion.identity;
                break;
            default:
                model.transform.localRotation = AircraftBombModelBindRotation;
                break;
        }
    }

    private Material GetAircraftBombMaterial()
    {
        if (aircraftBombMaterial != null)
        {
            return aircraftBombMaterial;
        }

        Texture2D diffuse = Resources.Load<Texture2D>(AircraftBombDiffusePath);
        if (diffuse != null)
        {
            aircraftBombMaterial = GetOpaqueMaterial(new Color(0.85f, 0.85f, 0.82f, 1f));
            if (aircraftBombMaterial != null && aircraftBombMaterial.HasProperty("_MainTex"))
            {
                aircraftBombMaterial.mainTexture = diffuse;
            }

            return aircraftBombMaterial;
        }

        aircraftBombMaterial = GetOpaqueMaterial(new Color(0.42f, 0.44f, 0.38f, 1f));
        return aircraftBombMaterial;
    }

    private void ApplyAircraftBombMaterials(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Material material = GetAircraftBombMaterial();
        if (material == null)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }
}
