using UnityEngine;

// Root of one level prefab (Assets/Prefabs/Levels/LevelN.prefab).
// Everything the level needs lives under it: player, ship, rocks, tanks, hints,
// background and border. Edit the prefab in the editor: move, add or remove objects.
// DraftGame instantiates the prefab on start and destroys it on restart.
public class Level : MonoBehaviour
{
    [Header("Info")]
    public string levelName = "";
    [TextArea] public string radio = "";       // one line of story (not shown yet)

    [Header("Rules")]
    [Range(1f, Tuning.OxygenMax)] public float oxygen = Tuning.OxygenMax; // oxygen at the start
    [Range(0f, 1f)] public float movers;       // share of rocks that drift noticeably
    public float star3 = 50f, star2 = 25f;     // % of oxygen left for 3 and 2 stars (not used yet)

    [Header("Area (world units). Outside it the signal is lost")]
    public Rect bounds = new Rect(-16f, -40f, 32f, 80f);

    [Header("Objects")]
    public PlayerController player;
    public Transform ship;

    HintMarker[] hints;

    public HintMarker[] Hints
    {
        get
        {
            if (hints == null) hints = GetComponentsInChildren<HintMarker>(true);
            return hints;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.25f, 0.9f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
