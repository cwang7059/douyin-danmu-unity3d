using System.Collections.Generic;
using UnityEngine;

public sealed class EffectManager : MonoBehaviour
{
    private const string VfxSelectedPath = "VFX/Online/Selected/";
    private const string ShaderRuntimeUnlitTint = "RuntimeMaterials/RuntimeUnlitTint";
    private const string TextureSmokeBlack = VfxSelectedPath + "smoke_black";
    private const string TextureSmokeWhite = VfxSelectedPath + "smoke_white";
    private const string TextureFlashKenney = VfxSelectedPath + "flash_kenney";
    private const string TextureMuzzleRifle = VfxSelectedPath + "muzzle_rifle";
    private const string TextureMuzzleTank = VfxSelectedPath + "muzzle_tank";
    private const string TextureExplosionKenney = VfxSelectedPath + "explosion_kenney";
    private const string TextureExplosionFireball = VfxSelectedPath + "explosion_fireball";
    private const string TextureExplosionBomb = VfxSelectedPath + "explosion_bomb";
    private const string TextureExplosionSinestesiaSmall = VfxSelectedPath + "explosion_sinestesia_small";
    private const string TextureExplosionSinestesiaLarge = VfxSelectedPath + "explosion_sinestesia_large";
    private const string TextureExplosionSinestesiaBomb = VfxSelectedPath + "explosion_sinestesia_bomb";
    private const string TextureShockwaveRing = VfxSelectedPath + "shockwave_ring";

    [SerializeField] private EffectConfig[] configs;
    [SerializeField] private bool createFallbackEffects = true;

