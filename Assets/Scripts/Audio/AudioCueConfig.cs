using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Battle/Audio Cue Config")]
public sealed class AudioCueConfig : ScriptableObject
{
    public BattleAudioCueId id;
    public AudioClip[] clips;
    public BattleAudioChannel channel = BattleAudioChannel.Sfx;
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitchJitter = 0.04f;
    public float minInterval = 0.08f;
    public bool spatial;
}

