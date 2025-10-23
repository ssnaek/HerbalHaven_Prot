using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Slide-in menu panel from right side of screen.
/// In MainMenu: Static (always visible)
/// In Game: Slides in/out with ESC key
/// </summary>
public class SlideMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene to load when 'Play New Save' is clicked")]
    public string gameSceneName = "Herb_Island";
    
    [Tooltip("Is this the main menu scene? (menu stays open)")]
    public bool isMainMenuScene = true;

    [Header("UI References")]
    public RectTransform menuPanel;
    public Button playNewSaveButton;
    public Button loadButton;
    public Button settingsButton;
    public Button extraButton; // Credits, Quit, etc.

    [Header("Slide Animation")]
    [Tooltip("Only used if NOT main menu scene")]
    public float slideDuration = 0.3f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Menu Positions")]
    [Tooltip("X position when open (0 = at screen edge)")]
    public float openPositionX = 0f;
    
    [Tooltip("X position when closed (off-screen right)")]
    public float closedPositionX = 500f; // Adjust based on panel width

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector2 openPosition;
    private Vector2 closedPosition;

    void Start()
    {
        SetupButtons();
        InitializeMenu();
    }

    void SetupButtons()
    {
        if (playNewSaveButton != null)
            playNewSaveButton.onClick.AddListener(OnPlayNewSaveClicked);

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(OnLoadClicked);
            // Disable for now (no functionality yet)
            loadButton.interactable = false;
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            // Disable for now (no functionality yet)
            settingsButton.interactable = false;
        }

        if (extraButton != null)
            extraButton.onClick.AddListener(OnExtraButtonClicked);
    }

    void InitializeMenu()
    {
        if (menuPanel == null)
        {
            Debug.LogError("[SlideMenu] Menu panel not assigned!");
            return;
        }

        // Calculate positions
        openPosition = new Vector2(openPositionX, menuPanel.anchoredPosition.y);
        closedPosition = new Vector2(closedPositionX, menuPanel.anchoredPosition.y);

        if (isMainMenuScene)
        {
            // Main menu: always open, static
            menuPanel.anchoredPosition = openPosition;
            isOpen = true;
            
            if (showDebugLogs) Debug.Log("[SlideMenu] Main menu - panel set to open position");
        }
        else
        {
            // Game scene: start closed
            menuPanel.anchoredPosition = closedPosition;
            isOpen = false;
            
            if (showDebugLogs) Debug.Log("[SlideMenu] Game scene - panel set to closed position");
        }
    }

    void Update()
    {
        // Only handle ESC key in game scenes (not main menu)
        if (!isMainMenuScene && Input.GetKeyDown(toggleKey))
        {
            if (!isAnimating)
            {
                ToggleMenu();
            }
        }
    }

    public void ToggleMenu()
    {
        if (isMainMenuScene)
        {
            if (showDebugLogs) Debug.Log("[SlideMenu] Cannot toggle in main menu scene");
            return;
        }

        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isOpen || isAnimating) return;

        if (showDebugLogs) Debug.Log("[SlideMenu] Opening menu");

        StartCoroutine(SlideToPosition(openPosition, true));
    }

    public void CloseMenu()
    {
        if (!isOpen || isAnimating) return;

        if (showDebugLogs) Debug.Log("[SlideMenu] Closing menu");

        StartCoroutine(SlideToPosition(closedPosition, false));
    }

    IEnumerator SlideToPosition(Vector2 targetPosition, bool opening)
    {
        isAnimating = true;
        Vector2 startPosition = menuPanel.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled in case game is paused
            float t = elapsed / slideDuration;
            float curveT = slideCurve.Evaluate(t);

            menuPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, curveT);

            yield return null;
        }

        menuPanel.anchoredPosition = targetPosition;
        isOpen = opening;
        isAnimating = false;

        if (showDebugLogs) Debug.Log($"[SlideMenu] Animation complete - isOpen: {isOpen}");
    }

    // ========== BUTTON CALLBACKS ==========

    void OnPlayNewSaveClicked()
    {
        if (showDebugLogs) Debug.Log($"[SlideMenu] Play New Save clicked - loading {gameSceneName}");

        // TODO: Reset save data when implemented
        
        LoadScene(gameSceneName);
    }

    void OnLoadClicked()
    {
        if (showDebugLogs) Debug.Log("[SlideMenu] Load clicked (not implemented yet)");
        
        // TODO: Show load save UI
        // TODO: Load selected save
    }

    void OnSettingsClicked()
    {
        if (showDebugLogs) Debug.Log("[SlideMenu] Settings clicked (not implemented yet)");
        
        // TODO: Open settings panel
    }

    void OnExtraButtonClicked()
    {
        if (showDebugLogs) Debug.Log("[SlideMenu] Extra button clicked");
        
        // Could be: Credits, Quit, How to Play, etc.
        // For now, let's make it quit in game scenes
        if (!isMainMenuScene)
        {
            // Resume game and close menu
            CloseMenu();
        }
        else
        {
            // In main menu, quit application
            QuitGame();
        }
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void QuitGame()
    {
        if (showDebugLogs) Debug.Log("[SlideMenu] Quitting game");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ========== PUBLIC METHODS ==========

    public bool IsOpen() => isOpen;
    public bool IsAnimating() => isAnimating;
}