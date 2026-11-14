using UnityEngine;

// From time to time launches a meteor from above the screen down at the player.
public class MeteorSpawner : MonoBehaviour
{
    Transform player;
    float timer;
    float intervalMin, intervalMax;

    // intervalMin..intervalMax: seconds between meteors (set per level).
    public static MeteorSpawner Create(Transform parent, Transform player, float intervalMin, float intervalMax)
    {
        var go = new GameObject("MeteorSpawner");
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<MeteorSpawner>();
        s.player = player;
        s.intervalMin = intervalMin;
        s.intervalMax = intervalMax;
        s.timer = Tuning.MeteorFirstDelay;
        return s;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing || player == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(intervalMin, intervalMax);

        var cam = Camera.main;
        if (cam == null) return;
        float top = cam.transform.position.y + cam.orthographicSize;
        var body = player.GetComponent<Rigidbody2D>();
        Vector2 playerVelocity = body != null ? body.linearVelocity : Vector2.zero;

        // Sometimes a whole shower comes instead of one meteor.
        int count = Random.value < Tuning.MeteorShowerChance ? Tuning.MeteorShowerCount : 1;
        for (int i = 0; i < count; i++)
        {
            float speed = Random.Range(Tuning.MeteorSpeedMin, Tuning.MeteorSpeedMax);
            var start = new Vector2(
                player.position.x + Random.Range(-Tuning.MeteorSpread, Tuning.MeteorSpread),
                top + Tuning.MeteorSpawnAhead + i * 5f);

            // Aim at where the player is going to be, not where they are now.
            // The first meteor is aimed precisely, the rest of a shower fall around it.
            float flight = Mathf.Min((start.y - player.position.y) / speed, Tuning.MeteorLead);
            Vector2 target = (Vector2)player.position + playerVelocity * flight;
            if (i > 0) target += Random.insideUnitCircle * 5f;

            Vector2 dir = target - start;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.down;
            Meteor.Create(start, dir.normalized * speed, transform.parent);
        }
    }
}
