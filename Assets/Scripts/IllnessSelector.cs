using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// Implements Weighted Random Selection (Roulette Wheel Selection) for choosing daily illnesses.
/// Each illness has a weight - higher weight = higher chance of selection.
/// Selects 3 unique illnesses per day for players to treat.
/// Displays selected illnesses in bulleted TextMeshPro format.
/// </summary>
public class IllnessSelector : MonoBehaviour
{
    [System.Serializable]
    public class WeightedIllness
    {
        [Tooltip("Name of the illness/ailment")]
        public string illnessName;
        
        [Tooltip("Weight for random selection (higher = more likely). Use 0 to disable.")]
        [Min(0)]
        public float weight = 1f;
        
        [Tooltip("Corresponding herb use enum for matching with medicine properties")]
        public PlantDataSO.HerbUse herbUse;
        
        [TextArea(2, 4)]
        [Tooltip("Optional description of the illness")]
        public string description;
    }
    
    [Header("Illness Configuration")]
    [Tooltip("List of all possible illnesses with their weights")]
    public List<WeightedIllness> availableIllnesses = new List<WeightedIllness>();
    
    [Header("Selection Settings")]
    [Tooltip("Number of illnesses to select per day")]
    [Range(1, 10)]
    public int illnessesToSelect = 3;
    
    [Tooltip("Allow duplicate illnesses in selection?")]
    public bool allowDuplicates = false;
    
    [Header("UI Display")]
    [Tooltip("TextMeshPro component to display selected illnesses")]
    public TextMeshProUGUI illnessDisplayText;
    
    [Tooltip("Bullet character to use (e.g., •, -, ★)")]
    public string bulletCharacter = "•";
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private List<WeightedIllness> currentDayIllnesses = new List<WeightedIllness>();
    private float totalWeight = 0f;
    
    void Start()
    {
        CalculateTotalWeight();
        
        if (showDebugLogs)
        {
            Debug.Log($"[IllnessSelector] Initialized with {availableIllnesses.Count} illnesses. Total weight: {totalWeight}");
        }
    }
    
    /// <summary>
    /// Calculate the total weight of all available illnesses
    /// </summary>
    void CalculateTotalWeight()
    {
        totalWeight = 0f;
        
        foreach (var illness in availableIllnesses)
        {
            if (illness.weight > 0)
            {
                totalWeight += illness.weight;
            }
        }
    }
    
    /// <summary>
    /// Select illnesses for the current day using weighted random selection
    /// </summary>
    public List<WeightedIllness> SelectDailyIllnesses()
    {
        currentDayIllnesses.Clear();
        
        if (availableIllnesses.Count == 0)
        {
            Debug.LogError("[IllnessSelector] No illnesses available to select!");
            return currentDayIllnesses;
        }
        
        CalculateTotalWeight();
        
        if (totalWeight <= 0)
        {
            Debug.LogError("[IllnessSelector] Total weight is 0! Check illness weights.");
            return currentDayIllnesses;
        }
        
        // Create a working list (for non-duplicate selection)
        List<WeightedIllness> workingList = new List<WeightedIllness>(availableIllnesses);
        
        int selectionsToMake = Mathf.Min(illnessesToSelect, allowDuplicates ? illnessesToSelect : workingList.Count);
        
        for (int i = 0; i < selectionsToMake; i++)
        {
            WeightedIllness selected = SelectWeightedRandom(workingList);
            
            if (selected != null)
            {
                currentDayIllnesses.Add(selected);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[IllnessSelector] Selected: {selected.illnessName} (weight: {selected.weight})");
                }
                
                // Remove from working list if duplicates not allowed
                if (!allowDuplicates)
                {
                    workingList.Remove(selected);
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[IllnessSelector] Selected {currentDayIllnesses.Count} illnesses for today");
        }
        
        // Update the display text
        UpdateIllnessDisplay();
        
        return currentDayIllnesses;
    }
    
    /// <summary>
    /// Weighted random selection (Roulette Wheel Selection algorithm)
    /// </summary>
    WeightedIllness SelectWeightedRandom(List<WeightedIllness> illnessList)
    {
        if (illnessList.Count == 0) return null;
        
        // Calculate total weight for current list
        float currentTotalWeight = 0f;
        foreach (var illness in illnessList)
        {
            if (illness.weight > 0)
            {
                currentTotalWeight += illness.weight;
            }
        }
        
        if (currentTotalWeight <= 0) return null;
        
        // Generate random value between 0 and total weight
        float randomValue = Random.Range(0f, currentTotalWeight);
        
        // Iterate through illnesses and find which "slice" the random value falls into
        float cumulativeWeight = 0f;
        
        foreach (var illness in illnessList)
        {
            if (illness.weight <= 0) continue;
            
            cumulativeWeight += illness.weight;
            
            if (randomValue <= cumulativeWeight)
            {
                return illness;
            }
        }
        
        // Fallback (should never reach here)
        return illnessList[illnessList.Count - 1];
    }
    
    /// <summary>
    /// Update the TextMeshPro display with selected illnesses in bulleted format
    /// </summary>
    void UpdateIllnessDisplay()
    {
        if (illnessDisplayText == null)
        {
            Debug.LogError("[IllnessSelector] Illness display text (TextMeshProUGUI) not assigned in Inspector!");
            return;
        }
        
        if (currentDayIllnesses.Count == 0)
        {
            illnessDisplayText.text = "No illnesses selected";
            Debug.LogWarning("[IllnessSelector] No illnesses in currentDayIllnesses list!");
            return;
        }
        
        // Build bulleted list
        string displayText = "";
        
        for (int i = 0; i < currentDayIllnesses.Count; i++)
        {
            displayText += $"{bulletCharacter} {currentDayIllnesses[i].illnessName}";
            
            // Add newline if not the last item
            if (i < currentDayIllnesses.Count - 1)
            {
                displayText += "\n";
            }
        }
        
        illnessDisplayText.text = displayText;
        
        Debug.Log($"[IllnessSelector] Updated display with {currentDayIllnesses.Count} illnesses:\n{displayText}");
    }
    
    /// <summary>
    /// Manually update the display (useful if text reference changes)
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateIllnessDisplay();
    }
    
