using UnityEngine;

// From time to time launches a meteor (sometimes a whole shower) from above the screen
// at the place the player is flying to.
// Add it to a level root to turn meteors on for that level; disable or remove it to turn them off.
public class MeteorSpawner : MonoBehaviour
{
    [Tooltip("Balance numbers: Assets/_Settings/Tuning.asset")]
    [SerializeField] Tuning tuning;
    Tuning T { get { return tuning != null ? tuning : Tuning.Active; } }

    [SerializeField] Asteroid smallMeteorPrefab;
    [SerializeField] Asteroid mediumMeteorPrefab;
    [Tooltip("Seconds between meteors")]
    public float intervalMin = 3f;
    public float intervalMax = 6f;

    float timer;

    void Start()
    {
        timer = T.MeteorFirstDelay;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        Transform player = game != null ? game.PlayerTransform : null;
        if (game == null || game.State != GameState.Playing || player == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(intervalMin, Mathf.Max(intervalMin, intervalMax));

        var cam = Camera.main;
        if (cam == null) return;
        float top = cam.transform.position.y + cam.orthographicSize;
        var body = player.GetComponent<Rigidbody2D>();
        Vector2 playerVelocity = body != null ? body.linearVelocity : Vector2.zero;

        // Sometimes a whole shower comes instead of one meteor.
        int count = Random.value < T.MeteorShowerChance ? T.MeteorShowerCount : 1;
        for (int i = 0; i < count; i++)
        {
            var prefab = Random.value < T.MeteorMediumChance ? mediumMeteorPrefab : smallMeteorPrefab;
            if (prefab == null) prefab = smallMeteorPrefab != null ? smallMeteorPrefab : mediumMeteorPrefab;
            if (prefab == null) return;

            float speed = Random.Range(T.MeteorSpeedMin, T.MeteorSpeedMax);
            var start = new Vector2(
                player.position.x + Random.Range(-T.MeteorSpread, T.MeteorSpread),
                top + T.MeteorSpawnAhead + i * 5f);

            // Aim at where the player is going to be, not where they are now.
            // The first meteor is aimed precisely, the rest of a shower fall around it.
            float flight = Mathf.Min((start.y - player.position.y) / speed, T.MeteorLead);
            Vector2 target = (Vector2)player.position + playerVelocity * flight;
            if (i > 0) target += Random.insideUnitCircle * 5f;

            Vector2 dir = target - start;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
            Asteroid.Spawn(prefab, start, dir.normalized * speed, transform, false);
        }
    }
}
