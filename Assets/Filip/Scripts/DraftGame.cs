using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

// Builds the whole game from code, but only in the scene named DraftScene.
public class DraftGame : MonoBehaviour
{
    public const string SceneName = "DraftScene";

    public static DraftGame Instance;

    public GameState State { get; private set; }
    public float Oxygen { get; private set; }

    Transform worldRoot;   // everything of the current level lives under this; destroy it to clear
    CameraRig cameraRig;
    Hud hud;
    PlayerController player;

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
        hud = Hud.Create();
        BuildWorld();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildWorld();
            return;
        }

        if (State == GameState.Playing)
            SpendOxygen(Tuning.OxygenDrainPerSecond * Time.deltaTime);
    }

    // --- Game state ---

    public void SpendOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Max(0f, Oxygen - amount);
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
        if (Oxygen <= 0f) Lose();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (player != null) player.Stop();
        hud.ShowMessage("DOCKED", new Color(0.4f, 1f, 0.5f));
    }

    public void Lose()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        hud.ShowMessage("OUT OF OXYGEN", new Color(1f, 0.35f, 0.3f));
    }

    // --- Scene setup ---

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

    // --- World ---

    public void ClearWorld()
    {
        if (worldRoot != null)
        {
            // Deactivate first: Destroy only happens at the end of the frame.
            worldRoot.gameObject.SetActive(false);
            Destroy(worldRoot.gameObject);
        }
        worldRoot = null;
        player = null;
    }

    // Also used for restart: clears everything and builds it again.
    public void BuildWorld()
    {
        ClearWorld();
        worldRoot = new GameObject("World").transform;

        Vector2 start = Vector2.zero;
        BuildStars(start);
        player = PlayerController.Create(start, worldRoot);
        Ship.Create(start + Vector2.right * Tuning.ShipDistance, worldRoot);

        State = GameState.Playing;
        Oxygen = Tuning.OxygenMax;
        hud.SetOxygen(1f);
        hud.HideMessage();

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
