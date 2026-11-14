using UnityEngine;

// One layer of the space background. It follows the camera and scrolls on screen
// at a fraction of the world speed (parallax): the player flies THROUGH it.
//  - Nebula layers (nebula_far, nebula_near: 3x3 tiles) repeat endlessly in all directions.
//  - Artist picture "background", if present, replaces the nebula. It does not repeat,
//    so its shift is limited and the picture edge never gets into view.
// Part of Assets/Prefabs/Background.prefab.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class ParallaxBackground : MonoBehaviour
{
    [Tooltip("World point where the picture is centered on the camera")]
    [SerializeField] Vector2 anchor;
    [Tooltip("How fast the layer scrolls on screen compared to the world (1 = like the world)")]
    [SerializeField] float factor = 0.25f;
    [Tooltip("Picture side in units")]
    [SerializeField] float size = 64f;
    [SerializeField] bool repeats = true;

    Camera cam;

    public void Setup(Vector2 anchor, float factor, float size, bool repeats)
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
