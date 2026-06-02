using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private const float EnvironmentDayNightCycleSeconds = 480f;

    private Light sunLight;
    private Transform sunLightRoot;
    private Transform environmentSunDisk;
    private Material runtimeSkyboxMaterial;
    private float nuclearFlashTimer;

    private void RegisterSunLight(Light light, Transform root)
    {
        sunLight = light;
        sunLightRoot = root;
    }

    private void RegisterEnvironmentSunDisk(Transform disk)
    {
        environmentSunDisk = disk;
    }

    private void TriggerNuclearFlash(float durationSeconds)
    {
        nuclearFlashTimer = Mathf.Max(nuclearFlashTimer, durationSeconds);
    }

    private void UpdateBattlefieldEnvironment(float time)
    {
        if (sunLight == null || sunLightRoot == null)
        {
            return;
        }

        float nuclearFlashBlend = 0f;
        if (nuclearFlashTimer > 0f)
        {
            nuclearFlashTimer = Mathf.Max(0f, nuclearFlashTimer - Time.deltaTime);
            nuclearFlashBlend = Mathf.Clamp01(nuclearFlashTimer / 2.4f);
        }

        float phase = Mathf.Repeat(time / Mathf.Max(30f, EnvironmentDayNightCycleSeconds), 1f);
        float sunArc = Mathf.Sin(phase * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float dayBlend = Mathf.Clamp01((sunArc + 0.18f) / 0.82f);
        float duskBlend = Mathf.Clamp01(1f - Mathf.Abs(phase - 0.5f) / 0.18f);
        float nightBlend = 1f - dayBlend;

        float sunElevation = Mathf.Lerp(-12f, 58f, dayBlend);
        float sunAzimuth = Mathf.Lerp(-118f, -48f, phase);
        sunLightRoot.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);

        Color dayLight = new Color(1f, 0.97f, 0.90f, 1f);
        Color duskLight = new Color(1f, 0.68f, 0.38f, 1f);
        Color nightLight = new Color(0.62f, 0.74f, 1f, 1f);
        Color lightColor = Color.Lerp(nightLight, Color.Lerp(duskLight, dayLight, dayBlend), nightBlend * 0.65f);
        if (duskBlend > 0.01f && dayBlend > 0.12f)
        {
            lightColor = Color.Lerp(lightColor, duskLight, duskBlend * 0.55f);
        }

        sunLight.color = Color.Lerp(lightColor, new Color(1f, 0.94f, 0.82f, 1f), nuclearFlashBlend * 0.85f);
        sunLight.intensity = Mathf.Lerp(0.22f, 1.22f, dayBlend) + duskBlend * 0.08f + nuclearFlashBlend * 2.8f;

        Color dayAmbient = new Color(0.58f, 0.62f, 0.56f, 1f);
        Color duskAmbient = new Color(0.52f, 0.40f, 0.34f, 1f);
        Color nightAmbient = new Color(0.14f, 0.17f, 0.28f, 1f);
        Color ambient = Color.Lerp(nightAmbient, Color.Lerp(duskAmbient, dayAmbient, dayBlend), nightBlend * 0.7f);
        RenderSettings.ambientLight = Color.Lerp(ambient, new Color(0.95f, 0.88f, 0.78f, 1f), nuclearFlashBlend * 0.72f);

        Color dayFog = new Color(0.72f, 0.82f, 0.90f, 1f);
        Color duskFog = new Color(0.58f, 0.42f, 0.32f, 1f);
        Color nightFog = new Color(0.08f, 0.11f, 0.20f, 1f);
        RenderSettings.fogColor = Color.Lerp(nightFog, Color.Lerp(duskFog, dayFog, dayBlend), nightBlend * 0.72f);
        RenderSettings.fogDensity = Mathf.Lerp(0.011f, 0.0058f, dayBlend);

        if (mainCamera != null)
        {
            Color sky = Color.Lerp(nightFog, Color.Lerp(duskFog, dayFog, dayBlend), nightBlend * 0.5f);
            mainCamera.backgroundColor = Color.Lerp(sky, new Color(1f, 0.92f, 0.78f, 1f), nuclearFlashBlend * 0.55f);
        }

        ApplySkyboxForPhase(dayBlend, duskBlend, nightBlend);

        if (environmentSunDisk != null)
        {
            float diskX = Mathf.Lerp(-34f, 30f, phase);
            float diskY = Mathf.Lerp(4.5f, 14.5f, dayBlend) + duskBlend * 2.2f;
            float diskZ = Mathf.Lerp(52f, 64f, dayBlend);
            environmentSunDisk.localPosition = new Vector3(diskX, diskY, diskZ);
            float diskScale = Mathf.Lerp(2.6f, 4.4f, dayBlend + duskBlend * 0.35f);
            environmentSunDisk.localScale = new Vector3(diskScale, diskScale, 0.35f);
            var renderer = environmentSunDisk.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = Color.Lerp(
                    new Color(0.75f, 0.82f, 1f, 1f),
                    new Color(1f, 0.55f, 0.18f, 1f),
                    1f - dayBlend);
            }
        }
    }

    private void ApplySkyboxForPhase(float dayBlend, float duskBlend, float nightBlend)
    {
        if (runtimeSkyboxMaterial == null)
        {
            return;
        }

        Color dayTint = new Color(0.92f, 0.96f, 1f, 1f);
        Color duskTint = new Color(1f, 0.72f, 0.52f, 1f);
        Color nightTint = new Color(0.42f, 0.48f, 0.72f, 1f);
        Color tint = Color.Lerp(nightTint, Color.Lerp(duskTint, dayTint, dayBlend), nightBlend * 0.75f);
        if (runtimeSkyboxMaterial.HasProperty("_Tint"))
        {
            runtimeSkyboxMaterial.SetColor("_Tint", tint);
        }

        if (runtimeSkyboxMaterial.HasProperty("_Exposure"))
        {
            float exposure = Mathf.Lerp(0.42f, 0.95f, dayBlend) + duskBlend * 0.12f;
            runtimeSkyboxMaterial.SetFloat("_Exposure", exposure);
        }
    }

}
