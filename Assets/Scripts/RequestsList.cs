using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the Requests List panel in the Counter UI.
/// Shows/hides the requests panel when button is pressed.
/// </summary>
public class RequestsList : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The panel to show/hide when button is pressed")]
    public GameObject requestsPanel;
    
    [Tooltip("Button to toggle the requests panel")]
    public Button toggleButton;
    
    [Header("Illness Selection")]
    [Tooltip("Reference to IllnessSelector to pick daily illnesses")]
    public IllnessSelector illnessSelector;
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private bool isPanelVisible = false;
    
    void Start()
    {
        // Wire up button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
        
        // Initialize panel as hidden
        if (requestsPanel != null)
        {
            requestsPanel.SetActive(false);
            isPanelVisible = false;
        }
        
        // Select daily illnesses on start
        if (illnessSelector != null)
        {
            illnessSelector.SelectDailyIllnesses();
            if (showDebugLogs) Debug.Log("[RequestsList] Daily illnesses selected on start");
        }
        
        if (showDebugLogs) Debug.Log("[RequestsList] Initialized");
    }
    
    /// <summary>
    /// Toggle the visibility of the requests panel
    /// </summary>
    public void TogglePanel()
    {
        if (requestsPanel == null)
        {
            Debug.LogError("[RequestsList] Requests panel not assigned!");
            return;
        }
        
        isPanelVisible = !isPanelVisible;
        requestsPanel.SetActive(isPanelVisible);
        
        PlaySound(isPanelVisible ? sfxLibrary?.uiSelect : sfxLibrary?.uiCancel);
        
        if (showDebugLogs) Debug.Log($"[RequestsList] Panel {(isPanelVisible ? "shown" : "hidden")}");
    }
    
    /// <summary>
    /// Show the requests panel
    /// </summary>
    public void ShowPanel()
    {
        if (requestsPanel == null) return;
        
        isPanelVisible = true;
        requestsPanel.SetActive(true);
        
        PlaySound(sfxLibrary?.uiSelect);
        
        if (showDebugLogs) Debug.Log("[RequestsList] Panel shown");
    }
    
    /// <summary>
    /// Reselect new daily illnesses (call at start of new day)
    /// </summary>
    public void SelectNewDailyIllnesses()
    {
        if (illnessSelector != null)
        {
            illnessSelector.SelectDailyIllnesses();
            Debug.Log("[RequestsList] New daily illnesses selected!");
        }
        else
        {
            Debug.LogError("[RequestsList] IllnessSelector not assigned!");
        }
    }
    
    /// <summary>
    /// Hide the requests panel
    /// </summary>
    public void HidePanel()
    {
        if (requestsPanel == null) return;
        
        isPanelVisible = false;
        requestsPanel.SetActive(false);
        
        PlaySound(sfxLibrary?.uiCancel);
        
        if (showDebugLogs) Debug.Log("[RequestsList] Panel hidden");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getter
    public bool IsPanelVisible() => isPanelVisible;
}
