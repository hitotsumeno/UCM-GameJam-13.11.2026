using UnityEngine;
using UnityEngine.UI;

// On-screen interface, built from code: oxygen bar and the big end-of-level message.
public class Hud : MonoBehaviour
{
    RectTransform oxygenFill;
    Text message;
    Text hint;

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

        // OXYGEN label, top-left.
        var label = MakeText("OxygenLabel", go.transform, font, 36, TextAnchor.UpperLeft, Color.white);
        label.text = "OXYGEN";
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(400f, 44f));

        // Oxygen bar under the label.
        var bg = MakeImage("OxygenBar", go.transform, new Color(1f, 1f, 1f, 0.2f));
        Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -80f), new Vector2(400f, 28f));
        var fill = MakeImage("Fill", bg.transform, new Color(0.3f, 0.85f, 1f));
        hud.oxygenFill = fill.rectTransform;
        hud.oxygenFill.anchorMin = Vector2.zero;
        hud.oxygenFill.anchorMax = Vector2.one;
        hud.oxygenFill.offsetMin = Vector2.zero;
        hud.oxygenFill.offsetMax = Vector2.zero;

        // Big message in the center (DOCKED / OUT OF OXYGEN).
        hud.message = MakeText("Message", go.transform, font, 110, TextAnchor.MiddleCenter, Color.white);
        Place(hud.message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 160f));
        hud.message.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        hud.hint = MakeText("Hint", go.transform, font, 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
        Place(hud.hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1800f, 60f));
        hud.hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        hud.hint.text = "PRESS R TO RESTART";

        hud.HideMessage();
        return hud;
    }

    public void SetOxygen(float fraction)
    {
        oxygenFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
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
}
