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

    [Header("Oxygen")]
    public float OxygenMax = 100f;
    [Tooltip("was 1.5: air now also comes from destroyed rocks")]
    public float OxygenDrainPerSecond = 2f;
    public float OxygenPerShot = 1f;
    [Tooltip("oxygen drains this much faster outside the level")]
    public float OutOfBoundsDrainMultiplier = 4f;

    [Header("Levels from text files (Assets/_Resources/Levels)")]
    [Tooltip("one map character is a square of this many units")]
    public float CellSize = 2f;
    [Tooltip("a hint is shown while the player is this close to its place")]
    public float HintRadius = 9f;

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
    public float ShipLength = 6f;
    [Tooltip("placeholder only; real art keeps its own proportions")]
    public float ShipHeight = 2.5f;
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
    public float PlanetSize = 8f;
    public float BackgroundSize = 48f;

    [Header("Background: calm, so it does not compete with the rocks")]
    // Parallax: how fast a layer scrolls on screen compared to the world (1 = like the world).
    [Tooltip("artist picture 'background' (does not repeat)")]
    public float BackgroundParallax = 0.1f;
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
    public float MeteorSpeedMin = 7f;
    public float MeteorSpeedMax = 10f;
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
    [Tooltip("red marker at the top edge of the screen")]
    public float MeteorWarningSize = 0.9f;

    [Header("Impact: hitting an asteroid faster than the safe speed costs oxygen")]
    public float ImpactSafeSpeed = 6f;
    [Tooltip("oxygen lost = (speed - safe speed) * this")]
    public float ImpactOxygenPerSpeed = 3f;
    [Tooltip("one hit never costs more than this")]
    public float ImpactMaxOxygen = 25f;

    [Header("Oxygen tanks")]
    public float OxygenTankAmount = 30f;
    public float OxygenTankPickupRadius = 0.7f;
    [Tooltip("cyan glow, units")]
    public float OxygenTankGlowSize = 2f;
    public float OxygenTankGlowOpacity = 0.6f;

    [Header("Oxygen crystals: destroyed rocks leave air behind ('destroy to breathe')")]
    [Tooltip("oxygen per crystal (before the combo bonus)")]
    public float CrystalOxygen = 2f;
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
