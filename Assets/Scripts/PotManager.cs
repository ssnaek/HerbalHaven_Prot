using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Maps a seed ID to its fully grown plant sprite and harvest data
/// </summary>
[System.Serializable]
public class PlantSpriteMapping
{
    [Tooltip("The seed item ID (e.g., 'garlic_seed')")]
    public string seedID;
    
    [Tooltip("The sprite to show when this plant is fully grown")]
    public Sprite grownPlantSprite;
    
    [Header("Harvest Settings")]
    [Tooltip("The PlantDataSO asset that will be added to inventory when harvested")]
    public PlantDataSO harvestPlantData;
    
    [Tooltip("How many plants to give when harvested")]
    public int harvestQuantity = 1;
}

/// <summary>
/// Manages individual pot interactions and planting seeds.
/// Attach this script to each pot GameObject.
/// </summary>
public class PotManager : MonoBehaviour
{
    [Header("Pot Sprites")]
    [Tooltip("Empty pot sprite (default state)")]
    public Sprite emptyPotSprite;
    
    [Tooltip("Pot with sprout sprite (after planting)")]
    public Sprite sproutPotSprite;
    
    [Header("Plant Growth")]
    [Tooltip("Mappings of seed IDs to their fully grown plant sprites")]
    public PlantSpriteMapping[] plantSpriteMappings = new PlantSpriteMapping[7]
    {
        new PlantSpriteMapping { seedID = "garlic_seed" },
        new PlantSpriteMapping { seedID = "ginger_seed" },
        new PlantSpriteMapping { seedID = "lagundi_seed" },
        new PlantSpriteMapping { seedID = "tangad_seed" },
        new PlantSpriteMapping { seedID = "yerbabuena_seed" },
        new PlantSpriteMapping { seedID = "oregano_seed" },
        new PlantSpriteMapping { seedID = "maria_seed" }
    };
    
    [Header("Components")]
    [Tooltip("Image component that displays the pot sprite")]
    public Image potImage;
    
    [Tooltip("Optional: Separate Image for grown plant that can overflow the grid cell. If null, uses potImage instead.")]
    public Image plantOverlayImage;
    
    [Header("Plant Overlay Settings")]
    [Tooltip("Allow plant overlay to ignore layout constraints and grow tall")]
    public bool allowPlantOverflow = true;
    
    [Tooltip("Offset for plant overlay position (useful for positioning tall plants)")]
    public Vector2 plantOverlayOffset = new Vector2(0, 50);
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Runtime state
    private bool isPlanted = false;
    private string plantedSeedID = null;
    private int dayPlanted = -1; // Track which day the seed was planted
    private bool isFullyGrown = false;
    private string potID; // Unique identifier for this pot
    private bool stateNeedsSaving = false;
    
    void Awake()
    {
        // Initialize unique pot ID
        InitializePotID();
        
        // Load saved state
        LoadPotState();
        
        // Auto-find Image component if not assigned
        if (potImage == null)
        {
            potImage = GetComponent<Image>();
        }
        
        // Auto-find Button component or add one
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        // Add click listener
        button.onClick.AddListener(OnPotClicked);
        
        // Setup plant overlay if enabled
        SetupPlantOverlay();
        
        // Subscribe to scene unload to save state
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    /// <summary>
    /// Setup the plant overlay image for tall plants
    /// </summary>
    void SetupPlantOverlay()
    {
        if (plantOverlayImage != null && allowPlantOverflow)
        {
            // Make sure the overlay starts hidden
            plantOverlayImage.enabled = false;
            
            // Add LayoutElement to ignore layout constraints
            LayoutElement layoutElement = plantOverlayImage.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = plantOverlayImage.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;
            
            // Apply offset
            RectTransform rectTransform = plantOverlayImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = plantOverlayOffset;
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[PotManager] Plant overlay setup complete for pot '{gameObject.name}'");
            }
        }
    }
    
