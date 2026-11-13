using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

// Runs the game. Everything is built from code, but only in the scene named DraftScene.
// The level objects themselves are created by WorldBuilder.
public class DraftGame : MonoBehaviour
{
    public const string SceneName = "DraftScene";

    public static DraftGame Instance;

    public GameState State { get; private set; }
    public float Oxygen { get; private set; }

    LevelWorld world;      // current level; null when cleared
    CameraRig cameraRig;
    Hud hud;

    // Reset static fields (works even when domain reload is disabled).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        CameraRig.ResetStatics();
        SpriteFactory.ResetStatics();
        Art.ResetStatics();
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

        bool outside = false;
        if (world != null && world.player != null)
        {
            Vector2 pos = world.player.transform.position;
            outside = !world.bounds.Contains(pos);
            if (world.ship != null) hud.SetCompass((Vector2)world.ship.position - pos);
        }
        hud.SetSignalLost(outside && State == GameState.Playing);

        if (State == GameState.Playing)
        {
            float drain = Tuning.OxygenDrainPerSecond * Time.deltaTime;
            if (outside) drain *= Tuning.OutOfBoundsDrainMultiplier;
            SpendOxygen(drain);
        }
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
        if (world != null && world.player != null) world.player.Stop();
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
        if (world != null && world.root != null)
        {
            // Deactivate first: Destroy only happens at the end of the frame.
            world.root.gameObject.SetActive(false);
            Destroy(world.root.gameObject);
        }
        world = null;
    }

    // Also used for restart: clears everything and builds it again.
    public void BuildWorld()
    {
        ClearWorld();
        world = WorldBuilder.Build();

        State = GameState.Playing;
        Oxygen = Tuning.OxygenMax;
        hud.SetOxygen(1f);
        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.SetCompass(world.ship.position - world.player.transform.position);

        cameraRig.target = world.player.transform;
        cameraRig.SnapToTarget();
    }
}
