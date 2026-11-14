using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Makes the game's assets ONCE in the editor, so nothing is built at runtime:
//   Assets/Prefabs/*.prefab              player, bullet, rocks, meteors, ship, tank, crystal,
//                                        mines, mine ring, black holes, planet, leak, air puff, blast,
//                                        one background per level (Background_LevelN)
//   Assets/Prefabs/Levels/LevelN.prefab  one per Assets/_Resources/Levels/levelN.txt
//   Assets/_Scenes/DraftScene.unity      camera, light, HUD and the DraftGame object
// After that, edit the prefabs and the scene by hand like any Unity project.
// Running it again OVERWRITES those prefabs and the HUD/DraftGame objects in the scene.
public static class DraftSetup
{
    const string ScenePath = "Assets/_Scenes/DraftScene.unity";
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

            var bouncy = PhysicsMaterial("Bouncy", tune.Bounciness);
            var mineMaterial = PhysicsMaterial("Mine", 0.3f);

            var bullet = SavePrefab(BuildBullet(), "Bullet").GetComponent<Bullet>();
            var crystal = SavePrefab(BuildCrystal(), "OxygenCrystal").GetComponent<OxygenCrystal>();
            var tank = SavePrefab(BuildTank(), "OxygenTank");
            var ship = SavePrefab(BuildShip(), "Ship");

            // Effects.
            var puff = SavePrefab(BuildAirPuff(), "AirPuff").GetComponent<LeakPuff>();
            var leak = SavePrefab(BuildLeak(puff), "Leak").GetComponent<Leak>();
            var blast = SavePrefab(BuildBlast(), "Blast").GetComponent<Blast>();

            var player = SavePrefab(BuildPlayer(bullet, leak, bouncy), "Player");

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

            // Mines, black holes, planet.
            var things = new Things { player = player, ship = ship, tank = tank, rocks = rocks };
            foreach (MineMode mode in System.Enum.GetValues(typeof(MineMode)))
                things.mines[mode] = SavePrefab(BuildMine(mode, crystal, blast, mineMaterial), "Mine_" + mode);
            things.mineRing = SavePrefab(BuildMineRing(things.mines[MineMode.Orbit]), "MineRing");
            things.holeBig = SavePrefab(BuildBlackHole("BlackHole_Big", tune.HoleBigRadius, tune.HoleBigField,
                tune.HoleBigStrength, false, tune.HoleOrbitRocks, true, rocks), "BlackHole_Big");
            things.holeSmall = SavePrefab(BuildBlackHole("BlackHole_Small", tune.HoleSmallRadius, tune.HoleSmallField,
                tune.HoleSmallStrength, false, tune.HoleSmallOrbitRocks, false, rocks), "BlackHole_Small");
            things.holeDrift = SavePrefab(BuildBlackHole("BlackHole_Drift", tune.HoleSmallRadius, tune.HoleSmallField,
                tune.HoleSmallStrength, true, 0, false, rocks), "BlackHole_Drift");
            things.planet = SavePrefab(BuildPlanet(), "Planet");

            // Every level gets its own background (Background_LevelN) in the level's colours.
            // "Background" is the plain look, used by no level: a starting point for a new one.
            SavePrefab(BuildBackground(new LevelData()), "Background");

            var levels = new List<Level>();
            int count = LevelData.Count();
            for (int i = 1; i <= count; i++)
            {
                var data = LevelData.Load(i, tune);
                var background = SavePrefab(BuildBackground(data), "Background_Level" + i);
                var root = BuildLevel(data, things, background, meteorSmall, meteorMedium);
                TuningConnector.Connect(root, tune);
                var asset = PrefabUtility.SaveAsPrefabAsset(root, LevelFolder + "/Level" + i + ".prefab");
                Object.DestroyImmediate(root);
                levels.Add(asset.GetComponent<Level>());
            }
            if (levels.Count == 0) Debug.LogWarning("DraftSetup: no level files in Assets/_Resources/Levels.");

