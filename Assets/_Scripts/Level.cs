using UnityEngine;

// Root of one level prefab (Assets/Prefabs/Levels/LevelN.prefab).
// Everything the level needs lives under it: player, ship, rocks, tanks, mines,
// black holes, background and border. Edit the prefab in the editor: move, add or
// remove objects. DraftGame instantiates the prefab on start and destroys it on restart.
public class Level : MonoBehaviour
{
    [Header("Info")]
    public string levelName = "";
    [TextArea] public string radio = "";       // one line of story (not shown yet)

    [Header("Rules")]
    [Tooltip("Oxygen at the start (capped by Tuning.OxygenMax)")]
    [Min(1f)] public float oxygen = 100f;
    [Range(0f, 1f)] public float movers;       // share of rocks that drift noticeably
    public float star3 = 50f, star2 = 25f;     // % of oxygen left for 3 and 2 stars (not used yet)

    [Header("Look")]
    [Tooltip("Colour of the light on rocks, the player and the ship (set on the Global Light 2D)")]
    public Color lightColor = Color.white;
    [Tooltip("Colour of the storm wall at the level edge (level file: # frame=)")]
    public Color stormColor = new Color(1f, 0.3f, 0.25f);

    [Header("Area (world units). Outside it is the storm: air runs out faster, the ship is pushed back")]
    public Rect bounds = new Rect(-16f, -40f, 32f, 80f);

    [Header("Objects")]
    public PlayerController player;
    public Transform ship;

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.25f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
