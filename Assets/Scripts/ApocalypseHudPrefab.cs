using UnityEngine;
using UnityEngine.UI;

public sealed class ApocalypseHudPrefab : MonoBehaviour
{
    public Canvas StaticCanvas;
    public Canvas DynamicCanvas;
    public RectTransform StaticHudRoot;
    public RectTransform DynamicHudRoot;
    public Image LoadingPanel;
    public Text LoadingLabel;
    public Text BannerLabel;
    public Text TimerLabel;
    public Text PoolLabel;
    public Text LeftTeamLabel;
    public Text RightTeamLabel;
    public Text BattlePhaseLabel;
    public Text BottomTickerLabel;
    public Text SkillCountdownLabel;
    public Text GiftFeedLabel;
    public Text HumanLabel;
    public Text GiantLabel;
    public Text StatusLabel;
    public Image HpFill;
    public Image HumanPowerFill;
    public Image MonsterPowerFill;
    public Image ResolutionStrip;
    public Button[] ResolutionButtons;
    public Image[] ResolutionButtonImages;
}
