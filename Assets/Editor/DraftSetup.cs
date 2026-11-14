using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Makes the game's assets ONCE in the editor, so nothing is built at runtime:
//   Assets/Prefabs/*.prefab           player, bullet, rocks, meteors, ship, tank, crystal, background
//   Assets/Prefabs/Levels/LevelN.prefab  one per Assets/Resources/Levels/levelN.txt
//   Assets/DraftScene.unity            camera, light, HUD and the DraftGame object
// After that, edit the prefabs and the scene by hand like any Unity project.
// Running it again OVERWRITES those prefabs and the HUD/DraftGame objects in the scene.
public static class DraftSetup
{
    const string ScenePath = "Assets/DraftScene.unity";
    const string PrefabFolder = "Assets/Prefabs";
    const string LevelFolder = "Assets/Prefabs/Levels";
    const string PhysicsFolder = "Assets/Physics";

    // Balance numbers used while building (Assets/_Settings/Tuning.asset); also connected
    // to every component made here.
    static Tuning tune;

    [MenuItem("Filip/Rebuild Prefabs And Scene")]
    static void RebuildMenu()
    {
        if (!EditorUtility.DisplayDialog("Rebuild prefabs and scene",
            "This overwrites everything in Assets/Prefabs (including level prefabs you edited by hand) " +
            "and the HUD and DraftGame objects in DraftScene.\n\nContinue?", "Rebuild", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        RebuildAll();
    }

    // For batch mode: Unity -batchmode -quit -executeMethod DraftSetup.RebuildAll
    public static void RebuildAll()
    {
        var oldRandom = Random.state;
        Random.InitState(1311); // same layout every time
        try
        {
            tune = TuningConnector.GetOrCreate();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureFolder(PrefabFolder);
            EnsureFolder(LevelFolder);
            EnsureFolder(PhysicsFolder);

            var bouncy = PhysicsMaterial("Bouncy");

            var bullet = SavePrefab(BuildBullet(), "Bullet").GetComponent<Bullet>();
            var crystal = SavePrefab(BuildCrystal(), "OxygenCrystal").GetComponent<OxygenCrystal>();
            var tank = SavePrefab(BuildTank(), "OxygenTank");
            var ship = SavePrefab(BuildShip(), "Ship");
            var player = SavePrefab(BuildPlayer(bullet, bouncy), "Player");

            var rocks = new Dictionary<AsteroidSize, Asteroid>();
            rocks[AsteroidSize.Small] = SavePrefab(BuildAsteroid(AsteroidSize.Small, null, crystal, bouncy, true),
                "Asteroid_Small").GetComponent<Asteroid>();
            rocks[AsteroidSize.Medium] = SavePrefab(BuildAsteroid(AsteroidSize.Medium, null, crystal, bouncy, true),
                "Asteroid_Medium").GetComponent<Asteroid>();
            rocks[AsteroidSize.Large] = SavePrefab(BuildAsteroid(AsteroidSize.Large, rocks[AsteroidSize.Medium],
                crystal, bouncy, true), "Asteroid_Large").GetComponent<Asteroid>();

            var meteorSmall = SavePrefab(BuildMeteor(AsteroidSize.Small, crystal, bouncy), "Meteor_Small")
                .GetComponent<Asteroid>();
            var meteorMedium = SavePrefab(BuildMeteor(AsteroidSize.Medium, crystal, bouncy), "Meteor_Medium")
                .GetComponent<Asteroid>();

            var background = SavePrefab(BuildBackground(), "Background");

            var levels = new List<Level>();
            int count = LevelData.Count();
            for (int i = 1; i <= count; i++)
            {
                var data = LevelData.Load(i, tune);
                var root = BuildLevel(data, player, ship, tank, rocks, background, meteorSmall, meteorMedium);
                TuningConnector.Connect(root, tune);
                var asset = PrefabUtility.SaveAsPrefabAsset(root, LevelFolder + "/Level" + i + ".prefab");
                Object.DestroyImmediate(root);
                levels.Add(asset.GetComponent<Level>());
            }
            if (levels.Count == 0) Debug.LogWarning("DraftSetup: no level files in Assets/Resources/Levels.");

            SetupScene(levels.ToArray(), bullet);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("DraftSetup: prefabs, " + levels.Count + " levels and DraftScene are ready.");
        }
        finally
        {
            Random.state = oldRandom;
        }
    }

    // --- Prefabs ---

    static GameObject BuildBullet()
    {
        var go = new GameObject("Bullet");
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.8f, 0.25f, tune.BulletGlowOpacity), Vector2.one * tune.BulletGlowSize, 19);
        SpriteFactory.MakeSprite("Visual", go.transform, SpriteFactory.Circle(),
            new Color(1f, 0.95f, 0.6f), Vector2.one * tune.BulletDiameter, 20);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = tune.BulletMass;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = tune.BulletDiameter * 0.5f;

        var b = go.AddComponent<Bullet>();
        Set(b, "rb", rb);
        Set(b, "col", col);
        return go;
    }

