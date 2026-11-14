using UnityEngine;
using UnityEngine.UI;

// One line of text in the game's pixel font (PixelFont). Put it on a UI object with a
// RawImage: type the text in the Inspector, pick the scale (how many screen pixels one
// font pixel takes) and the colour on the RawImage. The size of the object follows the text.
// Only capital letters, digits and - . : / + ! exist; small letters are shown as capitals.
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class PixelLabel : MonoBehaviour
{
    [SerializeField] string text = "";
    [Min(1)] [SerializeField] int scale = 4;

    RawImage image;
    string shown;
    int shownScale;

    public RawImage Image
    {
        get
        {
            if (image == null) image = GetComponent<RawImage>();
            return image;
        }
    }

    public float Width
    {
        get { return ((RectTransform)transform).sizeDelta.x; }
    }

    public void Setup(string text, int scale)
    {
        this.text = text;
        this.scale = scale;
        Refresh();
    }

    public void Set(string value)
    {
        if (value == null) value = "";
        if (value == text && shown == text) return;
        text = value;
        Refresh();
    }

    public void Show(bool on)
    {
        if (gameObject.activeSelf != on) gameObject.SetActive(on);
    }

    public void Alpha(float a)
    {
        var c = Image.color;
        c.a = a;
        Image.color = c;
    }

    void OnEnable()
    {
        Refresh();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Size cannot be changed inside OnValidate, so do it right after.
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Refresh(); };
    }
#endif

    void Refresh()
    {
        if (shown == text && shownScale == scale && Image.texture != null) return;
        shown = text;
        shownScale = scale;
        var tex = PixelFont.Text(text);
        Image.texture = tex;
        ((RectTransform)transform).sizeDelta = new Vector2(tex.width * scale, tex.height * scale);
    }
}
