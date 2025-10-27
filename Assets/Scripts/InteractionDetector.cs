using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Detects nearby interactables and shows prompt UI.
/// Attach to Player GameObject.
/// Now uses events to efficiently hide/show prompt when shop/journal/dialogue opens.
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
    private DialogueRunner dialogueRunner;

    void Start()
    {
        // Subscribe to shop events
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.onShopOpened += OnUIOpened;
            ShopSystem.Instance.onShopClosed += OnUIClosed;
        }

        // Subscribe to journal events
        if (JournalController.Instance != null)
        {
            JournalController.Instance.onJournalOpened += OnUIOpened;
            JournalController.Instance.onJournalClosed += OnUIClosed;
        }

        // Subscribe to dialogue events
        dialogueRunner = FindObjectOfType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.AddListener(OnDialogueStarted);
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueEnded);
            
            if (showDebugLogs) Debug.Log("[Detector] Subscribed to DialogueRunner events");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[Detector] No DialogueRunner found in scene");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (ShopSystem.Instance != null)
        {
            ShopSystem.Instance.onShopOpened -= OnUIOpened;
            ShopSystem.Instance.onShopClosed -= OnUIClosed;
        }

        if (JournalController.Instance != null)
        {
            JournalController.Instance.onJournalOpened -= OnUIOpened;
            JournalController.Instance.onJournalClosed -= OnUIClosed;
        }

        // Unsubscribe from dialogue events
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(OnDialogueStarted);
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueEnded);
        }
    }

    void OnUIOpened()
    {
        HidePrompt();
        currentInteractable = null; // Reset so it detects as "new" when UI closes
        currentInteractableObject = null;
        enabled = false; // Stop checking while UI is open
        
        if (showDebugLogs) Debug.Log("[Detector] UI opened, detector paused, current target reset");
    }

    void OnUIClosed()
    {
        enabled = true; // Resume checking
        checkTimer = checkInterval; // Force immediate check on next frame
        
        if (showDebugLogs) Debug.Log("[Detector] UI closed, detector resumed");
    }

    void OnDialogueStarted()
    {
        OnUIOpened(); // Reuse existing UI opened logic
        if (showDebugLogs) Debug.Log("[Detector] Dialogue started, hiding prompt");
    }

    void OnDialogueEnded()
    {
        OnUIClosed(); // Reuse existing UI closed logic
        if (showDebugLogs) Debug.Log("[Detector] Dialogue ended, resuming detection");
    }

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
        if (showDebugLogs) Debug.Log("[Detector] Checking for interactables...");
        
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

            if (showDebugLogs) Debug.Log($"[Detector] Sphere found {colliders.Length} colliders");

            foreach (Collider col in colliders)
            {
                if (col == null) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);
                IInteractable interactable = col.GetComponentInParent<IInteractable>();

                if (showDebugLogs) Debug.Log($"[Detector] Checking collider: {col.name}, distance: {distance:F2}, has IInteractable: {interactable != null}");

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
                if (showDebugLogs) Debug.Log($"[Detector] Found new target: {nearestObject.name}, showing prompt");
                ShowPrompt(nearestInteractable);
                currentInteractable = nearestInteractable;
                currentInteractableObject = nearestObject;
            }
            else
            {
                // Hide prompt - nothing in range
                if (showDebugLogs) Debug.Log("[Detector] No interactable in range, hiding prompt");
                HidePrompt();
                currentInteractable = null;
                currentInteractableObject = null;
            }
        }
        else if (showDebugLogs && nearestInteractable != null)
        {
            Debug.Log($"[Detector] Same target as before: {nearestObject.name}");
        }
    }

    void ShowPrompt(IInteractable interactable)
    {
        if (InteractionPromptUI.Instance == null)
        {
            if (showDebugLogs) Debug.LogWarning("[Detector] InteractionPromptUI.Instance is null!");
            return;
        }

        string message = interactable.GetInteractionPrompt();
        string keyString = interactionKey.ToString();

        if (showDebugLogs) Debug.Log($"[Detector] Calling Show() with message: '{message}'");
        InteractionPromptUI.Instance.Show(message, keyString);
    }

    void HidePrompt()
    {
        if (InteractionPromptUI.Instance == null) return;

        if (showDebugLogs) Debug.Log("[Detector] Calling Hide()");
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