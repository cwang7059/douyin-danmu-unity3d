using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class BattleAudioManager : MonoBehaviour
{
    [SerializeField] private AudioCueConfig[] cues;
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
        if (!cueById.TryGetValue(id, out cue) || cue == null || cue.clips == null || cue.clips.Length == 0)
        {
            return null;
        }

        if (!CanPlay(id, cue.minInterval))
        {
            return null;
        }

        AudioClip clip = cue.clips[Random.Range(0, cue.clips.Length)];
        return PlayOneShot(clip, position, cue.channel, cue.volume, cue.spatial, cue.pitchJitter, cue.mixerGroup);
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
}
