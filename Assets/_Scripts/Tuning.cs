using UnityEngine;

// All balance numbers of the game, in one asset: Assets/_Settings/Tuning.asset.
// Select the asset and change values in the Inspector (also during Play: changes apply
// at once, but values changed in Play mode stay changed, since this is an asset).
// Every component that uses these numbers has a "Tuning" field pointing to the asset;
// Filip > Connect Tuning To Prefabs And Scene fills those fields everywhere.
//
// Look of the game: only things that matter for play glow (player, bullets,
// oxygen tanks, meteors). Rocks are big, light and warm. The background is calm.
[CreateAssetMenu(fileName = "Tuning", menuName = "Game/Tuning")]
public class Tuning : ScriptableObject
{
    public const string AssetPath = "Assets/_Settings/Tuning.asset";

    // Used by components whose Tuning field is empty. Set by DraftGame from its own field.
    public static Tuning Active;

    public static void ResetStatics()
    {
        Active = null;
    }

#if UNITY_EDITOR
    // Editor only: the Tuning asset of the project (the first one found), or null.
    public static Tuning FindInEditor()
    {
        var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Tuning>(AssetPath);
        if (t != null) return t;
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Tuning"))
        {
            t = UnityEditor.AssetDatabase.LoadAssetAtPath<Tuning>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (t != null) return t;
        }
        return null;
    }
#endif

    [Header("Shooting / recoil")]
    [Tooltip("impulse pushed into the player per shot")]
    public float RecoilImpulse = 3f;
    [Tooltip("units per second")]
    public float BulletSpeed = 20f;
    [Tooltip("seconds before a bullet disappears")]
    public float BulletLifetime = 2f;
    [Tooltip("shots per second while holding the button")]
    public float FireRate = 4f;
    public float BulletDiameter = 0.3f;
    public float BulletMass = 0.05f;
    [Tooltip("yellow glow around a bullet, units")]
    public float BulletGlowSize = 0.9f;
    public float BulletGlowOpacity = 0.8f;

    [Header("Player")]
    public float PlayerMaxSpeed = 12f;
    public float PlayerDiameter = 1f;
    [Tooltip("side of the player's ship picture (player_ship.png); its round body is about one PlayerDiameter")]
    public float PlayerShipSize = 2.2f;
    public float PlayerMass = 1f;
    [Tooltip("white glow under the player, in player diameters")]
    public float PlayerGlowSize = 3f;
    public float PlayerGlowOpacity = 0.35f;

    [Header("Camera")]
    [Tooltip("orthographic size (half of visible height): about 35.5 x 20 units on screen")]
    public float CameraSize = 10f;
    [Tooltip("higher = camera catches up faster")]
    public float CameraFollowSharpness = 5f;
    [Tooltip("seconds")]
    public float CameraShakeTime = 0.08f;
    [Tooltip("units")]
    public float CameraShakeStrength = 0.19f;
    [Tooltip("camera shake when the hull is breached, seconds")]
    public float CameraHardShakeTime = 0.3f;
    [Tooltip("times the usual shake")]
    public float CameraHardShakeStrength = 2.5f;

    [Header("Oxygen")]
    public float OxygenMax = 100f;
    [Tooltip("was 1.5: air now also comes from destroyed rocks")]
    public float OxygenDrainPerSecond = 2f;
    public float OxygenPerShot = 1f;
    [Tooltip("oxygen drains this much faster outside the level (in the storm)")]
    public float OutOfBoundsDrainMultiplier = 4f;

    [Header("Storm at the level edge")]
    [Tooltip("how hard the storm pushes the ship back into the level, units per second squared (at full depth)")]
    public float StormPush = 8f;
    [Tooltip("the push grows until the ship is this deep in the storm, units")]
    public float StormPushDepth = 6f;
    [Tooltip("the screen edges are fully dark this deep in the storm, units")]
    public float StormDarkDepth = 6f;

    [Header("Levels from text files (Assets/_Resources/Levels)")]
    [Tooltip("one map character is a square of this many units")]
    public float CellSize = 2f;

    [Header("Docking: reach the ship slowly")]
    [Tooltip("faster than this the player bounces off")]
    public float DockMaxSpeed = 5f;
    [Tooltip("share of speed kept after bouncing off")]
    public float DockBounce = 0.6f;
    public float DockBounceMinSpeed = 3f;
    [Tooltip("oxygen lost for hitting the ship too fast")]
    public float DockPenalty = 5f;