    /// <summary>
    /// Clear the illness display text
    /// </summary>
    public void ClearDisplay()
    {
        if (illnessDisplayText != null)
        {
            illnessDisplayText.text = "";
        }
    }
    
    /// <summary>
    /// Get the currently selected illnesses for the day
    /// </summary>
    public List<WeightedIllness> GetCurrentDayIllnesses()
    {
        return currentDayIllnesses;
    }
    
    /// <summary>
    /// Get illness names as a list of strings
    /// </summary>
    public List<string> GetCurrentIllnessNames()
    {
        return currentDayIllnesses.Select(i => i.illnessName).ToList();
    }
    
    /// <summary>
    /// Check if a specific herb use matches any current illnesses
    /// </summary>
    public bool IsHerbUseNeeded(PlantDataSO.HerbUse herbUse)
    {
        foreach (var illness in currentDayIllnesses)
        {
            if (illness.herbUse == herbUse)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get all herb uses needed for current illnesses
    /// </summary>
    public List<PlantDataSO.HerbUse> GetRequiredHerbUses()
    {
        return currentDayIllnesses.Select(i => i.herbUse).ToList();
    }
    
    /// <summary>
    /// Add a new illness to the available list at runtime
    /// </summary>
    public void AddIllness(string name, float weight, PlantDataSO.HerbUse herbUse, string description = "")
    {
        WeightedIllness newIllness = new WeightedIllness
        {
            illnessName = name,
            weight = weight,
            herbUse = herbUse,
            description = description
        };
        
        availableIllnesses.Add(newIllness);
        CalculateTotalWeight();
        
        if (showDebugLogs)
        {
            Debug.Log($"[IllnessSelector] Added illness: {name} with weight {weight}");
        }
    }
    
    /// <summary>
    /// Update the weight of an existing illness
    /// </summary>
    public void UpdateIllnessWeight(string illnessName, float newWeight)
    {
        foreach (var illness in availableIllnesses)
        {
            if (illness.illnessName == illnessName)
            {
                illness.weight = newWeight;
                CalculateTotalWeight();
                
                if (showDebugLogs)
                {
                    Debug.Log($"[IllnessSelector] Updated {illnessName} weight to {newWeight}");
                }
                return;
            }
        }
        
        Debug.LogWarning($"[IllnessSelector] Illness '{illnessName}' not found");
    }
    
    /// <summary>
    /// Get probability percentage for an illness based on its weight
    /// </summary>
    public float GetIllnessProbability(WeightedIllness illness)
    {
        if (totalWeight <= 0) return 0f;
        return (illness.weight / totalWeight) * 100f;
    }
    
    /// <summary>
    /// Debug: Display all illnesses and their probabilities
    /// </summary>
    [ContextMenu("Show All Illness Probabilities")]
    public void ShowAllProbabilities()
    {
        CalculateTotalWeight();
        
        Debug.Log($"=== Illness Probabilities (Total Weight: {totalWeight}) ===");
        
        foreach (var illness in availableIllnesses)
        {
            float probability = GetIllnessProbability(illness);
            Debug.Log($"{illness.illnessName}: {probability:F2}% (weight: {illness.weight})");
        }
    }
    
    /// <summary>
    /// Debug: Test the selection algorithm
    /// </summary>
    [ContextMenu("Test Selection")]
    public void TestSelection()
    {
        List<WeightedIllness> selected = SelectDailyIllnesses();
        
        Debug.Log($"=== Test Selection Result ({selected.Count} illnesses) ===");
        foreach (var illness in selected)
        {
            Debug.Log($"- {illness.illnessName} (HerbUse: {illness.herbUse})");
        }
    }
    
    /// <summary>
    /// Button-friendly method to select and display illnesses
    /// Call this from a Unity Button or at the start of the day
    /// </summary>
    public void SelectAndDisplayIllnesses()
    {
        Debug.Log("[IllnessSelector] SelectAndDisplayIllnesses called");
        List<WeightedIllness> selected = SelectDailyIllnesses();
        Debug.Log($"[IllnessSelector] Selection complete. {selected.Count} illnesses selected.");
    }
}
