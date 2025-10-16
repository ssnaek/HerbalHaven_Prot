using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core inventory system. Stores items, tracks quantities, and notifies listeners of changes.
/// Singleton - access via InventorySystem.Instance
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Main inventory storage - itemID → item data
    private Dictionary<string, InventoryItemData> inventory = new Dictionary<string, InventoryItemData>();

    // Events for UI systems to listen to
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    public delegate void OnItemAdded(string itemID, int quantityAdded);
    public event OnItemAdded onItemAddedCallback;

    public delegate void OnItemRemoved(string itemID, int quantityRemoved);
    public event OnItemRemoved onItemRemovedCallback;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Add an item to inventory
    /// </summary>
    public void AddItem(string itemID, string itemName, Sprite icon, int quantity = 1)
    {
        if (quantity <= 0) return;

        if (inventory.ContainsKey(itemID))
        {
            // Item exists - increase quantity
            inventory[itemID].quantity += quantity;
            
            if (showDebugLogs) Debug.Log($"[Inventory] Added {quantity}x {itemName}. Total: {inventory[itemID].quantity}");
        }
        else
        {
            // New item - create entry
            InventoryItemData newItem = new InventoryItemData(itemID, itemName, icon, quantity);
            inventory.Add(itemID, newItem);
            
            if (showDebugLogs) Debug.Log($"[Inventory] New item added: {quantity}x {itemName}");
        }

        // Notify listeners
        onItemAddedCallback?.Invoke(itemID, quantity);
        onInventoryChangedCallback?.Invoke();
    }

    /// <summary>
    /// Remove an item from inventory
    /// </summary>
    public bool RemoveItem(string itemID, int quantity = 1)
    {
        if (quantity <= 0) return false;

        if (!inventory.ContainsKey(itemID))
        {
            if (showDebugLogs) Debug.LogWarning($"[Inventory] Cannot remove '{itemID}' - not in inventory");
            return false;
        }

        InventoryItemData item = inventory[itemID];

        if (item.quantity < quantity)
        {
            if (showDebugLogs) Debug.LogWarning($"[Inventory] Not enough {item.itemName}. Have: {item.quantity}, Need: {quantity}");
            return false;
        }

        item.quantity -= quantity;

        if (showDebugLogs) Debug.Log($"[Inventory] Removed {quantity}x {item.itemName}. Remaining: {item.quantity}");

        // Remove from dictionary if quantity reaches 0
        if (item.quantity <= 0)
        {
            inventory.Remove(itemID);
            if (showDebugLogs) Debug.Log($"[Inventory] {item.itemName} removed from inventory (quantity = 0)");
        }

        // Notify listeners
        onItemRemovedCallback?.Invoke(itemID, quantity);
        onInventoryChangedCallback?.Invoke();

        return true;
    }

    /// <summary>
    /// Check if inventory has a specific item
    /// </summary>
    public bool HasItem(string itemID)
    {
        return inventory.ContainsKey(itemID);
    }

    /// <summary>
    /// Get quantity of a specific item
    /// </summary>
    public int GetItemQuantity(string itemID)
    {
        return inventory.ContainsKey(itemID) ? inventory[itemID].quantity : 0;
    }

    /// <summary>
    /// Get item data for a specific item
    /// </summary>
    public InventoryItemData GetItemData(string itemID)
    {
        return inventory.ContainsKey(itemID) ? inventory[itemID] : null;
    }

    /// <summary>
    /// Get all items in inventory (for UI display)
    /// </summary>
    public List<InventoryItemData> GetAllItems()
    {
        return new List<InventoryItemData>(inventory.Values);
    }

    /// <summary>
    /// Get total number of unique items
    /// </summary>
    public int GetUniqueItemCount()
    {
        return inventory.Count;
    }

    /// <summary>
    /// Get total quantity of all items combined
    /// </summary>
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var item in inventory.Values)
        {
            total += item.quantity;
        }
        return total;
    }

    /// <summary>
    /// Clear entire inventory
    /// </summary>
    public void ClearInventory()
    {
        if (showDebugLogs) Debug.Log("[Inventory] Clearing all items");
        
        inventory.Clear();
        onInventoryChangedCallback?.Invoke();
    }

    /// <summary>
    /// Set description for an item (useful for journal entries)
    /// </summary>
    public void SetItemDescription(string itemID, string description)
    {
        if (inventory.ContainsKey(itemID))
        {
            inventory[itemID].description = description;
            onInventoryChangedCallback?.Invoke();
        }
    }
}