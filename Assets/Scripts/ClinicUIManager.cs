using UnityEngine;
using System.Collections;

/// <summary>
/// Horizontal sliding between Counter and Crafting panels.
/// Counter → Crafting: Counter slides left, Crafting slides in from right
/// Crafting → Counter: Crafting slides right, Counter slides in from left
/// </summary>
public class ClinicPanelManager : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("Counter panel - starts visible")]
    public RectTransform counterPanel;

    [Tooltip("Crafting panel - starts hidden right")]
    public RectTransform craftingPanel;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Positions")]
    [Tooltip("Screen width - how far panels slide")]
    public float screenWidth = 1920f;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool showingCrafting = false;
    private bool isAnimating = false;

    void Start()
    {
        // Counter starts visible at center (X: 0)
        if (counterPanel != null)
        {
            SetPanelXPosition(counterPanel, 0f);
            counterPanel.gameObject.SetActive(true);
        }

        // Crafting starts hidden to the right
        if (craftingPanel != null)
        {
            SetPanelXPosition(craftingPanel, screenWidth);
            craftingPanel.gameObject.SetActive(true);
        }

        if (showDebugLogs) Debug.Log("[ClinicPanelManager] Initialized - Counter at X:0, Crafting at X:1920");
    }

    /// <summary>
    /// Toggle between counter and crafting
    /// Also handles returning from garden view to crafting (workstation)
    /// </summary>
    public void ToggleView()
    {
        if (isAnimating) return;
        
        // Check if garden UI is showing - if so, return to crafting (workstation)
        GardenUIManager gardenManager = FindObjectOfType<GardenUIManager>();
        if (gardenManager != null && gardenManager.IsShowingGarden())
        {
            // Show counter UI first (which enables all counter/crafting panels)
            gardenManager.ShowCounterUI();
            // Then show crafting view
            ShowCrafting();
            return;
        }

        // Normal counter/crafting toggle
        if (showingCrafting)
            ShowCounter();
        else
            ShowCrafting();
    }

    /// <summary>
    /// Counter → Crafting: Counter slides LEFT, Crafting slides in from RIGHT
    /// </summary>
    public void ShowCrafting()
    {
        if (craftingPanel == null || isAnimating || showingCrafting) return;

        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(SlideToCrafting());

        if (showDebugLogs) Debug.Log("[ClinicPanelManager] Counter → Crafting");
    }

    /// <summary>
    /// Crafting → Counter: Crafting slides RIGHT, Counter slides in from LEFT
    /// </summary>
    public void ShowCounter()
    {
        if (counterPanel == null || isAnimating || !showingCrafting) return;

        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(SlideToCounter());

        if (showDebugLogs) Debug.Log("[ClinicPanelManager] Crafting → Counter");
    }

    IEnumerator SlideToCrafting()
    {
        isAnimating = true;
        showingCrafting = true;

        // Counter: X:0 → X:-screenWidth (slides LEFT off-screen)
        Vector2 counterStart = counterPanel.anchoredPosition;
        Vector2 counterTarget = new Vector2(-screenWidth, counterStart.y);

        // Crafting: X:screenWidth → X:0 (slides in from RIGHT)
        Vector2 craftingStart = craftingPanel.anchoredPosition;
        Vector2 craftingTarget = new Vector2(0f, craftingStart.y);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideCurve.Evaluate(elapsed / slideDuration);

            counterPanel.anchoredPosition = Vector2.Lerp(counterStart, counterTarget, t);
            craftingPanel.anchoredPosition = Vector2.Lerp(craftingStart, craftingTarget, t);

            yield return null;
        }

        // Finalize positions
        counterPanel.anchoredPosition = counterTarget;
        craftingPanel.anchoredPosition = craftingTarget;

        isAnimating = false;

        if (showDebugLogs) Debug.Log("[ClinicPanelManager] Crafting now visible at X:0");
    }

    IEnumerator SlideToCounter()
    {
        isAnimating = true;
        showingCrafting = false;

        // Crafting: X:0 → X:screenWidth (slides RIGHT off-screen)
        Vector2 craftingStart = craftingPanel.anchoredPosition;
        Vector2 craftingTarget = new Vector2(screenWidth, craftingStart.y);

        // Counter: X:-screenWidth → X:0 (slides in from LEFT)
        Vector2 counterStart = counterPanel.anchoredPosition;
        Vector2 counterTarget = new Vector2(0f, counterStart.y);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = slideCurve.Evaluate(elapsed / slideDuration);

            craftingPanel.anchoredPosition = Vector2.Lerp(craftingStart, craftingTarget, t);
            counterPanel.anchoredPosition = Vector2.Lerp(counterStart, counterTarget, t);

            yield return null;
        }

        // Finalize positions
        craftingPanel.anchoredPosition = craftingTarget;
        counterPanel.anchoredPosition = counterTarget;

        isAnimating = false;

        if (showDebugLogs) Debug.Log("[ClinicPanelManager] Counter now visible at X:0");
    }

    void SetPanelXPosition(RectTransform panel, float xPosition)
    {
        Vector2 pos = panel.anchoredPosition;
        pos.x = xPosition;
        panel.anchoredPosition = pos;
    }

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    // Public getters
    public bool IsAnimating() => isAnimating;
    public bool IsShowingCrafting() => showingCrafting;
}