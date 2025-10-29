using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple main menu controller.
/// Delegates panel switching to MenuPanelManager.
/// Attach to Canvas or empty GameObject.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the game scene to load")]
    public string gameSceneName = "Herb_Island";

    [Header("Panel Manager")]
    [Tooltip("Reference to MenuPanelManager (auto-finds if empty)")]
    public MenuPanelManager panelManager;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = true;

    void Start()
    {
        // Auto-find MenuPanelManager if not assigned
        if (panelManager == null)
        {
            panelManager = FindObjectOfType<MenuPanelManager>();
        }

        if (showDebugLogs) Debug.Log("[MainMenu] Menu controller initialized");
    }

    // ========== MAIN MENU BUTTONS ==========

    /// <summary>
    /// Start new game - loads game scene directly
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("=== START NEW GAME CALLED ==="); // ALWAYS logs, regardless of showDebugLogs
        
        if (showDebugLogs) Debug.Log($"[MainMenu] StartNewGame called! Loading: {gameSceneName}");

        PlaySound(sfxLibrary?.uiConfirm);

        // Use circle transition if available
        if (CircleTransition.Instance != null)
        {
            Debug.Log("CircleTransition found, using transition");
            CircleTransition.Instance.DoTransition(() =>
            {
                Debug.Log("Inside transition callback, loading scene");
                SceneManager.LoadScene(gameSceneName);
            });
        }
        else
        {
            Debug.Log("No CircleTransition, loading directly");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Open load game panel
    /// </summary>
    public void OpenLoadPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Load panel");

        if (panelManager != null)
        {
            panelManager.ShowLoadGamePanel();
        }
        else
        {
            Debug.LogError("[MainMenu] MenuPanelManager not found!");
        }
    }

    /// <summary>
    /// Open settings panel
    /// </summary>
    public void OpenSettingsPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Settings panel");

        if (panelManager != null)
        {
            panelManager.ShowSettingsPanel();
        }
        else
        {
            Debug.LogError("[MainMenu] MenuPanelManager not found!");
        }
    }

    /// <summary>
    /// Open credits panel (scrolldown)
    /// </summary>
    public void OpenCreditsPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Credits panel");

        if (panelManager != null)
        {
            panelManager.ShowCreditsPanel();
        }
        else
        {
            Debug.LogError("[MainMenu] MenuPanelManager not found!");
        }
    }

    /// <summary>
    /// Quit game - can be called from main menu or extra panel
    /// </summary>
    public void QuitGame()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] QuitGame called");

        PlaySound(sfxLibrary?.uiSelect);

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // ========== SUB-PANEL BUTTONS ==========

    /// <summary>
    /// Return to main menu from any sub-panel
    /// </summary>
    public void BackToMainMenu()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Returning to main menu");

        if (panelManager != null)
        {
            panelManager.ShowMainMenu();
        }
    }

    // ========== HELPERS ==========

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}