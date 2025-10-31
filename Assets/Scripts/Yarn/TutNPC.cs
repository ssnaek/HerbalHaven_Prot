using UnityEngine;
using Yarn.Unity;

/// <summary>
/// NPC that triggers Yarn Spinner dialogue.
/// Shows interaction prompt and starts dialogue on interact.
/// Attach to NPC GameObject.
/// </summary>
public class DialogueNPC : MonoBehaviour, IInteractable
{
    [Header("Yarn Settings")]
    [Tooltip("Name of the Yarn node to start (e.g., 'TutorialNPC')")]
    public string yarnNodeName = "TutorialNPC";
    
    [Tooltip("Dialogue runner in the scene (auto-finds if empty)")]
    public DialogueRunner dialogueRunner;

    [Header("Prompt Settings")]
    public string npcName = "Tutorial NPC";
    public string interactionPrompt = "Talk";

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Start()
    {
        // Auto-find DialogueRunner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindObjectOfType<DialogueRunner>();
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("[DialogueNPC] No DialogueRunner found in scene!");
        }

        // Ensure NPC has collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1, 0);
            
            if (showDebugLogs) Debug.Log("[DialogueNPC] Added CapsuleCollider to NPC");
        }
    }

    public bool CanInteract()
    {
        // Can't interact if dialogue is already running
        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning)
        {
            return false;
        }
        
        return true;
    }

    public void Interact()
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("[DialogueNPC] DialogueRunner not found!");
            return;
        }

        if (string.IsNullOrEmpty(yarnNodeName))
        {
            Debug.LogError("[DialogueNPC] Yarn node name is empty!");
            return;
        }

        if (showDebugLogs) Debug.Log($"[DialogueNPC] Starting dialogue: {yarnNodeName}");

        // Start the dialogue
        dialogueRunner.StartDialogue(yarnNodeName);
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction visualization
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
