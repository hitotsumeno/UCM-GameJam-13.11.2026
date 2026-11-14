using UnityEngine;

// One layer of the space background. It follows the camera and scrolls on screen
// at a fraction of the world speed (parallax): the player flies THROUGH it.
//  - Repeating: cloud layers (nebula pictures, 3x3 tiles) repeat endlessly in all directions.
//  - Free: a backdrop, one big far object (sun, wreck, planet) that drifts slowly across the screen.
//  - Clamped: artist picture "background", if present, replaces the clouds. It does not
//    repeat, so its shift is limited and the picture edge never gets into view.
// Part of Assets/Prefabs/Background_LevelN.prefab.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class ParallaxBackground : MonoBehaviour
{
    public enum Mode { Clamped, Repeating, Free }

    [Tooltip("World point where the picture is centered on the camera")]
    [SerializeField] Vector2 anchor;
    [Tooltip("Free mode: where the object sits on screen when the camera is at the anchor")]
    [SerializeField] Vector2 baseOffset;
    [Tooltip("How fast the layer scrolls on screen compared to the world (1 = like the world)")]
    [SerializeField] float factor = 0.25f;
    [Tooltip("Picture side in units")]
    [SerializeField] float size = 64f;
    [SerializeField] Mode mode = Mode.Repeating;

    Camera cam;

    public void Setup(Vector2 anchor, float factor, float size, Mode mode, Vector2 baseOffset)
    {
        this.anchor = anchor;
        this.factor = factor;
        this.size = size;
        this.mode = mode;
        this.baseOffset = baseOffset;
        transform.position = anchor + baseOffset;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 camPos = cam.transform.position;
        Vector2 offset = baseOffset + (anchor - camPos) * factor;

        if (mode == Mode.Repeating)
        {
            float half = size * 0.5f;
            offset.x = Mathf.Repeat(offset.x + half, size) - half;
            offset.y = Mathf.Repeat(offset.y + half, size) - half;
        }
        else if (mode == Mode.Clamped)
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
