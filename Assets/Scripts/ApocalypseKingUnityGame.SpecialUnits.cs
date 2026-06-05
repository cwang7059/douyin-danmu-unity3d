using System;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private const float GiantRocketPreferMeleeDistance = 108f;
    private const float PterosaurRocketRangeBonus = 180f;
    private const float PterosaurSiegeStandoffX = 240f;
    /// <summary>翼龙编队纵深：各行允许比出生点更靠近人族城堡的最大逻辑距离。</summary>
    private const float PterosaurFormationRankAdvanceX = 120f;
    private const int PterosaurMassSpawnColumns = 5;
    private const float PterosaurMassSpawnSpacingX = 52f;
    private const float PterosaurMassSpawnSpacingZ = 42f * FormationWidthScale;
    private const float PterosaurSpawnAltitudeRowStep = 2.2f;
    private const float PterosaurSpawnAltitudeColJitter = 0.65f;

    private void LoadSpecialUnitPrototypes()
    {
        if (pterosaurPrototype != null)
        {
            Destroy(pterosaurPrototype);
            pterosaurPrototype = null;
        }

        if (pterosaurVisibilityFallbackPrototype != null)
        {
            Destroy(pterosaurVisibilityFallbackPrototype);
            pterosaurVisibilityFallbackPrototype = null;
        }

        if (rocketTruckPrototype != null)
        {
            Destroy(rocketTruckPrototype);
            rocketTruckPrototype = null;
        }

        SetupPterosaurBattlePrototype();
        rocketTruckPrototype = TryLoadSpecialResourcePrototype(
            RocketTruckResourceModelPath,
            null,
            UnitKind.Tank,
            RocketTruckModelTargetHeight,
            "RocketTruck");

        if (rocketTruckPrototype == null || !SpecialUnitPrototypeIsVisible(rocketTruckPrototype))
        {
            if (rocketTruckPrototype != null)
            {
                Destroy(rocketTruckPrototype);
            }

            rocketTruckPrototype = CreateFallbackRocketTruckPrototype();
        }

        Debug.Log($"[ApocalypseKing] Special units: Pterosaur={pterosaurPrototype?.name}, RocketTruck={rocketTruckPrototype?.name}");
    }

    private static bool SpecialUnitPrototypeIsVisible(GameObject prototype)
    {
        if (prototype == null)
        {
            return false;
        }

        int rendererCount = prototype.GetComponentsInChildren<Renderer>(true).Length;
        if (rendererCount == 0)
        {
            return false;
        }

        if (!TryComputeModelBounds(prototype, out Bounds bounds))
        {
            return rendererCount >= 1;
        }

        float span = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        return span >= 0.2f;
    }

    /// <summary>
    /// 优先 GLB（URP 材质重映射）；失败则用低模，且运行时不再覆盖为 GLTF 洋红材质。
    /// </summary>
    private void SetupPterosaurBattlePrototype()
    {
        if (pterosaurPrototype != null && pterosaurPrototype != pterosaurVisibilityFallbackPrototype)
        {
            Destroy(pterosaurPrototype);
        }

        pterosaurVisibilityFallbackPrototype = CreateFallbackPterosaurPrototype();
        pterosaurVisibilityFallbackPrototype.name = "Pterosaur_VisibilityFallback";

        string[] clipSources =
        {
            PterosaurPteranodonResourceModelPath,
            PterosaurResourceModelPath,
        };

        GameObject display = null;
        string displaySource = null;
        for (int i = 0; i < clipSources.Length; i++)
        {
            display = TryCreatePterosaurGlbDisplayPrototype(clipSources[i]);
            if (display != null)
            {
                displaySource = clipSources[i];
                break;
            }
        }

        bool usingGlb = display != null;
        pterosaurPrototype = usingGlb ? display : pterosaurVisibilityFallbackPrototype;
        pterosaurPrototype.name = usingGlb ? "Pterosaur_GlbDisplay" : "Pterosaur_BattleDisplay";

        int clipCount = 0;
        string clipSourcePath = null;
        for (int i = 0; i < clipSources.Length; i++)
        {
            AttachPterosaurResourceAnimationClips(pterosaurPrototype, clipSources[i]);
            AnimationClip[] clips = CollectRuntimeAnimationClips(pterosaurPrototype);
            if (clips.Length > clipCount)
            {
                clipCount = clips.Length;
                clipSourcePath = clipSources[i];
            }
        }

        int displayRenderers = pterosaurPrototype.GetComponentsInChildren<Renderer>(true).Length;
        Debug.Log(
            $"[ApocalypseKing] Pterosaur display={(usingGlb ? "GLB" : "procedural")} source={displaySource ?? "fallback"}, "
            + $"renderers={displayRenderers}, clips={clipCount} from {clipSourcePath ?? "none"}, "
            + $"{DescribePterosaurBounds(pterosaurPrototype)}");
    }

    private GameObject TryCreatePterosaurGlbDisplayPrototype(string resourcePath)
    {
        GameObject model = TryInstantiatePterosaurResourcePrototype(resourcePath);
        if (model == null)
        {
            return null;
        }

            PreparePterosaurGlbBattleDisplay(model, resourcePath);
        if (!PterosaurRuntimeModelIsVisible(model) || !PterosaurModelWorldSpanLooksValid(model))
        {
            Debug.LogWarning(
                $"[ApocalypseKing] Pterosaur GLB rejected: {resourcePath}, "
                + $"renderers={model.GetComponentsInChildren<Renderer>(true).Length}, "
                + $"{DescribePterosaurBounds(model)}");
            Destroy(model);
            return null;
        }

        return model;
    }

    private void PreparePterosaurGlbBattleDisplay(GameObject model, string resourcePath)
    {
        if (model == null)
        {
            return;
        }

        bool wasActive = model.activeSelf;
        if (!wasActive)
        {
            model.SetActive(true);
        }

        StripImportedModelStrayComponents(model);
        RemoveSketchfabSceneExtras(model);
        ApplyPterosaurGltfTextures(model, resourcePath);
        RemapPterosaurImportedMaterials(model);

        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null)
            {
                continue;
            }

            animators[i].applyRootMotion = false;
            animators[i].enabled = false;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.updateWhenOffscreen = true;
            }
        }

        RuntimeAnimationClipStore clipStore = GetOrCreateAnimationClipStore(model);
        // Legacy 飞行动画会把 SkinnedMesh 压扁到不可见；保持绑定姿态 + 程序化扇翼。
        clipStore.UseLegacyBoneAnimation = false;

        FitPterosaurRuntimeModel(model);
        AlignAirUnitModelForCruise(model, UnitCombatVariant.Pterosaur);
        if (!wasActive)
        {
            model.SetActive(false);
        }
    }

    private GameObject TryInstantiatePterosaurResourcePrototype(string resourcePath)
    {
        GameObject prototype = TryInstantiateResourceModel(resourcePath, UnitKind.Aircraft, "Pterosaur");
        if (prototype == null)
        {
            return null;
        }

        AttachPterosaurResourceAnimationClips(prototype, resourcePath);
        ConfigureImportedPrototype(prototype, UnitKind.Aircraft);
        prototype.SetActive(false);
        return prototype;
    }

    private static void AttachPterosaurResourceAnimationClips(GameObject prototype, string resourcePath)
    {
        var clips = new System.Collections.Generic.List<AnimationClip>();
        AppendEmbeddedModelAnimationClips(clips, prototype);
        AppendResourceAnimationClips(clips, resourcePath);
        if (clips.Count == 0)
        {
            return;
        }

        var clipStore = GetOrCreateAnimationClipStore(prototype);
        clipStore.Clips = clips.ToArray();
        clipStore.AnimatorClips = CreateAnimatorCompatibleClips(clipStore.Clips);
        clipStore.AnimatorReady = clipStore.AnimatorClips.Length > 0;
    }

    private static int ScorePterosaurFlyAnimation(GameObject prototype, string resourcePath)
    {
        var clips = new System.Collections.Generic.List<AnimationClip>();
        AppendEmbeddedModelAnimationClips(clips, prototype);
        AppendResourceAnimationClips(clips, resourcePath);
        if (clips.Count == 0)
        {
            AnimationClip[] runtimeClips = CollectRuntimeAnimationClips(prototype);
            for (int i = 0; i < runtimeClips.Length; i++)
            {
                clips.Add(runtimeClips[i]);
            }
        }

        int score = 0;
        for (int i = 0; i < clips.Count; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string name = clip.name.ToLowerInvariant();
            if (name == "flying" || name == "fly")
            {
                score += 220;
            }
            else if (name.Contains("fly") || name.Contains("flap") || name.Contains("glide") || name.Contains("soar"))
            {
                score += 110;
            }
            else if (name.Contains("walk") || name.Contains("stand"))
            {
                score += 12;
            }
            else
            {
                score += 4;
            }
        }

        return score;
    }

    private GameObject TryLoadSpecialResourcePrototype(
        string primaryPath,
        string alternatePath,
        UnitKind kind,
        float targetHeight,
        string label)
    {
        GameObject prototype = TryInstantiateResourceModel(primaryPath, kind, label);
        if (prototype == null && !string.IsNullOrEmpty(alternatePath))
        {
            prototype = TryInstantiateResourceModel(alternatePath, kind, label + "_Alt");
        }

        if (prototype == null)
        {
            return null;
        }

        ConfigureImportedPrototype(prototype, kind);
        prototype.SetActive(false);
        return prototype;
    }

    private GameObject TryInstantiateResourceModel(string resourcePath, UnitKind kind, string label)
    {
        var source = LoadTankResourceModelRoot(resourcePath);
        if (source == null)
        {
            return null;
        }

        var prototype = Instantiate(source, modelCacheRoot, false);
        prototype.name = $"{label}_Prototype";
        prototype.hideFlags = HideFlags.HideInHierarchy;
        return prototype;
    }

    private GameObject CreateFallbackPterosaurPrototype()
    {
        var root = new GameObject("Pterosaur_Fallback");
        root.transform.SetParent(modelCacheRoot, false);
        root.hideFlags = HideFlags.HideInHierarchy;

        Material bodyMat = GetOpaqueMaterial(new Color(0.52f, 0.44f, 0.36f, 1f));
        Material wingMat = GetOpaqueMaterial(new Color(0.44f, 0.38f, 0.32f, 1f));
        Material crestMat = GetOpaqueMaterial(new Color(0.58f, 0.50f, 0.42f, 1f));

        var body = CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform);
        body.transform.localScale = new Vector3(0.58f, 0.28f, 0.82f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;

        var head = CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform);
        head.transform.localScale = new Vector3(0.26f, 0.20f, 0.22f);
        head.transform.localPosition = new Vector3(0.42f, 0.06f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = crestMat;

        var jaw = CreatePrimitive(PrimitiveType.Cube, "Beak", root.transform);
        jaw.transform.localScale = new Vector3(0.18f, 0.06f, 0.10f);
        jaw.transform.localPosition = new Vector3(0.52f, -0.02f, 0f);
        jaw.GetComponent<Renderer>().sharedMaterial = crestMat;

        var mouth = new GameObject("Mouth");
        mouth.transform.SetParent(root.transform, false);
        mouth.transform.localPosition = new Vector3(0.58f, 0.02f, 0f);

        var crest = CreatePrimitive(PrimitiveType.Cube, "Crest", root.transform);
        crest.transform.localScale = new Vector3(0.22f, 0.20f, 0.06f);
        crest.transform.localPosition = new Vector3(0.28f, 0.14f, 0f);
        crest.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        crest.GetComponent<Renderer>().sharedMaterial = crestMat;

        for (int side = -1; side <= 1; side += 2)
        {
            var wing = CreatePrimitive(PrimitiveType.Capsule, side < 0 ? "Wing_L" : "Wing_R", root.transform);
            wing.transform.localScale = new Vector3(0.06f, 0.48f, 0.22f);
            wing.transform.localPosition = new Vector3(-0.02f, 0.05f, side * 0.34f);
            wing.transform.localRotation = Quaternion.Euler(8f, side * 22f, side * 90f);
            wing.GetComponent<Renderer>().sharedMaterial = wingMat;
        }

        var tail = CreatePrimitive(PrimitiveType.Cube, "Tail", root.transform);
        tail.transform.localScale = new Vector3(0.36f, 0.05f, 0.09f);
        tail.transform.localPosition = new Vector3(-0.40f, 0.02f, 0f);
        tail.GetComponent<Renderer>().sharedMaterial = wingMat;

        NormalizePrototype(root, PterosaurModelTargetHeight, UnitKind.Aircraft);
        root.SetActive(false);
        return root;
    }

    private GameObject CreateFallbackRocketTruckPrototype()
    {
        var root = new GameObject("RocketTruck_Fallback");
        root.transform.SetParent(modelCacheRoot, false);
        root.hideFlags = HideFlags.HideInHierarchy;

        Material hullMat = GetOpaqueMaterial(new Color(0.30f, 0.38f, 0.26f, 1f));
        Material tubeMat = GetOpaqueMaterial(new Color(0.18f, 0.20f, 0.17f, 1f));

        var hull = CreatePrimitive(PrimitiveType.Cube, "Hull", root.transform);
        hull.transform.localScale = new Vector3(1.35f, 0.55f, 0.95f);
        hull.GetComponent<Renderer>().sharedMaterial = hullMat;

        var cab = CreatePrimitive(PrimitiveType.Cube, "Cab", root.transform);
        cab.transform.localScale = new Vector3(0.34f, 0.34f, 0.36f);
        cab.transform.localPosition = new Vector3(0.28f, 0.18f, 0f);
        cab.GetComponent<Renderer>().sharedMaterial = hullMat;

        for (int i = 0; i < 6; i++)
        {
            var tube = CreatePrimitive(PrimitiveType.Cylinder, $"RocketTube_{i}", root.transform);
            tube.transform.localScale = new Vector3(0.08f, 0.34f, 0.08f);
            tube.transform.localPosition = new Vector3(-0.12f + i * 0.09f, 0.52f, 0f);
            tube.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tube.GetComponent<Renderer>().sharedMaterial = tubeMat;
        }

        NormalizePrototype(root, RocketTruckModelTargetHeight, UnitKind.Tank);
        root.SetActive(false);
        return root;
    }

    private static bool PterosaurModelIsProceduralBattleMesh(GameObject model)
    {
        if (model == null)
        {
            return true;
        }

        string name = model.name;
        if (name.IndexOf("Fallback", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("BattleDisplay", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Visibility", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0;
    }

    private void ApplyPterosaurProceduralBattleMaterials(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Material bodyMat = GetOpaqueMaterial(new Color(0.52f, 0.44f, 0.36f, 1f));
        Material wingMat = GetOpaqueMaterial(new Color(0.44f, 0.38f, 0.32f, 1f));
        Material crestMat = GetOpaqueMaterial(new Color(0.58f, 0.50f, 0.42f, 1f));
        Material beakMat = GetOpaqueMaterial(new Color(0.48f, 0.42f, 0.36f, 1f));
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;
            bool isWing = partName.IndexOf("wing", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isCrest = partName.IndexOf("crest", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isBeak = partName.IndexOf("beak", StringComparison.OrdinalIgnoreCase) >= 0;
            Material target = isCrest ? crestMat : isBeak ? beakMat : isWing ? wingMat : bodyMat;
            Material[] materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = target;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void ApplyPterosaurAuthenticPresentation(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        if (UnitModelUsesAuthoredTextures(model))
        {
            RemapPterosaurImportedMaterials(model);
            return;
        }

        Material body = GetOpaqueMaterial(new Color(0.45f, 0.40f, 0.34f, 1f));
        Material wing = GetOpaqueMaterial(new Color(0.38f, 0.36f, 0.32f, 1f));
        Material crest = GetOpaqueMaterial(new Color(0.50f, 0.46f, 0.40f, 1f));
        Material beak = GetOpaqueMaterial(new Color(0.52f, 0.48f, 0.42f, 1f));
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;
            bool isWing = partName.IndexOf("wing", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isCrest = partName.IndexOf("crest", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isBeak = partName.IndexOf("beak", StringComparison.OrdinalIgnoreCase) >= 0
                || partName.IndexOf("skull", StringComparison.OrdinalIgnoreCase) >= 0;

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                Material source = materials[m];
                if (PterosaurMaterialUsesAuthoredTexture(source))
                {
                    materials[m] = CreatePterosaurNaturalMaterial(source);
                    continue;
                }

                materials[m] = isCrest ? crest : isBeak ? beak : isWing ? wing : body;
            }

            renderer.sharedMaterials = materials;
            for (int m = 0; m < materials.Length; m++)
            {
                ApplyOpaqueDoubleSided(materials[m]);
            }
        }
    }

    private static bool PterosaurMaterialUsesAuthoredTexture(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material.mainTexture != null)
        {
            return true;
        }

        return material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null;
    }

    private Material CreatePterosaurNaturalMaterial(Material source)
    {
        if (source == null)
        {
            return GetOpaqueMaterial(new Color(0.40f, 0.38f, 0.34f, 1f));
        }

        var tinted = new Material(source);
        Color baseColor = tinted.HasProperty("_BaseColor") ? tinted.GetColor("_BaseColor") : tinted.color;
        baseColor *= new Color(0.88f, 0.86f, 0.82f, 1f);
        if (tinted.HasProperty("_BaseColor"))
        {
            tinted.SetColor("_BaseColor", baseColor);
        }

        tinted.color = baseColor;
        ApplyOpaqueDoubleSided(tinted);
        return tinted;
    }

    private void ApplyRocketTruckPresentation(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (ModelHasEmbeddedTextures(model))
        {
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
                    ApplyOpaqueDoubleSided(materials[m]);
                }
            }

            return;
        }

        // 程序低模 / 无贴图 GLB：纯色军绿（勿用坦克贴图，易在 GLTF 材质上变黑）
        Material hullMat = GetOpaqueMaterial(new Color(0.32f, 0.40f, 0.26f, 1f));
        Material rackMat = GetOpaqueMaterial(new Color(0.24f, 0.28f, 0.20f, 1f));
        Material rubberMat = GetOpaqueMaterial(new Color(0.14f, 0.14f, 0.14f, 1f));

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;
            Material partMat = ResolveRocketTruckPartMaterial(partName, hullMat, rackMat, rubberMat);
            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                materials[m] = partMat;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static Material ResolveRocketTruckPartMaterial(
        string partName,
        Material hullMat,
        Material rackMat,
        Material rubberMat)
    {
        if (ContainsNameToken(partName, "Wheel")
            || ContainsNameToken(partName, "tyre")
            || ContainsNameToken(partName, "tire")
            || ContainsNameToken(partName, "Stabilizer"))
        {
            return rubberMat;
        }

        if (ContainsNameToken(partName, "Tube")
            || ContainsNameToken(partName, "Launcher")
            || ContainsNameToken(partName, "Rocket"))
        {
            return rackMat;
        }

        return hullMat;
    }

    private static bool PterosaurPrototypeIsRenderable(GameObject prototype)
    {
        if (!SpecialUnitPrototypeIsVisible(prototype))
        {
            return false;
        }

        if (!TryComputeModelBounds(prototype, out Bounds bounds))
        {
            return false;
        }

        Vector3 size = bounds.size;
        float span = Mathf.Max(size.x, size.y, size.z);
        return span >= 0.25f;
    }

    private void GetPterosaurMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % PterosaurMassSpawnColumns;
        int row = unitIndex / PterosaurMassSpawnColumns;
        // 两军中线略偏兽族一侧，默认镜头内可见（与 SpecialUnitBattleCenterX 一致）
        float midfieldX = (HumanCastleGateX + BeastCastleGateX) * 0.5f;
        float anchorX = midfieldX + SpecialUnitBattleCenterX;
        float anchorZ = BeastCastleCenterZ;
        z = anchorZ + (col - (PterosaurMassSpawnColumns - 1) * 0.5f) * PterosaurMassSpawnSpacingZ;
        x = anchorX - row * PterosaurMassSpawnSpacingX;
    }

    private static float PterosaurPatrolSpanX()
    {
        float span = BeastCastleGateX - HumanCastleGateX - CastleSiegeStandoffX - PterosaurSiegeStandoffX - 40f;
        return Mathf.Max(600f, span);
    }

    private static float PterosaurMinAdvanceX()
    {
        return BeastCastleGateX - PterosaurPatrolSpanX();
    }

    private float ResolvePterosaurFormationDesiredX(BattleUnit unit, float formationX)
    {
        if (matchPhase != MatchPhase.Battle)
        {
            return formationX;
        }

        float rearLimitX = formationX;
        float minAdvanceX = PterosaurMinAdvanceX();
        float rankAdvanceX = formationX - PterosaurFormationRankAdvanceX;
        if (!TryGetEnemyCastleSiegePoint(unit, out float siegeX, out _))
        {
            return Mathf.Clamp(rankAdvanceX, minAdvanceX, rearLimitX);
        }

        float siegeApproachX = siegeX + PterosaurSiegeStandoffX;
        float desiredX = Mathf.Lerp(formationX, siegeApproachX, 0.22f);
        return Mathf.Clamp(desiredX, minAdvanceX, rearLimitX);
    }

    private static float GetPterosaurSpawnAltitude(int unitIndex)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % PterosaurMassSpawnColumns;
        int row = unitIndex / PterosaurMassSpawnColumns;
        return PterosaurDefaultAltitude
            + row * PterosaurSpawnAltitudeRowStep
            + (col % 3) * PterosaurSpawnAltitudeColJitter;
    }

    private const int RocketTruckMassSpawnColumns = 9;
    private const int RocketTruckMassSpawnRows = 2;
    private const float RocketTruckMassSpawnSpacingX = 64f;
    private const float RocketTruckMassSpawnSpacingZ = 48f * FormationWidthScale;

    private void GetHumanRocketTruckMassSpawn(int truckIndex, out float x, out float z)
    {
        truckIndex = Mathf.Max(0, truckIndex);
        int col = truckIndex % RocketTruckMassSpawnColumns;
        int row = truckIndex / RocketTruckMassSpawnColumns;
        row = Mathf.Clamp(row, 0, RocketTruckMassSpawnRows - 1);
        float anchorX = HumanTankFormationRearSpawnX() - RocketTruckBehindTankGapX - 26f;
        z = (col - (RocketTruckMassSpawnColumns - 1) * 0.5f) * RocketTruckMassSpawnSpacingZ;
        x = anchorX - row * RocketTruckMassSpawnSpacingX;
    }

    private const int RocketGiantMassSpawnColumns = 10;
    private const float RocketGiantMassSpawnSpacingX = 48f;
    private const float RocketGiantMassSpawnSpacingZ = 40f * FormationWidthScale;

    private static float RocketGiantMassSpawnAnchorX()
    {
        return BeastCastleSpawnExitX() - 14f;
    }

    private static float RocketGiantFormationFrontSpawnX()
    {
        int rows = (RocketGiantCount + RocketGiantMassSpawnColumns - 1) / RocketGiantMassSpawnColumns;
        return RocketGiantMassSpawnAnchorX() - Mathf.Max(0, rows - 1) * RocketGiantMassSpawnSpacingX;
    }

    private void GetRocketGiantMassSpawn(int rocketIndex, out float x, out float z)
    {
        rocketIndex = Mathf.Max(0, rocketIndex);
        int col = rocketIndex % RocketGiantMassSpawnColumns;
        int row = rocketIndex / RocketGiantMassSpawnColumns;
        float anchorX = RocketGiantMassSpawnAnchorX();
        z = (col - (RocketGiantMassSpawnColumns - 1) * 0.5f) * RocketGiantMassSpawnSpacingZ;
        x = anchorX - row * RocketGiantMassSpawnSpacingX;
    }

    private bool TryGetRocketGiantFormationSpawn(BattleUnit giant, out float spawnX, out float spawnZ)
    {
        spawnX = 0f;
        spawnZ = 0f;
        if (giant == null
            || giant.combatVariant != UnitCombatVariant.RocketGiant
            || giant.rank < BaseGiantCount)
        {
            return false;
        }

        GetRocketGiantMassSpawn(giant.rank - BaseGiantCount, out spawnX, out spawnZ);
        return true;
    }

    private void ResetRocketGiants()
    {
        for (int i = 0; i < RocketGiantCount; i++)
        {
            int giantIndex = BaseGiantCount + i;
            if (giantIndex >= giants.Count)
            {
                break;
            }

            var giant = giants[giantIndex];
            giant.combatVariant = UnitCombatVariant.RocketGiant;
            GetRocketGiantMassSpawn(i, out float x, out float z);
            float range = giantConfig.AttackRange + 240f;
            ActivateUnit(
                giant,
                x,
                z,
                giantConfig.MaxHp * 1.05f,
                giantConfig.Damage * 1.2f,
                giantConfig.MoveSpeed + Noise(i + 811f) * 5f,
                giantConfig.Radius * 1.08f,
                range,
                giantConfig.AttackInterval * 1.02f + Noise(i + 911f) * 0.12f,
                BaseGiantCount + i,
                -1,
                0f);
            giant.attackCooldown = 1.1f + Noise(i + 1011f);
            EnsureUnitModelAttached(giant);
            if (giant.modelInstance != null)
            {
                AttachGiantRocketLauncher(giant.modelInstance);
            }
        }
    }

    private void ResetPterosaurs()
    {
        for (int i = 0; i < pterosaurs.Count; i++)
        {
            if (i >= PterosaurCount)
            {
                DeactivatePooledUnit(pterosaurs[i]);
                continue;
            }

            var unit = pterosaurs[i];
            unit.combatVariant = UnitCombatVariant.Pterosaur;
            unit.team = TeamKind.Giant;
            unit.faction = FactionId.Zombie;
            GetPterosaurMassSpawn(i, out float x, out float z);
            unit.baseZ = z;
            float hp = giantConfig != null ? giantConfig.MaxHp * 0.42f : 980f;
            float damage = giantConfig != null ? giantConfig.Damage * 1.05f : 72f;
            float speed = aircraftConfig != null ? aircraftConfig.MoveSpeed * 0.88f + i * 5f : 88f;
            float range = (aircraftConfig != null ? aircraftConfig.AttackRange : 420f) + PterosaurRocketRangeBonus;
            DetachUnitModelInstance(unit);
            ActivateUnit(
                unit,
                x,
                z,
                hp,
                damage,
                speed,
                aircraftConfig != null ? aircraftConfig.Radius * 0.9f : 28f,
                range,
                (aircraftConfig != null ? aircraftConfig.AttackInterval : 1.4f) + i * 0.08f,
                i,
                -1,
                GetPterosaurSpawnAltitude(i));
            unit.headingDegrees = DirectionYawDegrees(
                HumanCastleGateX - x,
                BeastCastleCenterZ - z,
                unit.headingDegrees);
            unit.facing = -1;
            unit.turretYawDegrees = unit.headingDegrees;
            EnsureUnitModelAttached(unit);
            EnsurePterosaurUnitDisplay(unit);
        }

        int active = 0;
        int withModel = 0;
        for (int i = 0; i < pterosaurs.Count && i < PterosaurCount; i++)
        {
            if (pterosaurs[i] != null && pterosaurs[i].active)
            {
                active++;
                if (pterosaurs[i].modelInstance != null)
                {
                    withModel++;
                }
            }
        }

        if (pterosaurs.Count > 0 && pterosaurs[0] != null && pterosaurs[0].active)
        {
            GetPterosaurMassSpawn(0, out float sampleX, out float sampleZ);
            Debug.Log(
                $"[ApocalypseKing] Pterosaurs spawned: active={active}, withModel={withModel}/{PterosaurCount}, "
                + $"sample=({sampleX:F0},{sampleZ:F0}) alt={pterosaurs[0].altitude:F1} (beast front air)");
        }
        else
        {
            Debug.Log($"[ApocalypseKing] Pterosaurs spawned: active={active}, withModel={withModel}/{PterosaurCount}");
        }
    }

    private void UpdatePterosaurs(float dt)
    {
        for (int i = 0; i < pterosaurs.Count; i++)
        {
            UpdatePterosaurUnit(pterosaurs[i], dt);
        }
    }

    private void UpdatePterosaurUnit(BattleUnit unit, float dt)
    {
        if (unit == null || !unit.active)
        {
            return;
        }

        unit.animTimer += dt;
        unit.attackCooldown = Mathf.Max(0f, unit.attackCooldown - dt);
        unit.attackVisualTimer = Mathf.Max(0f, unit.attackVisualTimer - dt);

        if (unit.animTimer < 2.5f && ((int)(unit.animTimer * 10f)) % 7 == 0)
        {
            RefreshPterosaurUnitDisplay(unit);
        }

        GetPterosaurMassSpawn(unit.rank, out float formationX, out float formationZ);
        float phase = battleTime > 0.01f ? battleTime : unit.animTimer;
        float holdZ = formationZ + Mathf.Sin(phase * 1.6f + unit.seed * 5f) * 6f;

        if (matchPhase != MatchPhase.Battle)
        {
            unit.x = formationX;
            unit.z = formationZ;
            unit.baseZ = formationZ;
            unit.moveSpeed = 0f;
            RefreshRuntimeStateFromMovement(unit);
            UpdateUnitTransform(unit, dt);
            return;
        }

        var target = FindNearestHumanAirEnemy(unit);
        bool engage = target != null
            && DistanceSq(unit.x, unit.z, target.x, target.z) <= (unit.attackRange + target.radius * 0.55f) * (unit.attackRange + target.radius * 0.55f);

        float previousX = unit.x;
        float previousZ = unit.z;
        float holdX = ResolvePterosaurFormationDesiredX(unit, formationX);
        float holdAltitude = GetPterosaurSpawnAltitude(unit.rank);
        unit.altitude = Mathf.Lerp(unit.altitude, holdAltitude, Mathf.Clamp01(dt * 1.8f));
        float nextX = unit.x;
        float nextZ = unit.z;

        if (engage && target != null)
        {
            float jitterZ = (Noise(unit.id * 0.31f + unit.rank * 1.7f) - 0.5f) * 22f;
            float desiredX = holdX + (Noise(unit.id * 0.29f + unit.rank) - 0.5f) * 18f;
            float desiredZ = holdZ + jitterZ;
            float step = unit.speed * dt * 0.55f;
            nextX = unit.x + Mathf.Sign(desiredX - unit.x) * Mathf.Min(step, Mathf.Abs(desiredX - unit.x));
            nextZ = unit.z + Mathf.Sign(desiredZ - unit.z) * Mathf.Min(step, Mathf.Abs(desiredZ - unit.z));

            if (unit.attackCooldown <= 0f)
            {
                PerformPterosaurFireAttack(unit, target);
            }
        }
        else
        {
            Vector2 march = DirectionTo(unit.x, unit.z, holdX, holdZ, unit.headingDegrees);
            nextX = unit.x + march.x * unit.speed * dt;
            nextZ = unit.z + march.y * unit.speed * dt;
        }

        MoveUnitToAvoidingBuildings(unit, nextX, nextZ, unit.speed * dt * 1.2f);
        unit.x = Mathf.Clamp(unit.x, PterosaurMinAdvanceX() - 30f, BeastCastleGateX + 40f);

        if (target != null)
        {
            float aimYaw = DirectionYawDegrees(target.x - unit.x, target.z - unit.z, unit.headingDegrees);
            unit.headingDegrees = Mathf.LerpAngle(unit.headingDegrees, aimYaw, Mathf.Clamp01(dt * 4.5f));
        }

        unit.facing = unit.headingDegrees >= 0f ? 1 : -1;
        RecordUnitMovement(unit, previousX, previousZ, dt);
        RefreshRuntimeStateFromMovement(unit);
        UpdateUnitTransform(unit, dt);
    }

    private BattleUnit FindNearestHumanAirEnemy(BattleUnit origin)
    {
        BattleUnit best = null;
        float bestScore = float.PositiveInfinity;
        ConsiderEnemyPool(soldiers, origin, true, ref best, ref bestScore);
        ConsiderEnemyPool(tanks, origin, true, ref best, ref bestScore);
        ConsiderEnemyPool(aircraft, origin, true, ref best, ref bestScore);
        return best;
    }

    private static readonly string[] PterosaurMouthBoneHints =
    {
        "mouth", "jaw", "beak", "snout", "mandible", "head", "neck",
    };

    private static Transform FindPterosaurMouthTransform(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < PterosaurMouthBoneHints.Length; i++)
        {
            Transform hit = FindChildTransformByNameHint(root, PterosaurMouthBoneHints[i]);
            if (hit != null)
            {
                return hit;
            }
        }

        return null;
    }

    private static Transform FindChildTransformByNameHint(Transform root, string hint)
    {
        if (root == null || string.IsNullOrEmpty(hint))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child;
            }

            Transform nested = FindChildTransformByNameHint(child, hint);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private bool TryGetPterosaurMouthLaunchLogical(BattleUnit unit, Vector2 aim, out float x, out float z, out float height)
    {
        x = unit.x;
        z = unit.z;
        height = Mathf.Max(PterosaurDefaultAltitude, unit.altitude);
        if (unit?.body == null)
        {
            return false;
        }

        float aimLen = aim.magnitude;
        Vector3 forward = aimLen > 0.001f
            ? new Vector3(aim.x / aimLen, 0f, aim.y / aimLen)
            : unit.body.forward;
        Vector3 launchWorld = unit.body.position
            + Vector3.up * (PterosaurModelTargetHeight * 0.2f)
            + forward * (PterosaurModelTargetHeight * 0.38f);

        if (unit.modelInstance != null)
        {
            Transform mouth = FindPterosaurMouthTransform(unit.modelInstance.transform);
            if (mouth != null)
            {
                launchWorld = mouth.position + mouth.forward * 0.12f + Vector3.up * 0.04f;
            }
        }

        x = launchWorld.x / LogicalToWorld;
        z = launchWorld.z / LogicalToWorld;
        height = launchWorld.y - SampleBattlefieldGroundHeightWorld(launchWorld.x, launchWorld.z);
        return true;
    }

    private void PerformPterosaurFireAttack(BattleUnit unit, BattleUnit target)
    {
        if (unit == null || target == null)
        {
            return;
        }

        unit.runtimeState = UnitRuntimeState.Attacking;
        unit.attackCooldown = unit.attackInterval * (0.95f + Noise(battleTime * 17f + unit.id) * 0.2f);
        unit.attackVisualTimer = 0.55f;

        Vector2 aim = DirectionTo(unit.x, unit.z, target.x, target.z, unit.headingDegrees);
        TryGetPterosaurMouthLaunchLogical(unit, aim, out float launchX, out float launchZ, out float launchHeight);
        launchHeight = Mathf.Max(PterosaurDefaultAltitude * 0.85f, launchHeight);
        PlayBattleEffect(
            BattleEffectId.PterosaurFireballMuzzle,
            launchX,
            launchZ,
            launchHeight,
            0.36f,
            RotationFromDirection(aim));
        PlayBattleAudio(BattleAudioCueId.OrcSkill, launchX, launchZ, launchHeight);
        float scaledDamage = ScaleOutgoingDamage(unit, target, unit.damage);
        SpawnProjectile(
            ProjectileKind.Fireball,
            ProjectileTarget.Human,
            launchX,
            launchZ,
            launchHeight,
            target.x,
            target.z,
            Mathf.Max(1.2f, target.altitude * 0.55f),
            scaledDamage,
            PterosaurFireballHitRadius,
            PterosaurFireballFlightSpeed,
            PterosaurFireProjectileColor,
            target.id);
    }

    private void PerformGiantRocketAttack(BattleUnit giant, BattleUnit target)
    {
        if (giant == null || target == null)
        {
            return;
        }

        giant.runtimeState = UnitRuntimeState.Attacking;
        giant.attackCooldown = giant.attackInterval * 1.08f;
        giant.attackVisualTimer = 0.52f;

        Vector2 aim = DirectionTo(giant.x, giant.z, target.x, target.z, giant.headingDegrees);
        float launchX = giant.x - aim.x * 18f;
        float launchZ = giant.z - aim.y * 18f;
        float launchHeight = target.kind == UnitKind.Aircraft ? Mathf.Max(2.8f, target.altitude * 0.85f) : 1.35f;
        PlayBattleEffect(BattleEffectId.MuzzleTank, launchX, launchZ, launchHeight, 0.52f, RotationFromDirection(aim));
        PlayBattleEffect(BattleEffectId.ShellLaunchSmoke, launchX, launchZ, launchHeight, 0.38f, RotationFromDirection(aim));
        PlayBattleAudio(BattleAudioCueId.TankShot, launchX, launchZ, launchHeight);
        float scaledDamage = ScaleOutgoingDamage(giant, target, giant.damage * 1.18f);
        SpawnProjectile(
            ProjectileKind.Rocket,
            ProjectileTarget.Human,
            launchX,
            launchZ,
            launchHeight,
            target.x,
            target.z,
            1.12f,
            scaledDamage,
            44f,
            500f,
            new Color(0.92f, 0.38f, 0.14f, 1f));
        ShowBanner(target.kind == UnitKind.Aircraft ? "翼龙火箭" : "火箭丧尸", true, 0.75f);
    }

    private void AttachGiantRocketLauncher(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Transform existing = model.transform.Find("GiantRocketLauncher");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        var launcher = new GameObject("GiantRocketLauncher");
        launcher.transform.SetParent(model.transform, false);
        launcher.transform.localPosition = new Vector3(-0.08f, 1.05f, 0.28f);
        launcher.transform.localRotation = Quaternion.Euler(-12f, 90f, 0f);
        launcher.transform.localScale = Vector3.one * 1.35f;
        launcher.SetActive(false);

        Material tube = GetOpaqueMaterial(new Color(0.22f, 0.24f, 0.20f, 1f));
        Material warhead = GetOpaqueMaterial(new Color(0.92f, 0.38f, 0.12f, 1f));
        for (int i = 0; i < 2; i++)
        {
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = $"RocketTube_{i}";
            pipe.transform.SetParent(launcher.transform, false);
            pipe.transform.localScale = new Vector3(0.08f, 0.36f, 0.08f);
            pipe.transform.localPosition = new Vector3(-0.05f + i * 0.10f, 0.16f, 0.18f);
            pipe.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            pipe.GetComponent<Renderer>().sharedMaterial = tube;
            DestroyCollider(pipe);

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = $"RocketTip_{i}";
            tip.transform.SetParent(pipe.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            tip.transform.localScale = Vector3.one * 0.48f;
            tip.GetComponent<Renderer>().sharedMaterial = warhead;
            DestroyCollider(tip);
        }
    }

    private void SetGiantRocketLauncherVisible(BattleUnit giant, bool visible)
    {
        if (giant == null
            || giant.combatVariant != UnitCombatVariant.RocketGiant
            || giant.modelInstance == null)
        {
            return;
        }

        Transform launcher = giant.modelInstance.transform.Find("GiantRocketLauncher");
        if (launcher != null)
        {
            launcher.gameObject.SetActive(visible);
        }
    }
}
