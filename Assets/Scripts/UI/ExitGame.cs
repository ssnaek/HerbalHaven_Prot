using UnityEngine;

/// <summary>
/// Simple script to quit the entire game application.
/// Attach to a button or call QuitApplication() from any script.
/// Works in both editor and builds.
/// </summary>
public class QuitGame : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to quit the game (optional)")]
    public KeyCode quitKey = KeyCode.Escape;
    
    [Tooltip("Enable keyboard quit (Esc key)")]
    public bool allowKeyboardQuit = false;
    
    [Tooltip("Show confirmation before quitting")]
    public bool showConfirmation = false;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool confirmationPending = false;

    void Update()
    {
        if (allowKeyboardQuit && Input.GetKeyDown(quitKey))
        {
            if (showConfirmation)
            {
                if (!confirmationPending)
                {
                    confirmationPending = true;
                    if (showDebugLogs) Debug.Log("[QuitGame] Press Escape again to confirm quit");
                }
                else
                {
                    QuitApplication();
                }
            }
            else
            {
                QuitApplication();
            }
        }
    }

    /// <summary>
    /// Quit the game application. Call this from a UI button.
    /// </summary>
    public void QuitApplication()
    {
        if (showDebugLogs) Debug.Log("[QuitGame] Quitting application...");

        #if UNITY_EDITOR
        // Stop playing in editor
        UnityEditor.EditorApplication.isPlaying = false;
        if (showDebugLogs) Debug.Log("[QuitGame] Stopped editor playmode");
        #else
        // Quit the application in build
        Application.Quit();
        if (showDebugLogs) Debug.Log("[QuitGame] Application.Quit() called");
        #endif
    }

    /// <summary>
    /// Quit with optional delay (useful for fade-out effects).
    /// </summary>
    public void QuitApplicationDelayed(float delaySeconds)
    {
        if (showDebugLogs) Debug.Log($"[QuitGame] Quitting in {delaySeconds} seconds...");
        
        Invoke(nameof(QuitApplication), delaySeconds);
    }
}