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
        var pos = new Vector2(
            player.position.x + Random.Range(-Tuning.MeteorSpread, Tuning.MeteorSpread),
            top + Tuning.MeteorSpawnAhead);
        var velocity = new Vector2(
            Random.Range(-Tuning.MeteorSideSpeed, Tuning.MeteorSideSpeed),
            -Random.Range(Tuning.MeteorSpeedMin, Tuning.MeteorSpeedMax));
        Meteor.Create(pos, velocity, transform.parent);
    }
}
