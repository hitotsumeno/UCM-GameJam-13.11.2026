using UnityEngine;

// From time to time launches a meteor from above the screen down at the player.
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
        var pos = new Vector2(
            player.position.x + Random.Range(-T.MeteorSpread, T.MeteorSpread),
            top + T.MeteorSpawnAhead);
        var velocity = new Vector2(
            Random.Range(-T.MeteorSideSpeed, T.MeteorSideSpeed),
            -Random.Range(T.MeteorSpeedMin, T.MeteorSpeedMax));

        var prefab = Random.value < T.MeteorMediumChance ? mediumMeteorPrefab : smallMeteorPrefab;
        if (prefab == null) prefab = smallMeteorPrefab != null ? smallMeteorPrefab : mediumMeteorPrefab;
        if (prefab == null) return;
        Asteroid.Spawn(prefab, pos, velocity, transform, false);
    }
}
