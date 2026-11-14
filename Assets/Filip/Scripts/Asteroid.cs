using UnityEngine;

public enum AsteroidSize { Small, Medium, Large }

// Drifts slowly, bounces off other asteroids and the player. Bullets break it.
// Prefabs: Assets/Filip/Prefabs/Asteroid_Small / _Medium / _Large.
// Scale the object to make one rock bigger or smaller: collider and mass follow.
public class Asteroid : MonoBehaviour
{
    static readonly Color DamagedColor = new Color(1f, 0.45f, 0.35f);

    // Level area. Confined asteroids turn back at its edges, so they never fly away.
    public static Rect Bounds;

    [SerializeField] AsteroidSize size;
    [SerializeField] SpriteRenderer body;
    [SerializeField] Rigidbody2D rb;
    [Tooltip("Turns back at the level edges")]
    [SerializeField] bool confined = true;
    [Tooltip("A random one is used when this rock is spawned during play (splits, meteors)")]
    [SerializeField] Sprite[] variants;
    [Tooltip("Large rocks split into two of these when destroyed")]
    [SerializeField] Asteroid splitPrefab;
    [SerializeField] OxygenCrystal crystalPrefab;

    int hp, maxHp;
    Color baseColor;
    bool launched;

    // Actual diameter of this rock in units (the class size times the object scale).
    public float Size
    {
        get { return Diameter(size) * Mathf.Abs(transform.lossyScale.x); }
    }

    public static float Diameter(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return Tuning.AsteroidSmallSize;
            case AsteroidSize.Medium: return Tuning.AsteroidMediumSize;
            default: return Tuning.AsteroidLargeSize;
        }
    }

    static int HitPoints(AsteroidSize size)
    {
        switch (size)
        {
            case AsteroidSize.Small: return Tuning.AsteroidHpSmall;
            case AsteroidSize.Medium: return Tuning.AsteroidHpMedium;
            default: return Tuning.AsteroidHpLarge;
        }
    }

    // Spawns a rock during play with a random size variation, look and rotation.
    public static Asteroid Spawn(Asteroid prefab, Vector2 position, Vector2 velocity, Transform parent,
        bool confined = true)
    {
        var a = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), parent);
        float s = Random.Range(Tuning.AsteroidSizeVariationMin, Tuning.AsteroidSizeVariationMax);
        a.transform.localScale = new Vector3(s, s, 1f);
        a.confined = confined;
        a.PickRandomLook();
        a.Launch(velocity);
        return a;
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        maxHp = HitPoints(size);
        hp = maxHp;
        baseColor = body.color;
    }

    void Start()
    {
        float d = Size;
        rb.mass = Tuning.AsteroidDensity * d * d;

        // Rocks placed in the level get a random drift: some barely move, others fly noticeably.
        if (!launched)
        {
            var level = GetComponentInParent<Level>();
            float moverChance = level != null ? level.movers : 0f;
            bool mover = Random.value < moverChance;
            float speed = mover
                ? Random.Range(Tuning.AsteroidMoveSpeedMin, Tuning.AsteroidMoveSpeedMax)
                : Random.Range(Tuning.AsteroidDriftMin, Tuning.AsteroidDriftMax);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            rb.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
        }
        rb.angularVelocity = Random.Range(-Tuning.AsteroidSpin, Tuning.AsteroidSpin);
    }

    // Sets the starting velocity (otherwise a random drift is chosen in Start).
    public void Launch(Vector2 velocity)
    {
        launched = true;
        rb.linearVelocity = velocity;
    }

    public void PickRandomLook()
    {
        if (variants == null || variants.Length == 0) return;
        var s = variants[Random.Range(0, variants.Length)];
        if (s != null) body.sprite = s;
    }

    public void SetTint(Color tint)
    {
        baseColor *= tint;
        body.color = baseColor;
    }

    void FixedUpdate()
    {
        if (!confined) return;
        Vector2 p = rb.position;
        Vector2 v = rb.linearVelocity;
        bool turned = false;
        if ((p.x < Bounds.xMin && v.x < 0f) || (p.x > Bounds.xMax && v.x > 0f)) { v.x = -v.x; turned = true; }
        if ((p.y < Bounds.yMin && v.y < 0f) || (p.y > Bounds.yMax && v.y > 0f)) { v.y = -v.y; turned = true; }
        if (turned) rb.linearVelocity = v;
    }

    // Called by a bullet.
    public void Hit()
    {
        if (hp <= 0) return;
        hp--;
        if (hp > 0)
        {
            // Gets redder with every hit, so damage is visible.
            body.color = Color.Lerp(DamagedColor, baseColor, (float)hp / maxHp);
            return;
        }

        // Destroyed: leave oxygen crystals. A combo makes each crystal worth more.
        var game = DraftGame.Instance;
        int combo = game != null ? game.RegisterKill() : 1;
        int crystals = size == AsteroidSize.Small ? Tuning.CrystalsFromSmall
                     : size == AsteroidSize.Medium ? Tuning.CrystalsFromMedium
                     : Tuning.CrystalsFromLarge;
        float worth = Tuning.CrystalOxygen * (1f + Tuning.ComboBonusPerStep * (combo - 1));
        OxygenCrystal.Burst(crystalPrefab, transform.position, crystals, worth, transform.parent);

        if (splitPrefab != null)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;
            Vector2 pos = transform.position;
            float offset = splitPrefab.Size * 0.5f;
            Vector2 push = dir * Tuning.AsteroidSplitSpeed;
            Spawn(splitPrefab, pos + dir * offset, rb.linearVelocity + push, transform.parent, confined);
            Spawn(splitPrefab, pos - dir * offset, rb.linearVelocity - push, transform.parent, confined);
        }
        Destroy(gameObject);
    }
}
