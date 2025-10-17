using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all interaction prompts. Shows nearest one to player.
/// Attach to PlayerInteraction GameObject or create a separate manager.
/// </summary>
public class InteractionPromptManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Maximum distance to show prompts")]
    public float promptRange = 5f;
    
    [Tooltip("How often to check for nearby prompts (seconds)")]
    public float checkInterval = 0.1f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<InteractionPrompt> allPrompts = new List<InteractionPrompt>();
    private InteractionPrompt currentVisiblePrompt = null;
    private Transform playerTransform;
    private float checkTimer = 0f;

    void Start()
    {
        playerTransform = transform;

        // Find all prompts in scene
        RefreshPromptList();
    }

    void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            UpdateNearestPrompt();
        }
    }

    /// <summary>
    /// Find all InteractionPrompt components in scene
    /// </summary>
    public void RefreshPromptList()
    {
        allPrompts.Clear();
        allPrompts.AddRange(FindObjectsOfType<InteractionPrompt>());
        
        if (showDebugLogs) Debug.Log($"[PromptManager] Found {allPrompts.Count} interaction prompts");
    }

    void UpdateNearestPrompt()
    {
        InteractionPrompt nearestPrompt = null;
        float nearestDistance = float.MaxValue;

        // Find closest prompt in range
        foreach (var prompt in allPrompts)
        {
            if (prompt == null) continue;

            float distance = Vector3.Distance(playerTransform.position, prompt.transform.position);

            if (distance <= promptRange && distance < nearestDistance)
            {
                // Check if interactable can be interacted with
                IInteractable interactable = prompt.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    nearestDistance = distance;
                    nearestPrompt = prompt;
                }
            }
        }

        // Update visibility
        if (nearestPrompt != currentVisiblePrompt)
        {
            // Hide old prompt
            if (currentVisiblePrompt != null)
            {
                currentVisiblePrompt.Hide();
            }

            // Show new prompt
            if (nearestPrompt != null)
            {
                nearestPrompt.Show();
                if (showDebugLogs) Debug.Log($"[PromptManager] Showing prompt for {nearestPrompt.gameObject.name}");
            }

            currentVisiblePrompt = nearestPrompt;
        }
    }

    /// <summary>
    /// Register a new prompt (useful for dynamically spawned objects)
    /// </summary>
    public void RegisterPrompt(InteractionPrompt prompt)
    {
        if (!allPrompts.Contains(prompt))
        {
            allPrompts.Add(prompt);
        }
    }

    /// <summary>
    /// Unregister a prompt (when object is destroyed)
    /// </summary>
    public void UnregisterPrompt(InteractionPrompt prompt)
    {
        if (currentVisiblePrompt == prompt)
        {
            currentVisiblePrompt = null;
        }
        allPrompts.Remove(prompt);
    }

    void OnDrawGizmosSelected()
    {
        // Draw prompt range
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, promptRange);
    }
}