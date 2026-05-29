using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class BattleAudioManager : MonoBehaviour
{
    private const int FallbackSampleRate = 22050;

    [SerializeField] private AudioCueConfig[] cues;
    [SerializeField] private bool createFallbackAudio = true;
    [SerializeField] private int prewarmSources = 24;
    [SerializeField] private int maxSources = 48;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup weaponGroup;
    [SerializeField] private AudioMixerGroup explosionGroup;
    [SerializeField] private AudioMixerGroup creatureGroup;
    [SerializeField] private AudioMixerGroup magicGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup voiceGroup;
    [SerializeField] private AudioMixerGroup ambienceGroup;

    private readonly Queue<AudioSource> sourcePool = new Queue<AudioSource>();
    private readonly Dictionary<BattleAudioCueId, AudioCueConfig> cueById = new Dictionary<BattleAudioCueId, AudioCueConfig>();
    private readonly Dictionary<BattleAudioCueId, float> lastPlayTimes = new Dictionary<BattleAudioCueId, float>();
    private readonly Dictionary<BattleAudioCueId, AudioClip[]> fallbackClipById = new Dictionary<BattleAudioCueId, AudioClip[]>();
    private Transform sourceRoot;
    private int createdSources;

    public static BattleAudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        sourceRoot = new GameObject("AudioPool").transform;
        sourceRoot.SetParent(transform, false);
        IndexCues();
        Prewarm();
    }

    public AudioSource Play(BattleAudioCueId id, Vector3 position)
    {
        AudioCueConfig cue;
        RuntimeAudioCue runtimeCue;
        if (!TryGetRuntimeCue(id, out cue, out runtimeCue))
        {
            return null;
        }

        if (!CanPlay(id, runtimeCue.minInterval))
        {
            return null;
        }

        AudioClip clip = runtimeCue.clips[Random.Range(0, runtimeCue.clips.Length)];
        AudioMixerGroup mixerGroup = cue != null ? cue.mixerGroup : null;
        return PlayOneShot(clip, position, runtimeCue.channel, runtimeCue.volume, runtimeCue.spatial, runtimeCue.pitchJitter, mixerGroup);
    }

    public AudioSource PlayOneShot(AudioClip clip, Vector3 position, BattleAudioChannel channel, float volume = 1f, bool spatial = false, float pitchJitter = 0f, AudioMixerGroup mixerGroup = null)
    {
        if (clip == null)
        {
            return null;
        }

        var source = GetSource();
        if (source == null)
        {
            return null;
        }

        source.transform.position = position;
        source.clip = clip;
        source.outputAudioMixerGroup = mixerGroup != null ? mixerGroup : GetMixerGroup(channel);
        source.volume = Mathf.Clamp01(volume);
        source.pitch = 1f + Random.Range(-Mathf.Abs(pitchJitter), Mathf.Abs(pitchJitter));
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 4f;
        source.maxDistance = 45f;
        source.loop = false;
        source.gameObject.SetActive(true);
        source.Play();
        StartCoroutine(ReturnAfter(source, clip.length / Mathf.Max(0.01f, Mathf.Abs(source.pitch))));
        return source;
    }

    private void IndexCues()
    {
        cueById.Clear();
        if (cues == null)
        {
            return;
        }

        for (int i = 0; i < cues.Length; i++)
        {
            var cue = cues[i];
            if (cue != null && cue.id != BattleAudioCueId.None)
            {
                cueById[cue.id] = cue;
            }
        }
    }

    private void Prewarm()
    {
        int count = Mathf.Clamp(prewarmSources, 0, maxSources);
        for (int i = 0; i < count; i++)
        {
            ReturnSource(CreateSource());
        }
    }

    private bool TryGetRuntimeCue(BattleAudioCueId id, out AudioCueConfig configuredCue, out RuntimeAudioCue runtimeCue)
    {
        configuredCue = null;
        runtimeCue = default;

        if (cueById.TryGetValue(id, out configuredCue)
            && configuredCue != null
            && configuredCue.clips != null
            && configuredCue.clips.Length > 0)
        {
            runtimeCue = new RuntimeAudioCue
            {
                clips = configuredCue.clips,
                channel = configuredCue.channel,
                volume = configuredCue.volume,
                pitchJitter = configuredCue.pitchJitter,
                minInterval = configuredCue.minInterval,
                spatial = configuredCue.spatial,
            };
            return true;
        }

        configuredCue = null;
        if (!createFallbackAudio)
        {
            return false;
        }

        AudioClip[] clips = GetFallbackClips(id);
        if (clips == null || clips.Length == 0)
        {
            return false;
        }

        runtimeCue = FallbackCueDefaults(id, clips);
        return true;
    }

    private AudioClip[] GetFallbackClips(BattleAudioCueId id)
    {
        AudioClip[] clips;
        if (fallbackClipById.TryGetValue(id, out clips))
        {
            return clips;
        }

        clips = CreateFallbackClips(id);
        fallbackClipById[id] = clips;
        return clips;
    }

    private static AudioClip[] CreateFallbackClips(BattleAudioCueId id)
    {
        switch (id)
        {
            case BattleAudioCueId.RifleShot:
                return new[] { CreateFallbackClip("Fallback_RifleShot", 0.085f, 980f, 160f, 0.42f, 0.01f, 2.8f, 0.55f) };
            case BattleAudioCueId.TankShot:
                return new[] { CreateFallbackClip("Fallback_TankShot", 0.24f, 190f, 48f, 0.72f, 0.015f, 2.2f, 0.85f) };
            case BattleAudioCueId.ExplosionSmall:
                return new[] { CreateFallbackClip("Fallback_ExplosionSmall", 0.52f, 92f, 28f, 0.88f, 0.02f, 1.55f, 0.78f) };
            case BattleAudioCueId.ExplosionLarge:
                return new[] { CreateFallbackClip("Fallback_ExplosionLarge", 0.92f, 68f, 18f, 0.94f, 0.025f, 1.25f, 0.95f) };
            case BattleAudioCueId.CreatureHit:
                return new[] { CreateFallbackClip("Fallback_CreatureHit", 0.20f, 150f, 70f, 0.38f, 0.015f, 2.0f, 0.62f) };
            case BattleAudioCueId.HumanSkill:
                return new[] { CreateFallbackClip("Fallback_HumanSkill", 0.38f, 540f, 900f, 0.18f, 0.02f, 1.8f, 0.48f) };
            case BattleAudioCueId.OrcSkill:
                return new[] { CreateFallbackClip("Fallback_OrcSkill", 0.45f, 170f, 82f, 0.48f, 0.02f, 1.7f, 0.58f) };
            case BattleAudioCueId.UiClick:
                return new[] { CreateFallbackClip("Fallback_UiClick", 0.055f, 880f, 620f, 0.08f, 0.005f, 3.2f, 0.28f) };
            case BattleAudioCueId.UiWarning:
                return new[] { CreateFallbackClip("Fallback_UiWarning", 0.22f, 440f, 330f, 0.12f, 0.01f, 1.9f, 0.42f) };
            default:
                return null;
        }
    }

    private static RuntimeAudioCue FallbackCueDefaults(BattleAudioCueId id, AudioClip[] clips)
    {
        RuntimeAudioCue cue = new RuntimeAudioCue
        {
            clips = clips,
            channel = BattleAudioChannel.Sfx,
            volume = 0.8f,
            pitchJitter = 0.035f,
            minInterval = 0.08f,
            spatial = true,
        };

        switch (id)
        {
            case BattleAudioCueId.RifleShot:
                cue.channel = BattleAudioChannel.Weapon;
                cue.volume = 0.42f;
                cue.minInterval = 0.035f;
                break;
            case BattleAudioCueId.TankShot:
                cue.channel = BattleAudioChannel.Weapon;
                cue.volume = 0.86f;
                cue.minInterval = 0.12f;
                break;
            case BattleAudioCueId.ExplosionSmall:
                cue.channel = BattleAudioChannel.Explosion;
                cue.volume = 0.76f;
                cue.minInterval = 0.08f;
                break;
            case BattleAudioCueId.ExplosionLarge:
                cue.channel = BattleAudioChannel.Explosion;
                cue.volume = 0.95f;
                cue.minInterval = 0.16f;
                break;
            case BattleAudioCueId.CreatureHit:
                cue.channel = BattleAudioChannel.Creature;
                cue.volume = 0.62f;
                cue.minInterval = 0.08f;
                break;
            case BattleAudioCueId.HumanSkill:
            case BattleAudioCueId.OrcSkill:
                cue.channel = BattleAudioChannel.Magic;
                cue.volume = 0.58f;
                cue.minInterval = 0.25f;
                break;
            case BattleAudioCueId.UiClick:
            case BattleAudioCueId.UiWarning:
                cue.channel = BattleAudioChannel.Ui;
                cue.spatial = false;
                break;
        }

        return cue;
    }

    private static AudioClip CreateFallbackClip(string name, float duration, float startHz, float endHz, float noiseMix, float attack, float decayPower, float gain)
    {
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * FallbackSampleRate));
        float[] samples = new float[sampleCount];
        float phase = 0f;
        float seed = Mathf.Abs(name.GetHashCode() % 9973) * 0.017f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount <= 1 ? 1f : i / (sampleCount - 1f);
            float frequency = Mathf.Lerp(startHz, endHz, t);
            phase += 2f * Mathf.PI * frequency / FallbackSampleRate;

            float attackEnvelope = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            float releaseEnvelope = Mathf.Pow(1f - t, decayPower);
            float envelope = attackEnvelope * releaseEnvelope;
            float tone = Mathf.Sin(phase);
            float noise = Noise(i * 0.037f + seed) * 2f - 1f;
            samples[i] = Mathf.Clamp((tone * (1f - noiseMix) + noise * noiseMix) * envelope * gain, -1f, 1f);
        }

        var clip = AudioClip.Create(name, sampleCount, 1, FallbackSampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Noise(float value)
    {
        return Mathf.Repeat(Mathf.Sin(value * 12.9898f + 78.233f) * 43758.5453f, 1f);
    }

    private AudioSource GetSource()
    {
        if (sourcePool.Count > 0)
        {
            return sourcePool.Dequeue();
        }

        if (createdSources >= maxSources)
        {
            return null;
        }

        return CreateSource();
    }

    private AudioSource CreateSource()
    {
        var go = new GameObject("PooledAudioSource");
        go.transform.SetParent(sourceRoot, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        createdSources++;
        return source;
    }

    private IEnumerator ReturnAfter(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.03f, delay + 0.02f));
        ReturnSource(source);
    }

    private void ReturnSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        source.transform.SetParent(sourceRoot, false);
        sourcePool.Enqueue(source);
    }

    private bool CanPlay(BattleAudioCueId id, float interval)
    {
        if (interval <= 0f)
        {
            return true;
        }

        float lastTime;
        if (lastPlayTimes.TryGetValue(id, out lastTime) && Time.realtimeSinceStartup - lastTime < interval)
        {
            return false;
        }

        lastPlayTimes[id] = Time.realtimeSinceStartup;
        return true;
    }

    private AudioMixerGroup GetMixerGroup(BattleAudioChannel channel)
    {
        switch (channel)
        {
            case BattleAudioChannel.Bgm:
                return bgmGroup != null ? bgmGroup : masterGroup;
            case BattleAudioChannel.Weapon:
                return weaponGroup != null ? weaponGroup : sfxGroup;
            case BattleAudioChannel.Explosion:
                return explosionGroup != null ? explosionGroup : sfxGroup;
            case BattleAudioChannel.Creature:
                return creatureGroup != null ? creatureGroup : sfxGroup;
            case BattleAudioChannel.Magic:
                return magicGroup != null ? magicGroup : sfxGroup;
            case BattleAudioChannel.Ui:
                return uiGroup != null ? uiGroup : sfxGroup;
            case BattleAudioChannel.Voice:
                return voiceGroup != null ? voiceGroup : masterGroup;
            case BattleAudioChannel.Ambience:
                return ambienceGroup != null ? ambienceGroup : masterGroup;
            case BattleAudioChannel.Sfx:
                return sfxGroup != null ? sfxGroup : masterGroup;
            default:
                return masterGroup;
        }
    }

    private struct RuntimeAudioCue
    {
        public AudioClip[] clips;
        public BattleAudioChannel channel;
        public float volume;
        public float pitchJitter;
        public float minInterval;
        public bool spatial;
    }
}
