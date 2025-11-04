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
        }
        
        PlaySound(sfxLibrary?.successSound);
        
        // TODO: Create MedicineData object and store/submit it
        // For now, just reset
        ResetSlots();
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
