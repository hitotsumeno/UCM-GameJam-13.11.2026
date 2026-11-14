using UnityEngine;
using UnityEngine.UI;

// On-screen interface, built from code: oxygen bar, ship compass with speed,
// combo counter, hints, short notices and the big end-of-level message.
public class Hud : MonoBehaviour
{
    static readonly Color OxygenColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color GoodColor = new Color(0.4f, 1f, 0.5f);
    static readonly Color BadColor = new Color(1f, 0.35f, 0.3f);

    static readonly Color LeakingColor = new Color(1f, 0.62f, 0.3f);

    RectTransform oxygenFill;
    Image oxygenFillImage;
    Color oxygenBase = OxygenColor;   // cyan normally, orange while the hull leaks
    float oxygenFlash;
    Text breachText;
    Image damageFlash;
    float damageTimer;

    Text message;
    Text messageSub;
    GameObject signalOverlay;
    GameObject signalText;

    RectTransform compassNeedle;
    Text compassText;
    int shownDistance = -1;
    Text speedText;
    int shownSpeed = -1;
    bool shownSpeedOk;

    GameObject comboRoot;
    Text comboText;
    RectTransform comboBar;
    int shownCombo = -1;

    Text hintText;
    string shownHint;
    Text toastText;
    float toastTimer;
    Text titleText;
    float titleTimer;

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
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Red tint over the whole screen while outside the level (created first = drawn behind the rest).
        var overlay = MakeImage("SignalOverlay", go.transform, new Color(1f, 0f, 0f, 0.22f));
        Stretch(overlay.rectTransform);
        hud.signalOverlay = overlay.gameObject;

        // Red flash over the whole screen when the hull is breached.
        hud.damageFlash = MakeImage("DamageFlash", go.transform, new Color(1f, 0.1f, 0.05f, 0f));
        Stretch(hud.damageFlash.rectTransform);

        // Number of leaks, next to the OXYGEN label. Hidden while the hull is whole.
        hud.breachText = MakeText("Breaches", go.transform, font, 24, TextAnchor.UpperLeft, BadColor);
        Place(hud.breachText.rectTransform, new Vector2(0f, 1f), new Vector2(110f, -28f), new Vector2(300f, 34f));
        hud.breachText.gameObject.SetActive(false);