    private static readonly Dictionary<string, Material> particleMaterialCache = new Dictionary<string, Material>();
    private readonly Dictionary<BattleEffectId, EffectConfig> configById = new Dictionary<BattleEffectId, EffectConfig>();
    private readonly Dictionary<BattleEffectId, Queue<PooledParticleEffect>> pools = new Dictionary<BattleEffectId, Queue<PooledParticleEffect>>();
    private readonly Dictionary<BattleEffectId, int> liveCounts = new Dictionary<BattleEffectId, int>();
    private Transform poolRoot;

    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        poolRoot = new GameObject("EffectPool").transform;
        poolRoot.SetParent(transform, false);
        IndexConfigs();
        Prewarm();
    }

    public PooledParticleEffect Play(BattleEffectId id, Vector3 position)
    {
        return Play(EffectPlayback.Create(id, position, Quaternion.identity));
    }

    public PooledParticleEffect Play(EffectPlayback playback)
    {
        if (playback.id == BattleEffectId.None)
        {
            return null;
        }

        EffectConfig config = GetConfig(playback.id);
        int currentLive = GetLiveCount(playback.id);
        int maxCount = config != null ? Mathf.Max(1, config.maxCount) : 64;
        if (currentLive >= maxCount)
        {
            return null;
        }

        var effect = GetOrCreate(playback.id, config);
        if (effect == null)
        {
            return null;
        }

        liveCounts[playback.id] = currentLive + 1;
        effect.Play(playback);
        PlayAttachedSound(config, playback.position);
        return effect;
    }

    public void ReturnToPool(BattleEffectId id, PooledParticleEffect effect)
    {
        if (effect == null)
        {
            return;
        }

        effect.gameObject.SetActive(false);
        effect.transform.SetParent(poolRoot, false);

        int currentLive = GetLiveCount(id);
        liveCounts[id] = Mathf.Max(0, currentLive - 1);

        Queue<PooledParticleEffect> queue;
        if (!pools.TryGetValue(id, out queue))
        {
            queue = new Queue<PooledParticleEffect>();
            pools[id] = queue;
        }

        queue.Enqueue(effect);
    }

    private void IndexConfigs()
    {
        configById.Clear();
        if (configs == null)
        {
            return;
        }

        for (int i = 0; i < configs.Length; i++)
        {
            var config = configs[i];
            if (config != null && config.id != BattleEffectId.None)
            {
                configById[config.id] = config;
            }
        }
    }

    private void Prewarm()
    {
        foreach (var pair in configById)
        {
            int count = Mathf.Max(0, pair.Value.prewarmCount);
            for (int i = 0; i < count; i++)
            {
                ReturnToPool(pair.Key, CreateEffect(pair.Key, pair.Value));
            }
        }
    }

    private PooledParticleEffect GetOrCreate(BattleEffectId id, EffectConfig config)
    {
        Queue<PooledParticleEffect> queue;
        if (pools.TryGetValue(id, out queue) && queue.Count > 0)
        {
            return queue.Dequeue();
        }

        return CreateEffect(id, config);
    }

    private PooledParticleEffect CreateEffect(BattleEffectId id, EffectConfig config)
    {
        GameObject prefab = config != null ? config.prefab : null;
        GameObject instance;
        if (prefab != null)
        {
            instance = Instantiate(prefab, poolRoot, false);
        }
        else if (createFallbackEffects)
        {
            instance = CreateFallbackEffect(id);
        }
        else
        {
            return null;
        }

        instance.name = $"FX_{id}";
        var pooled = instance.GetComponent<PooledParticleEffect>();
        if (pooled == null)
        {
            pooled = instance.AddComponent<PooledParticleEffect>();
        }

        pooled.Initialize(this, id);
        instance.SetActive(false);
        return pooled;
    }

    private EffectConfig GetConfig(BattleEffectId id)
    {
        EffectConfig config;
        configById.TryGetValue(id, out config);
        return config;
    }

    private int GetLiveCount(BattleEffectId id)
    {
        int count;
        return liveCounts.TryGetValue(id, out count) ? count : 0;
    }

    private void PlayAttachedSound(EffectConfig config, Vector3 position)
    {
        if (config == null || config.sounds == null || config.sounds.Length == 0 || BattleAudioManager.Instance == null)
        {
            return;
        }

        var clip = config.sounds[Random.Range(0, config.sounds.Length)];
        BattleAudioManager.Instance.PlayOneShot(clip, position, BattleAudioChannel.Sfx, 0.9f, true);
    }

    private GameObject CreateFallbackEffect(BattleEffectId id)
    {
        var root = new GameObject($"Fallback_{id}");
        root.transform.SetParent(poolRoot, false);
        BuildFallbackEffect(root.transform, id);
        return root;
    }

    private static void BuildFallbackEffect(Transform root, BattleEffectId id)
    {
        switch (id)
        {
            case BattleEffectId.MuzzleRifle:
                AddBurst(root, "RifleFlashCore", 0.10f, 0.045f, 0.10f, 3.2f, 6.4f, 0.24f, 0.42f, 2, Color.white, new Color(1f, 0.42f, 0.04f, 0f), ParticleSystemShapeType.Cone, 0.035f, 9f, 0f, ParticleSystemRenderMode.Billboard, TextureMuzzleRifle);
                AddBurst(root, "RifleFlashSparks", 0.18f, 0.055f, 0.18f, 5.6f, 9.2f, 0.035f, 0.075f, 7, new Color(1f, 0.82f, 0.24f, 1f), new Color(1f, 0.20f, 0.02f, 0f), ParticleSystemShapeType.Cone, 0.04f, 17f, 0.03f, ParticleSystemRenderMode.Stretch);
                AddBurst(root, "RifleSmoke", 0.35f, 0.18f, 0.36f, 0.35f, 0.95f, 0.08f, 0.20f, 7, new Color(0.62f, 0.64f, 0.62f, 0.65f), new Color(0.35f, 0.35f, 0.35f, 0f), ParticleSystemShapeType.Cone, 0.055f, 11f, -0.05f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                AddPointLight(root, "RifleFlashLight", new Color(1f, 0.74f, 0.30f, 1f), 2.2f, 4.2f);
                break;
            case BattleEffectId.MuzzleTank:
                AddBurst(root, "TankFlash", 0.16f, 0.06f, 0.13f, 4.5f, 7.8f, 0.48f, 0.86f, 4, Color.white, new Color(1f, 0.25f, 0.04f, 0f), ParticleSystemShapeType.Cone, 0.12f, 16f, 0f, ParticleSystemRenderMode.Billboard, TextureMuzzleTank);
                AddBurst(root, "TankSmoke", 0.75f, 0.42f, 0.95f, 1.0f, 2.3f, 0.28f, 0.68f, 24, new Color(0.58f, 0.56f, 0.50f, 0.78f), new Color(0.18f, 0.17f, 0.16f, 0f), ParticleSystemShapeType.Cone, 0.18f, 22f, -0.12f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
                AddBurst(root, "TankSparks", 0.22f, 0.16f, 0.28f, 4.2f, 8.5f, 0.08f, 0.16f, 10, new Color(1f, 0.80f, 0.34f, 1f), new Color(1f, 0.20f, 0.02f, 0f), ParticleSystemShapeType.Cone, 0.08f, 26f, 0.1f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
                AddPointLight(root, "TankFlashLight", new Color(1f, 0.56f, 0.20f, 1f), 4f, 7f);
                break;
            case BattleEffectId.MuzzleAircraft:
                AddBurst(root, "AircraftDropFlash", 0.12f, 0.06f, 0.16f, 1.2f, 2.8f, 0.18f, 0.34f, 5, Color.white, new Color(1f, 0.50f, 0.12f, 0f), ParticleSystemShapeType.Cone, 0.10f, 12f, 0f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
                AddBurst(root, "AircraftDropSmoke", 0.42f, 0.18f, 0.46f, 0.25f, 0.75f, 0.12f, 0.28f, 8, new Color(0.72f, 0.72f, 0.68f, 0.52f), new Color(0.28f, 0.28f, 0.25f, 0f), ParticleSystemShapeType.Sphere, 0.08f, 0f, -0.06f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                break;
            case BattleEffectId.ShellLaunchSmoke:
                AddBurst(root, "ShellTrailSmoke", 0.62f, 0.32f, 0.82f, 0.28f, 1.1f, 0.16f, 0.38f, 12, new Color(0.56f, 0.55f, 0.52f, 0.55f), new Color(0.22f, 0.22f, 0.20f, 0f), ParticleSystemShapeType.Sphere, 0.10f, 0f, -0.06f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
                break;
            case BattleEffectId.BombDropTrail:
                AddBurst(root, "BombTrailSmoke", 0.72f, 0.38f, 0.88f, 0.18f, 0.7f, 0.16f, 0.34f, 10, new Color(0.70f, 0.72f, 0.70f, 0.48f), new Color(0.30f, 0.30f, 0.28f, 0f), ParticleSystemShapeType.Sphere, 0.08f, 0f, -0.08f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                break;
            case BattleEffectId.BulletHitMetal:
                AddBurst(root, "BulletImpactFlash", 0.18f, 0.08f, 0.16f, 0.02f, 0.12f, 0.18f, 0.32f, 2, Color.white, new Color(1f, 0.36f, 0.05f, 0f), ParticleSystemShapeType.Hemisphere, 0.05f, 0f, 0.02f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
                AddBurst(root, "BulletImpactSparks", 0.28f, 0.12f, 0.24f, 2.2f, 5.8f, 0.035f, 0.09f, 9, new Color(1f, 0.78f, 0.24f, 0.95f), new Color(1f, 0.18f, 0.02f, 0f), ParticleSystemShapeType.Hemisphere, 0.07f, 0f, -0.02f, ParticleSystemRenderMode.Stretch);
                AddBurst(root, "BulletDust", 0.34f, 0.16f, 0.36f, 0.3f, 1.2f, 0.08f, 0.18f, 8, new Color(0.55f, 0.51f, 0.42f, 0.5f), new Color(0.24f, 0.22f, 0.18f, 0f), ParticleSystemShapeType.Sphere, 0.07f, 0f, -0.05f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                break;
            case BattleEffectId.BulletHitDirt:
            case BattleEffectId.SoldierDeath:
                AddBurst(root, "DirtPuff", 0.48f, 0.28f, 0.62f, 0.45f, 1.7f, 0.12f, 0.28f, id == BattleEffectId.SoldierDeath ? 18 : 10, new Color(0.62f, 0.52f, 0.38f, 0.65f), new Color(0.30f, 0.25f, 0.18f, 0f), ParticleSystemShapeType.Hemisphere, 0.15f, 0f, -0.12f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                break;
            case BattleEffectId.ShellExplosionSmall:
            case BattleEffectId.ExplosionSmall:
                AddExplosion(root, 0.72f, 16, 18, 1.0f, TextureExplosionSinestesiaSmall);
                break;
            case BattleEffectId.ShellImpactMonster:
                AddMonsterShellImpact(root);
                break;
            case BattleEffectId.ShellExplosionLarge:
            case BattleEffectId.BombExplosion:
            case BattleEffectId.TankDeathExplosion:
            case BattleEffectId.AircraftDeathExplosion:
            case BattleEffectId.AirCrashExplosion:
            case BattleEffectId.ExplosionLarge:
                AddExplosion(root, id == BattleEffectId.BombExplosion ? 1.15f : 0.98f, id == BattleEffectId.BombExplosion ? 24 : 18, id == BattleEffectId.BombExplosion ? 34 : 28, id == BattleEffectId.TankDeathExplosion ? 1.22f : 1.0f, SelectExplosionTexture(id));
                break;
            case BattleEffectId.TankWreckSmoke:
            case BattleEffectId.AircraftCrashSmoke:
                AddBurst(root, "WreckSmoke", 2.2f, 1.2f, 2.4f, 0.35f, 1.2f, 0.46f, 1.05f, 38, new Color(0.20f, 0.19f, 0.17f, 0.78f), new Color(0.08f, 0.08f, 0.08f, 0f), ParticleSystemShapeType.Hemisphere, 0.28f, 0f, -0.18f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
                break;
            case BattleEffectId.MonsterHammerImpact:
                AddBurst(root, "HammerDebris", 0.72f, 0.34f, 0.82f, 2.0f, 5.0f, 0.12f, 0.30f, 28, new Color(0.68f, 0.58f, 0.42f, 0.9f), new Color(0.22f, 0.18f, 0.12f, 0f), ParticleSystemShapeType.Hemisphere, 0.22f, 0f, -0.16f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                AddBurst(root, "HammerImpactFlash", 0.36f, 0.18f, 0.34f, 0.04f, 0.16f, 0.74f, 1.12f, 3, Color.white, new Color(1f, 0.42f, 0.06f, 0f), ParticleSystemShapeType.Hemisphere, 0.10f, 0f, 0.08f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
                AddBurst(root, "HammerImpactSparks", 0.38f, 0.12f, 0.30f, 2.4f, 6.4f, 0.06f, 0.15f, 18, new Color(1f, 0.78f, 0.30f, 0.92f), new Color(0.95f, 0.16f, 0.03f, 0f), ParticleSystemShapeType.Hemisphere, 0.18f, 0f, -0.04f, ParticleSystemRenderMode.Stretch);
                AddShockwave(root, 20, 1.1f, new Color(1f, 0.74f, 0.34f, 0.58f));
                break;
            case BattleEffectId.MonsterStompDust:
                AddBurst(root, "StompDust", 0.9f, 0.45f, 1.0f, 1.2f, 3.4f, 0.22f, 0.50f, 36, new Color(0.58f, 0.48f, 0.34f, 0.68f), new Color(0.26f, 0.22f, 0.16f, 0f), ParticleSystemShapeType.Hemisphere, 0.32f, 0f, -0.25f, ParticleSystemRenderMode.Billboard, TextureSmokeWhite);
                AddShockwave(root, 24, 0.9f, new Color(0.75f, 0.62f, 0.40f, 0.45f));
                break;
            case BattleEffectId.MonsterShockwave:
                AddShockwave(root, 36, 1.35f, new Color(1f, 0.88f, 0.50f, 0.48f));
                break;
            case BattleEffectId.ClawHit:
                AddBurst(root, "ClawImpactFlash", 0.28f, 0.12f, 0.26f, 0.02f, 0.14f, 0.48f, 0.78f, 2, Color.white, new Color(1f, 0.24f, 0.04f, 0f), ParticleSystemShapeType.Hemisphere, 0.08f, 0f, 0.06f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
                AddBurst(root, "ClawImpactSparks", 0.30f, 0.10f, 0.24f, 2.0f, 5.5f, 0.05f, 0.12f, 12, new Color(1f, 0.62f, 0.20f, 0.9f), new Color(0.95f, 0.10f, 0.02f, 0f), ParticleSystemShapeType.Hemisphere, 0.12f, 0f, -0.02f, ParticleSystemRenderMode.Stretch);
                break;
            case BattleEffectId.MonsterDeathExplosion:
                AddExplosion(root, 1.25f, 26, 36, 1.45f, TextureExplosionSinestesiaBomb);
                break;
            case BattleEffectId.MonsterDeathDust:
                AddBurst(root, "MonsterDeathDust", 1.6f, 0.8f, 1.8f, 1.5f, 4.6f, 0.44f, 1.08f, 72, new Color(0.48f, 0.40f, 0.30f, 0.72f), new Color(0.18f, 0.15f, 0.12f, 0f), ParticleSystemShapeType.Hemisphere, 0.48f, 0f, -0.28f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
                AddShockwave(root, 42, 1.6f, new Color(0.85f, 0.66f, 0.38f, 0.38f));
                break;
            case BattleEffectId.HumanAirStrikeWarning:
                AddShockwave(root, 48, 1.6f, new Color(0.32f, 0.72f, 1f, 0.52f));
                AddBurst(root, "AirStrikeMarker", 0.9f, 0.5f, 0.9f, 0.1f, 0.5f, 0.08f, 0.18f, 22, new Color(0.34f, 0.80f, 1f, 0.86f), new Color(0.10f, 0.38f, 1f, 0f), ParticleSystemShapeType.Circle, 0.5f, 0f, 0f);
                break;
            case BattleEffectId.OrcRageBuff:
                AddBurst(root, "RageFlare", 1.2f, 0.45f, 1.1f, 0.8f, 2.2f, 0.16f, 0.42f, 46, new Color(1f, 0.18f, 0.08f, 0.82f), new Color(0.35f, 0.02f, 0.01f, 0f), ParticleSystemShapeType.Sphere, 0.42f, 0f, -0.08f);
                break;
            default:
                AddBurst(root, "FallbackBurst", IsLargeEffect(id) ? 0.9f : 0.45f, IsSmokeLike(id) ? 0.7f : 0.25f, IsSmokeLike(id) ? 1.2f : 0.45f, IsLargeEffect(id) ? 3.5f : 1.4f, IsLargeEffect(id) ? 7f : 3.5f, IsLargeEffect(id) ? 0.32f : 0.16f, IsLargeEffect(id) ? 0.62f : 0.28f, IsLargeEffect(id) ? 42 : 16, FallbackColor(id), new Color(0.36f, 0.36f, 0.36f, 0f), ParticleSystemShapeType.Sphere, IsLargeEffect(id) ? 0.55f : 0.18f, 0f, IsSmokeLike(id) ? -0.1f : 0f);
                break;
        }
    }

    private static void AddMonsterShellImpact(Transform root)
    {
        AddBurst(root, "MonsterShellImpactFireball", 0.74f, 0.32f, 0.56f, 0.04f, 0.22f, 1.1f, 1.75f, 2, Color.white, new Color(1f, 0.24f, 0.02f, 0f), ParticleSystemShapeType.Sphere, 0.08f, 0f, -0.02f, ParticleSystemRenderMode.Billboard, TextureExplosionFireball);
        AddBurst(root, "MonsterShellImpactFlash", 0.42f, 0.16f, 0.32f, 0.08f, 0.36f, 0.84f, 1.35f, 3, new Color(1f, 0.96f, 0.62f, 0.92f), new Color(1f, 0.28f, 0.04f, 0f), ParticleSystemShapeType.Sphere, 0.12f, 0f, -0.02f, ParticleSystemRenderMode.Billboard, TextureFlashKenney);
        AddBurst(root, "MonsterShellImpactSmoke", 1.35f, 0.55f, 1.25f, 0.32f, 1.4f, 0.32f, 0.86f, 24, new Color(0.34f, 0.30f, 0.24f, 0.66f), new Color(0.08f, 0.06f, 0.04f, 0f), ParticleSystemShapeType.Sphere, 0.18f, 0f, -0.24f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
        AddBurst(root, "MonsterShellImpactDebris", 0.58f, 0.20f, 0.52f, 3.0f, 8.0f, 0.08f, 0.18f, 26, new Color(1f, 0.72f, 0.24f, 0.95f), new Color(1f, 0.16f, 0.02f, 0f), ParticleSystemShapeType.Sphere, 0.24f, 0f, -0.18f, ParticleSystemRenderMode.Stretch);
        AddShockwave(root, 30, 1.18f, new Color(1f, 0.58f, 0.18f, 0.52f));
        AddPointLight(root, "MonsterShellImpactLight", new Color(1f, 0.48f, 0.14f, 1f), 6.8f, 9.5f);
    }

    private static void AddExplosion(Transform root, float duration, int fireCount, int debrisCount, float scale, string fireTextureResourcePath)
    {
        int animatedFireCount = Mathf.Clamp(Mathf.CeilToInt(fireCount / 12f), 1, 3);
        AddBurst(root, "ExplosionFire", duration, 0.34f, 0.58f, 0.05f * scale, 0.28f * scale, 0.95f * scale, 1.45f * scale, animatedFireCount, Color.white, new Color(1f, 0.28f, 0.04f, 0f), ParticleSystemShapeType.Sphere, 0.12f * scale, 0f, 0f, ParticleSystemRenderMode.Billboard, fireTextureResourcePath);
        AddBurst(root, "ExplosionSmoke", duration + 1.2f, 0.8f, 1.85f, 0.8f * scale, 2.2f * scale, 0.36f * scale, 0.92f * scale, Mathf.Max(18, fireCount), new Color(0.34f, 0.32f, 0.28f, 0.78f), new Color(0.10f, 0.10f, 0.09f, 0f), ParticleSystemShapeType.Hemisphere, 0.34f * scale, 0f, -0.12f, ParticleSystemRenderMode.Billboard, TextureSmokeBlack);
        AddBurst(root, "ExplosionDebris", duration, 0.28f, 0.75f, 2.6f * scale, 7.0f * scale, 0.07f * scale, 0.18f * scale, debrisCount, new Color(0.58f, 0.46f, 0.32f, 0.95f), new Color(0.18f, 0.13f, 0.08f, 0f), ParticleSystemShapeType.Hemisphere, 0.28f * scale, 0f, -0.30f, ParticleSystemRenderMode.Stretch);
        AddShockwave(root, Mathf.RoundToInt(24 * scale), 1.0f * scale, new Color(1f, 0.70f, 0.28f, 0.42f));
        AddPointLight(root, "ExplosionLight", new Color(1f, 0.52f, 0.18f, 1f), 5.5f * scale, 8f * scale);
    }

    private static ParticleSystem AddBurst(Transform parent, string name, float duration, float lifetimeMin, float lifetimeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, int count, Color start, Color end, ParticleSystemShapeType shapeType, float radius, float coneAngle = 0f, float gravity = 0f, ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard, string textureResourcePath = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = Mathf.Max(0.05f, duration);
        main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Max(0.02f, lifetimeMin), Mathf.Max(lifetimeMin, lifetimeMax));
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(start, Color.Lerp(start, Color.white, 0.15f));
        main.gravityModifier = gravity;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, short.MaxValue)) });

        var shape = system.shape;
        shape.shapeType = shapeType;
        shape.radius = radius;
        if (shapeType == ParticleSystemShapeType.Cone)
        {
            shape.angle = Mathf.Max(1f, coneAngle);
        }

        var color = system.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(start, end);

        var size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.35f, 1.0f),
            new Keyframe(1f, 0.15f)));

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = renderMode;
        renderer.lengthScale = renderMode == ParticleSystemRenderMode.Stretch ? 1.7f : 1f;
        renderer.velocityScale = renderMode == ParticleSystemRenderMode.Stretch ? 0.35f : 0f;
        Material material = GetParticleMaterial(textureResourcePath, renderMode);
        if (material != null)
        {
            renderer.sharedMaterial = material;
            ConfigureTextureSheet(system, textureResourcePath);
        }

        return system;
    }

    private static void AddShockwave(Transform parent, int count, float scale, Color color)
    {
        int ringCount = Mathf.Clamp(Mathf.CeilToInt(count / 12f), 1, 5);
        var system = AddBurst(parent, "ShockwaveRing", 0.55f, 0.35f, 0.55f, 0.02f * scale, 0.08f * scale, 0.85f * scale, 1.28f * scale, ringCount, color, new Color(color.r, color.g, color.b, 0f), ParticleSystemShapeType.Circle, 0.02f * scale, 0f, 0f, ParticleSystemRenderMode.HorizontalBillboard, TextureShockwaveRing);
        var shape = system.shape;
        shape.rotation = new Vector3(90f, 0f, 0f);
        var size = system.sizeOverLifetime;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.20f),
            new Keyframe(0.38f, 1.05f),
            new Keyframe(1f, 1.65f)));
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.02f);
    }

    private static string SelectExplosionTexture(BattleEffectId id)
    {
        if (id == BattleEffectId.BombExplosion
            || id == BattleEffectId.TankDeathExplosion
            || id == BattleEffectId.AircraftDeathExplosion
            || id == BattleEffectId.AirCrashExplosion)
        {
            return TextureExplosionSinestesiaBomb;
        }

        return TextureExplosionSinestesiaLarge;
    }

    private static Material GetParticleMaterial(string textureResourcePath, ParticleSystemRenderMode renderMode)
    {
        string cacheKey = (string.IsNullOrEmpty(textureResourcePath) ? "__solid" : textureResourcePath) + "|" + renderMode;
        Material material;
        if (particleMaterialCache.TryGetValue(cacheKey, out material))
        {
            return material;
        }

        Texture2D texture = null;
        if (!string.IsNullOrEmpty(textureResourcePath))
        {
            texture = Resources.Load<Texture2D>(textureResourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"VFX texture not found in Resources: {textureResourcePath}");
                return null;
            }
        }

        Shader tintShader = Resources.Load<Shader>(ShaderRuntimeUnlitTint) ?? Shader.Find("ApocalypseKing/UnlitTint");
        if (tintShader != null)
        {
            material = new Material(tintShader);
        }
        else
        {
            Shader shader = Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Transparent");
            if (shader == null)
            {
                return null;
            }

            material = new Material(shader);
        }

        material.name = texture != null ? "VFX_" + texture.name : "VFX_Solid";
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (texture != null && material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", Color.white);
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        particleMaterialCache[cacheKey] = material;
        return material;
    }

    private static void ConfigureTextureSheet(ParticleSystem system, string textureResourcePath)
    {
        int tilesX;
        int tilesY;
        if (!TryGetTextureSheetTiles(textureResourcePath, out tilesX, out tilesY))
        {
            return;
        }

        var sheet = system.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = tilesX;
        sheet.numTilesY = tilesY;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        sheet.cycleCount = 1;
    }

    private static bool TryGetTextureSheetTiles(string textureResourcePath, out int tilesX, out int tilesY)
    {
        if (textureResourcePath == TextureExplosionSinestesiaSmall
            || textureResourcePath == TextureExplosionSinestesiaLarge
            || textureResourcePath == TextureExplosionSinestesiaBomb)
        {
            tilesX = 8;
            tilesY = 8;
            return true;
        }

        if (textureResourcePath == TextureExplosionFireball
            || textureResourcePath == TextureExplosionBomb
            || textureResourcePath == TextureShockwaveRing)
        {
            tilesX = 4;
            tilesY = 4;
            return true;
        }

        tilesX = 1;
        tilesY = 1;
        return false;
    }

    private static void AddPointLight(Transform parent, string name, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private static ParticleSystem.MinMaxGradient CreateFadeGradient(Color start, Color end)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(Mathf.Lerp(start.a, end.a, 0.45f), 0.45f), new GradientAlphaKey(end.a, 1f) });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    private static bool IsLargeEffect(BattleEffectId id)
    {
        return id == BattleEffectId.ExplosionLarge
            || id == BattleEffectId.ShellExplosionLarge
            || id == BattleEffectId.ShellImpactMonster
            || id == BattleEffectId.BombExplosion
            || id == BattleEffectId.TankDeathExplosion
            || id == BattleEffectId.AircraftDeathExplosion
            || id == BattleEffectId.MonsterDeathExplosion
            || id == BattleEffectId.HumanAirStrikeWarning
            || id == BattleEffectId.OrcRageBuff;
    }

    private static bool IsSmokeLike(BattleEffectId id)
    {
        return id == BattleEffectId.BulletHitDirt
            || id == BattleEffectId.ShellLaunchSmoke
            || id == BattleEffectId.BombDropTrail
            || id == BattleEffectId.TankWreckSmoke
            || id == BattleEffectId.AircraftCrashSmoke
            || id == BattleEffectId.MonsterDeathDust
            || id == BattleEffectId.HumanSummon
            || id == BattleEffectId.OrcSummon;
    }

    private static Color FallbackColor(BattleEffectId id)
    {
        switch (id)
        {
            case BattleEffectId.BulletHitMetal:
            case BattleEffectId.MuzzleRifle:
            case BattleEffectId.MuzzleTank:
            case BattleEffectId.ShellLaunchSmoke:
                return new Color(1f, 0.82f, 0.32f, 1f);
            case BattleEffectId.HumanSummon:
            case BattleEffectId.HumanAirStrikeWarning:
                return new Color(0.25f, 0.68f, 1f, 1f);
            case BattleEffectId.OrcSummon:
            case BattleEffectId.OrcRageBuff:
            case BattleEffectId.ClawHit:
                return new Color(1f, 0.25f, 0.16f, 1f);
            default:
                return new Color(1f, 0.48f, 0.12f, 1f);
        }
    }
}
