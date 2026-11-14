using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Danger markers at the screen edge for threats the player cannot see yet:
//  - a meteor: shown where it WILL enter the screen (along its flight), only if it crosses the screen;
//  - a hunter mine that is awake and chasing the player.
// A marker is a small pixel icon of the threat at the edge with a soft glow, and a red arrow
// pointing INTO the screen ("coming at you"). The green ship arrow points out ("go there"),
// so the two never mix up. The closer the threat, the bigger, brighter and faster it pulses.
// Lives on the HUD; the pool of markers is made by Filip > Rebuild Prefabs And Scene.
public class ThreatMarkers : MonoBehaviour
{
    static readonly Color MeteorColor = new Color(1f, 0.55f, 0.2f);
    static readonly Color MineColor = new Color(1f, 0.3f, 0.25f);

    const float EdgeMargin = 64f;     // distance from the screen edge, canvas units
    const float TopMargin = 130f;     // the top edge is lower: the air bar and combo live there
    const float ArrowGap = 38f;       // the arrow sits this far inward from the icon
    const float MeteorWarnTime = 2f;  // a meteor this many seconds from the screen is shown at full alarm
    const float MineNear = 10f;       // a hunter this close (world units) is shown at full alarm
    const float MineFar = 26f;        // ... and this far at the lowest

    [Tooltip("Marker roots, children of the HUD; hidden when not used")]
    [SerializeField] RectTransform[] markers;
    [SerializeField] Image[] glows;
    [SerializeField] Image[] icons;
    [SerializeField] Image[] arrows;
    [SerializeField] Sprite meteorIcon;
    [SerializeField] Sprite mineIcon;

    RectTransform canvasRect;

    struct Threat
    {
        public Vector2 edge;      // where the marker sits, canvas units from the centre
        public Vector2 inward;    // direction the arrow points (into the screen)
        public float alarm;       // 0 = far, 1 = about to hit
        public bool meteor;
    }

    readonly List<Threat> found = new List<Threat>();

    void Awake()
    {
        canvasRect = (RectTransform)transform;
        HideAll();
    }

    void HideAll()
    {
        if (markers == null) return;
        foreach (var m in markers) if (m != null && m.gameObject.activeSelf) m.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        var game = DraftGame.Instance;
        var cam = Camera.main;
        if (markers == null || markers.Length == 0 || game == null || cam == null
            || game.State != GameState.Playing || game.PlayerTransform == null)
        {
            HideAll();
            return;
        }

        Vector2 size = canvasRect.rect.size;
        if (size.x < 1f || size.y < 1f) size = new Vector2(1920f, 1080f);
        float halfW = size.x * 0.5f - EdgeMargin;
        float bottom = -(size.y * 0.5f - EdgeMargin), top = size.y * 0.5f - TopMargin;

        found.Clear();
        foreach (var m in Meteor.All) AddMeteor(m, cam, size, halfW, bottom, top);
        foreach (var m in Mine.All) AddMine(m, cam, size, halfW, bottom, top, game.PlayerTransform.position);
        found.Sort((a, b) => b.alarm.CompareTo(a.alarm)); // the most urgent get a marker first

        for (int i = 0; i < markers.Length; i++)
        {
            bool show = i < found.Count;
            if (markers[i].gameObject.activeSelf != show) markers[i].gameObject.SetActive(show);
            if (show) Paint(i, found[i]);
        }
    }

    // Canvas position (from the centre) of a world point.
    static Vector2 ToCanvas(Camera cam, Vector2 world, Vector2 size)
    {
        Vector3 v = cam.WorldToViewportPoint(world);
        return new Vector2((v.x - 0.5f) * size.x, (v.y - 0.5f) * size.y);
    }

