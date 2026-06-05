using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const int PrewarmFireballProjectiles = 24;
    private const string UrpFireballResourcePath = "Battle/VFX/UrpPterosaurFireball";
    private const float PterosaurFireballUrpVisualScale = 1.15f;
    private const float PterosaurFireballFallbackVisualScale = 1.55f;
    private const float PterosaurFireballTrailInterval = 0.1f;
    private static readonly Color PterosaurFireProjectileColor = new Color(0.92f, 0.08f, 0.04f, 1f);
    private static readonly Color PterosaurFireballDeepRedTint = new Color(0.92f, 0.08f, 0.04f, 1f);

    private static GameObject urpFireballPrefabCache;
    private GameObject pterosaurFireballVfxPrototype;
    private bool? pterosaurFireballUsesUrpPrefab;

    private void PrewarmPterosaurFireballProjectiles()
    {
        if (pterosaurFireballVfxPrototype == null)
        {
            pterosaurFireballVfxPrototype = CreatePterosaurFireballVfxTemplate();
        }

        PrewarmProjectiles(ProjectileKind.Fireball, PrewarmFireballProjectiles, PterosaurFireProjectileColor);
    }

    private bool PterosaurFireballUsesUrpPrefab()
    {
        if (!pterosaurFireballUsesUrpPrefab.HasValue)
        {
            pterosaurFireballUsesUrpPrefab = TryLoadUrpFireballPrefab() != null;
        }

        return pterosaurFireballUsesUrpPrefab.Value;
    }

    private float GetPterosaurFireballVisualScale()
    {
        return PterosaurFireballUsesUrpPrefab()
            ? PterosaurFireballUrpVisualScale
            : PterosaurFireballFallbackVisualScale;
    }

    private static GameObject TryLoadUrpFireballPrefab()
    {
        if (urpFireballPrefabCache != null)
        {
            return urpFireballPrefabCache;
        }

        urpFireballPrefabCache = Resources.Load<GameObject>(UrpFireballResourcePath);
        return urpFireballPrefabCache;
    }

    private GameObject CreatePterosaurFireballVfxTemplate()
    {
        GameObject urpSource = TryLoadUrpFireballPrefab();
        if (urpSource != null)
        {
            var urpRoot = Instantiate(urpSource, modelCacheRoot, false);
            urpRoot.name = "PterosaurUrpFireball_Template";
            urpRoot.SetActive(false);
            StripFireballSceneLights(urpRoot);
            ApplyPterosaurFireballDeepRedTint(urpRoot);
            return urpRoot;
        }

        var root = new GameObject("PterosaurFireballVfx_Template");
        root.transform.SetParent(modelCacheRoot, false);
        root.SetActive(false);
        EffectManager.BuildPterosaurFireballProjectileVfx(root.transform);
        return root;
    }

    private void EnsureFireballProjectileVisual(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        EnsureFireballProjectileHeadAnchor(projectile);
        DisableFireballProjectileTrailRenderer(projectile);
        if (projectile.head == null)
        {
            projectile.usesFireballParticleVisual = false;
            return;
        }

        if (pterosaurFireballVfxPrototype == null)
        {
            pterosaurFireballVfxPrototype = CreatePterosaurFireballVfxTemplate();
        }

        if (pterosaurFireballVfxPrototype == null)
        {
            projectile.usesFireballParticleVisual = false;
            return;
        }

        Transform existingVfx = projectile.head.Find("FireballVfx");
        if (existingVfx != null)
        {
            projectile.usesFireballParticleVisual = true;
            projectile.head.localScale = Vector3.one * GetPterosaurFireballVisualScale();
            if (projectile.line != null)
            {
                projectile.line.enabled = false;
            }

            RestartFireballParticleSystems(existingVfx.gameObject);
            return;
        }

        for (int i = projectile.head.childCount - 1; i >= 0; i--)
        {
            Transform child = projectile.head.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        var visual = Instantiate(pterosaurFireballVfxPrototype, projectile.head, false);
        visual.name = "FireballVfx";
        visual.SetActive(true);
        projectile.usesFireballParticleVisual = true;
        projectile.head.localScale = Vector3.one * GetPterosaurFireballVisualScale();
        if (projectile.line != null)
        {
            projectile.line.enabled = false;
        }

        RestartFireballParticleSystems(visual);
    }

    private static void DisableFireballProjectileTrailRenderer(ProjectileView projectile)
    {
        if (projectile?.root == null)
        {
            return;
        }

        TrailRenderer trail = projectile.root.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
            trail.enabled = false;
        }
    }

    private static void StripFireballSceneLights(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = false;
            }
        }
    }

    private static void ApplyPterosaurFireballDeepRedTint(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null)
            {
                continue;
            }

            if (ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear(true);
            }

            ParticleSystem.MainModule main = ps.main;
            ParticleSystem.MinMaxGradient startColor = main.startColor;
            if (startColor.mode == ParticleSystemGradientMode.Color)
            {
                main.startColor = TintFireballColor(startColor.color);
            }
            else if (startColor.mode == ParticleSystemGradientMode.TwoColors)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(
                    TintFireballColor(startColor.colorMin),
                    TintFireballColor(startColor.colorMax));
            }
        }
    }

    private static Color TintFireballColor(Color original)
    {
        return new Color(
            Mathf.Lerp(original.r, PterosaurFireballDeepRedTint.r, 0.55f),
            Mathf.Lerp(original.g, PterosaurFireballDeepRedTint.g, 0.55f),
            Mathf.Lerp(original.b, PterosaurFireballDeepRedTint.b, 0.55f),
            original.a);
    }

    private static void RestartFireballParticleSystems(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null)
            {
                continue;
            }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void EnsureFireballProjectileHeadAnchor(ProjectileView projectile)
    {
        if (projectile == null || projectile.root == null)
        {
            return;
        }

        // 仅当锚点缺失或旧版布局（锚点自身带 Mesh/Renderer）时重建；勿因子物体 FireballVfx 含粒子而销毁 head。
        bool needsFreshAnchor = projectile.head == null
            || !string.Equals(projectile.head.name, "FireballBody", System.StringComparison.Ordinal);
        if (!needsFreshAnchor && projectile.head.GetComponent<Renderer>() != null)
        {
            needsFreshAnchor = true;
        }

        if (!needsFreshAnchor)
        {
            return;
        }

        if (projectile.head != null)
        {
            Destroy(projectile.head.gameObject);
            projectile.head = null;
        }

        var headAnchor = new GameObject("FireballBody");
        headAnchor.transform.SetParent(projectile.root.transform, false);
        headAnchor.transform.localPosition = Vector3.zero;
        headAnchor.transform.localRotation = Quaternion.identity;
        headAnchor.transform.localScale = Vector3.one;
        projectile.head = headAnchor.transform;
    }
}
