using System.Collections.Generic;
using UnityEngine;

// Everything that belongs to one level. Destroy root to clear the level.
public class LevelWorld
{
    public Transform root;
    public PlayerController player;
    public Transform ship;
    public Rect bounds;          // level area; outside it the signal is lost
}

// Builds the level objects. Used by the game (on Play) and by the editor preview
// (without Play), so this code must also work in edit mode: no Destroy(), no coroutines.
public static class WorldBuilder
{
    public static LevelWorld Build()
    {
        var w = new LevelWorld();
        w.root = new GameObject("World").transform;

        // Temporary level: player at the bottom, ship almost at the very top.
        w.bounds = new Rect(-Tuning.LevelWidth * 0.5f, -Tuning.LevelHeight * 0.5f,
            Tuning.LevelWidth, Tuning.LevelHeight);
        Vector2 start = new Vector2(w.bounds.center.x, w.bounds.yMin + Tuning.PlayerStartFromBottom);
        Vector2 shipPos = new Vector2(w.bounds.center.x, w.bounds.yMax - Tuning.ShipFromTop);

        BuildBackground(w.root, w.bounds.center);
        BuildBorder(w.root, w.bounds);
        w.player = PlayerController.Create(start, w.root);
        w.ship = Ship.Create(shipPos, w.root).transform;
        Scatter(w.root, start, shipPos);
        return w;
    }

    // Temporary: random asteroids on the way to the ship and oxygen tanks off to the sides.
    static void Scatter(Transform parent, Vector2 start, Vector2 shipPos)
    {
        float yMin = start.y + Tuning.ScatterClearance;
        float yMax = shipPos.y - Tuning.ScatterClearance;
        var placed = new List<Vector3>(); // x, y, radius

        for (int i = 0; i < Tuning.ScatterAsteroids; i++)
        {
            float roll = Random.value;
            var size = roll < 0.45f ? AsteroidSize.Small
                     : roll < 0.8f ? AsteroidSize.Medium
                     : AsteroidSize.Large;
            float radius = Asteroid.Diameter(size) * 0.5f;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                var pos = new Vector2(
                    start.x + Random.Range(-Tuning.ScatterHalfWidth, Tuning.ScatterHalfWidth),
                    Random.Range(yMin, yMax));
                if (!IsFree(placed, pos, radius + 0.5f)) continue;

                placed.Add(new Vector3(pos.x, pos.y, radius));
                Vector2 drift = Random.insideUnitCircle.normalized
                    * Random.Range(Tuning.AsteroidDriftMin, Tuning.AsteroidDriftMax);
                Asteroid.Create(size, pos, drift, parent);
                break;
            }
        }

        for (int i = 0; i < Tuning.ScatterTanks; i++)
        {
            float t = (i + 1f) / (Tuning.ScatterTanks + 1f);
            float side = i % 2 == 0 ? 1f : -1f;
            var pos = new Vector2(
                start.x + side * Random.Range(Tuning.TankSideOffsetMin, Tuning.TankSideOffsetMax),
                Mathf.Lerp(yMin, yMax, t));
            OxygenTank.Create(pos, parent);
        }
    }

    static bool IsFree(List<Vector3> placed, Vector2 pos, float radius)
    {
        foreach (var p in placed)
            if (Vector2.Distance(new Vector2(p.x, p.y), pos) < p.z + radius) return false;
        return true;
    }

    // Thin red frame showing where the level ends.
    static void BuildBorder(Transform parent, Rect bounds)
    {
        var border = new GameObject("Border").transform;
        border.SetParent(parent, false);
        var square = SpriteFactory.Square();
        var color = new Color(1f, 0.3f, 0.25f, 0.6f);
        const float t = 0.12f;
        Vector2 c = bounds.center;

        var top = SpriteFactory.MakeSprite("Top", border, square, color, new Vector2(bounds.width + t, t), -50);
        top.transform.position = new Vector2(c.x, bounds.yMax);
        var bottom = SpriteFactory.MakeSprite("Bottom", border, square, color, new Vector2(bounds.width + t, t), -50);
        bottom.transform.position = new Vector2(c.x, bounds.yMin);
        var left = SpriteFactory.MakeSprite("Left", border, square, color, new Vector2(t, bounds.height + t), -50);
        left.transform.position = new Vector2(bounds.xMin, c.y);
        var right = SpriteFactory.MakeSprite("Right", border, square, color, new Vector2(t, bounds.height + t), -50);
        right.transform.position = new Vector2(bounds.xMax, c.y);
    }

    // Background picture (if the file exists) and two layers of stars on top of it.
    // Far layers scroll slower than near ones.
    static void BuildBackground(Transform parent, Vector2 center)
    {
        var root = new GameObject("Background").transform;
        root.SetParent(parent, false);

        ParallaxBackground.Create(root, center);
        StarLayer.Create("StarsFar", root, center, Tuning.StarsFarParallax, 0.04f, 0.1f, 0.4f, -150);
        StarLayer.Create("StarsNear", root, center, Tuning.StarsNearParallax, 0.08f, 0.18f, 0.7f, -140);
    }
}
