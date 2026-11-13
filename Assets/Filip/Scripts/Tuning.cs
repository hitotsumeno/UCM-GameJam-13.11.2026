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

    // --- Background ---
    public const int StarCount = 300;
    public const float StarFieldSize = 200f;      // stars are scattered over a square of this size
}
