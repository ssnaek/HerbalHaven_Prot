using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Button to instantly end the day and move to clinic scene.
/// Sets time to 10:00 AM and triggers day end transition.
/// Useful for testing and for players who finish collecting early.
/// Attach to a button in your UI.
/// </summary>
public class EndDayButton : MonoBehaviour
{
    [Header("Button")]
    [Tooltip("Button component (auto-finds if empty)")]
    public Button endDayButton;
    
    [Header("Warning Panel")]
    [Tooltip("Panel to show when player doesn't have enough herbs")]
    public GameObject warningPanel;
    
    [Tooltip("Text component for the warning message")]
    public TextMeshProUGUI warningText;
    
    [Tooltip("Warning message to display")]
    public string warningMessage = "You need at least 3 plants foraged to end the day!";
    
    [Tooltip("Time to display each character (typewriter effect)")]
    public float characterDelay = 0.05f;
    
    [Tooltip("Time to keep panel visible after text finishes")]
    public float panelDisplayDuration = 5f;

    [Header("Settings")]
    [Tooltip("Show confirmation before ending day")]
    public bool requireConfirmation = false;
    
    [Tooltip("Confirmation message (if enabled)")]
    public string confirmationMessage = "End collecting and move to clinic?";
    
    [Tooltip("Minimum number of herbs required to end day")]
    public int minimumHerbsRequired = 3;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private bool isShowingWarning = false;
    private Coroutine warningCoroutine;

    void Start()
    {
        // Auto-find button
        if (endDayButton == null)
        {
            endDayButton = GetComponent<Button>();
        }

        // Wire up button
        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(OnEndDayButtonClicked);
        }
        else
        {
            Debug.LogError("[EndDayButton] No button component found!");
        }
        
        // Hide warning panel initially
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update button interactable state based on whether warning is showing
    /// </summary>
    void Update()
    {
        // Disable button while warning is showing to prevent spam
        if (endDayButton != null)
        {
            endDayButton.interactable = !isShowingWarning;
        }
    }

    /// <summary>
    /// Called when button is clicked
    /// </summary>
    void OnEndDayButtonClicked()
    {
        if (showDebugLogs) Debug.Log("[EndDayButton] End Day button clicked");
        
        // Check if player has enough herbs
        int herbCount = GetTotalHerbCount();
        if (herbCount < minimumHerbsRequired)
        {
            Debug.Log($"[EndDayButton] Not enough herbs - showing warning (have {herbCount}, need {minimumHerbsRequired})");
            ShowWarningPanel();
            return;
        }

        PlaySound(sfxLibrary?.uiSelect);

        if (requireConfirmation)
        {
            // Show confirmation (you can implement a popup if desired)
            if (showDebugLogs) Debug.Log($"[EndDayButton] Confirmation: {confirmationMessage}");
            
            // For now, just end day directly
            // TODO: Implement confirmation popup if needed
            EndDayNow();
        }
        else
        {
            EndDayNow();
        }
    }
    
    /// <summary>
    /// Show warning panel with typewriter effect
    /// </summary>
    void ShowWarningPanel()
    {
        if (warningPanel == null)
        {
            Debug.LogWarning("[EndDayButton] Warning panel not assigned!");
            return;
        }
        
        if (warningText == null)
        {
            Debug.LogWarning("[EndDayButton] Warning text not assigned!");
            return;
        }
        
        // Stop any existing warning coroutine
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        
        // Start new warning coroutine
        warningCoroutine = StartCoroutine(ShowWarningCoroutine());
    }
    
    /// <summary>
    /// Coroutine to display warning with typewriter effect
    /// </summary>
    IEnumerator ShowWarningCoroutine()
    {
        isShowingWarning = true;
        
        // Show panel
        warningPanel.SetActive(true);
        
        // Clear text
        warningText.text = "";
        
        // Typewriter effect
        foreach (char c in warningMessage)
        {
            warningText.text += c;
            yield return new WaitForSeconds(characterDelay);
        }
        
        // Wait for display duration
        yield return new WaitForSeconds(panelDisplayDuration);
        
        // Hide panel
        warningPanel.SetActive(false);
        
        isShowingWarning = false;
        warningCoroutine = null;
    }
    
    /// <summary>
    /// Count total herbs (not seeds) in inventory
    /// </summary>
    int GetTotalHerbCount()
    {
        if (InventorySystem.Instance == null)
        {
            return 0;
        }
        
        int totalHerbs = 0;
        var items = InventorySystem.Instance.GetAllItems();
        
        foreach (var item in items)
        {
            // Only count items that have herbData (herbs, not seeds)
            if (item.herbData != null)
            {
                totalHerbs += item.quantity;
            }
        }
        
        return totalHerbs;
    }

    /// <summary>
    /// End the day immediately
    /// </summary>
    void EndDayNow()
    {
        if (TimeSystem.Instance == null)
        {
            Debug.LogError("[EndDayButton] TimeSystem.Instance is null!");
            return;
        }

        if (showDebugLogs) Debug.Log("[EndDayButton] Ending day now...");

        PlaySound(sfxLibrary?.uiConfirm);

        // Force time to 10:00 AM and trigger day end
        TimeSystem.Instance.ForceEndDay();
    }

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    void OnDestroy()
    {
        // Cleanup button listener
        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(OnEndDayButtonClicked);
        }
    }
}
