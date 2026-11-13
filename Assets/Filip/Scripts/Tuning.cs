// All balance numbers live here. Change values, press Play, test.
public static class Tuning
{
    // --- Shooting / recoil ---
    public const float RecoilImpulse = 3f;        // impulse pushed into the player per shot
    public const float BulletSpeed = 20f;         // units per second
    public const float BulletLifetime = 2f;       // seconds before a bullet disappears
    public const float FireRate = 4f;             // shots per second while holding the button
    public const float BulletDiameter = 0.2f;
    public const float BulletMass = 0.05f;

    // --- Player ---
    public const float PlayerMaxSpeed = 12f;
    public const float PlayerDiameter = 1f;
    public const float PlayerMass = 1f;

    // --- Camera ---
    public const float CameraSize = 8f;           // orthographic size (half of visible height)
    public const float CameraFollowSharpness = 5f; // higher = camera catches up faster
    public const float CameraShakeTime = 0.08f;   // seconds
    public const float CameraShakeStrength = 0.15f; // units

    // --- Oxygen ---
    public const float OxygenMax = 100f;
    public const float OxygenDrainPerSecond = 1.5f;
    public const float OxygenPerShot = 1f;
    public const float OutOfBoundsDrainMultiplier = 4f; // oxygen drains this much faster outside the level

    // --- Level (temporary test ground, until levels are loaded from text files) ---
    public const float LevelWidth = 60f;
    public const float LevelHeight = 120f;
    public const float PlayerStartFromBottom = 5f; // player starts this far above the bottom border
    public const float ShipFromTop = 3f;           // ship center is this far below the top border

    // --- Sizes in the world, in units (the larger side of the picture) ---
    public const float ShipLength = 6f;
    public const float ShipHeight = 2.5f;         // placeholder only; real art keeps its own proportions
    public const float AsteroidSmallSize = 1f;
    public const float AsteroidMediumSize = 2f;
    public const float AsteroidLargeSize = 4f;
    public const float OxygenTankSize = 0.6f;
    public const float PlanetSize = 8f;
    public const float BackgroundSize = 48f;

    // --- Background parallax: how fast a layer scrolls on screen compared to the world (1 = like the world) ---
    public const float BackgroundParallax = 0.1f; // artist picture "background" (does not repeat)
    public const float NebulaFarParallax = 0.25f; // far clouds (repeat endlessly)
    public const float NebulaNearParallax = 0.55f; // near clouds: faster, so the picture keeps changing
    public const float NebulaFarSize = 64f;       // one copy of the far cloud picture, in units
    public const float NebulaNearSize = 80f;
    public const float StarsFarParallax = 0.3f;
    public const float StarsNearParallax = 0.6f;
    public const int StarsFarCount = 90;          // small dim stars
    public const int StarsNearCount = 35;         // bigger bright stars
    public const float StarTileSize = 40f;        // stars repeat every this many units; must be wider than the screen
    public const float StarArtScale = 4f;         // star pictures are drawn this many times bigger than placeholder dots

    // --- Asteroids ---
    public const int AsteroidHpSmall = 1;         // hits to destroy
    public const int AsteroidHpMedium = 2;
    public const int AsteroidHpLarge = 4;         // a large one splits into two medium ones
    public const float AsteroidDensity = 1.5f;    // mass = density * size * size (the player weighs 1)
    public const float AsteroidDriftMin = 0.2f;   // slow asteroids: drift speed, units per second
    public const float AsteroidDriftMax = 0.6f;
    public const float AsteroidMoverChance = 0.5f; // share of asteroids that fly noticeably (0..1)
    public const float AsteroidMoveSpeedMin = 1f;  // moving asteroids: speed, random direction
    public const float AsteroidMoveSpeedMax = 3f;

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
    public const float AsteroidSpin = 40f;        // max rotation speed, degrees per second
    public const float AsteroidSplitSpeed = 1.5f; // how fast the two halves fly apart
    public const float Bounciness = 0.5f;         // 0 = no bounce, 1 = full bounce

    // --- Impact: hitting an asteroid faster than the safe speed costs oxygen ---
    public const float ImpactSafeSpeed = 6f;
    public const float ImpactOxygenPerSpeed = 3f; // oxygen lost = (speed - safe speed) * this
    public const float ImpactMaxOxygen = 25f;     // one hit never costs more than this

    // --- Oxygen tanks ---
    public const float OxygenTankAmount = 30f;
    public const float OxygenTankPickupRadius = 0.5f;

    // --- Temporary scatter on the test ground (until levels come from text files) ---
    public const int ScatterAsteroids = 15;
    public const int ScatterTanks = 3;
    public const float ScatterHalfWidth = 8f;     // asteroids lie this far left/right of the straight path
    public const float ScatterClearance = 8f;     // free space around the player start and the ship
    public const float TankSideOffsetMin = 12f;   // tanks lie this far off the straight path
    public const float TankSideOffsetMax = 16f;
}
