using UnityEngine;

/// <summary>
/// ScriptableObject for shop items.
/// Also serves as journal entry data to avoid redundancy.
/// Create one for each item you want to sell.
/// </summary>
[CreateAssetMenu(fileName = "New Shop Item", menuName = "Shop/Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    
    [TextArea(2, 4)]
    [Tooltip("Short description shown in shop")]
    public string description;
    
    [Header("Pricing")]
    public int price;
    
    [Header("Item Type")]
    public ShopItemType itemType;
    
    [Header("Seed Data (if type is Seed)")]
    public string plantID; // Links to PlantNode plantID
    public int seedQuantity = 1; // How many seeds you get
    
    [Header("Journal Data (Optional)")]
    [TextArea(3, 6)]
    [Tooltip("Longer description for journal. If empty, uses regular description.")]
    public string journalDescription;
    
    [Tooltip("Scientific name shown in journal (optional)")]
    public string scientificName;
    
    [TextArea(2, 4)]
    [Tooltip("Uses/benefits shown in journal (optional)")]
    public string uses;
    
    /// <summary>
    /// Get the appropriate description for journal display
    /// </summary>
    public string GetJournalDescription()
    {
        return string.IsNullOrEmpty(journalDescription) ? description : journalDescription;
    }
}

public enum ShopItemType
{
    Seed,           // Plant seeds
    Tool,           // Tools (future)
    Decoration,     // Decorative items (future)
    Consumable      // One-time use items (future)
}