    static GameObject BuildCrystal()
    {
        var go = new GameObject("OxygenCrystal");
        // Cyan glow, like the oxygen tanks: everything that gives air is cyan.
        var glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(0.3f, 0.85f, 1f, 0.7f), Vector2.one * tune.CrystalSize * 3.5f, 6);

        // Picture "crystal" or a placeholder: a small diamond.
        var core = Art.TryMake("crystal", go.transform, tune.CrystalSize, 7);
        if (core == null)
        {
            core = SpriteFactory.MakeSprite("Core", go.transform, SpriteFactory.Square(),
                new Color(0.75f, 0.97f, 1f), Vector2.one * tune.CrystalSize * 0.7f, 7);
            core.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        var c = go.AddComponent<OxygenCrystal>();
        Set(c, "core", core);
        Set(c, "glow", glow);
        return go;
    }

    static GameObject BuildTank()
    {
        var go = new GameObject("OxygenTank");
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(0.3f, 0.85f, 1f, tune.OxygenTankGlowOpacity), Vector2.one * tune.OxygenTankGlowSize, 2);

        float s = tune.OxygenTankSize;
        if (Art.TryMake("oxygen", go.transform, s, 4) == null)
        {
            var color = new Color(0.3f, 0.85f, 1f);
            var bottle = SpriteFactory.MakeSprite("Bottle", go.transform, SpriteFactory.Square(),
                color, new Vector2(s * 0.5f, s * 0.8f), 4);
            bottle.transform.localPosition = new Vector3(0f, -s * 0.1f, 0f);
            var cap = SpriteFactory.MakeSprite("Cap", go.transform, SpriteFactory.Square(),
                Color.white, new Vector2(s * 0.25f, s * 0.2f), 4);
            cap.transform.localPosition = new Vector3(0f, s * 0.4f, 0f);
        }

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = tune.OxygenTankPickupRadius;
        col.isTrigger = true;
        go.AddComponent<OxygenTank>();
        return go;
    }

    static GameObject BuildShip()
    {
        var go = new GameObject("Ship");
        float len = tune.ShipLength, h = tune.ShipHeight;
        Vector2 size = new Vector2(len, h);

        var art = Art.TryMake("ship", go.transform, len, 5);
        if (art != null)
        {
            size = Art.WorldSize(art); // docking area matches the picture
        }
        else
        {
            SpriteFactory.MakeSprite("Hull", go.transform, SpriteFactory.Square(),
                new Color(0.55f, 0.65f, 0.8f), new Vector2(len, h), 5);
            var nose = SpriteFactory.MakeSprite("Nose", go.transform, SpriteFactory.Square(),
                new Color(0.8f, 0.9f, 1f), new Vector2(len * 0.2f, h * 0.5f), 6);
            nose.transform.localPosition = new Vector3(len * 0.4f, 0f, 0f);
            var engine = SpriteFactory.MakeSprite("Engine", go.transform, SpriteFactory.Square(),
                new Color(0.3f, 0.35f, 0.45f), new Vector2(len * 0.12f, h * 0.7f), 6);
            engine.transform.localPosition = new Vector3(-len * 0.44f, 0f, 0f);
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;
        go.AddComponent<Ship>();
        return go;
    }

    static GameObject BuildPlayer(Bullet bullet, PhysicsMaterial2D material)
    {
        var go = new GameObject("Player");
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = tune.PlayerMass;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = tune.PlayerDiameter * 0.5f;
        col.sharedMaterial = material;

        // Soft white glow under the player: the player is always the easiest thing to find.
        SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(1f, 1f, 1f, tune.PlayerGlowOpacity),
            Vector2.one * tune.PlayerDiameter * tune.PlayerGlowSize, 9);

        var visual = new GameObject("Visual").transform;
        visual.SetParent(go.transform, false);
        if (Art.TryMake("astronaut", visual, tune.PlayerDiameter, 10) == null)
        {
            SpriteFactory.MakeSprite("Body", visual, SpriteFactory.Circle(),
                new Color(0.9f, 0.9f, 0.95f), Vector2.one * tune.PlayerDiameter, 10);
            var visor = SpriteFactory.MakeSprite("Visor", visual, SpriteFactory.Circle(),
                new Color(0.3f, 0.6f, 1f), new Vector2(0.35f, 0.5f), 11);
            visor.transform.localPosition = new Vector3(0.2f, 0f, 0f);
            var gun = SpriteFactory.MakeSprite("Gun", visual, SpriteFactory.Square(),
                new Color(0.5f, 0.5f, 0.55f), new Vector2(0.7f, 0.2f), 12);
            gun.transform.localPosition = new Vector3(0.55f, -0.25f, 0f);
        }
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(visual, false);
        muzzle.localPosition = new Vector3(0.9f, -0.25f, 0f);

        var p = go.AddComponent<PlayerController>();
        Set(p, "rb", rb);
        Set(p, "col", col);
        Set(p, "visual", visual);
        Set(p, "muzzle", muzzle);
        Set(p, "bulletPrefab", bullet);
        return go;
    }

    static string ArtName(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return "asteroid_s";
            case AsteroidSize.Medium: return "asteroid_m";
            default: return "asteroid_l";
        }
    }

    // A rock at its class size (scale 1). Pictures are rendered so that a circle of
    // 0.45 x picture size has the same area as the rock (see Editor/ArtBake.cs).
    static GameObject BuildAsteroid(AsteroidSize size, Asteroid split, OxygenCrystal crystal,
        PhysicsMaterial2D material, bool confined)
    {
        float d = Asteroid.Diameter(tune, size);
        var go = new GameObject("Asteroid_" + size);

        var variants = Art.LoadAll(ArtName(size));
        float radius = d * 0.45f;
        SpriteRenderer body;
        if (variants.Count > 0)
        {
            body = Art.Make("Body", variants[0], go.transform, d, 3);
        }
        else
        {
            radius = d * 0.5f;
            body = SpriteFactory.MakeSprite("Body", go.transform, SpriteFactory.Circle(),
                new Color(0.5f, 0.475f, 0.45f), Vector2.one * d, 3);
            var crater1 = SpriteFactory.MakeSprite("Crater", go.transform, SpriteFactory.Circle(),
                new Color(0f, 0f, 0f, 0.25f), Vector2.one * d * 0.3f, 4);
            crater1.transform.localPosition = new Vector3(d * 0.18f, d * 0.12f, 0f);
            var crater2 = SpriteFactory.MakeSprite("Crater", go.transform, SpriteFactory.Circle(),
                new Color(0f, 0f, 0f, 0.25f), Vector2.one * d * 0.2f, 4);
            crater2.transform.localPosition = new Vector3(-d * 0.2f, -d * 0.15f, 0f);
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = tune.AsteroidDensity * d * d; // recalculated in Start from the actual scale
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.sharedMaterial = material;

        var a = go.AddComponent<Asteroid>();
        var so = new SerializedObject(a);
        so.FindProperty("size").enumValueIndex = (int)size;
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.FindProperty("confined").boolValue = confined;
        so.FindProperty("splitPrefab").objectReferenceValue = split;
        so.FindProperty("crystalPrefab").objectReferenceValue = crystal;
        var list = so.FindProperty("variants");
        list.arraySize = variants.Count;
        for (int i = 0; i < variants.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    static GameObject BuildMeteor(AsteroidSize size, OxygenCrystal crystal, PhysicsMaterial2D material)
    {
        var go = BuildAsteroid(size, null, crystal, material, false);
        go.name = "Meteor_" + size;
        var a = go.GetComponent<Asteroid>();
        var body = go.transform.Find("Body").GetComponent<SpriteRenderer>();
        body.color *= new Color(1f, 0.7f, 0.5f); // warmer than normal rocks

        float d = Asteroid.Diameter(tune, size);
        // Hot glow around the rock and a soft fiery streak behind it (both drawn under the rock).
        SpriteFactory.MakeSprite("Heat", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.5f, 0.15f, tune.MeteorHeatOpacity), Vector2.one * d * tune.MeteorHeatSize, 1);
        var tail = SpriteFactory.MakeSprite("Tail", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.6f, 0.2f, 0.6f), new Vector2(d * 1.1f, d * 5f), 2).transform;
        tail.localPosition = new Vector3(0f, d * 2f, 0f);
        var warning = SpriteFactory.MakeSprite("Warning", go.transform, SpriteFactory.Triangle(),
            new Color(1f, 0.3f, 0.25f), Vector2.one * tune.MeteorWarningSize, 30).transform;
        warning.gameObject.SetActive(false);

        var m = go.AddComponent<Meteor>();
        Set(m, "asteroid", a);
        Set(m, "rb", go.GetComponent<Rigidbody2D>());
        Set(m, "tail", tail);
        Set(m, "warning", warning);
        return go;
    }

    // Nebula (or the artist's background picture) and three layers of stars, centered at (0, 0).
    // Draw order, back to front: far clouds, star dust, far stars, near clouds, near stars.
    static GameObject BuildBackground()
    {
        var root = new GameObject("Background");
        Vector2 center = Vector2.zero;

        var art = Art.TryMake("background", root.transform, tune.BackgroundSize, -200);
        if (art != null)
        {
            art.gameObject.AddComponent<ParallaxBackground>()
                .Setup(center, tune.BackgroundParallax, tune.BackgroundSize, false);
        }
        else
        {
            float b = tune.NebulaFarBrightness;
            Nebula("nebula_far", root.transform, center, tune.NebulaFarSize, tune.NebulaFarParallax, -200,
                1.002f, new Color(b, b, b, 1f));
            Nebula("nebula_near", root.transform, center, tune.NebulaNearSize, tune.NebulaNearParallax, -145,
                1f, new Color(1f, 1f, 1f, tune.NebulaNearOpacity));
        }

        StarDust("StarDust", root.transform, center, tune.StarDustParallax, tune.StarDustCount, -160);
        GlowingStars("StarsFar", root.transform, center, tune.StarsFarParallax, tune.StarsFarCount,
            tune.StarFarSizeMin, tune.StarFarSizeMax, 0.45f, -150);
        GlowingStars("StarsNear", root.transform, center, tune.StarsNearParallax, tune.StarsNearCount,
            tune.StarNearSizeMin, tune.StarNearSizeMax, 0.75f, -140);
        return root;
    }

    // 3x3 copies of a seamless picture. If the picture is missing, nothing is created (black sky).
    static void Nebula(string artName, Transform parent, Vector2 anchor, float size, float factor,
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

    // Colour of the level applied to the nebula tiles of a Background instance.
    static void TintBackground(GameObject background, Color tint)
    {
        float b = tune.NebulaFarBrightness;
        TintLayer(background.transform.Find("nebula_far"), new Color(b * tint.r, b * tint.g, b * tint.b, 1f));
        TintLayer(background.transform.Find("nebula_near"),
            new Color(tint.r, tint.g, tint.b, tune.NebulaNearOpacity));
    }

    static void TintLayer(Transform layer, Color color)
    {
        if (layer == null) return;
        foreach (var sr in layer.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.color = color;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        }
    }

    // Halo colours, picked to match the nebula; the last two are the artist's gold and plain white.
    static readonly Color[] StarPalette =
    {
        new Color(0.65f, 0.82f, 1.00f), // ice blue
        new Color(0.55f, 0.95f, 1.00f), // cyan
        new Color(0.78f, 0.65f, 1.00f), // violet
        new Color(1.00f, 0.68f, 0.88f), // pink
        new Color(1.00f, 0.82f, 0.50f), // gold
        new Color(1.00f, 1.00f, 1.00f), // white
    };

    // The star shape takes this share of its picture; the rest is room for the glow.
    const float StarShapeFraction = 0.42f;

    static Vector2[] StarPositions(int count)
    {
        float half = tune.StarTileSize * 0.5f;
        var local = new Vector2[count];
        for (int i = 0; i < count; i++) local[i] = new Vector2(Random.Range(-half, half), Random.Range(-half, half));
        return local;
    }

    // Tiny dim dots.
    static void StarDust(string name, Transform parent, Vector2 anchor, float factor, int count, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var local = StarPositions(count);
        var stars = new Transform[count];
        var circle = SpriteFactory.Circle();
        for (int i = 0; i < count; i++)
        {
            var color = new Color(0.84f, 0.9f, 1f, Random.Range(0.18f, 0.5f));
            stars[i] = SpriteFactory.MakeSprite("Dust", go.transform, circle, color,
                Vector2.one * Random.Range(0.03f, 0.07f), sortingOrder).transform;
        }
        go.AddComponent<StarLayer>().Setup(anchor, factor, tune.StarTileSize, stars, local,
            new SpriteRenderer[0], new SpriteRenderer[0], new float[0], new float[0]);
    }

    // Glowing stars: a bright core (star1, star2, ...) over a soft coloured halo (star_glow1, ...).
    static void GlowingStars(string name, Transform parent, Vector2 anchor, float factor, int count,
        float minSize, float maxSize, float minBrightness, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var local = StarPositions(count);
        var stars = new Transform[count];
        var cores = new SpriteRenderer[count];
        var glows = new SpriteRenderer[count];
        var phase = new float[count];
        var speed = new float[count];

        for (int i = 0; i < count; i++)
        {
            var star = new GameObject("Star").transform;
            star.SetParent(go.transform, false);
            stars[i] = star;

            float size = Random.Range(minSize, maxSize);
            float brightness = Random.Range(minBrightness, 1f);
            Color tint = StarPalette[Random.Range(0, StarPalette.Length)];

            SpriteRenderer core, glow;
            var shape = Art.Load("star");
            if (shape != null)
            {
                // Core and glow pictures share one canvas, so both get the same size.
                float canvas = size / StarShapeFraction;
                var glowSprite = Art.LoadExact(shape.name.Replace("star", "star_glow"));
                glow = Art.Make("Glow", glowSprite != null ? glowSprite : SpriteFactory.Glow(), star, canvas, sortingOrder);
                core = Art.Make("Core", shape, star, canvas, sortingOrder + 1);
            }
            else
            {
                glow = Art.Make("Glow", SpriteFactory.Glow(), star, size * 2.4f, sortingOrder);
                core = Art.Make("Core", SpriteFactory.Circle(), star, size * 0.4f, sortingOrder + 1);
            }

            Color coreColor = Color.Lerp(Color.white, tint, 0.25f);
            coreColor.a = brightness * tune.StarBrightness;
            Color glowColor = tint;
            glowColor.a = brightness * tune.StarGlowOpacity;
            core.color = coreColor;
            glow.color = glowColor;

            cores[i] = core;
            glows[i] = glow;
            phase[i] = Random.Range(0f, Mathf.PI * 2f);
            speed[i] = Random.Range(tune.StarTwinkleSpeedMin, tune.StarTwinkleSpeedMax);
        }
        go.AddComponent<StarLayer>().Setup(anchor, factor, tune.StarTileSize, stars, local,
            cores, glows, phase, speed);
    }

    // --- Levels ---

    static GameObject BuildLevel(LevelData data, GameObject player, GameObject ship, GameObject tank,
        Dictionary<AsteroidSize, Asteroid> rocks, GameObject background, Asteroid meteorSmall, Asteroid meteorMedium)
    {
        var root = new GameObject("Level" + data.index);
        var level = root.AddComponent<Level>();
        level.levelName = data.name;
        level.radio = data.radio;
        level.oxygen = Mathf.Clamp(data.oxygen, 1f, tune.OxygenMax);
        level.movers = data.movers;
        level.star3 = data.star3;
        level.star2 = data.star2;
        level.bounds = data.Bounds;

        var bg = (GameObject)PrefabUtility.InstantiatePrefab(background, root.transform);
        TintBackground(bg, data.tint);
        BuildBorder(root.transform, level.bounds);

        var rockParent = Group("Rocks", root.transform);
        var tankParent = Group("OxygenTanks", root.transform);
        var hintParent = Group("Hints", root.transform);

        // Used if the map has no P or S.
        Vector2 start = new Vector2(level.bounds.center.x, level.bounds.yMin + tune.PlayerStartFromBottom);
        Vector2 shipPos = new Vector2(level.bounds.center.x, level.bounds.yMax - tune.ShipFromTop);

        for (int row = 0; row < data.rows.Count; row++)
        {
            string line = data.rows[row];
            for (int col = 0; col < line.Length; col++)
            {
                char c = line[col];
                if (c == '.') continue;
                Vector2 p = data.CellCenter(col, row);
                switch (c)
                {
                    case 'P': start = p; break;
                    case 'S': shipPos = p; break;
                    case 'a': PlaceRock(rocks[AsteroidSize.Small], p, rockParent); break;
                    case 'A': PlaceRock(rocks[AsteroidSize.Medium], p, rockParent); break;
                    case 'B': PlaceRock(rocks[AsteroidSize.Large], p, rockParent); break;
                    case 'o': Place(tank, p, tankParent); break;
                    default:
                        if (char.IsDigit(c) && data.hints.ContainsKey(c))
                        {
                            var h = new GameObject("Hint" + c).AddComponent<HintMarker>();
                            h.transform.SetParent(hintParent, false);
                            h.transform.position = p;
                            h.text = data.hints[c];
                        }
                        break;
                }
            }
        }

        level.player = Place(player, start, root.transform).GetComponent<PlayerController>();
        level.ship = Place(ship, shipPos, root.transform).transform;

        if (data.meteors)
        {
            var spawner = root.AddComponent<MeteorSpawner>();
            spawner.intervalMin = data.meteorMin;
            spawner.intervalMax = data.meteorMax;
            Set(spawner, "smallMeteorPrefab", meteorSmall);
            Set(spawner, "mediumMeteorPrefab", meteorMedium);
        }
        return root;
    }

    // A rock from the map: random look, rotation and a little bigger or smaller than its class.
    static void PlaceRock(Asteroid prefab, Vector2 position, Transform parent)
    {
        var go = Place(prefab.gameObject, position, parent);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        float s = Random.Range(tune.AsteroidSizeVariationMin, tune.AsteroidSizeVariationMax);
        go.transform.localScale = new Vector3(s, s, 1f);
        go.GetComponent<Asteroid>().PickRandomLook();
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    // Thin red frame showing where the level ends. If you change Level.bounds, move these too.
    static void BuildBorder(Transform parent, Rect bounds)
    {
        var border = Group("Border", parent);
        var square = SpriteFactory.Square();
        var color = new Color(1f, 0.3f, 0.25f, 0.6f);
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

    // --- Scene ---

    static void SetupScene(Level[] levels, Bullet bullet)
    {
        // Remove what an earlier run made.
        foreach (var name in new[] { "DraftGame", "HUD" })
        {
            GameObject old;
            while ((old = GameObject.Find(name)) != null) Object.DestroyImmediate(old);
        }

        EnsureGlobalLight();
        var rig = SetupCamera();
        var hud = BuildHud();

        var go = new GameObject("DraftGame");
        var game = go.AddComponent<DraftGame>();
        var so = new SerializedObject(game);
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("cameraRig").objectReferenceValue = rig;
        so.FindProperty("bulletPrefab").objectReferenceValue = bullet;
        var list = so.FindProperty("levels");
        list.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        so.FindProperty("startLevel").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        TuningConnector.Connect(go, tune);
        TuningConnector.Connect(rig.gameObject, tune);
    }

    static void EnsureGlobalLight()
    {
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return;

        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
    }

    static CameraRig SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);
        }
        cam.orthographic = true;
        cam.orthographicSize = tune.CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        var rig = cam.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
        return rig;
    }

    // --- HUD ---

    static readonly Color OxygenColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color GoodColor = new Color(0.4f, 1f, 0.5f);

    static Hud BuildHud()
    {
        var go = new GameObject("HUD");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var hud = go.AddComponent<Hud>();
        var so = new SerializedObject(hud);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Red tint over the whole screen while outside the level (created first = drawn behind the rest).
        var overlay = MakeImage("SignalOverlay", go.transform, new Color(1f, 0f, 0f, 0.22f));
        Stretch(overlay.rectTransform);
        overlay.gameObject.SetActive(false);
        Ref(so, "signalOverlay", overlay.gameObject);

        var signal = MakeText("SignalText", go.transform, font, 72, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f));
        signal.text = "SIGNAL LOST";
        Place(signal.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 100f));
        signal.gameObject.SetActive(false);
        Ref(so, "signalText", signal.gameObject);

        // OXYGEN label and bar, top-left.
        var label = MakeText("OxygenLabel", go.transform, font, 36, TextAnchor.UpperLeft, Color.white);
        label.text = "OXYGEN";
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(400f, 44f));

        var bg = MakeImage("OxygenBar", go.transform, new Color(1f, 1f, 1f, 0.2f));
        Place(bg.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -80f), new Vector2(400f, 28f));
        var fill = MakeImage("Fill", bg.transform, OxygenColor);
        Stretch(fill.rectTransform);
        Ref(so, "oxygenFillImage", fill);
        Ref(so, "oxygenFill", fill.rectTransform);

        BuildCompass(so, go.transform, font);
        BuildCombo(so, go.transform, font);

        // Level name, shown for a few seconds at the start of a level.
        var title = MakeText("Title", go.transform, font, 44, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1600f, 60f));
        title.text = "LEVEL 1: NAME";
        title.gameObject.SetActive(false);
        Ref(so, "titleText", title);

        // Short notice in the upper part of the screen (TOO FAST).
        var toast = MakeText("Toast", go.transform, font, 60, TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.2f));
        Place(toast.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1600f, 80f));
        toast.text = "TOO FAST";
        toast.gameObject.SetActive(false);
        Ref(so, "toastText", toast);

        // Hint at the bottom of the screen.
        var hint = MakeText("Hint", go.transform, font, 34, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.9f));
        Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1800f, 50f));
        hint.text = "HINT";
        hint.gameObject.SetActive(false);
        Ref(so, "hintText", hint);

        // Big message in the center (DOCKED / OUT OF OXYGEN) with a line under it.
        var message = MakeText("Message", go.transform, font, 110, TextAnchor.MiddleCenter, Color.white);
        Place(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 160f));
        message.text = "DOCKED";
        message.gameObject.SetActive(false);
        Ref(so, "message", message);
        var messageSub = MakeText("MessageSub", go.transform, font, 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
        Place(messageSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1800f, 60f));
        messageSub.gameObject.SetActive(false);
        Ref(so, "messageSub", messageSub);

        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // Round compass on the left side, under the oxygen bar: the arrow on the rim points
    // to the ship, the distance is in the middle, the player's speed is under it.
    static void BuildCompass(SerializedObject so, Transform parent, Font font)
    {
        const float size = 180f;
        var root = new GameObject("Compass", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, new Vector2(0f, 1f), new Vector2(40f, -135f), new Vector2(size, size));

        var disc = MakeImage("Disc", root, new Color(0f, 0f, 0f, 0.45f));
        disc.sprite = SpriteFactory.Circle();
        Stretch(disc.rectTransform);

        var ring = MakeImage("Ring", root, new Color(1f, 1f, 1f, 0.7f));
        ring.sprite = SpriteFactory.Ring();
        Stretch(ring.rectTransform);

        var needle = new GameObject("Needle", typeof(RectTransform)).GetComponent<RectTransform>();
        needle.SetParent(root, false);
        Place(needle, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Ref(so, "compassNeedle", needle);

        var arrow = MakeImage("Arrow", needle, GoodColor);
        arrow.sprite = SpriteFactory.Triangle();
        Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, size * 0.5f - 26f), new Vector2(34f, 34f));

        var distance = MakeText("Distance", root, font, 28, TextAnchor.MiddleCenter, Color.white);
        Place(distance.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, 80f));
        distance.text = "SHIP\n0 m";
        Ref(so, "compassText", distance);

        // Green while slow enough to dock, red when too fast.
        var speed = MakeText("Speed", parent, font, 30, TextAnchor.UpperLeft, GoodColor);
        Place(speed.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -325f), new Vector2(300f, 40f));
        speed.text = "SPEED 0";
        Ref(so, "speedText", speed);
    }

    // Combo counter in the top-right corner, with a bar showing the time left to continue it.
    static void BuildCombo(SerializedObject so, Transform parent, Font font)
    {
        var root = new GameObject("Combo", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        Place(root, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(420f, 100f));

        var text = MakeText("Text", root, font, 64, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f));
        Stretch(text.rectTransform);
        text.text = "COMBO x2";

        var barBg = MakeImage("Bar", root, new Color(1f, 1f, 1f, 0.2f));
        Place(barBg.rectTransform, new Vector2(1f, 0f), Vector2.zero, new Vector2(260f, 8f));
        var barFill = MakeImage("Fill", barBg.transform, new Color(1f, 0.85f, 0.3f));
        Stretch(barFill.rectTransform);

        root.gameObject.SetActive(false);
        Ref(so, "comboRoot", root.gameObject);
        Ref(so, "comboText", text);
        Ref(so, "comboBar", barFill.rectTransform);
    }

    static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Anchors the rect to one point of the parent; pivot matches the anchor.
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    // Fills the whole parent.
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // --- Helpers ---

    static GameObject SavePrefab(GameObject go, string name)
    {
        TuningConnector.Connect(go, tune);
        var asset = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return asset;
    }

    static PhysicsMaterial2D PhysicsMaterial(string name)
    {
        string path = PhysicsFolder + "/" + name + ".physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (m == null)
        {
            m = new PhysicsMaterial2D(name);
            AssetDatabase.CreateAsset(m, path);
        }
        m.friction = 0f;
        m.bounciness = tune.Bounciness;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        Ref(so, field, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Ref(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError("DraftSetup: field '" + field + "' not found on " + so.targetObject.GetType().Name);
            return;
        }
        p.objectReferenceValue = value;
    }
}
