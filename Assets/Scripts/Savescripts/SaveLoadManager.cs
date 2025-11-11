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
            
            // Subscribe to scene loaded events to clear pot reset flag
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Clear the pot reset flag after any scene loads (gives all pots time to read it)
        if (PlayerPrefs.GetInt("ResetAllPots", 0) == 1)
        {
            StartCoroutine(ClearPotResetFlagDelayed());
        }
        
        // Re-apply day number if loading from a save (fixes race condition)
        if (currentSave != null && TimeSystem.Instance != null)
        {
            // Force the correct day again after scene loads
            TimeSystem.Instance.ForceSetDay(currentSave.dayNumber);
            if (showDebugLogs) Debug.Log($"[SaveLoad] Re-forced day to {currentSave.dayNumber} after scene load");
        }
    }

    System.Collections.IEnumerator ClearPotResetFlagDelayed()
    {
        // Wait for end of frame to ensure all pots have loaded and read the flag
        yield return new WaitForEndOfFrame();
        
        PlayerPrefs.DeleteKey("ResetAllPots");
        PlayerPrefs.Save();
        
        if (showDebugLogs) Debug.Log("[SaveLoad] Cleared pot reset flag after scene load");
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

        Debug.Log("[SaveLoad] ===== CREATING NEW GAME =====");

        // CRITICAL: Clear old session data before creating new save
        ClearGameStatePlayerPrefs();
        
        // CRITICAL: Clear the InventorySystem completely
        if (InventorySystem.Instance != null)
        {
            Debug.Log("[SaveLoad] Clearing InventorySystem for new game...");
            InventorySystem.Instance.ClearInventory();
            InventorySystem.Instance.SetCurrency(InventorySystem.Instance.startingCurrency);
            Debug.Log("[SaveLoad] ✓ InventorySystem cleared and reset to starting currency");
        }
        
        // Force reset TimeSystem if it exists (from previous session)
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.ForceSetDay(1);
            if (showDebugLogs) Debug.Log("[SaveLoad] Reset TimeSystem to Day 1 for new game");
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
            playerCurrency = InventorySystem.Instance != null ? InventorySystem.Instance.startingCurrency : 100,
            previousDayHarvests = 0,
            totalHerbsCollected = 0,
            inventory = new List<SavedInventoryItem>()
        };

        // Set file path
        currentSaveFilePath = Path.Combine(saveDirectory, $"{saveID}.json");

        // Save immediately
        SaveCurrentGame();

        Debug.Log($"[SaveLoad] ✓ Created new save: {saveName} (ID: {saveID})");
        Debug.Log("[SaveLoad] ===== NEW GAME CREATED =====");

        // Load game state into systems (should have nothing to load since it's new)
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

        // DEBUG: Log where this is being called from
        Debug.Log($"[SaveLoad] === SAVING GAME === Called from:\n{System.Environment.StackTrace}");

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
            if (showDebugLogs) Debug.Log($"[SaveLoad] âœ“ Game saved: {currentSave.saveName} (Day {currentSave.dayNumber})");
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
        if (InventorySystem.Instance != null)
        {
            currentSave.playerCurrency = InventorySystem.Instance.GetCurrency();
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

        // Capture pot states
        CapturePotStates();

        if (showDebugLogs) Debug.Log($"[SaveLoad] Captured game state: Day {currentSave.dayNumber}, ${currentSave.playerCurrency}, {currentSave.inventory.Count} item types, {currentSave.pots.Count} pots");
    }

    /// <summary>
    /// Capture all pot states from PlayerPrefs (since pots may not be in current scene when saving)
    /// </summary>
    void CapturePotStates()
    {
        currentSave.pots.Clear();
        
        // Get list of registered pot IDs from PlayerPrefs
        string potListJson = PlayerPrefs.GetString("RegisteredPotIDs", "");
        if (string.IsNullOrEmpty(potListJson))
        {
            if (showDebugLogs) Debug.Log("[SaveLoad] No registered pot IDs found in PlayerPrefs");
            return;
        }
        
        // Parse the JSON array of pot IDs
        try
        {
            PotIDList potIDList = JsonUtility.FromJson<PotIDList>(potListJson);
            if (potIDList == null || potIDList.potIDs == null)
            {
                if (showDebugLogs) Debug.Log("[SaveLoad] Could not parse pot ID list");
                return;
            }
            
            // Read each pot's state from PlayerPrefs
            foreach (string potID in potIDList.potIDs)
            {
                bool isPlanted = PlayerPrefs.GetInt($"Pot_{potID}_IsPlanted", 0) == 1;
                string seedID = PlayerPrefs.GetString($"Pot_{potID}_SeedID", "");
                int dayPlanted = PlayerPrefs.GetInt($"Pot_{potID}_DayPlanted", 0);
                bool isFullyGrown = PlayerPrefs.GetInt($"Pot_{potID}_IsFullyGrown", 0) == 1;
                
                SavedPotData potData = new SavedPotData(
                    potID,
                    isPlanted,
                    seedID,
                    dayPlanted,
                    isFullyGrown
                );
                currentSave.pots.Add(potData);
            }
            
            if (showDebugLogs) Debug.Log($"[SaveLoad] Captured {currentSave.pots.Count} pots from PlayerPrefs");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoad] Error parsing pot IDs: {e.Message}");
        }
    }
    
    /// <summary>
    /// Helper class for JSON serialization of pot ID list
    /// </summary>
    [System.Serializable]
    class PotIDList
    {
        public List<string> potIDs = new List<string>();
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
            Debug.Log("[SaveLoad] ===== LOADING SAVE =====");
            
            // CLEAR PlayerPrefs first to ensure clean state
            ClearGameStatePlayerPrefs();
            Debug.Log("[SaveLoad] PlayerPrefs cleared before loading save");
            
            // Read JSON
            string json = File.ReadAllText(filePath);
            currentSave = JsonUtility.FromJson<SaveData>(json);
            currentSaveFilePath = filePath;

            Debug.Log($"[SaveLoad] Loaded save: {currentSave.saveName} (Day {currentSave.dayNumber})");

            // Apply save data to game systems
            LoadGameStateIntoSystems();

            Debug.Log("[SaveLoad] ===== SAVE LOADED =====");
            
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

        if (showDebugLogs) Debug.Log($"[SaveLoad] Loading game state: Day {currentSave.dayNumber}, ${currentSave.playerCurrency}");

        // Set day and time - FORCE SET to avoid race conditions
        if (TimeSystem.Instance != null)
        {
            // Use ForceSetDay to directly set the day (bypasses PlayerPrefs read race condition)
            TimeSystem.Instance.ForceSetDay(currentSave.dayNumber);
            
            // Also set PlayerPrefs for backup
            PlayerPrefs.SetInt("CurrentDay", currentSave.dayNumber);
            PlayerPrefs.SetInt("PreviousDayHarvests", currentSave.previousDayHarvests);
            PlayerPrefs.Save();
            
            if (showDebugLogs) Debug.Log($"[SaveLoad] TimeSystem forced to Day {currentSave.dayNumber}");
        }
        else
        {
            // TimeSystem doesn't exist yet - set PlayerPrefs so it loads correctly
            PlayerPrefs.SetInt("CurrentDay", currentSave.dayNumber);
            PlayerPrefs.SetInt("PreviousDayHarvests", currentSave.previousDayHarvests);
            PlayerPrefs.Save();
            
            if (showDebugLogs) Debug.Log($"[SaveLoad] TimeSystem not found - set PlayerPrefs for Day {currentSave.dayNumber}");
        }

        // Set currency
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.SetCurrency(currentSave.playerCurrency);
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

        // Restore pot states to PlayerPrefs for PotManager instances to load
        RestorePotStates();

        if (showDebugLogs) Debug.Log($"[SaveLoad] Game state loaded into systems");
    }

    /// <summary>
    /// Restore pot states from currentSave to PlayerPrefs for PotManager instances to read.
    /// </summary>
    void RestorePotStates()
    {
        if (currentSave == null || currentSave.pots == null)
        {
            if (showDebugLogs) Debug.Log("[SaveLoad] No pot data to restore");
            return;
        }

        foreach (var potData in currentSave.pots)
        {
            PlayerPrefs.SetInt($"Pot_{potData.potID}_IsPlanted", potData.isPlanted ? 1 : 0);
            PlayerPrefs.SetString($"Pot_{potData.potID}_SeedID", potData.seedID ?? "");
            PlayerPrefs.SetInt($"Pot_{potData.potID}_DayPlanted", potData.dayPlanted);
            PlayerPrefs.SetInt($"Pot_{potData.potID}_IsFullyGrown", potData.isFullyGrown ? 1 : 0);
        }

        PlayerPrefs.Save();
        if (showDebugLogs) Debug.Log($"[SaveLoad] Restored {currentSave.pots.Count} pots to PlayerPrefs");
    }

    /// <summary>
    /// Find PlantDataSO by plantID (searches all PlantDataSO assets in project).
    /// </summary>
    PlantDataSO FindPlantDataByID(string plantID)
    {
        if (string.IsNullOrEmpty(plantID))
        {
            if (showDebugLogs) Debug.Log("[SaveLoad] FindPlantDataByID: plantID is empty");
            return null;
        }

        if (showDebugLogs) Debug.Log($"[SaveLoad] Searching for PlantDataSO with ID: {plantID}");

        // Method 1: Load from Resources/Assets/Plants/ folder
        PlantDataSO[] allPlants = Resources.LoadAll<PlantDataSO>("Assets/Plants");
        if (showDebugLogs) Debug.Log($"[SaveLoad] Found {allPlants.Length} PlantDataSO assets in Resources/Assets/Plants");
        
        foreach (var plant in allPlants)
        {
            if (plant != null && plant.plantID == plantID)
            {
                if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Found PlantDataSO: {plant.plantName} (ID: {plant.plantID})");
                return plant;
            }
        }
        
        // Method 2: Try root Assets folder
        allPlants = Resources.LoadAll<PlantDataSO>("Assets");
        foreach (var plant in allPlants)
        {
            if (plant != null && plant.plantID == plantID)
            {
                if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Found PlantDataSO in root Assets: {plant.plantName}");
                return plant;
            }
        }

        // Method 3: Fallback - Try to find in loaded scenes (from PlantNodes)
        PlantNode[] nodes = FindObjectsOfType<PlantNode>();
        foreach (var node in nodes)
        {
            foreach (var plant in node.plantPool)
            {
                if (plant != null && plant.plantID == plantID)
                {
                    if (showDebugLogs) Debug.Log($"[SaveLoad] ✓ Found PlantDataSO from PlantNode: {plant.plantName}");
                    return plant;
                }
            }
        }

        Debug.LogError($"[SaveLoad] ✗ Could not find PlantDataSO with ID '{plantID}'! Searched {allPlants.Length} plant assets.");
        return null;
    }

    // ==================== CLEAN UP PLAYERPREFS ====================

    /// <summary>
    /// Clear game-related PlayerPrefs to prevent conflicts between sessions.
    /// Called when loading a save or creating new game to ensure clean state.
    /// Note: Does NOT clear audio settings or other user preferences.
    /// </summary>
    void ClearGameStatePlayerPrefs()
    {
        if (showDebugLogs) Debug.Log("[SaveLoad] Clearing old game state from PlayerPrefs...");

        // Clear TimeSystem data
        PlayerPrefs.DeleteKey("CurrentDay");
        PlayerPrefs.DeleteKey("PreviousDayHarvests");
        
        // Clear all pot-related keys
        ClearAllPotPlayerPrefs();
        
        // Note: We intentionally DO NOT clear:
        // - Audio settings (volume keys)
        // - Other user preferences
        
        PlayerPrefs.Save();
        
        if (showDebugLogs) Debug.Log("[SaveLoad] Old game state cleared from PlayerPrefs");
    }

    /// <summary>
    /// Clear all pot-related PlayerPrefs keys.
    /// This removes state from previous sessions to prevent conflicts.
    /// </summary>
    void ClearAllPotPlayerPrefs()
    {
        Debug.Log("[SaveLoad] ===== CLEARING ALL POT PLAYERPREFS =====");
        
        // NUCLEAR OPTION: Clear ALL PlayerPrefs (except we'll restore audio settings after)
        // This ensures NO pot data survives
        
        // Save audio settings if they exist
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        // CLEAR EVERYTHING
        PlayerPrefs.DeleteAll();
        
        // Restore audio settings
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        
        // Set a flag that tells PotManager instances to reset themselves
        PlayerPrefs.SetInt("ResetAllPots", 1);
        PlayerPrefs.Save(); // CRITICAL: Save immediately!
        
        Debug.Log("[SaveLoad] ✓ DELETED ALL PLAYERPREFS and set ResetAllPots flag");
        Debug.Log("[SaveLoad] ===== POT CLEARING COMPLETE =====");
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
            if (showDebugLogs) Debug.Log($"[SaveLoad] âœ“ Deleted save: {saveID}");
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