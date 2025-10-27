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
                AudioManager.Instance.PlayMusic(sceneMusic, crossfade);
                
                if (showDebugLogs) Debug.Log($"[SceneMusicLoader] Started music: {sceneMusic.name}");
            }
            else
            {
                Debug.LogError("[SceneMusicLoader] AudioManager not found in scene!");
            }
        }
    }
}
