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
    }

    /// <summary>
    /// Called when button is clicked
    /// </summary>
    void OnEndDayButtonClicked()
    {
        if (showDebugLogs) Debug.Log("[EndDayButton] End Day button clicked");

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
