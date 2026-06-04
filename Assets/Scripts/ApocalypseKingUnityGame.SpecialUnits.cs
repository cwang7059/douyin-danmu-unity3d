using System;
using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const float GiantRocketPreferMeleeDistance = 108f;
    private const float PterosaurRocketRangeBonus = 180f;
    private const float PterosaurSiegeStandoffX = 240f;
    /// <summary>开战後自城堡門前最多向前巡逻的逻辑距离，避免飞到中场列队。</summary>
    private const float PterosaurMaxAdvanceFromGateX = 95f;
    private const int PterosaurMassSpawnColumns = 5;
    private const float PterosaurMassSpawnSpacingX = 22f;
    private const float PterosaurMassSpawnSpacingZ = 26f * FormationWidthScale;

    private void LoadSpecialUnitPrototypes()
    {
        if (pterosaurPrototype != null)
        {
            Destroy(pterosaurPrototype);
            pterosaurPrototype = null;
        }

        if (rocketTruckPrototype != null)
        {
            Destroy(rocketTruckPrototype);
            rocketTruckPrototype = null;
        }

        pterosaurPrototype = TryLoadPterosaurPrototype();
        rocketTruckPrototype = TryLoadSpecialResourcePrototype(
            RocketTruckResourceModelPath,
            null,
            UnitKind.Tank,
            RocketTruckModelTargetHeight,
            "RocketTruck");

        if (pterosaurPrototype == null)
        {
            Debug.LogWarning("[ApocalypseKing] Pteranodon GLB not loaded; using primitive pterosaur fallback.");
            pterosaurPrototype = CreateFallbackPterosaurPrototype();
        }
        else
        {
            int rendererCount = pterosaurPrototype.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[ApocalypseKing] Pterosaur prototype ready: {pterosaurPrototype.name}, renderers={rendererCount}");
        }

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

    private bool SpecialUnitPrototypeIsVisible(GameObject prototype)
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

    private GameObject TryLoadPterosaurPrototype()
    {
        string[] candidates =
        {
            PterosaurPteranodonResourceModelPath,
            PterosaurResourceModelPath,
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject prototype = TryLoadSpecialResourcePrototype(
                candidates[i],
                null,
                UnitKind.Aircraft,
                PterosaurModelTargetHeight,
                "Pterosaur");
            if (prototype == null)
            {
                continue;
            }

            int rendererCount = prototype.GetComponentsInChildren<Renderer>(true).Length;
            if (SpecialUnitPrototypeIsVisible(prototype))
            {
                Debug.Log($"[ApocalypseKing] Pterosaur model: {candidates[i]} (renderers={rendererCount})");
                return prototype;
            }

            Debug.LogWarning($"[ApocalypseKing] Pterosaur rejected {candidates[i]}: renderers={rendererCount}");
            Destroy(prototype);
        }

        return null;
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

        Material bodyMat = GetOpaqueMaterial(new Color(0.42f, 0.40f, 0.36f, 1f));
        Material wingMat = GetOpaqueMaterial(new Color(0.34f, 0.33f, 0.30f, 1f));
        Material crestMat = GetOpaqueMaterial(new Color(0.52f, 0.48f, 0.42f, 1f));

        var body = CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform);
        body.transform.localScale = new Vector3(0.62f, 0.30f, 0.88f);
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

        var crest = CreatePrimitive(PrimitiveType.Cube, "Crest", root.transform);
        crest.transform.localScale = new Vector3(0.22f, 0.20f, 0.06f);
        crest.transform.localPosition = new Vector3(0.28f, 0.14f, 0f);
        crest.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        crest.GetComponent<Renderer>().sharedMaterial = crestMat;

        for (int side = -1; side <= 1; side += 2)
        {
            var wing = CreatePrimitive(PrimitiveType.Cube, side < 0 ? "Wing_L" : "Wing_R", root.transform);
            wing.transform.localScale = new Vector3(0.10f, 0.92f, 0.42f);
            wing.transform.localPosition = new Vector3(-0.02f, 0.05f, side * 0.38f);
            wing.transform.localRotation = Quaternion.Euler(8f, side * 22f, side * 6f);
            wing.GetComponent<Renderer>().sharedMaterial = wingMat;
        }

        var tail = CreatePrimitive(PrimitiveType.Cube, "Tail", root.transform);
        tail.transform.localScale = new Vector3(0.36f, 0.05f, 0.09f);
        tail.transform.localPosition = new Vector3(-0.40f, 0.02f, 0f);
        tail.GetComponent<Renderer>().sharedMaterial = wingMat;

        ApplyPterosaurAuthenticPresentation(root);
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

        Material body = GetOpaqueMaterial(new Color(0.42f, 0.40f, 0.36f, 1f));
        Material wing = GetOpaqueMaterial(new Color(0.34f, 0.33f, 0.30f, 1f));
        Material crest = GetOpaqueMaterial(new Color(0.52f, 0.48f, 0.42f, 1f));
        Material beak = GetOpaqueMaterial(new Color(0.58f, 0.54f, 0.48f, 1f));
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

    private void GetPterosaurMassSpawn(int unitIndex, out float x, out float z)
    {
        unitIndex = Mathf.Max(0, unitIndex);
        int col = unitIndex % PterosaurMassSpawnColumns;
        int row = unitIndex / PterosaurMassSpawnColumns;
        float anchorX = BeastCastleGateX - 14f;
        z = (col - (PterosaurMassSpawnColumns - 1) * 0.5f) * PterosaurMassSpawnSpacingZ;
        x = anchorX - row * PterosaurMassSpawnSpacingX;
    }

    private float ResolvePterosaurFormationDesiredX(BattleUnit unit, float formationX)
    {
        if (matchPhase != MatchPhase.Battle)
        {
            return formationX;
        }

        float minX = BeastCastleGateX - PterosaurMaxAdvanceFromGateX;
        if (!TryGetEnemyCastleSiegePoint(unit, out float siegeX, out _))
        {
            return Mathf.Max(minX, formationX);
        }

        float forwardLimitX = formationX - PterosaurMaxAdvanceFromGateX;
        float siegeApproachX = siegeX + PterosaurSiegeStandoffX;
        float desiredX = Mathf.Max(siegeApproachX, forwardLimitX);
        desiredX = Mathf.Min(formationX, desiredX);
        return Mathf.Max(minX, desiredX);
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
                PterosaurDefaultAltitude);
            unit.headingDegrees = DirectionYawDegrees(
                HumanCastleGateX - x,
                BeastCastleCenterZ - z,
                unit.headingDegrees);
            unit.facing = -1;
            unit.turretYawDegrees = unit.headingDegrees;
            DetachUnitModelInstance(unit);
            EnsureUnitModelAttached(unit);
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
        float nextX = unit.x;
        float nextZ = unit.z;

        if (engage && target != null)
        {
            float jitterZ = (Noise(unit.id * 0.31f + unit.rank * 1.7f) - 0.5f) * 14f;
            float desiredX = holdX + (Noise(unit.id * 0.29f + unit.rank) - 0.5f) * 12f;
            float desiredZ = holdZ + jitterZ;
            float step = unit.speed * dt * 0.55f;
            nextX = unit.x + Mathf.Sign(desiredX - unit.x) * Mathf.Min(step, Mathf.Abs(desiredX - unit.x));
            nextZ = unit.z + Mathf.Sign(desiredZ - unit.z) * Mathf.Min(step, Mathf.Abs(desiredZ - unit.z));

            if (unit.attackCooldown <= 0f)
            {
                PerformPterosaurRocketAttack(unit, target);
            }
        }
        else
        {
            Vector2 march = DirectionTo(unit.x, unit.z, holdX, holdZ, unit.headingDegrees);
            nextX = unit.x + march.x * unit.speed * dt;
            nextZ = unit.z + march.y * unit.speed * dt;
        }

        MoveUnitToAvoidingBuildings(unit, nextX, nextZ, unit.speed * dt * 1.2f);
        unit.x = Mathf.Clamp(unit.x, BeastCastleGateX - PterosaurMaxAdvanceFromGateX - 30f, BeastCastleGateX + 40f);

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

    private void PerformPterosaurRocketAttack(BattleUnit unit, BattleUnit target)
    {
        if (unit == null || target == null)
        {
            return;
        }

        unit.runtimeState = UnitRuntimeState.Attacking;
        unit.attackCooldown = unit.attackInterval * (0.95f + Noise(battleTime * 17f + unit.id) * 0.2f);
        unit.attackVisualTimer = 0.42f;

        Vector2 aim = DirectionTo(unit.x, unit.z, target.x, target.z, unit.headingDegrees);
        TryGetUnitBodyLaunchLogical(unit, out float launchX, out float launchZ, out float launchHeight);
        launchHeight = Mathf.Max(PterosaurDefaultAltitude, launchHeight);
        PlayBattleEffect(BattleEffectId.MuzzleTank, launchX, launchZ, launchHeight, 0.48f, RotationFromDirection(aim));
        PlayBattleAudio(BattleAudioCueId.TankShot, launchX, launchZ, launchHeight);
        float scaledDamage = ScaleOutgoingDamage(unit, target, unit.damage);
        SpawnProjectile(
            ProjectileKind.Rocket,
            ProjectileTarget.Human,
            launchX,
            launchZ,
            launchHeight,
            target.x,
            target.z,
            1.05f,
            scaledDamage,
            38f,
            540f,
            new Color(0.95f, 0.42f, 0.18f, 1f));
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
