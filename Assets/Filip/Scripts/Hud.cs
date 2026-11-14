using UnityEngine;
using UnityEngine.UI;

// On-screen interface, built from code. Icons and numbers instead of sentences:
//   top-left   tank icon + air bar cut into cells, red drops = leaks in the hull
//   top-right  combo
//   screen edge  arrow to the ship with the distance (hidden while the ship is in view)
// All lettering is the game's own pixel font (PixelFont).
public class Hud : MonoBehaviour
{
    static readonly Color AirColor = new Color(0.35f, 0.9f, 1f);
    static readonly Color LeakingColor = new Color(1f, 0.62f, 0.3f);
    static readonly Color GoodColor = new Color(0.45f, 1f, 0.55f);
    static readonly Color BadColor = new Color(1f, 0.35f, 0.3f);
    static readonly Color ComboColor = new Color(1f, 0.85f, 0.3f);
    static readonly Color EmptyCell = new Color(1f, 1f, 1f, 0.16f);
    static readonly Color Ink = new Color(0.05f, 0.07f, 0.14f);

    const int AirCells = 20;
    const float LowAir = 0.25f;   // below this share the bar turns red and blinks

    // One line of pixel text.
    class Label
    {
        public RawImage image;
        public int scale;
        string shown;

        public void Set(string text)
        {
            if (text == shown) return;
            shown = text;
            var tex = PixelFont.Text(text);
            image.texture = tex;
            image.rectTransform.sizeDelta = new Vector2(tex.width * scale, tex.height * scale);
        }

        public float Width { get { return image.rectTransform.sizeDelta.x; } }
        public void Show(bool on) { if (image.gameObject.activeSelf != on) image.gameObject.SetActive(on); }
        public void Alpha(float a) { var c = image.color; c.a = a; image.color = c; }
    }

    RectTransform canvasRect;

    Image[] airCells = new Image[AirCells];
    float airFraction = 1f;
    float airFlash;
    bool leaking;
    RawImage[] leakDrops;

    Image damageFlash;
    float damageTimer;
    GameObject outOverlay;
    Label outLabel;

    RectTransform shipArrow;
    Label shipDistance;
    int shownDistance = -1;

    GameObject comboRoot;
    Label comboLabel;
    RectTransform comboBar;
    int shownCombo = -1;

    Label toast;
    float toastTimer;
    Label title;
    float titleTimer;

    GameObject endRoot;
    Label endCounter;
    Label endTitle;
    GameObject endAirRow;
    RectTransform endAirIcon;
    Label endAir;
    RectTransform endKey;
    Label endKeyLabel;

    public static Hud Create()
    {
        var go = new GameObject("HUD");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var hud = go.AddComponent<Hud>();
        hud.canvasRect = (RectTransform)go.transform;
        hud.Build(go.transform);
        return hud;
    }

    void Build(Transform root)
    {
        // Created first = drawn behind the rest.
        var overlay = MakeImage("OutOverlay", root, new Color(1f, 0f, 0f, 0.2f));
        Stretch(overlay.rectTransform);
        outOverlay = overlay.gameObject;

        damageFlash = MakeImage("DamageFlash", root, new Color(1f, 0.1f, 0.05f, 0f));
        Stretch(damageFlash.rectTransform);

        BuildAir(root);
        BuildCombo(root);
        BuildShipArrow(root);

        outLabel = MakeLabel("OutLabel", root, 6, BadColor);
        Place(outLabel.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f));
        outLabel.Set(Texts.OutOfBounds);

