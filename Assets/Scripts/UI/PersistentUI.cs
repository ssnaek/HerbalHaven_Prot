using UnityEngine;

/// <summary>
/// Makes UI persist across scenes.
/// Attach to root Canvas or UI container that should survive scene loads.
/// Prevents duplicates and maintains singleton pattern.
/// </summary>
public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Unique ID for this UI (in case you have multiple persistent UIs)")]
    public string uiID = "GameUI";

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Awake()
    {
        // Check if another instance exists
        if (Instance != null && Instance.uiID == uiID)
        {
            // Duplicate found - destroy this one
            if (showDebugLogs) Debug.Log($"[PersistentUI] Duplicate '{uiID}' found, destroying this instance");
            Destroy(gameObject);
            return;
        }

        // First instance - keep it
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (showDebugLogs) Debug.Log($"[PersistentUI] '{uiID}' set to persist across scenes");
    }

    void OnDestroy()
    {
        // Clear instance if this was the main one
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
