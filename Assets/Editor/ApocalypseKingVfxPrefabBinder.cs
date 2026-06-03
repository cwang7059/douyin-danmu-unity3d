#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scans War FX / Cartoon FX Remaster (and related) prefabs and binds them to Battle EffectConfig assets.
/// Safe to run without store packages — only updates configs when a match is found.
/// </summary>
public static class ApocalypseKingVfxPrefabBinder
{
    private const string EffectsResourcesFolder = "Assets/Resources/Battle/Effects";

    private const string KenneyPrefabsRoot = "Assets/Kenney/Particle samples/Prefabs";

    private static readonly string[] VfxSearchRoots =
    {
        "Assets/ThirdParty",
        "Assets/Kenney",
        "Assets/JMO Assets",
        "Assets/JMO",
    };

    private static readonly KenneyPrefabBinding[] KenneyPrefabBindings =
    {
        new KenneyPrefabBinding(BattleEffectId.MuzzleRifle, "Sparks.prefab", 0.52f),
        new KenneyPrefabBinding(BattleEffectId.MuzzleTank, "Sparks.prefab", 0.30f),
        new KenneyPrefabBinding(BattleEffectId.MuzzleAircraft, "Sparks.prefab", 0.48f),
        new KenneyPrefabBinding(BattleEffectId.ShellLaunchSmoke, "Smoke.prefab", 0.45f),
        new KenneyPrefabBinding(BattleEffectId.BombDropTrail, "Smoke.prefab", 0.35f),
        new KenneyPrefabBinding(BattleEffectId.BulletHitMetal, "Sparks.prefab", 0.55f),
        new KenneyPrefabBinding(BattleEffectId.BulletHitDirt, "Smoke.prefab", 0.42f),
        new KenneyPrefabBinding(BattleEffectId.SoldierDeath, "Smoke.prefab", 0.48f),
        new KenneyPrefabBinding(BattleEffectId.ShellExplosionSmall, "Fire.prefab", 0.58f),
        new KenneyPrefabBinding(BattleEffectId.ExplosionSmall, "Fire.prefab", 0.58f),
        new KenneyPrefabBinding(BattleEffectId.ShellExplosionLarge, "Fire.prefab", 0.72f),
        new KenneyPrefabBinding(BattleEffectId.ExplosionLarge, "Fire.prefab", 0.72f),
        new KenneyPrefabBinding(BattleEffectId.BombExplosion, "Fire.prefab", 0.78f),
        new KenneyPrefabBinding(BattleEffectId.ShellImpactMonster, "Fire.prefab", 0.68f),
        new KenneyPrefabBinding(BattleEffectId.MonsterHammerImpact, "Fire.prefab", 0.65f),
        new KenneyPrefabBinding(BattleEffectId.TankDeathExplosion, "Fire.prefab", 0.75f),
        new KenneyPrefabBinding(BattleEffectId.AircraftDeathExplosion, "Fire.prefab", 0.75f),
        new KenneyPrefabBinding(BattleEffectId.AirCrashExplosion, "Fire.prefab", 0.75f),
        new KenneyPrefabBinding(BattleEffectId.TankWreckSmoke, "Smoke.prefab", 0.55f),
        new KenneyPrefabBinding(BattleEffectId.AircraftCrashSmoke, "Smoke.prefab", 0.50f),
        new KenneyPrefabBinding(BattleEffectId.MonsterDeathExplosion, "Fire.prefab", 0.82f),
        new KenneyPrefabBinding(BattleEffectId.MonsterDeathDust, "Smoke.prefab", 0.62f),
        new KenneyPrefabBinding(BattleEffectId.MonsterStompDust, "Smoke.prefab", 0.58f),
        new KenneyPrefabBinding(BattleEffectId.HumanSummon, "Magic.prefab", 0.72f),
        new KenneyPrefabBinding(BattleEffectId.OrcSummon, "Magic.prefab", 0.78f),
        new KenneyPrefabBinding(BattleEffectId.HumanAirStrikeWarning, "Electricity.prefab", 0.85f),
        new KenneyPrefabBinding(BattleEffectId.OrcRageBuff, "Fire.prefab", 0.80f),
        new KenneyPrefabBinding(BattleEffectId.ClawHit, "Sparks.prefab", 0.52f),
        new KenneyPrefabBinding(BattleEffectId.NuclearStrikeWarning, "Electricity.prefab", 0.95f),
        new KenneyPrefabBinding(BattleEffectId.NuclearDetonation, "Fire.prefab", 1.05f),
    };

