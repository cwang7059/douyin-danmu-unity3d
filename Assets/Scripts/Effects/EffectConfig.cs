using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Effect Config")]
public sealed class EffectConfig : ScriptableObject
{
    public BattleEffectId id;
    public GameObject prefab;
    public int prewarmCount = 8;
    public int maxCount = 64;
    public bool attachToParent;
    public bool allowParticleCollision;
    public AudioClip[] sounds;
}

[Serializable]
public struct EffectPlayback
{
    public BattleEffectId id;
    public Vector3 position;
    public Quaternion rotation;
    public Transform parent;
    public float scale;

    public static EffectPlayback Create(BattleEffectId id, Vector3 position, Quaternion rotation, Transform parent = null, float scale = 1f)
    {
        return new EffectPlayback
        {
            id = id,
            position = position,
            rotation = rotation,
            parent = parent,
            scale = Mathf.Max(0.01f, scale),
        };
    }
}