    void AddMeteor(Meteor m, Camera cam, Vector2 size, float halfW, float bottom, float top)
    {
        if (m == null) return;
        Vector2 p = ToCanvas(cam, m.transform.position, size);
        Vector2 v = ToCanvas(cam, (Vector2)m.transform.position + m.Velocity, size) - p; // canvas units per second
        if (v.sqrMagnitude < 1f) return;

        // Already on screen (with its size): no marker.
        float r = m.Radius / (cam.orthographicSize * 2f) * size.y;
        if (Mathf.Abs(p.x) < size.x * 0.5f + r && Mathf.Abs(p.y) < size.y * 0.5f + r) return;

        // Where its path enters the screen; no entry = it misses the screen, not a threat.
        float tIn = 0f, tOut = float.MaxValue;
        if (!Slab(p.x, v.x, -size.x * 0.5f, size.x * 0.5f, ref tIn, ref tOut)) return;
        if (!Slab(p.y, v.y, -size.y * 0.5f, size.y * 0.5f, ref tIn, ref tOut)) return;
        if (tIn > tOut || tOut <= 0f) return;

        Vector2 entry = p + v * tIn;
        var t = new Threat();
        t.edge = new Vector2(Mathf.Clamp(entry.x, -halfW, halfW), Mathf.Clamp(entry.y, bottom, top));
        t.inward = v.normalized;
        t.alarm = 1f - Mathf.Clamp01(tIn / MeteorWarnTime);
        t.meteor = true;
        found.Add(t);
    }

    // Ray-box clipping along one axis: narrows [tIn, tOut] to when p + v*t is between lo and hi.
    static bool Slab(float p, float v, float lo, float hi, ref float tIn, ref float tOut)
    {
        if (Mathf.Abs(v) < 1e-5f) return p >= lo && p <= hi;
        float a = (lo - p) / v, b = (hi - p) / v;
        if (a > b) { float s = a; a = b; b = s; }
        tIn = Mathf.Max(tIn, a);
        tOut = Mathf.Min(tOut, b);
        return true;
    }

    void AddMine(Mine m, Camera cam, Vector2 size, float halfW, float bottom, float top, Vector2 player)
    {
        if (m == null || !m.Hunting) return;
        Vector2 p = ToCanvas(cam, m.transform.position, size);
        if (Mathf.Abs(p.x) < size.x * 0.5f && Mathf.Abs(p.y) < size.y * 0.5f) return; // seen

        // On the edge in the mine's direction, pointing in (it comes at the player).
        Vector2 dir = p.normalized;
        float reach = Mathf.Min(
            Mathf.Abs(dir.x) > 0.001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
            dir.y > 0.001f ? top / dir.y : (dir.y < -0.001f ? bottom / dir.y : float.MaxValue));
        var t = new Threat();
        t.edge = dir * reach;
        t.inward = -dir;
        float dist = Vector2.Distance(m.transform.position, player);
        t.alarm = 1f - Mathf.Clamp01((dist - MineNear) / (MineFar - MineNear));
        t.meteor = false;
        found.Add(t);
    }

    void Paint(int i, Threat t)
    {
        Color c = t.meteor ? MeteorColor : MineColor;
        float rate = 2.5f + 6f * t.alarm;                                  // pulses per second
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * rate * Mathf.PI * 2f);
        float scale = 0.85f + 0.4f * t.alarm;

        markers[i].anchoredPosition = t.edge;
        markers[i].localScale = new Vector3(scale, scale, 1f);

        icons[i].sprite = t.meteor ? meteorIcon : mineIcon;
        icons[i].color = new Color(c.r, c.g, c.b, 0.75f + 0.25f * pulse);

        var a = (RectTransform)arrows[i].transform;
        a.anchoredPosition = t.inward * ArrowGap;
        a.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(t.inward.y, t.inward.x) * Mathf.Rad2Deg - 90f);
        arrows[i].color = new Color(c.r, c.g, c.b, 0.55f + 0.45f * pulse);

        glows[i].color = new Color(c.r, c.g, c.b, (0.18f + 0.3f * t.alarm) * (0.6f + 0.4f * pulse));
    }
}
