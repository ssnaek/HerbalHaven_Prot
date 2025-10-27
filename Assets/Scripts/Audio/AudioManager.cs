using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized audio manager for music and sound effects.
/// Handles music crossfading between scenes and SFX playback.
/// Singleton - persists across scenes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [Tooltip("Two audio sources for crossfading")]
    public AudioSource musicSource1;
    public AudioSource musicSource2;
    
    [Tooltip("Default music volume")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    
    [Tooltip("Crossfade duration (seconds)")]
    public float crossfadeDuration = 2f;

    [Header("Sound Effects")]
    [Tooltip("Audio source for one-shot SFX")]
    public AudioSource sfxSource;
    
    [Tooltip("Default SFX volume")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private AudioSource currentMusicSource;
    private AudioSource nextMusicSource;
    private bool isCrossfading = false;

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
        musicSource1.volume = 0f;

        musicSource2.loop = true;
        musicSource2.playOnAwake = false;
        musicSource2.volume = 0f;

        // Configure SFX source
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        // Start with source1 as current
        currentMusicSource = musicSource1;
        nextMusicSource = musicSource2;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showDebugLogs) Debug.Log($"[AudioManager] Scene loaded: {scene.name}");
        
        // Auto-play music for scene (you can set this up per scene)
        // For now, this is just a hook - you'll call PlayMusic() manually
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
            currentMusicSource.volume = musicVolume;
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

            currentMusicSource.volume = Mathf.Lerp(musicVolume, 0f, t);
            nextMusicSource.volume = Mathf.Lerp(0f, musicVolume, t);

            yield return null;
        }

        // Finalize
        currentMusicSource.Stop();
        currentMusicSource.volume = 0f;

        nextMusicSource.volume = musicVolume;

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

    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        currentMusicSource.volume = musicVolume;
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

        sfxSource.PlayOneShot(sfxClip, sfxVolume * volumeMultiplier);

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

        AudioSource.PlayClipAtPoint(sfxClip, position, sfxVolume * volumeMultiplier);

        if (showDebugLogs) Debug.Log($"[AudioManager] Playing SFX at {position}: {sfxClip.name}");
    }

    /// <summary>
    /// Set SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
    }
}
