using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages saving and loading game data to/from JSON files.
/// Singleton - persists across scenes.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Scene to load when starting/loading a game")]
    public string homeIslandSceneName = "HomeIsland";
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    // Current active save
    private SaveData currentSave;
    private string currentSaveFilePath;

    // Save file directory
    private string saveDirectory;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeSaveSystem()
    {
        // Set save directory to persistent data path
        saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
            if (showDebugLogs) Debug.Log($"[SaveLoad] Created save directory: {saveDirectory}");
        }
        
        if (showDebugLogs) Debug.Log($"[SaveLoad] Save system initialized. Directory: {saveDirectory}");
    }

    // ==================== NEW GAME ====================

    /// <summary>
    /// Create a new save file with the given name and start the game.
    /// </summary>
    public void CreateNewSave(string saveName)
    {
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = "New Save";
        }

        // Generate unique save ID
        string saveID = GenerateUniqueSaveID();
        
        // Create new save data
        currentSave = new SaveData
        {
            saveID = saveID,
            saveName = saveName,
            lastSavedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            dayNumber = 1,
            currentTimeMinutes = 360, // 6:00 AM
            playerCurrency = 100, // Starting money (adjust as needed)
            previousDayHarvests = 0,
            totalHerbsCollected = 0,
            inventory = new List<SavedInventoryItem>()
        };

        // Set file path
        currentSaveFilePath = Path.Combine(saveDirectory, $"{saveID}.json");

        // Save immediately
        SaveCurrentGame();

        if (showDebugLogs) Debug.Log($"[SaveLoad] Created new save: {saveName} (ID: {saveID})");

        // Load game state into systems
        LoadGameStateIntoSystems();

        // Load Home Island scene
        SceneManager.LoadScene(homeIslandSceneName);
    }

    /// <summary>
    /// Generate a unique save ID (SaveData_001, SaveData_002, etc.)
    /// </summary>
    string GenerateUniqueSaveID()
    {
        int counter = 1;
        string saveID;

        do
        {
            saveID = $"SaveData_{counter:D3}"; // e.g., SaveData_001
            counter++;
        }
        while (File.Exists(Path.Combine(saveDirectory, $"{saveID}.json")));

        return saveID;
    }

    // ==================== SAVE GAME ====================

    /// <summary>
    /// Save the current game state. Called automatically at end of Phase 2.
    /// </summary>
    public void SaveCurrentGame()
    {
        if (currentSave == null)
        {
            Debug.LogError("[SaveLoad] No active save to save!");
            return;
        }

        // Capture current game state
        CaptureCurrentGameState();

        // Update timestamp
        currentSave.lastSavedTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Serialize to JSON
        string json = JsonUtility.ToJson(currentSave, true);

        // Write to file
        try
        {
            File.WriteAllText(currentSaveFilePath, json);
            if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Game saved: {currentSave.saveName} (Day {currentSave.dayNumber})");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoad] Failed to save game: {e.Message}");
        }
    }

    /// <summary>
    /// Capture current state from all game systems into currentSave.
    /// </summary>
    void CaptureCurrentGameState()
    {
        // Day and time
        if (TimeSystem.Instance != null)
        {
            currentSave.dayNumber = TimeSystem.Instance.GetCurrentDay();
            currentSave.currentTimeMinutes = TimeSystem.Instance.GetCurrentTimeMinutes();
            currentSave.previousDayHarvests = TimeSystem.Instance.GetPreviousDayHarvests();
        }

        // Currency
        if (JournalController.Instance != null)
        {
            currentSave.playerCurrency = JournalController.Instance.GetCurrency();
        }

        // Inventory
        if (InventorySystem.Instance != null)
        {
            currentSave.inventory.Clear();
            List<InventoryItemData> items = InventorySystem.Instance.GetAllItems();
            
            int totalHerbs = 0;
            foreach (var item in items)
            {
                SavedInventoryItem savedItem = new SavedInventoryItem(
                    item.itemID,
                    item.itemName,
                    item.quantity,
                    "", // iconPath - can be reconstructed from PlantDataSO
                    item.herbData != null ? item.herbData.plantID : ""
                );
                currentSave.inventory.Add(savedItem);
                
                totalHerbs += item.quantity;
            }
            
            currentSave.totalHerbsCollected = totalHerbs;
        }

        if (showDebugLogs) Debug.Log($"[SaveLoad] Captured game state: Day {currentSave.dayNumber}, ${currentSave.playerCurrency}, {currentSave.inventory.Count} item types");
    }

    // ==================== LOAD GAME ====================

    /// <summary>
    /// Load a save file and start the game.
    /// </summary>
    public void LoadSave(string saveID)
    {
        string filePath = Path.Combine(saveDirectory, $"{saveID}.json");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[SaveLoad] Save file not found: {saveID}");
            return;
        }

        try
        {
            // Read JSON
            string json = File.ReadAllText(filePath);
            currentSave = JsonUtility.FromJson<SaveData>(json);
            currentSaveFilePath = filePath;

            if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Loaded save: {currentSave.saveName} (Day {currentSave.dayNumber})");

            // Apply save data to game systems
            LoadGameStateIntoSystems();

            // Load Home Island scene
            SceneManager.LoadScene(homeIslandSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoad] Failed to load save: {e.Message}");
        }
    }

    /// <summary>
    /// Apply loaded save data to all game systems.
    /// </summary>
    void LoadGameStateIntoSystems()
    {
        if (currentSave == null) return;

        // Set day and time
        if (TimeSystem.Instance != null)
        {
            // TimeSystem will read from PlayerPrefs on Start, so set them now
            PlayerPrefs.SetInt("CurrentDay", currentSave.dayNumber);
            PlayerPrefs.SetInt("PreviousDayHarvests", currentSave.previousDayHarvests);
            PlayerPrefs.Save();
        }

        // Set currency
        if (JournalController.Instance != null)
        {
            JournalController.Instance.playerCurrency = currentSave.playerCurrency;
        }

        // Restore inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
            
            foreach (var savedItem in currentSave.inventory)
            {
                // Try to find herbData by ID
                PlantDataSO herbData = FindPlantDataByID(savedItem.herbDataID);
                
                Sprite icon = herbData != null ? herbData.icon : null;
                
                InventorySystem.Instance.AddItem(
                    savedItem.itemID,
                    savedItem.itemName,
                    icon,
                    savedItem.quantity,
                    herbData
                );
            }
        }

        if (showDebugLogs) Debug.Log($"[SaveLoad] Game state loaded into systems");
    }

    /// <summary>
    /// Find PlantDataSO by plantID (searches all PlantDataSO assets in project).
    /// </summary>
    PlantDataSO FindPlantDataByID(string plantID)
    {
        if (string.IsNullOrEmpty(plantID)) return null;

        // This requires Resources folder or an asset database
        // For now, we'll rely on PlantNodes having the data
        // You may want to create a PlantDatabase ScriptableObject that holds all PlantDataSO references
        
        // Fallback: Try to find in loaded scenes (from PlantNodes)
        PlantNode[] nodes = FindObjectsOfType<PlantNode>();
        foreach (var node in nodes)
        {
            foreach (var plant in node.plantPool)
            {
                if (plant != null && plant.plantID == plantID)
                {
                    return plant;
                }
            }
        }

        return null;
    }

    // ==================== GET ALL SAVES ====================

    /// <summary>
    /// Get list of all save files with their data.
    /// </summary>
    public List<SaveData> GetAllSaves()
    {
        List<SaveData> saves = new List<SaveData>();

        if (!Directory.Exists(saveDirectory))
        {
            return saves;
        }

        string[] files = Directory.GetFiles(saveDirectory, "*.json");

        foreach (string filePath in files)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                SaveData save = JsonUtility.FromJson<SaveData>(json);
                saves.Add(save);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to read save file {filePath}: {e.Message}");
            }
        }

        // Sort by last saved timestamp (newest first)
        saves.Sort((a, b) => DateTime.Parse(b.lastSavedTimestamp).CompareTo(DateTime.Parse(a.lastSavedTimestamp)));

        if (showDebugLogs) Debug.Log($"[SaveLoad] Found {saves.Count} save files");

        return saves;
    }

    // ==================== DELETE SAVE ====================

    /// <summary>
    /// Delete a save file.
    /// </summary>
    public bool DeleteSave(string saveID)
    {
        string filePath = Path.Combine(saveDirectory, $"{saveID}.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveLoad] Save file not found for deletion: {saveID}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Deleted save: {saveID}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveLoad] Failed to delete save: {e.Message}");
            return false;
        }
    }

    // ==================== GETTERS ====================

    public SaveData GetCurrentSave() => currentSave;
    
    public bool HasActiveSave() => currentSave != null;

    public string GetSaveDirectory() => saveDirectory;
}
