using UnityEngine;

// A layer of star dots that scrolls on screen at a fraction of the world speed.
// Stars repeat (wrap around the camera), so the layer never runs out,
// however far the player flies.
[DefaultExecutionOrder(100)] // after CameraRig has moved the camera
public class StarLayer : MonoBehaviour
{
    Vector2 anchor;
    float factor;
    float tile;
    Transform[] stars;
    Vector2[] local;   // star positions inside the tile
    Camera cam;

    public static StarLayer Create(string name, Transform parent, Vector2 anchor, float factor,
        float minSize, float maxSize, float minBrightness, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var layer = go.AddComponent<StarLayer>();
        layer.anchor = anchor;
        layer.factor = factor;
        layer.tile = Tuning.StarTileSize;

        int count = Tuning.StarsPerLayer;
        layer.stars = new Transform[count];
        layer.local = new Vector2[count];
        var circle = SpriteFactory.Circle();
        float half = layer.tile * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float b = Random.Range(minBrightness, 1f);
            float size = Random.Range(minSize, maxSize);
            // Picture "star1", "star2", ... (a random one per star) or a placeholder dot.
            var sr = Art.TryMake("star", go.transform, size * Tuning.StarArtScale, sortingOrder);
            if (sr != null)
                sr.color = new Color(1f, 1f, 1f, b);
            else
                sr = SpriteFactory.MakeSprite("Star", go.transform, circle,
                    new Color(b, b, Mathf.Min(1f, b * Random.Range(0.9f, 1.15f))),
                    Vector2.one * size, sortingOrder);
            layer.stars[i] = sr.transform;
            layer.local[i] = new Vector2(Random.Range(-half, half), Random.Range(-half, half));
        }
        layer.Place(anchor);
        return layer;
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        Place(cam.transform.position);
    }

    void Place(Vector2 camPos)
    {
        Vector2 scroll = (camPos - anchor) * factor;
        float half = tile * 0.5f;
        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 d = local[i] - scroll;
            d.x = Mathf.Repeat(d.x + half, tile) - half;
            d.y = Mathf.Repeat(d.y + half, tile) - half;
            stars[i].position = camPos + d;
        }
    }
}
