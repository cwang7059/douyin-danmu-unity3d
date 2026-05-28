using System.Collections.Generic;
using UnityEngine;

public sealed class EffectManager : MonoBehaviour
{
    [SerializeField] private EffectConfig[] configs;
    [SerializeField] private bool createFallbackEffects = true;

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
                AddBurst(root, "RifleFlash", 0.08f, 0.045f, 0.09f, 2.8f, 5.2f, 0.10f, 0.19f, 6, new Color(1f, 0.92f, 0.52f, 1f), new Color(1f, 0.35f, 0.08f, 0f), ParticleSystemShapeType.Cone, 0.035f, 13f, 0f, ParticleSystemRenderMode.Stretch);
                AddBurst(root, "RifleSmoke", 0.35f, 0.18f, 0.36f, 0.35f, 0.95f, 0.06f, 0.16f, 7, new Color(0.62f, 0.64f, 0.62f, 0.65f), new Color(0.35f, 0.35f, 0.35f, 0f), ParticleSystemShapeType.Cone, 0.055f, 11f, -0.05f);
                AddPointLight(root, "RifleFlashLight", new Color(1f, 0.78f, 0.38f, 1f), 1.5f, 3f);
                break;
            case BattleEffectId.MuzzleTank:
                AddBurst(root, "TankFlash", 0.16f, 0.06f, 0.13f, 4.5f, 7.8f, 0.34f, 0.62f, 18, new Color(1f, 0.88f, 0.42f, 1f), new Color(1f, 0.25f, 0.04f, 0f), ParticleSystemShapeType.Cone, 0.12f, 16f, 0f, ParticleSystemRenderMode.Stretch);
                AddBurst(root, "TankSmoke", 0.75f, 0.42f, 0.95f, 1.0f, 2.3f, 0.22f, 0.58f, 24, new Color(0.58f, 0.56f, 0.50f, 0.78f), new Color(0.18f, 0.17f, 0.16f, 0f), ParticleSystemShapeType.Cone, 0.18f, 22f, -0.12f);
                AddBurst(root, "TankSparks", 0.22f, 0.16f, 0.28f, 4.2f, 8.5f, 0.045f, 0.085f, 14, new Color(1f, 0.80f, 0.34f, 1f), new Color(1f, 0.20f, 0.02f, 0f), ParticleSystemShapeType.Cone, 0.08f, 26f, 0.1f, ParticleSystemRenderMode.Stretch);
                AddPointLight(root, "TankFlashLight", new Color(1f, 0.56f, 0.20f, 1f), 4f, 7f);
                break;
            case BattleEffectId.ShellLaunchSmoke:
                AddBurst(root, "ShellTrailSmoke", 0.62f, 0.32f, 0.82f, 0.28f, 1.1f, 0.12f, 0.30f, 12, new Color(0.56f, 0.55f, 0.52f, 0.55f), new Color(0.22f, 0.22f, 0.20f, 0f), ParticleSystemShapeType.Sphere, 0.10f, 0f, -0.06f);
                break;
            case BattleEffectId.BombDropTrail:
                AddBurst(root, "BombTrailSmoke", 0.72f, 0.38f, 0.88f, 0.18f, 0.7f, 0.12f, 0.28f, 10, new Color(0.70f, 0.72f, 0.70f, 0.48f), new Color(0.30f, 0.30f, 0.28f, 0f), ParticleSystemShapeType.Sphere, 0.08f, 0f, -0.08f);
                break;
            case BattleEffectId.BulletHitMetal:
                AddBurst(root, "BulletSparks", 0.22f, 0.12f, 0.28f, 3.6f, 7.5f, 0.035f, 0.075f, 14, new Color(1f, 0.88f, 0.34f, 1f), new Color(1f, 0.16f, 0.02f, 0f), ParticleSystemShapeType.Hemisphere, 0.10f, 0f, 0.05f, ParticleSystemRenderMode.Stretch);
                AddBurst(root, "BulletDust", 0.34f, 0.16f, 0.36f, 0.3f, 1.2f, 0.06f, 0.16f, 8, new Color(0.55f, 0.51f, 0.42f, 0.5f), new Color(0.24f, 0.22f, 0.18f, 0f), ParticleSystemShapeType.Sphere, 0.07f, 0f, -0.05f);
                break;
            case BattleEffectId.BulletHitDirt:
            case BattleEffectId.SoldierDeath:
                AddBurst(root, "DirtPuff", 0.48f, 0.28f, 0.62f, 0.45f, 1.7f, 0.09f, 0.24f, id == BattleEffectId.SoldierDeath ? 18 : 10, new Color(0.62f, 0.52f, 0.38f, 0.65f), new Color(0.30f, 0.25f, 0.18f, 0f), ParticleSystemShapeType.Hemisphere, 0.15f, 0f, -0.12f);
                break;
            case BattleEffectId.ShellExplosionSmall:
            case BattleEffectId.ExplosionSmall:
                AddExplosion(root, 0.72f, 28, 18, 1.0f);
                break;
            case BattleEffectId.ShellExplosionLarge:
            case BattleEffectId.BombExplosion:
            case BattleEffectId.TankDeathExplosion:
            case BattleEffectId.AircraftDeathExplosion:
            case BattleEffectId.AirCrashExplosion:
            case BattleEffectId.ExplosionLarge:
                AddExplosion(root, id == BattleEffectId.BombExplosion ? 1.15f : 0.98f, id == BattleEffectId.BombExplosion ? 58 : 48, id == BattleEffectId.BombExplosion ? 34 : 28, id == BattleEffectId.TankDeathExplosion ? 1.22f : 1.0f);
                break;
            case BattleEffectId.TankWreckSmoke:
            case BattleEffectId.AircraftCrashSmoke:
                AddBurst(root, "WreckSmoke", 2.2f, 1.2f, 2.4f, 0.35f, 1.2f, 0.42f, 0.92f, 38, new Color(0.20f, 0.19f, 0.17f, 0.78f), new Color(0.08f, 0.08f, 0.08f, 0f), ParticleSystemShapeType.Hemisphere, 0.28f, 0f, -0.18f);
                break;
            case BattleEffectId.MonsterHammerImpact:
                AddBurst(root, "HammerDebris", 0.72f, 0.34f, 0.82f, 2.0f, 5.0f, 0.10f, 0.26f, 28, new Color(0.68f, 0.58f, 0.42f, 0.9f), new Color(0.22f, 0.18f, 0.12f, 0f), ParticleSystemShapeType.Hemisphere, 0.22f, 0f, -0.16f);
                AddBurst(root, "HammerSparks", 0.28f, 0.12f, 0.24f, 3.0f, 7.2f, 0.04f, 0.10f, 18, new Color(1f, 0.74f, 0.26f, 1f), new Color(1f, 0.12f, 0.04f, 0f), ParticleSystemShapeType.Hemisphere, 0.18f, 0f, 0.15f, ParticleSystemRenderMode.Stretch);
                AddShockwave(root, 20, 1.1f, new Color(1f, 0.74f, 0.34f, 0.58f));
                break;
            case BattleEffectId.MonsterStompDust:
                AddBurst(root, "StompDust", 0.9f, 0.45f, 1.0f, 1.2f, 3.4f, 0.16f, 0.42f, 36, new Color(0.58f, 0.48f, 0.34f, 0.68f), new Color(0.26f, 0.22f, 0.16f, 0f), ParticleSystemShapeType.Hemisphere, 0.32f, 0f, -0.25f);
                AddShockwave(root, 24, 0.9f, new Color(0.75f, 0.62f, 0.40f, 0.45f));
                break;
            case BattleEffectId.MonsterShockwave:
                AddShockwave(root, 36, 1.35f, new Color(1f, 0.88f, 0.50f, 0.48f));
                break;
            case BattleEffectId.ClawHit:
                AddBurst(root, "ClawSlash", 0.36f, 0.12f, 0.28f, 1.5f, 4.5f, 0.08f, 0.18f, 18, new Color(1f, 0.24f, 0.12f, 0.92f), new Color(0.40f, 0.02f, 0.01f, 0f), ParticleSystemShapeType.Hemisphere, 0.18f, 0f, 0.1f);
                break;
            case BattleEffectId.MonsterDeathExplosion:
                AddExplosion(root, 1.25f, 66, 36, 1.45f);
                break;
            case BattleEffectId.MonsterDeathDust:
                AddBurst(root, "MonsterDeathDust", 1.6f, 0.8f, 1.8f, 1.5f, 4.6f, 0.34f, 0.92f, 72, new Color(0.48f, 0.40f, 0.30f, 0.72f), new Color(0.18f, 0.15f, 0.12f, 0f), ParticleSystemShapeType.Hemisphere, 0.48f, 0f, -0.28f);
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

