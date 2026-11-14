using UnityEngine;
using UnityEngine.UI;
using TMPro;

// On-screen interface. Icons and numbers instead of sentences:
//   top-left     "O2" + air bar cut into cells, red cracks = leaks in the hull
//   top-right    combo
//   screen edge  arrow to the ship with the distance (hidden while the ship is in view)
// All lettering is the game's own pixel font (PixelLabel / PixelFont).
// The HUD object lives in DraftScene; its parts are assigned in the Inspector
// (made by Filip > Rebuild Prefabs And Scene).
public class Hud : MonoBehaviour
{
    static readonly Color AirColor = new Color(0.35f, 0.9f, 1f);
    static readonly Color LeakingColor = new Color(1f, 0.62f, 0.3f);
    static readonly Color BadColor = new Color(1f, 0.35f, 0.3f);
    static readonly Color EmptyCell = new Color(1f, 1f, 1f, 0.16f);
    static readonly Color EmptyDanger = new Color(1f, 0.32f, 0.25f, 0.27f); // empty cells below LowAir
    static readonly Color LostColor = new Color(1f, 0.75f, 0.7f);           // air just lost in one go

    const float LowAir = 0.25f;     // below this share the bar turns red and blinks
    const float ChunkLoss = 0.015f; // a drop bigger than this at once leaves a pale tail (a shot costs 0.01)
    const float TrailHold = 0.35f;  // seconds the tail stays before it runs out
    const float TrailSpeed = 0.6f;  // how fast the tail runs out (bar shares per second)
    const float GainTime = 0.6f;    // seconds newly gained air stays bright

    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

<<<<<<< HEAD
    [Header("Compass")]
    [SerializeField] RectTransform compassNeedle;
    [SerializeField] Text compassText;
    [SerializeField] TMP_Text speedText; // TextMeshPro - Text (UI) on the HUD canvas
    [SerializeField] Image speedIcon;
=======
    [Header("Air")]
    [Tooltip("Cells of the air bar, left to right")]
    [SerializeField] Image[] airCells;
    [Tooltip("The O2 sign in front of the bar; turns red and blinks when air is low")]
    [SerializeField] Image airIcon;
    [Tooltip("Red cracks under the bar, one per hull breach")]
    [SerializeField] GameObject[] leakMarks;

    [Header("Whole screen")]
    [SerializeField] Image damageFlash;
    [Tooltip("Dark screen edges in the storm colour while the ship is in the storm beyond the level edge")]
    [SerializeField] Image stormVignette;

    [Header("Arrow to the ship")]
    [SerializeField] RectTransform shipArrow;
    [SerializeField] PixelLabel shipDistance;
>>>>>>> filip-code-to-prod

    [Header("Combo")]
    [SerializeField] GameObject comboRoot;
    [SerializeField] PixelLabel comboLabel;
    [SerializeField] RectTransform comboBar;

    [Header("Short lines")]
    [SerializeField] PixelLabel toast;
    [SerializeField] PixelLabel title;

    [Header("End of a level")]
    [SerializeField] GameObject endRoot;
    [SerializeField] PixelLabel endCounter;
    [SerializeField] PixelLabel endTitle;
    [SerializeField] GameObject endAirRow;
    [SerializeField] RectTransform endAirIcon;
    [SerializeField] PixelLabel endAir;
    [SerializeField] RectTransform endKey;
    [SerializeField] PixelLabel endKeyLabel;

    RectTransform canvasRect;
    float airFraction = 1f;
    float storm;            // 0 = in the level, 1 = deep in the storm
    Color stormColor = new Color(1f, 0.3f, 0.25f);
    float trail = 1f;       // where the pale tail of lost air ends
    float trailHold;
    float gainFrom = -1f;   // newly gained air starts here (-1 = none)
    float gainTimer;
    bool leaking;
    float damageTimer;
    int shownDistance = -1;
    int shownCombo = -1;
    float toastTimer;
    float titleTimer;

    void Awake()
    {
        canvasRect = (RectTransform)transform;
        if (airIcon == null || leakMarks == null || leakMarks.Length == 0 || GetComponent<ThreatMarkers>() == null
            || stormVignette == null)
            Debug.LogWarning("HUD is out of date: run Filip > Rebuild Prefabs And Scene.", this);
        HideMessage();
        SetStorm(0f);
        SetBreaches(0);
        toast.Show(false);
        title.Show(false);
        comboRoot.SetActive(false);
        shipArrow.gameObject.SetActive(false);
        shipDistance.Show(false);
        damageFlash.color = new Color(1f, 0.1f, 0.05f, 0f);
    }

