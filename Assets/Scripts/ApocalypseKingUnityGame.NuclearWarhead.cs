using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityGLTF;

public sealed partial class ApocalypseKingUnityGame
{
    private enum NuclearStrikePhase
    {
        Idle,
        InFlight,
        PostDetonation,
    }

    private static readonly string[] NuclearMissileResourceCandidates =
    {
        "Nuclear/CruiseMissilePrefab",
        "Nuclear/CruiseMissile",
        "Nuclear/TomahawkMissilePrefab",
        "Nuclear/TomahawkMissile",
        "Nuclear/TacticalMissilePrefab",
        "Nuclear/TacticalMissile",
    };

    private static readonly string[] NuclearMissileStreamingCandidates =
    {
        "Nuclear/CruiseMissile.glb",
        "Nuclear/TomahawkMissile.glb",
        "Nuclear/TacticalMissile.fbx",
    };

    private static readonly string[] NuclearMissileDiffuseCandidates =
    {
        "Nuclear/Textures/missile01_Diff",
        "Nuclear/colormap",
    };

    /// <summary>弹体显示长度：比直升机略大（约 1.5×），小于坦克/城堡，俯视仍清晰。</summary>
    private const float NuclearWarheadTargetLength = AircraftModelTargetHeight * 1.5f;
    private const float NuclearWarheadMinVisibleLength = AircraftModelTargetHeight * 1.1f;
    private const float NuclearWarheadCameraTiltDegrees = 12f;
    private const float NuclearWarheadLaunchHeight = 12f;
    private const float NuclearWarheadImpactHeight = 0.22f;
    private const float NuclearWarheadLaunchForwardLogical = 24f;
    private const float NuclearWarheadMinFlightSeconds = 3.2f;
    private const float NuclearWarheadMaxFlightSeconds = 7.5f;
    private const float NuclearWarheadFlightSpeed = 280f;
    private const float NuclearDetonationVfxHoldSeconds = 3.5f;
    private const float NuclearWarheadTrailInterval = 0.09f;
    private const float NuclearWarningRefreshSeconds = 0.55f;
    private const float NuclearOrientationSampleDelta = 0.018f;

    private NuclearStrikePhase nuclearStrikePhase;
    private GameObject nuclearWarheadRoot;
    private Transform nuclearWarheadBody;
    private GameObject nuclearMissilePrototype;
    private Material nuclearMissileMaterial;
    private Light nuclearWarheadFlightLight;
    private float nuclearWarheadFlightProgress;
    private float nuclearWarheadFlightDuration;
    private float nuclearWarheadArcPeakExtra;
    private float nuclearWarheadFromX;
    private float nuclearWarheadFromZ;
    private float nuclearWarheadToX;
    private float nuclearWarheadToZ;
    private float nuclearWarheadTrailTimer;
    private float nuclearWarheadPostVfxTimer;
    private float nuclearWarningRefreshTimer;
    private Vector3 nuclearWarheadLastWorldPosition;

    private bool IsNuclearStrikeSequenceActive => nuclearStrikePhase != NuclearStrikePhase.Idle;

    private async Task PrewarmNuclearMissileAsync()
    {
        GameObject resourceRoot = LoadNuclearMissilePrefabRoot();
        if (IsNuclearMissilePrototypeValid(resourceRoot))
        {
            nuclearMissilePrototype = CacheNuclearMissileTemplate(resourceRoot, preserveImportedMaterials: true);
            Debug.Log("[Nuclear] Prewarm: realistic GLB/prefab from Resources.");
            return;
        }

        for (int i = 0; i < NuclearMissileStreamingCandidates.Length; i++)
        {
            string relativePath = NuclearMissileStreamingCandidates[i];
            string localPath = Path.Combine(Application.streamingAssetsPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localPath))
            {
                continue;
            }

            GameObject streamingPrototype = await LoadNuclearMissileFromStreamingAsync(relativePath);
            if (IsNuclearMissilePrototypeValid(streamingPrototype))
            {
                nuclearMissilePrototype = streamingPrototype;
                Debug.Log($"[Nuclear] Prewarm: StreamingAssets/{relativePath}");
                return;
            }
        }

        nuclearMissilePrototype = CreateNuclearMissileMeshPrototype();
        if (IsNuclearMissilePrototypeValid(nuclearMissilePrototype))
        {
            Debug.Log("[Nuclear] Prewarm: FBX mesh fallback from Resources.");
            return;
        }

