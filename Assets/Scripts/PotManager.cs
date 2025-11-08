using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages individual pot interactions and planting seeds.
/// Attach this script to each pot GameObject.
/// </summary>
public class PotManager : MonoBehaviour
{
    [Header("Pot Sprites")]
    [Tooltip("Empty pot sprite (default state)")]
    public Sprite emptyPotSprite;
    
    [Tooltip("Pot with sprout sprite (after planting)")]
    public Sprite sproutPotSprite;
    
    [Header("Components")]
    [Tooltip("Image component that displays the pot sprite")]
    public Image potImage;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Runtime state
    private bool isPlanted = false;
    private string plantedSeedID = null;
    
    void Awake()
    {
        // Auto-find Image component if not assigned
        if (potImage == null)
        {
            potImage = GetComponent<Image>();
        }
        
        // Auto-find Button component or add one
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        // Add click listener
        button.onClick.AddListener(OnPotClicked);
        
        // Initialize with empty pot sprite
        if (potImage != null && emptyPotSprite != null)
        {
            potImage.sprite = emptyPotSprite;
        }
    }
    
    /// <summary>
    /// Called when pot is clicked
    /// </summary>
    void OnPotClicked()
    {
        if (showDebugLogs) Debug.Log($"[PotManager] Pot '{gameObject.name}' clicked. IsPlanted: {isPlanted}");
        
        // Check if there's a selected seed from GardenHotbar
        if (GardenHotbar.Instance != null)
        {
            string selectedSeedID = GardenHotbar.Instance.GetSelectedSeedID();
            
            if (!string.IsNullOrEmpty(selectedSeedID))
            {
                // Try to plant the selected seed
                PlantSeed(selectedSeedID);
            }
            else
            {
                if (showDebugLogs) Debug.Log("[PotManager] No seed selected. Click a seed slot first.");
            }
        }
        else
        {
            Debug.LogWarning("[PotManager] GardenHotbar.Instance is null!");
        }
    }
    
    /// <summary>
    /// Plant a seed in this pot
    /// </summary>
    public bool PlantSeed(string seedID)
    {
        if (isPlanted)
        {
            if (showDebugLogs) Debug.Log($"[PotManager] Pot '{gameObject.name}' is already planted!");
            return false;
        }
        
        if (string.IsNullOrEmpty(seedID))
        {
            if (showDebugLogs) Debug.LogWarning("[PotManager] Cannot plant - seedID is null or empty");
            return false;
        }
        
        // Check if player has this seed in inventory
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[PotManager] InventorySystem.Instance is null!");
            return false;
        }
        
        if (!InventorySystem.Instance.HasItem(seedID))
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Player doesn't have seed: {seedID}");
            return false;
        }
        
        int quantity = InventorySystem.Instance.GetItemQuantity(seedID);
        if (quantity <= 0)
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Not enough seeds: {seedID}");
            return false;
        }
        
        // Remove one seed from inventory
        bool removed = InventorySystem.Instance.RemoveItem(seedID, 1);
        if (!removed)
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Failed to remove seed from inventory: {seedID}");
            return false;
        }
        
        // Update pot state
        isPlanted = true;
        plantedSeedID = seedID;
        
        // Change sprite to sprout
        if (potImage != null && sproutPotSprite != null)
        {
            potImage.sprite = sproutPotSprite;
        }
        else if (potImage != null)
        {
            Debug.LogWarning($"[PotManager] Sprout sprite not assigned for pot '{gameObject.name}'");
        }
        
        // Clear selection in hotbar
        if (GardenHotbar.Instance != null)
        {
            GardenHotbar.Instance.ClearSelection();
        }
        
        if (showDebugLogs)
        {
            InventoryItemData seedData = InventorySystem.Instance.GetItemData(seedID);
            string seedName = seedData != null ? seedData.itemName : seedID;
            Debug.Log($"[PotManager] Planted {seedName} in pot '{gameObject.name}'");
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if this pot is planted
    /// </summary>
    public bool IsPlanted()
    {
        return isPlanted;
    }
    
    /// <summary>
    /// Get the seed ID planted in this pot
    /// </summary>
    public string GetPlantedSeedID()
    {
        return plantedSeedID;
    }
    
    /// <summary>
    /// Reset pot to empty state (for testing/debugging)
    /// </summary>
    public void ResetPot()
    {
        isPlanted = false;
        plantedSeedID = null;
        
        if (potImage != null && emptyPotSprite != null)
        {
            potImage.sprite = emptyPotSprite;
        }
    }
}

