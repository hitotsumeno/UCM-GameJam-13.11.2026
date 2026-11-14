// Every word the game shows on screen (except level names and hints, which are in the
// level files). Rewrite them here in your own words: this is the only place.
// An empty string "" means "show nothing".
public static class Texts
{
    // Interface
    public const string Air = "air";                 // label over the bar
    public const string Leaks = "leak x";            // followed by the number of hull breaches
    public const string Speed = "speed ";            // followed by the number
    public const string Combo = "x";                 // followed by the number
    public const string Metres = " m";               // after the distance to the ship
    public const string OutOfBounds = "too far out";

    // Short notices
    public const string TooFast = "too fast";
    public const string Breach = "hull breached";
    public const string Sealed = "leak sealed";
    public const string Wormhole = "";               // falling through a black hole: the flash says enough

    // End of a level
    public const string Docked = "docked";
    public const string AirLeft = "air left: ";      // followed by the number
    public const string Next = "enter - next level";
    public const string TheEnd = "that was the last one. enter - play again";

    // Losing
    public const string OutOfAir = "out of air";
    public const string HitPlanet = "crashed";
    public const string Retry = "r - try again";
}
