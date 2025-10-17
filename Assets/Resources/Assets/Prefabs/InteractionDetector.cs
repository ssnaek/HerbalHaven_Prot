using UnityEngine;

/// <summary>
/// Detects nearby interactables and shows prompt UI.
/// Attach to Player GameObject.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far to detect interactables")]
    public float detectionRange = 5f;
    
    [Tooltip("Layer mask for interactables")]
    public LayerMask interactionLayer;
    
    [Tooltip("How often to check for interactables (seconds)")]
    public float checkInterval = 0.1f;

    [Header("Interaction Key")]
    [Tooltip("Key shown in prompt UI")]
    public KeyCode interactionKey = KeyCode.E;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float checkTimer = 0f;
    private IInteractable currentInteractable = null;
    private GameObject currentInteractableObject = null;

    void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckForNearbyInteractables();
        }
    }

    void CheckForNearbyInteractables()
    {
        IInteractable nearestInteractable = null;
        GameObject nearestObject = null;
        float nearestDistance = float.MaxValue;

        // Raycast forward from camera
        bool raycastHit = false;
        if (Camera.main != null)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, detectionRange, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    nearestInteractable = interactable;
                    nearestObject = hit.collider.gameObject;
                    nearestDistance = hit.distance;
                    raycastHit = true;

                    if (showDebugLogs) Debug.Log($"[Detector] Raycast hit: {hit.collider.name}");
                }
            }
        }

        // Sphere check if raycast didn't find anything
        if (!raycastHit)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, interactionLayer);

            foreach (Collider col in colliders)
            {
                if (col == null) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                IInteractable interactable = col.GetComponentInParent<IInteractable>();

                if (interactable != null && interactable.CanInteract() && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteractable = interactable;
                    nearestObject = col.gameObject;
                }
            }
        }

        // Update UI based on what we found
        if (nearestInteractable != currentInteractable)
        {
            if (nearestInteractable != null)
            {
                // Show prompt for new interactable
                ShowPrompt(nearestInteractable);
                currentInteractable = nearestInteractable;
                currentInteractableObject = nearestObject;

                if (showDebugLogs) Debug.Log($"[Detector] Now targeting: {nearestObject.name}");
            }
            else
            {
                // Hide prompt - nothing in range
                HidePrompt();
                currentInteractable = null;
                currentInteractableObject = null;

                if (showDebugLogs) Debug.Log("[Detector] No interactable in range");
            }
        }
    }

    void ShowPrompt(IInteractable interactable)
    {
        if (InteractionPromptUI.Instance == null) return;

        string message = interactable.GetInteractionPrompt();
        string keyString = interactionKey.ToString();

        InteractionPromptUI.Instance.Show(message, keyString);
    }

    void HidePrompt()
    {
        if (InteractionPromptUI.Instance == null) return;

        InteractionPromptUI.Instance.Hide();
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw line to current target
        if (currentInteractableObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentInteractableObject.transform.position);
        }
    }
}