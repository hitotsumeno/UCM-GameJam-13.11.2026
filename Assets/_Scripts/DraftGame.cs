using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum GameState { Playing, Won, Lost }

// Runs the game. Lives on the "DraftGame" object in DraftScene; the camera, the HUD,
// the light and the level prefabs are assigned in the Inspector. Nothing is built from
// code: a level is an ordinary prefab, instantiated on start and destroyed on restart.
public class DraftGame : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    public static DraftGame Instance;

    [SerializeField] Hud hud;
    [SerializeField] CameraRig cameraRig;
    [Tooltip("Global Light 2D of the scene; every level gives it its own colour (Level.lightColor)")]
    [SerializeField] Light2D globalLight;
    [Tooltip("Level prefabs in play order")]
    [SerializeField] Level[] levels;
    [Tooltip("Level started when pressing Play (1 = first)")]
    [SerializeField] int startLevel = 1;
    [Tooltip("Prefab used by Bullet.Spawn when the caller has none")]
    [SerializeField] Bullet bulletPrefab;
    [Tooltip("Flash shown where the player falls into a black hole and where they come out")]
    [SerializeField] Blast blastPrefab;

    public GameState State { get; private set; }
    public float Oxygen { get; private set; }
    public int Combo { get; private set; }
    public int Breaches { get; private set; }   // holes in the hull; each one drains oxygen
    public Bullet BulletPrefab { get { return bulletPrefab; } }

    public Transform PlayerTransform
    {
        get { return level != null && level.player != null ? level.player.transform : null; }
    }

    public float PlayerSpeed
    {
        get { return level != null && level.player != null ? level.player.Speed : 0f; }
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
        BlackHole.ResetStatics();
        PixelFont.ResetStatics();
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
        // Esc closes the game (only matters in the built game, not in the editor).
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

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
        if (level.player != null)
        {
            Vector2 pos = level.player.transform.position;
            outside = !level.bounds.Contains(pos);
            if (level.ship != null) hud.SetCompass((Vector2)level.ship.position - pos);
        }
        hud.SetSignalLost(outside && State == GameState.Playing);

        UpdateCombo();

        if (State == GameState.Playing)
        {
            float perSecond = T.OxygenDrainPerSecond;
            if (outside) perSecond *= T.OutOfBoundsDrainMultiplier;
            perSecond += Breaches * T.BreachDrainPerSecond; // every breach leaks air
            SpendOxygen(perSecond * Time.deltaTime);
        }
    }

    // --- Hull breaches: there are no lives, every hard hit opens a leak ---

    // from: where the hit came from (the crack appears on that side of the ship).
    public void BreachHull(Vector2 from)
    {
        if (State != GameState.Playing) return;
        if (Breaches < T.BreachMax)
        {
            Breaches++;
            if (level != null && level.player != null) level.player.AddBreach(from);
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
        SpendOxygen(T.WormholeOxygenCost);
        hud.Toast(Texts.Wormhole);
        if (level != null)
        {
            Blast.Spawn(blastPrefab, from, 5f, level.transform); // flash where they vanished and where they came out
            Blast.Spawn(blastPrefab, to, 5f, level.transform);
        }
        if (cameraRig != null) cameraRig.SnapToTarget();
    }

    // Seals one breach. Called when an oxygen tank is picked up.
    public void RepairBreach()
    {
        if (State != GameState.Playing || Breaches <= 0) return;
        Breaches--;
        if (level != null && level.player != null) level.player.RemoveBreach();
        hud.SetBreaches(Breaches);
        hud.Toast(Texts.Sealed);
    }

    // --- Oxygen ---

    public void SpendOxygen(float amount)
    {
        if (State != GameState.Playing) return;
        Oxygen = Mathf.Max(0f, Oxygen - amount);
        hud.SetOxygen(Oxygen / T.OxygenMax);
        if (Oxygen <= 0f) Lose(Texts.OutOfAir);
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
        hud.Toast(Texts.TooFast);
        CameraRig.Shake();
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (level != null && level.player != null) level.player.Stop();
        hud.ShowMessage(Texts.Docked, new Color(0.45f, 1f, 0.55f), levelIndex + "/" + levels.Length,
            Mathf.RoundToInt(Oxygen), Texts.KeyNext);
    }

    // title: what killed the player, shown in big letters.
    public void Lose(string title)
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        hud.ShowMessage(title, new Color(1f, 0.35f, 0.3f), null, -1, Texts.KeyRetry);
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
        if (globalLight != null) globalLight.color = level.lightColor;

        State = GameState.Playing;
        Breaches = 0;
        hud.SetBreaches(0);
        Oxygen = Mathf.Clamp(level.oxygen, 1f, T.OxygenMax);
        hud.ResetOxygen(Oxygen / T.OxygenMax);
        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.ShowTitle(levelIndex + "/" + levels.Length + "  " + level.levelName);
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
