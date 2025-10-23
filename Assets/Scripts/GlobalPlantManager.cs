using UnityEngine;

/// <summary>
/// Manages global plant regeneration based on previous day's harvest behavior.
/// Uses exponential penalty formula to encourage sustainable harvesting.
/// </summary>
public class GlobalPlantManager : MonoBehaviour
{
    public static GlobalPlantManager Instance { get; private set; }

    [Header("Regeneration Settings")]
    [Tooltip("Harvest threshold - harvests below this get bonus, above get penalty")]
    public int harvestThreshold = 15;
    
    [Tooltip("Base multiplier when under threshold")]
    public float bonusMultiplier = 1.2f;
    
    [Tooltip("Penalty rate for exponential decay")]
    public float penaltyRate = 0.15f;
    
    [Tooltip("Minimum spawn multiplier (always spawn at least this %)")]
    public float minimumMultiplier = 0.3f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float currentRegenerationMultiplier = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Subscribe to new day event
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.onNewDayCallback += OnNewDay;
        }

        // Calculate initial multiplier
        CalculateRegenerationMultiplier();
    }

    void OnDestroy()
    {
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.onNewDayCallback -= OnNewDay;
        }
    }

    void OnNewDay(int day)
    {
        if (showDebugLogs) Debug.Log($"[PlantManager] New day started: Day {day}");
        
        CalculateRegenerationMultiplier();
    }

    void CalculateRegenerationMultiplier()
    {
        if (TimeSystem.Instance == null)
        {
            Debug.LogError("[PlantManager] TimeSystem.Instance is null!");
            currentRegenerationMultiplier = 1.0f;
            return;
        }

        int previousDayHarvests = TimeSystem.Instance.GetPreviousDayHarvests();

        if (showDebugLogs) Debug.Log($"[PlantManager] Previous day harvests: {previousDayHarvests}, Threshold: {harvestThreshold}");

        if (previousDayHarvests <= harvestThreshold)
        {
            // Reward: Below threshold = bonus plants
            currentRegenerationMultiplier = bonusMultiplier;
            
            if (showDebugLogs) Debug.Log($"[PlantManager] ✓ Good harvesting! Multiplier: {currentRegenerationMultiplier:F2}x (BONUS)");
        }
        else
        {
            // Penalty: Above threshold = exponential decay
            int excessHarvests = previousDayHarvests - harvestThreshold;
            
            // Formula: multiplier = exp(-penaltyRate * excessHarvests)
            // Clamped to minimum
            currentRegenerationMultiplier = Mathf.Exp(-penaltyRate * excessHarvests);
            currentRegenerationMultiplier = Mathf.Max(currentRegenerationMultiplier, minimumMultiplier);
            
            if (showDebugLogs) 
            {
                Debug.Log($"[PlantManager] ⚠️ Overharvesting detected! Excess: {excessHarvests}");
                Debug.Log($"[PlantManager] Multiplier: {currentRegenerationMultiplier:F2}x (PENALTY)");
            }
        }

        if (showDebugLogs) Debug.Log($"[PlantManager] Final regeneration multiplier: {currentRegenerationMultiplier:F2}x");
    }

    /// <summary>
    /// Get the current regeneration multiplier for plant spawning.
    /// Nodes should multiply their base plant count by this value.
    /// </summary>
    public float GetRegenerationMultiplier()
    {
        return currentRegenerationMultiplier;
    }

    /// <summary>
    /// Calculate how many plants a node should spawn based on its base amount.
    /// </summary>
    public int CalculateSpawnAmount(int baseAmount)
    {
        float calculatedAmount = baseAmount * currentRegenerationMultiplier;
        int spawnAmount = Mathf.RoundToInt(calculatedAmount);
        
        // Always spawn at least 1 plant
        spawnAmount = Mathf.Max(1, spawnAmount);
        
        if (showDebugLogs) Debug.Log($"[PlantManager] Base: {baseAmount}, Multiplier: {currentRegenerationMultiplier:F2}x, Result: {spawnAmount}");
        
        return spawnAmount;
    }
}