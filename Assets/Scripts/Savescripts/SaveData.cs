using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data structure for a single save file.
/// Contains all player progression that needs to persist.
/// </summary>
[System.Serializable]
public class SaveData
{
    // Save file metadata
    public string saveID;                    // Unique identifier (e.g., "SaveData_001")
    public string saveName;                  // Player-chosen name (e.g., "My First Playthrough")
    public string lastSavedTimestamp;        // Real-world time (e.g., "2025-10-31 14:30:00")
    
    // Game progression
    public int dayNumber;                    // Current day (e.g., 5)
    public int currentTimeMinutes;           // Time of day in minutes (e.g., 360 = 6:00 AM)
    public int playerCurrency;               // Money for shop
    public int previousDayHarvests;          // For plant regeneration
    
    // Statistics (for UI display)
    public int totalHerbsCollected;          // Lifetime herb count
    
    // Inventory data
    public List<SavedInventoryItem> inventory = new List<SavedInventoryItem>();
    
    // Constructor
    public SaveData()
    {
        saveID = "";
        saveName = "New Save";
        lastSavedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        dayNumber = 1;
        currentTimeMinutes = 360; // 6:00 AM
        playerCurrency = 0;
        previousDayHarvests = 0;
        totalHerbsCollected = 0;
        inventory = new List<SavedInventoryItem>();
    }
}

/// <summary>
/// Serializable inventory item for saving.
/// </summary>
[System.Serializable]
public class SavedInventoryItem
{
    public string itemID;
    public string itemName;
    public int quantity;
    public string iconPath;      // Path to icon sprite (if needed for restoration)
    public string herbDataID;    // PlantDataSO identifier for herb reference
    
    public SavedInventoryItem(string id, string name, int qty, string iconPath = "", string herbID = "")
    {
        itemID = id;
        itemName = name;
        quantity = qty;
        this.iconPath = iconPath;
        herbDataID = herbID;
    }
}