        Debug.LogError("[Nuclear] Prewarm failed — 运行 tools/import-nuclear-warhead.ps1");
    }

    private async Task<GameObject> LoadNuclearMissileFromStreamingAsync(string relativePath)
    {
        var loaderRoot = new GameObject("GLTFLoader_NuclearMissile");
        loaderRoot.transform.SetParent(modelCacheRoot, false);
        var gltf = loaderRoot.AddComponent<GLTFComponent>();
        gltf.GLTFUri = relativePath;
        gltf.LoadFromStreamingAssets = true;
        gltf.PlayAnimationOnLoad = false;
        gltf.ImportAnimationMethod = AnimationMethod.Legacy;
        gltf.HideSceneObjDuringLoad = true;
        gltf.loadOnStart = false;
        gltf.Multithreaded = true;
        gltf.Timeout = 12;
        gltf.KeepCPUCopyOfMesh = false;
        gltf.KeepCPUCopyOfTexture = false;
        gltf.ShaderOverride = FindRuntimeShader(
            "RuntimeMaterials/RuntimeGltfPbrMetallicRoughness",
            "GLTF/PbrMetallicRoughness",
            "Standard",
            "Legacy Shaders/Diffuse");

        await gltf.Load();

        GameObject scene = gltf.LastLoadedScene;
        if (scene == null)
        {
            Destroy(loaderRoot);
            return null;
        }

        scene.name = "NuclearMissile_StreamingTemplate";
        scene.transform.SetParent(modelCacheRoot, false);
        scene.SetActive(false);
        ConfigureNuclearMissilePrototype(scene);
        Destroy(loaderRoot);
        return scene;
    }

    private GameObject CacheNuclearMissileTemplate(GameObject source, bool preserveImportedMaterials)
    {
        if (source == null)
        {
            return null;
        }

        if (source.transform.parent == modelCacheRoot && source.name.Contains("Template"))
        {
            if (preserveImportedMaterials)
            {
                ConfigureNuclearMissilePrototype(source);
            }
            else
            {
                ApplyNuclearMissileMaterialsForce(source);
            }

            return source;
        }

        GameObject template = Instantiate(source, modelCacheRoot, false);
        template.name = "NuclearMissile_ResourceTemplate";
        template.SetActive(false);
        if (preserveImportedMaterials)
        {
            ConfigureNuclearMissilePrototype(template);
        }
        else
        {
            ApplyNuclearMissileMaterialsForce(template);
        }

        return template;
    }

    private void ConfigureNuclearMissilePrototype(GameObject prototype)
    {
        if (prototype == null)
        {
            return;
        }

        Renderer[] renderers = prototype.GetComponentsInChildren<Renderer>(true);
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
            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                ApplyOpaqueDoubleSided(materials[m]);
            }
        }
    }

    private static bool IsNuclearMissilePrototypeValid(GameObject prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        Renderer[] renderers = prototype.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && renderer.enabled)
            {
                return true;
            }
        }

        return false;
    }

    private void InitializeNuclearWarheadVisual()
    {
        if (nuclearWarheadRoot != null)
        {
            return;
        }

        nuclearWarheadRoot = new GameObject("NuclearWarhead");
        nuclearWarheadRoot.transform.SetParent(worldRoot != null ? worldRoot : projectileRoot, false);
        nuclearWarheadBody = CreateNuclearWarheadBody(nuclearWarheadRoot.transform);
        nuclearWarheadRoot.SetActive(false);
    }

    private Transform CreateNuclearWarheadBody(Transform parent)
    {
        var assemblyRoot = new GameObject("WarheadMesh");
        assemblyRoot.transform.SetParent(parent, false);
        assemblyRoot.transform.localPosition = Vector3.zero;
        assemblyRoot.transform.localRotation = Quaternion.identity;

        if (nuclearMissilePrototype != null)
        {
            var visualRoot = new GameObject("MissileMesh");
            visualRoot.transform.SetParent(assemblyRoot.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            GameObject instance = Instantiate(nuclearMissilePrototype, visualRoot.transform, false);
            instance.name = "CruiseMissile";
            instance.hideFlags = HideFlags.None;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);
            ClearHideFlagsInHierarchy(instance);
            AlignNuclearMissileLongAxisToForward(visualRoot.transform, instance);
            ApplyNuclearMissileMaterialsIfNeeded(instance);
            EnableNuclearMissileRenderers(instance);
        }
        else
        {
            Debug.LogError("[Nuclear] 无导弹模型，仅显示尾迹。请运行 tools/import-nuclear-warhead.ps1");
        }

        FitNuclearWarheadMesh(assemblyRoot);
        EnsureNuclearWarheadMinimumVisibleSize(assemblyRoot);
        EnsureNuclearWarheadFlightLight(assemblyRoot.transform);
        return assemblyRoot.transform;
    }

    private GameObject CreateNuclearMissileMeshPrototype()
    {
        Mesh bestMesh = null;
        int bestVertexCount = 0;
        for (int i = 0; i < NuclearMissileResourceCandidates.Length; i++)
        {
            UnityEngine.Object[] assets = Resources.LoadAll(NuclearMissileResourceCandidates[i]);
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

        var root = new GameObject("NuclearMissile_MeshTemplate");
        root.transform.SetParent(modelCacheRoot, false);
        root.SetActive(false);
        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = bestMesh;
        var renderer = root.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        ApplyNuclearMissileMaterialsForce(root);
        Debug.Log($"[Nuclear] Built mesh template ({bestVertexCount} verts, mesh={bestMesh.name}).");
        return root;
    }

    private static GameObject LoadNuclearMissilePrefabRoot()
    {
        for (int i = 0; i < NuclearMissileResourceCandidates.Length; i++)
        {
            string resourcePath = NuclearMissileResourceCandidates[i];
            GameObject source = Resources.Load<GameObject>(resourcePath);
            if (source != null && source.GetComponentsInChildren<Renderer>(true).Length > 0)
            {
                return source;
            }

            UnityEngine.Object[] gameObjects = Resources.LoadAll(resourcePath, typeof(GameObject));
            GameObject best = PickBestRenderableRoot(gameObjects);
            if (best != null)
            {
                return best;
            }
        }

        return null;
    }

    private static GameObject PickBestRenderableRoot(UnityEngine.Object[] assets)
    {
        GameObject best = null;
        int bestRendererCount = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not GameObject gameObject)
            {
                continue;
            }

            int rendererCount = gameObject.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount > bestRendererCount)
            {
                best = gameObject;
                bestRendererCount = rendererCount;
            }
        }

        return best;
    }

    private static void ClearHideFlagsInHierarchy(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.hideFlags = HideFlags.None;
        }
    }

    private static void AlignNuclearMissileLongAxisToForward(Transform visualRoot, GameObject model)
    {
        if (visualRoot == null || model == null)
        {
            return;
        }

        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            visualRoot.localRotation = Quaternion.Euler(-90f, 0f, 0f);
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

        Quaternion correction = Quaternion.identity;
        if (axis == 1)
        {
            correction = Quaternion.Euler(-90f, 0f, 0f);
        }
        else if (axis == 0)
        {
            correction = Quaternion.Euler(0f, 90f, 0f);
        }

        visualRoot.localRotation = correction * Quaternion.Euler(NuclearWarheadCameraTiltDegrees, 0f, 0f);
    }

    private static void EnableNuclearMissileRenderers(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.forceRenderingOff = false;
        }
    }

    private void EnsureNuclearWarheadFlightLight(Transform assemblyRoot)
    {
        if (nuclearWarheadFlightLight != null)
        {
            return;
        }

        var lightObject = new GameObject("FlightLight");
        lightObject.transform.SetParent(assemblyRoot, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.5f, NuclearWarheadTargetLength * 0.15f);
        nuclearWarheadFlightLight = lightObject.AddComponent<Light>();
        nuclearWarheadFlightLight.type = LightType.Point;
        nuclearWarheadFlightLight.range = 10f;
        nuclearWarheadFlightLight.intensity = 1.4f;
        nuclearWarheadFlightLight.color = new Color(1f, 0.78f, 0.5f, 1f);
        nuclearWarheadFlightLight.shadows = LightShadows.None;
        nuclearWarheadFlightLight.enabled = false;
    }

    private Material GetNuclearMissileMaterial()
    {
        if (nuclearMissileMaterial != null)
        {
            return nuclearMissileMaterial;
        }

        for (int i = 0; i < NuclearMissileDiffuseCandidates.Length; i++)
        {
            Material textured = GetTexturedOpaqueMaterial(
                NuclearMissileDiffuseCandidates[i],
                new Color(0.78f, 0.78f, 0.76f, 1f),
                Vector2.one,
                0.22f);
            if (textured != null && textured.mainTexture != null)
            {
                nuclearMissileMaterial = textured;
                return nuclearMissileMaterial;
            }
        }

        nuclearMissileMaterial = GetOpaqueMaterial(new Color(0.45f, 0.46f, 0.48f, 1f));
        return nuclearMissileMaterial;
    }

    private static bool RendererHasMainTexture(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.mainTexture != null)
            {
                return true;
            }

            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyNuclearMissileMaterialsIfNeeded(GameObject instance)
    {
        Material fallbackMaterial = GetNuclearMissileMaterial();
        if (fallbackMaterial == null || instance == null)
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
            renderer.lightProbeUsage = LightProbeUsage.Off;
            if (RendererHasMainTexture(renderer))
            {
                Material[] existing = renderer.sharedMaterials;
                for (int m = 0; m < existing.Length; m++)
                {
                    ApplyOpaqueDoubleSided(existing[m]);
                }

                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = fallbackMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void ApplyNuclearMissileMaterialsForce(GameObject instance)
    {
        Material missileMaterial = GetNuclearMissileMaterial();
        if (missileMaterial == null || instance == null)
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
            renderer.lightProbeUsage = LightProbeUsage.Off;
            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = missileMaterial;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void FitNuclearWarheadMesh(GameObject model)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            model.transform.localScale = Vector3.one * NuclearWarheadTargetLength;
            return;
        }

        float length = Mathf.Max(0.05f, bounds.size.z, bounds.size.y, bounds.size.x);
        float scale = NuclearWarheadTargetLength / length;
        model.transform.localScale = Vector3.one * scale;
    }

    private void EnsureNuclearWarheadMinimumVisibleSize(GameObject model)
    {
        if (!TryGetCastleModuleLocalBounds(model, out Bounds bounds))
        {
            return;
        }

        float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxExtent >= NuclearWarheadMinVisibleLength)
        {
            return;
        }

        float boost = NuclearWarheadMinVisibleLength / Mathf.Max(0.05f, maxExtent);
        model.transform.localScale *= boost;
        Debug.LogWarning($"[Nuclear] Boosted missile scale x{boost:0.##} for visibility.");
    }

    private void BeginNuclearWarheadFlight()
    {
        InitializeNuclearWarheadVisual();

        nuclearWarheadFromX = HumanCastleGateX + NuclearWarheadLaunchForwardLogical;
        nuclearWarheadFromZ = HumanCastleCenterZ;
        nuclearWarheadToX = nuclearStrikeCenterX;
        nuclearWarheadToZ = nuclearStrikeCenterZ;

        float distance = Distance(nuclearWarheadFromX, nuclearWarheadFromZ, nuclearWarheadToX, nuclearWarheadToZ);
        nuclearWarheadFlightDuration = Mathf.Clamp(distance / NuclearWarheadFlightSpeed, NuclearWarheadMinFlightSeconds, NuclearWarheadMaxFlightSeconds);
        nuclearWarheadArcPeakExtra = Mathf.Clamp(distance * 0.032f + 7f, 9f, 24f);
        nuclearWarheadFlightProgress = 0f;
        nuclearWarheadTrailTimer = 0f;
        nuclearWarningRefreshTimer = 0f;
        nuclearStrikePhase = NuclearStrikePhase.InFlight;
        nuclearStrikeSequenceTimer = 1f;
        nuclearStrikeDetonated = false;

        nuclearWarheadRoot.SetActive(true);
        if (nuclearWarheadFlightLight != null)
        {
            nuclearWarheadFlightLight.enabled = true;
        }

        nuclearWarheadLastWorldPosition = SampleNuclearWarheadWorldPosition(0f);
        if (nuclearWarheadBody != null)
        {
            nuclearWarheadBody.position = nuclearWarheadLastWorldPosition;
            UpdateNuclearWarheadOrientation(0f);
        }

        PlayBattleEffect(BattleEffectId.ShellLaunchSmoke, nuclearWarheadFromX, nuclearWarheadFromZ, 0.55f, 3.6f, Quaternion.identity);
        PlayBattleAudio(BattleAudioCueId.ExplosionLarge, nuclearWarheadFromX, nuclearWarheadFromZ, 0.28f);
        RefreshNuclearStrikeWarningAtTarget();
    }

    private void UpdateNuclearWarheadFlight(float dt)
    {
        float previousProgress = nuclearWarheadFlightProgress;
        nuclearWarheadFlightProgress += dt / Mathf.Max(0.04f, nuclearWarheadFlightDuration);
        float t = Mathf.Clamp01(nuclearWarheadFlightProgress);
        Vector3 worldPosition = SampleNuclearWarheadWorldPosition(t);
        if (nuclearWarheadBody != null)
        {
            UpdateNuclearWarheadOrientation(Mathf.Max(previousProgress, t - NuclearOrientationSampleDelta));
            nuclearWarheadLastWorldPosition = worldPosition;
            nuclearWarheadBody.position = worldPosition;
        }

        nuclearWarheadTrailTimer -= dt;
        if (nuclearWarheadTrailTimer <= 0f)
        {
            nuclearWarheadTrailTimer = NuclearWarheadTrailInterval;
            Vector3 trailPos = worldPosition;
            if (nuclearWarheadBody != null)
            {
                trailPos -= nuclearWarheadBody.forward * (NuclearWarheadTargetLength * 0.35f);
            }

            PlayBattleEffect(BattleEffectId.BombDropTrail, trailPos, 0.42f, Quaternion.identity);
        }

        nuclearWarningRefreshTimer -= dt;
        if (nuclearWarningRefreshTimer <= 0f)
        {
            nuclearWarningRefreshTimer = NuclearWarningRefreshSeconds;
            RefreshNuclearStrikeWarningAtTarget();
        }

        if (t < 1f)
        {
            return;
        }

        if (!nuclearStrikeDetonated)
        {
            nuclearStrikeDetonated = true;
            if (nuclearWarheadRoot != null)
            {
                nuclearWarheadRoot.SetActive(false);
            }

            if (nuclearWarheadFlightLight != null)
            {
                nuclearWarheadFlightLight.enabled = false;
            }

            PlayBattleEffect(BattleEffectId.BombExplosion, nuclearWarheadToX, nuclearWarheadToZ, 0.42f, 1.2f, Quaternion.identity);
            DetonateScheduledNuclearStrike();
            nuclearStrikePhase = NuclearStrikePhase.PostDetonation;
            nuclearWarheadPostVfxTimer = NuclearDetonationVfxHoldSeconds;
        }
    }

    /// <summary>对称抛物线：水平匀速，竖直 4·H·t·(1-t) 叠加。</summary>
    private Vector3 SampleNuclearWarheadWorldPosition(float t)
    {
        float x = Mathf.Lerp(nuclearWarheadFromX, nuclearWarheadToX, t);
        float z = Mathf.Lerp(nuclearWarheadFromZ, nuclearWarheadToZ, t);
        float baseHeight = Mathf.Lerp(NuclearWarheadLaunchHeight, NuclearWarheadImpactHeight, t);
        float arc = 4f * nuclearWarheadArcPeakExtra * t * (1f - t);
        return ToWorldPoint(x, z, baseHeight + arc);
    }

    private void UpdateNuclearWarheadOrientation(float sampleT)
    {
        if (nuclearWarheadBody == null)
        {
            return;
        }

        float t0 = Mathf.Clamp01(sampleT);
        float t1 = Mathf.Clamp01(sampleT + NuclearOrientationSampleDelta);
        Vector3 from = SampleNuclearWarheadWorldPosition(t0);
        Vector3 to = SampleNuclearWarheadWorldPosition(t1);
        Vector3 direction = to - from;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = SampleNuclearWarheadWorldPosition(Mathf.Min(1f, t1 + NuclearOrientationSampleDelta)) - from;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
        nuclearWarheadBody.rotation = look;
    }

    private void RefreshNuclearStrikeWarningAtTarget()
    {
        PlayBattleEffect(BattleEffectId.NuclearStrikeWarning, nuclearStrikeCenterX, nuclearStrikeCenterZ, 0.08f, 1.85f, Quaternion.identity);
    }

    private void ResetNuclearStrikeSequence()
    {
        nuclearStrikePhase = NuclearStrikePhase.Idle;
        nuclearStrikeSequenceTimer = 0f;
        nuclearStrikeDetonated = false;
        nuclearWarheadFlightProgress = 0f;
        nuclearWarheadPostVfxTimer = 0f;
        if (nuclearWarheadFlightLight != null)
        {
            nuclearWarheadFlightLight.enabled = false;
        }

        if (nuclearWarheadRoot != null)
        {
            nuclearWarheadRoot.SetActive(false);
        }
    }
}
