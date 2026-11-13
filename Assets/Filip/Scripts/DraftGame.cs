using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Builds the whole game from code, but only in the scene named DraftScene.
public class DraftGame : MonoBehaviour
{
    public const string SceneName = "DraftScene";

    public static DraftGame Instance;

    Transform worldRoot;   // everything of the current level lives under this; destroy it to clear
    CameraRig cameraRig;

    // Reset static fields (works even when domain reload is disabled).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        CameraRig.ResetStatics();
        SpriteFactory.ResetStatics();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (SceneManager.GetActiveScene().name != SceneName) return;
        var go = new GameObject("DraftGame");
        go.AddComponent<DraftGame>();
    }

    void Awake()
    {
        Instance = this;
        EnsureGlobalLight();
        SetupCamera();
        BuildWorld();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    static void EnsureGlobalLight()
    {
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return;

        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }
        cam.orthographic = true;
        cam.orthographicSize = Tuning.CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        cameraRig = cam.GetComponent<CameraRig>();
        if (cameraRig == null) cameraRig = cam.gameObject.AddComponent<CameraRig>();
    }

    public void ClearWorld()
    {
        if (worldRoot != null) Destroy(worldRoot.gameObject);
        worldRoot = null;
    }

    public void BuildWorld()
    {
        ClearWorld();
        worldRoot = new GameObject("World").transform;

        Vector2 start = Vector2.zero;
        BuildStars(start);
        var player = PlayerController.Create(start, worldRoot);

        cameraRig.target = player.transform;
        cameraRig.SnapToTarget();
    }

    void BuildStars(Vector2 center)
    {
        var stars = new GameObject("Stars").transform;
        stars.SetParent(worldRoot, false);
        var circle = SpriteFactory.Circle();
        float half = Tuning.StarFieldSize * 0.5f;
        for (int i = 0; i < Tuning.StarCount; i++)
        {
            var pos = center + new Vector2(Random.Range(-half, half), Random.Range(-half, half));
            float size = Random.Range(0.05f, 0.18f);
            float b = Random.Range(0.5f, 1f);
            var sr = SpriteFactory.MakeSprite("Star", stars, circle,
                new Color(b, b, b * Random.Range(0.9f, 1.1f)), Vector2.one * size, -100);
            sr.transform.position = pos;
        }
    }
}
