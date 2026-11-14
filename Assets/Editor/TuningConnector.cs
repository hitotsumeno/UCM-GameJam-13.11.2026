using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Connects the Tuning asset (Assets/_Settings/Tuning.asset) to every component that has a
// "Tuning" field: all prefabs in the project and the objects in the open scenes.
// Menu: Filip > Connect Tuning To Prefabs And Scene. Creates the asset if it is missing.
// Fields that already point to a Tuning asset are left alone (so a prefab can use its own).
public static class TuningConnector
{
    const string FieldName = "tuning";

    [MenuItem("Filip/Connect Tuning To Prefabs And Scene")]
    static void ConnectMenu()
    {
        var tuning = GetOrCreate();
        int prefabs = ConnectAllPrefabs(tuning);
        int sceneObjects = ConnectOpenScenes(tuning);
        Selection.activeObject = tuning;
        EditorGUIUtility.PingObject(tuning);
        Debug.Log("TuningConnector: connected " + tuning.name + " in " + prefabs + " prefabs and " +
            sceneObjects + " scene components." + (sceneObjects > 0 ? " Save the scene to keep it." : ""), tuning);
    }

    [MenuItem("Filip/Select Tuning Asset")]
    static void SelectMenu()
    {
        var tuning = GetOrCreate();
        Selection.activeObject = tuning;
        EditorGUIUtility.PingObject(tuning);
    }

    // The project's Tuning asset; created with the default values if there is none.
    public static Tuning GetOrCreate()
    {
        var t = Tuning.FindInEditor();
        if (t != null) return t;

        string folder = Path.GetDirectoryName(Tuning.AssetPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
        t = ScriptableObject.CreateInstance<Tuning>();
        AssetDatabase.CreateAsset(t, Tuning.AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("TuningConnector: created " + Tuning.AssetPath, t);
        return t;
    }

    // Sets every empty "tuning" field under root. Returns how many were set.
    public static int Connect(GameObject root, Tuning tuning)
    {
        int n = 0;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue; // missing script
            var so = new SerializedObject(mb);
            var p = so.FindProperty(FieldName);
            if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (p.objectReferenceValue != null) continue;
            p.objectReferenceValue = tuning;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(mb)) PrefabUtility.RecordPrefabInstancePropertyModifications(mb);
            n++;
        }
        return n;
    }

    // Plain prefabs first, so prefabs nested inside levels already carry the reference.
    static int ConnectAllPrefabs(Tuning tuning)
    {
        int changed = 0;
        var paths = new System.Collections.Generic.List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        paths.Sort((a, b) => NestingDepth(a).CompareTo(NestingDepth(b)));

        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (Connect(root, tuning) > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        return changed;
    }

    // Prefabs that contain other prefabs (levels) sort after the ones they contain.
    static int NestingDepth(string path)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) return 0;
        int nested = 0;
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            if (t.gameObject != go && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) nested++;
        return nested > 0 ? 1 : 0;
    }

    static int ConnectOpenScenes(Tuning tuning)
    {
        int n = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            int sceneCount = 0;
            foreach (var root in scene.GetRootGameObjects()) sceneCount += Connect(root, tuning);
            if (sceneCount > 0) EditorSceneManager.MarkSceneDirty(scene); // save the scene to keep it
            n += sceneCount;
        }
        return n;
    }
}