        var signal = MakeText("SignalText", go.transform, font, 44, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f));
        signal.text = Texts.OutOfBounds;
        Place(signal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 100f));
        hud.signalText = signal.gameObject;

        // OXYGEN label, top-left.
        var label = MakeText("OxygenLabel", go.transform, font, 24, TextAnchor.UpperLeft, Color.white);
        label.text = Texts.Air;
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(400f, 34f));

        // Oxygen bar under the label: thin, so it does not shout.
        var bg = MakeImage("OxygenBar", go.transform, new Color(1f, 1f, 1f, 0.2f));
        Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -62f), new Vector2(320f, 12f));
        var fill = MakeImage("Fill", bg.transform, OxygenColor);
        hud.oxygenFillImage = fill;
        hud.oxygenFill = fill.rectTransform;
        Stretch(hud.oxygenFill);

        hud.BuildCompass(go.transform, font);
        hud.BuildCombo(go.transform, font);

        // Level name, shown for a few seconds at the start of a level.
        hud.titleText = MakeText("Title", go.transform, font, 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
        Place(hud.titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1600f, 60f));

        // Short notice in the upper part of the screen (TOO FAST).
        hud.toastText = MakeText("Toast", go.transform, font, 36, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.2f));
        Place(hud.toastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1600f, 80f));

        // Hint at the bottom of the screen.
        hud.hintText = MakeText("Hint", go.transform, font, 28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
        Place(hud.hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1800f, 50f));

        // Big message in the center (DOCKED / OUT OF OXYGEN) with a line under it.
        hud.message = MakeText("Message", go.transform, font, 72, TextAnchor.MiddleCenter, Color.white);
        Place(hud.message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 160f));
        hud.messageSub = MakeText("MessageSub", go.transform, font, 26, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
        Place(hud.messageSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1800f, 60f));

        hud.HideMessage();
        hud.SetSignalLost(false);
        hud.SetHint(null);
        hud.toastText.gameObject.SetActive(false);
        hud.titleText.gameObject.SetActive(false);
        return hud;
    }

    // Round compass on the left side, under the oxygen bar: the arrow on the rim points
    // to the ship, the distance is in the middle, the player's speed is under it.
    void BuildCompass(Transform parent, Font font)
    {
        const float size = 130f;
        var root = new GameObject("Compass", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, new Vector2(0f, 1f), new Vector2(40f, -92f), new Vector2(size, size));

        var disc = MakeImage("Disc", root, new Color(0f, 0f, 0f, 0.3f));
        disc.sprite = SpriteFactory.Circle();
        Stretch(disc.rectTransform);

        var ring = MakeImage("Ring", root, new Color(1f, 1f, 1f, 0.4f));
        ring.sprite = SpriteFactory.Ring();
        Stretch(ring.rectTransform);

        compassNeedle = new GameObject("Needle", typeof(RectTransform)).GetComponent<RectTransform>();
        compassNeedle.SetParent(root, false);
        Place(compassNeedle, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var arrow = MakeImage("Arrow", compassNeedle, GoodColor);
        arrow.sprite = SpriteFactory.Triangle();
        Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.5f - 20f), new Vector2(26f, 26f));

        compassText = MakeText("Distance", root, font, 24, TextAnchor.MiddleCenter, Color.white);
        Place(compassText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, 60f));

        // Green while slow enough to dock, red when too fast.
        speedText = MakeText("Speed", parent, font, 22, TextAnchor.UpperLeft, GoodColor);
        Place(speedText.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -230f), new Vector2(300f, 34f));
    }

    // Combo counter in the top-right corner, with a bar showing the time left to continue it.
    void BuildCombo(Transform parent, Font font)
    {
        var root = new GameObject("Combo", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(420f, 100f));
        comboRoot = root.gameObject;

        comboText = MakeText("Text", root, font, 48, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f));
        Stretch(comboText.rectTransform);

        var barBg = MakeImage("Bar", root, new Color(1f, 1f, 1f, 0.2f));
        Place(barBg.rectTransform, new Vector2(1f, 0f), Vector2.zero, new Vector2(260f, 8f));
        var barFill = MakeImage("Fill", barBg.transform, new Color(1f, 0.85f, 0.3f));
        comboBar = barFill.rectTransform;
        Stretch(comboBar);

        comboRoot.SetActive(false);
    }

    void Update()
    {
        if (oxygenFlash > 0f)
        {
            oxygenFlash = Mathf.Max(0f, oxygenFlash - Time.deltaTime * 4f);
            oxygenFillImage.color = Color.Lerp(oxygenBase, Color.white, oxygenFlash);
        }
        if (damageTimer > 0f)
        {
            damageTimer = Mathf.Max(0f, damageTimer - Time.deltaTime * 2.5f);
            damageFlash.color = new Color(1f, 0.1f, 0.05f, 0.45f * damageTimer);
        }
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
            if (toastTimer <= 0f) toastText.gameObject.SetActive(false);
        }
        if (titleTimer > 0f)
        {
            titleTimer -= Time.deltaTime;
            if (titleTimer <= 0f) titleText.gameObject.SetActive(false);
        }
    }

    public void SetOxygen(float fraction)
    {
        oxygenFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    // The oxygen bar flashes white when a crystal is collected.
    public void FlashOxygen()
    {
        oxygenFlash = 1f;
    }

    // count: holes in the hull. The oxygen bar turns orange while there is at least one.
    public void SetBreaches(int count)
    {
        breachText.gameObject.SetActive(count > 0);
        if (count > 0) breachText.text = Texts.Leaks + count;
        oxygenBase = count > 0 ? LeakingColor : OxygenColor;
        oxygenFillImage.color = oxygenBase;
    }

    // Red flash of the whole screen: the hull was just breached.
    public void FlashDamage()
    {
        damageTimer = 1f;
        damageFlash.color = new Color(1f, 0.1f, 0.05f, 0.45f);
    }

    // toShip: vector from the player to the ship, in world units.
    public void SetCompass(Vector2 toShip)
    {
        float angle = Mathf.Atan2(toShip.y, toShip.x) * Mathf.Rad2Deg - 90f;
        compassNeedle.localRotation = Quaternion.Euler(0f, 0f, angle);

        int distance = Mathf.RoundToInt(toShip.magnitude);
        if (distance != shownDistance)
        {
            shownDistance = distance;
            compassText.text = distance + Texts.Metres;
        }
    }

    // slowEnough: the player may dock at this speed.
    public void SetSpeed(float speed, bool slowEnough)
    {
        int rounded = Mathf.RoundToInt(speed);
        if (rounded == shownSpeed && slowEnough == shownSpeedOk) return;
        shownSpeed = rounded;
        shownSpeedOk = slowEnough;
        speedText.text = Texts.Speed + rounded;
        speedText.color = slowEnough ? GoodColor : BadColor;
    }

    // combo: kills in a row (shown from x2). timeLeft: 1 = just continued, 0 = about to end.
    public void SetCombo(int combo, float timeLeft)
    {
        bool show = combo >= 2;
        if (comboRoot.activeSelf != show) comboRoot.SetActive(show);
        if (!show) { shownCombo = -1; return; }

        if (combo != shownCombo)
        {
            shownCombo = combo;
            comboText.text = Texts.Combo + combo;
        }
        comboBar.anchorMin = new Vector2(1f - Mathf.Clamp01(timeLeft), 0f); // shrinks toward the right edge
    }

    // text == null hides the hint.
    public void SetHint(string text)
    {
        if (text == shownHint && hintText.gameObject.activeSelf == (text != null)) return;
        shownHint = text;
        hintText.gameObject.SetActive(text != null);
        if (text != null) hintText.text = text;
    }

    // Short notice that disappears by itself.
    public void Toast(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        toastText.text = text;
        toastText.gameObject.SetActive(true);
        toastTimer = 1.2f;
    }

    // Level name at the start of a level.
    public void ShowTitle(string text)
    {
        bool show = !string.IsNullOrEmpty(text);
        titleText.gameObject.SetActive(show);
        if (!show) { titleTimer = 0f; return; }
        titleText.text = text;
        titleTimer = 4f;
    }

    public void SetSignalLost(bool on)
    {
        if (signalOverlay.activeSelf == on) return;
        signalOverlay.SetActive(on);
        signalText.SetActive(on);
    }

    public void ShowMessage(string text, Color color, string sub)
    {
        message.text = text;
        message.color = color;
        messageSub.text = sub;
        message.gameObject.SetActive(true);
        messageSub.gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        message.gameObject.SetActive(false);
        messageSub.gameObject.SetActive(false);
    }

    static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
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

    // Anchors the rect to one point of the parent; pivot matches the anchor.
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
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
