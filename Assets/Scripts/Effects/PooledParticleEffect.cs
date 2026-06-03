using System.Collections;
using UnityEngine;

public sealed class PooledParticleEffect : MonoBehaviour
{
    private ParticleSystem[] particleSystems;
    private NuclearMushroomCloudSprite[] mushroomSprites;
    private EffectManager owner;
    private BattleEffectId id;
    private Coroutine returnRoutine;

    public void Initialize(EffectManager effectOwner, BattleEffectId effectId)
    {
        owner = effectOwner;
        id = effectId;
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        mushroomSprites = GetComponentsInChildren<NuclearMushroomCloudSprite>(true);
    }

    public void Play(EffectPlayback playback)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        transform.SetParent(playback.parent, true);
        transform.position = playback.position;
        transform.rotation = playback.rotation;
        transform.localScale = Vector3.one * playback.scale;
        gameObject.SetActive(true);

        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        if (mushroomSprites == null || mushroomSprites.Length == 0)
        {
            mushroomSprites = GetComponentsInChildren<NuclearMushroomCloudSprite>(true);
        }

        float duration = 0.5f;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var system = particleSystems[i];
            if (system == null)
            {
                continue;
            }

            system.Clear(true);
            system.Play(true);
            duration = Mathf.Max(duration, EstimateParticleSystemDuration(system));
        }

        for (int i = 0; i < mushroomSprites.Length; i++)
        {
            var sprite = mushroomSprites[i];
            if (sprite == null)
            {
                continue;
            }

            sprite.gameObject.SetActive(true);
            duration = Mathf.Max(duration, sprite.EstimatedDuration);
        }

        returnRoutine = StartCoroutine(ReturnAfter(duration + 0.35f));
    }

    public void StopAndReturn()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        ReturnToPool();
    }

    private IEnumerator ReturnAfter(float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        returnRoutine = null;
        ReturnToPool();
    }

    private static float EstimateParticleSystemDuration(ParticleSystem system)
    {
        var main = system.main;
        float lifetimeMax = Mathf.Max(main.startLifetime.constant, main.startLifetime.constantMax);
        return main.duration + lifetimeMax;
    }

    private void ReturnToPool()
    {
        if (particleSystems != null)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null)
                {
                    particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        if (mushroomSprites != null)
        {
            for (int i = 0; i < mushroomSprites.Length; i++)
            {
                if (mushroomSprites[i] != null)
                {
                    mushroomSprites[i].gameObject.SetActive(false);
                }
            }
        }

        owner.ReturnToPool(id, this);
    }
}
