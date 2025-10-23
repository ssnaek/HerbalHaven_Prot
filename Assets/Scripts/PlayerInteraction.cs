using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 3f;
    public LayerMask interactLayer;
    public float interactCooldown = 0.2f;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool showGizmos = true;

    private float lastInteractTime = -10f;
    
    // Gizmo visualization data
    private RaycastHit lastRaycastHit;
    private bool lastRaycastSuccess = false;
    private Collider[] lastSphereHits = new Collider[0];

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (showDebugLogs) Debug.Log("=== E KEY PRESSED ===");
            TryInteract();
        }

        // Update gizmo visualization
        if (showGizmos)
        {
            UpdateGizmoData();
        }
    }

    void UpdateGizmoData()
    {
        if (Camera.main)
        {
            lastRaycastSuccess = Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out lastRaycastHit,
                interactRange,
                interactLayer
            );
        }

        lastSphereHits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
    }

    void TryInteract()
    {
        // Cooldown check
        if (Time.time - lastInteractTime < interactCooldown)
        {
            if (showDebugLogs) Debug.Log("[Interaction] On cooldown");
            return;
        }
        lastInteractTime = Time.time;

        // Try raycast first (prioritized - what player is looking at)
        if (TryRaycastInteract()) return;

        // Fallback to sphere (closest object in range)
        TrySphereInteract();
    }

    bool TryRaycastInteract()
    {
        if (!Camera.main) return false;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (showDebugLogs) Debug.Log($"[RAYCAST] Checking from {ray.origin}");

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            if (showDebugLogs) Debug.Log($"[RAYCAST] Hit '{hit.collider.name}' at {hit.distance:F2}m");

            return AttemptInteraction(hit.collider, "RAYCAST");
        }

        if (showDebugLogs) Debug.Log("[RAYCAST] No hit");
        return false;
    }

    bool TrySphereInteract()
    {
        if (showDebugLogs) Debug.Log("[SPHERE] Checking proximity");

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);

        if (showDebugLogs) Debug.Log($"[SPHERE] Found {hits.Length} colliders");

        // Find closest interactable
        Collider closestCollider = null;
        float closestDistance = float.MaxValue;

        foreach (Collider c in hits)
        {
            if (c == null) continue;

            float distance = Vector3.Distance(transform.position, c.transform.position);
            IInteractable interactable = c.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract() && distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = c;
            }
        }

        if (closestCollider != null)
        {
            if (showDebugLogs) Debug.Log($"[SPHERE] Closest: '{closestCollider.name}' at {closestDistance:F2}m");
            return AttemptInteraction(closestCollider, "SPHERE");
        }

        if (showDebugLogs) Debug.Log("[SPHERE] No valid interactable found");
        return false;
    }

    bool AttemptInteraction(Collider collider, string method)
    {
        IInteractable interactable = collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            if (showDebugLogs) Debug.Log($"[{method}] No IInteractable on '{collider.name}'");
            return false;
        }

        if (!interactable.CanInteract())
        {
            if (showDebugLogs) Debug.Log($"[{method}] CanInteract() returned false");
            return false;
        }

        // Perform interaction
        if (showDebugLogs) Debug.Log($"[{method}] ✓ Interacting with '{collider.name}'");
        interactable.Interact();

        // Check for time advancement
        TimeAdvancer timeAdvancer = collider.GetComponent<TimeAdvancer>();
        if (timeAdvancer == null)
        {
            timeAdvancer = collider.GetComponentInParent<TimeAdvancer>();
        }

        if (timeAdvancer != null)
        {
            if (showDebugLogs) Debug.Log($"[{method}] ⏰ Advancing time by {timeAdvancer.GetMinutesToAdvance()} minutes");
            timeAdvancer.AdvanceTime();
        }

        return true;
    }

    // ========== GIZMOS ==========

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        DrawInteractionSphere();
        DrawRaycast();
        DrawSphereHits();
    }

    void DrawInteractionSphere()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    void DrawRaycast()
    {
        if (!Camera.main) return;

        Vector3 rayStart = Camera.main.transform.position;
        Vector3 rayDirection = Camera.main.transform.forward;
        Vector3 rayEnd = rayStart + rayDirection * interactRange;

        if (lastRaycastSuccess && lastRaycastHit.collider != null)
        {
            // Hit - green to hit point
            Gizmos.color = Color.green;
            Gizmos.DrawLine(rayStart, lastRaycastHit.point);

            // Red after hit point
            Gizmos.color = Color.red;
            Gizmos.DrawLine(lastRaycastHit.point, rayEnd);

            // Cyan sphere at hit point
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastRaycastHit.point, 0.1f);

            // Magenta line to object center (if collider still exists)
            if (lastRaycastHit.collider != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(lastRaycastHit.point, lastRaycastHit.collider.transform.position);
            }
        }
        else
        {
            // No hit - red line
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawLine(rayStart, rayEnd);
        }
    }

    void DrawSphereHits()
    {
        if (lastSphereHits == null || lastSphereHits.Length == 0) return;

        foreach (Collider c in lastSphereHits)
        {
            if (c == null) continue;

            IInteractable interactable = c.GetComponentInParent<IInteractable>();
            bool isValid = interactable != null && interactable.CanInteract();

            // Yellow for valid, orange for invalid
            Gizmos.color = isValid ? new Color(1f, 1f, 0f, 0.6f) : new Color(1f, 0.5f, 0f, 0.3f);

            Gizmos.DrawLine(transform.position, c.transform.position);
            Gizmos.DrawWireSphere(c.transform.position, 0.2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Highlighted interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);

        // Camera forward direction
        if (Camera.main)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactRange);
        }
    }
}