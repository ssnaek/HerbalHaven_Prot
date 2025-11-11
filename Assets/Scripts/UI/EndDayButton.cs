using UnityEngine;
using UnityEngine.UI;

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
        
        // Check initial button state
        UpdateButtonState();
    }
    
    void Update()
    {
        // Continuously check if player has enough herbs to enable button
        UpdateButtonState();
    }
    
    /// <summary>
    /// Update button interactable state based on herb count
    /// </summary>
    void UpdateButtonState()
    {
        if (endDayButton == null) return;
        
        int herbCount = GetTotalHerbCount();
        bool canEndDay = herbCount >= minimumHerbsRequired;
        
        endDayButton.interactable = canEndDay;
        
        if (showDebugLogs && endDayButton.interactable != canEndDay)
        {
            Debug.Log($"[EndDayButton] Button state: {(canEndDay ? "ENABLED" : "DISABLED")} (Herbs: {herbCount}/{minimumHerbsRequired})");
        }
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
    /// Called when button is clicked
    /// </summary>
    void OnEndDayButtonClicked()
    {
        if (showDebugLogs) Debug.Log("[EndDayButton] End Day button clicked");
        
        // Double-check that player has enough herbs
        int herbCount = GetTotalHerbCount();
        if (herbCount < minimumHerbsRequired)
        {
            Debug.LogWarning($"[EndDayButton] Cannot end day - need {minimumHerbsRequired} herbs, only have {herbCount}");
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
