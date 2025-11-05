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
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
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
        
        if (showDebugLogs)
        {
            Debug.Log($"[Crafting] Crafted medicine with {combinedProperties.Count} properties:");
            foreach (var prop in combinedProperties)
            {
                Debug.Log($"  - {prop}");
            }
            Debug.Log($"[Crafting] Herbs permanently consumed: {removedHerbs.Count}");
        }
        
        PlaySound(sfxLibrary?.successSound);
        
        // TODO: Create MedicineData object and store/submit it
        
        // Clear tracking list - herbs are now permanently consumed
        removedHerbs.Clear();
        
        // Reset slots (herbs won't be returned since removedHerbs is now empty)
        ClearSlots();
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
