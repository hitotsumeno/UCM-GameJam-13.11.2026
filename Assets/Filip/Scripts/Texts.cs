// Every word the game shows on screen (level names are in the level files).
// Rewrite them here in your own words: this is the only place.
// An empty string "" means "show nothing".
// The pixel font has only capital letters, digits and - . : / + !
public static class Texts
{
    // Interface
    public const string Combo = "x";                 // followed by the number
    public const string OutOfBounds = "turn back";

    // Short notices
    public const string TooFast = "too fast";
    public const string Breach = "";                 // the red flash and the drop icon say enough
    public const string Sealed = "";                 // the drop icon disappears
    public const string Wormhole = "";               // falling through a black hole: the flash says enough

    // End of a level
    public const string Docked = "docked";
    public const string KeyNext = "enter";           // drawn as a keycap
    public const string KeyRetry = "r";

    // Losing
    public const string OutOfAir = "out of air";
    public const string HitPlanet = "crashed";
}
