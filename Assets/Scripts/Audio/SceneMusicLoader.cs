using UnityEngine;

/// <summary>
/// Automatically plays music when scene loads.
/// Attach to any GameObject in the scene (one per scene).
/// AudioManager handles crossfading automatically.
/// </summary>
public class SceneMusicLoader : MonoBehaviour
{
    [Header("Scene Music")]
    [Tooltip("Music to play in this scene")]
    public AudioClip sceneMusic;
    
    [Tooltip("Volume for this music (0-1)")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    
    [Tooltip("Crossfade from previous scene music")]
    public bool crossfade = true;
    
    [Tooltip("Start music immediately on scene load")]
    public bool playOnStart = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Start()
    {
        if (playOnStart && sceneMusic != null)
        {
            if (AudioManager.Instance != null)
            {
                // Set volume for this scene
                AudioManager.Instance.SetMusicVolume(musicVolume);
                
                // Play music
                AudioManager.Instance.PlayMusic(sceneMusic, crossfade);
                
                if (showDebugLogs) Debug.Log($"[SceneMusicLoader] Started music: {sceneMusic.name} at volume {musicVolume}");
            }
            else
            {
                Debug.LogError("[SceneMusicLoader] AudioManager not found in scene!");
            }
        }
    }
}