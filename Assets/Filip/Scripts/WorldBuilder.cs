using System.Collections.Generic;
using UnityEngine;

// Everything that belongs to one level. Destroy root to clear the level.
public class LevelWorld
{
    public Transform root;
    public LevelData level;      // null on the test ground (no level files)
    public PlayerController player;
    public Transform ship;
    public Rect bounds;          // level area; outside it the signal is lost
    public List<Vector2> hintPositions = new List<Vector2>();
    public List<string> hintTexts = new List<string>();
}

// Builds the level objects. Used by the game (on Play) and by the editor preview
// (without Play), so this code must also work in edit mode: no Destroy(), no coroutines.
public static class WorldBuilder
{
    // level == null builds the random test ground (used when there are no level files).
    public static LevelWorld Build(LevelData level)
    {
        var w = new LevelWorld();
        w.level = level;
        w.root = new GameObject("World").transform;
        if (level != null) BuildLevel(w, level);
        else BuildTestGround(w);
        return w;
    }

    // --- Level from a text file ---

    static void BuildLevel(LevelWorld w, LevelData level)
    {
        w.bounds = level.Bounds;
        Asteroid.Bounds = w.bounds;
        BuildBackground(w.root, w.bounds.center, level);
        BuildBorder(w.root, w.bounds, level.frame);

        // Used if the map has no P or S.
        Vector2 start = new Vector2(w.bounds.center.x, w.bounds.yMin + Tuning.PlayerStartFromBottom);
        Vector2 shipPos = new Vector2(w.bounds.center.x, w.bounds.yMax - Tuning.ShipFromTop);

        for (int row = 0; row < level.rows.Count; row++)
        {
            string line = level.rows[row];
            for (int col = 0; col < line.Length; col++)
            {
                char c = line[col];
                if (c == '.') continue;
                Vector2 p = level.CellCenter(col, row);
                switch (c)
                {
                    case 'P': start = p; break;
                    case 'S': shipPos = p; break;
                    case 'a': SpawnRock(AsteroidSize.Small, p, level.movers, w.root); break;
                    case 'A': SpawnRock(AsteroidSize.Medium, p, level.movers, w.root); break;
                    case 'B': SpawnRock(AsteroidSize.Large, p, level.movers, w.root); break;
                    case 'o': OxygenTank.Create(p, w.root); break;
                    case 'm': Mine.Create(p, w.root); break;
                    case '@': Planet.Create(p, w.root); break;
                    default:
                        if (char.IsDigit(c) && level.hints.ContainsKey(c))
                        {
                            w.hintPositions.Add(p);
                            w.hintTexts.Add(level.hints[c]);
                        }
                        break;
                }
            }
        }

        w.player = PlayerController.Create(start, w.root);
        w.ship = Ship.Create(shipPos, w.root).transform;
        if (level.meteors)
            MeteorSpawner.Create(w.root, w.player.transform, level.meteorMin, level.meteorMax);
    }

    // Some rocks barely drift, others fly noticeably in a random direction.
    static void SpawnRock(AsteroidSize size, Vector2 position, float moverChance, Transform parent)
    {
        bool mover = Random.value < moverChance;
        float speed = mover
            ? Random.Range(Tuning.AsteroidMoveSpeedMin, Tuning.AsteroidMoveSpeedMax)
            : Random.Range(Tuning.AsteroidDriftMin, Tuning.AsteroidDriftMax);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Asteroid.Create(size, position, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, parent);
    }

    // --- Random test ground (only when there are no level files) ---

    static void BuildTestGround(LevelWorld w)
    {
        w.bounds = new Rect(-Tuning.LevelWidth * 0.5f, -Tuning.LevelHeight * 0.5f,
            Tuning.LevelWidth, Tuning.LevelHeight);
        Asteroid.Bounds = w.bounds;
        Vector2 start = new Vector2(w.bounds.center.x, w.bounds.yMin + Tuning.PlayerStartFromBottom);
        Vector2 shipPos = new Vector2(w.bounds.center.x, w.bounds.yMax - Tuning.ShipFromTop);

        var theme = new LevelData(); // default look
        BuildBackground(w.root, w.bounds.center, theme);
        BuildBorder(w.root, w.bounds, theme.frame);
        w.player = PlayerController.Create(start, w.root);
        w.ship = Ship.Create(shipPos, w.root).transform;
        Scatter(w.root, start, shipPos);
        MeteorSpawner.Create(w.root, w.player.transform, Tuning.MeteorIntervalMin, Tuning.MeteorIntervalMax);
    }

    // Random asteroids on the way to the ship and oxygen tanks off to the sides.
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
            // Spacing is checked for the biggest rock of this class (sizes vary a little).
            float radius = Asteroid.Diameter(size) * Tuning.AsteroidSizeVariationMax * 0.5f;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                var pos = new Vector2(
                    start.x + Random.Range(-Tuning.ScatterHalfWidth, Tuning.ScatterHalfWidth),
                    Random.Range(yMin, yMax));
                if (!IsFree(placed, pos, radius + 0.5f)) continue;

                placed.Add(new Vector3(pos.x, pos.y, radius));
                SpawnRock(size, pos, Tuning.AsteroidMoverChance, parent);
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

    // --- Shared pieces ---

    // Thin frame showing where the level ends (red unless the level sets its own colour).
    static void BuildBorder(Transform parent, Rect bounds, Color frame)
    {
        var border = new GameObject("Border").transform;
        border.SetParent(parent, false);
        var square = SpriteFactory.Square();
        var color = new Color(frame.r, frame.g, frame.b, 0.6f);
        const float t = 0.16f;
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

    // Clouds (or the artist's background picture), a far backdrop object and three layers
    // of stars. Far layers scroll slower than near ones. theme: the look settings of the level.
    static void BuildBackground(Transform parent, Vector2 center, LevelData theme)
    {
        var root = new GameObject("Background").transform;
        root.SetParent(parent, false);

        // Draw order, back to front: far clouds, backdrop, star dust, far stars, near clouds, near stars.
        ParallaxBackground.CreateAll(root, center, theme);

        float density = theme.starDensity;
        Color starColor = theme.starColor;
        StarLayer.CreateDust("StarDust", root, center, Tuning.StarDustParallax,
            Mathf.RoundToInt(Tuning.StarDustCount * density), -160, starColor);
        StarLayer.CreateGlowing("StarsFar", root, center, Tuning.StarsFarParallax,
            Mathf.RoundToInt(Tuning.StarsFarCount * density),
            Tuning.StarFarSizeMin, Tuning.StarFarSizeMax, 0.45f, -150, starColor);
        StarLayer.CreateGlowing("StarsNear", root, center, Tuning.StarsNearParallax,
            Mathf.RoundToInt(Tuning.StarsNearCount * density),
            Tuning.StarNearSizeMin, Tuning.StarNearSizeMax, 0.75f, -140, starColor);
    }
}
