using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
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
    
    [Header("Medicine Product")]
    [Tooltip("Icon/sprite for finished medicine products (assign a placeholder sprite here)")]
    public Sprite medicineIcon;
    
    [Tooltip("Name prefix for crafted medicines (e.g., 'Infusion' will create 'Infusion of <Herbs>')")]
    public string medicineNamePrefix = "Infusion";
	
    [Header("Proceed Behavior")] 
	[Tooltip("Automatically invoke proceed after this many seconds (0 = don't auto)")]
	public float autoProceedAfterSeconds = 0f;
	
	[Tooltip("Event invoked after evaluation (wire your 'Next' action here)")]
	public UnityEvent onEvaluationComplete;
    
    [Header("Summary Manager")]
    [Tooltip("Reference to CraftingSummaryManager to enable submit button after crafting")]
    public CraftingSummaryManager summaryManager;
    
    [Header("Debug")]
    public bool showDebugLogs = false;    // Score tracking
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
		
		// Notify summary manager that medicine was crafted
		if (summaryManager != null)
		{
			summaryManager.OnMedicineCrafted(craftScore);
		}
		else if (showDebugLogs)
		{
			Debug.LogWarning("[Crafting] CraftingSummaryManager not assigned - submit button won't be enabled");
		}
		
		// Clear tracking list - herbs are now permanently consumed
        removedHerbs.Clear();
        
        // Reset slots (herbs won't be returned since removedHerbs is now empty)
        ClearSlots();
		
		// Invoke proceed behavior
		if (autoProceedAfterSeconds > 0f)
		{
			StartCoroutine(AutoProceedCoroutine(autoProceedAfterSeconds));
		}
		else
		{
			onEvaluationComplete?.Invoke();
		}
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
	
	System.Collections.IEnumerator AutoProceedCoroutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		onEvaluationComplete?.Invoke();
	}
    
    /// <summary>
    /// Create a medicine item from combined properties and add to inventory
    /// </summary>
    void CreateAndAddMedicine(List<PlantDataSO.HerbUse> properties)
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[Crafting] InventorySystem not found! Cannot add medicine.");
            return;
        }
        
        // Generate medicine name based on properties
        string medicineName = GenerateMedicineName(properties);
        string medicineID = GenerateMedicineID(properties);
        string description = GenerateMedicineDescription(properties);
        
        // Use assigned icon or fallback to first herb's icon
        Sprite iconToUse = medicineIcon;
        if (iconToUse == null && selectedHerbs[0] != null)
        {
            iconToUse = selectedHerbs[0].icon;
            if (showDebugLogs) Debug.LogWarning("[Crafting] No medicine icon assigned, using first herb icon as fallback");
        }
        
        // Add medicine to inventory
        InventorySystem.Instance.AddItem(medicineID, medicineName, iconToUse, 1);
        
        // Set description if available
        if (!string.IsNullOrEmpty(description))
        {
            InventorySystem.Instance.SetItemDescription(medicineID, description);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[Crafting] Created medicine: {medicineName} (ID: {medicineID})");
            Debug.Log($"[Crafting] Description: {description}");
        }
    }
    
    /// <summary>
    /// Generate a medicine name based on the herbs used, not the properties.
    /// Example: "Infusion of Ginger + Basil + Mint"
    /// </summary>
    string GenerateMedicineName(List<PlantDataSO.HerbUse> properties)
    {
        // Collect herb display names from selectedHerbs
        List<string> herbNames = new List<string>();
        for (int i = 0; i < selectedHerbs.Length; i++)
        {
            if (selectedHerbs[i] != null && !string.IsNullOrEmpty(selectedHerbs[i].plantName))
            {
                string name = selectedHerbs[i].plantName.Trim();
                if (!herbNames.Contains(name))
                {
                    herbNames.Add(name);
                }
            }
        }

        if (herbNames.Count == 0)
        {
            return $"{medicineNamePrefix}";
        }

        string herbsPart = string.Join(" + ", herbNames);
        return $"{medicineNamePrefix} of {herbsPart}";
    }
    
    /// <summary>
    /// Generate a unique medicine ID based on properties
    /// </summary>
    string GenerateMedicineID(List<PlantDataSO.HerbUse> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return "medicine_generic";
        }
        
        // Create ID from sorted properties for consistency
        List<string> propNames = new List<string>();
        foreach (var prop in properties)
        {
            propNames.Add(prop.ToString().ToLower());
        }
        propNames.Sort();
        
        return $"medicine_{string.Join("_", propNames)}";
    }
    
    /// <summary>
    /// Generate a description for the medicine based on properties
    /// </summary>
    string GenerateMedicineDescription(List<PlantDataSO.HerbUse> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return "A basic medicinal concoction.";
        }
        
        List<string> uses = new List<string>();
        foreach (var prop in properties)
        {
            string useText = prop.ToString().Replace("_", " ").ToLower();
            uses.Add(useText);
        }
        
        return $"A medicine that treats: {string.Join(", ", uses)}.";
    }
    
    // ===== Utility helpers so other systems can check medicine properties without new scripts =====
    public static bool IsMedicineItem(string itemID)
    {
        return !string.IsNullOrEmpty(itemID) && itemID.StartsWith("medicine_");
    }
    
    public static bool ItemHasHerbUse(string itemID, PlantDataSO.HerbUse use)
    {
        if (string.IsNullOrEmpty(itemID)) return false;
        if (!IsMedicineItem(itemID)) return false;
        
        List<PlantDataSO.HerbUse> props = GetMedicinePropertiesFromID(itemID);
        return props.Contains(use);
    }
    
    public static List<PlantDataSO.HerbUse> GetMedicinePropertiesFromID(string itemID)
    {
        List<PlantDataSO.HerbUse> result = new List<PlantDataSO.HerbUse>();
        if (string.IsNullOrEmpty(itemID)) return result;
        if (!IsMedicineItem(itemID)) return result;
        
        // itemID format: "medicine_<prop>_<prop>..." where props are lower-case enum names
        string[] parts = itemID.Split('_');
        // parts[0] == "medicine"
        for (int i = 1; i < parts.Length; i++)
        {
            string token = parts[i];
            // Handle multi-word enum values that used underscores originally (e.g., sore_throat)
            // Since we joined with '_', tokens are already split; we can't perfectly reconstruct multi-word enums
            // However our enum names themselves contain underscores, and we sorted/joined exact enum names in lowercase
            // during ID generation, so each token actually corresponds to a full enum name tokenization across multiple parts.
            // To recover, re-parse by checking cumulative joins against enum names.
        }
        
        // Robust re-parse: split prefix and then split remaining by '_' and rebuild tokens to match enum names
        string remaining = itemID.Substring("medicine_".Length);
        string[] tokens = remaining.Split('_');
        
        // Build a set of all enum names in lowercase for matching
        Dictionary<string, PlantDataSO.HerbUse> nameToEnum = new Dictionary<string, PlantDataSO.HerbUse>();
        foreach (PlantDataSO.HerbUse e in System.Enum.GetValues(typeof(PlantDataSO.HerbUse)))
        {
            nameToEnum[e.ToString().ToLower()] = e;
        }
        
        // Since we generated IDs by joining full enum names already lowercased (including underscores),
        // we can directly split by '_' and then recombine progressively to match known enum names.
        // Example: "sore_throat" becomes tokens ["sore","throat"] → recombine to "sore_throat".
        List<string> buffer = new List<string>();
        for (int i = 0; i < tokens.Length; i++)
        {
            buffer.Add(tokens[i]);
            string joined = string.Join("_", buffer);
            if (nameToEnum.ContainsKey(joined))
            {
                result.Add(nameToEnum[joined]);
                buffer.Clear();
            }
        }
        
        return result;
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
