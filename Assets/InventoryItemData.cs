using UnityEngine;

/// <summary>
/// Data container for a single inventory item.
/// Stores all display information for UI systems.
/// Now includes reference to PlantDataSO for herb data access across scenes.
/// </summary>
[System.Serializable]
public class InventoryItemData
{
    public string itemID;        // Unique identifier (e.g., "red_flower")
    public string itemName;      // Display name (e.g., "Red Flower")
    public Sprite icon;          // Icon for UI display
    public int quantity;         // Current quantity in inventory
    
    [TextArea(2, 4)]
    public string description;   // Optional description for journal
    
    // Reference to herb data (for journal access)
    public PlantDataSO herbData; // NEW: Cache herb data for cross-scene access

    public InventoryItemData(string id, string name, Sprite itemIcon, int qty = 0, PlantDataSO herb = null)
    {
        itemID = id;
        itemName = name;
        icon = itemIcon;
        quantity = qty;
        description = "";
        herbData = herb;
    }
}