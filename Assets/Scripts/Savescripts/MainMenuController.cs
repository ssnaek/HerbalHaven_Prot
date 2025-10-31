using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller with save system integration.
/// Works with MenuPanelManager for panel sliding animations.
/// Attach to Canvas or empty GameObject.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the home island scene")]
    public string homeIslandSceneName = "HomeIsland";

    [Header("Panel Manager")]
    [Tooltip("Reference to MenuPanelManager (auto-finds if empty)")]
    public MenuPanelManager panelManager;

    [Header("Save System")]
    [Tooltip("New save popup controller (overlay popup, not a sliding panel)")]
    public NewSavePopup newSavePopup;

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

        // Auto-find NewSavePopup
        if (newSavePopup == null)
        {
            newSavePopup = FindObjectOfType<NewSavePopup>();
        }

        if (showDebugLogs) Debug.Log("[MainMenu] Menu controller initialized");
    }

    // ========== MAIN MENU BUTTONS ==========

    /// <summary>
    /// Play New Save - toggles popup to name save
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("=== START NEW GAME CALLED ===");
        
        if (showDebugLogs) Debug.Log($"[MainMenu] StartNewGame called - toggling new save popup");

        PlaySound(sfxLibrary?.uiConfirm);

        // Toggle new save popup (this is an overlay, not a sliding panel)
        if (newSavePopup != null)
        {
            // Check if popup is currently active
            if (newSavePopup.popupPanel != null && newSavePopup.popupPanel.activeSelf)
            {
                // Popup is open - close it
                newSavePopup.HidePopup();
                if (showDebugLogs) Debug.Log("[MainMenu] Closing new save popup");
            }
            else
            {
                // Popup is closed - open it
                newSavePopup.ShowPopup();
                if (showDebugLogs) Debug.Log("[MainMenu] Opening new save popup");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] NewSavePopup not found! Cannot create new save.");
        }
    }

    /// <summary>
    /// Load Game - uses MenuPanelManager to slide to Load Game panel
    /// </summary>
    public void OpenLoadPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Load panel");

        PlaySound(sfxLibrary?.uiSelect);

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
    /// Settings - uses MenuPanelManager to slide to Settings panel
    /// </summary>
    public void OpenSettingsPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Settings panel");

        PlaySound(sfxLibrary?.uiSelect);

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
    /// Credits - uses MenuPanelManager to slide to Credits panel
    /// </summary>
    public void OpenCreditsPanel()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Opening Credits panel");

        PlaySound(sfxLibrary?.uiSelect);

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
    /// Return to main menu from any sub-panel (uses MenuPanelManager)
    /// </summary>
    public void BackToMainMenu()
    {
        if (showDebugLogs) Debug.Log("[MainMenu] Returning to main menu");

        PlaySound(sfxLibrary?.uiCancel);

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