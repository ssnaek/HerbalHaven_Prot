using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized audio manager for music and sound effects.
/// Handles music crossfading between scenes, SFX playback, and volume mixing.
/// Singleton - persists across scenes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Main audio mixer asset")]
    public AudioMixer audioMixer;

    [Header("Music")]
    [Tooltip("Two audio sources for crossfading")]
    public AudioSource musicSource1;
    public AudioSource musicSource2;
    
    [Tooltip("Default music volume (0-1)")]
    [Range(0f, 1f)]
    public float defaultMusicVolume = 0.7f;
    
    [Tooltip("Crossfade duration (seconds)")]
    public float crossfadeDuration = 2f;

    [Header("Sound Effects")]
    [Tooltip("Audio source for one-shot SFX")]
    public AudioSource sfxSource;
    
    [Tooltip("Default SFX volume (0-1)")]
    [Range(0f, 1f)]
    public float defaultSFXVolume = 1f;

    [Header("Volume Settings")]
    [Tooltip("Default master volume (0-1)")]
    [Range(0f, 1f)]
    public float defaultMasterVolume = 0.8f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private AudioSource currentMusicSource;
    private AudioSource nextMusicSource;
    private bool isCrossfading = false;

    // PlayerPrefs keys for saving volume settings
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSources();
        LoadVolumeSettings();
    }

    void Start()
    {
        // Subscribe to scene changes for auto music management
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void SetupAudioSources()
    {
        // Create audio sources if not assigned
        if (musicSource1 == null)
        {
            GameObject music1 = new GameObject("MusicSource1");
            music1.transform.SetParent(transform);
            musicSource1 = music1.AddComponent<AudioSource>();
        }

        if (musicSource2 == null)
        {
            GameObject music2 = new GameObject("MusicSource2");
            music2.transform.SetParent(transform);
            musicSource2 = music2.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfx = new GameObject("SFXSource");
            sfx.transform.SetParent(transform);
            sfxSource = sfx.AddComponent<AudioSource>();
        }

        // Configure music sources
        musicSource1.loop = true;
        musicSource1.playOnAwake = false;
        musicSource1.volume = 1f; // Controlled by mixer

        musicSource2.loop = true;
        musicSource2.playOnAwake = false;
        musicSource2.volume = 1f; // Controlled by mixer

        // Configure SFX source
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f; // Controlled by mixer

        // Assign to mixer groups if available
        if (audioMixer != null)
        {
            musicSource1.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
            musicSource2.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
            sfxSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
        }

        // Start with source1 as current
        currentMusicSource = musicSource1;
        nextMusicSource = musicSource2;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showDebugLogs) Debug.Log($"[AudioManager] Scene loaded: {scene.name}");
    }

    // ==================== VOLUME CONTROL (MIXER) ====================

    /// <summary>
    /// Set master volume (0-1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioManager] No Audio Mixer assigned!");
            return;
        }

        float dbVolume = VolumeToDecibels(volume);
        audioMixer.SetFloat("MasterVolume", dbVolume);
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (showDebugLogs)
            Debug.Log($"[AudioManager] Master volume set to {volume:F2} ({dbVolume:F1} dB)");
    }

    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioManager] No Audio Mixer assigned!");
            return;
        }

        float dbVolume = VolumeToDecibels(volume);
        audioMixer.SetFloat("MusicVolume", dbVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (showDebugLogs)
            Debug.Log($"[AudioManager] Music volume set to {volume:F2} ({dbVolume:F1} dB)");
    }

    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("[AudioManager] No Audio Mixer assigned!");
            return;
        }

        float dbVolume = VolumeToDecibels(volume);
        audioMixer.SetFloat("SFXVolume", dbVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        if (showDebugLogs)
            Debug.Log($"[AudioManager] SFX volume set to {volume:F2} ({dbVolume:F1} dB)");
    }

    /// <summary>
    /// Get current master volume (0-1)
    /// </summary>
    public float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
    }

    /// <summary>
    /// Get current music volume (0-1)
    /// </summary>
    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
    }

    /// <summary>
    /// Get current SFX volume (0-1)
    /// </summary>
    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSFXVolume);
    }

    /// <summary>
    /// Load saved volume settings from PlayerPrefs
    /// </summary>
    void LoadVolumeSettings()
    {
        float masterVol = GetMasterVolume();
        float musicVol = GetMusicVolume();
        float sfxVol = GetSFXVolume();

        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);

        if (showDebugLogs)
            Debug.Log($"[AudioManager] Loaded volumes - Master: {masterVol:F2}, Music: {musicVol:F2}, SFX: {sfxVol:F2}");
    }

    /// <summary>
    /// Convert linear volume (0-1) to decibels (-80 to 0)
    /// Audio mixers use logarithmic scale
    /// </summary>
    float VolumeToDecibels(float volume)
    {
        // Clamp volume to avoid log(0)
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        
        // Convert to decibels (logarithmic)
        // -80 dB is effectively silent
        return Mathf.Log10(volume) * 20f;
    }

    /// <summary>
    /// Convert decibels to linear volume (0-1)
    /// </summary>
    float DecibelsToVolume(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }

    // ==================== MUSIC ====================

    /// <summary>
    /// Play music with crossfade from current track
    /// </summary>
    public void PlayMusic(AudioClip musicClip, bool crossfade = true)
    {
        if (musicClip == null)
        {
            Debug.LogWarning("[AudioManager] Music clip is null!");
            return;
        }

        // If same clip is already playing, do nothing
        if (currentMusicSource.clip == musicClip && currentMusicSource.isPlaying)
        {
            if (showDebugLogs) Debug.Log($"[AudioManager] {musicClip.name} already playing");
            return;
        }

        if (crossfade && currentMusicSource.isPlaying)
        {
            // Crossfade to new track
            if (showDebugLogs) Debug.Log($"[AudioManager] Crossfading to {musicClip.name}");
            StartCoroutine(CrossfadeMusic(musicClip));
        }
        else
        {
            // Play immediately
            if (showDebugLogs) Debug.Log($"[AudioManager] Playing {musicClip.name}");
            currentMusicSource.clip = musicClip;
            currentMusicSource.volume = 1f; // Mixer controls actual volume
            currentMusicSource.Play();
        }
    }

    IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        if (isCrossfading) yield break;
        isCrossfading = true;

        // Setup next source
        nextMusicSource.clip = newClip;
        nextMusicSource.volume = 0f;
        nextMusicSource.Play();

        float elapsed = 0f;

        // Crossfade
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;

            currentMusicSource.volume = Mathf.Lerp(1f, 0f, t);
            nextMusicSource.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Finalize
        currentMusicSource.Stop();
        currentMusicSource.volume = 0f;

        nextMusicSource.volume = 1f;

        // Swap sources
        AudioSource temp = currentMusicSource;
        currentMusicSource = nextMusicSource;
        nextMusicSource = temp;

        isCrossfading = false;
    }

    /// <summary>
    /// Stop music with fade out
    /// </summary>
    public void StopMusic(bool fade = true)
    {
        if (fade)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            currentMusicSource.Stop();
            currentMusicSource.volume = 0f;
        }
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = currentMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;

            currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        currentMusicSource.Stop();
        currentMusicSource.volume = 0f;
    }

    // ==================== SOUND EFFECTS ====================

    /// <summary>
    /// Play a one-shot sound effect
    /// </summary>
    public void PlaySFX(AudioClip sfxClip, float volumeMultiplier = 1f)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[AudioManager] SFX clip is null!");
            return;
        }

        sfxSource.PlayOneShot(sfxClip, volumeMultiplier);

        if (showDebugLogs) Debug.Log($"[AudioManager] Playing SFX: {sfxClip.name}");
    }

    /// <summary>
    /// Play SFX at a specific position in 3D space
    /// </summary>
    public void PlaySFXAtPosition(AudioClip sfxClip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[AudioManager] SFX clip is null!");
            return;
        }

        // Create temporary audio source at position
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        
        // Assign to mixer
        if (audioMixer != null)
        {
            tempSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
        }
        
        tempSource.clip = sfxClip;
        tempSource.volume = volumeMultiplier;
        tempSource.spatialBlend = 1f; // 3D sound
        tempSource.Play();

        // Destroy after clip finishes
        Destroy(tempGO, sfxClip.length);

        if (showDebugLogs) Debug.Log($"[AudioManager] Playing SFX at {position}: {sfxClip.name}");
    }
}