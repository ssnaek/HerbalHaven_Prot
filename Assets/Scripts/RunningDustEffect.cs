using UnityEngine;

/// <summary>
/// Controls running dust particle effect based on player speed.
/// Attach to Player GameObject.
/// </summary>
public class RunningDustEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The particle system at player's feet")]
    public ParticleSystem dustParticles;
    
    [Tooltip("Player controller to check speed")]
    public PlayerController playerController;

    [Header("Settings")]
    [Tooltip("Speed threshold to trigger dust (should be run speed)")]
    public float runSpeedThreshold = 5.5f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    public float footstepInterval = 0.4f;

    private float footstepTimer = 0f;
    private bool isPlayingDust = false;

    void Start()
    {
        // Auto-find if not assigned
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (dustParticles == null)
            dustParticles = GetComponentInChildren<ParticleSystem>();

        // Validate
        if (playerController == null)
        {
            Debug.LogError("[RunningDustEffect] PlayerController not found!");
            enabled = false;
            return;
        }

        if (dustParticles == null)
        {
            Debug.LogError("[RunningDustEffect] Particle System not found!");
            enabled = false;
            return;
        }

        // Make sure particles start stopped
        if (dustParticles.isPlaying)
            dustParticles.Stop();
    }

    void Update()
    {
        float currentSpeed = playerController.CurrentSpeed;

        // Player is running fast enough
        if (currentSpeed >= runSpeedThreshold)
        {
            if (!isPlayingDust)
            {
                dustParticles.Play();
                isPlayingDust = true;

                if (showDebugLogs) Debug.Log("[RunningDust] Started dust particles");
            }

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= footstepInterval)
            {
                if (AudioManager.Instance != null && sfxLibrary != null)
                {
                    Debug.Log("Playin audio");
                    AudioManager.Instance.PlaySFX(sfxLibrary.footstepRun, 0.6f);
                }
                footstepTimer = 0f;
            }

        }
        else
        {
            if (isPlayingDust)
            {
                dustParticles.Stop();
                isPlayingDust = false;

                if (showDebugLogs) Debug.Log("[RunningDust] Stopped dust particles");
            }

            footstepTimer = 0f;
        }
    }
}
