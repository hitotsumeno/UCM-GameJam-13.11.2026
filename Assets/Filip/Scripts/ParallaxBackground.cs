using UnityEngine;

// One layer of the space background. It follows the camera and scrolls on screen
// at a fraction of the world speed (parallax): the player flies THROUGH it.
//  - Nebula layers (pictures nebula_far, nebula_near) repeat endlessly in all directions.
//    The near layer scrolls faster than the far one, so the picture keeps changing.
//  - Artist picture "background", if present, replaces the nebula. It does not repeat,
//    so its shift is limited and the picture edge never gets into view.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class ParallaxBackground : MonoBehaviour
{
    Vector2 anchor;   // world point where the picture is centered on the camera
    float factor;
    float size;       // picture side in units
    bool repeats;
    Camera cam;

    // Builds all background layers under parent.
    public static void CreateAll(Transform parent, Vector2 anchor)
    {
        var art = Art.TryMake("background", parent, Tuning.BackgroundSize, -200);
        if (art != null)
        {
            var bg = art.gameObject.AddComponent<ParallaxBackground>();
            bg.Setup(anchor, Tuning.BackgroundParallax, Tuning.BackgroundSize, false);
            return;
        }
        // The far clouds are darkened and the near ones made more transparent,
        // so the background stays behind the rocks instead of competing with them.
        float b = Tuning.NebulaFarBrightness;
        CreateRepeating("nebula_far", parent, anchor, Tuning.NebulaFarSize, Tuning.NebulaFarParallax, -200, 1.002f,
            new Color(b, b, b, 1f));
        CreateRepeating("nebula_near", parent, anchor, Tuning.NebulaNearSize, Tuning.NebulaNearParallax, -145, 1f,
            new Color(1f, 1f, 1f, Tuning.NebulaNearOpacity));
    }

    // 3x3 copies of a seamless picture. If the picture is missing, nothing is created (black sky).
    static void CreateRepeating(string artName, Transform parent, Vector2 anchor, float size, float factor,
        int sortingOrder, float tileScale, Color color)
    {
        var sprite = Art.Load(artName);
        if (sprite == null) return;

        var go = new GameObject(artName);
        go.transform.SetParent(parent, false);
        Vector2 b = sprite.bounds.size;
        float scale = size / Mathf.Max(b.x, b.y) * tileScale;
        for (int ix = -1; ix <= 1; ix++)
        for (int iy = -1; iy <= 1; iy++)
        {
            var tile = new GameObject("Tile");
            tile.transform.SetParent(go.transform, false);
            tile.transform.localPosition = new Vector3(ix * size, iy * size, 0f);
            tile.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }
        go.AddComponent<ParallaxBackground>().Setup(anchor, factor, size, true);
    }

    void Setup(Vector2 anchor, float factor, float size, bool repeats)
    {
        this.anchor = anchor;
        this.factor = factor;
        this.size = size;
        this.repeats = repeats;
        transform.position = anchor;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 camPos = cam.transform.position;
        Vector2 offset = (anchor - camPos) * factor;

        if (repeats)
        {
            float half = size * 0.5f;
            offset.x = Mathf.Repeat(offset.x + half, size) - half;
            offset.y = Mathf.Repeat(offset.y + half, size) - half;
        }
        else
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float maxX = Mathf.Max(0f, size * 0.5f - halfW);
            float maxY = Mathf.Max(0f, size * 0.5f - halfH);
            offset.x = Mathf.Clamp(offset.x, -maxX, maxX);
            offset.y = Mathf.Clamp(offset.y, -maxY, maxY);
        }
        transform.position = camPos + offset;
    }
}
