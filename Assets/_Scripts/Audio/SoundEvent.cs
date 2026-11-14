using UnityEngine;
using UnityEngine.Audio;

// One sound of the game as an asset (Create > Game/Sound Event).
// Holds one or more clips (a random one plays each time), volume, a random pitch range
// and the mixer group it goes to (Music or SFX). Play it with AudioManager.Play(event).
[CreateAssetMenu(fileName = "SFX_New", menuName = "Game/Sound Event")]
public class SoundEvent : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup output;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitchMin = 0.95f;
    [Range(0.5f, 2f)] public float pitchMax = 1.05f;

    [Tooltip("seconds before this sound can play again (stops 20 pickups playing at once)")]
    public float cooldown = 0.05f;

    [Tooltip("music only: loop the clip")]
    public bool loop;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    public float PickPitch()
    {
        return Random.Range(pitchMin, pitchMax);
    }
}
