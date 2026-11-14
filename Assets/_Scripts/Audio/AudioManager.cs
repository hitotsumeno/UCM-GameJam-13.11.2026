using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Plays all sounds and music. Put one "AudioManager" object in every scene.
// The first one survives scene changes; a later one only hands over its scene music
// and removes itself, so music changes per scene without a second manager.
//
// From any script:  AudioManager.Play(someSoundEvent);
// Safe to call when there is no manager or the event is empty: nothing happens.
//
// Volumes: the mixer must expose the parameters named below (MasterVolume, MusicVolume,
// SFXVolume). Sliders send 0..1, saved in PlayerPrefs.
public class AudioManager : MonoBehaviour
{
    public const string MasterParam = "MasterVolume";
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";

    public static AudioManager Instance;

    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] AudioMixerGroup sfxGroup;

    [Tooltip("music that starts when this scene opens (can be empty)")]
    [SerializeField] SoundEvent sceneMusic;

    [SerializeField] int sfxVoices = 12;
    [SerializeField] float musicFadeTime = 1f;

    readonly List<AudioSource> sfxSources = new List<AudioSource>();
    readonly Dictionary<SoundEvent, float> lastPlayed = new Dictionary<SoundEvent, float>();
    AudioSource[] musicSources;
    int activeMusic;
    int nextVoice;
    SoundEvent currentMusic;
    Coroutine fade;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.PlayMusic(sceneMusic);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < sfxVoices; i++)
            sfxSources.Add(NewSource(sfxGroup));
        musicSources = new[] { NewSource(musicGroup), NewSource(musicGroup) };
    }

    void Start()
    {
        // The mixer ignores SetFloat during Awake, so volumes are applied here.
        ApplyVolume(MasterParam, GetVolume(MasterParam));
        ApplyVolume(MusicParam, GetVolume(MusicParam));
        ApplyVolume(SfxParam, GetVolume(SfxParam));
        PlayMusic(sceneMusic);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    AudioSource NewSource(AudioMixerGroup group)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f; // 2D game: no 3D falloff
        s.outputAudioMixerGroup = group;
        return s;
    }

    // ---------- Sound effects ----------

    public static void Play(SoundEvent e)
    {
        if (Instance != null) Instance.PlaySfx(e);
    }

    void PlaySfx(SoundEvent e)
    {
        if (e == null) return;
        var clip = e.PickClip();
        if (clip == null) return;

        if (lastPlayed.TryGetValue(e, out float t) && Time.unscaledTime - t < e.cooldown) return;
        lastPlayed[e] = Time.unscaledTime;

        // Take a free voice; if all are busy, reuse the oldest one in turn.
        AudioSource src = null;
        for (int i = 0; i < sfxSources.Count; i++)
        {
            var s = sfxSources[(nextVoice + i) % sfxSources.Count];
            if (!s.isPlaying) { src = s; break; }
        }
        if (src == null) src = sfxSources[nextVoice];
        nextVoice = (nextVoice + 1) % sfxSources.Count;

        src.outputAudioMixerGroup = e.output != null ? e.output : sfxGroup;
        src.clip = clip;
        src.volume = e.volume;
        src.pitch = e.PickPitch();
        src.loop = false;
        src.Play();
    }

    // ---------- Music ----------

    public static void Music(SoundEvent e)
    {
        if (Instance != null) Instance.PlayMusic(e);
    }

    public void PlayMusic(SoundEvent e)
    {
        if (e == null || e == currentMusic) return;
        var clip = e.PickClip();
        if (clip == null) return;
        currentMusic = e;

        var from = musicSources[activeMusic];
        activeMusic = 1 - activeMusic;
        var to = musicSources[activeMusic];

        to.outputAudioMixerGroup = e.output != null ? e.output : musicGroup;
        to.clip = clip;
        to.loop = e.loop;
        to.pitch = 1f;
        to.volume = 0f;
        to.Play();

        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(Crossfade(from, to, e.volume));
    }

    public static void StopMusic()
    {
        if (Instance == null) return;
        Instance.currentMusic = null;
        if (Instance.fade != null) Instance.StopCoroutine(Instance.fade);
        var from = Instance.musicSources[Instance.activeMusic];
        Instance.fade = Instance.StartCoroutine(Instance.Crossfade(from, null, 0f));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float toVolume)
    {
        float fromStart = from != null ? from.volume : 0f;
        float t = 0f;
        while (t < musicFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = musicFadeTime > 0f ? Mathf.Clamp01(t / musicFadeTime) : 1f;
            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (to != null) to.volume = Mathf.Lerp(0f, toVolume, k);
            yield return null;
        }
        if (from != null && from != to) from.Stop();
        fade = null;
    }

    // ---------- Volumes (0..1) ----------

    public static float GetVolume(string param)
    {
        return PlayerPrefs.GetFloat(param, 0.8f);
    }

    public static void SetVolume(string param, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(param, value);
        if (Instance != null) Instance.ApplyVolume(param, value);
    }

    void ApplyVolume(string param, float value)
    {
        if (mixer == null) return;
        // Slider 0..1 to decibels: 1 = 0 dB, 0.5 = -6 dB, 0 = silent (-80 dB).
        float db = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        mixer.SetFloat(param, db);
    }
}
