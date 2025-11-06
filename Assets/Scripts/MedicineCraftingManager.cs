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
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Medicine Product")]
    [Tooltip("Icon/sprite for finished medicine products (assign a placeholder sprite here)")]
    public Sprite medicineIcon;
    
    [Tooltip("Name prefix for crafted medicines (e.g., 'Infusion' will create 'Infusion of <Herbs>')")]
    public string medicineNamePrefix = "Infusion";
	
	[Header("Evaluation (Note) Settings")]
	[Tooltip("Symptoms required by the current note/patient")] 
	public List<PlantDataSO.HerbUse> requiredSymptoms = new List<PlantDataSO.HerbUse>();
	
	[Tooltip("Parent container to populate symptom rows (optional)")]
	public Transform notesContainer;
	
	[Tooltip("Prefab for a single symptom row (should include a Text or TMP label and an Image to color)")]
	public GameObject symptomItemPrefab;
	
	[Tooltip("Color used when symptom is covered by infusion")]
	public Color matchedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
	
	[Tooltip("Color used when symptom is NOT covered by infusion")]
	public Color unmatchedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
	
	[Tooltip("If true, list will be cleared and repopulated on each evaluation")]
	public bool repopulateNotesListOnEvaluate = true;
	
	[Header("Proceed Behavior")] 
	[Tooltip("Automatically invoke proceed after this many seconds (0 = don't auto)")]
	public float autoProceedAfterSeconds = 0f;
	
	[Tooltip("Event invoked after evaluation (wire your 'Next' action here)")]
	public UnityEvent onEvaluationComplete;
    
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
        
		// Evaluate against the current note requirements
		EvaluateAgainstRequiredSymptoms(combinedProperties);
		
		PlaySound(sfxLibrary?.successSound);
		
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

	/// <summary>
	/// Compare combined properties against the requiredSymptoms list and optionally render UI.
	/// - Each symptom row: green if covered, red if not.
	/// - If no UI is wired, logs the evaluation result.
	/// </summary>
	void EvaluateAgainstRequiredSymptoms(List<PlantDataSO.HerbUse> combinedProperties)
	{
		if (requiredSymptoms == null) requiredSymptoms = new List<PlantDataSO.HerbUse>();
		
		bool allCovered = true;
		foreach (var req in requiredSymptoms)
		{
			bool covered = combinedProperties.Contains(req);
			if (!covered) allCovered = false;
		}
		
		// Optional UI rendering
		if (notesContainer != null && symptomItemPrefab != null)
		{
			if (repopulateNotesListOnEvaluate)
			{
				for (int i = notesContainer.childCount - 1; i >= 0; i--)
				{
					var child = notesContainer.GetChild(i);
					GameObject.Destroy(child.gameObject);
				}
			}
			
			foreach (var req in requiredSymptoms)
			{
				GameObject row = GameObject.Instantiate(symptomItemPrefab, notesContainer);
				// Try TMP first
				TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
				if (tmp != null) tmp.text = req.ToString().Replace("_", " ");
				else
				{
					Text text = row.GetComponentInChildren<Text>();
					if (text != null) text.text = req.ToString().Replace("_", " ");
				}
				
				bool covered = combinedProperties.Contains(req);
				Color useColor = covered ? matchedColor : unmatchedColor;
				Image img = row.GetComponentInChildren<Image>();
				if (img != null)
				{
					img.color = useColor;
				}
			}
		}
		else
		{
			// Fallback: log results
			if (showDebugLogs)
			{
				Debug.Log("[Crafting] Evaluation Results:");
				foreach (var req in requiredSymptoms)
				{
					bool covered = combinedProperties.Contains(req);
					Debug.Log($" - {req}: {(covered ? "OK" : "MISSING")}");
				}
				Debug.Log(allCovered ? "[Crafting] ✓ All symptoms covered" : "[Crafting] ✗ Some symptoms not covered");
			}
		}
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
