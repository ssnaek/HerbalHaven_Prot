using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically teleports player to another scene when they enter the trigger.
/// Uses circle transition animation.
/// Attach to the boat mesh that should trigger travel.
/// Make sure collider is set to "Is Trigger"!
/// </summary>
public class BoatTravel : MonoBehaviour
{
    [Header("Travel Settings")]
    [Tooltip("Name of the scene to load")]
    public string destinationScene = "OtherIsland";

    [Header("Transition")]
    [Tooltip("How long to wait at black screen before loading (seconds)")]
    public float transitionDelay = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool isTraveling = false;

    void Start()
    {
        // Make sure this has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[BoatTravel] No collider found! Add a Box Collider.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[BoatTravel] Collider is not a trigger! Checking 'Is Trigger'...");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered
        if (other.CompareTag("Player") && !isTraveling)
        {
            if (showDebugLogs) Debug.Log($"[BoatTravel] Player entered boat, traveling to {destinationScene}");
            StartTravel();
        }
    }

    void StartTravel()
    {
        if (string.IsNullOrEmpty(destinationScene))
        {
            Debug.LogError("[BoatTravel] Destination scene not set!");
            return;
        }

        isTraveling = true;

        // Use circle transition if available
        if (CircleTransition.Instance != null)
        {
            if (showDebugLogs) Debug.Log("[BoatTravel] Playing transition animation");
            
            CircleTransition.Instance.DoTransition(() => 
            {
                // This code runs in the middle of transition (screen is black)
                LoadDestinationScene();
            });
        }
        else
        {
            // No transition available, load immediately
            Debug.LogWarning("[BoatTravel] CircleTransition not found, loading scene directly");
            LoadDestinationScene();
        }
    }

    void LoadDestinationScene()
    {
        if (showDebugLogs) Debug.Log($"[BoatTravel] Loading scene: {destinationScene}");
        
        SceneManager.LoadScene(destinationScene);
    }

    void OnDrawGizmosSelected()
    {
        // Draw trigger zone
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f); // Yellow transparent
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
        
        // Draw "destination" arrow
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        Vector3 arrowStart = transform.position + Vector3.up * 2f;
        Vector3 arrowEnd = arrowStart + Vector3.forward * 2f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        Gizmos.DrawLine(arrowEnd, arrowEnd + Vector3.left * 0.3f + Vector3.back * 0.3f);
        Gizmos.DrawLine(arrowEnd, arrowEnd + Vector3.right * 0.3f + Vector3.back * 0.3f);
    }
}