            SetupScene(levels.ToArray(), bullet, blast);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.SetDirty(tune); // new balance numbers get written into the asset
            AssetDatabase.SaveAssets();
            Debug.Log("DraftSetup: prefabs, " + levels.Count + " levels and DraftScene are ready.");
        }
        finally
        {
            Random.state = oldRandom;
        }
    }

    // Prefabs that level maps place.
    class Things
    {
        public GameObject player, ship, tank, mineRing, holeBig, holeSmall, holeDrift, planet;
        public Dictionary<AsteroidSize, Asteroid> rocks;
        public Dictionary<MineMode, GameObject> mines = new Dictionary<MineMode, GameObject>();
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

        // Docking ring: invisible until the player comes close (Ship.Update colours it).
        float ringSize = Mathf.Max(size.x, size.y) + tune.DockRingMargin * 2f;
        var ring = SpriteFactory.MakeSprite("DockRing", go.transform, SpriteFactory.Ring(),
            new Color(1f, 1f, 1f, 0f), Vector2.one * ringSize, 4);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;
        var s = go.AddComponent<Ship>();
        Set(s, "dockRing", ring);
        return go;
    }

    // One puff of air escaping from a hull breach.
    static GameObject BuildAirPuff()
    {
        var go = new GameObject("AirPuff");
        var sr = SpriteFactory.MakeSprite("Puff", go.transform, SpriteFactory.Glow(),
            new Color(0.85f, 0.97f, 1f, 0.7f), Vector2.one * 0.25f, 9);
        var p = go.AddComponent<LeakPuff>();
        Set(p, "sprite", sr);
        return go;
    }

    // A hull breach: a dark crack that blows air puffs.
    static GameObject BuildLeak(LeakPuff puff)
    {
        var go = new GameObject("Leak");
        var crack = SpriteFactory.MakeSprite("Crack", go.transform, SpriteFactory.Square(),
            new Color(0.05f, 0.05f, 0.08f), new Vector2(0.26f, 0.07f), 13);
        var l = go.AddComponent<Leak>();
        Set(l, "crack", crack.transform);
        Set(l, "puffPrefab", puff);
        return go;
    }

    // The flash of an explosion.
    static GameObject BuildBlast()
    {
        var go = new GameObject("Blast");
        var outer = SpriteFactory.MakeSprite("Outer", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.45f, 0.15f, 0.9f), Vector2.one, 25);
        var core = SpriteFactory.MakeSprite("Core", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.95f, 0.8f, 1f), Vector2.one, 26);
        var b = go.AddComponent<Blast>();
        Set(b, "outer", outer);
        Set(b, "core", core);
        return go;
    }

    static GameObject BuildPlayer(Bullet bullet, Leak leak, PhysicsMaterial2D material)
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
        Set(p, "leakPrefab", leak);
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

        var m = go.AddComponent<Meteor>();
        Set(m, "asteroid", a);
        Set(m, "rb", go.GetComponent<Rigidbody2D>());
        Set(m, "tail", tail);
        return go;
    }

    // --- Mines ---

    static readonly Color MineRed = new Color(1f, 0.25f, 0.2f);
    static readonly Color MineYellow = new Color(1f, 0.8f, 0.2f);
    static readonly Color MineOrange = new Color(1f, 0.5f, 0.1f);

    // Picture "mine" (round, 1.1 units) or a placeholder: a dark ball with spikes.
    // The lamp in the middle is drawn over the picture too: its colour tells the kind of mine.
    static GameObject BuildMine(MineMode mode, OxygenCrystal crystal, Blast blast, PhysicsMaterial2D material)
    {
        bool heavy = mode == MineMode.Heavy;
        float s = heavy ? tune.MineHeavySize : tune.MineSize;
        Color color = mode == MineMode.Hunter ? MineRed : heavy ? MineOrange : MineYellow;

        var go = new GameObject("Mine_" + mode);
        // Glow in the mine's colour: everything dangerous glows.
        var glow = SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
            new Color(color.r, color.g, color.b, 0.45f), Vector2.one * s * 2.6f, 2);

        if (Art.TryMake("mine", go.transform, s, 4) == null)
        {
            for (int i = 0; i < 4; i++)
            {
                var spike = SpriteFactory.MakeSprite("Spike", go.transform, SpriteFactory.Square(),
                    new Color(0.2f, 0.21f, 0.26f), new Vector2(s * 1.25f, s * 0.16f), 4);
                spike.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }
            SpriteFactory.MakeSprite("Body", go.transform, SpriteFactory.Circle(),
                new Color(0.13f, 0.14f, 0.18f), Vector2.one * s * 0.82f, 4);
        }
        var lamp = SpriteFactory.MakeSprite("Lamp", go.transform, SpriteFactory.Circle(),
            color, Vector2.one * s * 0.3f, 5);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.mass = tune.MineMass;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = s * 0.45f;
        col.sharedMaterial = material;

        var m = go.AddComponent<Mine>();
        var so = new SerializedObject(m);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("rb").objectReferenceValue = rb;
        so.FindProperty("lamp").objectReferenceValue = lamp;
        so.FindProperty("glow").objectReferenceValue = glow;
        so.FindProperty("hits").intValue = heavy ? tune.MineHeavyHits : 1;
        so.FindProperty("size").floatValue = s;
        so.FindProperty("blastScale").floatValue = heavy ? tune.MineHeavyBlastScale : 1f;
        so.FindProperty("axis").vector2Value = Vector2.right;
        so.FindProperty("crystalPrefab").objectReferenceValue = crystal;
        so.FindProperty("blastPrefab").objectReferenceValue = blast;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // Mines circling a point (put an oxygen tank in the middle). They are spaced so that
    // one blast does not set off the next. Each mine circles around this object.
    static GameObject BuildMineRing(GameObject orbitMine)
    {
        var root = new GameObject("MineRing");
        for (int i = 0; i < tune.MineRingCount; i++)
        {
            float angle = Mathf.PI * 2f * i / tune.MineRingCount;
            Place(orbitMine, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * tune.MineRingRadius, root.transform);
        }
        return root;
    }

    // --- Black holes and the planet ---

    // The dark disc is this share of the "blackhole" picture's side (see Tools~/BackdropGen.cs).
    const float HoleDiscShare = 0.16f;

    // orbitRocks: rocks circling the hole. On the hole level they are the only air: shoot them
    // with curved shots and grab the crystals before the hole eats them.
    // twoCircles: every second rock is a medium one on a wider circle turning the other way.
    static GameObject BuildBlackHole(string name, float bodyRadius, float fieldRadius, float strength,
        bool drifts, int orbitRocks, bool twoCircles, Dictionary<AsteroidSize, Asteroid> rocks)
    {
        var go = new GameObject(name);

        // Soft warm haze that gets denser toward the hole: where it is visible, the hole pulls.
        SpriteFactory.MakeSprite("PullHaze", go.transform, SpriteFactory.Glow(),
            new Color(1f, 0.55f, 0.3f, 0.2f), Vector2.one * fieldRadius * 2f, -41);

        // Picture "blackhole" (dark disc with a glowing ring) or a placeholder.
        Transform picture = null;
        var art = Art.TryMake("blackhole", go.transform, bodyRadius / HoleDiscShare, 3);
        if (art != null) picture = art.transform;
        else
        {
            SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
                new Color(1f, 0.6f, 0.25f, 0.6f), Vector2.one * bodyRadius * 5f, 2);
            SpriteFactory.MakeSprite("Ring", go.transform, SpriteFactory.Ring(),
                new Color(1f, 0.8f, 0.5f), Vector2.one * bodyRadius * 2.8f, 3);
            SpriteFactory.MakeSprite("Disc", go.transform, SpriteFactory.Circle(),
                Color.black, Vector2.one * bodyRadius * 2f, 4);
        }

        var hole = go.AddComponent<BlackHole>();
        var so = new SerializedObject(hole);
        so.FindProperty("bodyRadius").floatValue = bodyRadius;
        so.FindProperty("fieldRadius").floatValue = fieldRadius;
        so.FindProperty("strength").floatValue = strength;
        so.FindProperty("drifts").boolValue = drifts;
        so.FindProperty("axis").vector2Value = Vector2.right;
        so.FindProperty("picture").objectReferenceValue = picture;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (orbitRocks > 0)
        {
            var rockParent = Group("Rocks", go.transform);
            for (int i = 0; i < orbitRocks; i++)
            {
                bool outer = twoCircles && i % 2 == 1;
                float radius = bodyRadius + (twoCircles ? (outer ? 9f : 5f) : 4f);
                float angle = Mathf.PI * 2f * i / orbitRocks + Random.Range(-0.3f, 0.3f);
                Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                var prefab = rocks[outer ? AsteroidSize.Medium : AsteroidSize.Small];
                var rock = PlaceRock(prefab, pos, rockParent);
                SetBool(rock.GetComponent<Asteroid>(), "confined", false);
                rock.AddComponent<Orbiter>().Setup(rock.GetComponent<Rigidbody2D>(), go.transform,
                    radius, angle, outer ? -1f : 1f);
            }
        }
        return go;
    }

    // Picture "planet1" (the ball takes 0.44 of it) or a placeholder ball with a glow.
    static GameObject BuildPlanet()
    {
        var go = new GameObject("Planet");
        float size = tune.PlanetSize;
        float bodyRadius = size * tune.PlanetBodyShare;

        if (Art.TryMake("planet", go.transform, size, 3) == null)
        {
            SpriteFactory.MakeSprite("Glow", go.transform, SpriteFactory.Glow(),
                new Color(1f, 0.7f, 0.4f, 0.5f), Vector2.one * bodyRadius * 3f, 2);
            SpriteFactory.MakeSprite("Ball", go.transform, SpriteFactory.Circle(),
                new Color(0.8f, 0.5f, 0.3f), Vector2.one * bodyRadius * 2f, 3);
        }

        // Dotted circle: the edge of the gravity field.
        var edge = Group("FieldEdge", go.transform);
        const int dots = 90;
        for (int i = 0; i < dots; i++)
        {
            float a = Mathf.PI * 2f * i / dots;
            var dot = SpriteFactory.MakeSprite("Dot", edge, SpriteFactory.Circle(),
                new Color(1f, 0.8f, 0.55f, 0.4f), Vector2.one * 0.22f, -40);
            dot.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * tune.GravityRadius;
        }

        // Solid body: rocks bounce off it, bullets vanish on it, the player dies on it.
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = bodyRadius;
        go.AddComponent<Planet>();
        return go;
    }

    // --- Background (one prefab per level, in the level's own colours) ---

    // Clouds (or the artist's background picture), a far backdrop object and three layers of
    // stars, centered at (0, 0). theme: the look settings of the level file.
    // Draw order, back to front: far clouds, backdrop, star dust, far stars, near clouds, near stars.
    static GameObject BuildBackground(LevelData theme)
    {
        var root = new GameObject("Background");
        Vector2 center = Vector2.zero;

        var art = Art.TryMake("background", root.transform, tune.BackgroundSize, -200);
        if (art != null)
        {
            art.gameObject.AddComponent<ParallaxBackground>().Setup(center, tune.BackgroundParallax,
                tune.BackgroundSize, ParallaxBackground.Mode.Clamped, Vector2.zero);
        }
        else
        {
            // Cloud set of this level; an unknown name falls back to the default clouds.
            string set = string.IsNullOrEmpty(theme.nebula) ? "nebula" : "nebula_" + theme.nebula;
            if (Art.LoadExact(set + "_far") == null) set = "nebula";

            // The far clouds are darkened and the near ones made more transparent, so the
            // background stays behind the rocks. The level can dim or tint its clouds.
            float b = tune.NebulaFarBrightness * theme.clouds;
            Color tint = theme.tint;
            Nebula(set + "_far", root.transform, center, tune.NebulaFarSize, tune.NebulaFarParallax, -200,
                1.002f, new Color(b * tint.r, b * tint.g, b * tint.b, 1f));
            Nebula(set + "_near", root.transform, center, tune.NebulaNearSize, tune.NebulaNearParallax, -145,
                1f, new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(tune.NebulaNearOpacity * theme.clouds)));

            Backdrop(theme.backdrop, root.transform, center);
        }

        float density = theme.starDensity;
        StarDust("StarDust", root.transform, center, tune.StarDustParallax,
            Mathf.RoundToInt(tune.StarDustCount * density), -160, theme.starColor);
        GlowingStars("StarsFar", root.transform, center, tune.StarsFarParallax,
            Mathf.RoundToInt(tune.StarsFarCount * density),
            tune.StarFarSizeMin, tune.StarFarSizeMax, 0.45f, -150, theme.starColor);
        GlowingStars("StarsNear", root.transform, center, tune.StarsNearParallax,
            Mathf.RoundToInt(tune.StarsNearCount * density),
            tune.StarNearSizeMin, tune.StarNearSizeMax, 0.75f, -140, theme.starColor);
        return root;
    }

    // 3x3 copies of a seamless picture. If the picture is missing, nothing is created (black sky).
    static void Nebula(string artName, Transform parent, Vector2 anchor, float size, float factor,
        int sortingOrder, float tileScale, Color color)
    {
        var sprite = Art.LoadExact(artName);
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
        go.AddComponent<ParallaxBackground>().Setup(anchor, factor, size, ParallaxBackground.Mode.Repeating, Vector2.zero);
    }

    // One big far object, picture backdrop_<name>. Drawn over the far clouds and under the near ones.
    static void Backdrop(string name, Transform parent, Vector2 anchor)
    {
        if (string.IsNullOrEmpty(name) || name == "none") return;

        // Size in units, place on screen (units from the screen center, when the player is in
        // the middle of the level) and colour. Kept dim: it is only scenery.
        // The slower an object drifts across the screen, the farther away it feels.
        float size = 20f, parallax = tune.BackdropParallax;
        Vector2 offset = new Vector2(8f, 4f);
        Color color = new Color(1f, 1f, 1f, 0.7f);
        switch (name)
        {
            case "sun": size = 14f; offset = new Vector2(11f, 6f); color = new Color(1f, 1f, 1f, 0.8f); parallax = 0.04f; break;
            // The station wreck is rendered dark, hazy and slightly out of focus: it must read
            // as far scenery in shadow, not as something the player can fly to.
            case "wreck": size = 34f; offset = new Vector2(4f, 2f); color = new Color(0.85f, 0.9f, 1f, 1f); parallax = 0.06f; break;
            case "planet": size = 46f; offset = new Vector2(16f, -8f); color = new Color(1f, 1f, 1f, 0.85f); break;
        }

        var sprite = Art.LoadExact("backdrop_" + name);
        if (sprite == null) return;
        var sr = Art.Make("backdrop_" + name, sprite, parent, size, -190);
        sr.color = color;
        sr.gameObject.AddComponent<ParallaxBackground>().Setup(anchor, parallax, size, ParallaxBackground.Mode.Free, offset);
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

    // Tiny dim dots. levelColor: colour of the stars on this level (white = as usual).
    static void StarDust(string name, Transform parent, Vector2 anchor, float factor, int count, int sortingOrder,
        Color levelColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var local = StarPositions(count);
        var stars = new Transform[count];
        var circle = SpriteFactory.Circle();
        for (int i = 0; i < count; i++)
        {
            var color = new Color(0.84f * levelColor.r, 0.9f * levelColor.g, levelColor.b, Random.Range(0.18f, 0.5f));
            stars[i] = SpriteFactory.MakeSprite("Dust", go.transform, circle, color,
                Vector2.one * Random.Range(0.03f, 0.07f), sortingOrder).transform;
        }
        go.AddComponent<StarLayer>().Setup(anchor, factor, tune.StarTileSize, stars, local,
            new SpriteRenderer[0], new SpriteRenderer[0], new float[0], new float[0]);
    }

    // Glowing stars: a bright core (star1, star2, ...) over a soft coloured halo (star_glow1, ...).
    static void GlowingStars(string name, Transform parent, Vector2 anchor, float factor, int count,
        float minSize, float maxSize, float minBrightness, int sortingOrder, Color levelColor)
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
            Color tint = StarPalette[Random.Range(0, StarPalette.Length)] * levelColor;

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

    static GameObject BuildLevel(LevelData data, Things things, GameObject background,
        Asteroid meteorSmall, Asteroid meteorMedium)
    {
        var root = new GameObject("Level" + data.index);
        var level = root.AddComponent<Level>();
        level.levelName = data.name;
        level.radio = data.radio;
        level.oxygen = Mathf.Clamp(data.oxygen, 1f, tune.OxygenMax);
        level.movers = data.movers;
        level.star3 = data.star3;
        level.star2 = data.star2;
        level.lightColor = data.light;
        level.stormColor = data.frame;
        level.bounds = data.Bounds;

        PrefabUtility.InstantiatePrefab(background, root.transform);
        BuildStorm(root.transform, level.bounds, data.frame);

        var rockParent = Group("Rocks", root.transform);
        var tankParent = Group("OxygenTanks", root.transform);
        var mineParent = Group("Mines", root.transform);
        var holeParent = Group("BlackHoles", root.transform);

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
                    case 'a': PlaceRock(things.rocks[AsteroidSize.Small], p, rockParent); break;
                    case 'A': PlaceRock(things.rocks[AsteroidSize.Medium], p, rockParent); break;
                    case 'B': PlaceRock(things.rocks[AsteroidSize.Large], p, rockParent); break;
                    case 'o': Place(things.tank, p, tankParent); break;

                    // Mines: hunter, sideways and up-down patrol, chain wall, heavy, ring around a tank.
                    case 'm': Place(things.mines[MineMode.Hunter], p, mineParent); break;
                    case 'h': Place(things.mines[MineMode.Patrol], p, mineParent); break;
                    case 'v': SetAxis(Place(things.mines[MineMode.Patrol], p, mineParent).GetComponent<Mine>(), Vector2.up); break;
                    case 's': Place(things.mines[MineMode.Wall], p, mineParent); break;
                    case 'M': Place(things.mines[MineMode.Heavy], p, mineParent); break;
                    case 'R': Place(things.tank, p, tankParent); Place(things.mineRing, p, mineParent); break;

                    // Gravity: planet, big and small black hole, small holes drifting sideways or up and down.
                    case '@': Place(things.planet, p, holeParent); break;
                    case 'O': Place(things.holeBig, p, holeParent); break;
                    case 'c': Place(things.holeSmall, p, holeParent); break;
                    case 'x': Place(things.holeDrift, p, holeParent); break;
                    case 'y': SetAxis(Place(things.holeDrift, p, holeParent).GetComponent<BlackHole>(), Vector2.up); break;
                }
            }
        }

        level.player = Place(things.player, start, root.transform).GetComponent<PlayerController>();
        level.ship = Place(things.ship, shipPos, root.transform).transform;

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
    static GameObject PlaceRock(Asteroid prefab, Vector2 position, Transform parent)
    {
        var go = Place(prefab.gameObject, position, parent);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        float s = Random.Range(tune.AsteroidSizeVariationMin, tune.AsteroidSizeVariationMax);
        go.transform.localScale = new Vector3(s, s, 1f);
        go.GetComponent<Asteroid>().PickRandomLook();
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        return go;
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go;
    }

    // Direction of a patrol mine or a drifting hole placed in a level.
    static void SetAxis(Object component, Vector2 axis)
    {
        var so = new SerializedObject(component);
        so.FindProperty("axis").vector2Value = axis;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    static void SetBool(Object component, string field, bool value)
    {
        var so = new SerializedObject(component);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    static Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    // The level edge: a wall of storm on all four sides, in the level's frame colour (red unless the
    // level file sets # frame=). Two layers of storm clouds flow along each side (StormEdge),
    // lightning flickers in the part on screen (StormBolts), and far out a plain dark storm colour.
    // Without the storm pictures the old thin frame is drawn. If you change Level.bounds, rebuild.
    static void BuildStorm(Transform parent, Rect bounds, Color color)
    {
        foreach (var f in new[] { "storm_body", "storm_glow", "storm_bolt1", "storm_bolt2", "storm_bolt3" })
        {
            string path = "Assets/_Resources/Art/" + f + ".png";
            if (File.Exists(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // apply ArtImportSettings
        }
        var body = Art.LoadExact("storm_body");
        var glow = Art.LoadExact("storm_glow");
        if (body == null || glow == null)
        {
            BuildBorder(parent, bounds, color);
            return;
        }

        var storm = Group("Storm", parent);
        Vector2 c = bounds.center;
        StormSide(storm, "Right", new Vector2(bounds.xMax, c.y), 0f, bounds.height, body, glow, color);
        StormSide(storm, "Top", new Vector2(c.x, bounds.yMax), 90f, bounds.width, body, glow, color);
        StormSide(storm, "Left", new Vector2(bounds.xMin, c.y), 180f, bounds.height, body, glow, color);
        StormSide(storm, "Bottom", new Vector2(c.x, bounds.yMin), 270f, bounds.width, body, glow, color);

        var shapes = new List<Sprite>();
        for (int i = 1; i <= 3; i++) { var s = Art.LoadExact("storm_bolt" + i); if (s != null) shapes.Add(s); }
        var pool = new SpriteRenderer[3];
        for (int i = 0; i < pool.Length; i++)
        {
            var go = new GameObject("Bolt");
            go.transform.SetParent(storm, false);
            pool[i] = go.AddComponent<SpriteRenderer>();
            pool[i].sortingOrder = -29;
            pool[i].enabled = false;
        }
        storm.gameObject.AddComponent<StormBolts>().Setup(bounds, pool, shapes.ToArray(),
            Color.Lerp(color, Color.white, 0.6f));
    }

    // One side of the storm: its +X points out of the level, the edge runs along its Y.
    static void StormSide(Transform storm, string name, Vector2 position, float angle, float length,
        Sprite body, Sprite glow, Color color)
    {
        const float tile = 32f, across = 24f, centre = 8f, glowScale = 1.3f;
        var side = Group(name, storm);
        side.position = position;
        side.rotation = Quaternion.Euler(0f, 0f, angle);

        // Far out: plain dark storm colour, so the ship never flies out of the storm into space.
        var deep = SpriteFactory.MakeSprite("Deep", side, SpriteFactory.Square(),
            new Color(color.r * 0.2f, color.g * 0.2f, color.b * 0.2f, 0.94f), new Vector2(200f, length + 400f), -32);
        deep.transform.localPosition = new Vector3(19f + 100f, 0f, 0f);

        var b = TiledSprite("Body", side, body, color, new Vector2(across, length + 2f * tile), -31, 1f);
        b.transform.localPosition = new Vector3(centre, 0f, 0f);
        var gl = TiledSprite("Glow", side, glow, new Color(Mathf.Lerp(color.r, 1f, 0.4f), Mathf.Lerp(color.g, 1f, 0.4f),
            Mathf.Lerp(color.b, 1f, 0.4f), 0.85f), new Vector2(across, (length + 2f * tile * glowScale) / glowScale), -30, glowScale);
        gl.transform.localPosition = new Vector3(centre * glowScale, 0f, 0f);

        var edge = side.gameObject.AddComponent<StormEdge>();
        var so = new SerializedObject(edge);
        so.FindProperty("body").objectReferenceValue = b;
        so.FindProperty("glow").objectReferenceValue = gl;
        so.FindProperty("tile").floatValue = tile;
        so.FindProperty("centre").floatValue = centre;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SpriteRenderer TiledSprite(string name, Transform parent, Sprite sprite, Color color, Vector2 size,
        int sortingOrder, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        return sr;
    }

    // Thin frame showing where the level ends: only used when the storm pictures are missing.
    static void BuildBorder(Transform parent, Rect bounds, Color frame)
    {
        var border = Group("Border", parent);
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

    // --- Scene ---

    static void SetupScene(Level[] levels, Bullet bullet, Blast blast)
    {
        // Remove what an earlier run made.
        foreach (var name in new[] { "DraftGame", "HUD" })
        {
            GameObject old;
            while ((old = GameObject.Find(name)) != null) Object.DestroyImmediate(old);
        }

        var light = EnsureGlobalLight();
        var rig = SetupCamera();
        var hud = BuildHud();

        var go = new GameObject("DraftGame");
        var game = go.AddComponent<DraftGame>();
        var so = new SerializedObject(game);
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("cameraRig").objectReferenceValue = rig;
        so.FindProperty("globalLight").objectReferenceValue = light;
        so.FindProperty("bulletPrefab").objectReferenceValue = bullet;
        so.FindProperty("blastPrefab").objectReferenceValue = blast;
        var list = so.FindProperty("levels");
        list.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        so.FindProperty("startLevel").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        TuningConnector.Connect(go, tune);
        TuningConnector.Connect(rig.gameObject, tune);
        TuningConnector.Connect(hud.gameObject, tune);
    }

    static Light2D EnsureGlobalLight()
    {
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return l;

        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;
        return light;
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

    // --- HUD: icons and numbers in the game's pixel font, no sentences ---

    static readonly Color AirColor = new Color(0.35f, 0.9f, 1f);
    static readonly Color LeakingColor = new Color(1f, 0.62f, 0.3f);
    static readonly Color GoodColor = new Color(0.45f, 1f, 0.55f);
    static readonly Color BadColor = new Color(1f, 0.35f, 0.3f);
    static readonly Color ComboColor = new Color(1f, 0.85f, 0.3f);
    static readonly Color EmptyCell = new Color(1f, 1f, 1f, 0.16f);
    static readonly Color Ink = new Color(0.05f, 0.07f, 0.14f);
    static readonly Vector2 TopLeft = new Vector2(0f, 1f), TopRight = new Vector2(1f, 1f),
        TopMiddle = new Vector2(0.5f, 1f), Middle = new Vector2(0.5f, 0.5f);

    const int AirCells = 20;

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
        Transform root = go.transform;

        var o2Icon = SpriteFactory.PixelIcon("o2", PixelFont.O2);
        var crackIcon = SpriteFactory.PixelIcon("crack", PixelFont.Crack);
        var arrowIcon = SpriteFactory.PixelIcon("arrow", PixelFont.Arrow);

        // Dark screen edges in the storm colour while the ship is in the storm beyond the level edge
        // (created first = drawn behind the rest).
        var vignette = MakeImage("StormVignette", root, new Color(0.2f, 0.05f, 0.05f, 0f));
        vignette.sprite = SpriteFactory.Vignette();
        Stretch(vignette.rectTransform);
        vignette.gameObject.SetActive(false);
        Ref(so, "stormVignette", vignette);

        // Red flash over the whole screen when the hull is breached.
        var flash = MakeImage("DamageFlash", root, new Color(1f, 0.1f, 0.05f, 0f));
        Stretch(flash.rectTransform);
        Ref(so, "damageFlash", flash);

        // Air, top-left: "O2", a bar cut into cells and a row of red cracks (one per leak).
        // The first quarter of the bar has reddish empty cells: below that line air is low.
        var o2 = MakeIcon("AirIcon", root, o2Icon, 5, Color.white, true);
        Place(o2.rectTransform, TopLeft, new Vector2(40f, -30f));
        Ref(so, "airIcon", o2);

        const float barX = 100f, cellW = 12f, cellH = 26f, gap = 3f;
        var cells = new Object[AirCells];
        for (int i = 0; i < AirCells; i++)
        {
            var cell = MakeImage("AirCell", root, AirColor);
            Place(cell.rectTransform, TopLeft, new Vector2(barX + i * (cellW + gap), -38f), new Vector2(cellW, cellH));
            AddShadow(cell.gameObject, 4);
            cells[i] = cell;
        }
        RefArray(so, "airCells", cells);

        var marks = new Object[tune.BreachMax];
        for (int i = 0; i < marks.Length; i++)
        {
            var mark = MakeIcon("Leak", root, crackIcon, 4, BadColor, true);
            Place(mark.rectTransform, TopLeft, new Vector2(barX + i * 28f, -74f));
            mark.gameObject.SetActive(false);
            marks[i] = mark.gameObject;
        }
        RefArray(so, "leakMarks", marks);

        // Combo, top-right, with a thin bar: the time left to continue it.
        var combo = new GameObject("Combo", typeof(RectTransform)).GetComponent<RectTransform>();
        combo.SetParent(root, false);
        Place(combo, TopRight, new Vector2(-40f, -32f), new Vector2(200f, 80f));
        var comboLabel = MakeLabel("Text", combo, "X2", 8, ComboColor, true);
        Place((RectTransform)comboLabel.transform, TopRight, Vector2.zero);
        var barBg = MakeImage("Bar", combo, EmptyCell);
        Place(barBg.rectTransform, new Vector2(1f, 0f), Vector2.zero, new Vector2(140f, 8f));
        var barFill = MakeImage("Fill", barBg.transform, ComboColor);
        Stretch(barFill.rectTransform);
        combo.gameObject.SetActive(false);
        Ref(so, "comboRoot", combo.gameObject);
        Ref(so, "comboLabel", comboLabel);
        Ref(so, "comboBar", barFill.rectTransform);

        // Arrow that slides along the screen edge and points at the ship, with the distance.
        var arrow = MakeIcon("ShipArrow", root, arrowIcon, 6, GoodColor, true);
        Place(arrow.rectTransform, Middle, new Vector2(0f, 460f));
        Ref(so, "shipArrow", arrow.rectTransform);
        var distance = MakeLabel("ShipDistance", root, "124", 4, Color.white, true);
        Place((RectTransform)distance.transform, Middle, new Vector2(0f, 402f));
        Ref(so, "shipDistance", distance);

        // Blinking line while outside the level.

        // Level name, shown for a few seconds at the start of a level.
        var title = MakeLabel("Title", root, "1/3  ROCKS", 6, Color.white, true);
        Place((RectTransform)title.transform, TopMiddle, new Vector2(0f, -190f));
        title.gameObject.SetActive(false);
        Ref(so, "title", title);

        // Short notice in the upper part of the screen (TOO FAST).
        var toast = MakeLabel("Toast", root, Texts.TooFast, 5, LeakingColor, true);
        Place((RectTransform)toast.transform, Middle, new Vector2(0f, 230f));
        toast.gameObject.SetActive(false);
        Ref(so, "toast", toast);

        BuildThreatMarkers(go, arrowIcon);
        BuildEndScreen(so, root, o2Icon);

        so.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    // Danger markers at the screen edge (ThreatMarkers): a pool of six. Each one is a soft glow,
    // a pixel icon of the threat (set while playing) and an arrow pointing into the screen.
    static void BuildThreatMarkers(GameObject hud, Sprite arrowIcon)
    {
        const int count = 6;
        var threats = hud.AddComponent<ThreatMarkers>();
        var meteorIcon = SpriteFactory.PixelIcon("meteor", PixelFont.MeteorSign);
        var mineIcon = SpriteFactory.PixelIcon("mine", PixelFont.MineSign);

        var group = new GameObject("DangerMarkers", typeof(RectTransform)).GetComponent<RectTransform>();
        group.SetParent(hud.transform, false);
        Stretch(group);

        var roots = new Object[count];
        var glows = new Object[count];
        var icons = new Object[count];
        var arrows = new Object[count];
        for (int i = 0; i < count; i++)
        {
            var m = new GameObject("Danger", typeof(RectTransform)).GetComponent<RectTransform>();
            m.SetParent(group, false);
            Place(m, Middle, Vector2.zero, Vector2.zero);

            var glow = MakeImage("Glow", m, new Color(1f, 0.3f, 0.25f, 0.3f));
            glow.sprite = SpriteFactory.Glow();
            Place(glow.rectTransform, Middle, Vector2.zero, new Vector2(130f, 130f));
            var icon = MakeIcon("Icon", m, mineIcon, 4, Color.white, true);
            Place(icon.rectTransform, Middle, Vector2.zero);
            var arrow = MakeIcon("Arrow", m, arrowIcon, 4, Color.white, true);
            Place(arrow.rectTransform, Middle, Vector2.zero);

            m.gameObject.SetActive(false);
            roots[i] = m; glows[i] = glow; icons[i] = icon; arrows[i] = arrow;
        }

        var so = new SerializedObject(threats);
        RefArray(so, "markers", roots);
        RefArray(so, "glows", glows);
        RefArray(so, "icons", icons);
        RefArray(so, "arrows", arrows);
        Ref(so, "meteorIcon", meteorIcon);
        Ref(so, "mineIcon", mineIcon);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // End of a level: dark veil, big word, air left (after docking) and the key to press.
    static void BuildEndScreen(SerializedObject so, Transform root, Sprite o2Icon)
    {
        var veil = MakeImage("EndScreen", root, new Color(0f, 0f, 0.03f, 0.55f));
        Stretch(veil.rectTransform);
        Transform t = veil.transform;
        Ref(so, "endRoot", veil.gameObject);

        var counter = MakeLabel("Counter", t, "1/3", 4, new Color(1f, 1f, 1f, 0.7f), true);
        Place((RectTransform)counter.transform, Middle, new Vector2(0f, 150f));
        Ref(so, "endCounter", counter);

        var title = MakeLabel("Title", t, Texts.Docked, 12, GoodColor, true);
        Place((RectTransform)title.transform, Middle, new Vector2(0f, 60f));
        Ref(so, "endTitle", title);

        var row = new GameObject("Air", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(t, false);
        Place(row, Middle, new Vector2(0f, -50f), Vector2.zero);
        Ref(so, "endAirRow", row.gameObject);
        var icon = MakeIcon("Icon", row, o2Icon, 5, AirColor, true);
        Place(icon.rectTransform, Middle, new Vector2(-50f, 0f));
        Ref(so, "endAirIcon", icon.rectTransform);
        var air = MakeLabel("Number", row, "43", 6, Color.white, true);
        Place((RectTransform)air.transform, Middle, new Vector2(30f, 0f));
        Ref(so, "endAir", air);

        // The key to press, drawn as a keycap.
        var key = MakeImage("Key", t, new Color(1f, 1f, 1f, 0.92f));
        var keyLabel = MakeLabel("Text", key.transform, Texts.KeyNext, 4, Ink, false);
        Place((RectTransform)keyLabel.transform, Middle, Vector2.zero);
        Place(key.rectTransform, Middle, new Vector2(0f, -150f), new Vector2(keyLabel.Width + 36f, 52f));
        Ref(so, "endKey", key.rectTransform);
        Ref(so, "endKeyLabel", keyLabel);

        veil.gameObject.SetActive(false);
    }

    // One line in the pixel font. Change the text, scale or colour in the Inspector.
    static PixelLabel MakeLabel(string name, Transform parent, string text, int scale, Color color, bool shadow)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<RawImage>();
        img.color = color;
        img.raycastTarget = false;
        if (shadow) AddShadow(go, scale);
        var label = go.AddComponent<PixelLabel>();
        label.Setup(text, scale);
        return label;
    }

    // A pixel icon shown `scale` times bigger than drawn.
    static Image MakeIcon(string name, Transform parent, Sprite sprite, int scale, Color color, bool shadow)
    {
        var img = MakeImage(name, parent, color);
        img.sprite = sprite;
        if (sprite != null) img.rectTransform.sizeDelta = sprite.rect.size * scale;
        if (shadow) AddShadow(img.gameObject, scale);
        return img;
    }

    // Hard dark shadow, half a font pixel down and right, so things read on any background.
    static void AddShadow(GameObject go, int scale)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0.05f, 0.7f);
        s.effectDistance = new Vector2(scale * 0.5f, -scale * 0.5f);
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

    // Pins the rect to one point of the parent; pivot matches the anchor. Size is left alone.
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = position;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Place(rt, anchor, position);
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

    static PhysicsMaterial2D PhysicsMaterial(string name, float bounciness)
    {
        string path = PhysicsFolder + "/" + name + ".physicsMaterial2D";
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (m == null)
        {
            m = new PhysicsMaterial2D(name);
            AssetDatabase.CreateAsset(m, path);
        }
        m.friction = 0f;
        m.bounciness = bounciness;
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

    static void RefArray(SerializedObject so, string field, Object[] values)
    {
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError("DraftSetup: field '" + field + "' not found on " + so.targetObject.GetType().Name);
            return;
        }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
