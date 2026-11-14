// All balance numbers live here. Change values, press Play, test.
//
// Look of the game: only things that matter for play glow (player, bullets,
// oxygen tanks, meteors). Rocks are big, light and warm. The background is calm.
public static class Tuning
{
    // --- Shooting / recoil ---
    public const float RecoilImpulse = 3f;        // impulse pushed into the player per shot
    public const float BulletSpeed = 20f;         // units per second
    public const float BulletLifetime = 2f;       // seconds before a bullet disappears
    public const float FireRate = 4f;             // shots per second while holding the button
    public const float BulletDiameter = 0.3f;
    public const float BulletMass = 0.05f;
    public const float BulletGlowSize = 0.9f;     // yellow glow around a bullet, units
    public const float BulletGlowOpacity = 0.8f;

    // --- Player ---
    public const float PlayerMaxSpeed = 12f;
    public const float PlayerDiameter = 1f;
    public const float PlayerMass = 1f;
    public const float PlayerGlowSize = 3f;       // white glow under the player, in player diameters
    public const float PlayerGlowOpacity = 0.35f;

    // --- Camera ---
    public const float CameraSize = 10f;          // orthographic size (half of visible height): about 35.5 x 20 units on screen
    public const float CameraFollowSharpness = 5f; // higher = camera catches up faster
    public const float CameraShakeTime = 0.08f;   // seconds
    public const float CameraShakeStrength = 0.19f; // units

    // --- Oxygen ---
    public const float OxygenMax = 100f;
    public const float OxygenDrainPerSecond = 2f; // was 1.5: air now also comes from destroyed rocks
    public const float OxygenPerShot = 1f;
    public const float OutOfBoundsDrainMultiplier = 4f; // oxygen drains this much faster outside the level

    // --- Levels from text files (Assets/Resources/Levels) ---
    public const float CellSize = 2f;             // one map character is a square of this many units
    public const float HintRadius = 9f;           // a hint is shown while the player is this close to its place

    // --- Docking: reach the ship slowly ---
    public const float DockMaxSpeed = 5f;         // faster than this the player bounces off
    public const float DockBounce = 0.6f;         // share of speed kept after bouncing off
    public const float DockBounceMinSpeed = 3f;
    public const float DockPenalty = 5f;          // oxygen lost for hitting the ship too fast

    // --- Test ground (only used when there are no level files) ---
    public const float LevelWidth = 60f;
    public const float LevelHeight = 120f;
    public const float PlayerStartFromBottom = 5f; // player starts this far above the bottom border
    public const float ShipFromTop = 3f;           // ship center is this far below the top border

    // --- Sizes in the world, in units (the larger side of the picture) ---
    public const float ShipLength = 6f;
    public const float ShipHeight = 2.5f;         // placeholder only; real art keeps its own proportions
    public const float AsteroidSmallSize = 2f;    // 1 level cell
    public const float AsteroidMediumSize = 4f;   // 2x2 level cells
    public const float AsteroidLargeSize = 6f;    // 3x3 level cells
    public const float AsteroidSizeVariationMin = 0.88f; // every rock gets a random size multiplier
    public const float AsteroidSizeVariationMax = 1.12f;
    public const float OxygenTankSize = 0.8f;
    public const float PlanetSize = 8f;
    public const float BackgroundSize = 48f;

    // --- Background: calm, so it does not compete with the rocks ---
    // Parallax: how fast a layer scrolls on screen compared to the world (1 = like the world).
    public const float BackgroundParallax = 0.1f; // artist picture "background" (does not repeat)
    public const float BackdropParallax = 0.12f;  // big far object of a level (sun, wreck, planet)
    public const float NebulaFarParallax = 0.25f; // far clouds (repeat endlessly)
    public const float NebulaNearParallax = 0.4f; // near clouds: faster, so the picture keeps changing
    public const float NebulaFarSize = 64f;       // one copy of the far cloud picture, in units
    public const float NebulaNearSize = 80f;
    public const float NebulaFarBrightness = 0.6f; // 1 = as drawn, lower = darker
    public const float NebulaNearOpacity = 0.4f;  // 1 = as drawn, lower = more transparent
    public const float StarsFarParallax = 0.3f;
    public const float StarsNearParallax = 0.45f;
    public const float StarDustParallax = 0.2f;
    public const float StarTileSize = 56f;        // stars repeat every this many units; must be wider than the screen
    // Counts are per repeating tile (56x56 units); the screen shows a bit less than a quarter of it.
    public const int StarDustCount = 420;         // tiny dim dots
    public const int StarsFarCount = 80;          // small stars
    public const int StarsNearCount = 14;         // a few bigger stars
    public const float StarFarSizeMin = 0.08f;    // size of the star shape, units (the glow is wider)
    public const float StarFarSizeMax = 0.18f;
    public const float StarNearSizeMin = 0.18f;
    public const float StarNearSizeMax = 0.34f;
    public const float StarBrightness = 0.75f;    // star cores: 1 = full white
    public const float StarGlowOpacity = 0.5f;    // coloured halo around stars
    public const float StarTwinkleStrength = 0f;  // 0 = steady light, 1 = strong pulsing
    public const float StarTwinkleSpeedMin = 0.6f; // how fast stars pulse
    public const float StarTwinkleSpeedMax = 2.2f;

