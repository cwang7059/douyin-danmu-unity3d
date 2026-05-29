using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private bool TryCreateBattlefieldFromPrefab()
    {
        if (battlefieldPrefab == null)
        {
            return false;
        }

        var instance = Instantiate(battlefieldPrefab, decorRoot, false);
        instance.name = battlefieldPrefab.name;

        var prefabBindings = instance.GetComponent<ApocalypseBattlefieldPrefab>();
        if (prefabBindings == null)
        {
            Debug.LogWarning("[ApocalypseKing] Battlefield prefab has no ApocalypseBattlefieldPrefab bindings; using prefab visuals without obstacle metadata.");
            return true;
        }

        RegisterBattlefieldObstacles(prefabBindings);
        RegisterRoadCorridors(prefabBindings);
        return true;
    }

    private void RegisterBattlefieldObstacles(ApocalypseBattlefieldPrefab prefabBindings)
    {
        var obstacles = prefabBindings.BuildingObstacles;
        if (obstacles == null)
        {
            return;
        }

        for (int i = 0; i < obstacles.Length; i++)
        {
            var obstacle = obstacles[i];
            if (obstacle == null)
            {
                continue;
            }

            Vector2 center = obstacle.UseRootPosition && obstacle.Root != null
                ? WorldToLogical(obstacle.Root.position)
                : obstacle.LogicalCenter;
            Vector2 halfSize = ClampHalfSize(obstacle.HalfSize);
            string name = !string.IsNullOrEmpty(obstacle.Name)
                ? obstacle.Name
                : obstacle.Root != null
                    ? obstacle.Root.name
                    : $"PrefabObstacle_{i}";

            AddBuildingObstacle(
                obstacle.Root != null ? obstacle.Root.gameObject : null,
                name,
                center.x,
                center.y,
                halfSize.x,
                halfSize.y,
                Mathf.Max(0.1f, obstacle.Height),
                Mathf.Max(0f, obstacle.Padding),
                Mathf.Max(1f, obstacle.Hp));
        }
    }

    private void RegisterRoadCorridors(ApocalypseBattlefieldPrefab prefabBindings)
    {
        var corridors = prefabBindings.RoadCorridors;
        if (corridors == null)
        {
            return;
        }

        for (int i = 0; i < corridors.Length; i++)
        {
            var corridor = corridors[i];
            if (corridor == null)
            {
                continue;
            }

            Vector2 center = corridor.UseCenterTransform && corridor.CenterTransform != null
                ? WorldToLogical(corridor.CenterTransform.position)
                : corridor.LogicalCenter;
            Vector2 halfSize = ClampHalfSize(corridor.HalfSize);
            string name = !string.IsNullOrEmpty(corridor.Name) ? corridor.Name : $"PrefabRoad_{i}";
            AddRoadCorridor(name, center.x, center.y, halfSize.x, halfSize.y, corridor.Priority);
        }
    }

    private static Vector2 WorldToLogical(Vector3 worldPosition)
    {
        return new Vector2(worldPosition.x / LogicalToWorld, worldPosition.z / LogicalToWorld);
    }

    private static Vector2 ClampHalfSize(Vector2 halfSize)
    {
        return new Vector2(Mathf.Max(1f, halfSize.x), Mathf.Max(1f, halfSize.y));
    }
}
