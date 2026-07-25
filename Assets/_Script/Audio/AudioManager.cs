using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central Singleton Audio Manager for managing all game audio clips, SFX, background music,
/// smooth music crossfading, and AudioMixer volume controls.
/// Supports scene persistence, testing-friendly duplicate auto-cleanup, and non-repeating random music playlists.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("[AudioManager]");
                    _instance = managerObj.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Audio Mixer References")]
    [Tooltip("Reference to the Main AudioMixer asset.")]
    [SerializeField] private AudioMixer audioMixer;

    [Tooltip("Mixer Group for Background Music (Music).")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Tooltip("Mixer Group for Sound Effects (SFX).")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Exposed AudioMixer Parameter Names")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    [Header("Audio Library")]
    [Tooltip("List of all Sound entries configured with unique IDs, clips, volumes, and pitch settings.")]
    [SerializeField] private List<Sound> sounds = new List<Sound>();

    [Header("Music Settings")]
    [Tooltip("Default crossfade transition duration in seconds when changing music tracks.")]
    [SerializeField] private float defaultCrossfadeDuration = 1.2f;

    [Tooltip("If true, automatically starts playing background music when the game starts.")]
    [SerializeField] private bool autoPlayMusicOnStart = true;

    [Tooltip("If true, picks a random music track on start. If false, uses defaultMusicID.")]
    [SerializeField] private bool playRandomMusicOnStart = true;

    [Tooltip("Specific Music soundID to play on start if playRandomMusicOnStart is false.")]
    [SerializeField] private string defaultMusicID = "";

    [Tooltip("If true, automatically plays the next random music track when the current non-looping track finishes.")]
    [SerializeField] private bool autoPlayNextMusicWhenEnded = true;

    // Fast lookup dictionary
    private Dictionary<string, Sound> soundDict = new Dictionary<string, Sound>();

    // Dual AudioSources for smooth music crossfading
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isSourceAPlaying = false;

    // Track active music state
    private string currentMusicID = string.Empty;
    private string lastRandomMusicID = string.Empty;
    private Coroutine activeCrossfadeRoutine;

    // Pool of AudioSources for SFX
    private List<AudioSource> sfxSourcePool = new List<AudioSource>();
    private const int INITIAL_SFX_POOL_SIZE = 8;

    private void Awake()
    {
        // Singleton duplicate handling for scene testing
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitializeAudioSources();
        BuildSoundDictionary();
    }

    private void Start()
    {
        StartCoroutine(ApplyInitialVolumesRoutine());

        if (autoPlayMusicOnStart)
        {
            if (playRandomMusicOnStart)
            {
                PlayRandomMusic(0.5f);
            }
            else if (!string.IsNullOrEmpty(defaultMusicID))
            {
                PlayMusic(defaultMusicID, 0.5f);
            }
        }
    }

    private IEnumerator ApplyInitialVolumesRoutine()
    {
        // Wait 1 frame until Unity AudioMixer finishes initializing exposed parameters internally
        yield return null;

        SetMasterVolume(GetMasterVolume());
        SetMusicVolume(GetMusicVolume());
        SetSFXVolume(GetSFXVolume());
    }

    private bool isApplicationFocused = true;

    private void OnApplicationFocus(bool hasFocus)
    {
        isApplicationFocused = hasFocus;

        if (hasFocus)
        {
            // When regaining focus, resume current music track if it was paused by OS focus change
            AudioSource activeSource = isSourceAPlaying ? musicSourceA : musicSourceB;
            if (activeSource != null && activeSource.clip != null && !activeSource.isPlaying)
            {
                // Resume song if it did not naturally reach the end of the track
                if (activeSource.time < (activeSource.clip.length - 0.5f))
                {
                    activeSource.UnPause();
                    if (!activeSource.isPlaying)
                    {
                        activeSource.Play();
                    }
                    Debug.Log($"[AudioManager] Regained focus. Resuming music track '{currentMusicID}' without skipping.");
                }
            }
        }
    }

    private void Update()
    {
        // Monitor active music playback and auto-advance to next random track ONLY if non-looping track naturally ends
        if (autoPlayNextMusicWhenEnded && !string.IsNullOrEmpty(currentMusicID) && activeCrossfadeRoutine == null && isApplicationFocused)
        {
            AudioSource activeSource = isSourceAPlaying ? musicSourceA : musicSourceB;
            if (activeSource != null && activeSource.clip != null && !activeSource.loop)
            {
                // Only consider track finished if playback position has reached the end of the clip
                bool isTrackFinished = !activeSource.isPlaying && activeSource.time >= (activeSource.clip.length - 0.3f);
                if (isTrackFinished)
                {
                    Debug.Log("[AudioManager] Current music finished naturally. Auto-playing next random track.");
                    PlayRandomMusic(defaultCrossfadeDuration);
                }
            }
        }
    }

    private void InitializeAudioSources()
    {
        // Setup dual AudioSources for Music crossfading
        GameObject musicContainer = new GameObject("MusicSources");
        musicContainer.transform.SetParent(transform);

        musicSourceA = musicContainer.AddComponent<AudioSource>();
        musicSourceA.playOnAwake = false;
        if (musicMixerGroup != null) musicSourceA.outputAudioMixerGroup = musicMixerGroup;

        musicSourceB = musicContainer.AddComponent<AudioSource>();
        musicSourceB.playOnAwake = false;
        if (musicMixerGroup != null) musicSourceB.outputAudioMixerGroup = musicMixerGroup;

        // Setup SFX Pool
        GameObject sfxContainer = new GameObject("SFXPool");
        sfxContainer.transform.SetParent(transform);

        for (int i = 0; i < INITIAL_SFX_POOL_SIZE; i++)
        {
            AudioSource source = sfxContainer.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;
            sfxSourcePool.Add(source);
        }
    }

    private void BuildSoundDictionary()
    {
        soundDict.Clear();
        foreach (Sound sound in sounds)
        {
            if (sound == null || sound.audioFile == null) continue;

            // Auto-assign fallback soundID using audio clip name if blank
            if (string.IsNullOrEmpty(sound.soundID))
            {
                sound.soundID = sound.audioFile.name;
            }

            if (soundDict.ContainsKey(sound.soundID))
            {
                Debug.LogWarning($"[AudioManager] Duplicate soundID found: '{sound.soundID}'. Only the first entry will be used.");
            }
            else
            {
                soundDict.Add(sound.soundID, sound);
            }
        }
    }

    private void OnValidate()
    {
        BuildSoundDictionary();
    }

    #region Public SFX Methods

    /// <summary>
    /// Plays 2D Sound Effect by unique soundID.
    /// Example: AudioManager.Instance.PlaySFX("ButtonClick");
    /// </summary>
    public void PlaySFX(string soundID)
    {
        if (string.IsNullOrEmpty(soundID)) return;

        Sound sound = GetSoundByID(soundID);
        if (sound == null || sound.audioFile == null)
        {
            Debug.LogWarning($"[AudioManager] SFX with ID '{soundID}' not found or missing audio clip.");
            return;
        }

        AudioSource freeSource = GetFreeSFXAudioSource();
        freeSource.clip = sound.audioFile;
        freeSource.volume = sound.volume;
        freeSource.pitch = sound.GetCalculatedPitch();
        freeSource.loop = sound.loop;
        if (sfxMixerGroup != null) freeSource.outputAudioMixerGroup = sfxMixerGroup;

        freeSource.Play();
    }

    /// <summary>
    /// Plays 3D Spatial Sound Effect at world position by soundID.
    /// </summary>
    public void PlaySFXAtPosition(string soundID, Vector3 position)
    {
        if (string.IsNullOrEmpty(soundID)) return;

        Sound sound = GetSoundByID(soundID);
        if (sound == null || sound.audioFile == null)
        {
            Debug.LogWarning($"[AudioManager] Spatial SFX with ID '{soundID}' not found or missing audio clip.");
            return;
        }

        GameObject tempSpatialObj = new GameObject($"TempAudio_{soundID}");
        tempSpatialObj.transform.position = position;

        AudioSource source = tempSpatialObj.AddComponent<AudioSource>();
        source.clip = sound.audioFile;
        source.volume = sound.volume;
        source.pitch = sound.GetCalculatedPitch();
        source.spatialBlend = 1f; // Full 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 1f;
        source.maxDistance = 25f;
        if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;

        source.Play();
        Destroy(tempSpatialObj, sound.audioFile.length / Mathf.Max(0.1f, source.pitch) + 0.1f);
    }

    private AudioSource GetFreeSFXAudioSource()
    {
        foreach (AudioSource source in sfxSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // Expand pool if all sources are busy
        AudioSource newSource = transform.Find("SFXPool").gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        if (sfxMixerGroup != null) newSource.outputAudioMixerGroup = sfxMixerGroup;
        sfxSourcePool.Add(newSource);
        return newSource;
    }

    #endregion

    #region Public Music Methods

    /// <summary>
    /// Plays background music with smooth crossfading transition.
    /// If the requested music is already playing, it will continue uninterrupted.
    /// Example: AudioManager.Instance.PlayMusic("BGM_Map1");
    /// </summary>
    public void PlayMusic(string soundID, float fadeDuration = -1f)
    {
        if (string.IsNullOrEmpty(soundID)) return;

        if (fadeDuration < 0f) fadeDuration = defaultCrossfadeDuration;

        // If the requested track is already currently playing, keep it playing seamlessly
        if (currentMusicID == soundID)
        {
            AudioSource currentSource = isSourceAPlaying ? musicSourceA : musicSourceB;
            if (currentSource != null && currentSource.isPlaying)
            {
                return;
            }
        }

        Sound sound = GetSoundByID(soundID);
        if (sound == null || sound.audioFile == null)
        {
            Debug.LogWarning($"[AudioManager] Music with ID '{soundID}' not found or missing audio clip.");
            return;
        }

        currentMusicID = soundID;

        AudioSource newSource = isSourceAPlaying ? musicSourceB : musicSourceA;
        AudioSource oldSource = isSourceAPlaying ? musicSourceA : musicSourceB;

        newSource.clip = sound.audioFile;
        newSource.loop = sound.loop;
        newSource.pitch = sound.GetCalculatedPitch();
        if (musicMixerGroup != null) newSource.outputAudioMixerGroup = musicMixerGroup;

        if (activeCrossfadeRoutine != null)
        {
            StopCoroutine(activeCrossfadeRoutine);
        }

        Debug.Log($"[AudioManager] Playing Music track: '{sound.soundID}' (Clip: '{sound.audioFile.name}', Target Volume: {sound.volume})");

        activeCrossfadeRoutine = StartCoroutine(CrossfadeMusicRoutine(oldSource, newSource, sound.volume, fadeDuration));
        isSourceAPlaying = !isSourceAPlaying;
    }

    /// <summary>
    /// Randomly selects and plays a music track from all registered SoundType.Music entries.
    /// Ensures the next chosen track is NEVER identical to the currently/previously played track.
    /// Example: AudioManager.Instance.PlayRandomMusic();
    /// </summary>
    public void PlayRandomMusic(float fadeDuration = -1f)
    {
        List<Sound> musicList = sounds.FindAll(s => s != null && s.soundType == SoundType.Music && s.audioFile != null && !string.IsNullOrEmpty(s.soundID));

        if (musicList.Count == 0)
        {
            Debug.LogWarning("[AudioManager] No valid Music entries found in sounds list.");
            return;
        }

        if (musicList.Count == 1)
        {
            PlayMusic(musicList[0].soundID, fadeDuration);
            lastRandomMusicID = musicList[0].soundID;
            return;
        }

        // Filter out the previously played music track so it never repeats consecutively
        List<Sound> availableTracks = musicList.FindAll(s => s.soundID != lastRandomMusicID);
        if (availableTracks.Count == 0)
        {
            availableTracks = musicList;
        }

        int randomIndex = Random.Range(0, availableTracks.Count);
        Sound selectedSound = availableTracks[randomIndex];

        lastRandomMusicID = selectedSound.soundID;
        PlayMusic(selectedSound.soundID, fadeDuration);
    }

    private IEnumerator CrossfadeMusicRoutine(AudioSource oldSource, AudioSource newSource, float targetVolume, float duration)
    {
        newSource.volume = 0f;
        newSource.Play();

        float startOldVolume = oldSource != null ? oldSource.volume : 0f;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            newSource.volume = targetVolume;
            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.volume = 0f;
            }
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);

            newSource.volume = Mathf.Lerp(0f, targetVolume, percent);
            if (oldSource != null && oldSource.isPlaying)
            {
                oldSource.volume = Mathf.Lerp(startOldVolume, 0f, percent);
            }

            yield return null;
        }

        newSource.volume = targetVolume;
        if (oldSource != null)
        {
            oldSource.Stop();
            oldSource.volume = 0f;
        }

        activeCrossfadeRoutine = null;
    }

    #endregion

    #region AudioMixer Volume Control API

    private const string PREF_MASTER_VOLUME = "Audio_MasterVolume";
    private const string PREF_MUSIC_VOLUME = "Audio_MusicVolume";
    private const string PREF_SFX_VOLUME = "Audio_SFXVolume";

    public static event System.Action<string, float> OnVolumeChanged;

    /// <summary>
    /// Sets Master Volume (0.0 to 1.0 linear slider value). Converts internally to dB and saves preference.
    /// Default on first launch is 0.3f (30%).
    /// </summary>
    public void SetMasterVolume(float linearVolume)
    {
        linearVolume = Mathf.Clamp01(linearVolume);
        SetMixerVolume(masterVolumeParam, linearVolume);
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, linearVolume);
        PlayerPrefs.Save();
        OnVolumeChanged?.Invoke(masterVolumeParam, linearVolume);
    }

    /// <summary>
    /// Sets Music Volume (0.0 to 1.0 linear slider value). Converts internally to dB and saves preference.
    /// </summary>
    public void SetMusicVolume(float linearVolume)
    {
        linearVolume = Mathf.Clamp01(linearVolume);
        SetMixerVolume(musicVolumeParam, linearVolume);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, linearVolume);
        PlayerPrefs.Save();
        OnVolumeChanged?.Invoke(musicVolumeParam, linearVolume);
    }

    /// <summary>
    /// Sets SFX Volume (0.0 to 1.0 linear slider value). Converts internally to dB and saves preference.
    /// </summary>
    public void SetSFXVolume(float linearVolume)
    {
        linearVolume = Mathf.Clamp01(linearVolume);
        SetMixerVolume(sfxVolumeParam, linearVolume);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, linearVolume);
        PlayerPrefs.Save();
        OnVolumeChanged?.Invoke(sfxVolumeParam, linearVolume);
    }

    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (audioMixer == null) return;

        // Convert linear (0.0 to 1.0) slider value to decibels (-80dB to 0dB)
        float decibelValue = linearVolume <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linearVolume)) * 20f;
        audioMixer.SetFloat(parameterName, decibelValue);
    }

    /// <summary>
    /// Gets Master Volume linear value (0.0 to 1.0). Default on first launch is 0.3f (30%).
    /// </summary>
    public float GetMasterVolume() => PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 0.3f);

    /// <summary>
    /// Gets Music Volume linear value (0.0 to 1.0). Default on first launch is 0.3f (30%).
    /// </summary>
    public float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.3f);

    /// <summary>
    /// Gets SFX Volume linear value (0.0 to 1.0). Default on first launch is 0.3f (30%).
    /// </summary>
    public float GetSFXVolume() => PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.3f);

    private float GetMixerVolume(string parameterName)
    {
        if (audioMixer == null) return 1f;

        if (audioMixer.GetFloat(parameterName, out float decibelValue))
        {
            return Mathf.Pow(10f, decibelValue / 20f);
        }
        return 1f;
    }

    #endregion

    #region Helper Methods

    public Sound GetSoundByID(string soundID)
    {
        if (string.IsNullOrEmpty(soundID)) return null;

        if (soundDict.TryGetValue(soundID, out Sound sound))
        {
            return sound;
        }

        // Fallback linear search if dict missed
        return sounds.Find(s => s != null && s.soundID == soundID);
    }

    #endregion
}
