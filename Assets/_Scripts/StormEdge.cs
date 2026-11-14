using UnityEngine;

// One side of the storm wall at the level edge. Two layers of the storm picture (the dark,
// dense body and its bright folds) slowly flow along the edge in opposite directions, so the
// clouds seem to boil. The side's own +X points out of the level; the edge is at local x = 0.
// Made by Filip > Rebuild Prefabs And Scene (pictures: storm_body.png, storm_glow.png).
public class StormEdge : MonoBehaviour
{
    [SerializeField] SpriteRenderer body;
    [SerializeField] SpriteRenderer glow;
    [Tooltip("Length of one storm picture along the edge, units (at scale 1)")]
    [SerializeField] float tile = 32f;
    [Tooltip("Local x of a picture's middle at scale 1 (the picture starts 4 units inside the level)")]
    [SerializeField] float centre = 8f;
    [SerializeField] float bodySpeed = 0.7f;
    [SerializeField] float glowSpeed = -1.2f;

    float phase;
    Color glowColor;

    void Awake()
    {
        phase = Random.Range(0f, tile);
        if (glow != null) glowColor = glow.color;
    }

    void Update()
    {
        float t = Time.time;
        Flow(body, t * bodySpeed + phase);
        Flow(glow, t * glowSpeed + phase * 0.5f);
        if (glow != null)
        {
            // The folds breathe a little.
            float k = 0.85f + 0.15f * Mathf.Sin(t * 0.9f + phase);
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * k);
        }
    }

    // Slides a tiled picture along the edge; it is one tile longer at each end, so no gap shows.
    void Flow(SpriteRenderer sr, float distance)
    {
        if (sr == null) return;
        float scale = sr.transform.localScale.x;
        float length = tile * scale;
        float y = Mathf.Repeat(distance, length) - length * 0.5f;
        sr.transform.localPosition = new Vector3(centre * scale, y, 0f);
    }
}
