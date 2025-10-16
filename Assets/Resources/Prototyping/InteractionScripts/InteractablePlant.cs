using UnityEngine;

/// <summary>
/// Individual plant object that can be picked up by the player.
/// Attached to each spawned plant GameObject.
/// </summary>
public class InteractablePlant : MonoBehaviour, IInteractable
{
    [Header("References")]
    public PlantNode parentNode;  // Reference back to the node that spawned this
    
    [Header("Time Settings")]
    public int harvestTimeMinutes = 30;  // How many minutes harvesting takes
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    public bool CanInteract()
    {
        // Can always interact with visible plants
        return true;
    }

    public void Interact()
    {
        if (showDebugLogs) Debug.Log($"[InteractablePlant] '{gameObject.name}' interacted!");
        
        if (parentNode != null)
        {
            // Tell the parent node to collect this specific plant
            parentNode.CollectSpecificPlant(gameObject);
        }
        else
        {
            Debug.LogError($"[InteractablePlant] '{gameObject.name}' has no parent node reference!");
        }
    }

    public string GetInteractionPrompt()
    {
        if (parentNode != null && parentNode.plantInfo != null)
        {
            return $"Press [E] to collect {parentNode.plantInfo.plantName}";
        }
        return "Press [E] to collect";
    }
}