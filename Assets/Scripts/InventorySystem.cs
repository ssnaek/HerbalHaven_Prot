using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core inventory system. Stores items, tracks quantities, and manages currency.
/// Singleton - access via InventorySystem.Instance
/// Now supports storing PlantDataSO references for cross-scene herb access.
/// Currency system moved here for persistent cross-scene management.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Currency")]
    [Tooltip("Starting currency for new games")]
    public int startingCurrency = 100;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Main inventory storage - itemID → item data
    private Dictionary<string, InventoryItemData> inventory = new Dictionary<string, InventoryItemData>();

    // Currency
    private int playerCurrency = 0;

    // Events for UI systems to listen to
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    public delegate void OnItemAdded(string itemID, int quantityAdded);
    public event OnItemAdded onItemAddedCallback;

    public delegate void OnItemRemoved(string itemID, int quantityRemoved);
    public event OnItemRemoved onItemRemovedCallback;

    public delegate void OnCurrencyChanged(int newAmount);
    public event OnCurrencyChanged onCurrencyChangedCallback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize currency with starting amount
            playerCurrency = startingCurrency;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // ==================== INVENTORY METHODS ====================

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

    // ==================== CURRENCY METHODS ====================

    /// <summary>
    /// Get current player currency
    /// </summary>
    public int GetCurrency()
    {
        return playerCurrency;
    }

    /// <summary>
    /// Set currency to a specific amount (used by save/load system)
    /// </summary>
    public void SetCurrency(int amount)
    {
        playerCurrency = Mathf.Max(0, amount);
        
        if (showDebugLogs) Debug.Log($"[Inventory] Currency set to ${playerCurrency}");
        
        onCurrencyChangedCallback?.Invoke(playerCurrency);
    }

    /// <summary>
    /// Add currency to player's wallet
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        playerCurrency += amount;
        
        if (showDebugLogs) Debug.Log($"[Inventory] Added ${amount}. Total: ${playerCurrency}");
        
        onCurrencyChangedCallback?.Invoke(playerCurrency);
    }

    /// <summary>
    /// Remove currency from player's wallet. Returns true if successful.
    /// </summary>
    public bool RemoveCurrency(int amount)
    {
        if (amount <= 0) return false;

        if (playerCurrency >= amount)
        {
            playerCurrency -= amount;
            
            if (showDebugLogs) Debug.Log($"[Inventory] Spent ${amount}. Remaining: ${playerCurrency}");
            
            onCurrencyChangedCallback?.Invoke(playerCurrency);
            return true;
        }
        
        if (showDebugLogs) Debug.Log($"[Inventory] Not enough currency. Need ${amount}, have ${playerCurrency}");
        return false;
    }

    /// <summary>
    /// Check if player has enough currency
    /// </summary>
    public bool HasEnoughCurrency(int amount)
    {
        return playerCurrency >= amount;
    }
}