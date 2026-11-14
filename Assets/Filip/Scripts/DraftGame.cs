using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

// Runs the game. Everything is built from code, but only in the scene named DraftScene.
// Levels come from text files (LevelData); the level objects are created by WorldBuilder.
public class DraftGame : MonoBehaviour
{
    public const string SceneName = "DraftScene";

    public static DraftGame Instance;

    public GameState State { get; private set; }
    public float Oxygen { get; private set; }
    public int Combo { get; private set; }

    public Transform PlayerTransform
    {
        get { return world != null && world.player != null ? world.player.transform : null; }
    }

    LevelWorld world;      // current level; null when cleared
    CameraRig cameraRig;
    Hud hud;
    float comboTimer;
    int levelIndex = 1;    // 1 = level1.txt
    int levelCount;        // 0 = no level files: the random test ground is used

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
        levelCount = LevelData.Count();
        levelIndex = 1;
        BuildWorld();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // R restarts the current level at any moment.
        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildWorld();
            return;
        }

        // After docking, Enter goes to the next level (after the last one, back to the first).
        if (State == GameState.Won && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            levelIndex = levelIndex < levelCount ? levelIndex + 1 : 1;
            BuildWorld();
            return;
        }

        bool outside = false;
        string hint = null;
        if (world != null && world.player != null)
        {
            Vector2 pos = world.player.transform.position;
            outside = !world.bounds.Contains(pos);
            if (world.station != null) hud.SetCompass((Vector2)world.station.position - pos);

            float speed = world.player.Speed;
            hud.SetSpeed(speed, speed <= Tuning.DockMaxSpeed);

            // The nearest hint place within reach decides which hint is shown.
            float best = Tuning.HintRadius;
            for (int i = 0; i < world.hintPositions.Count; i++)
            {
                float d = Vector2.Distance(pos, world.hintPositions[i]);
                if (d < best) { best = d; hint = world.hintTexts[i]; }
            }
        }
        hud.SetSignalLost(outside && State == GameState.Playing);
        hud.SetHint(State == GameState.Playing ? hint : null);

        UpdateCombo();

        if (State == GameState.Playing)
        {
            float drain = Tuning.OxygenDrainPerSecond * Time.deltaTime;
            if (outside) drain *= Tuning.OutOfBoundsDrainMultiplier;
            SpendOxygen(drain);
        }
    }

    // --- Oxygen ---

    public void SpendOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Max(0f, Oxygen - amount);
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
        if (Oxygen <= 0f) Lose();
    }

    public void AddOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Min(Tuning.OxygenMax, Oxygen + amount);
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
    }

    public void CollectCrystal(float amount)
    {
        AddOxygen(amount);
        hud.FlashOxygen();
    }

    // --- Combo ---

    // Called when something is destroyed. Returns the combo count including this kill.
    public int RegisterKill()
    {
        Combo = comboTimer > 0f ? Mathf.Min(Combo + 1, Tuning.ComboMax) : 1;
        comboTimer = Tuning.ComboWindow;
        hud.SetCombo(Combo, 1f);
        return Combo;
    }

    // Called when the player takes a hard hit.
    public void BreakCombo()
    {
        Combo = 0;
        comboTimer = 0f;
        hud.SetCombo(0, 0f);
    }

    void UpdateCombo()
    {
        if (comboTimer <= 0f) return;
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f) BreakCombo();
        else hud.SetCombo(Combo, comboTimer / Tuning.ComboWindow);
    }

    // --- Docking, winning, losing ---

    // Called by the station when the player touches it. Docking only works at low speed.
    public void PlayerReachedStation(PlayerController player, Vector2 stationPosition)
    {
        if (State != GameState.Playing) return;
        if (player.Speed <= Tuning.DockMaxSpeed)
        {
            Win();
            return;
        }
        player.BounceFrom(stationPosition);
        SpendOxygen(Tuning.DockPenalty);
        hud.Toast("TOO FAST");
        CameraRig.Shake();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (world != null && world.player != null) world.player.Stop();

        bool last = levelIndex >= levelCount;
        string sub = "OXYGEN LEFT " + Mathf.RoundToInt(Oxygen) + "     "
            + (last ? "ALL LEVELS COMPLETE. PRESS ENTER TO PLAY AGAIN" : "PRESS ENTER FOR THE NEXT LEVEL");
        hud.ShowMessage("DOCKED", new Color(0.4f, 1f, 0.5f), sub);
    }

    public void Lose()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        hud.ShowMessage("OUT OF OXYGEN", new Color(1f, 0.35f, 0.3f), "PRESS R TO RESTART");
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

    // Builds the current level. Also used for restart: clears everything and builds it again.
    public void BuildWorld()
    {
        ClearWorld();
        LevelData level = levelCount > 0 ? LevelData.Load(levelIndex) : null;
        world = WorldBuilder.Build(level);

        State = GameState.Playing;
        Oxygen = level != null ? Mathf.Clamp(level.oxygen, 1f, Tuning.OxygenMax) : Tuning.OxygenMax;
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.SetHint(null);
        hud.ShowTitle(level != null ? "LEVEL " + level.index + ": " + level.name : null);
        hud.SetCompass(world.station.position - world.player.transform.position);
        BreakCombo();

        cameraRig.target = world.player.transform;
        cameraRig.SnapToTarget();
    }
}
