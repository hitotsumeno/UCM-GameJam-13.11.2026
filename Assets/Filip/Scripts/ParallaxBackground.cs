using UnityEngine;

// The space background. It follows the camera and scrolls on screen at a fraction
// of the world speed (parallax), like the stars but slower.
//  - Artist picture "background": does not repeat, so the shift is limited
//    and the picture edge never gets into view.
//  - No picture: a nebula generated in code, repeated endlessly in all directions.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class ParallaxBackground : MonoBehaviour
{
    Vector2 anchor;   // world point where the picture is centered on the camera
    float factor;
    float size;       // picture side in units
    bool repeats;
    Camera cam;

    public static ParallaxBackground Create(Transform parent, Vector2 anchor)
    {
        ParallaxBackground bg;
        var art = Art.TryMake("background", parent, Tuning.BackgroundSize, -200);
        if (art != null)
        {
            bg = art.gameObject.AddComponent<ParallaxBackground>();
            bg.factor = Tuning.BackgroundParallax;
            bg.repeats = false;
        }
        else
        {
            var go = new GameObject("Nebula");
            go.transform.SetParent(parent, false);
            var sprite = Nebula.GetSprite();
            float t = Tuning.BackgroundSize;
            // 3x3 copies; slightly enlarged so no gaps show between them.
            for (int ix = -1; ix <= 1; ix++)
            for (int iy = -1; iy <= 1; iy++)
            {
                var tile = SpriteFactory.MakeSprite("Tile", go.transform, sprite, Color.white,
                    Vector2.one * 1.002f, -200);
                tile.transform.localPosition = new Vector3(ix * t, iy * t, 0f);
            }
            bg = go.AddComponent<ParallaxBackground>();
            bg.factor = Tuning.NebulaParallax;
            bg.repeats = true;
        }

        bg.anchor = anchor;
        bg.size = Tuning.BackgroundSize;
        bg.transform.position = anchor;
        return bg;
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
