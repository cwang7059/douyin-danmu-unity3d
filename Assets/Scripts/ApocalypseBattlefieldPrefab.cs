using System;
using UnityEngine;

public sealed class ApocalypseBattlefieldPrefab : MonoBehaviour
{
    public BuildingObstacleBinding[] BuildingObstacles;
    public RoadCorridorBinding[] RoadCorridors;
}

[Serializable]
public sealed class BuildingObstacleBinding
{
    public string Name;
    public Transform Root;
    public bool UseRootPosition = true;
    public Vector2 LogicalCenter;
    public Vector2 HalfSize = new Vector2(40f, 40f);
    public float Height = 2f;
    public float Padding = 8f;
    public float Hp = 160f;
}

[Serializable]
public sealed class RoadCorridorBinding
{
    public string Name;
    public Transform CenterTransform;
    public bool UseCenterTransform = true;
    public Vector2 LogicalCenter;
    public Vector2 HalfSize = new Vector2(80f, 40f);
    public float Priority;
}
