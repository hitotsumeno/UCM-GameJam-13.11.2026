using UnityEngine;
using UnityEngine.UI;

// On-screen interface, built from code: oxygen bar, ship compass,
// SIGNAL LOST warning and the big end-of-level message.
public class Hud : MonoBehaviour
{
    RectTransform oxygenFill;
    Text message;
    Text hint;
    GameObject signalOverlay;
    GameObject signalText;
    RectTransform compassNeedle;
    Text compassText;
    int shownDistance = -1;

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
        overlay.rectTransform.anchorMin = Vector2.zero;
        overlay.rectTransform.anchorMax = Vector2.one;
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;
        hud.signalOverlay = overlay.gameObject;

        var signal = MakeText("SignalText", go.transform, font, 72, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f));
        signal.text = "SIGNAL LOST";
        Place(signal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 100f));
        hud.signalText = signal.gameObject;

        // OXYGEN label, top-left.
        var label = MakeText("OxygenLabel", go.transform, font, 36, TextAnchor.UpperLeft, Color.white);
        label.text = "OXYGEN";
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(400f, 44f));

        // Oxygen bar under the label.
        var bg = MakeImage("OxygenBar", go.transform, new Color(1f, 1f, 1f, 0.2f));
        Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -80f), new Vector2(400f, 28f));
        var fill = MakeImage("Fill", bg.transform, new Color(0.3f, 0.85f, 1f));
        hud.oxygenFill = fill.rectTransform;
        Stretch(hud.oxygenFill);

        hud.BuildCompass(go.transform, font);

        // Big message in the center (DOCKED / OUT OF OXYGEN).
        hud.message = MakeText("Message", go.transform, font, 110, TextAnchor.MiddleCenter, Color.white);
        Place(hud.message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 160f));

        hud.hint = MakeText("Hint", go.transform, font, 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
        Place(hud.hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1800f, 60f));
        hud.hint.text = "PRESS R TO RESTART";

        hud.HideMessage();
        hud.SetSignalLost(false);
        return hud;
    }

    // Round compass on the left side, under the oxygen bar: the arrow on the rim points to the ship.
    void BuildCompass(Transform parent, Font font)
    {
        const float size = 180f;
        var root = new GameObject("Compass", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, new Vector2(0f, 1f), new Vector2(40f, -135f), new Vector2(size, size));

        var disc = MakeImage("Disc", root, new Color(0f, 0f, 0f, 0.45f));
        disc.sprite = SpriteFactory.Circle();
        Stretch(disc.rectTransform);

        var ring = MakeImage("Ring", root, new Color(1f, 1f, 1f, 0.7f));
        ring.sprite = SpriteFactory.Ring();
        Stretch(ring.rectTransform);

        compassNeedle = new GameObject("Needle", typeof(RectTransform)).GetComponent<RectTransform>();
        compassNeedle.SetParent(root, false);
        Place(compassNeedle, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var arrow = MakeImage("Arrow", compassNeedle, new Color(0.4f, 1f, 0.5f));
        arrow.sprite = SpriteFactory.Triangle();
        Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.5f - 26f), new Vector2(34f, 34f));

        compassText = MakeText("Distance", root, font, 28, TextAnchor.MiddleCenter, Color.white);
        Place(compassText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, 80f));
    }

    public void SetOxygen(float fraction)
    {
        oxygenFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
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
            compassText.text = "SHIP\n" + distance + " m";
        }
    }

    public void SetSignalLost(bool on)
    {
        if (signalOverlay.activeSelf == on) return;
        signalOverlay.SetActive(on);
        signalText.SetActive(on);
    }

    public void ShowMessage(string text, Color color)
    {
        message.text = text;
        message.color = color;
        message.gameObject.SetActive(true);
        hint.gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        message.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
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
