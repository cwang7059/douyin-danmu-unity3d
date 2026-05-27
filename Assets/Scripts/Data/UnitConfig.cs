using UnityEngine;

[CreateAssetMenu(menuName = "ApocalypseKing/UnitConfig", fileName = "UnitConfig")]
public class UnitConfig : ScriptableObject
{
    public UnitKind Kind;
    public string DisplayName;
    public float MaxHp;
    public float Damage;
    public float MoveSpeed;
    public float Radius;
    public float AttackRange;
    public float AttackInterval;
}