    private static readonly NuclearVfxBinding[] NuclearVfxBindings =
    {
        new NuclearVfxBinding(
            BattleEffectId.NuclearDetonation,
            1.35f,
            new[]
            {
                "WFX_Nuke",
                "WFX_ExplosiveSmokeGround Big",
                "WFX_ExplosiveSmoke Big",
                "WFX_Explosion",
                "CFXR Explosion Smoke 2 Solo (HDR)",
                "CFXR Explosion 1",
                "CFXR2 WW Explosion",
                "CFXR3 Fire Explosion B",
                "WFX_Explosion Simple",
            },
            new VfxBindRule
            {
                Id = BattleEffectId.NuclearDetonation,
                PreferWarFx = true,
                PrefabScaleMultiplier = 1.35f,
                RequiredTokens = new[] { "explosion", "nuke" },
                OptionalTokens = new[] { "massive", "smoke", "ground", "hdr", "fire" },
                NegativeTokens = new[] { "muzzle", "small", "quick", "impact", "hit", "bullet", "mobile", "firework" },
                MinimumScore = 18,
            }),
        new NuclearVfxBinding(
            BattleEffectId.NuclearStrikeWarning,
            1.45f,
            new[]
            {
                "CFXR Explosion Smoke 2 Solo (HDR)",
                "WFX_Explosion Small",
                "CFXR2 Sparks Rain",
                "CFXR Explosion 1",
                "CFXR2 WW Explosion",
                "WFX_Explosion Simple",
            },
            new VfxBindRule
            {
                Id = BattleEffectId.NuclearStrikeWarning,
                PreferCfxr = true,
                PrefabScaleMultiplier = 1.45f,
                RequiredTokens = new[] { "explosion" },
                OptionalTokens = new[] { "sparks", "quick", "small", "warning", "ring" },
                NegativeTokens = new[] { "muzzle", "massive", "monster", "ground", "mobile" },
                MinimumScore = 14,
            }),
    };

    [MenuItem("Apocalypse King/Bind Store VFX Prefabs to Effect Configs")]
    public static void BindFromMenu()
    {
        int bound = TryBindAllEffectConfigs(logDetails: true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ApocalypseKing] VFX prefab bind complete. Updated {bound} EffectConfig(s). See doc/炫酷特效素材清单与接入指南.md");
    }

    [MenuItem("Apocalypse King/Validate Nuclear VFX Binding")]
    public static void ValidateNuclearFromMenu()
    {
        ValidateNuclearVfxBinding(logDetails: true);
    }

    /// <summary>Called from tools/bind-vfx-prefabs.ps1 in batchmode (Unity Editor must be closed).</summary>
    public static void BindAllForBatchMode()
    {
        int bound = TryBindAllEffectConfigs(logDetails: true);
        AssetDatabase.SaveAssets();
        bool nuclearOk = ValidateNuclearVfxBinding(logDetails: true);
        if (!nuclearOk)
        {
            Debug.LogWarning("[ApocalypseKing] Nuclear VFX binding validation failed. See log above.");
        }

        Debug.Log($"[ApocalypseKing] VFX bind batch complete. Updated {bound} EffectConfig(s). Nuclear OK={nuclearOk}.");
    }

