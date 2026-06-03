using UnityEngine;

[CreateAssetMenu(menuName = "Apocalypse/Match Settings", fileName = "ApocalypseMatchSettings")]
public sealed class ApocalypseMatchSettings : ScriptableObject
{
    [Header("Mode select")]
    public float BaseMaxHp = 100000f;
    public float MatchDurationSeconds = 600f;
    public bool BetrayalEnabled = true;
    [Range(0f, 1f)] public float BetrayalChance = 0.35f;
    [Range(0.01f, 0.2f)] public float BetrayalBaseRestorePercent = 0.01f;
    public float BetrayalExtraSeconds = 120f;

    [Header("Engagement")]
    public bool RageLikeEnabled;
    public int RageLikeMultiplier = 4;
    public float NuclearCountdownSeconds = 10f;
    public int MaxSpawnsPerFrame = 24;

    [Header("Infection")]
    [Range(0f, 1f)] public float InfectionChance = 0.12f;
    public float InfectionDurationSeconds = 3f;

    [Header("Presentation")]
    [Tooltip("关闭时仅显示 HUD 三条基地血条，不在场景里生成彩色基地柱")]
    public bool ShowWorldBaseMarkers;

    [Header("Economy mock")]
    public float PointPoolBase = 380000f;
    public float PointPoolPerSecond = 8200f;

    public static ApocalypseMatchSettings CreateRuntimeDefault()
    {
        var s = CreateInstance<ApocalypseMatchSettings>();
        s.BaseMaxHp = 100000f;
        s.MatchDurationSeconds = 600f;
        s.BetrayalEnabled = true;
        s.BetrayalChance = 0.35f;
        s.BetrayalBaseRestorePercent = 0.01f;
        s.BetrayalExtraSeconds = 120f;
        s.RageLikeEnabled = false;
        s.RageLikeMultiplier = 4;
        s.NuclearCountdownSeconds = 10f;
        s.MaxSpawnsPerFrame = 24;
        s.InfectionChance = 0.12f;
        s.InfectionDurationSeconds = 3f;
        s.ShowWorldBaseMarkers = false;
        s.PointPoolBase = 380000f;
        s.PointPoolPerSecond = 8200f;
        return s;
    }
}
