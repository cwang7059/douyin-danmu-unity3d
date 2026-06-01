public static class ApocalypseCombatMatrix
{
    public static float GetDamageMultiplier(ApocalypseUnitRole attacker, ApocalypseUnitRole defender)
    {
        if (attacker == ApocalypseUnitRole.None || defender == ApocalypseUnitRole.None)
        {
            return 1f;
        }

        // 能力药丸 克 能量电池
        if (attacker == ApocalypseUnitRole.RangedGrunt && defender == ApocalypseUnitRole.AirUnit)
        {
            return 1.25f;
        }

        if (attacker == ApocalypseUnitRole.AirUnit && defender == ApocalypseUnitRole.RangedGrunt)
        {
            return 0.8f;
        }

        // 魔法镜 克 能力药丸
        if (attacker == ApocalypseUnitRole.RushVehicle && defender == ApocalypseUnitRole.RangedGrunt)
        {
            return 1.25f;
        }

        if (attacker == ApocalypseUnitRole.RangedGrunt && defender == ApocalypseUnitRole.RushVehicle)
        {
            return 0.8f;
        }

        // 甜甜圈 克 魔法镜
        if (attacker == ApocalypseUnitRole.ShieldTank && defender == ApocalypseUnitRole.RushVehicle)
        {
            return 1.25f;
        }

        if (attacker == ApocalypseUnitRole.RushVehicle && defender == ApocalypseUnitRole.ShieldTank)
        {
            return 0.8f;
        }

        // 能量电池 克 甜甜圈
        if (attacker == ApocalypseUnitRole.AirUnit && defender == ApocalypseUnitRole.ShieldTank)
        {
            return 1.25f;
        }

        if (attacker == ApocalypseUnitRole.ShieldTank && defender == ApocalypseUnitRole.AirUnit)
        {
            return 0.8f;
        }

        return 1f;
    }
}
