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
    public int Breaches { get; private set; }   // holes in the hull; each one drains oxygen

    public Transform PlayerTransform
    {
        get { return world != null && world.player != null ? world.player.transform : null; }
    }

    LevelWorld world;      // current level; null when cleared
    CameraRig cameraRig;
    Hud hud;
    Light2D globalLight;   // lights every sprite; each level gives it its own colour
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
        BlackHole.ResetStatics();
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
        globalLight = EnsureGlobalLight();
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
        // Esc closes the game (only matters in the built game, not in the editor).
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

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
            if (world.ship != null) hud.SetCompass((Vector2)world.ship.position - pos);

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
            float perSecond = Tuning.OxygenDrainPerSecond;
            if (outside) perSecond *= Tuning.OutOfBoundsDrainMultiplier;
            perSecond += Breaches * Tuning.BreachDrainPerSecond; // every breach leaks air
            SpendOxygen(perSecond * Time.deltaTime);
        }
    }

    // --- Hull breaches: there are no lives, every hard hit opens a leak ---

    // from: where the hit came from (the crack appears on that side of the ship).
    public void BreachHull(Vector2 from)
    {
        if (State != GameState.Playing) return;
        if (Breaches < Tuning.BreachMax)
        {
            Breaches++;
            if (world != null && world.player != null) world.player.AddBreach(from);
        }
        BreakCombo();
        hud.SetBreaches(Breaches);
        hud.FlashDamage();
        hud.Toast(Texts.Breach);
        CameraRig.ShakeHard();
    }

    // The player fell into a black hole and came out of another one.
    // from: the hole they fell into, to: where they are now.
    public void ThrownThroughWormhole(Vector2 from, Vector2 to)
    {
        if (State != GameState.Playing) return;
        BreachHull(from);                         // the hull does not like it
        SpendOxygen(Tuning.WormholeOxygenCost);
        hud.Toast(Texts.Wormhole);
        if (world != null)
        {
            Blast.Create(from, 5f, world.root);   // flash where they vanished and where they came out
            Blast.Create(to, 5f, world.root);
        }
        cameraRig.SnapToTarget();
    }

    // Seals one breach. Called when an oxygen tank is picked up.
    public void RepairBreach()
    {
        if (State != GameState.Playing || Breaches <= 0) return;
        Breaches--;
        if (world != null && world.player != null) world.player.RemoveBreach();
        hud.SetBreaches(Breaches);
        hud.Toast(Texts.Sealed);
    }

    // --- Oxygen ---

    public void SpendOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Max(0f, Oxygen - amount);
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
        if (Oxygen <= 0f) Lose(Texts.OutOfAir);
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

    // Called by the ship when the player touches it. Docking only works at low speed.
    public void PlayerReachedShip(PlayerController player, Vector2 shipPosition)
    {
        if (State != GameState.Playing) return;
        if (player.Speed <= Tuning.DockMaxSpeed)
        {
            Win();
            return;
        }
        player.BounceFrom(shipPosition);
        SpendOxygen(Tuning.DockPenalty);
        hud.Toast(Texts.TooFast);
        CameraRig.Shake();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (world != null && world.player != null) world.player.Stop();

        bool last = levelIndex >= levelCount;
        string sub = Texts.AirLeft + Mathf.RoundToInt(Oxygen) + "      "
            + (last ? Texts.TheEnd : Texts.Next);
        hud.ShowMessage(Texts.Docked, new Color(0.4f, 1f, 0.5f), sub);
    }

    // title: what killed the player, shown in big letters.
    public void Lose(string title)
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        hud.ShowMessage(title, new Color(1f, 0.35f, 0.3f), Texts.Retry);
    }

    // --- Scene setup ---

    // Finds the scene's global light or creates one. Its colour is set per level.
    static Light2D EnsureGlobalLight()
    {
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return l;

        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
        return light;
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
        if (globalLight != null) globalLight.color = level != null ? level.light : Color.white;

        State = GameState.Playing;
        Breaches = 0;
        hud.SetBreaches(0);
        Oxygen = level != null ? Mathf.Clamp(level.oxygen, 1f, Tuning.OxygenMax) : Tuning.OxygenMax;
        hud.SetOxygen(Oxygen / Tuning.OxygenMax);
        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.SetHint(null);
        hud.ShowTitle(level != null ? level.index + " / " + levelCount + "   " + level.name : null);
        hud.SetCompass(world.ship.position - world.player.transform.position);
        BreakCombo();

        cameraRig.target = world.player.transform;
        cameraRig.SnapToTarget();
    }
}
