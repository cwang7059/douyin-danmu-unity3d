using UnityEngine;
using UnityEngine.UI;

public sealed partial class ApocalypseKingUnityGame
{
    private bool TryCreateHudFromPrefab()
    {
        if (hudPrefab == null)
        {
            return false;
        }

        var view = Instantiate(hudPrefab, transform, false);
        view.name = hudPrefab.name;
        BindHudPrefab(view);

        if (canvas == null)
        {
            Debug.LogWarning("[ApocalypseKing] HUD prefab has no canvas bindings; falling back to generated HUD.");
            Destroy(view.gameObject);
            return false;
        }

        ApplySafeArea();
        RegisterResolutionButtons();
        RefreshResolutionControls();
        return true;
    }

    private void BindHudPrefab(ApocalypseHudPrefab view)
    {
        staticHudCanvas = view.StaticCanvas != null ? view.StaticCanvas : view.DynamicCanvas;
        dynamicHudCanvas = view.DynamicCanvas != null ? view.DynamicCanvas : view.StaticCanvas;
        canvas = dynamicHudCanvas != null ? dynamicHudCanvas : staticHudCanvas;

        staticHudRoot = view.StaticHudRoot != null ? view.StaticHudRoot : CanvasRoot(staticHudCanvas);
        hudRoot = view.DynamicHudRoot != null ? view.DynamicHudRoot : CanvasRoot(dynamicHudCanvas);
        loadingPanel = view.LoadingPanel;
        loadingLabel = view.LoadingLabel;
        bannerLabel = view.BannerLabel;
        timerLabel = view.TimerLabel;
        poolLabel = view.PoolLabel;
        leftTeamLabel = view.LeftTeamLabel;
        rightTeamLabel = view.RightTeamLabel;
        battlePhaseLabel = view.BattlePhaseLabel;
        bottomTickerLabel = view.BottomTickerLabel;
        skillCountdownLabel = view.SkillCountdownLabel;
        giftFeedLabel = view.GiftFeedLabel;
        humanLabel = view.HumanLabel;
        giantLabel = view.GiantLabel;
        statusLabel = view.StatusLabel;
        hpFill = view.HpFill != null ? view.HpFill : view.MonsterPowerFill;
        humanPowerFill = view.HumanPowerFill;
        monsterPowerFill = view.MonsterPowerFill;
        resolutionStrip = view.ResolutionStrip;
        resolutionButtons = view.ResolutionButtons;
        resolutionButtonImages = view.ResolutionButtonImages;
    }

    private void RegisterResolutionButtons()
    {
        if (resolutionButtons == null)
        {
            return;
        }

        int count = Mathf.Min(resolutionButtons.Length, ResolutionPresets.Length);
        for (int i = 0; i < count; i++)
        {
            if (resolutionButtons[i] == null)
            {
                continue;
            }

            int index = i;
            resolutionButtons[i].onClick.AddListener(() => ApplyResolutionPreset(index));
        }

        if (resolutionStrip != null)
        {
            resolutionStrip.gameObject.SetActive(ShowResolutionDebugControls);
        }
    }

    private static RectTransform CanvasRoot(Canvas targetCanvas)
    {
        return targetCanvas != null ? targetCanvas.GetComponent<RectTransform>() : null;
    }
}
