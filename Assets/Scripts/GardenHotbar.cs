using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the Garden Hotbar UI that displays seeds from inventory.
/// Each seed type occupies one slot with its stack count displayed.
/// Only shows seeds (items purchased from shop), not harvested herbs.
/// </summary>
public class GardenHotbar : MonoBehaviour
{
    [System.Serializable]
    public class HotbarSlot
    {
        [Tooltip("The slot's root GameObject")]
        public GameObject slotObject;
        
        [Tooltip("Image component to display seed icon")]
        public Image iconImage;
        
        [Tooltip("TextMeshPro to display quantity")]
        public TextMeshProUGUI quantityText;
        
        [Tooltip("Empty slot sprite (shown when no seed)")]
        public Sprite emptySprite;
        
        // Runtime data
        [HideInInspector] public string currentItemID;
        [HideInInspector] public bool isEmpty = true;
    }
    
    [Header("Hotbar Slots")]
    [Tooltip("All hotbar slots in order")]
    public HotbarSlot[] hotbarSlots;
    
    [Header("Settings")]
    [Tooltip("Suffix to identify seeds in inventory (e.g., '_seed')")]
    public string seedIDSuffix = "_seed";
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private Dictionary<string, int> seedInventory = new Dictionary<string, int>();
    
    void Start()
    {
        // Subscribe to inventory changes
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshHotbar;
        }
        
        // Initial refresh
        RefreshHotbar();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshHotbar;
        }
    }
    
    /// <summary>
    /// Refresh the hotbar display from inventory
    /// </summary>
    public void RefreshHotbar()
    {
        if (showDebugLogs) Debug.Log("[GardenHotbar] Refreshing hotbar");
        
        // Get all seeds from inventory
        CollectSeedsFromInventory();
        
        // Update hotbar slots
        UpdateHotbarSlots();
    }
    
    /// <summary>
    /// Collect all seeds from inventory
    /// </summary>
    void CollectSeedsFromInventory()
    {
        seedInventory.Clear();
        
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[GardenHotbar] InventorySystem not found!");
            return;
        }
        
        List<InventoryItemData> allItems = InventorySystem.Instance.GetAllItems();
        
        foreach (var item in allItems)
        {
            // Check if item is a seed (based on ID suffix or other criteria)
            if (IsSeedItem(item.itemID))
            {
                seedInventory[item.itemID] = item.quantity;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[GardenHotbar] Found seed: {item.itemName} x{item.quantity}");
                }
            }
        }
    }
    
    /// <summary>
    /// Check if an item is a seed
    /// </summary>
    bool IsSeedItem(string itemID)
    {
        // Seeds are identified by having the seed suffix in their ID
        return itemID.Contains(seedIDSuffix);
    }
    
    /// <summary>
    /// Update all hotbar slot displays
    /// </summary>
    void UpdateHotbarSlots()
    {
        // Clear all slots first
        foreach (var slot in hotbarSlots)
        {
            ClearSlot(slot);
        }
        
        // Fill slots with seeds
        int slotIndex = 0;
        
        foreach (var seedEntry in seedInventory)
        {
            if (slotIndex >= hotbarSlots.Length)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[GardenHotbar] Not enough slots! Have {hotbarSlots.Length}, need {seedInventory.Count}");
                }
                break;
            }
            
            string seedID = seedEntry.Key;
            int quantity = seedEntry.Value;
            
            // Get item data from inventory
            InventoryItemData itemData = InventorySystem.Instance.GetItemData(seedID);
            
            if (itemData != null)
            {
                SetSlot(hotbarSlots[slotIndex], itemData, quantity);
                slotIndex++;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GardenHotbar] Updated {slotIndex} slots with seeds");
        }
    }
    
    /// <summary>
    /// Set a slot with seed data
    /// </summary>
    void SetSlot(HotbarSlot slot, InventoryItemData seedData, int quantity)
    {
        if (slot == null) return;
        
        slot.isEmpty = false;
        slot.currentItemID = seedData.itemID;
        
        // Set icon
        if (slot.iconImage != null && seedData.icon != null)
        {
            slot.iconImage.sprite = seedData.icon;
            slot.iconImage.color = Color.white;
            slot.iconImage.gameObject.SetActive(true);
        }
        
        // Set quantity text
        if (slot.quantityText != null)
        {
            slot.quantityText.text = quantity.ToString();
            slot.quantityText.gameObject.SetActive(true);
        }
        
        // Activate slot
        if (slot.slotObject != null)
        {
            slot.slotObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Clear a slot (make it empty)
    /// </summary>
    void ClearSlot(HotbarSlot slot)
    {
        if (slot == null) return;
        
        slot.isEmpty = true;
        slot.currentItemID = null;
        
        // Set to empty sprite
        if (slot.iconImage != null)
        {
            if (slot.emptySprite != null)
            {
                slot.iconImage.sprite = slot.emptySprite;
                slot.iconImage.color = new Color(1f, 1f, 1f, 0.3f); // Transparent
            }
            else
            {
                slot.iconImage.gameObject.SetActive(false);
            }
        }
        
        // Hide quantity text
        if (slot.quantityText != null)
        {
            slot.quantityText.text = "";
            slot.quantityText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get seed data from a specific slot
    /// </summary>
    public InventoryItemData GetSlotSeed(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            Debug.LogWarning($"[GardenHotbar] Invalid slot index: {slotIndex}");
            return null;
        }
        
        HotbarSlot slot = hotbarSlots[slotIndex];
        
        if (slot.isEmpty || string.IsNullOrEmpty(slot.currentItemID))
        {
            return null;
        }
        
        return InventorySystem.Instance?.GetItemData(slot.currentItemID);
    }
    
    /// <summary>
    /// Check if a slot has a seed
    /// </summary>
    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            return true;
        }
        
        return hotbarSlots[slotIndex].isEmpty;
    }
    
    /// <summary>
    /// Get the item ID from a slot
    /// </summary>
    public string GetSlotItemID(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            return null;
        }
        
        return hotbarSlots[slotIndex].currentItemID;
    }
    
    /// <summary>
    /// Get total number of available hotbar slots
    /// </summary>
    public int GetSlotCount()
    {
        return hotbarSlots.Length;
    }
    
    /// <summary>
    /// Get number of occupied slots
    /// </summary>
    public int GetOccupiedSlotCount()
    {
        int count = 0;
        foreach (var slot in hotbarSlots)
        {
            if (!slot.isEmpty)
            {
                count++;
            }
        }
        return count;
    }
}
