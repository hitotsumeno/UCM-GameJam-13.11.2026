using UnityEngine;

public enum GameState { Playing, Won, Lost }

// Runs the game. Lives on the "DraftGame" object in DraftScene; the camera, the HUD
// and the level prefabs are assigned in the Inspector. Nothing is built from code:
// a level is an ordinary prefab, instantiated on start and destroyed on restart.
public class DraftGame : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    public static DraftGame Instance;

    [SerializeField] Hud hud;
    [SerializeField] CameraRig cameraRig;
    [Tooltip("Level prefabs in play order")]
    [SerializeField] Level[] levels;
    [Tooltip("Level started when pressing Play (1 = first)")]
    [SerializeField] int startLevel = 1;
    [Tooltip("Prefab used by Bullet.Spawn when the caller has none")]
    [SerializeField] Bullet bulletPrefab;

    public GameState State { get; private set; }
    public float Oxygen { get; private set; }
    public int Combo { get; private set; }
    public Bullet BulletPrefab { get { return bulletPrefab; } }

    public Transform PlayerTransform
    {
        get { return level != null && level.player != null ? level.player.transform : null; }
    }

    Level level;           // current level instance; null when cleared
    float comboTimer;
    int levelIndex = 1;    // 1 = levels[0]

    // Reset static fields (works even when domain reload is disabled).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        Tuning.ResetStatics();
        CameraRig.ResetStatics();
    }

    void Awake()
    {
        Instance = this;
        // Components with an empty Tuning field use this one.
        if (tuning != null) Tuning.Active = tuning;
    }

    void Start()
    {
        if (T == null)
        {
            Debug.LogError("DraftGame: no Tuning asset assigned. Run Filip > Connect Tuning To Prefabs And Scene.", this);
            enabled = false;
            return;
        }
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("DraftGame: no level prefabs assigned.", this);
            enabled = false;
            return;
        }
        levelIndex = Mathf.Clamp(startLevel, 1, levels.Length);
        BuildWorld();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (level == null) return;

        // R restarts the current level at any moment.
        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildWorld();
            return;
        }

        // After docking, Enter goes to the next level (after the last one, back to the first).
        if (State == GameState.Won && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            levelIndex = levelIndex < levels.Length ? levelIndex + 1 : 1;
            BuildWorld();
            return;
        }

        bool outside = false;
        string hint = null;
        if (level.player != null)
        {
            Vector2 pos = level.player.transform.position;
            outside = !level.bounds.Contains(pos);
            if (level.ship != null) hud.SetCompass((Vector2)level.ship.position - pos);

            float speed = level.player.Speed;
            hud.SetSpeed(speed, speed <= T.DockMaxSpeed);

            // The nearest hint place within reach decides which hint is shown.
            float best = T.HintRadius;
            foreach (var h in level.Hints)
            {
                if (h == null) continue;
                float d = Vector2.Distance(pos, h.transform.position);
                if (d < best) { best = d; hint = h.text; }
            }
        }
        hud.SetSignalLost(outside && State == GameState.Playing);
        hud.SetHint(State == GameState.Playing ? hint : null);

        UpdateCombo();

        if (State == GameState.Playing)
        {
            float drain = T.OxygenDrainPerSecond * Time.deltaTime;
            if (outside) drain *= T.OutOfBoundsDrainMultiplier;
            SpendOxygen(drain);
        }
    }

    // --- Oxygen ---

    public void SpendOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Max(0f, Oxygen - amount);
        hud.SetOxygen(Oxygen / T.OxygenMax);
        if (Oxygen <= 0f) Lose();
    }

    public void AddOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Min(T.OxygenMax, Oxygen + amount);
        hud.SetOxygen(Oxygen / T.OxygenMax);
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
        Combo = comboTimer > 0f ? Mathf.Min(Combo + 1, T.ComboMax) : 1;
        comboTimer = T.ComboWindow;
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
        else hud.SetCombo(Combo, comboTimer / T.ComboWindow);
    }

    // --- Docking, winning, losing ---

    // Called by the ship when the player touches it. Docking only works at low speed.
    public void PlayerReachedShip(PlayerController player, Vector2 shipPosition)
    {
        if (State != GameState.Playing) return;
        if (player.Speed <= T.DockMaxSpeed)
        {
            Win();
            return;
        }
        player.BounceFrom(shipPosition);
        SpendOxygen(T.DockPenalty);
        hud.Toast("TOO FAST");
        CameraRig.Shake();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (level != null && level.player != null) level.player.Stop();

        bool last = levelIndex >= levels.Length;
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

    // --- Level ---

    public void ClearWorld()
    {
        if (level != null)
        {
            // Deactivate first: Destroy only happens at the end of the frame.
            level.gameObject.SetActive(false);
            Destroy(level.gameObject);
        }
        level = null;
    }

    // Instantiates the current level prefab. Also used for restart: the old copy is destroyed.
    public void BuildWorld()
    {
        ClearWorld();
        var prefab = levels[levelIndex - 1];
        level = Instantiate(prefab);
        level.name = prefab.name;
        Asteroid.Bounds = level.bounds;

        State = GameState.Playing;
        Oxygen = Mathf.Clamp(level.oxygen, 1f, T.OxygenMax);
        hud.SetOxygen(Oxygen / T.OxygenMax);
        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.SetHint(null);
        hud.ShowTitle("LEVEL " + levelIndex + ": " + level.levelName);
        if (level.ship != null && level.player != null)
            hud.SetCompass(level.ship.position - level.player.transform.position);
        BreakCombo();

        if (cameraRig != null && level.player != null)
        {
            cameraRig.target = level.player.transform;
            cameraRig.SnapToTarget();
        }
    }
}
