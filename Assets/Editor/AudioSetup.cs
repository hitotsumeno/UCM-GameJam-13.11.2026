using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

// Game > Setup Audio: creates one SoundEvent asset per game sound (Assets/_Settings/Sounds)
// and puts an AudioManager object into DraftScene with every slot filled.
// Safe to run again: existing sound assets are kept (your Inspector changes survive),
// only empty slots are filled. Swap clips later on the assets themselves.
// Command line: Unity -batchmode -quit -executeMethod AudioSetup.Run
public static class AudioSetup
{
    const string MixerPath = "Assets/_Resources/Levels/MasterMixer.mixer";
    const string ScenePath = "Assets/_Scenes/DraftScene.unity";
    const string SoundsDir = "Assets/_Settings/Sounds";
    const string Sfx = "Assets/_Resources/Shapeforms Audio Free Sound Effects/";
    const string Weapons = Sfx + "Sci Fi Weapons Cyberpunk Arsenal Preview/AUDIO/";
    const string Punch = Sfx + "Hit and Punch Preview/AUDIO/";
    const string Ui = Sfx + "Future UI Preview/Audio/";

    // field on AudioManager, asset name, clip path, volume
    static readonly (string field, string asset, string clip, float volume)[] Sounds =
    {
        ("shoot",         "SFX_Shoot",         Weapons + "LASRGun_Classic Blaster A Fire_03.wav", 0.6f),
        ("asteroidHit",   "SFX_AsteroidHit",   Weapons + "BLLTImpt_Hit Marker_07.wav", 0.6f),
        ("asteroidBreak", "SFX_AsteroidBreak", Sfx + "Sci Fi Spacecraft Preview/Impact Asteroid Debris Tail_03.wav", 0.8f),
        ("mineBoom",      "SFX_MineBoom",      Weapons + "EXPLDsgn_Explosion Impact_14.wav", 0.9f),
        ("breach",        "SFX_Breach",        Punch + "HIT_METAL_WRENCH_HEAVIEST_02.wav", 0.9f),
        ("bump",          "SFX_Bump",          Punch + "PUNCH_CLEAN_HEAVY_10.wav", 0.8f),
        ("pickup",        "SFX_Pickup",        Weapons + "SCIEnrg_Shield Activate Deactivate_02.wav", 0.8f),
        ("crystal",       "SFX_Crystal",       Ui + "FUI Button Beep Clean.wav", 0.5f),
        ("win",           "SFX_Win",           Ui + "High-Tech Gadget Activate.wav", 0.9f),
        ("lose",          "SFX_Lose",          Sfx + "Glitch and Noise Preview/Future Hologram Turn Off_02.wav", 0.9f),
    };
    const string MusicClip = "Assets/_Resources/Pixel Perfect.wav";

    [MenuItem("Game/Setup Audio")]
    public static void Run()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null) { Debug.LogError("AudioSetup: mixer not found at " + MixerPath); return; }
        var groups = AssetDatabase.LoadAllAssetsAtPath(MixerPath).OfType<AudioMixerGroup>().ToArray();
        var musicGroup = groups.FirstOrDefault(g => g.name == "Music");
        var sfxGroup = groups.FirstOrDefault(g => g.name == "SFX");

        if (!AssetDatabase.IsValidFolder(SoundsDir))
            AssetDatabase.CreateFolder("Assets/_Settings", "Sounds");

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manager = Object.FindFirstObjectByType<AudioManager>();
        if (manager == null)
            manager = new GameObject("AudioManager").AddComponent<AudioManager>();

        var so = new SerializedObject(manager);
        so.FindProperty("mixer").objectReferenceValue = mixer;
        so.FindProperty("musicGroup").objectReferenceValue = musicGroup;
        so.FindProperty("sfxGroup").objectReferenceValue = sfxGroup;

        var music = GetOrCreate("MUS_Game", MusicClip, musicGroup, 0.7f, true);
        if (so.FindProperty("sceneMusic").objectReferenceValue == null)
            so.FindProperty("sceneMusic").objectReferenceValue = music;

        foreach (var s in Sounds)
        {
            var e = GetOrCreate(s.asset, s.clip, sfxGroup, s.volume, false);
            var p = so.FindProperty(s.field);
            if (p.objectReferenceValue == null) p.objectReferenceValue = e;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AudioSetup: done");
    }

    static SoundEvent GetOrCreate(string name, string clipPath, AudioMixerGroup group, float volume, bool music)
    {
        string path = SoundsDir + "/" + name + ".asset";
        var e = AssetDatabase.LoadAssetAtPath<SoundEvent>(path);
        if (e != null) return e;

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null) Debug.LogWarning("AudioSetup: clip not found " + clipPath);

        e = ScriptableObject.CreateInstance<SoundEvent>();
        e.clips = clip != null ? new[] { clip } : new AudioClip[0];
        e.output = group;
        e.volume = volume;
        e.loop = music;
        if (music) { e.pitchMin = 1f; e.pitchMax = 1f; e.cooldown = 0f; }
        AssetDatabase.CreateAsset(e, path);
        return e;
    }
}
