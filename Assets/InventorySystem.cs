using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core inventory system. Stores items, tracks quantities, and notifies listeners of changes.
/// Singleton - access via InventorySystem.Instance
/// Now supports storing PlantDataSO references for cross-scene herb access.
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
    /// Add an item to inventory (without herb data)
    /// </summary>
    public void AddItem(string itemID, string itemName, Sprite icon, int quantity = 1)
    {
        AddItem(itemID, itemName, icon, quantity, null);
    }

    /// <summary>
    /// Add an item to inventory (with herb data for journal)
    /// </summary>
    public void AddItem(string itemID, string itemName, Sprite icon, int quantity, PlantDataSO herbData)
    {
        if (quantity <= 0) return;

        if (inventory.ContainsKey(itemID))
        {
            inventory[itemID].quantity += quantity;
            
            if (herbData != null && inventory[itemID].herbData == null)
            {
                inventory[itemID].herbData = herbData;
            }
            
            if (showDebugLogs) Debug.Log($"[Inventory] Added {quantity}x {itemName}. Total: {inventory[itemID].quantity}");
        }
        else
        {
            InventoryItemData newItem = new InventoryItemData(itemID, itemName, icon, quantity, herbData);
            inventory.Add(itemID, newItem);
            
            if (showDebugLogs) Debug.Log($"[Inventory] New item added: {quantity}x {itemName}");
        }

        onItemAddedCallback?.Invoke(itemID, quantity);
        onInventoryChangedCallback?.Invoke();
    }

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

        if (item.quantity <= 0)
        {
            inventory.Remove(itemID);
            if (showDebugLogs) Debug.Log($"[Inventory] {item.itemName} removed from inventory (quantity = 0)");
        }

        onItemRemovedCallback?.Invoke(itemID, quantity);
        onInventoryChangedCallback?.Invoke();

        return true;
    }

    public bool HasItem(string itemID)
    {
        return inventory.ContainsKey(itemID);
    }

    public int GetItemQuantity(string itemID)
    {
        return inventory.ContainsKey(itemID) ? inventory[itemID].quantity : 0;
    }

    public InventoryItemData GetItemData(string itemID)
    {
        return inventory.ContainsKey(itemID) ? inventory[itemID] : null;
    }

    public List<InventoryItemData> GetAllItems()
    {
        return new List<InventoryItemData>(inventory.Values);
    }

    public int GetUniqueItemCount()
    {
        return inventory.Count;
    }

    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var item in inventory.Values)
        {
            total += item.quantity;
        }
        return total;
    }

    public void ClearInventory()
    {
        if (showDebugLogs) Debug.Log("[Inventory] Clearing all items");
        
        inventory.Clear();
        onInventoryChangedCallback?.Invoke();
    }

    public void SetItemDescription(string itemID, string description)
    {
        if (inventory.ContainsKey(itemID))
        {
            inventory[itemID].description = description;
            onInventoryChangedCallback?.Invoke();
        }
    }
}