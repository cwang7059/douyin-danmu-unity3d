using UnityEngine;

/// <summary>Camera-facing quad for cropped nuclear mushroom cloud photo.</summary>
public sealed class NuclearMushroomCloudSprite : MonoBehaviour
{
    private float duration = 9f;
    private float riseMeters = 12f;
    private float scaleStart = 0.22f;
    private float scaleEnd = 1.35f;
    private float startDelay = 0.55f;
    private float fadeInSeconds = 0.45f;
    private Vector3 baseLocalScale = Vector3.one;
    private Vector3 startLocalPosition;
    private Material instanceMaterial;
    private Renderer targetRenderer;
    private float elapsed;
    private bool configured;

    public float EstimatedDuration => startDelay + duration + 0.5f;

    public void Configure(
        float playDuration,
        float rise,
        float startScale,
        float endScale,
        Vector3 localScale,
        Vector3 localPosition,
        float delaySeconds = 0.55f)
    {
        duration = Mathf.Max(1f, playDuration);
        riseMeters = rise;
        scaleStart = startScale;
        scaleEnd = endScale;
        baseLocalScale = localScale;
        startLocalPosition = localPosition;
        startDelay = Mathf.Max(0f, delaySeconds);
        configured = true;
    }

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            instanceMaterial = new Material(targetRenderer.sharedMaterial);
            instanceMaterial.renderQueue = 3001;
            targetRenderer.material = instanceMaterial;
        }
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (!configured)
        {
            baseLocalScale = transform.localScale;
            startLocalPosition = transform.localPosition;
        }

        transform.localPosition = startLocalPosition;
        transform.localScale = baseLocalScale * scaleStart;
        SetAlpha(0f);
        if (targetRenderer != null)
        {
            targetRenderer.enabled = false;
        }
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed < startDelay)
        {
            return;
        }

        if (targetRenderer != null && !targetRenderer.enabled)
        {
            targetRenderer.enabled = true;
        }

        float t = Mathf.Clamp01((elapsed - startDelay) / duration);
        float eased = 1f - Mathf.Pow(1f - t, 2.1f);
        transform.localPosition = startLocalPosition + Vector3.up * (riseMeters * eased);
        transform.localScale = baseLocalScale * Mathf.Lerp(scaleStart, scaleEnd, eased);
        AlignToGameplayCamera();

        float alpha = Mathf.Clamp01((elapsed - startDelay) / fadeInSeconds);
        if (t > 0.72f)
        {
            alpha *= 1f - Mathf.SmoothStep(0.72f, 1f, t);
        }

        SetAlpha(alpha);
    }

    private void AlignToGameplayCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            transform.rotation = Quaternion.Euler(26f, -12f, 0f);
            return;
        }

        Vector3 toCam = cam.transform.position - transform.position;
        if (toCam.sqrMagnitude < 0.01f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
    }

    private void SetAlpha(float alpha)
    {
        if (instanceMaterial == null)
        {
            return;
        }

        Color c = Color.white;
        c.a = Mathf.Clamp01(alpha);
        if (instanceMaterial.HasProperty("_Color"))
        {
            instanceMaterial.SetColor("_Color", c);
        }

        if (instanceMaterial.HasProperty("_TintColor"))
        {
            instanceMaterial.SetColor("_TintColor", c);
        }
    }
}