    void Update()
    {
        if (trailHold > 0f) trailHold -= Time.deltaTime;
        else if (trail > airFraction) trail = Mathf.Max(airFraction, trail - TrailSpeed * Time.deltaTime);
        if (gainTimer > 0f)
        {
            gainTimer -= Time.deltaTime;
            if (gainTimer <= 0f) gainFrom = -1f;
        }
        PaintAir();

        if (damageTimer > 0f)
        {
            damageTimer = Mathf.Max(0f, damageTimer - Time.deltaTime * 2.5f);
            damageFlash.color = new Color(1f, 0.1f, 0.05f, 0.45f * damageTimer);
        }
        PaintStorm();

        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f) toast.Show(false);
        }
        if (titleTimer > 0f)
        {
            titleTimer -= Time.deltaTime;
            title.Alpha(Mathf.Clamp01(titleTimer * 2f)); // fades out over the last half second
            if (titleTimer <= 0f) title.Show(false);
        }
    }

    static bool Blink(float perSecond)
    {
        return Mathf.Repeat(Time.unscaledTime * perSecond, 2f) < 1f;
    }

    // --- Air ---

    // fraction: air left, 0..1. A big loss at once leaves a pale tail that runs out a moment
    // later (the player sees how much a hit cost); gained air stays bright for a moment.
    public void SetOxygen(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (airFraction - fraction > ChunkLoss)
        {
            trail = Mathf.Max(trail, airFraction);
            trailHold = TrailHold;
        }
        else if (fraction < airFraction)
        {
            if (trailHold <= 0f) trail = fraction;   // ordinary slow drain: no tail
        }
        else if (fraction > airFraction + 0.0001f)
        {
            if (gainFrom < 0f || gainFrom > airFraction) gainFrom = airFraction;
            gainTimer = GainTime;
        }
        airFraction = fraction;
        if (trail < fraction) trail = fraction;
        PaintAir();
    }

    // Start of a level: the bar jumps to the new value without a tail or a highlight.
    public void ResetOxygen(float fraction)
    {
        airFraction = Mathf.Clamp01(fraction);
        trail = airFraction;
        trailHold = 0f;
        gainFrom = -1f;
        gainTimer = 0f;
        PaintAir();
    }

    // A crystal was collected: keep the gained part bright a little longer.
    public void FlashOxygen()
    {
        if (gainFrom >= 0f) gainTimer = GainTime;
    }

    void PaintAir()
    {
        bool low = airFraction < LowAir;
        bool dim = low && !Blink(5f);
        Color full = low ? BadColor : (leaking ? LeakingColor : AirColor);
        if (dim) full.a = 0.45f;
        if (airIcon != null) airIcon.color = low ? (dim ? new Color(1f, 0.35f, 0.3f, 0.45f) : BadColor) : Color.white;

        int n = airCells.Length;
        float lit = airFraction * n, tail = trail * n;
        float gainStart = gainFrom >= 0f ? gainFrom * n : float.MaxValue;
        float glow = Mathf.Clamp01(gainTimer / GainTime);
        for (int i = 0; i < n; i++)
        {
            float share = Mathf.Clamp01(lit - i);            // 1 = full cell, 0 = empty
            Color empty = i < n * LowAir ? EmptyDanger : EmptyCell;
            Color c;
            if (share > 0f)
            {
                c = Color.Lerp(empty, full, 0.35f + 0.65f * share);
                if (i + 1 > gainStart) c = Color.Lerp(c, Color.white, 0.75f * glow); // just gained
                if (Crackle(i)) c.a *= 0.3f;                                      // storm interference
            }
            else if (tail - i > 0f) c = LostColor;              // just lost
            else c = empty;
            airCells[i].color = c;
        }
    }

    // count: holes in the hull. One red crack each; the air bar turns orange while it leaks.
    public void SetBreaches(int count)
    {
        leaking = count > 0;
        if (leakMarks != null)   // empty until Filip > Rebuild Prefabs And Scene has made the HUD
            for (int i = 0; i < leakMarks.Length; i++)
                if (leakMarks[i] != null) leakMarks[i].SetActive(i < count);
        PaintAir();
    }

    // Red flash of the whole screen: the hull was just breached.
    public void FlashDamage()
    {
        damageTimer = 1f;
        damageFlash.color = new Color(1f, 0.1f, 0.05f, 0.45f);
    }

    // --- Ship arrow ---

    // toShip: vector from the player to the ship, in world units.
    public void SetCompass(Vector2 toShip)
    {
        Vector2 size = canvasRect.rect.size;
        if (size.x < 1f || size.y < 1f) size = new Vector2(1920f, 1080f);

        // While the ship is on screen the arrow is not needed.
        float viewH = T != null ? T.CameraSize : 10f, viewW = viewH * size.x / size.y;
        bool inView = Mathf.Abs(toShip.x) < viewW - 2f && Mathf.Abs(toShip.y) < viewH - 2f;
        shipArrow.gameObject.SetActive(!inView);
        shipDistance.Show(!inView);
        if (inView) return;

        Vector2 dir = toShip.normalized;
        float halfW = size.x * 0.5f - 80f, halfH = size.y * 0.5f - 80f;
        float reach = Mathf.Min(
            Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
            Mathf.Abs(dir.y) > 0.001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);
        Vector2 pos = dir * reach;

        shipArrow.anchoredPosition = pos;
        shipArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        ((RectTransform)shipDistance.transform).anchoredPosition = pos - dir * 58f;

        int distance = Mathf.RoundToInt(toShip.magnitude);
        if (distance != shownDistance)
        {
            shownDistance = distance;
            shipDistance.Set(distance.ToString());
        }
    }