    public static int TryBindAllEffectConfigs(bool logDetails = false)
    {
        int bound = BindKenneyPrefabs(logDetails);
        var prefabs = CollectCandidatePrefabs();
        bool hasStorePrefabs = prefabs.Count > 0;

        foreach (var rule in Rules)
        {
            if (rule.Id == BattleEffectId.None || rule.Id == BattleEffectId.BulletTracer)
            {
                continue;
            }

            if (IsNuclearEffectId(rule.Id))
            {
                continue;
            }

            string path = EffectsResourcesFolder + "/Effect_" + rule.Id + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EffectConfig>(path);
            if (config == null)
            {
                continue;
            }

            GameObject match = FindBestPrefab(prefabs, rule);
            if (match == null)
            {
                continue;
            }

            if (IsKenneyPrefab(config.prefab) && !IsStoreVfxPrefab(match))
            {
                continue;
            }

            if (config.prefab == match && Mathf.Approximately(config.prefabScaleMultiplier, rule.PrefabScaleMultiplier))
            {
                continue;
            }

            config.prefab = match;
            config.prefabScaleMultiplier = rule.PrefabScaleMultiplier;
            if (rule.AttachToParent)
            {
                config.attachToParent = true;
            }

            EditorUtility.SetDirty(config);
            bound++;
            if (logDetails)
            {
                Debug.Log($"[ApocalypseKing] {rule.Id} <- {AssetDatabase.GetAssetPath(match)} (scale x{rule.PrefabScaleMultiplier:0.##})");
            }
        }

        bound += BindNuclearVfxPrefabs(prefabs, logDetails);
        ValidateNuclearVfxBinding(logDetails);

        if (!hasStorePrefabs && bound == 0 && logDetails)
        {
            Debug.Log(
                "[ApocalypseKing] No store VFX prefabs found. Import War FX + Cartoon FX Remaster Free, then run Bind again.");
        }

        return bound;
    }

    public static bool ValidateNuclearVfxBinding(bool logDetails = false)
    {
        bool ok = true;
        for (int i = 0; i < NuclearVfxBindings.Length; i++)
        {
            NuclearVfxBinding entry = NuclearVfxBindings[i];
            string configPath = EffectsResourcesFolder + "/Effect_" + entry.Id + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EffectConfig>(configPath);
            if (config == null)
            {
                ok = false;
                if (logDetails)
                {
                    Debug.LogWarning($"[ApocalypseKing] Missing {configPath}");
                }

                continue;
            }

            if (config.prefab == null)
            {
                ok = false;
                if (logDetails)
                {
                    Debug.LogWarning(
                        $"[ApocalypseKing] {entry.Id} has no prefab — import War FX / Cartoon FX Remaster Free and run Bind Store VFX Prefabs.");
                }

                continue;
            }

            if (!config.prefab.GetComponentInChildren<ParticleSystem>(true))
            {
                ok = false;
                if (logDetails)
                {
                    Debug.LogWarning($"[ApocalypseKing] {entry.Id} prefab has no ParticleSystem: {AssetDatabase.GetAssetPath(config.prefab)}");
                }

                continue;
            }

            if (logDetails)
            {
                string source = IsStoreVfxPrefab(config.prefab) ? "Asset Store" : "Kenney fallback";
                Debug.Log(
                    $"[ApocalypseKing] {entry.Id} OK ({source}): {config.prefab.name} scale x{config.prefabScaleMultiplier:0.##}");
            }
        }

        return ok;
    }

