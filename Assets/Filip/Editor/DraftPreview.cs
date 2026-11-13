using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shows the level in the editor WITHOUT pressing Play, when DraftScene is open.
// It builds the same objects as the game (WorldBuilder + Hud), marks them as
// "do not save" and read-only, and removes them before Play. Nothing is written
// into the scene file. Turn it off in the menu: Filip > Level Preview In Editor.
[InitializeOnLoad]
public static class DraftPreview
{
    const string Prefix = "[Preview] ";
    const string PrefKey = "Filip.DraftPreview.Enabled";
    const string MenuPath = "Filip/Level Preview In Editor";

    static DraftPreview()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorSceneManager.activeSceneChangedInEditMode += (from, to) => Schedule();
        EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
        EditorSceneManager.sceneClosing += (scene, removing) => Clear();
        EditorSceneManager.sceneClosed += scene => Schedule();
        AssemblyReloadEvents.beforeAssemblyReload += Clear;
        Schedule();
    }

    static bool Enabled
    {
        get { return EditorPrefs.GetBool(PrefKey, true); }
        set { EditorPrefs.SetBool(PrefKey, value); }
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Schedule();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode) Clear();
        else if (change == PlayModeStateChange.EnteredEditMode) Schedule();
    }

    // Rebuilds the preview a moment later (also called when pictures in Resources change).
    public static void Schedule()
    {
        EditorApplication.delayCall -= Refresh;
        EditorApplication.delayCall += Refresh;
    }

    static void Refresh()
    {
        Clear();
        if (!Enabled) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().name != DraftGame.SceneName) return;

        try
        {
            Art.ResetStatics(); // pick up pictures added or removed since the last build
            var world = WorldBuilder.Build();
            Mark(world.root.gameObject);

            var hud = Hud.Create();
            hud.SetOxygen(1f);
            hud.SetCompass(world.ship.position - world.player.transform.position);
            Mark(hud.gameObject);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Draft level preview failed: " + e.Message);
            Clear();
        }
    }

    // Not saved into the scene, read-only in the Inspector.
    static void Mark(GameObject root)
    {
        root.name = Prefix + root.name;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
    }

    static void Clear()
    {
        var roots = new List<GameObject>();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go == null || go.transform.parent != null) continue;
            if (!go.name.StartsWith(Prefix)) continue;
            if ((go.hideFlags & HideFlags.DontSave) == 0) continue;
            if (EditorUtility.IsPersistent(go)) continue;
            roots.Add(go);
        }
        foreach (var go in roots)
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
    }
}
