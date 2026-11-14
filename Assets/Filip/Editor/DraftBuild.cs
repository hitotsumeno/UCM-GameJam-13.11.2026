using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu Filip > Test Build: makes a Windows .exe of the draft game and starts it.
// The scene is passed to the build directly, so the project's build settings
// (ProjectSettings) are not touched. The result goes to a DraftBuild folder next
// to the project folder, outside the repository.
public static class DraftBuild
{
    const string ScenePath = "Assets/Filip/DraftScene.unity";

    [MenuItem("Filip/Test Build (Windows)")]
    static void TestBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Test Build", "Stop the game first (the square button), then try again.", "OK");
            return;
        }

        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DraftBuild"));
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(folder, "DraftGame.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.AutoRunPlayer, // start the game when the build is done
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("DRAFT TEST BUILD OK: " + options.locationPathName
                + " (" + (summary.totalSize / (1024 * 1024)) + " MB, " + (int)summary.totalTime.TotalSeconds + " s)");
        }
        else
        {
            Debug.LogError("DRAFT TEST BUILD FAILED: " + summary.result + ", errors: " + summary.totalErrors);
            EditorUtility.DisplayDialog("Test Build", "The build failed. Tell Claude: the details are in the Console.", "OK");
        }
    }
}
