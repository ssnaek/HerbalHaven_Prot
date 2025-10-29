using UnityEngine;
using System.Collections;

/// <summary>
/// Manages sliding menu panels (Load, Settings, etc.)
/// Panels slide up and deactivate, new panels slide up from below.
/// Attach to Canvas or empty GameObject.
/// </summary>
public class MenuPanelManager : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("The main menu panel with buttons")]
    public RectTransform mainMenuPanel;
    
    [Tooltip("Load game panel")]
    public RectTransform loadGamePanel;
    
    [Tooltip("Settings panel")]
    public RectTransform settingsPanel;
    
    [Tooltip("Credits panel")]
    public RectTransform creditsPanel;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Positions")]
    [Tooltip("How far below screen panels start")]
    public float belowScreenOffset = -1500f;
    
    [Tooltip("How far above screen panels move out")]
    public float aboveScreenOffset = 1500f;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private RectTransform currentPanel;
    private bool isAnimating = false;
    
    // Store original Y positions
    private float mainMenuOriginalY;
    private float loadGameOriginalY;
    private float settingsOriginalY;
    private float creditsOriginalY;

    void Start()
    {
        // Store original Y positions
        if (mainMenuPanel != null) mainMenuOriginalY = mainMenuPanel.anchoredPosition.y;
        if (loadGamePanel != null) loadGameOriginalY = loadGamePanel.anchoredPosition.y;
        if (settingsPanel != null) settingsOriginalY = settingsPanel.anchoredPosition.y;
        if (creditsPanel != null) creditsOriginalY = creditsPanel.anchoredPosition.y;

        // Main menu starts visible at its position
        if (mainMenuPanel != null)
        {
            mainMenuPanel.gameObject.SetActive(true);
            currentPanel = mainMenuPanel;
        }

        // All other panels start hidden below screen
        if (loadGamePanel != null)
        {
            SetPanelYPosition(loadGamePanel, belowScreenOffset);
            loadGamePanel.gameObject.SetActive(false);
        }
        if (settingsPanel != null)
        {
            SetPanelYPosition(settingsPanel, belowScreenOffset);
            settingsPanel.gameObject.SetActive(false);
        }
        if (creditsPanel != null)
        {
            SetPanelYPosition(creditsPanel, belowScreenOffset);
            creditsPanel.gameObject.SetActive(false);
        }

        if (showDebugLogs) Debug.Log("[MenuPanelManager] Initialized");
    }

    /// <summary>
    /// Show load game panel
    /// </summary>
    public void ShowLoadGamePanel()
    {
        if (loadGamePanel == null)
        {
            Debug.LogWarning("[MenuPanelManager] Load game panel not assigned!");
            return;
        }

        PlaySound(sfxLibrary?.uiSelect);
        SwitchToPanel(loadGamePanel, loadGameOriginalY);
    }

    /// <summary>
    /// Show settings panel
    /// </summary>
    public void ShowSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("[MenuPanelManager] Settings panel not assigned!");
            return;
        }

        PlaySound(sfxLibrary?.uiSelect);
        SwitchToPanel(settingsPanel, settingsOriginalY);
    }

    /// <summary>
    /// Show credits panel
    /// </summary>
    public void ShowCreditsPanel()
    {
        if (creditsPanel == null)
        {
            Debug.LogWarning("[MenuPanelManager] Credits panel not assigned!");
            return;
        }

        PlaySound(sfxLibrary?.uiSelect);
        SwitchToPanel(creditsPanel, creditsOriginalY);
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel == null)
        {
            Debug.LogWarning("[MenuPanelManager] Main menu panel not assigned!");
            return;
        }

        PlaySound(sfxLibrary?.uiCancel);
        SwitchToPanel(mainMenuPanel, mainMenuOriginalY);
    }

    void SwitchToPanel(RectTransform newPanel, float newPanelTargetY)
    {
        if (isAnimating || newPanel == currentPanel) return;

        if (showDebugLogs) Debug.Log($"[MenuPanelManager] Switching from {currentPanel.name} to {newPanel.name}");

        StartCoroutine(SlideUpTransition(currentPanel, newPanel, newPanelTargetY));
    }

    IEnumerator SlideUpTransition(RectTransform oldPanel, RectTransform newPanel, float newPanelTargetY)
    {
        isAnimating = true;

        // Activate new panel and position it below screen
        newPanel.gameObject.SetActive(true);
        SetPanelYPosition(newPanel, belowScreenOffset);

        // Get starting positions
        Vector2 oldStartPos = oldPanel.anchoredPosition;
        Vector2 oldTargetPos = new Vector2(oldStartPos.x, aboveScreenOffset);
        
        Vector2 newStartPos = newPanel.anchoredPosition;
        Vector2 newTargetPos = new Vector2(newStartPos.x, newPanelTargetY);

        float elapsed = 0f;

        // Animate both panels
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float curveT = slideCurve.Evaluate(t);

            // Old panel slides up
            oldPanel.anchoredPosition = Vector2.Lerp(oldStartPos, oldTargetPos, curveT);
            
            // New panel slides up from below
            newPanel.anchoredPosition = Vector2.Lerp(newStartPos, newTargetPos, curveT);

            yield return null;
        }

        // Finalize positions
        oldPanel.anchoredPosition = oldTargetPos;
        newPanel.anchoredPosition = newTargetPos;

        // Deactivate old panel
        oldPanel.gameObject.SetActive(false);

        currentPanel = newPanel;
        isAnimating = false;

        if (showDebugLogs) Debug.Log($"[MenuPanelManager] Transition complete - now showing {newPanel.name}");
    }

    void SetPanelYPosition(RectTransform panel, float yPosition)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.y = yPosition;
        panel.anchoredPosition = pos;
    }

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}