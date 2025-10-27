using UnityEngine;

/// <summary>
/// Teleports player back to spawn point when they go too deep in water.
/// Attach to a trigger collider representing deep water zone.
/// </summary>
public class WaterDepthTeleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Where to teleport player back to")]
    public Transform respawnPoint;
    
    [Tooltip("If empty, uses player's start position")]
    public bool usePlayerStartPosition = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Vector3 playerStartPosition;
    private bool hasRecordedStartPosition = false;

    void Start()
    {
        // Make sure this has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[WaterDepthTeleporter] No collider found! Add a Box Collider or similar.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[WaterDepthTeleporter] Collider is not a trigger! Checking 'Is Trigger'...");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            // Record player's start position first time
            if (!hasRecordedStartPosition && usePlayerStartPosition)
            {
                playerStartPosition = other.transform.position;
                hasRecordedStartPosition = true;
                
                if (showDebugLogs) Debug.Log($"[WaterDepthTeleporter] Recorded player start position: {playerStartPosition}");
            }

            // Teleport player back
            TeleportPlayer(other.gameObject);
        }
    }

    void TeleportPlayer(GameObject player)
    {
        if (showDebugLogs) Debug.Log("[WaterDepthTeleporter] Starting teleport with transition");

        // Use circle transition if available
        if (CircleTransition.Instance != null)
        {
            CircleTransition.Instance.DoTransition(() => 
            {
                // This code runs in the middle of transition (screen is black)
                PerformTeleport(player);
            });
        }
        else
        {
            // No transition available, teleport immediately
            PerformTeleport(player);
        }
    }

    void PerformTeleport(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        
        Vector3 teleportPosition;
        
        // Determine where to teleport
        if (usePlayerStartPosition)
        {
            teleportPosition = playerStartPosition;
        }
        else if (respawnPoint != null)
        {
            teleportPosition = respawnPoint.position;
        }
        else
        {
            Debug.LogError("[WaterDepthTeleporter] No respawn point set and usePlayerStartPosition is false!");
            return;
        }

        // Disable controller temporarily to teleport
        if (controller != null)
        {
            controller.enabled = false;
            player.transform.position = teleportPosition;
            controller.enabled = true;
        }
        else
        {
            player.transform.position = teleportPosition;
        }

        if (showDebugLogs) Debug.Log($"[WaterDepthTeleporter] Teleported player to {teleportPosition}");
    }

    void OnDrawGizmos()
    {
        // Draw the trigger zone in editor
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f); // Blue transparent
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

        // Draw respawn point
        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }
    }
}