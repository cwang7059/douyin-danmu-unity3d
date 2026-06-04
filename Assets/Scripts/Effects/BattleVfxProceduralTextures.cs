using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime-generated particle textures when Resources/VFX/Online/Selected PNGs are missing.</summary>
public static class BattleVfxProceduralTextures
{
    private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static bool IsProceduralOnlyPath(string resourcesPath)
    {
        return !string.IsNullOrEmpty(resourcesPath)
            && resourcesPath.StartsWith("__proc/", System.StringComparison.Ordinal);
    }

    public static Texture2D Resolve(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath))
        {
            return null;
        }

        if (!IsProceduralOnlyPath(resourcesPath))
        {
            Texture2D loaded = Resources.Load<Texture2D>(resourcesPath);
            if (loaded != null)
            {
                return loaded;
            }
        }

        Texture2D cached;
        if (Cache.TryGetValue(resourcesPath, out cached) && cached != null)
        {
            return cached;
        }

        cached = CreateForPath(resourcesPath);
        if (cached != null)
        {
            Cache[resourcesPath] = cached;
        }

        return cached;
    }

    private static Texture2D CreateForPath(string path)
    {
        string name = path;
        int slash = path.LastIndexOf('/');
        if (slash >= 0 && slash < path.Length - 1)
        {
            name = path.Substring(slash + 1);
        }

        if (name.Contains("fire_glow"))
        {
            return CreateSoftCircle(128, new Color(1f, 0.78f, 0.28f, 1f), 0.1f);
        }

        if (name.Contains("smoke"))
        {
            return CreateSoftCircle(96, name.Contains("black") ? new Color(0.14f, 0.13f, 0.12f, 1f) : new Color(0.92f, 0.92f, 0.9f, 1f));
        }

        if (name.Contains("shockwave") || name.Contains("ring"))
        {
            return CreateRing(128, new Color(1f, 0.78f, 0.32f, 1f), 0.55f, 0.88f);
        }

        if (name.Contains("muzzle"))
        {
            return CreateStreak(128, 64, new Color(1f, 0.72f, 0.22f, 1f), name.Contains("tank"));
        }

        if (name.Contains("mushroom_cloud"))
        {
            return CreateSoftCircle(128, new Color(0.82f, 0.78f, 0.72f, 1f), 0.12f);
        }

        if (name.Contains("mushroom_smoke"))
        {
            return CreateSoftCircle(96, new Color(0.42f, 0.38f, 0.34f, 1f));
        }

        if (name.Contains("explosion") || name.Contains("sinestesia") || name.Contains("fireball") || name.Contains("bomb") || name.Contains("nuclear"))
        {
            int cells = name.Contains("large") || name.Contains("bomb") || name.Contains("nuclear") ? 8 : 4;
            float intensity = name.Contains("nuclear") ? 1.55f : name.Contains("bomb") ? 1.25f : name.Contains("large") ? 1.05f : 0.9f;
            return CreateExplosionSheet(cells, cells, intensity);
        }

        if (name.Contains("flash") || name.Contains("impact"))
        {
            return CreateSoftCircle(64, new Color(1f, 0.92f, 0.55f, 1f), 0.08f);
        }

        return CreateSoftCircle(64, new Color(1f, 0.55f, 0.18f, 1f));
    }

    private static Texture2D CreateSoftCircle(int size, Color core, float hardInner = 0.18f)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.48f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(hardInner, 1f, dist));
                alpha *= alpha;
                Color c = core;
                c.a *= alpha;
                pixels[y * size + x] = c;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.name = "VFXProc_SoftCircle";
        return texture;
    }

    private static Texture2D CreateRing(int size, Color tint, float inner, float outer)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.48f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float band = Mathf.Clamp01(1f - Mathf.Abs(dist - ((inner + outer) * 0.5f)) / ((outer - inner) * 0.5f));
                Color c = tint;
                c.a = band * band;
                pixels[y * size + x] = c;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.name = "VFXProc_Ring";
        return texture;
    }

    private static Texture2D CreateStreak(int width, int height, Color tint, bool wide)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            float ny = (y / (height - 1f)) * 2f - 1f;
            for (int x = 0; x < width; x++)
            {
                float nx = x / (width - 1f);
                float core = Mathf.Exp(-Mathf.Abs(ny) * (wide ? 2.2f : 4.5f)) * Mathf.Exp(-Mathf.Pow((nx - 0.12f) / (wide ? 0.22f : 0.14f), 2f) * 6f);
                Color c = Color.Lerp(tint, Color.white, core * 0.65f);
                c.a = Mathf.Clamp01(core);
                pixels[y * width + x] = c;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.name = "VFXProc_Streak";
        return texture;
    }

    private static Texture2D CreateExplosionSheet(int cols, int rows, float intensity)
    {
        int cell = 64;
        int width = cols * cell;
        int height = rows * cell;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[width * height];
        int frameCount = cols * rows;
        for (int frame = 0; frame < frameCount; frame++)
        {
            int fx = frame % cols;
            int fy = frame / cols;
            float progress = frame / (float)Mathf.Max(1, frameCount - 1);
            float radius = Mathf.Lerp(0.08f, 0.46f, progress) * intensity;
            float alpha = Mathf.Clamp01(1.15f - progress * 1.05f);
            for (int y = 0; y < cell; y++)
            {
                for (int x = 0; x < cell; x++)
                {
                    float dx = (x / (cell - 1f) - 0.5f) / radius;
                    float dy = (y / (cell - 1f) - 0.5f) / radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float core = Mathf.Clamp01(1f - dist);
                    core = core * core * core;
                    Color hot = Color.Lerp(new Color(1f, 0.92f, 0.45f, 1f), new Color(1f, 0.28f, 0.04f, 1f), progress);
                    Color c = hot;
                    c.a = core * alpha;
                    int px = fx * cell + x;
                    int py = fy * cell + y;
                    pixels[py * width + px] = c;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        texture.name = "VFXProc_ExplosionSheet";
        return texture;
    }
}
