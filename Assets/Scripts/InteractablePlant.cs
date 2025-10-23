using UnityEngine;

/// <summary>
/// Individual plant object that can be picked up by the player.
/// Attached to each spawned plant GameObject.
/// Now stores reference to PlantDataSO for collection.
/// </summary>
public class InteractablePlant : MonoBehaviour, IInteractable
{
    [Header("References")]
    public PlantNode parentNode;
    public PlantDataSO plantData;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    public bool CanInteract()
    {
        return true;
    }

    public void Interact()
    {
        if (showDebugLogs) Debug.Log($"[InteractablePlant] '{gameObject.name}' interacted!");
        
        if (parentNode != null && plantData != null)
        {
            parentNode.CollectSpecificPlant(gameObject, plantData);
        }
        else
        {
            if (parentNode == null)
                Debug.LogError($"[InteractablePlant] '{gameObject.name}' has no parent node reference!");
            if (plantData == null)
                Debug.LogError($"[InteractablePlant] '{gameObject.name}' has no plant data reference!");
        }
    }

    public string GetInteractionPrompt()
    {
        if (parentNode != null && plantData != null)
        {
            return $"Collect {plantData.plantName}";
        }
        return "Collect Plant";
    }
}