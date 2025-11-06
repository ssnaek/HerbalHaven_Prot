using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages medicine crafting from 3 herbs.
/// Displays herb icons above pot in arcing positions.
/// Combines herb properties into final medicine.
/// </summary>
public class MedicineCraftingManager : MonoBehaviour
{
    [Header("Herb Slot UI")]
    [Tooltip("3 Image components for herb icons above pot")]
    public Image[] herbSlots = new Image[3];
    
    [Tooltip("Empty sprite to show when slot is empty")]
    public Sprite emptySlotSprite;
    
    [Header("Craft Button")]
    public Button craftButton;
    public Button resetButton;
    
    [Header("Illness Selector")]
    [Tooltip("Reference to the IllnessSelector to check required herb uses")]
    public IllnessSelector illnessSelector;
    
    [Header("Scoring")]
    [Tooltip("Points awarded for each correct herb use match")]
    public int pointsPerMatch = 10;
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    // Score tracking
    private int currentScore = 0;
    
    // Currently selected herbs
    private PlantDataSO[] selectedHerbs = new PlantDataSO[3];
    private int currentSlotIndex = 0;
    
    // Track herbs that were removed from inventory (for restoration on reset)
    private List<PlantDataSO> removedHerbs = new List<PlantDataSO>();
    
    void Start()
    {
        // Wire up buttons
        if (craftButton != null)
            craftButton.onClick.AddListener(CraftMedicine);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSlots);
        
        // Initialize slots as empty
        ResetSlots();
        
