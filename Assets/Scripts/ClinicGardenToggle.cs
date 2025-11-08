using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles between Clinic view and Garden view by enabling/disabling GameObjects.
/// Attach to the toggle button in the Clinic scene.
/// </summary>
public class ClinicGardenToggle : MonoBehaviour
{
    [Header("Toggle Button")]
    [Tooltip("The button that triggers the toggle")]
    public Button toggleButton;
    
    [Header("Clinic View GameObjects (to disable when in Garden)")]
    [Tooltip("List of GameObjects to disable when switching to Garden")]
    public GameObject[] clinicViewObjects;
    
    [Header("Garden View GameObjects (to enable when in Garden)")]
    [Tooltip("List of GameObjects to enable when switching to Garden")]
    public GameObject[] gardenViewObjects;
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Track current view state
    private bool isGardenView = false;
    
    void Start()
    {
        // Wire up button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleView);
        }
        
        // Initialize - start with Clinic view
        ShowClinicView();
        
        if (showDebugLogs) Debug.Log("[ClinicGardenToggle] Initialized - Starting with Clinic view");
    }
    
    /// <summary>
    /// Toggle between Clinic and Garden views
    /// </summary>
    public void ToggleView()
    {
        if (isGardenView)
        {
            ShowClinicView();
        }
        else
        {
            ShowGardenView();
        }
        
        PlaySound(sfxLibrary?.uiSelect);
    }
    
    /// <summary>
    /// Show the Clinic view (disable Garden, enable Clinic)
    /// </summary>
    public void ShowClinicView()
    {
        // Enable Clinic objects
        foreach (GameObject obj in clinicViewObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        // Disable Garden objects
        foreach (GameObject obj in gardenViewObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        isGardenView = false;
        
        if (showDebugLogs) Debug.Log("[ClinicGardenToggle] Switched to Clinic view");
    }
    
    /// <summary>
    /// Show the Garden view (disable Clinic, enable Garden)
    /// </summary>
    public void ShowGardenView()
    {
        // Disable Clinic objects
        foreach (GameObject obj in clinicViewObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        // Enable Garden objects
        foreach (GameObject obj in gardenViewObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        isGardenView = true;
        
        if (showDebugLogs) Debug.Log("[ClinicGardenToggle] Switched to Garden view");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getters
    public bool IsGardenView() => isGardenView;
    public bool IsClinicView() => !isGardenView;
}