    private static void AddExplosion(Transform root, float duration, int fireCount, int debrisCount, float scale)
    {
        AddBurst(root, "ExplosionFire", duration, 0.18f, 0.36f, 2.8f * scale, 6.8f * scale, 0.32f * scale, 0.72f * scale, fireCount, new Color(1f, 0.64f, 0.18f, 1f), new Color(0.35f, 0.04f, 0.01f, 0f), ParticleSystemShapeType.Sphere, 0.26f * scale, 0f, 0f);
        AddBurst(root, "ExplosionSmoke", duration + 1.2f, 0.8f, 1.85f, 0.8f * scale, 2.2f * scale, 0.36f * scale, 0.92f * scale, Mathf.Max(18, fireCount / 2), new Color(0.34f, 0.32f, 0.28f, 0.78f), new Color(0.10f, 0.10f, 0.09f, 0f), ParticleSystemShapeType.Hemisphere, 0.34f * scale, 0f, -0.12f);
        AddBurst(root, "ExplosionDebris", duration, 0.28f, 0.75f, 2.6f * scale, 7.0f * scale, 0.07f * scale, 0.18f * scale, debrisCount, new Color(0.58f, 0.46f, 0.32f, 0.95f), new Color(0.18f, 0.13f, 0.08f, 0f), ParticleSystemShapeType.Hemisphere, 0.28f * scale, 0f, -0.30f, ParticleSystemRenderMode.Stretch);
        AddShockwave(root, Mathf.RoundToInt(24 * scale), 1.0f * scale, new Color(1f, 0.70f, 0.28f, 0.42f));
        AddPointLight(root, "ExplosionLight", new Color(1f, 0.52f, 0.18f, 1f), 5.5f * scale, 8f * scale);
    }

    private static ParticleSystem AddBurst(Transform parent, string name, float duration, float lifetimeMin, float lifetimeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, int count, Color start, Color end, ParticleSystemShapeType shapeType, float radius, float coneAngle = 0f, float gravity = 0f, ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard)
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
        return system;
    }

    private static void AddShockwave(Transform parent, int count, float scale, Color color)
    {
        var system = AddBurst(parent, "ShockwaveRing", 0.42f, 0.28f, 0.42f, 2.2f * scale, 4.2f * scale, 0.10f * scale, 0.22f * scale, count, color, new Color(color.r, color.g, color.b, 0f), ParticleSystemShapeType.Circle, 0.08f * scale, 0f, 0f, ParticleSystemRenderMode.Stretch);
        var shape = system.shape;
        shape.rotation = new Vector3(90f, 0f, 0f);
        var velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.02f);
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
