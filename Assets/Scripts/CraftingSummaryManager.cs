using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the crafting-to-summary workflow:
/// - Enables submit button in Request panel after crafting
/// - Shows Summary Panel with points earned when submit is pressed
/// </summary>
public class CraftingSummaryManager : MonoBehaviour
{
    [Header("Request Panel")]
    [Tooltip("The submit button in the Request panel (starts disabled)")]
    public Button submitButton;
    
    [Header("Summary Panel")]
    [Tooltip("The Summary Panel to show after submitting (starts hidden)")]
    public GameObject summaryPanel;
    
    [Tooltip("Text field to display points earned")]
    public TextMeshProUGUI pointsText;
    
    [Tooltip("Alternative Text component if not using TextMeshPro")]
    public Text pointsTextLegacy;
    
    [Tooltip("Button to finish day and return to Home Island")]
    public Button finishDayButton;
    
    [Header("Scene Transition")]
    [Tooltip("Name of the Home Island scene to load")]
    public string homeIslandSceneName = "HomeIsland";
    
    [Tooltip("Use CircleTransition for smooth transition")]
    public bool useTransition = true;
    
    [Header("Points Display")]
    [Tooltip("Prefix for points display (e.g., 'Points Earned: ')")]
    public string pointsPrefix = "Points Earned: ";
    
    [Tooltip("Suffix for points display (e.g., ' pts')")]
    public string pointsSuffix = " pts";
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Crafting Reference")]
    [Tooltip("Reference to MedicineCraftingManager to get score")]
    public MedicineCraftingManager craftingManager;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Track if medicine has been crafted
    private bool hasCraftedMedicine = false;
    private int lastCraftedScore = 0;
    
    void Start()
    {
        // Initialize submit button as disabled
        if (submitButton != null)
        {
            submitButton.interactable = false;
            submitButton.onClick.AddListener(OnSubmitPressed);
        }
        
        // Initialize summary panel as hidden
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
        
        // Wire up finish day button
        if (finishDayButton != null)
        {
            finishDayButton.onClick.AddListener(FinishDay);
        }
        
        if (showDebugLogs) Debug.Log("[CraftingSummary] Initialized - Submit button disabled, Summary panel hidden");
    }
    
    /// <summary>
    /// Call this after crafting is complete to enable the submit button
    /// </summary>
    public void OnMedicineCrafted(int pointsEarned)
    {
        hasCraftedMedicine = true;
        lastCraftedScore = pointsEarned;
        
        // Enable submit button
        if (submitButton != null)
        {
            submitButton.interactable = true;
            
            if (showDebugLogs) Debug.Log($"[CraftingSummary] Medicine crafted! Submit button enabled. Points: {pointsEarned}");
        }
        
        PlaySound(sfxLibrary?.successSound);
    }
    
    /// <summary>
    /// Called when submit button is pressed
    /// </summary>
    void OnSubmitPressed()
    {
        if (!hasCraftedMedicine)
        {
            if (showDebugLogs) Debug.LogWarning("[CraftingSummary] Submit pressed but no medicine crafted!");
            PlaySound(sfxLibrary?.errorSound);
            return;
        }
        
        // Show summary panel with points
        ShowSummaryPanel(lastCraftedScore);
        
        // Disable submit button again (prevent multiple submissions)
        if (submitButton != null)
        {
            submitButton.interactable = false;
        }
        
        PlaySound(sfxLibrary?.uiSelect);
        
        if (showDebugLogs) Debug.Log($"[CraftingSummary] Submit pressed - showing summary with {lastCraftedScore} points");
    }
    
    /// <summary>
    /// Show the summary panel with the points earned
    /// </summary>
    void ShowSummaryPanel(int points)
    {
        if (summaryPanel == null)
        {
            Debug.LogError("[CraftingSummary] Summary panel not assigned!");
            return;
        }
        
        // Show panel
        summaryPanel.SetActive(true);
        
        // Update points text
        string pointsDisplay = $"{pointsPrefix}{points}{pointsSuffix}";
        
        if (pointsText != null)
        {
            pointsText.text = pointsDisplay;
        }
        else if (pointsTextLegacy != null)
        {
            pointsTextLegacy.text = pointsDisplay;
        }
        
        if (showDebugLogs) Debug.Log($"[CraftingSummary] Summary panel shown with {points} points");
    }
    
    /// <summary>
    /// Finish the day, advance to next day, and return to Home Island
    /// </summary>
    public void FinishDay()
    {
        if (showDebugLogs) Debug.Log("[CraftingSummary] Finishing day - advancing day counter and returning to Home Island");
        
        PlaySound(sfxLibrary?.uiSelect);
        
        // Advance the day in TimeSystem
        if (TimeSystem.Instance != null)
        {
            int currentDay = TimeSystem.Instance.GetCurrentDay();
            int nextDay = currentDay + 1;
            
            // Save the next day
            PlayerPrefs.SetInt("CurrentDay", nextDay);
            PlayerPrefs.Save();
            
            if (showDebugLogs) Debug.Log($"[CraftingSummary] Day advanced: {currentDay} -> {nextDay}");
        }
        else
        {
            Debug.LogWarning("[CraftingSummary] TimeSystem.Instance not found! Day counter not advanced.");
        }
        
        // Transition to Home Island
        if (useTransition && CircleTransition.Instance != null)
        {
            // Use circle transition
            CircleTransition.Instance.DoTransition(() =>
            {
                LoadHomeIsland();
            });
        }
        else
        {
            // Direct load
            LoadHomeIsland();
        }
    }
    
    /// <summary>
    /// Load the Home Island scene
    /// </summary>
    void LoadHomeIsland()
    {
        if (string.IsNullOrEmpty(homeIslandSceneName))
        {
            Debug.LogError("[CraftingSummary] Home Island scene name not set!");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"[CraftingSummary] Loading scene: {homeIslandSceneName}");
        
        SceneManager.LoadScene(homeIslandSceneName);
    }
    
    /// <summary>
    /// Close the summary panel without finishing day (for testing/debugging)
    /// </summary>
    public void CloseSummaryPanel()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
        
        // Reset state for next craft
        hasCraftedMedicine = false;
        lastCraftedScore = 0;
        
        PlaySound(sfxLibrary?.uiCancel);
        
        if (showDebugLogs) Debug.Log("[CraftingSummary] Summary panel closed");
    }
    
    /// <summary>
    /// Reset the system (useful for testing or new day)
    /// </summary>
    public void ResetState()
    {
        hasCraftedMedicine = false;
        lastCraftedScore = 0;
        
        if (submitButton != null)
        {
            submitButton.interactable = false;
        }
        
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
        
        if (showDebugLogs) Debug.Log("[CraftingSummary] State reset");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getters
    public bool HasCraftedMedicine() => hasCraftedMedicine;
    public int GetLastCraftedScore() => lastCraftedScore;
}
