using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the clinic phase - ends the clinic after creating a specified number of medicines.
/// Automatically transitions to the next scene when the medicine count is reached.
/// </summary>
public class ClinicPhaseManager : MonoBehaviour
{
    [Header("Medicine Tracking")]
    [Tooltip("Number of medicines required before ending clinic phase")]
    public int medicinesRequired = 1;
    
    [Header("Scene Transition")]
    [Tooltip("Name of the scene to load after clinic phase ends (leave empty to use TimeSystem's nextSceneName)")]
    public string nextSceneName = "";
    
    [Tooltip("Delay before transitioning (seconds)")]
    public float transitionDelay = 1f;
    
    [Header("References")]
    [Tooltip("Reference to MedicineCraftingManager (auto-finds if empty)")]
    public MedicineCraftingManager craftingManager;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private int medicinesCreated = 0;
    private bool hasTransitioned = false;
    
    void Start()
    {
        // Auto-find MedicineCraftingManager if not assigned
        if (craftingManager == null)
        {
            craftingManager = FindObjectOfType<MedicineCraftingManager>();
        }
        
        if (craftingManager == null)
        {
            Debug.LogError("[ClinicPhaseManager] MedicineCraftingManager not found! Please assign it in the Inspector.");
            return;
        }
        
        // Subscribe to medicine creation event
        craftingManager.onEvaluationComplete.AddListener(OnMedicineCreated);
        
        if (showDebugLogs) Debug.Log($"[ClinicPhaseManager] Initialized - Tracking {medicinesRequired} medicine(s)");
    }
    
    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (craftingManager != null)
        {
            craftingManager.onEvaluationComplete.RemoveListener(OnMedicineCreated);
        }
    }
    
    /// <summary>
    /// Called when a medicine is created
    /// </summary>
    void OnMedicineCreated()
    {
        if (hasTransitioned) return; // Prevent multiple transitions
        
        medicinesCreated++;
        
        if (showDebugLogs) Debug.Log($"[ClinicPhaseManager] Medicine created! ({medicinesCreated}/{medicinesRequired})");
        
        // Check if we've reached the required count
        if (medicinesCreated >= medicinesRequired)
        {
            if (showDebugLogs) Debug.Log($"[ClinicPhaseManager] Required medicines reached! Ending clinic phase...");
            EndClinicPhase();
        }
    }
    
    /// <summary>
    /// End the clinic phase and transition to next scene
    /// </summary>
    void EndClinicPhase()
    {
        if (hasTransitioned) return;
        hasTransitioned = true;
        
        // Determine which scene to load
        string sceneToLoad = nextSceneName;
        
        // If no scene specified, try to get it from TimeSystem
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            TimeSystem timeSystem = FindObjectOfType<TimeSystem>();
            if (timeSystem != null)
            {
                // Use reflection or check if TimeSystem has a public nextSceneName
                // For now, we'll use a default or let user specify
                if (showDebugLogs) Debug.LogWarning("[ClinicPhaseManager] No scene specified and TimeSystem nextSceneName not accessible. Please set nextSceneName in Inspector.");
            }
        }
        
        // If still no scene, use a default
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = "Herb_Island"; // Default fallback
            if (showDebugLogs) Debug.LogWarning($"[ClinicPhaseManager] Using default scene: {sceneToLoad}");
        }
        
        // Transition after delay
        Invoke(nameof(TransitionToNextScene), transitionDelay);
    }
    
    /// <summary>
    /// Actually perform the scene transition
    /// </summary>
    void TransitionToNextScene()
    {
        string sceneToLoad = string.IsNullOrEmpty(nextSceneName) ? "Herb_Island" : nextSceneName;
        
        // Use CircleTransition if available
        if (CircleTransition.Instance != null)
        {
            if (showDebugLogs) Debug.Log($"[ClinicPhaseManager] Using CircleTransition to load: {sceneToLoad}");
            
            CircleTransition.Instance.DoTransition(() =>
            {
                LoadScene(sceneToLoad);
            });
        }
        else
        {
            // No transition available, load directly
            if (showDebugLogs) Debug.LogWarning("[ClinicPhaseManager] CircleTransition not found, loading scene directly");
            LoadScene(sceneToLoad);
        }
    }
    
    /// <summary>
    /// Load the specified scene
    /// </summary>
    void LoadScene(string sceneName)
    {
        if (showDebugLogs) Debug.Log($"[ClinicPhaseManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// Public method to manually end clinic phase (for testing or other triggers)
    /// </summary>
    public void ForceEndClinicPhase()
    {
        if (!hasTransitioned)
        {
            EndClinicPhase();
        }
    }
    
    /// <summary>
    /// Reset the medicine counter (useful for testing)
    /// </summary>
    public void ResetCounter()
    {
        medicinesCreated = 0;
        hasTransitioned = false;
        if (showDebugLogs) Debug.Log("[ClinicPhaseManager] Counter reset");
    }
    
    // Public getters
    public int GetMedicinesCreated() => medicinesCreated;
    public int GetMedicinesRequired() => medicinesRequired;
    public bool IsComplete() => medicinesCreated >= medicinesRequired;
}