    [Header("Test ground (only used when there are no level files)")]
    public float LevelWidth = 60f;
    public float LevelHeight = 120f;
    [Tooltip("player starts this far above the bottom border")]
    public float PlayerStartFromBottom = 5f;
    [Tooltip("ship center is this far below the top border")]
    public float ShipFromTop = 3f;

    [Header("Sizes in the world, in units (the larger side of the picture)")]
    public float ShipLength = 10f;
    [Tooltip("placeholder only; real art keeps its own proportions")]
    public float ShipHeight = 2.5f;
    [Tooltip("gap between the goal ship and its docking ring")]
    public float DockRingMargin = 1.2f;
    [Tooltip("the docking ring starts to appear at this distance")]
    public float DockRingShowDistance = 22f;
    [Tooltip("1 level cell")]
    public float AsteroidSmallSize = 2f;
    [Tooltip("2x2 level cells")]
    public float AsteroidMediumSize = 4f;
    [Tooltip("3x3 level cells")]
    public float AsteroidLargeSize = 6f;
    [Tooltip("every rock gets a random size multiplier")]
    public float AsteroidSizeVariationMin = 0.88f;
    public float AsteroidSizeVariationMax = 1.12f;
    public float OxygenTankSize = 0.8f;
    [Tooltip("side of the planet picture")]
    public float PlanetSize = 12f;
    [Tooltip("the solid ball is this share of the picture (its radius)")]
    public float PlanetBodyShare = 0.44f;
    public float BackgroundSize = 48f;

    [Header("Background: calm, so it does not compete with the rocks")]
    // Parallax: how fast a layer scrolls on screen compared to the world (1 = like the world).
    [Tooltip("artist picture 'background' (does not repeat)")]
    public float BackgroundParallax = 0.1f;
    [Tooltip("big far object of a level (sun, wreck, planet)")]
    public float BackdropParallax = 0.12f;
    [Tooltip("far clouds (repeat endlessly)")]
    public float NebulaFarParallax = 0.25f;
    [Tooltip("near clouds: faster, so the picture keeps changing")]
    public float NebulaNearParallax = 0.4f;
    [Tooltip("one copy of the far cloud picture, in units")]
    public float NebulaFarSize = 64f;
    public float NebulaNearSize = 80f;
    [Tooltip("1 = as drawn, lower = darker")]
    public float NebulaFarBrightness = 0.6f;
    [Tooltip("1 = as drawn, lower = more transparent")]
    public float NebulaNearOpacity = 0.4f;
    public float StarsFarParallax = 0.3f;
    public float StarsNearParallax = 0.45f;
    public float StarDustParallax = 0.2f;
    [Tooltip("stars repeat every this many units; must be wider than the screen")]
    public float StarTileSize = 56f;
    // Counts are per repeating tile (56x56 units); the screen shows a bit less than a quarter of it.
    [Tooltip("tiny dim dots")]
    public int StarDustCount = 420;
    [Tooltip("small stars")]
    public int StarsFarCount = 80;
    [Tooltip("a few bigger stars")]
    public int StarsNearCount = 14;
    [Tooltip("size of the star shape, units (the glow is wider)")]
    public float StarFarSizeMin = 0.08f;
    public float StarFarSizeMax = 0.18f;
    public float StarNearSizeMin = 0.18f;
    public float StarNearSizeMax = 0.34f;
    [Tooltip("star cores: 1 = full white")]
    public float StarBrightness = 0.75f;
    [Tooltip("coloured halo around stars")]
    public float StarGlowOpacity = 0.5f;
    [Tooltip("0 = steady light, 1 = strong pulsing")]
    public float StarTwinkleStrength = 0f;
    [Tooltip("how fast stars pulse")]
    public float StarTwinkleSpeedMin = 0.6f;
    public float StarTwinkleSpeedMax = 2.2f;

    [Header("Asteroids")]
    [Tooltip("hits to destroy")]
    public int AsteroidHpSmall = 1;
    public int AsteroidHpMedium = 2;
    [Tooltip("a large one splits into two medium ones")]
    public int AsteroidHpLarge = 4;
    [Tooltip("mass = density * size * size (the player weighs 1)")]
    public float AsteroidDensity = 0.5f;
    [Tooltip("slow asteroids: drift speed, units per second")]
    public float AsteroidDriftMin = 0.2f;
    public float AsteroidDriftMax = 0.6f;
    [Tooltip("share of asteroids that fly noticeably (0..1)")]
    public float AsteroidMoverChance = 0.5f;
    [Tooltip("moving asteroids: speed, random direction")]
    public float AsteroidMoveSpeedMin = 1f;
    public float AsteroidMoveSpeedMax = 3f;
    [Tooltip("max rotation speed, degrees per second")]
    public float AsteroidSpin = 40f;
    [Tooltip("how fast the two halves fly apart")]
    public float AsteroidSplitSpeed = 1.5f;
    [Tooltip("0 = no bounce, 1 = full bounce")]
    public float Bounciness = 0.5f;

