using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const int PrewarmRocketProjectiles = 28;
    private static readonly Color TacticalRocketVisualColor = new Color(0.94f, 0.48f, 0.16f, 1f);
    private const float TacticalRocketTargetLength = 0.52f;
    private const string TacticalRocketDiffusePath = "Nuclear/Textures/missile01_Diff";

    private static readonly string[] TacticalRocketResourceCandidates =
    {
        "Projectiles/TacticalRocket/TacticalRocket",
        "Projectiles/TacticalRocket",
        "Nuclear/CruiseMissile",
        "Nuclear/TacticalMissile",
    };

    private GameObject tacticalRocketMeshPrototype;
    private Material tacticalRocketMaterial;

    private void PrewarmTacticalRocketMesh()
    {
        if (tacticalRocketMeshPrototype != null)
        {
            return;
        }

        tacticalRocketMeshPrototype = CreateTacticalRocketMeshPrototype();
    }

    private void RebuildRocketProjectilePool()
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            ProjectileView projectile = projectiles[i];
            if (projectile == null || projectile.kind != ProjectileKind.Rocket)
            {
                continue;
            }

            if (projectile.root != null)
            {
                Destroy(projectile.root);
            }

            projectiles.RemoveAt(i);
        }

        PrewarmTacticalRocketMesh();
        PrewarmProjectiles(ProjectileKind.Rocket, PrewarmRocketProjectiles, TacticalRocketVisualColor);
    }

    private static GameObject LoadTacticalRocketResourceRoot()
    {
        for (int i = 0; i < TacticalRocketResourceCandidates.Length; i++)
        {
            string resourcePath = TacticalRocketResourceCandidates[i];
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

        Object[] folderAssets = Resources.LoadAll("Projectiles/TacticalRocket", typeof(GameObject));
        return PickBestRenderableRoot(folderAssets);
    }

    private void EnsureRocketProjectileHeadAnchor(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        bool needsFreshAnchor = projectile.head == null
            || projectile.head.GetComponent<Renderer>() != null
            || !string.Equals(projectile.head.name, "RocketBody", System.StringComparison.Ordinal);

        if (!needsFreshAnchor)
        {
            return;
        }

        if (projectile.head != null)
        {
            Destroy(projectile.head.gameObject);
        }

        var headAnchor = new GameObject("RocketBody");
        headAnchor.transform.SetParent(projectile.root.transform, false);
        headAnchor.transform.localPosition = Vector3.zero;
        headAnchor.transform.localRotation = Quaternion.identity;
        headAnchor.transform.localScale = Vector3.one;
        projectile.head = headAnchor.transform;
    }

    private void EnsureRocketProjectileMeshVisual(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        EnsureRocketProjectileHeadAnchor(projectile);
        PrewarmTacticalRocketMesh();
        if (tacticalRocketMeshPrototype == null)
        {
            projectile.usesRocketMesh = false;
            return;
        }

        for (int i = projectile.head.childCount - 1; i >= 0; i--)
        {
            Destroy(projectile.head.GetChild(i).gameObject);
        }

        var visual = Instantiate(tacticalRocketMeshPrototype, projectile.head, false);
        visual.name = "TacticalRocketMesh";
        visual.SetActive(true);
        projectile.usesRocketMesh = true;
        projectile.head.localScale = Vector3.one;
        if (projectile.line != null)
        {
            projectile.line.enabled = false;
        }
    }

    private GameObject CreateTacticalRocketMeshPrototype()
    {
        GameObject source = LoadTacticalRocketResourceRoot();
        if (source != null)
        {
            var instance = Instantiate(source, modelCacheRoot, false);
            instance.name = "TacticalRocket_MeshTemplate";
            instance.SetActive(false);
            StripForProjectileTemplate(instance);
            FitTacticalRocketMesh(instance);
            AlignRocketLongAxisToForward(instance);
            ApplyTacticalRocketMaterials(instance);
            return instance;
        }

        Mesh bestMesh = null;
        int bestVertexCount = 0;
        for (int i = 0; i < TacticalRocketResourceCandidates.Length; i++)
        {
            Object[] assets = Resources.LoadAll(TacticalRocketResourceCandidates[i]);
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
            Debug.LogWarning("[ApocalypseKing] Tactical rocket mesh not found. Run tools/import-rocket-projectile.ps1");
            return null;
        }

        var root = new GameObject("TacticalRocket_MeshTemplate");
        root.transform.SetParent(modelCacheRoot, false);
        root.SetActive(false);
        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = bestMesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        ApplyTacticalRocketMaterials(root);
        FitTacticalRocketMesh(root);
        AlignRocketLongAxisToForward(root);
        Debug.Log($"[ApocalypseKing] Tactical rocket mesh template ({bestVertexCount} verts).");
        return root;
    }

    private static void StripForProjectileTemplate(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        var animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                Destroy(animators[i]);
            }
        }
    }

    private void FitTacticalRocketMesh(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            model.transform.localScale = Vector3.one * TacticalRocketTargetLength;
            return;
        }

        float length = Mathf.Max(0.05f, bounds.size.x, bounds.size.y, bounds.size.z);
        float scale = TacticalRocketTargetLength / length;
        model.transform.localScale = Vector3.one * scale;
    }

    private static void AlignRocketLongAxisToForward(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
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
                model.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                break;
            case 2:
                model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            default:
                model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
        }
    }

    private void ApplyTacticalRocketMaterials(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Material material = GetTacticalRocketMaterial();
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private Material GetTacticalRocketMaterial()
    {
        if (tacticalRocketMaterial != null)
        {
            return tacticalRocketMaterial;
        }

        Texture2D diffuse = Resources.Load<Texture2D>(TacticalRocketDiffusePath);
        tacticalRocketMaterial = GetOpaqueMaterial(TacticalRocketVisualColor);
        if (tacticalRocketMaterial != null && diffuse != null && tacticalRocketMaterial.HasProperty("_MainTex"))
        {
            tacticalRocketMaterial.mainTexture = diffuse;
        }

        return tacticalRocketMaterial;
    }
}
