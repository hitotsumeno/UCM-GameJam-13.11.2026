using UnityEngine;

// Plays a list of sprites in a loop on a SpriteRenderer: an animation from a sprite sheet that
// was sliced in the Sprite Editor (Sprite Mode: Multiple). DraftSetup fills the frames from the
// sheet when the prefabs are rebuilt. Every copy starts at a random frame, so meteors that fly
// together do not burn in step. With fewer than two frames it does nothing.
public class SpriteFrames : MonoBehaviour
{
    [SerializeField] SpriteRenderer target;
    [SerializeField] Sprite[] frames;
    [SerializeField] float framesPerSecond = 12f;

    float time;

    public void Setup(SpriteRenderer target, Sprite[] frames, float framesPerSecond)
    {
        this.target = target;
        this.frames = frames;
        this.framesPerSecond = framesPerSecond;
    }

    void OnEnable()
    {
        time = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (target == null || frames == null || frames.Length < 2) return;
        time += Time.deltaTime;
        target.sprite = frames[(int)(time * framesPerSecond) % frames.Length];
    }
}
