#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

public static class ApocalypseKingBattleContentSetup
{
    private const string SettingsFolder = "Assets/Settings";
    private const string AudioFolder = "Assets/Audio";
    private const string MixerPath = AudioFolder + "/BattleAudioMixer.mixer";
    private const string EffectsResourcesFolder = "Assets/Resources/Battle/Effects";
    private const string AudioResourcesFolder = "Assets/Resources/Battle/Audio";
    private const string EffectsCatalogPath = SettingsFolder + "/BattleEffectsCatalog.asset";
    private const string AudioCatalogPath = SettingsFolder + "/BattleAudioCatalog.asset";

    public static void CreateOrUpdateBattleContentAssets()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Battle");
        EnsureFolder(EffectsResourcesFolder);
        EnsureFolder(AudioResourcesFolder);
        EnsureFolder(SettingsFolder);
        EnsureFolder(AudioFolder);

        var effectConfigs = CreateOrUpdateEffectConfigs();
        var effectsCatalog = GetOrCreateCatalog<BattleEffectsCatalog>(EffectsCatalogPath);
        effectsCatalog.configs = effectConfigs;
        EditorUtility.SetDirty(effectsCatalog);

        AudioMixer mixer = LoadBattleMixer();
        if (mixer != null)
        {
            EnsureMixerChildGroups(mixer);
        }

        var mixerGroups = ResolveMixerGroups(mixer);
        var audioCues = CreateOrUpdateAudioCueConfigs(mixerGroups);
        var audioCatalog = GetOrCreateCatalog<BattleAudioCatalog>(AudioCatalogPath);
        audioCatalog.cues = audioCues;
        EditorUtility.SetDirty(audioCatalog);

