using UnityEngine;

/// <summary>
/// Toggles between Counter/Crafting UI and Garden UI.
/// When toggled, disables counter/crafting panels and enables garden panels, and vice versa.
/// </summary>
public class GardenUIManager : MonoBehaviour
{
    [Header("Counter/Crafting UI (Disabled when showing garden)")]
    [Tooltip("CounterBackground GameObject")]
    public GameObject counterBackground;
    
    [Tooltip("CounterPanel GameObject")]
    public GameObject counterPanel;
    
    [Tooltip("CounterUIPanel GameObject")]
    public GameObject counterUIPanel;
    
    [Tooltip("CraftingUIPanel GameObject")]
    public GameObject craftingUIPanel;
    
    [Tooltip("ToggleButton GameObject")]
    public GameObject toggleButton;
    
    [Header("Garden UI (Enabled when showing garden)")]
    [Tooltip("GardenUIPanel GameObject")]
    public GameObject gardenUIPanel;
    
    [Tooltip("SeedInventoryViewport GameObject")]
    public GameObject seedInventoryViewport;
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private bool showingGarden = false;
    
    void Start()
    {
        // Initialize: Start with counter/crafting UI visible, garden UI hidden
        ShowCounterUI();
    }
    
    /// <summary>
    /// Toggle between Counter/Crafting UI and Garden UI
    /// </summary>
    public void ToggleGardenView()
    {
        if (showingGarden)
        {
            ShowCounterUI();
        }
        else
        {
            ShowGardenUI();
        }
    }
    
    /// <summary>
    /// Show Counter/Crafting UI, hide Garden UI
    /// </summary>
    public void ShowCounterUI()
    {
        // Disable Garden UI
        SetGameObjectActive(gardenUIPanel, false);
        SetGameObjectActive(seedInventoryViewport, false);
        
        // Enable Counter/Crafting UI
        SetGameObjectActive(counterBackground, true);
        SetGameObjectActive(counterPanel, true);
        SetGameObjectActive(counterUIPanel, true);
        SetGameObjectActive(craftingUIPanel, true);
        SetGameObjectActive(toggleButton, true);
        
        showingGarden = false;
        
        PlaySound(sfxLibrary?.uiSelect);
        
        if (showDebugLogs) Debug.Log("[GardenUIManager] Showing Counter/Crafting UI");
    }
    
    /// <summary>
    /// Show Garden UI, hide Counter/Crafting UI
    /// </summary>
    public void ShowGardenUI()
    {
        // Disable Counter/Crafting UI
        SetGameObjectActive(counterBackground, false);
        SetGameObjectActive(counterPanel, false);
        SetGameObjectActive(counterUIPanel, false);
        SetGameObjectActive(craftingUIPanel, false);
        // Keep toggle button enabled so user can return to workstation
        // SetGameObjectActive(toggleButton, false);
        
        // Enable Garden UI
        SetGameObjectActive(gardenUIPanel, true);
        SetGameObjectActive(seedInventoryViewport, true);
        
        showingGarden = true;
        
        PlaySound(sfxLibrary?.uiSelect);
        
        if (showDebugLogs) Debug.Log("[GardenUIManager] Showing Garden UI");
    }
    
    /// <summary>
    /// Unified toggle method that works from both counter/crafting and garden views
    /// Call this from the toggle button to switch between views
    /// </summary>
    public void HandleToggleButtonClick()
    {
        if (showingGarden)
        {
            // Currently showing garden, return to counter/crafting
            ShowCounterUI();
        }
        else
        {
            // Currently showing counter/crafting, check if we should go to garden
            // Or let ClinicPanelManager handle counter/crafting toggle
            // For now, just toggle to garden
            ShowGardenUI();
        }
    }
    
    /// <summary>
    /// Safely set GameObject active state (handles null references)
    /// </summary>
    void SetGameObjectActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[GardenUIManager] GameObject reference is null! Cannot set active state to {active}");
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getters
    public bool IsShowingGarden() => showingGarden;
}