    [Header("Meteors: fast asteroids flying from the top down at the player")]
    [Tooltip("seconds before the first one")]
    public float MeteorFirstDelay = 4f;
    [Tooltip("seconds between meteors")]
    public float MeteorIntervalMin = 3f;
    public float MeteorIntervalMax = 6f;
    public float MeteorSpeedMin = 13f;
    public float MeteorSpeedMax = 17f;
    [Tooltip("aims this many seconds ahead of where the player is flying")]
    public float MeteorLead = 0.9f;
    [Tooltip("chance that a whole shower comes instead of one meteor")]
    public float MeteorShowerChance = 0.3f;
    public int MeteorShowerCount = 3;
    [Tooltip("max sideways speed")]
    public float MeteorSideSpeed = 1.5f;
    [Tooltip("appears this far left/right of the player at most")]
    public float MeteorSpread = 8f;
    [Tooltip("appears this far above the top of the screen")]
    public float MeteorSpawnAhead = 22f;
    [Tooltip("seconds")]
    public float MeteorLifetime = 20f;
    [Tooltip("share of medium meteors; the rest are small")]
    public float MeteorMediumChance = 0.25f;
    [Tooltip("orange glow, in rock diameters")]
    public float MeteorHeatSize = 3f;
    public float MeteorHeatOpacity = 0.75f;
    [Tooltip("fire tail (fire.png): side of the picture, in rock diameters")]
    public float MeteorFireSize = 3.6f;
    [Tooltip("frames per second when fire.png is a sliced sprite sheet")]
    public float MeteorFireFps = 12f;

    [Header("Oxygen tanks")]
    public float OxygenTankAmount = 30f;
    public float OxygenTankPickupRadius = 0.7f;
    [Tooltip("cyan glow, units")]
    public float OxygenTankGlowSize = 2f;
    public float OxygenTankGlowOpacity = 0.6f;

    [Header("Hull breaches: no lives. A hard hit opens a leak, every leak drains oxygen")]
    [Tooltip("more hits than this do not add leaks")]
    public int BreachMax = 5;
    [Tooltip("extra oxygen lost per second for every leak")]
    public float BreachDrainPerSecond = 1f;
    [Tooltip("hitting a rock or a meteor faster than this breaches the hull")]
    public float BreachImpactSpeed = 7f;
    [Tooltip("how dense the jet of air looks")]
    public float LeakPuffsPerSecond = 22f;
    public float LeakJetSpeed = 5f;
    [Tooltip("seconds")]
    public float LeakPuffLifetime = 0.5f;

    [Header("Gravity: a planet pulls the player and the bullets")]
    [Tooltip("the field ends here (dotted circle)")]
    public float GravityRadius = 22f;
    [Tooltip("pull = this / distance squared")]
    public float GravityStrength = 288f;
    [Tooltip("the pull never gets stronger than this")]
    public float GravityMaxAccel = 9f;

    [Header("Mines")]
    public float MineSize = 1.1f;
    public float MineMass = 0.8f;
    [Tooltip("a hunter wakes up when the player is this close")]
    public float MineWakeRadius = 16f;
    [Tooltip("and gives up when the player is this far")]
    public float MineLoseRadius = 26f;
    [Tooltip("the top speed of the player is 12, docking speed is 5")]
    public float MineSpeed = 5.5f;
    [Tooltip("how quickly it picks up speed")]
    public float MineAccel = 12f;
    [Tooltip("a blast this close to the player breaches the hull")]
    public float MineBreachRadius = 2.2f;
    [Tooltip("everything this close is pushed away")]
    public float MineBlastRadius = 4.5f;
    [Tooltip("how hard")]
    public float MineBlastImpulse = 10f;
    [Tooltip("rocks this close to the blast take one hit")]
    public float MineRockRadius = 2.5f;
    [Tooltip("a rock hitting a mine faster than this sets it off")]
    public float MineTriggerSpeed = 5f;
    [Tooltip("seconds before a neighbouring mine goes off too")]
    public float MineChainDelay = 0.12f;
    [Tooltip("a mine shot by the player leaves crystals")]
    public int CrystalsFromMine = 4;
    [Tooltip("a mine set off by a chain or a rock leaves fewer")]
    public int CrystalsFromChainedMine = 2;