    // --- Asteroids ---
    public const int AsteroidHpSmall = 1;         // hits to destroy
    public const int AsteroidHpMedium = 2;
    public const int AsteroidHpLarge = 4;         // a large one splits into two medium ones
    public const float AsteroidDensity = 0.5f;    // mass = density * size * size (the player weighs 1)
    public const float AsteroidDriftMin = 0.2f;   // slow asteroids: drift speed, units per second
    public const float AsteroidDriftMax = 0.6f;
    public const float AsteroidMoverChance = 0.5f; // share of asteroids that fly noticeably (0..1)
    public const float AsteroidMoveSpeedMin = 1f;  // moving asteroids: speed, random direction
    public const float AsteroidMoveSpeedMax = 3f;
    public const float AsteroidSpin = 40f;        // max rotation speed, degrees per second
    public const float AsteroidSplitSpeed = 1.5f; // how fast the two halves fly apart
    public const float Bounciness = 0.5f;         // 0 = no bounce, 1 = full bounce

    // --- Meteors: fast asteroids flying from the top down at the player ---
    public const float MeteorFirstDelay = 4f;     // seconds before the first one
    public const float MeteorIntervalMin = 3f;    // seconds between meteors
    public const float MeteorIntervalMax = 6f;
    public const float MeteorSpeedMin = 7f;
    public const float MeteorSpeedMax = 10f;
    public const float MeteorSideSpeed = 1.5f;    // max sideways speed
    public const float MeteorSpread = 8f;         // appears this far left/right of the player at most
    public const float MeteorSpawnAhead = 22f;    // appears this far above the top of the screen
    public const float MeteorLifetime = 20f;      // seconds
    public const float MeteorMediumChance = 0.25f; // share of medium meteors; the rest are small
    public const float MeteorHeatSize = 3f;       // orange glow, in rock diameters
    public const float MeteorHeatOpacity = 0.75f;
    public const float MeteorWarningSize = 0.9f;  // red marker at the top edge of the screen

    // --- Impact: hitting an asteroid faster than the safe speed costs oxygen ---
    public const float ImpactSafeSpeed = 6f;
    public const float ImpactOxygenPerSpeed = 3f; // oxygen lost = (speed - safe speed) * this
    public const float ImpactMaxOxygen = 25f;     // one hit never costs more than this

    // --- Oxygen tanks ---
    public const float OxygenTankAmount = 30f;
    public const float OxygenTankPickupRadius = 0.7f;
    public const float OxygenTankGlowSize = 2f;   // cyan glow, units
    public const float OxygenTankGlowOpacity = 0.6f;

    // --- Oxygen crystals: destroyed rocks leave air behind ("destroy to breathe") ---
    public const float CrystalOxygen = 2f;        // oxygen per crystal (before the combo bonus)
    public const int CrystalsFromSmall = 2;       // crystals left by a destroyed rock
    public const int CrystalsFromMedium = 3;
    public const int CrystalsFromLarge = 4;
    public const float CrystalLifetime = 3.5f;    // seconds before a crystal disappears
    public const float CrystalBlinkTime = 1f;     // it blinks for this long before disappearing
    public const float CrystalSize = 0.35f;
    public const float CrystalPickupRadius = 0.9f;
    public const float CrystalMagnetRadius = 3.5f; // closer than this, crystals fly to the player
    public const float CrystalMagnetAccel = 40f;
    public const float CrystalBurstSpeedMin = 1.5f; // how fast crystals scatter from the rock
    public const float CrystalBurstSpeedMax = 4f;
    public const float CrystalDamping = 2.5f;     // how quickly they slow down

    // --- Combo: destroying things in a row makes every crystal worth more ---
    public const float ComboWindow = 3f;          // seconds to destroy the next thing
    public const int ComboMax = 5;
    public const float ComboBonusPerStep = 0.5f;  // x2 gives +50% oxygen per crystal, x3 +100%, ...

    // --- Temporary scatter on the test ground (until levels come from text files) ---
    public const int ScatterAsteroids = 24;
    public const int ScatterTanks = 3;
    public const float ScatterHalfWidth = 11f;    // asteroids lie this far left/right of the straight path
    public const float ScatterClearance = 10f;    // free space around the player start and the ship
    public const float TankSideOffsetMin = 15f;   // tanks lie this far off the straight path
    public const float TankSideOffsetMax = 20f;
}