        AssetDatabase.SaveAssets();
        Debug.Log("[ApocalypseKing] Battle effect/audio assets created or updated.");
    }

    public static void AssignBattleContentToOpenScene()
    {
        var game = UnityEngine.Object.FindObjectOfType<ApocalypseKingUnityGame>();
        if (game == null)
        {
            return;
        }

        var effectsCatalog = AssetDatabase.LoadAssetAtPath<BattleEffectsCatalog>(EffectsCatalogPath);
        var audioCatalog = AssetDatabase.LoadAssetAtPath<BattleAudioCatalog>(AudioCatalogPath);
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        var mixerGroups = ResolveMixerGroups(mixer);

        var effectManager = game.GetComponent<EffectManager>();
        if (effectManager != null)
        {
            Undo.RecordObject(effectManager, "Assign Battle Effects");
            var effectSo = new SerializedObject(effectManager);
            effectSo.FindProperty("effectsCatalog").objectReferenceValue = effectsCatalog;
            effectSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(effectManager);
        }

        var audioManager = game.GetComponent<BattleAudioManager>();
        if (audioManager != null)
        {
            Undo.RecordObject(audioManager, "Assign Battle Audio");
            var audioSo = new SerializedObject(audioManager);
            audioSo.FindProperty("audioCatalog").objectReferenceValue = audioCatalog;
            audioSo.FindProperty("masterGroup").objectReferenceValue = mixerGroups.Master;
            audioSo.FindProperty("bgmGroup").objectReferenceValue = mixerGroups.Bgm;
            audioSo.FindProperty("sfxGroup").objectReferenceValue = mixerGroups.Sfx;
            audioSo.FindProperty("weaponGroup").objectReferenceValue = mixerGroups.Weapon;
            audioSo.FindProperty("explosionGroup").objectReferenceValue = mixerGroups.Explosion;
            audioSo.FindProperty("creatureGroup").objectReferenceValue = mixerGroups.Creature;
            audioSo.FindProperty("magicGroup").objectReferenceValue = mixerGroups.Magic;
            audioSo.FindProperty("uiGroup").objectReferenceValue = mixerGroups.Ui;
            audioSo.FindProperty("voiceGroup").objectReferenceValue = mixerGroups.Voice;
            audioSo.FindProperty("ambienceGroup").objectReferenceValue = mixerGroups.Ambience;
            audioSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(audioManager);
        }

        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
    }

    private static EffectConfig[] CreateOrUpdateEffectConfigs()
    {
        var values = (BattleEffectId[])Enum.GetValues(typeof(BattleEffectId));
        var list = new List<EffectConfig>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            BattleEffectId id = values[i];
            if (id == BattleEffectId.None)
            {
                continue;
            }

            string path = EffectsResourcesFolder + "/Effect_" + id + ".asset";
            var config = AssetDatabase.LoadAssetAtPath<EffectConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EffectConfig>();
                config.id = id;
                AssetDatabase.CreateAsset(config, path);
            }

            config.id = id;
            GetEffectDefaults(id, out int prewarm, out int maxCount);
            config.prewarmCount = prewarm;
            config.maxCount = maxCount;
            EditorUtility.SetDirty(config);
            list.Add(config);
        }

        return list.ToArray();
    }

    private static AudioCueConfig[] CreateOrUpdateAudioCueConfigs(MixerGroupSet groups)
    {
        var values = (BattleAudioCueId[])Enum.GetValues(typeof(BattleAudioCueId));
        var list = new List<AudioCueConfig>(values.Length);
        for (int i = 0; i < values.Length; i++)
        {
            BattleAudioCueId id = values[i];
            if (id == BattleAudioCueId.None)
            {
                continue;
            }

            string path = AudioResourcesFolder + "/AudioCue_" + id + ".asset";
            var cue = AssetDatabase.LoadAssetAtPath<AudioCueConfig>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<AudioCueConfig>();
                cue.id = id;
                AssetDatabase.CreateAsset(cue, path);
            }

            cue.id = id;
            cue.channel = GetDefaultAudioChannel(id);
            cue.mixerGroup = groups.GetGroup(cue.channel);
            cue.volume = GetDefaultCueVolume(id);
            cue.pitchJitter = GetDefaultPitchJitter(id);
            cue.minInterval = GetDefaultMinInterval(id);
            cue.spatial = id != BattleAudioCueId.UiClick && id != BattleAudioCueId.UiWarning;
            EditorUtility.SetDirty(cue);
            list.Add(cue);
        }

        return list.ToArray();
    }

    private static AudioMixer LoadBattleMixer()
    {
        return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
    }

    [MenuItem("Apocalypse King/Create Battle Audio Mixer Asset (Instructions)")]
    public static void LogBattleAudioMixerInstructions()
    {
        if (LoadBattleMixer() != null)
        {
            EnsureMixerChildGroups(LoadBattleMixer());
            Debug.Log($"[ApocalypseKing] Mixer ready at {MixerPath}. Re-run Setup Project Assets to bind groups.");
            return;
        }

        Debug.Log(
            "[ApocalypseKing] In Project window: Create > Audio Mixer, save as Assets/Audio/BattleAudioMixer.mixer, "
            + "then run Apocalypse King > Setup Project Assets.");
    }

    private static void EnsureMixerChildGroups(AudioMixer mixer)
    {
        if (mixer == null)
        {
            return;
        }

        try
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            Type controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            Type groupType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            if (controllerType == null || groupType == null)
            {
                return;
            }

            object controller = mixer;
            PropertyInfo masterProperty = controllerType.GetProperty("masterGroup", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo createGroupMethod = controllerType.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo addChildMethod = controllerType.GetMethod("AddChildToParent", BindingFlags.Instance | BindingFlags.Public);
            if (masterProperty == null || createGroupMethod == null || addChildMethod == null)
            {
                return;
            }

            object master = masterProperty.GetValue(controller);
            if (master == null)
            {
                return;
            }

            object sfx = EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, master, "SFX");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, master, "BGM");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, master, "Voice");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, master, "Ambience");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, sfx, "Weapon");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, sfx, "Explosion");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, sfx, "Creature");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, sfx, "Magic");
            EnsureMixerChildReflection(controller, createGroupMethod, addChildMethod, groupType, sfx, "UI");
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApocalypseKing] Could not auto-create mixer groups: {ex.Message}");
        }
    }

    private static object EnsureMixerChildReflection(
        object controller,
        MethodInfo createGroupMethod,
        MethodInfo addChildMethod,
        Type groupType,
        object parent,
        string name)
    {
        PropertyInfo childrenProperty = groupType.GetProperty("children", BindingFlags.Instance | BindingFlags.Public);
        if (childrenProperty != null)
        {
            var children = childrenProperty.GetValue(parent) as Array;
            if (children != null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    var child = children.GetValue(i);
                    if (child != null && string.Equals(((UnityEngine.Object)child).name, name, StringComparison.Ordinal))
                    {
                        return child;
                    }
                }
            }
        }

        object created = createGroupMethod.Invoke(controller, new object[] { name, false });
        addChildMethod.Invoke(controller, new[] { created, parent });
        return created;
    }

    private static MixerGroupSet ResolveMixerGroups(AudioMixer mixer)
    {
        var set = new MixerGroupSet();
        if (mixer == null)
        {
            return set;
        }

        set.Master = mixer.outputAudioMixerGroup;
        set.Bgm = FindGroup(mixer, "BGM") ?? set.Master;
        set.Sfx = FindGroup(mixer, "SFX") ?? set.Master;
        set.Weapon = FindGroup(mixer, "Weapon") ?? set.Sfx;
        set.Explosion = FindGroup(mixer, "Explosion") ?? set.Sfx;
        set.Creature = FindGroup(mixer, "Creature") ?? set.Sfx;
        set.Magic = FindGroup(mixer, "Magic") ?? set.Sfx;
        set.Ui = FindGroup(mixer, "UI") ?? set.Sfx;
        set.Voice = FindGroup(mixer, "Voice") ?? set.Master;
        set.Ambience = FindGroup(mixer, "Ambience") ?? set.Master;
        return set;
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var groups = mixer.FindMatchingGroups(name);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    private static T GetOrCreateCatalog<T>(string path) where T : ScriptableObject
    {
        var catalog = AssetDatabase.LoadAssetAtPath<T>(path);
        if (catalog != null)
        {
            return catalog;
        }

        catalog = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(catalog, path);
        return catalog;
    }

    private static void GetEffectDefaults(BattleEffectId id, out int prewarm, out int maxCount)
    {
        switch (id)
        {
            case BattleEffectId.HumanSummon:
            case BattleEffectId.OrcSummon:
                prewarm = 12;
                maxCount = 48;
                return;
            case BattleEffectId.MuzzleRifle:
                prewarm = 16;
                maxCount = 96;
                return;
            case BattleEffectId.MuzzleTank:
            case BattleEffectId.ShellLaunchSmoke:
            case BattleEffectId.BombDropTrail:
                prewarm = 8;
                maxCount = 64;
                return;
            case BattleEffectId.MuzzleAircraft:
                prewarm = 6;
                maxCount = 48;
                return;
            case BattleEffectId.TankDeathExplosion:
            case BattleEffectId.AircraftDeathExplosion:
            case BattleEffectId.MonsterDeathExplosion:
            case BattleEffectId.MonsterDeathDust:
                prewarm = 4;
                maxCount = 24;
                return;
            default:
                prewarm = 8;
                maxCount = 64;
                return;
        }
    }

    private static BattleAudioChannel GetDefaultAudioChannel(BattleAudioCueId id)
    {
        switch (id)
        {
            case BattleAudioCueId.RifleShot:
            case BattleAudioCueId.TankShot:
                return BattleAudioChannel.Weapon;
            case BattleAudioCueId.ExplosionSmall:
            case BattleAudioCueId.ExplosionLarge:
                return BattleAudioChannel.Explosion;
            case BattleAudioCueId.CreatureRoar:
            case BattleAudioCueId.CreatureHit:
                return BattleAudioChannel.Creature;
            case BattleAudioCueId.HumanSkill:
            case BattleAudioCueId.OrcSkill:
                return BattleAudioChannel.Magic;
            case BattleAudioCueId.UiClick:
            case BattleAudioCueId.UiWarning:
                return BattleAudioChannel.Ui;
            case BattleAudioCueId.Victory:
            case BattleAudioCueId.Defeat:
                return BattleAudioChannel.Bgm;
            default:
                return BattleAudioChannel.Sfx;
        }
    }

    private static float GetDefaultCueVolume(BattleAudioCueId id)
    {
        switch (id)
        {
            case BattleAudioCueId.RifleShot:
                return 0.42f;
            case BattleAudioCueId.TankShot:
                return 0.86f;
            case BattleAudioCueId.ExplosionLarge:
                return 0.95f;
            case BattleAudioCueId.UiClick:
                return 0.35f;
            default:
                return 0.8f;
        }
    }

    private static float GetDefaultPitchJitter(BattleAudioCueId id)
    {
        return id == BattleAudioCueId.RifleShot ? 0.035f : 0.04f;
    }

    private static float GetDefaultMinInterval(BattleAudioCueId id)
    {
        switch (id)
        {
            case BattleAudioCueId.RifleShot:
                return 0.035f;
            case BattleAudioCueId.TankShot:
                return 0.12f;
            case BattleAudioCueId.ExplosionLarge:
                return 0.16f;
            case BattleAudioCueId.HumanSkill:
            case BattleAudioCueId.OrcSkill:
                return 0.25f;
            default:
                return 0.08f;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private struct MixerGroupSet
    {
        public AudioMixerGroup Master;
        public AudioMixerGroup Bgm;
        public AudioMixerGroup Sfx;
        public AudioMixerGroup Weapon;
        public AudioMixerGroup Explosion;
        public AudioMixerGroup Creature;
        public AudioMixerGroup Magic;
        public AudioMixerGroup Ui;
        public AudioMixerGroup Voice;
        public AudioMixerGroup Ambience;

        public AudioMixerGroup GetGroup(BattleAudioChannel channel)
        {
            switch (channel)
            {
                case BattleAudioChannel.Bgm:
                    return Bgm;
                case BattleAudioChannel.Weapon:
                    return Weapon;
                case BattleAudioChannel.Explosion:
                    return Explosion;
                case BattleAudioChannel.Creature:
                    return Creature;
                case BattleAudioChannel.Magic:
                    return Magic;
                case BattleAudioChannel.Ui:
                    return Ui;
                case BattleAudioChannel.Voice:
                    return Voice;
                case BattleAudioChannel.Ambience:
                    return Ambience;
                case BattleAudioChannel.Sfx:
                    return Sfx;
                default:
                    return Master;
            }
        }
    }
}
#endif