    private static int BindNuclearVfxPrefabs(List<GameObject> storePrefabs, bool logDetails)
    {
        int bound = 0;
        for (int i = 0; i < NuclearVfxBindings.Length; i++)
        {
            NuclearVfxBinding entry = NuclearVfxBindings[i];
            GameObject match = FindPrefabByCandidateNames(entry.CandidatePrefabNames);
            if (match == null && storePrefabs.Count > 0)
            {
                match = FindBestPrefab(storePrefabs, entry.ScoreRule);
            }

            if (match == null)
            {
                continue;
            }

            string configPath = EffectsResourcesFolder + "/Effect_" + entry.Id + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EffectConfig>(configPath);
            if (config == null)
            {
                continue;
            }

            float scale = IsStoreVfxPrefab(match) ? entry.StoreScale : entry.StoreScale * 0.72f;
            if (config.prefab == match && Mathf.Approximately(config.prefabScaleMultiplier, scale))
            {
                continue;
            }

            config.prefab = match;
            config.prefabScaleMultiplier = scale;
            if (entry.Id == BattleEffectId.NuclearDetonation)
            {
                config.prewarmCount = Mathf.Max(config.prewarmCount, 4);
                config.maxCount = Mathf.Max(config.maxCount, 8);
            }

            EditorUtility.SetDirty(config);
            bound++;
            if (logDetails)
            {
                string kind = IsStoreVfxPrefab(match) ? "store" : "fallback";
                Debug.Log($"[ApocalypseKing] {entry.Id} <- {AssetDatabase.GetAssetPath(match)} ({kind}, scale x{scale:0.##})");
            }
        }

        return bound;
    }

