using UnityEngine;

public sealed class ApocalypseBaseState
{
    public FactionId Faction;
    public float MaxHp;
    public float Hp;
    public float WorldX;
    public float WorldZ;
    public bool Destroyed => Hp <= 0f;

    public ApocalypseBaseState(FactionId faction, float maxHp, float worldX, float worldZ)
    {
        Faction = faction;
        MaxHp = Mathf.Max(1f, maxHp);
        Hp = MaxHp;
        WorldX = worldX;
        WorldZ = worldZ;
    }

    public void ResetHp(float maxHp)
    {
        MaxHp = Mathf.Max(1f, maxHp);
        Hp = MaxHp;
    }

    public void RestorePercent(float percent)
    {
        Hp = Mathf.Clamp(MaxHp * Mathf.Clamp01(percent), 1f, MaxHp);
    }

    public void ApplyDamage(float amount)
    {
        Hp = Mathf.Max(0f, Hp - Mathf.Max(0f, amount));
    }
}
