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
    public const float BackgroundParallax = 0.1f;
    public const float StarsFarParallax = 0.3f;
    public const float StarsNearParallax = 0.6f;
    public const int StarsPerLayer = 40;
    public const float StarTileSize = 40f;        // stars repeat every this many units; must be wider than the screen
}
