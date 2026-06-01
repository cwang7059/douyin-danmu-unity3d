using UnityEngine;
using UnityEngine.UI;

public sealed partial class ApocalypseKingUnityGame
{
    private static readonly float[] ModeBaseHpPresets = { 80000f, 100000f, 150000f };
    private static readonly float[] ModeDurationPresets = { 300f, 600f, 900f };

    private GameObject modeSelectOverlay;
    private Text modeSelectInfoLabel;
    private Image greenPowerFill;
    private int modeBaseHpPresetIndex = 1;
    private int modeDurationPresetIndex = 1;
    internal string lastGiftFeedMessage = string.Empty;
    internal float giftFeedDisplayTimer;

    private static readonly Color GreenBarColor = new Color(0.35f, 0.95f, 0.45f, 1f);

    private void EnsureModeSelectUi()
    {
        if (modeSelectOverlay != null)
        {
            return;
        }

        Transform parent = hudRoot != null ? hudRoot : canvas != null ? canvas.transform : transform;
        modeSelectOverlay = new GameObject("ModeSelectOverlay", typeof(RectTransform));
        modeSelectOverlay.transform.SetParent(parent, false);
        var overlayRect = modeSelectOverlay.GetComponent<RectTransform>();
        SetAnchors(overlayRect, 0f, 0f, 1f, 1f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var backdrop = CreatePanel(modeSelectOverlay.transform, "Backdrop", new Color(0.02f, 0.04f, 0.08f, 0.82f));
        SetAnchors(backdrop.rectTransform, 0f, 0f, 1f, 1f);
        backdrop.rectTransform.offsetMin = Vector2.zero;
        backdrop.rectTransform.offsetMax = Vector2.zero;

        var card = CreatePanel(modeSelectOverlay.transform, "ModeCard", new Color(0.06f, 0.09f, 0.14f, 0.94f));
        SetAnchors(card.rectTransform, 0.08f, 0.28f, 0.92f, 0.78f);

        var title = CreateText(card.transform, "Title", "末日之王 · 模式选择", 26, new Color(1f, 0.88f, 0.42f, 1f), TextAnchor.UpperCenter);
        SetAnchors(title.rectTransform, 0.06f, 0.82f, 0.94f, 0.96f);
        ConfigureTextFit(title, 18, 26);

        modeSelectInfoLabel = CreateText(card.transform, "ModeInfo", BuildModeSelectInfoText(), 16, Color.white, TextAnchor.UpperLeft);
        SetAnchors(modeSelectInfoLabel.rectTransform, 0.08f, 0.12f, 0.92f, 0.82f);
        ConfigureTextFit(modeSelectInfoLabel, 12, 16);
        modeSelectInfoLabel.alignment = TextAnchor.UpperLeft;
        modeSelectInfoLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        modeSelectInfoLabel.verticalOverflow = VerticalWrapMode.Overflow;

        var hint = CreateText(card.transform, "Hint", "Enter 开始对战", 20, new Color(0.75f, 1f, 0.8f, 1f), TextAnchor.LowerCenter);
        SetAnchors(hint.rectTransform, 0.1f, 0.02f, 0.9f, 0.12f);
        ConfigureTextFit(hint, 14, 20);
    }

    private Transform ResolveTopHpBarRoot()
    {
        if (humanPowerFill != null)
        {
            return humanPowerFill.transform.parent;
        }

        if (hudRoot != null)
        {
            Transform topDynamic = hudRoot.Find("TopDynamicRoot");
            if (topDynamic != null)
            {
                return topDynamic;
            }
        }

        return hudRoot;
    }

    private Transform ResolveTopHpBarBackRoot(Transform topDynamic)
    {
        if (topDynamic != null && topDynamic.parent != null)
        {
            Transform topPanel = topDynamic.parent.Find("TopPanel");
            if (topPanel != null)
            {
                return topPanel;
            }
        }

        return topDynamic;
    }

    private void CleanupMisplacedGreenHpWidgets()
    {
        if (greenPowerFill != null && greenPowerFill.transform.parent == hudRoot)
        {
            Object.Destroy(greenPowerFill.gameObject);
            greenPowerFill = null;
        }

        if (hudRoot == null)
        {
            return;
        }

        Transform stray = hudRoot.Find("GreenPowerFill");
        if (stray != null && (greenPowerFill == null || stray != greenPowerFill.transform))
        {
            Object.Destroy(stray.gameObject);
        }

        if (staticHudRoot != null)
        {
            Transform strayBack = staticHudRoot.Find("GreenPowerBack");
            if (strayBack != null && strayBack.parent == staticHudRoot)
            {
                Object.Destroy(strayBack.gameObject);
            }
        }
    }

    private void EnsureTripleBaseHpBars()
    {
        CleanupMisplacedGreenHpWidgets();

        if (hudRoot == null)
        {
            return;
        }

        Transform topDynamic = ResolveTopHpBarRoot();
        Transform topBack = ResolveTopHpBarBackRoot(topDynamic);

        if (humanPowerFill != null)
        {
            SetAnchors(humanPowerFill.rectTransform, 0.03f, 0.03f, 0.32f, 0.17f);
        }

        if (greenPowerFill == null && topDynamic != null)
        {
            if (topBack != null && topBack.Find("GreenPowerBack") == null)
            {
                var greenBack = CreatePanel(topBack, "GreenPowerBack", new Color(0.05f, 0.12f, 0.06f, 1f));
                SetAnchors(greenBack.rectTransform, 0.34f, 0.03f, 0.63f, 0.17f);
            }

            greenPowerFill = CreatePanel(topDynamic, "GreenPowerFill", GreenBarColor);
            greenPowerFill.type = Image.Type.Filled;
            greenPowerFill.fillMethod = Image.FillMethod.Horizontal;
            greenPowerFill.fillOrigin = 0;
            SetAnchors(greenPowerFill.rectTransform, 0.34f, 0.03f, 0.63f, 0.17f);
        }

        if (monsterPowerFill != null)
        {
            SetAnchors(monsterPowerFill.rectTransform, 0.65f, 0.03f, 0.97f, 0.17f);
        }
    }

    private string BuildModeSelectInfoText()
    {
        float hp = GetSelectedBaseHp();
        float duration = GetSelectedMatchDuration();
        bool betrayal = matchSettings == null || matchSettings.BetrayalEnabled;
        bool rage = matchSettings != null && matchSettings.RageLikeEnabled;
        return
            $"基地血量：{hp:0}\n" +
            $"对局时长：{duration / 60f:0} 分钟（{duration:0} 秒）\n" +
            $"叛变机制：{(betrayal ? "开启" : "关闭")}\n" +
            $"狂暴点赞：{(rage ? "开启" : "关闭")}\n\n" +
            "[ / ] 调整基地血量\n" +
            "- / = 调整对局时长\n" +
            "B 开关叛变  |  L 开关狂暴点赞\n" +
            "竖屏推荐 1080×1920（分辨率条可选）";
    }

    private void RefreshModeSelectUi()
    {
        if (modeSelectOverlay != null)
        {
            modeSelectOverlay.SetActive(matchPhase == MatchPhase.ModeSelect);
        }

        if (modeSelectInfoLabel != null && matchPhase == MatchPhase.ModeSelect)
        {
            modeSelectInfoLabel.text = BuildModeSelectInfoText();
        }
    }

    private void HandleModeSelectSettingsInput()
    {
        if (matchPhase != MatchPhase.ModeSelect || matchSettings == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            CycleModePreset(ref modeBaseHpPresetIndex, ModeBaseHpPresets.Length);
            matchSettings.BaseMaxHp = GetSelectedBaseHp();
        }
        else if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            CycleModePreset(ref modeBaseHpPresetIndex, ModeBaseHpPresets.Length, 1);
            matchSettings.BaseMaxHp = GetSelectedBaseHp();
        }
        else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            CycleModePreset(ref modeDurationPresetIndex, ModeDurationPresets.Length);
            matchSettings.MatchDurationSeconds = GetSelectedMatchDuration();
        }
        else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            CycleModePreset(ref modeDurationPresetIndex, ModeDurationPresets.Length, 1);
            matchSettings.MatchDurationSeconds = GetSelectedMatchDuration();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            matchSettings.BetrayalEnabled = !matchSettings.BetrayalEnabled;
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            matchSettings.RageLikeEnabled = !matchSettings.RageLikeEnabled;
        }

        RefreshModeSelectUi();
    }

    private static void CycleModePreset(ref int index, int length, int delta = -1)
    {
        index = (index + delta + length) % length;
    }

    private float GetSelectedBaseHp()
    {
        int i = Mathf.Clamp(modeBaseHpPresetIndex, 0, ModeBaseHpPresets.Length - 1);
        return ModeBaseHpPresets[i];
    }

    private float GetSelectedMatchDuration()
    {
        int i = Mathf.Clamp(modeDurationPresetIndex, 0, ModeDurationPresets.Length - 1);
        return ModeDurationPresets[i];
    }

    private void ApplyModeSettingsToMatch()
    {
        if (matchSettings == null)
        {
            return;
        }

        matchSettings.BaseMaxHp = GetSelectedBaseHp();
        matchSettings.MatchDurationSeconds = GetSelectedMatchDuration();
    }

    internal void PushGiftFeedMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        lastGiftFeedMessage = message;
        giftFeedDisplayTimer = 4.5f;
    }

    private void TickGiftFeedDisplay(float dt)
    {
        if (giftFeedDisplayTimer <= 0f)
        {
            return;
        }

        giftFeedDisplayTimer -= dt;
    }

    private void ApplyPortraitLiveDefaults()
    {
        for (int i = 0; i < ResolutionPresets.Length; i++)
        {
            if (ResolutionPresets[i].Width == 1080 && ResolutionPresets[i].Height == 1920)
            {
                selectedResolutionIndex = i;
                break;
            }
        }

        if (!Application.isMobilePlatform)
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(1080, 1920, FullScreenMode.Windowed);
        }
    }
}