    [Header("Mine behaviours")]
    [Tooltip("a patrol mine glides this far to each side of its place")]
    public float MinePatrolRange = 6f;
    public float MinePatrolSpeed = 3.5f;
    [Tooltip("mines circling a guarded tank; far enough apart not to set each other off")]
    public int MineRingCount = 5;
    public float MineRingRadius = 6f;
    [Tooltip("units per second along the circle")]
    public float MineRingSpeed = 3f;
    [Tooltip("heavy mine: big, takes several hits, wipes out everything around")]
    public float MineHeavySize = 2.4f;
    public int MineHeavyHits = 3;
    [Tooltip("its blast and breach radius are this many times bigger")]
    public float MineHeavyBlastScale = 2f;

    [Header("Black holes: they pull the player, bullets and crystals")]
    [Tooltip("the dark middle of a big hole")]
    public float HoleBigRadius = 2.2f;
    [Tooltip("its pull reaches this far")]
    public float HoleBigField = 20f;
    [Tooltip("pull = this / distance squared")]
    public float HoleBigStrength = 520f;
    public float HoleSmallRadius = 1.1f;
    public float HoleSmallField = 11f;
    public float HoleSmallStrength = 200f;
    [Tooltip("the pull never gets stronger than this")]
    public float HoleMaxAccel = 14f;
    [Tooltip("crystals feel this share of the pull")]
    public float HoleCrystalPull = 0.7f;
    [Tooltip("a drifting hole glides this far to each side")]
    public float HoleDriftRange = 8f;
    public float HoleDriftSpeed = 3f;
    [Tooltip("rocks circling a big hole (they are the air supply of that level)")]
    public int HoleOrbitRocks = 6;
    [Tooltip("rocks circling a small still hole")]
    public int HoleSmallOrbitRocks = 2;
    [Tooltip("bullets feel the pull this many times stronger, so their arc is easy to see")]
    public float HoleBulletPull = 2.5f;
    [Tooltip("orbiting rocks: units per second along the circle")]
    public float HoleOrbitSpeed = 3f;
    [Tooltip("degrees per second: how fast the picture turns")]
    public float HoleSpinSpeed = 25f;
    [Tooltip("a hole does not kill: it throws the player out of another hole at this speed")]
    public float WormholeExitSpeed = 10f;
    [Tooltip("air lost on the way (plus one hull breach)")]
    public float WormholeOxygenCost = 10f;
    [Tooltip("seconds after coming out during which holes cannot swallow again")]
    public float WormholeImmunity = 2f;

    [Header("Oxygen crystals: destroyed rocks leave air behind ('destroy to breathe')")]
    [Tooltip("oxygen per crystal (before the combo bonus)")]
    public float CrystalOxygen = 2.8f;
    [Tooltip("crystals left by a destroyed rock")]
    public int CrystalsFromSmall = 2;
    public int CrystalsFromMedium = 3;
    public int CrystalsFromLarge = 4;
    [Tooltip("seconds before a crystal disappears")]
    public float CrystalLifetime = 3.5f;
    [Tooltip("it blinks for this long before disappearing")]
    public float CrystalBlinkTime = 1f;
    public float CrystalSize = 0.35f;
    public float CrystalPickupRadius = 0.9f;
    [Tooltip("closer than this, crystals fly to the player")]
    public float CrystalMagnetRadius = 3.5f;
    public float CrystalMagnetAccel = 40f;
    [Tooltip("how fast crystals scatter from the rock")]
    public float CrystalBurstSpeedMin = 1.5f;
    public float CrystalBurstSpeedMax = 4f;
    [Tooltip("how quickly they slow down")]
    public float CrystalDamping = 2.5f;

    [Header("Combo: destroying things in a row makes every crystal worth more")]
    [Tooltip("seconds to destroy the next thing")]
    public float ComboWindow = 3f;
    public int ComboMax = 5;
    [Tooltip("x2 gives +50% oxygen per crystal, x3 +100%, ...")]
    public float ComboBonusPerStep = 0.5f;

    [Header("Temporary scatter on the test ground (until levels come from text files)")]
    public int ScatterAsteroids = 24;
    public int ScatterTanks = 3;
    [Tooltip("asteroids lie this far left/right of the straight path")]
    public float ScatterHalfWidth = 11f;
    [Tooltip("free space around the player start and the ship")]
    public float ScatterClearance = 10f;
    [Tooltip("tanks lie this far off the straight path")]
    public float TankSideOffsetMin = 15f;
    public float TankSideOffsetMax = 20f;
}
