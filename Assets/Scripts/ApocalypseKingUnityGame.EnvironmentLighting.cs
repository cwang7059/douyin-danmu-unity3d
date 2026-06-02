using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
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

        const float dayBlend = 1f;
        sunLightRoot.rotation = Quaternion.Euler(52f, -58f, 0f);

        Color dayLight = new Color(1f, 0.98f, 0.92f, 1f);
        sunLight.color = Color.Lerp(dayLight, new Color(1f, 0.94f, 0.82f, 1f), nuclearFlashBlend * 0.85f);
        sunLight.intensity = 1.28f + nuclearFlashBlend * 2.8f;

        Color dayAmbient = new Color(0.62f, 0.66f, 0.60f, 1f);
        RenderSettings.ambientLight = Color.Lerp(dayAmbient, new Color(0.95f, 0.88f, 0.78f, 1f), nuclearFlashBlend * 0.72f);

        Color dayFog = new Color(0.74f, 0.84f, 0.92f, 1f);
        RenderSettings.fogColor = dayFog;
        RenderSettings.fogDensity = 0.0052f;

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.Lerp(dayFog, new Color(1f, 0.92f, 0.78f, 1f), nuclearFlashBlend * 0.55f);
        }

        ApplyDaySkyboxSettings();

        if (environmentSunDisk != null)
        {
            environmentSunDisk.localPosition = new Vector3(18f, 13.5f, 60f);
            environmentSunDisk.localScale = new Vector3(4.2f, 4.2f, 0.35f);
            var renderer = environmentSunDisk.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = new Color(1f, 0.92f, 0.55f, 1f);
            }
        }
    }

    private void ApplyDaySkyboxSettings()
    {
        if (runtimeSkyboxMaterial == null)
        {
            return;
        }

        if (runtimeSkyboxMaterial.HasProperty("_Tint"))
        {
            runtimeSkyboxMaterial.SetColor("_Tint", new Color(0.90f, 0.95f, 1f, 1f));
        }

        if (runtimeSkyboxMaterial.HasProperty("_Exposure"))
        {
            runtimeSkyboxMaterial.SetFloat("_Exposure", 1.08f);
        }
    }

}
