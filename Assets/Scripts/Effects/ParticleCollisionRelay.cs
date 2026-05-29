using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class ParticleCollisionRelay : MonoBehaviour
{
    [SerializeField] private BattleEffectId collisionEffect = BattleEffectId.BulletHitMetal;
    [SerializeField] private int maxEventsPerFrame = 6;

    private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    private ParticleSystem particleSystemCache;

    public void Configure(BattleEffectId effectId, int eventsPerFrame = -1)
    {
        collisionEffect = effectId == BattleEffectId.None ? BattleEffectId.BulletHitMetal : effectId;
        if (eventsPerFrame > 0)
        {
            maxEventsPerFrame = eventsPerFrame;
        }
    }

    private void Awake()
    {
        particleSystemCache = GetComponent<ParticleSystem>();
    }

    private void OnParticleCollision(GameObject other)
    {
        if (EffectManager.Instance == null || particleSystemCache == null)
        {
            return;
        }

        int count = particleSystemCache.GetCollisionEvents(other, collisionEvents);
        count = Mathf.Min(count, maxEventsPerFrame);
        for (int i = 0; i < count; i++)
        {
            EffectManager.Instance.Play(collisionEffect, collisionEvents[i].intersection);
        }
    }
}
