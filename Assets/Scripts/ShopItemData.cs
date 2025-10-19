using UnityEngine;

/// <summary>
/// ScriptableObject for shop items.
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
    public string description;
    
    [Header("Pricing")]
    public int price;
    
    [Header("Item Type")]
    public ShopItemType itemType;
    
    // For seed items
    [Header("Seed Data (if type is Seed)")]
    public string plantID; // Links to PlantNode plantID
    public int seedQuantity = 1; // How many seeds you get
}

public enum ShopItemType
{
    Seed,           // Plant seeds
    Tool,           // Tools (future)
    Decoration,     // Decorative items (future)
    Consumable      // One-time use items (future)
}