        title = MakeLabel("Title", root, 6, Color.white);
        Place(title.image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f));

        toast = MakeLabel("Toast", root, 5, LeakingColor);
        Place(toast.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f));

        BuildEndScreen(root);

        HideMessage();
        SetSignalLost(false);
        SetBreaches(0);
        toast.Show(false);
        title.Show(false);
        shipArrow.gameObject.SetActive(false);
        shipDistance.Show(false);
    }

    // Tank icon, the air bar cut into cells, and a row of red drops under it (one per leak).
    void BuildAir(Transform root)
    {
        var tank = MakeIcon("AirIcon", root, PixelFont.Icon("tank", PixelFont.Tank), 5, Color.white);
        Place(tank.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -32f));

        const float cellW = 12f, cellH = 26f, gap = 3f;
        for (int i = 0; i < AirCells; i++)
        {
            var cell = MakeImage("AirCell", root, AirColor);
            Place(cell.rectTransform, new Vector2(0f, 1f), new Vector2(92f + i * (cellW + gap), -40f));
            cell.rectTransform.sizeDelta = new Vector2(cellW, cellH);
            airCells[i] = cell;
        }

        leakDrops = new RawImage[Tuning.BreachMax];
        for (int i = 0; i < leakDrops.Length; i++)
        {
            leakDrops[i] = MakeIcon("Leak", root, PixelFont.Icon("drop", PixelFont.Drop), 4, BadColor);
            Place(leakDrops[i].rectTransform, new Vector2(0f, 1f), new Vector2(92f + i * 30f, -76f));
        }
    }

    // Combo in the top-right corner with a thin bar: the time left to continue it.
    void BuildCombo(Transform root)
    {
        var rt = new GameObject("Combo", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(root, false);
        Place(rt, new Vector2(1f, 1f), new Vector2(-40f, -32f));
        rt.sizeDelta = new Vector2(200f, 80f);
        comboRoot = rt.gameObject;

        comboLabel = MakeLabel("Text", rt, 8, ComboColor);
        Place(comboLabel.image.rectTransform, new Vector2(1f, 1f), Vector2.zero);

        var barBg = MakeImage("Bar", rt, EmptyCell);
        Place(barBg.rectTransform, new Vector2(1f, 0f), Vector2.zero);
        barBg.rectTransform.sizeDelta = new Vector2(140f, 8f);
        var fill = MakeImage("Fill", barBg.transform, ComboColor);
        comboBar = fill.rectTransform;
        Stretch(comboBar);

        comboRoot.SetActive(false);
    }

    // Arrow that slides along the screen edge and points at the ship.
    void BuildShipArrow(Transform root)
    {
        var arrow = MakeIcon("ShipArrow", root, PixelFont.Icon("arrow", PixelFont.Arrow), 6, GoodColor);
        shipArrow = arrow.rectTransform;
        Place(shipArrow, new Vector2(0.5f, 0.5f), Vector2.zero);

        shipDistance = MakeLabel("ShipDistance", root, 4, Color.white);
        Place(shipDistance.image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
    }

    // End of a level: dark veil, big word, air left (after docking) and the key to press.
    void BuildEndScreen(Transform root)
    {
        var veil = MakeImage("EndScreen", root, new Color(0f, 0f, 0.03f, 0.55f));
        Stretch(veil.rectTransform);
        endRoot = veil.gameObject;
        Transform t = veil.transform;
        Vector2 mid = new Vector2(0.5f, 0.5f);

        endCounter = MakeLabel("Counter", t, 4, new Color(1f, 1f, 1f, 0.7f));
        Place(endCounter.image.rectTransform, mid, new Vector2(0f, 150f));

        endTitle = MakeLabel("Title", t, 12, Color.white);
        Place(endTitle.image.rectTransform, mid, new Vector2(0f, 60f));

        var row = new GameObject("Air", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(t, false);
        Place(row, mid, new Vector2(0f, -50f));
        endAirRow = row.gameObject;
        endAirIcon = MakeIcon("Icon", row, PixelFont.Icon("tank", PixelFont.Tank), 5, AirColor).rectTransform;
        endAir = MakeLabel("Number", row, 6, Color.white);

        // The key to press, drawn as a keycap.
        var key = MakeImage("Key", t, new Color(1f, 1f, 1f, 0.92f));
        endKey = key.rectTransform;
        Place(endKey, mid, new Vector2(0f, -150f));
        endKeyLabel = MakeLabel("Text", endKey, 4, Ink, false);
        Place(endKeyLabel.image.rectTransform, mid, Vector2.zero);
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

        float lit = airFraction * AirCells;
        for (int i = 0; i < AirCells; i++)
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
            leakDrops[i].gameObject.SetActive(i < count);
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
        float viewH = Tuning.CameraSize, viewW = viewH * size.x / size.y;
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
        shipDistance.image.rectTransform.anchoredPosition = pos - dir * 58f;

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
        if (outOverlay.activeSelf == on && outLabel.image.gameObject.activeSelf == on) return;
        outOverlay.SetActive(on);
        outLabel.Show(on);
    }

    // --- End of a level ---

    // counter: small line over the word ("2/3"), may be null. air: air left, -1 hides it.
    // key: the key that continues ("ENTER", "R").
    public void ShowMessage(string text, Color color, string counter, int air, string key)
    {
        endRoot.SetActive(true);

        endCounter.Show(!string.IsNullOrEmpty(counter));
        if (!string.IsNullOrEmpty(counter)) endCounter.Set(counter);

        endTitle.Set(text);
        endTitle.image.color = color;

        endAirRow.SetActive(air >= 0);
        if (air >= 0)
        {
            // Icon and number side by side, centred as one piece.
            endAir.Set(air.ToString());
            float iconW = endAirIcon.sizeDelta.x, gap = 16f;
            float total = iconW + gap + endAir.Width;
            Place(endAirIcon, new Vector2(0.5f, 0.5f), new Vector2(-total * 0.5f + iconW * 0.5f, 0f));
            Place(endAir.image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(total * 0.5f - endAir.Width * 0.5f, 0f));
        }

        endKeyLabel.Set(key);
        endKey.sizeDelta = new Vector2(endKeyLabel.Width + 36f, 52f);
    }

    public void HideMessage()
    {
        endRoot.SetActive(false);
    }

    // --- Building blocks ---

    static Label MakeLabel(string name, Transform parent, int scale, Color color, bool shadow = true)
    {
        var label = new Label { image = MakeIcon(name, parent, null, scale, color, shadow), scale = scale };
        label.Set(" ");
        return label;
    }

    // A pixel picture shown scale times bigger, with a hard dark shadow so it reads on any background.
    static RawImage MakeIcon(string name, Transform parent, Texture2D tex, int scale, Color color, bool shadow = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<RawImage>();
        img.color = color;
        img.raycastTarget = false;
        if (tex != null)
        {
            img.texture = tex;
            img.rectTransform.sizeDelta = new Vector2(tex.width * scale, tex.height * scale);
        }
        if (shadow)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0.05f, 0.7f);
            s.effectDistance = new Vector2(scale * 0.5f, -scale * 0.5f);
        }
        return img;
    }

    static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Pins the rect to one point of the parent; the pivot matches the anchor. Size is left alone.
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
    }

    // Fills the whole parent.
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