        UpdateCraftButton();
    }
    
    /// <summary>
    /// Add herb to next available slot
    /// </summary>
    public void AddHerb(PlantDataSO herbData)
    {
        if (herbData == null) return;
        
        // Check if all slots full
        if (currentSlotIndex >= 3)
        {
            if (showDebugLogs) Debug.Log("[Crafting] All slots full! Reset first.");
            PlaySound(sfxLibrary?.errorSound);
            return;
        }
        
        // Check if herb is available in inventory
        if (InventorySystem.Instance != null)
        {
            if (!InventorySystem.Instance.HasItem(herbData.plantID))
            {
                if (showDebugLogs) Debug.LogWarning($"[Crafting] {herbData.plantName} not in inventory!");
                PlaySound(sfxLibrary?.errorSound);
                return;
            }
            
            int availableQuantity = InventorySystem.Instance.GetItemQuantity(herbData.plantID);
            if (availableQuantity <= 0)
            {
                if (showDebugLogs) Debug.LogWarning($"[Crafting] No {herbData.plantName} available!");
                PlaySound(sfxLibrary?.errorSound);
                return;
            }
            
            // Remove one herb from inventory
            bool removed = InventorySystem.Instance.RemoveItem(herbData.plantID, 1);
            if (!removed)
            {
                if (showDebugLogs) Debug.LogWarning($"[Crafting] Failed to remove {herbData.plantName} from inventory!");
                PlaySound(sfxLibrary?.errorSound);
                return;
            }
            
            // Track this herb for potential restoration on reset
            removedHerbs.Add(herbData);
        }
        
        // Add herb to slot
        selectedHerbs[currentSlotIndex] = herbData;
        
        // Update UI
        if (herbSlots[currentSlotIndex] != null)
        {
            herbSlots[currentSlotIndex].sprite = herbData.icon;
            herbSlots[currentSlotIndex].color = Color.white;
        }
        
        currentSlotIndex++;
        
        PlaySound(sfxLibrary?.uiSelect);
        UpdateCraftButton();
        
        if (showDebugLogs) Debug.Log($"[Crafting] Added {herbData.plantName} to slot {currentSlotIndex - 1}");
    }
    
    /// <summary>
    /// Reset all herb slots
    /// </summary>
    public void ResetSlots()
    {
        if (showDebugLogs) Debug.Log("[Crafting] Resetting slots");
        
        // Return all removed herbs back to inventory
        if (InventorySystem.Instance != null)
        {
            foreach (PlantDataSO herb in removedHerbs)
            {
                if (herb != null)
                {
                    InventorySystem.Instance.AddItem(herb.plantID, herb.plantName, herb.icon, 1, herb);
                    if (showDebugLogs) Debug.Log($"[Crafting] Returned {herb.plantName} to inventory");
                }
            }
        }
        
        // Clear tracking list
        removedHerbs.Clear();
        
        for (int i = 0; i < 3; i++)
        {
            selectedHerbs[i] = null;
            
            if (herbSlots[i] != null)
            {
                herbSlots[i].sprite = emptySlotSprite;
                herbSlots[i].color = new Color(1f, 1f, 1f, 0.3f); // Transparent
            }
        }
        
        currentSlotIndex = 0;
        UpdateCraftButton();
        PlaySound(sfxLibrary?.uiCancel);
    }
    
    /// <summary>
    /// Craft medicine from selected herbs
    /// </summary>
    void CraftMedicine()
    {
        // Must have at least 1 herb
        if (currentSlotIndex == 0)
        {
            if (showDebugLogs) Debug.Log("[Crafting] No herbs selected!");
            PlaySound(sfxLibrary?.errorSound);
            return;
        }
        
        // Combine herb properties
        List<PlantDataSO.HerbUse> combinedProperties = new List<PlantDataSO.HerbUse>();
        
        for (int i = 0; i < currentSlotIndex; i++)
        {
            if (selectedHerbs[i] != null && selectedHerbs[i].uses != null)
            {
                foreach (var use in selectedHerbs[i].uses)
                {
                    // Add property if not already in list (no duplicates)
                    if (!combinedProperties.Contains(use))
                    {
                        combinedProperties.Add(use);
                    }
                }
            }
        }
        
        // Calculate score based on matching herb uses with required illnesses
        int craftScore = CalculateScore(combinedProperties);
        currentScore += craftScore;
        
        // Log crafting results
        Debug.Log("=== MEDICINE CRAFTED ===");
        Debug.Log($"[Crafting] Crafted medicine with {combinedProperties.Count} properties:");
        foreach (var prop in combinedProperties)
        {
            Debug.Log($"  - {prop}");
        }
        Debug.Log($"[Crafting] Score for this craft: {craftScore} points");
        Debug.Log($"[Crafting] Total Score: {currentScore} points");
        Debug.Log("========================");
        
        PlaySound(sfxLibrary?.successSound);
        
        // TODO: Create MedicineData object and store/submit it
        
        // Clear tracking list - herbs are now permanently consumed
        removedHerbs.Clear();
        
        // Reset slots (herbs won't be returned since removedHerbs is now empty)
        ClearSlots();
    }
    
    /// <summary>
    /// Calculate score based on how many herb uses match the required illnesses
    /// </summary>
    int CalculateScore(List<PlantDataSO.HerbUse> craftedProperties)
    {
        if (illnessSelector == null)
        {
            Debug.LogWarning("[Crafting] IllnessSelector not assigned! Cannot calculate score.");
            return 0;
        }
        
        // Get required herb uses from the illness selector
        List<PlantDataSO.HerbUse> requiredUses = illnessSelector.GetRequiredHerbUses();
        
        if (requiredUses.Count == 0)
        {
            Debug.LogWarning("[Crafting] No illnesses selected! Cannot calculate score.");
            return 0;
        }
        
        int matches = 0;
        List<PlantDataSO.HerbUse> matchedUses = new List<PlantDataSO.HerbUse>();
        
        // Count how many crafted properties match required illnesses
        foreach (var craftedUse in craftedProperties)
        {
            if (requiredUses.Contains(craftedUse))
            {
                matches++;
                matchedUses.Add(craftedUse);
            }
        }
        
        int score = matches * pointsPerMatch;
        
        // Detailed scoring log
        Debug.Log($"[Scoring] Required illnesses: {requiredUses.Count}");
        Debug.Log($"[Scoring] Crafted properties: {craftedProperties.Count}");
        Debug.Log($"[Scoring] Correct matches: {matches}");
        
        if (matchedUses.Count > 0)
        {
            Debug.Log("[Scoring] Matched herb uses:");
            foreach (var use in matchedUses)
            {
                Debug.Log($"  ✓ {use}");
            }
        }
        
        return score;
    }
    
    /// <summary>
    /// Clear slots without returning herbs (used after crafting)
    /// </summary>
    void ClearSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            selectedHerbs[i] = null;
            
            if (herbSlots[i] != null)
            {
                herbSlots[i].sprite = emptySlotSprite;
                herbSlots[i].color = new Color(1f, 1f, 1f, 0.3f); // Transparent
            }
        }
        
        currentSlotIndex = 0;
        UpdateCraftButton();
    }
    
    void UpdateCraftButton()
    {
        if (craftButton != null)
        {
            // Enable craft button if at least 1 herb selected
            craftButton.interactable = currentSlotIndex > 0;
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getters
    public int GetSlotCount() => currentSlotIndex;
    public PlantDataSO[] GetSelectedHerbs() => selectedHerbs;
    public int GetCurrentScore() => currentScore;
    
    /// <summary>
    /// Reset the total score (call at start of new day)
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        Debug.Log("[Crafting] Score reset to 0");
    }
    
    public List<PlantDataSO.HerbUse> GetCombinedProperties()
    {
        List<PlantDataSO.HerbUse> properties = new List<PlantDataSO.HerbUse>();
        
        for (int i = 0; i < currentSlotIndex; i++)
        {
            if (selectedHerbs[i] != null && selectedHerbs[i].uses != null)
            {
                foreach (var use in selectedHerbs[i].uses)
                {
                    if (!properties.Contains(use))
                    {
                        properties.Add(use);
                    }
                }
            }
        }
        
        return properties;
    }
}
