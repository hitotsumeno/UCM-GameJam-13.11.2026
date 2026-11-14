using UnityEngine;
using UnityEngine.UI;

// On-screen interface. Icons and numbers instead of sentences:
//   top-left     tank icon + air bar cut into cells, red drops = leaks in the hull
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

    const float LowAir = 0.25f;   // below this share the bar turns red and blinks

    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [Header("Air")]
    [Tooltip("Cells of the air bar, left to right")]
    [SerializeField] Image[] airCells;
    [Tooltip("Red drops under the bar, one per hull breach")]
    [SerializeField] GameObject[] leakDrops;

    [Header("Whole screen")]
    [SerializeField] Image damageFlash;
    [SerializeField] GameObject outOverlay;
    [SerializeField] PixelLabel outLabel;

    [Header("Arrow to the ship")]
    [SerializeField] RectTransform shipArrow;
    [SerializeField] PixelLabel shipDistance;

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
    float airFlash;
    bool leaking;
    float damageTimer;
    int shownDistance = -1;
    int shownCombo = -1;
    float toastTimer;
    float titleTimer;

    void Awake()
    {
        canvasRect = (RectTransform)transform;
        outLabel.Set(Texts.OutOfBounds);
        HideMessage();
        SetSignalLost(false);
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
        if (airFlash > 0f) airFlash = Mathf.Max(0f, airFlash - Time.deltaTime * 4f);
        PaintAir();

        if (damageTimer > 0f)
        {
            damageTimer = Mathf.Max(0f, damageTimer - Time.deltaTime * 2.5f);
            damageFlash.color = new Color(1f, 0.1f, 0.05f, 0.45f * damageTimer);
        }
        if (outOverlay.activeSelf) outLabel.Alpha(Blink(6f) ? 1f : 0.35f);

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

    public void SetOxygen(float fraction)
    {
        airFraction = Mathf.Clamp01(fraction);
        PaintAir();
    }

    // The air bar flashes white when a crystal is collected.
    public void FlashOxygen()
    {
        airFlash = 1f;
    }

    void PaintAir()
    {
        bool low = airFraction < LowAir;
        Color full = low ? BadColor : (leaking ? LeakingColor : AirColor);
        if (low && !Blink(5f)) full.a = 0.45f;
        full = Color.Lerp(full, Color.white, airFlash);

        float lit = airFraction * airCells.Length;
        for (int i = 0; i < airCells.Length; i++)
        {
            float share = Mathf.Clamp01(lit - i);            // 1 = full cell, 0 = empty
            airCells[i].color = share <= 0f ? EmptyCell : Color.Lerp(EmptyCell, full, 0.35f + 0.65f * share);
        }
    }

    // count: holes in the hull. One red drop each; the air bar turns orange while it leaks.
    public void SetBreaches(int count)
    {
        leaking = count > 0;
        for (int i = 0; i < leakDrops.Length; i++)
            leakDrops[i].SetActive(i < count);
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

    // --- Combo ---

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

    // Red tint and a blinking line while the player is outside the level.
    public void SetSignalLost(bool on)
    {
        if (outOverlay.activeSelf != on) outOverlay.SetActive(on);
        outLabel.Show(on);
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
