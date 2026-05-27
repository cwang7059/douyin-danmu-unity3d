using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class ApocalypseKingRuntimeProbe : MonoBehaviour
{
    private const float TimeoutSeconds = 60f;

    private IEnumerator Start()
    {
        if (!HasArgument("-apocalypseProbe"))
        {
            yield break;
        }

        var game = GetComponent<ApocalypseKingUnityGame>();
        float startedAt = Time.realtimeSinceStartup;
        while ((game == null || !game.DiagnosticsAssetsReady) && Time.realtimeSinceStartup - startedAt < TimeoutSeconds)
        {
            yield return null;
        }

        if (HasArgument("-probeDanmu"))
        {
            var queue = GetComponent<DanmuCommandQueue>();
            if (queue != null)
            {
                queue.EnqueueRawMessage("probe-human", "Probe Human", "human soldier");
                queue.EnqueueRawMessage("probe-orc", "Probe Orc", "orc helldog");
                queue.EnqueueRawMessage("probe-skill", "Probe Skill", "human air strike");
                queue.EnqueueRawMessage("probe-rage", "Probe Rage", "orc rage");
            }
        }

        float probeDelay = Mathf.Max(0f, GetArgumentFloat("-probeDelay", 1f));
        float previousTimeScale = Time.timeScale;
        Time.timeScale = Mathf.Clamp(GetArgumentFloat("-probeTimeScale", 1f), 0.05f, 20f);
        yield return new WaitForSecondsRealtime(probeDelay);
        Time.timeScale = previousTimeScale;
        float soldierBoneMotionAngle = 0f;
        float soldierBoneMotionDistance = 0f;
        float tankTrackMotionAngle = 0f;
        float tankTrackMotionDistance = 0f;
        yield return MeasureActiveTransformMotion(0.25f, IsSoldierLegTransform, (angle, distance) =>
        {
            soldierBoneMotionAngle = angle;
            soldierBoneMotionDistance = distance;
        });
        yield return MeasureActiveTransformMotion(0.25f, IsTankTrackTransform, (angle, distance) =>
        {
            tankTrackMotionAngle = angle;
            tankTrackMotionDistance = distance;
        });
        yield return new WaitForEndOfFrame();

        string outputPath = GetArgumentValue("-probeOutput");
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.Combine(Application.persistentDataPath, "apocalypse-preview.png");
        }

        bool captured = CaptureScreen(outputPath);
        LogDiagnostics(game, captured, outputPath, soldierBoneMotionAngle, soldierBoneMotionDistance, tankTrackMotionAngle, tankTrackMotionDistance);

        bool ok = game != null && game.DiagnosticsAssetsReady && game.DiagnosticsPrototypeCount >= 6 && captured;
        Application.Quit(ok ? 0 : 1);
    }

    private static bool CaptureScreen(string outputPath)
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        if (width <= 1 || height <= 1)
        {
            width = 1280;
            height = 720;
        }

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var previousActive = RenderTexture.active;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        try
        {
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            return File.Exists(outputPath) && new FileInfo(outputPath).Length > 1024;
        }
        finally
        {
            RenderTexture.active = previousActive;
            Destroy(texture);
        }
    }

    private static IEnumerator MeasureActiveTransformMotion(float seconds, Func<string, bool> shouldTrack, Action<float, float> onComplete)
    {
        var transforms = FindObjectsOfType<Transform>(true);
        var tracked = new System.Collections.Generic.List<Transform>();
        var rotations = new System.Collections.Generic.List<Quaternion>();
        var positions = new System.Collections.Generic.List<Vector3>();

        for (int i = 0; i < transforms.Length; i++)
        {
            var item = transforms[i];
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            string name = item.name;
            if (shouldTrack == null || !shouldTrack(name))
            {
                continue;
            }

            tracked.Add(item);
            rotations.Add(item.localRotation);
            positions.Add(item.localPosition);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, seconds));

        float maxAngle = 0f;
        float maxDistance = 0f;
        for (int i = 0; i < tracked.Count; i++)
        {
            var item = tracked[i];
            if (item == null || !item.gameObject.activeInHierarchy)
            {
                continue;
            }

            maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(rotations[i], item.localRotation));
            maxDistance = Mathf.Max(maxDistance, Vector3.Distance(positions[i], item.localPosition));
        }

        onComplete?.Invoke(maxAngle, maxDistance);
    }

    private static bool IsSoldierLegTransform(string name)
    {
        return name == "UpperLeg.L"
            || name == "UpperLeg.R"
            || name == "LowerLeg.L"
            || name == "LowerLeg.R"
            || name == "Foot.L"
            || name == "Foot.R";
    }

    private static bool IsTankTrackTransform(string name)
    {
        return !string.IsNullOrEmpty(name)
            && name.IndexOf("TankTrack", StringComparison.OrdinalIgnoreCase) >= 0
            && name.IndexOf("_end", StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static void LogDiagnostics(ApocalypseKingUnityGame game, bool captured, string outputPath, float soldierBoneMotionAngle, float soldierBoneMotionDistance, float tankTrackMotionAngle, float tankTrackMotionDistance)
    {
        var gateway = game != null ? game.GetComponent<DanmuHttpGateway>() : null;
        var renderers = FindObjectsOfType<Renderer>(true);
        int activeRenderers = 0;
        int transparentMaterials = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
            {
                activeRenderers++;
            }

            var materials = renderer.sharedMaterials;
            for (int m = 0; m < materials.Length; m++)
            {
                var material = materials[m];
                if (material == null)
                {
                    continue;
                }

                bool alphaColor = material.HasProperty("_Color") && material.color.a < 0.95f;
                bool transparentQueue = material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent;
                bool noDepthWrite = material.HasProperty("_ZWrite") && material.GetFloat("_ZWrite") < 0.5f;
                if (alphaColor || transparentQueue || noDepthWrite)
                {
                    transparentMaterials++;
                }
            }
        }

        Debug.Log(
            "[ApocalypseProbe] " +
            $"ready={(game != null && game.DiagnosticsAssetsReady)} " +
            $"prototypes={(game != null ? game.DiagnosticsPrototypeCount : 0)} " +
            $"activeUnits={(game != null ? game.DiagnosticsActiveUnitCount : 0)} " +
            $"giants={(game != null ? game.DiagnosticsGiantCount : 0)} " +
            $"giantHp={(game != null ? game.DiagnosticsGiantHp : 0f):0.0}/{(game != null ? game.DiagnosticsGiantMaxHp : 0f):0.0} " +
            $"danmuPending={(game != null ? game.DiagnosticsDanmuPending : 0)} " +
            $"danmuAccepted={(game != null ? game.DiagnosticsDanmuAccepted : 0)} " +
            $"danmuDropped={(game != null ? game.DiagnosticsDanmuDropped : 0)} " +
            $"httpGateway={(gateway != null && gateway.IsRunning)} " +
            $"httpPort={(gateway != null ? gateway.Port : 0)} " +
            $"httpReceived={(gateway != null ? gateway.ReceivedMessageCount : 0)} " +
            $"httpAccepted={(gateway != null ? gateway.AcceptedMessageCount : 0)} " +
            $"httpDropped={(gateway != null ? gateway.DroppedMessageCount : 0)} " +
            $"fallback={(game != null && game.DiagnosticsUsingFallback)} " +
            $"battleTime={(game != null ? game.DiagnosticsBattleTime : 0f):0.0} " +
            $"giantX={(game != null ? game.DiagnosticsGiantX : 0f):0.0} " +
            $"giantZ={(game != null ? game.DiagnosticsGiantZ : 0f):0.0} " +
            $"engaged={(game != null && game.DiagnosticsGiantEngaged)} " +
            $"contact={(game != null && game.DiagnosticsGiantContact)} " +
            $"tankOverlaps={(game != null ? game.DiagnosticsTankOverlapCount : 0)} " +
            $"minTankGap={(game != null ? game.DiagnosticsMinimumTankGap : 0f):0.0} " +
            $"tankHeading={(game != null ? game.DiagnosticsAverageTankHeading : 0f):0.0} " +
            $"tankMoveSpeed={(game != null ? game.DiagnosticsAverageTankMoveSpeed : 0f):0.0} " +
            $"tankAnimators={(game != null ? game.DiagnosticsTankAnimatorCount : 0)} " +
            $"tankAnimation={(game != null ? game.DiagnosticsFirstTankAnimation : string.Empty)} " +
            $"tankTrackMotionAngle={tankTrackMotionAngle:0.00} " +
            $"tankTrackMotionDistance={tankTrackMotionDistance:0.0000} " +
            $"soldierAnimators={(game != null ? game.DiagnosticsSoldierAnimatorCount : 0)} " +
            $"soldierAnimation={(game != null ? game.DiagnosticsFirstSoldierAnimation : string.Empty)} " +
            $"soldierBoneMotionAngle={soldierBoneMotionAngle:0.00} " +
            $"soldierBoneMotionDistance={soldierBoneMotionDistance:0.0000} " +
            $"screen={Screen.width}x{Screen.height} " +
            $"activeRenderers={activeRenderers} " +
            $"transparentMaterials={transparentMaterials} " +
            $"captured={captured} " +
            $"screenshot={outputPath}");
    }

    private static bool HasArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetArgumentValue(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return string.Empty;
    }

    private static float GetArgumentFloat(string name, float defaultValue)
    {
        string value = GetArgumentValue(name);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        float result;
        return float.TryParse(value, out result) ? result : defaultValue;
    }
}
