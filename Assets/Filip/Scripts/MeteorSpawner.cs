using UnityEngine;

// From time to time launches a meteor from above the screen down at the player.
public class MeteorSpawner : MonoBehaviour
{
    Transform player;
    float timer;

    public static MeteorSpawner Create(Transform parent, Transform player)
    {
        var go = new GameObject("MeteorSpawner");
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<MeteorSpawner>();
        s.player = player;
        s.timer = Tuning.MeteorFirstDelay;
        return s;
    }

    void Update()
    {
        var game = DraftGame.Instance;
        if (game == null || game.State != GameState.Playing || player == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(Tuning.MeteorIntervalMin, Tuning.MeteorIntervalMax);

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
