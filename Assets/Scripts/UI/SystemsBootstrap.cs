using UnityEngine;

/// <summary>
/// Initializes the Systems UI container in Main Menu.
/// Attach to a GameObject in the Main Menu scene.
/// </summary>
public class SystemsBootstrap : MonoBehaviour
{
    [Header("Systems Container")]
    [Tooltip("Drag your Systems prefab (the one with Canvas/UI controllers)")]
    public GameObject systemsPrefab;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Awake()
    {
        // Only create if it doesn't exist yet
        if (PersistentUI.Instance == null && systemsPrefab != null)
        {
            Instantiate(systemsPrefab);
            if (showDebugLogs) Debug.Log("[SystemsBootstrap] Created Systems container");
        }
        else if (showDebugLogs)
        {
            Debug.Log("[SystemsBootstrap] Systems container already exists");
        }
    }
}