    private static GameObject FindPrefabByCandidateNames(string[] preferredNames)
    {
        if (preferredNames == null || preferredNames.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < preferredNames.Length; i++)
        {
            string target = preferredNames[i];
            if (string.IsNullOrEmpty(target))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets(target + " t:Prefab");
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || !prefab.GetComponentInChildren<ParticleSystem>(true))
                {
                    continue;
                }

                if (string.Equals(prefab.name, target, StringComparison.OrdinalIgnoreCase))
                {
                    return prefab;
                }
            }
        }

        for (int r = 0; r < VfxSearchRoots.Length; r++)
        {
            if (!AssetDatabase.IsValidFolder(VfxSearchRoots[r]))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { VfxSearchRoots[r] });
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (!IsLikelyVfxPrefabPath(path))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                for (int i = 0; i < preferredNames.Length; i++)
                {
                    if (string.Equals(fileName, preferredNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                }
            }
        }

        return null;
    }

    private static bool IsNuclearEffectId(BattleEffectId id)
    {
        return id == BattleEffectId.NuclearDetonation || id == BattleEffectId.NuclearStrikeWarning;
    }

    private static int BindKenneyPrefabs(bool logDetails)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Kenney"))
        {
            return 0;
        }

        int bound = 0;
        for (int i = 0; i < KenneyPrefabBindings.Length; i++)
        {
            KenneyPrefabBinding entry = KenneyPrefabBindings[i];
            string path = KenneyPrefabsRoot + "/" + entry.PrefabFileName;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            string configPath = EffectsResourcesFolder + "/Effect_" + entry.Id + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EffectConfig>(configPath);
            if (config == null)
            {
                continue;
            }

            bool isNuclear = IsNuclearEffectId(entry.Id);
            if (isNuclear && config.prefab != null && IsStoreVfxPrefab(config.prefab))
            {
                continue;
            }

            if (config.prefab == prefab && Mathf.Approximately(config.prefabScaleMultiplier, entry.Scale))
            {
                continue;
            }

            config.prefab = prefab;
            config.prefabScaleMultiplier = entry.Scale;
            EditorUtility.SetDirty(config);
            bound++;
            if (logDetails)
            {
                Debug.Log($"[ApocalypseKing] {entry.Id} <- {path} (Kenney, scale x{entry.Scale:0.##})");
            }
        }

        return bound;
    }

    private static List<GameObject> CollectCandidatePrefabs()
    {
        var list = new List<GameObject>(256);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int r = 0; r < VfxSearchRoots.Length; r++)
        {
            if (!AssetDatabase.IsValidFolder(VfxSearchRoots[r]))
            {
                continue;
            }

            CollectPrefabsUnder(VfxSearchRoots[r], list, seen);
        }

        CollectPrefabsByNameFilter("Assets", "t:Prefab", list, seen, "WFX_", "CFXR", "CFX_");
        return list;
    }

    private static void CollectPrefabsUnder(string folder, List<GameObject> list, HashSet<string> seen)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!IsLikelyVfxPrefabPath(path))
            {
                continue;
            }

            AddPrefab(path, list, seen);
        }
    }

    private static void CollectPrefabsByNameFilter(
        string folder,
        string filter,
        List<GameObject> list,
        HashSet<string> seen,
        params string[] nameContains)
    {
        string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = Path.GetFileNameWithoutExtension(path);
            bool match = false;
            for (int n = 0; n < nameContains.Length; n++)
            {
                if (fileName.IndexOf(nameContains[n], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    match = true;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            AddPrefab(path, list, seen);
        }
    }

    private static void AddPrefab(string path, List<GameObject> list, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(path) || seen.Contains(path))
        {
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null || !prefab.GetComponentInChildren<ParticleSystem>(true))
        {
            return;
        }

        seen.Add(path);
        list.Add(prefab);
    }

    private static bool IsLikelyVfxPrefabPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string p = path.Replace('\\', '/');
        if (p.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        string name = Path.GetFileNameWithoutExtension(p);
        if (name.StartsWith("WFX_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("CFXR", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("CFX_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return p.IndexOf("WarFX", StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("Cartoon FX", StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("CartoonFX", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static GameObject FindBestPrefab(List<GameObject> prefabs, VfxBindRule rule)
    {
        GameObject best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < prefabs.Count; i++)
        {
            var prefab = prefabs[i];
            if (prefab == null)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            string name = prefab.name;
            int score = ScorePrefab(name, path, rule);
            if (score > bestScore)
            {
                bestScore = score;
                best = prefab;
            }
        }

        return bestScore >= rule.MinimumScore ? best : null;
    }

    private static int ScorePrefab(string name, string path, VfxBindRule rule)
    {
        int score = 0;
        string combined = (name + " " + path).ToLowerInvariant();

        if (rule.PreferWarFx && (name.StartsWith("WFX_", StringComparison.OrdinalIgnoreCase) || combined.Contains("warfx")))
        {
            score += 8;
        }

        if (rule.PreferCfxr && (name.StartsWith("CFXR", StringComparison.OrdinalIgnoreCase) || combined.Contains("cartoon fx")))
        {
            score += 8;
        }

        if (rule.PreferMobile && combined.Contains("mobile"))
        {
            score += 4;
        }

        if (rule.AvoidMobile && combined.Contains("mobile"))
        {
            score -= 12;
        }

        for (int i = 0; i < rule.RequiredTokens.Length; i++)
        {
            if (combined.Contains(rule.RequiredTokens[i]))
            {
                score += 14;
            }
            else
            {
                return int.MinValue / 4;
            }
        }

        for (int i = 0; i < rule.OptionalTokens.Length; i++)
        {
            if (combined.Contains(rule.OptionalTokens[i]))
            {
                score += 6;
            }
        }

        for (int i = 0; i < rule.NegativeTokens.Length; i++)
        {
            if (combined.Contains(rule.NegativeTokens[i]))
            {
                score -= 10;
            }
        }

        return score;
    }

    private static bool IsKenneyPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(prefab);
        return !string.IsNullOrEmpty(path) && path.Replace('\\', '/').IndexOf("Assets/Kenney/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsStoreVfxPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        string name = prefab.name;
        return name.StartsWith("WFX_", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("CFXR", StringComparison.OrdinalIgnoreCase);
    }

    private readonly struct KenneyPrefabBinding
    {
        public readonly BattleEffectId Id;
        public readonly string PrefabFileName;
        public readonly float Scale;

        public KenneyPrefabBinding(BattleEffectId id, string prefabFileName, float scale)
        {
            Id = id;
            PrefabFileName = prefabFileName;
            Scale = scale;
        }
    }

    private readonly struct NuclearVfxBinding
    {
        public readonly BattleEffectId Id;
        public readonly float StoreScale;
        public readonly string[] CandidatePrefabNames;
        public readonly VfxBindRule ScoreRule;

        public NuclearVfxBinding(BattleEffectId id, float storeScale, string[] candidatePrefabNames, VfxBindRule scoreRule)
        {
            Id = id;
            StoreScale = storeScale;
            CandidatePrefabNames = candidatePrefabNames ?? Array.Empty<string>();
            ScoreRule = scoreRule;
        }
    }

    private sealed class VfxBindRule
    {
        public BattleEffectId Id;
        public string[] RequiredTokens = Array.Empty<string>();
        public string[] OptionalTokens = Array.Empty<string>();
        public string[] NegativeTokens = Array.Empty<string>();
        public bool PreferWarFx;
        public bool PreferCfxr;
        public bool PreferMobile;
        public bool AvoidMobile = true;
        public bool AttachToParent;
        public float PrefabScaleMultiplier = 1f;
        public int MinimumScore = 10;
    }

    private static readonly VfxBindRule[] Rules =
    {
        Rule(BattleEffectId.MuzzleRifle, true, false, 0.42f, req: new[] { "muzzle" }, opt: new[] { "rifle", "flash", "small" }, neg: new[] { "tank", "explosion" }),
        Rule(BattleEffectId.MuzzleTank, true, false, 0.55f, req: new[] { "muzzle" }, opt: new[] { "tank", "big", "large" }, neg: new[] { "rifle", "explosion" }),
        Rule(BattleEffectId.MuzzleAircraft, true, false, 0.48f, req: new[] { "muzzle" }, opt: new[] { "air", "plane" }, neg: new[] { "explosion" }),
        Rule(BattleEffectId.ShellLaunchSmoke, true, false, 0.50f, req: new[] { "smoke" }, opt: new[] { "gray", "white", "trail" }, neg: new[] { "explosion", "fire" }),
        Rule(BattleEffectId.BombDropTrail, true, false, 0.38f, req: new[] { "smoke" }, opt: new[] { "white", "trail" }, neg: new[] { "explosion" }),
        Rule(BattleEffectId.BulletHitMetal, true, false, 0.52f, req: new[] { "impact", "metal" }, opt: new[] { "bimpact", "bullet" }),
        Rule(BattleEffectId.BulletHitDirt, true, false, 0.52f, req: new[] { "impact" }, opt: new[] { "dirt", "sand", "soft", "wood" }, neg: new[] { "metal" }),
        Rule(BattleEffectId.ShellExplosionSmall, true, true, 0.58f, req: new[] { "explosion" }, opt: new[] { "small", "quick" }, neg: new[] { "massive", "nuke", "monster" }),
        Rule(BattleEffectId.ExplosionSmall, true, true, 0.58f, req: new[] { "explosion" }, opt: new[] { "small", "quick" }, neg: new[] { "massive", "nuke" }),
        Rule(BattleEffectId.ShellExplosionLarge, false, true, 0.62f, req: new[] { "explosion" }, opt: new[] { "ground", "lit", "orange" }, neg: new[] { "small", "muzzle" }),
        Rule(BattleEffectId.ExplosionLarge, false, true, 0.62f, req: new[] { "explosion" }, opt: new[] { "large", "lit", "orange" }, neg: new[] { "small", "muzzle" }),
        Rule(BattleEffectId.BombExplosion, false, true, 0.65f, req: new[] { "explosion" }, opt: new[] { "aerial", "orange", "bomb" }, neg: new[] { "muzzle", "small" }),
        Rule(BattleEffectId.ShellImpactMonster, false, true, 0.72f, req: new[] { "explosion" }, opt: new[] { "hit", "impact", "monster" }, neg: new[] { "muzzle" }),
        Rule(BattleEffectId.MonsterHammerImpact, false, true, 0.68f, req: new[] { "explosion" }, opt: new[] { "ww", "hit", "impact" }, neg: new[] { "muzzle" }),
        Rule(BattleEffectId.MonsterStompDust, true, false, 0.60f, req: new[] { "smoke" }, opt: new[] { "dust", "dirt", "gray" }),
        Rule(BattleEffectId.MonsterShockwave, false, true, 0.55f, req: new[] { "shock" }, opt: new[] { "wave", "ring" }, minScore: 8),
        Rule(BattleEffectId.SoldierDeath, true, false, 0.48f, req: new[] { "impact" }, opt: new[] { "dirt", "soft", "sand" }, neg: new[] { "explosion" }),
        Rule(BattleEffectId.TankDeathExplosion, true, true, 0.70f, req: new[] { "explosion" }, opt: new[] { "ground", "large" }, neg: new[] { "muzzle", "monster" }),
        Rule(BattleEffectId.TankWreckSmoke, true, false, 0.55f, req: new[] { "smoke" }, opt: new[] { "black", "gray" }, neg: new[] { "explosion" }),
        Rule(BattleEffectId.AircraftDeathExplosion, false, true, 0.68f, req: new[] { "explosion" }, opt: new[] { "aerial", "air" }, neg: new[] { "muzzle" }),
        Rule(BattleEffectId.AirCrashExplosion, false, true, 0.68f, req: new[] { "explosion" }, opt: new[] { "aerial", "air" }),
        Rule(BattleEffectId.AircraftCrashSmoke, true, false, 0.55f, req: new[] { "smoke" }, opt: new[] { "white", "gray" }),
        Rule(BattleEffectId.MonsterDeathExplosion, false, true, 0.75f, req: new[] { "explosion" }, opt: new[] { "monster", "massive", "purple" }, neg: new[] { "muzzle", "small" }),
        Rule(BattleEffectId.MonsterDeathDust, true, false, 0.58f, req: new[] { "smoke" }, opt: new[] { "dust", "dirt" }),
        Rule(BattleEffectId.HumanSummon, false, true, 0.72f, req: new[] { "magic" }, opt: new[] { "blue", "spawn" }, neg: new[] { "rage", "red" }),
        Rule(BattleEffectId.OrcSummon, false, true, 0.78f, req: new[] { "explosion" }, opt: new[] { "monster", "purple", "spawn" }),
        Rule(BattleEffectId.HumanAirStrikeWarning, false, true, 0.85f, req: new[] { "explosion" }, opt: new[] { "sparks", "blue", "warning" }, neg: new[] { "muzzle" }),
        Rule(BattleEffectId.OrcRageBuff, false, true, 0.80f, req: new[] { "fire" }, opt: new[] { "chemical", "rage", "red", "magic" }, neg: new[] { "explosion" }),
        Rule(BattleEffectId.ClawHit, false, true, 0.55f, req: new[] { "hit" }, opt: new[] { "slash", "spark" }, minScore: 8),
    };

    // NuclearDetonation / NuclearStrikeWarning: use BindNuclearVfxPrefabs (explicit names + rules above).

    private static VfxBindRule Rule(
        BattleEffectId id,
        bool preferWarFx,
        bool preferCfxr,
        float prefabScale,
        string[] req = null,
        string[] opt = null,
        string[] neg = null,
        int minScore = 10,
        bool attachToParent = false)
    {
        return new VfxBindRule
        {
            Id = id,
            PreferWarFx = preferWarFx,
            PreferCfxr = preferCfxr,
            PrefabScaleMultiplier = prefabScale,
            RequiredTokens = req ?? Array.Empty<string>(),
            OptionalTokens = opt ?? Array.Empty<string>(),
            NegativeTokens = neg ?? Array.Empty<string>(),
            MinimumScore = minScore,
            AttachToParent = attachToParent,
        };
    }
}
#endif
