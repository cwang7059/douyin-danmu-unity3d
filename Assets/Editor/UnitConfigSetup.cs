#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class UnitConfigSetup
{
    [MenuItem("ApocalypseKing/Setup Unit Configs")]
    public static void SetupConfigs()
    {
        EnsureFolder("Assets/Settings");

        UnitConfig soldier = GetOrCreateConfig("Assets/Settings/SoldierConfig.asset", UnitKind.Soldier, 58f, 5f, 68f, 18f, 260f, 0.62f);
        UnitConfig tank = GetOrCreateConfig("Assets/Settings/TankConfig.asset", UnitKind.Tank, 270f, 85f, 34f, 34f, 430f, 1.2f);
        UnitConfig aircraft = GetOrCreateConfig("Assets/Settings/AircraftConfig.asset", UnitKind.Aircraft, 180f, 76f, 84f, 54f, 520f, 0.95f);
        UnitConfig giant = GetOrCreateConfig("Assets/Settings/GiantConfig.asset", UnitKind.Giant, 2600f, 42f, 25f, 82f, 126f, 1.12f);

        var game = Object.FindObjectOfType<ApocalypseKingUnityGame>();
        if (game != null)
        {
            Undo.RecordObject(game, "Setup Unit Configs");
            
            var so = new SerializedObject(game);
            so.FindProperty("soldierConfig").objectReferenceValue = soldier;
            so.FindProperty("tankConfig").objectReferenceValue = tank;
            so.FindProperty("aircraftConfig").objectReferenceValue = aircraft;
            so.FindProperty("giantConfig").objectReferenceValue = giant;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Debug.Log("UnitConfigs assigned to ApocalypseKingUnityGame.");
        }
        else
        {
            Debug.LogWarning("ApocalypseKingUnityGame not found in the current scene. Configs were created but not assigned.");
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }

    private static UnitConfig GetOrCreateConfig(string path, UnitKind kind, float hp, float dmg, float speed, float radius, float range, float interval)
    {
        UnitConfig config = AssetDatabase.LoadAssetAtPath<UnitConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<UnitConfig>();
            config.Kind = kind;
            config.DisplayName = kind.ToString();
            config.MaxHp = hp;
            config.Damage = dmg;
            config.MoveSpeed = speed;
            config.Radius = radius;
            config.AttackRange = range;
            config.AttackInterval = interval;
            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created {path}");
        }
        return config;
    }
}
#endif
