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
        var system = root.AddComponent<ParticleSystem>();
        var main = system.main;
        main.duration = IsLargeEffect(id) ? 0.9f : 0.45f;
        main.startLifetime = IsSmokeLike(id) ? 1.2f : 0.45f;
        main.startSpeed = IsLargeEffect(id) ? 7f : 3.5f;
        main.startSize = IsLargeEffect(id) ? 0.45f : 0.22f;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = FallbackColor(id);

        var emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)(IsLargeEffect(id) ? 42 : 16)),
        });

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = IsLargeEffect(id) ? 0.55f : 0.18f;

        var color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(FallbackColor(id), 0f), new GradientColorKey(Color.gray, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        return root;
    }

    private static bool IsLargeEffect(BattleEffectId id)
    {
        return id == BattleEffectId.ExplosionLarge || id == BattleEffectId.HumanAirStrikeWarning || id == BattleEffectId.OrcRageBuff;
    }

    private static bool IsSmokeLike(BattleEffectId id)
    {
        return id == BattleEffectId.BulletHitDirt || id == BattleEffectId.HumanSummon || id == BattleEffectId.OrcSummon;
    }

    private static Color FallbackColor(BattleEffectId id)
    {
        switch (id)
        {
            case BattleEffectId.BulletHitMetal:
            case BattleEffectId.MuzzleRifle:
            case BattleEffectId.MuzzleTank:
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
