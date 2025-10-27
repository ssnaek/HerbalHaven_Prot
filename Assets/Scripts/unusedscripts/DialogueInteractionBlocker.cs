using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Disables player interactions (E key) during dialogue and option selection.
/// Attach to Player GameObject or create separate GameObject.
/// </summary>
public class DialogueInteractionBlocker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag PlayerInteraction component here")]
    public PlayerInteraction playerInteraction;
    
    [Tooltip("Drag Dialogue System here")]
    public DialogueRunner dialogueRunner;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool interactionsEnabled = true;

    void Start()
    {
        // Auto-find if not assigned
        if (playerInteraction == null)
        {
            playerInteraction = GetComponent<PlayerInteraction>();
            if (playerInteraction == null)
                playerInteraction = FindObjectOfType<PlayerInteraction>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindObjectOfType<DialogueRunner>();
        }

        // Validate
        if (playerInteraction == null)
        {
            Debug.LogError("[DialogueInteractionBlocker] PlayerInteraction not found!");
            return;
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("[DialogueInteractionBlocker] DialogueRunner not found!");
            return;
        }

        // Subscribe to dialogue events
        dialogueRunner.onDialogueStart.AddListener(OnDialogueStarted);
        dialogueRunner.onDialogueComplete.AddListener(OnDialogueEnded);
        
        if (showDebugLogs) Debug.Log("[DialogueInteractionBlocker] Set up successfully");
    }

    void OnDestroy()
    {
        // Unsubscribe
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(OnDialogueStarted);
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueEnded);
        }
    }

    void OnDialogueStarted()
    {
        DisableInteractions();
    }

    void OnDialogueEnded()
    {
        EnableInteractions();
    }

    void DisableInteractions()
    {
        if (playerInteraction != null && playerInteraction.enabled)
        {
            playerInteraction.enabled = false;
            interactionsEnabled = false;
            
            if (showDebugLogs) Debug.Log("[DialogueInteractionBlocker] Interactions disabled");
        }
    }

    void EnableInteractions()
    {
        if (playerInteraction != null && !playerInteraction.enabled)
        {
            playerInteraction.enabled = true;
            interactionsEnabled = true;
            
            if (showDebugLogs) Debug.Log("[DialogueInteractionBlocker] Interactions enabled");
        }
    }
}
