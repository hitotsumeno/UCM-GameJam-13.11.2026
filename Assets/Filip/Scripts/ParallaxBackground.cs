using UnityEngine;

// The big background picture. It follows the camera and scrolls on screen at a fraction
// of the world speed. The shift is limited so the picture edge never gets into view.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class ParallaxBackground : MonoBehaviour
{
    Vector2 anchor;   // world point where the picture is centered on the camera
    float factor;
    float size;       // picture side in units
    Camera cam;

    // Returns null if there is no background picture (then the background is just black with stars).
    public static ParallaxBackground Create(Transform parent, Vector2 anchor)
    {
        var sr = Art.TryMake("background", parent, Tuning.BackgroundSize, -200);
        if (sr == null) return null;

        var bg = sr.gameObject.AddComponent<ParallaxBackground>();
        bg.anchor = anchor;
        bg.factor = Tuning.BackgroundParallax;
        bg.size = Tuning.BackgroundSize;
        bg.transform.position = anchor;
        return bg;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 camPos = cam.transform.position;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float maxX = Mathf.Max(0f, size * 0.5f - halfW);
        float maxY = Mathf.Max(0f, size * 0.5f - halfH);

        Vector2 offset = (anchor - camPos) * factor;
        offset.x = Mathf.Clamp(offset.x, -maxX, maxX);
        offset.y = Mathf.Clamp(offset.y, -maxY, maxY);
        transform.position = camPos + offset;
    }
}