    void OnEnable()
    {
        // Check for reset flag again (in case it was set after Awake)
        int resetFlag = PlayerPrefs.GetInt("ResetAllPots", 0);
        if (resetFlag == 1)
        {
            Debug.Log($"[PotManager] *** OnEnable: Reset flag detected for {gameObject.name} - RESETTING! ***");
            ResetPot();
            stateNeedsSaving = true;
            SavePotState();
        }
        
        // Subscribe to TimeSystem's new day event
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.onNewDayCallback += OnNewDay;
        }
    }
    
    void OnDisable()
    {
        // Unsubscribe from TimeSystem's new day event
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.onNewDayCallback -= OnNewDay;
        }
    }
    
    void Start()
    {
        // FINAL SAFETY CHECK: Check for reset flag one more time
        int resetFlag = PlayerPrefs.GetInt("ResetAllPots", 0);
        if (resetFlag == 1 && isPlanted)
        {
            Debug.Log($"[PotManager] *** Start: Reset flag still active and pot is planted - FORCING RESET for {gameObject.name}! ***");
            ResetPot();
            stateNeedsSaving = true;
            SavePotState();
        }
        
        // If TimeSystem already exists, subscribe to it
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.onNewDayCallback += OnNewDay;
        }
        
        // After loading, check if plant should be grown
        CheckPlantGrowth();
    }
    
    void OnDestroy()
    {
        // Save before destruction
        if (stateNeedsSaving)
        {
            SavePotState();
            if (showDebugLogs)
                Debug.Log($"[PotManager] Saved pot state on destroy: {gameObject.name}");
        }
        
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    void OnApplicationQuit()
    {
        // Save when application quits
        if (stateNeedsSaving)
        {
            SavePotState();
            if (showDebugLogs)
                Debug.Log($"[PotManager] Saved pot state on application quit: {gameObject.name}");
        }
    }
    
    void OnSceneUnloaded(Scene scene)
    {
        // Save when scene unloads (more reliable than OnDestroy)
        if (stateNeedsSaving)
        {
            SavePotState();
            if (showDebugLogs)
                Debug.Log($"[PotManager] Saved pot state on scene unload: {scene.name} - {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Called when a new day starts
    /// </summary>
    void OnNewDay(int newDay)
    {
        if (showDebugLogs) Debug.Log($"[PotManager] New day: {newDay}, Pot '{gameObject.name}' - Planted: {isPlanted}, DayPlanted: {dayPlanted}, FullyGrown: {isFullyGrown}");
        
        // If a seed is planted and hasn't grown yet, grow it
        if (isPlanted && !isFullyGrown && dayPlanted > 0 && dayPlanted < newDay)
        {
            GrowPlant();
        }
    }
    
    /// <summary>
    /// Called when pot is clicked
    /// </summary>
    void OnPotClicked()
    {
        if (showDebugLogs) Debug.Log($"[PotManager] Pot '{gameObject.name}' clicked. IsPlanted: {isPlanted}, IsFullyGrown: {isFullyGrown}");
        
        // If plant is fully grown, harvest it
        if (isFullyGrown)
        {
            HarvestPlant();
            return;
        }
        
        // Otherwise, try to plant a seed
        // Check if there's a selected seed from GardenHotbar
        if (GardenHotbar.Instance != null)
        {
            string selectedSeedID = GardenHotbar.Instance.GetSelectedSeedID();
            
            if (!string.IsNullOrEmpty(selectedSeedID))
            {
                // Try to plant the selected seed
                PlantSeed(selectedSeedID);
            }
            else
            {
                if (showDebugLogs) Debug.Log("[PotManager] No seed selected. Click a seed slot first.");
            }
        }
        else
        {
            Debug.LogWarning("[PotManager] GardenHotbar.Instance is null!");
        }
    }
    
    /// <summary>
    /// Plant a seed in this pot
    /// </summary>
    public bool PlantSeed(string seedID)
    {
        if (isPlanted)
        {
            if (showDebugLogs) Debug.Log($"[PotManager] Pot '{gameObject.name}' is already planted!");
            return false;
        }
        
        if (string.IsNullOrEmpty(seedID))
        {
            if (showDebugLogs) Debug.LogWarning("[PotManager] Cannot plant - seedID is null or empty");
            return false;
        }
        
        // Check if player has this seed in inventory
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[PotManager] InventorySystem.Instance is null!");
            return false;
        }
        
        if (!InventorySystem.Instance.HasItem(seedID))
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Player doesn't have seed: {seedID}");
            return false;
        }
        
        int quantity = InventorySystem.Instance.GetItemQuantity(seedID);
        if (quantity <= 0)
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Not enough seeds: {seedID}");
            return false;
        }
        
        // Remove one seed from inventory
        bool removed = InventorySystem.Instance.RemoveItem(seedID, 1);
        if (!removed)
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Failed to remove seed from inventory: {seedID}");
            return false;
        }
        
        // Update pot state
        isPlanted = true;
        plantedSeedID = seedID;
        
        // Track which day the seed was planted
        if (TimeSystem.Instance != null)
        {
            dayPlanted = TimeSystem.Instance.GetCurrentDay();
        }
        
        // Change sprite to sprout
        if (potImage != null && sproutPotSprite != null)
        {
            potImage.sprite = sproutPotSprite;
        }
        else if (potImage != null)
        {
            Debug.LogWarning($"[PotManager] Sprout sprite not assigned for pot '{gameObject.name}'");
        }
        
        // Clear selection in hotbar
        if (GardenHotbar.Instance != null)
        {
            GardenHotbar.Instance.ClearSelection();
        }
        
        if (showDebugLogs)
        {
            InventoryItemData seedData = InventorySystem.Instance.GetItemData(seedID);
            string seedName = seedData != null ? seedData.itemName : seedID;
            Debug.Log($"[PotManager] Planted {seedName} in pot '{gameObject.name}'");
        }
        
        // Mark state as needing saving and save immediately
        stateNeedsSaving = true;
        SavePotState();
        
        return true;
    }
    
    /// <summary>
    /// Grow the planted seed into a fully grown plant
    /// </summary>
    void GrowPlant()
    {
        if (!isPlanted || string.IsNullOrEmpty(plantedSeedID))
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Cannot grow plant - no seed planted in pot '{gameObject.name}'");
            return;
        }
        
        if (isFullyGrown)
        {
            if (showDebugLogs) Debug.Log($"[PotManager] Plant already fully grown in pot '{gameObject.name}'");
            return;
        }
        
        // Find the matching sprite for this seed
        Sprite grownSprite = GetGrownPlantSprite(plantedSeedID);
        
        if (grownSprite == null)
        {
            Debug.LogWarning($"[PotManager] No grown plant sprite found for seed '{plantedSeedID}' in pot '{gameObject.name}'. Please assign the sprite in the Inspector.");
            return;
        }
        
        // Always use overlay for fully grown plants if it exists, otherwise fall back to pot image
        Image targetImage = (plantOverlayImage != null) ? plantOverlayImage : potImage;
        
        // Change sprite to fully grown plant
        if (targetImage != null)
        {
            targetImage.sprite = grownSprite;
            targetImage.enabled = true;
            targetImage.preserveAspect = true; // Preserve aspect ratio for tall plants
            isFullyGrown = true;
            
            // If using overlay, ensure it's properly set up and enabled
            if (targetImage == plantOverlayImage)
            {
                // Enable the overlay GameObject
                plantOverlayImage.gameObject.SetActive(true);
                
                // Ensure overlay is set up for overflow (even if allowPlantOverflow was false)
                LayoutElement layoutElement = plantOverlayImage.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = plantOverlayImage.gameObject.AddComponent<LayoutElement>();
                }
                layoutElement.ignoreLayout = true;
                
                // Apply offset
                RectTransform rectTransform = plantOverlayImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = plantOverlayOffset;
                }
            }
            
            if (showDebugLogs)
            {
                string imageType = (targetImage == plantOverlayImage) ? "overlay" : "pot";
                Debug.Log($"[PotManager] 🌱 Plant grew! Pot '{gameObject.name}' now showing fully grown '{plantedSeedID}' sprite on {imageType} image");
            }
            
            // Mark state as needing saving and save immediately
            stateNeedsSaving = true;
            SavePotState();
        }
    }
    
    /// <summary>
    /// Harvest the fully grown plant and add it to inventory
    /// </summary>
    void HarvestPlant()
    {
        if (!isFullyGrown)
        {
            if (showDebugLogs) Debug.LogWarning($"[PotManager] Cannot harvest - plant not fully grown in pot '{gameObject.name}'");
            return;
        }
        
        if (string.IsNullOrEmpty(plantedSeedID))
        {
            Debug.LogError($"[PotManager] Cannot harvest - no seed ID stored in pot '{gameObject.name}'");
            return;
        }
        
        // Find the harvest data for this seed
        PlantSpriteMapping mapping = GetPlantMapping(plantedSeedID);
        
        if (mapping == null)
        {
            Debug.LogWarning($"[PotManager] No plant mapping found for seed '{plantedSeedID}' in pot '{gameObject.name}'");
            return;
        }
        
        if (mapping.harvestPlantData == null)
        {
            Debug.LogWarning($"[PotManager] No harvest plant data assigned for seed '{plantedSeedID}' in pot '{gameObject.name}'. Please assign PlantDataSO in the Inspector.");
            return;
        }
        
        // Check if InventorySystem exists
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[PotManager] InventorySystem.Instance is null! Cannot add harvested plant.");
            return;
        }
        
        // Add the plant to inventory
        PlantDataSO plantData = mapping.harvestPlantData;
        int quantity = mapping.harvestQuantity > 0 ? mapping.harvestQuantity : 1;
        
        InventorySystem.Instance.AddItem(
            plantData.plantID,
            plantData.plantName,
            plantData.icon,
            quantity,
            plantData
        );
        
        if (showDebugLogs)
        {
            Debug.Log($"[PotManager] 🌿 Harvested {quantity}x {plantData.plantName} from pot '{gameObject.name}'");
        }
        
        // Reset the pot to empty state
        ResetPot();
        
        // Mark state as needing saving and save immediately
        stateNeedsSaving = true;
        SavePotState();
    }
    
    /// <summary>
    /// Get the plant mapping for a given seed ID
    /// </summary>
    PlantSpriteMapping GetPlantMapping(string seedID)
    {
        foreach (PlantSpriteMapping mapping in plantSpriteMappings)
        {
            if (mapping.seedID == seedID)
            {
                return mapping;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get the fully grown plant sprite for a given seed ID
    /// </summary>
    Sprite GetGrownPlantSprite(string seedID)
    {
        PlantSpriteMapping mapping = GetPlantMapping(seedID);
        return mapping?.grownPlantSprite;
    }
    
    /// <summary>
    /// Check if this pot is planted
    /// </summary>
    public bool IsPlanted()
    {
        return isPlanted;
    }
    
    /// <summary>
    /// Get the seed ID planted in this pot
    /// </summary>
    public string GetPlantedSeedID()
    {
        return plantedSeedID;
    }
    
    /// <summary>
    /// Get the unique pot ID
    /// </summary>
    public string GetPotID()
    {
        return potID;
    }
    
    /// <summary>
    /// Reset pot to empty state (for testing/debugging)
    /// </summary>
    public void ResetPot()
    {
        isPlanted = false;
        plantedSeedID = null;
        dayPlanted = -1;
        isFullyGrown = false;
        
        if (potImage != null && emptyPotSprite != null)
        {
            potImage.sprite = emptyPotSprite;
        }
        
        // Hide plant overlay if it exists
        if (plantOverlayImage != null)
        {
            plantOverlayImage.enabled = false;
            plantOverlayImage.sprite = null;
        }
    }
    
    /// <summary>
    /// Check if this plant is fully grown
    /// </summary>
    public bool IsFullyGrown()
    {
        return isFullyGrown;
    }
    
    /// <summary>
    /// Get the day the seed was planted
    /// </summary>
    public int GetDayPlanted()
    {
        return dayPlanted;
    }
    
    #region Save/Load System
    
    /// <summary>
    /// Initialize unique pot ID based on scene and GameObject name/position
    /// </summary>
    void InitializePotID()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        RectTransform rectTransform = GetComponent<RectTransform>();
        
        // Use scene name + GameObject name + position for unique ID
        // For UI elements, position might be more reliable than transform.position
        if (rectTransform != null)
        {
            Vector2 anchoredPos = rectTransform.anchoredPosition;
            potID = $"{currentScene.name}_{gameObject.name}_{anchoredPos.x:F1}_{anchoredPos.y:F1}";
        }
        else
        {
            Vector3 pos = transform.position;
            potID = $"{currentScene.name}_{gameObject.name}_{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";
        }
        
        if (showDebugLogs)
            Debug.Log($"[PotManager] Pot ID: {potID}");
    }
    
    /// <summary>
    /// Save pot state to PlayerPrefs
    /// </summary>
    void SavePotState()
    {
        if (string.IsNullOrEmpty(potID))
        {
            Debug.LogWarning($"[PotManager] Cannot save - pot ID not initialized for '{gameObject.name}'");
            return;
        }
        
        // Save individual values
        PlayerPrefs.SetInt(GetIsPlantedKey(), isPlanted ? 1 : 0);
        PlayerPrefs.SetString(GetSeedIDKey(), plantedSeedID ?? "");
        PlayerPrefs.SetInt(GetDayPlantedKey(), dayPlanted);
        PlayerPrefs.SetInt(GetIsFullyGrownKey(), isFullyGrown ? 1 : 0);
        PlayerPrefs.Save();
        
        stateNeedsSaving = false;
        
        if (showDebugLogs)
            Debug.Log($"[PotManager] Saved state for pot '{gameObject.name}': Planted={isPlanted}, Seed={plantedSeedID}, Day={dayPlanted}, Grown={isFullyGrown}");
    }
    
    /// <summary>
    /// Load pot state from PlayerPrefs
    /// </summary>
    void LoadPotState()
    {
        // Check for global reset flag (set when starting a new game)
        int resetFlag = PlayerPrefs.GetInt("ResetAllPots", 0);
        if (showDebugLogs)
            Debug.Log($"[PotManager] LoadPotState for {gameObject.name}: ResetAllPots flag = {resetFlag}");
        
        if (resetFlag == 1)
        {
            ResetPot();
            stateNeedsSaving = true;
            SavePotState(); // Save the reset state immediately
            if (showDebugLogs)
                Debug.Log($"[PotManager] *** RESET POT due to new game flag: {gameObject.name} ***");
            return; // Skip normal loading
        }
        
        if (string.IsNullOrEmpty(potID))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[PotManager] Cannot load - pot ID not initialized for '{gameObject.name}'");
            return;
        }
        
        // Load individual values
        isPlanted = PlayerPrefs.GetInt(GetIsPlantedKey(), 0) == 1;
        plantedSeedID = PlayerPrefs.GetString(GetSeedIDKey(), "");
        if (string.IsNullOrEmpty(plantedSeedID))
            plantedSeedID = null;
        dayPlanted = PlayerPrefs.GetInt(GetDayPlantedKey(), -1);
        isFullyGrown = PlayerPrefs.GetInt(GetIsFullyGrownKey(), 0) == 1;
        
        // Restore visual state
        if (isPlanted)
        {
            if (isFullyGrown)
            {
                // Plant is fully grown - restore grown sprite, always using overlay if available
                Sprite grownSprite = GetGrownPlantSprite(plantedSeedID);
                if (grownSprite != null)
                {
                    // Always use overlay for fully grown plants if it exists
                    Image targetImage = (plantOverlayImage != null) ? plantOverlayImage : potImage;
                    
                    if (targetImage != null)
                    {
                        targetImage.sprite = grownSprite;
                        targetImage.enabled = true;
                        targetImage.preserveAspect = true;
                        
                        // If using overlay, ensure it's properly set up and enabled
                        if (targetImage == plantOverlayImage)
                        {
                            // Enable the overlay GameObject
                            plantOverlayImage.gameObject.SetActive(true);
                            
                            // Ensure overlay is set up for overflow (even if allowPlantOverflow was false)
                            LayoutElement layoutElement = plantOverlayImage.GetComponent<LayoutElement>();
                            if (layoutElement == null)
                            {
                                layoutElement = plantOverlayImage.gameObject.AddComponent<LayoutElement>();
                            }
                            layoutElement.ignoreLayout = true;
                            
                            // Apply offset
                            RectTransform rectTransform = plantOverlayImage.GetComponent<RectTransform>();
                            if (rectTransform != null)
                            {
                                rectTransform.anchoredPosition = plantOverlayOffset;
                            }
                        }
                    }
                }
            }
            else
            {
                // Plant is still growing - show sprout sprite
                if (potImage != null && sproutPotSprite != null)
                {
                    potImage.sprite = sproutPotSprite;
                }
            }
        }
        else
        {
            // Pot is empty - show empty sprite
            if (potImage != null && emptyPotSprite != null)
            {
                potImage.sprite = emptyPotSprite;
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[PotManager] Loaded state for pot '{gameObject.name}': Planted={isPlanted}, Seed={plantedSeedID}, Day={dayPlanted}, Grown={isFullyGrown}");
    }
    
    /// <summary>
    /// Check if plant should be grown based on current day vs day planted
    /// </summary>
    void CheckPlantGrowth()
    {
        if (!isPlanted || isFullyGrown || dayPlanted <= 0)
            return;
        
        if (TimeSystem.Instance != null)
        {
            int currentDay = TimeSystem.Instance.GetCurrentDay();
            if (currentDay > dayPlanted)
            {
                // Plant should have grown
                if (showDebugLogs)
                    Debug.Log($"[PotManager] Plant in pot '{gameObject.name}' should be grown (planted day {dayPlanted}, current day {currentDay})");
                GrowPlant();
            }
        }
    }
    
    string GetIsPlantedKey() => $"Pot_{potID}_IsPlanted";
    string GetSeedIDKey() => $"Pot_{potID}_SeedID";
    string GetDayPlantedKey() => $"Pot_{potID}_DayPlanted";
    string GetIsFullyGrownKey() => $"Pot_{potID}_IsFullyGrown";
    
    #endregion
}

