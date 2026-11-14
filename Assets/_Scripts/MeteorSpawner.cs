using UnityEngine;

// From time to time launches a meteor from above the screen down at the player.
// Add it to a level root to turn meteors on for that level; disable or remove it to turn them off.
public class MeteorSpawner : MonoBehaviour
{
    [SerializeField] Asteroid smallMeteorPrefab;
    [SerializeField] Asteroid mediumMeteorPrefab;
    [Tooltip("Seconds between meteors")]
    public float intervalMin = Tuning.MeteorIntervalMin;
    public float intervalMax = Tuning.MeteorIntervalMax;

    float timer;

    void Start()
    {
        timer = Tuning.MeteorFirstDelay;
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
            player.position.x + Random.Range(-Tuning.MeteorSpread, Tuning.MeteorSpread),
            top + Tuning.MeteorSpawnAhead);
        var velocity = new Vector2(
            Random.Range(-Tuning.MeteorSideSpeed, Tuning.MeteorSideSpeed),
            -Random.Range(Tuning.MeteorSpeedMin, Tuning.MeteorSpeedMax));

        var prefab = Random.value < Tuning.MeteorMediumChance ? mediumMeteorPrefab : smallMeteorPrefab;
        if (prefab == null) prefab = smallMeteorPrefab != null ? smallMeteorPrefab : mediumMeteorPrefab;
        if (prefab == null) return;
        Asteroid.Spawn(prefab, pos, velocity, transform, false);
    }
}
