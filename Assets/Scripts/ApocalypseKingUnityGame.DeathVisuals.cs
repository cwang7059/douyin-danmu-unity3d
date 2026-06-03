using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ApocalypseKingUnityGame
{
    private const string GiantDeathResourceModelPath = GiantPixelhouseResourceFolderPath + "/ZombieDead";
    private const float GiantCorpseLayPitch = 88f;
    private const float GiantCorpseLayRoll = -4f;

    private GameObject giantDeathVisualPrototype;
    private AnimationClip cachedGiantDeathClip;

    private bool TryBuildGiantDeathVisual(Transform root)
    {
        GameObject prototype = EnsureGiantDeathVisualPrototype();
        if (prototype == null)
        {
            return false;
        }

        var corpse = Instantiate(prototype, root, false);
        corpse.name = "GiantCorpse";
        corpse.transform.localPosition = Vector3.zero;
        corpse.transform.localRotation = Quaternion.identity;
        corpse.transform.localScale = Vector3.one;
        corpse.SetActive(true);
        return true;
    }

    private GameObject EnsureGiantDeathVisualPrototype()
    {
        if (giantDeathVisualPrototype != null)
        {
            return giantDeathVisualPrototype;
        }

        GameObject corpse = CreateGiantDeathResourcePrototype(GiantDeathResourceModelPath);
        string sourceName = corpse != null ? corpse.name : string.Empty;
        if (corpse == null)
        {
            GameObject source = ResolveGiantDeathVisualSource();
            if (source == null)
            {
                return null;
            }

            corpse = Instantiate(source, modelCacheRoot, false);
            sourceName = source.name;
        }

        corpse.name = "GiantDeathVisual_Prototype";
        corpse.hideFlags = HideFlags.HideInHierarchy;
        PrepareGiantCorpsePrototype(corpse, sourceName);
        corpse.SetActive(false);
        giantDeathVisualPrototype = corpse;
        return giantDeathVisualPrototype;
    }

    private GameObject ResolveGiantDeathVisualSource()
    {
        if (modelPrototypes.TryGetValue(UnitKind.Giant, out GameObject giantPrototype) && giantPrototype != null)
        {
            return giantPrototype;
        }

        for (int i = 0; i < giantVariantPrototypes.Count; i++)
        {
            if (giantVariantPrototypes[i] != null)
            {
                return giantVariantPrototypes[i];
            }
        }

        return null;
    }

    private GameObject CreateGiantDeathResourcePrototype(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        var source = Resources.Load<GameObject>(resourcePath);
        if (source == null)
        {
            return null;
        }

        var instance = Instantiate(source, modelCacheRoot, false);
        instance.name = $"{UnitKind.Giant}_DeathSource_{SanitizeResourceToken(resourcePath)}";
        instance.hideFlags = HideFlags.HideInHierarchy;
        ConfigureImportedPrototype(instance, UnitKind.Giant);
        if (resourcePath.StartsWith(GiantPixelhouseResourceFolderPath, StringComparison.Ordinal))
        {
            ApplyPixelhouseZombieMaterials(instance);
        }

        instance.SetActive(false);
        return instance;
    }

    private void PrepareGiantCorpsePrototype(GameObject corpse, string sourceName)
    {
        if (corpse == null)
        {
            return;
        }

        StripCollidersRecursive(corpse);
        RemoveGiantStrayGeometry(corpse);

        bool pixelhouse = sourceName.IndexOf("Pixelhouse", StringComparison.OrdinalIgnoreCase) >= 0
            || sourceName.IndexOf("Zombie", StringComparison.OrdinalIgnoreCase) >= 0;
        if (pixelhouse || UnitModelUsesAuthoredTextures(corpse))
        {
            ApplyPixelhouseZombieMaterials(corpse);
        }

        TryFreezeGiantCorpsePose(corpse);
        DisableCorpseAnimators(corpse);

        var clipStore = corpse.GetComponent<RuntimeAnimationClipStore>();
        if (clipStore != null)
        {
            Destroy(clipStore);
        }

        corpse.transform.localRotation = Quaternion.Euler(GiantCorpseLayPitch, 0f, GiantCorpseLayRoll);
    }

    private void TryFreezeGiantCorpsePose(GameObject corpse)
    {
        AnimationClip deadClip = FindGiantDeathAnimationClip(corpse) ?? GetCachedGiantDeathClip();
        Animation host = FindGiantAnimationHost(corpse);
        if (host == null || deadClip == null)
        {
            return;
        }

        host.playAutomatically = false;
        host.cullingType = AnimationCullingType.AlwaysAnimate;
        if (host.GetClip(deadClip.name) == null)
        {
            host.AddClip(deadClip, deadClip.name);
        }

        AnimationState state = host[deadClip.name];
        if (state == null)
        {
            return;
        }

        state.enabled = true;
        state.weight = 1f;
        state.normalizedTime = 0.99f;
        state.wrapMode = WrapMode.ClampForever;
        host.Play(deadClip.name);
        host.Sample();
        host.Stop();
    }

    private AnimationClip FindGiantDeathAnimationClip(GameObject model)
    {
        AnimationClip[] clips = CollectRuntimeAnimationClips(model);
        AnimationClip best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            int score = ScoreGiantDeathClipName(clip.name);
            if (score > bestScore)
            {
                bestScore = score;
                best = clip;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static int ScoreGiantDeathClipName(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            return 0;
        }

        string lower = clipName.ToLowerInvariant();
        if (lower.Contains("dead"))
        {
            return 100;
        }

        if (lower.Contains("death") || lower.Contains("die") || lower.Contains("fall"))
        {
            return 80;
        }

        if (lower.Contains("down") || lower.Contains("hurt"))
        {
            return 40;
        }

        return 0;
    }

    private AnimationClip GetCachedGiantDeathClip()
    {
        if (cachedGiantDeathClip != null)
        {
            return cachedGiantDeathClip;
        }

        var source = Resources.Load<GameObject>(GiantDeathResourceModelPath);
        if (source == null)
        {
            return null;
        }

        var temp = Instantiate(source, modelCacheRoot, false);
        temp.hideFlags = HideFlags.HideInHierarchy;
        cachedGiantDeathClip = FindGiantDeathAnimationClip(temp);
        if (cachedGiantDeathClip == null)
        {
            AnimationClip[] clips = CollectRuntimeAnimationClips(temp);
            if (clips.Length > 0)
            {
                cachedGiantDeathClip = clips[clips.Length - 1];
            }
        }

        Destroy(temp);
        return cachedGiantDeathClip;
    }

    private static void DisableCorpseAnimators(GameObject corpse)
    {
        Animator[] animators = corpse.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].enabled = false;
            }
        }

        Animation[] animations = corpse.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            if (animations[i] != null)
            {
                animations[i].enabled = false;
            }
        }
    }

    private void GroundDeathVisualRoot(GameObject root, float groundHeight)
    {
        if (root == null || !TryComputeModelBounds(root, out Bounds bounds))
        {
            return;
        }

        Vector3 position = root.transform.position;
        position.y += groundHeight - bounds.min.y;
        root.transform.position = position;
    }
}