<<<<<<< HEAD
    // slowEnough: the player may dock at this speed.
    public void SetSpeed(float speed, bool slowEnough)
    {
        int rounded = Mathf.RoundToInt(speed);
        if (rounded == shownSpeed && slowEnough == shownSpeedOk) return;
        shownSpeed = rounded;
        shownSpeedOk = slowEnough;
        speedText.text = "SPEED " + rounded;
        speedText.color = slowEnough ? GoodColor : BadColor;
        speedIcon.color = slowEnough ? GoodColor : BadColor;   
    }
=======
    // --- Combo ---
>>>>>>> filip-code-to-prod

    // combo: kills in a row (shown from x2). timeLeft: 1 = just continued, 0 = about to end.
    public void SetCombo(int combo, float timeLeft)
    {
        bool show = combo >= 2;
        if (comboRoot.activeSelf != show) comboRoot.SetActive(show);
        if (!show) { shownCombo = -1; return; }

        if (combo != shownCombo)
        {
            shownCombo = combo;
            comboLabel.Set(Texts.Combo + combo);
        }
        comboBar.anchorMin = new Vector2(1f - Mathf.Clamp01(timeLeft), 0f); // shrinks toward the right edge
    }

    // --- Short lines ---

    // Short notice that disappears by itself.
    public void Toast(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        toast.Set(text);
        toast.Show(true);
        toastTimer = 1.2f;
    }

    // Level name at the start of a level.
    public void ShowTitle(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        title.Show(show);
        if (!show) { titleTimer = 0f; return; }
        title.Set(text);
        title.Alpha(1f);
        titleTimer = 3f;
    }

    // --- Storm beyond the level edge ---

    // amount: 0 = inside the level, 1 = deep in the storm. The screen edges darken in the
    // storm colour and the air bar crackles with interference; no words needed.
    public void SetStorm(float amount)
    {
        storm = Mathf.Clamp01(amount);
        PaintStorm();
    }

    public void SetStormColor(Color c)
    {
        stormColor = c;
    }

    void PaintStorm()
    {
        if (stormVignette == null) return;
        bool on = storm > 0.001f;
        if (stormVignette.gameObject.activeSelf != on) stormVignette.gameObject.SetActive(on);
        if (!on) return;
        float flicker = 0.92f + 0.08f * Mathf.Sin(Time.unscaledTime * 13f);
        float a = Mathf.Lerp(0.35f, 0.92f, storm) * flicker;
        stormVignette.color = new Color(stormColor.r * 0.22f, stormColor.g * 0.22f, stormColor.b * 0.22f, a);
    }

    // Interference on the air bar in the storm: lit cells flicker at random.
    bool Crackle(int cell)
    {
        if (storm <= 0f) return false;
        int step = (int)(Time.unscaledTime * 14f);
        unchecked
        {
            uint h = (uint)(cell * 374761393 + step * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFF) / 65536f < 0.4f * storm;
        }
    }

    // --- End of a level ---

    // counter: small line over the word ("2/3"), may be null. air: air left, -1 hides it.
    // key: the key that continues ("ENTER", "R").
    public void ShowMessage(string text, Color color, string counter, int air, string key)
    {
        endRoot.SetActive(true);

        bool hasCounter = !string.IsNullOrEmpty(counter);
        endCounter.Show(hasCounter);
        if (hasCounter) endCounter.Set(counter);

        endTitle.Set(text);
        endTitle.Image.color = color;

        endAirRow.SetActive(air >= 0);
        if (air >= 0)
        {
            // Icon and number side by side, centred as one piece.
            endAir.Set(air.ToString());
            float iconW = endAirIcon.sizeDelta.x, gap = 16f;
            float total = iconW + gap + endAir.Width;
            endAirIcon.anchoredPosition = new Vector2(-total * 0.5f + iconW * 0.5f, 0f);
            ((RectTransform)endAir.transform).anchoredPosition = new Vector2(total * 0.5f - endAir.Width * 0.5f, 0f);
        }

        endKeyLabel.Set(key);
        endKey.sizeDelta = new Vector2(endKeyLabel.Width + 36f, 52f);
    }

    public void HideMessage()
    {
        endRoot.SetActive(false);
    }
}
