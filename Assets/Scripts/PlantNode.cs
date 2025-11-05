using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plant node that spawns multiple plant types based on rarity weights.
/// Respawns daily based on GlobalPlantManager's regeneration multiplier.
/// Saves and restores plant state across scene loads.
/// FIXED: Improved save timing and added validation
/// </summary>
public class PlantNode : MonoBehaviour
{
    [Header("Plant Pool")]
    [Tooltip("Drag PlantDataSO assets here")]
    public List<PlantDataSO> plantPool = new List<PlantDataSO>();
    
    [Header("Spawn Settings")]
    [Tooltip("Base number of plants to spawn (affected by regeneration multiplier)")]
    public int basePlantCount = 3;
    public float spawnRadius = 4f;
    public bool addColliderToPlants = true;
    public float plantColliderRadius = 0.5f;

    [Header("Rarity Weights")]
    [Range(0f, 1f)] public float commonWeight = 0.70f;
    [Range(0f, 1f)] public float uncommonWeight = 0.25f;
    [Range(0f, 1f)] public float rareWeight = 0.05f;

    [Header("Debug")]
    public bool showDebugLogs = true; // Changed to true by default for debugging

    // Runtime state
    private List<GameObject> spawnedPlants = new List<GameObject>();
    private int lastSpawnDay = -1;
    private string nodeID;
    private List<PlantState> savedPlantStates = new List<PlantState>();
    private bool stateNeedsSaving = false; // NEW: Track if we need to save

    // Plant state for saving/loading
    [System.Serializable]
    private class PlantState
    {
        public string plantID;
        public Vector3 position;
        public Quaternion rotation;
    }

    // Wrapper for JSON serialization
    [System.Serializable]
    private class PlantStateList
    {
        public List<PlantState> states = new List<PlantState>();
    }

    #region Unity Lifecycle

    void Awake()
    {
        InitializeNodeID();
        LoadSavedState();
        
        // NEW: Subscribe to scene unload to save earlier
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void Start()
    {
        if (!ValidatePlantPool()) return;

        CheckAndSpawnForDay();
        SubscribeToEvents();
    }

    void OnApplicationQuit()
    {
        // Save when application quits
        if (stateNeedsSaving)
        {
            SavePlantStates();
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] Saved on application quit");
        }
    }

    void OnDestroy()
    {
        // Save before destruction
        if (stateNeedsSaving)
        {
            SavePlantStates();
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] Saved on destroy");
        }
        
        UnsubscribeFromEvents();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneUnloaded(Scene scene)
    {
        // NEW: Save when scene unloads (more reliable than OnDestroy)
        if (scene.name == SceneManager.GetActiveScene().name && stateNeedsSaving)
        {
            SavePlantStates();
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] Saved on scene unload: {scene.name}");
        }
    }

    #endregion

    #region Initialization

    void InitializeNodeID()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        nodeID = $"{currentScene.name}_{transform.position.x:F1}_{transform.position.y:F1}_{transform.position.z:F1}";
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Node ID: {nodeID}");
    }

    void LoadSavedState()
    {
        lastSpawnDay = PlayerPrefs.GetInt(GetDayKey(), -1);
        LoadPlantStates();

        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Loaded: Day {lastSpawnDay}, Plants: {savedPlantStates.Count}");
    }

    bool ValidatePlantPool()
    {
        if (plantPool.Count == 0)
        {
            Debug.LogError($"[PlantNode '{gameObject.name}'] Plant pool is empty!");
            return false;
        }

        foreach (var plantData in plantPool)
        {
            if (plantData == null)
            {
                Debug.LogWarning($"[PlantNode '{gameObject.name}'] Null plant in pool!");
                continue;
            }
            
            if (string.IsNullOrEmpty(plantData.plantID))
                Debug.LogWarning($"[PlantNode] Plant '{plantData.plantName}' has no plantID!");
        }

        return true;
    }

    void SubscribeToEvents()
    {
        if (TimeSystem.Instance != null)
            TimeSystem.Instance.onNewDayCallback += OnNewDay;
    }

    void UnsubscribeFromEvents()
    {
        if (TimeSystem.Instance != null)
            TimeSystem.Instance.onNewDayCallback -= OnNewDay;
    }

    #endregion

    #region Day Management

    void OnNewDay(int newDay)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] New day detected: {newDay}");
        
        ClearSavedStates();
        CheckAndSpawnForDay();
    }

    void CheckAndSpawnForDay()
    {
        if (TimeSystem.Instance == null)
        {
            Debug.LogWarning("[PlantNode] TimeSystem.Instance not found!");
            return;
        }

        int currentDay = TimeSystem.Instance.GetCurrentDay();
        
        if (IsNewDay(currentDay))
        {
            HandleNewDay(currentDay);
        }
        else
        {
            HandleSameDay();
        }
    }

    bool IsNewDay(int currentDay)
    {
        return lastSpawnDay != currentDay;
    }

    void HandleNewDay(int currentDay)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] NEW DAY - Spawning for day {currentDay}");
        
        ClearSavedStates();
        SpawnPlantsForNewDay();
        SaveDayNumber(currentDay);
        stateNeedsSaving = true; // Mark that we have state to save
    }

    void HandleSameDay()
    {
        if (savedPlantStates.Count > 0)
        {
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] SAME DAY - Restoring {savedPlantStates.Count} plants from save");
            
            RestorePlantsFromState();
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] SAME DAY - First load, spawning plants");
            
            SpawnPlantsForNewDay();
        }
        
        stateNeedsSaving = true; // Mark that we have state to save
    }

    void SaveDayNumber(int day)
    {
        lastSpawnDay = day;
        PlayerPrefs.SetInt(GetDayKey(), lastSpawnDay);
        PlayerPrefs.Save();
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Saved day number: {day}");
    }

    void ClearSavedStates()
    {
        savedPlantStates.Clear();
        PlayerPrefs.DeleteKey(GetStatesKey());
        PlayerPrefs.Save();
        stateNeedsSaving = false;
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Cleared saved states");
    }

    #endregion

    #region Plant Spawning

    void SpawnPlantsForNewDay()
    {
        int spawnAmount = CalculateSpawnAmount();
        SpawnPlants(spawnAmount);
    }

    int CalculateSpawnAmount()
    {
        if (GlobalPlantManager.Instance != null)
        {
            int amount = GlobalPlantManager.Instance.CalculateSpawnAmount(basePlantCount);
            
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] Base: {basePlantCount}, Adjusted: {amount}");
            
            return amount;
        }

        Debug.LogWarning("[PlantNode] GlobalPlantManager not found! Using base spawn amount.");
        return basePlantCount;
    }

    void SpawnPlants(int amount)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Spawning {amount} plants");
        
        ClearExistingPlants();

        for (int i = 0; i < amount; i++)
        {
            SpawnSinglePlant(i);
        }

        if (showDebugLogs)
            Debug.Log($"[PlantNode] Spawn complete. Total: {spawnedPlants.Count} plants");
    }

    void SpawnSinglePlant(int index)
    {
        PlantDataSO selectedPlant = SelectPlantByRarity();
        
        if (selectedPlant == null || selectedPlant.plantPrefab == null)
        {
            Debug.LogError($"[PlantNode '{gameObject.name}'] Selected plant is null or has no prefab!");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        Quaternion spawnRot = selectedPlant.plantPrefab.transform.rotation;
        
        GameObject plantObj = Instantiate(selectedPlant.plantPrefab, spawnPos, spawnRot, transform);
        plantObj.name = $"{selectedPlant.plantName}_{index}";

        SetupPlantComponent(plantObj, selectedPlant);
        spawnedPlants.Add(plantObj);
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode] Spawned '{plantObj.name}' ({selectedPlant.rarity}) at {spawnPos}");
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        pos.y = transform.position.y;
        return pos;
    }

    void SetupPlantComponent(GameObject plantObject, PlantDataSO plantData)
    {
        InteractablePlant interactable = plantObject.GetComponent<InteractablePlant>();
        if (interactable == null)
            interactable = plantObject.AddComponent<InteractablePlant>();
        
        interactable.parentNode = this;
        interactable.plantData = plantData;
        interactable.showDebugLogs = showDebugLogs;

        EnsureCollider(plantObject);
    }

    #endregion

    #region Plant Restoration

    void RestorePlantsFromState()
    {
        if (showDebugLogs)
            Debug.Log($"[PlantNode '{gameObject.name}'] Restoring {savedPlantStates.Count} plants");
        
        ClearExistingPlants();

        foreach (var state in savedPlantStates)
        {
            RestoreSinglePlant(state);
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode] Restore complete. Total: {spawnedPlants.Count} plants");
    }

    void RestoreSinglePlant(PlantState state)
    {
        PlantDataSO plantData = FindPlantDataByID(state.plantID);

        if (plantData == null || plantData.plantPrefab == null)
        {
            Debug.LogWarning($"[PlantNode] Could not find plant data for ID: {state.plantID}");
            return;
        }

        GameObject plantObj = Instantiate(plantData.plantPrefab, state.position, state.rotation, transform);
        plantObj.name = plantData.plantName;

        SetupPlantComponent(plantObj, plantData);
        spawnedPlants.Add(plantObj);
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode] Restored '{plantObj.name}' at {state.position}");
    }

    PlantDataSO FindPlantDataByID(string plantID)
    {
        foreach (var poolPlant in plantPool)
        {
            if (poolPlant != null && poolPlant.plantID == plantID)
                return poolPlant;
        }
        return null;
    }

    #endregion

    #region Plant Collection

    public void CollectSpecificPlant(GameObject plantObj, PlantDataSO plantData)
    {
        if (showDebugLogs)
            Debug.Log($"[PlantNode] ═══ CollectSpecificPlant('{plantObj.name}') ═══");

        if (!spawnedPlants.Contains(plantObj))
        {
            Debug.LogWarning($"[PlantNode] Plant '{plantObj.name}' not found in spawned list!");
            return;
        }

        RemovePlant(plantObj);
        RegisterHarvest();
        AddToInventory(plantData);
        
        // NEW: Immediately save after collection
        stateNeedsSaving = true;
        SavePlantStates();
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode] Collection complete. Remaining: {spawnedPlants.Count}");
    }

    void RemovePlant(GameObject plantObj)
    {
        bool removed = spawnedPlants.Remove(plantObj);
        Destroy(plantObj);
        
        if (showDebugLogs)
        {
            Debug.Log($"[PlantNode] Plant removed from list: {removed}");
            Debug.Log($"[PlantNode] Current spawned plants count: {spawnedPlants.Count}");
        }
    }

    void RegisterHarvest()
    {
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.RegisterHarvest();
        }
        else
        {
            Debug.LogWarning("[PlantNode] TimeSystem.Instance is null! Cannot register harvest.");
        }
    }

    void AddToInventory(PlantDataSO plantData)
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[PlantNode] InventorySystem.Instance is null!");
            return;
        }

        InventorySystem.Instance.AddItem(
            plantData.plantID, 
            plantData.plantName, 
            plantData.icon, 
            plantData.yieldPerPick, 
            plantData
        );
        
        if (showDebugLogs)
            Debug.Log($"✓ Added {plantData.yieldPerPick}x {plantData.plantName} to inventory");
    }

    #endregion

    #region Save/Load System

    void SavePlantStates()
    {
        savedPlantStates.Clear();

        foreach (var plantObj in spawnedPlants)
        {
            if (plantObj == null) continue;

            InteractablePlant interactable = plantObj.GetComponent<InteractablePlant>();
            if (interactable != null && interactable.plantData != null)
            {
                PlantState state = new PlantState
                {
                    plantID = interactable.plantData.plantID,
                    position = plantObj.transform.position,
                    rotation = plantObj.transform.rotation
                };
                savedPlantStates.Add(state);
            }
        }

        string json = JsonUtility.ToJson(new PlantStateList { states = savedPlantStates }, false);
        PlayerPrefs.SetString(GetStatesKey(), json);
        PlayerPrefs.Save();

        if (showDebugLogs)
        {
            Debug.Log($"[PlantNode '{gameObject.name}'] ═══ SAVED {savedPlantStates.Count} PLANT STATES ═══");
            Debug.Log($"[PlantNode] Save key: {GetStatesKey()}");
            Debug.Log($"[PlantNode] JSON length: {json.Length}");
        }
    }

    void LoadPlantStates()
    {
        string json = PlayerPrefs.GetString(GetStatesKey(), "");
        
        if (showDebugLogs)
        {
            Debug.Log($"[PlantNode '{gameObject.name}'] ═══ LOADING PLANT STATES ═══");
            Debug.Log($"[PlantNode] Load key: {GetStatesKey()}");
            Debug.Log($"[PlantNode] JSON length: {json.Length}");
        }
        
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                PlantStateList stateList = JsonUtility.FromJson<PlantStateList>(json);
                savedPlantStates = stateList.states ?? new List<PlantState>();
                
                if (showDebugLogs)
                {
                    Debug.Log($"[PlantNode '{gameObject.name}'] Successfully loaded {savedPlantStates.Count} plant states");
                    foreach (var state in savedPlantStates)
                    {
                        Debug.Log($"  - {state.plantID} at {state.position}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlantNode] Failed to parse saved states: {e.Message}");
                savedPlantStates = new List<PlantState>();
            }
        }
        else
        {
            savedPlantStates = new List<PlantState>();
            if (showDebugLogs)
                Debug.Log($"[PlantNode '{gameObject.name}'] No saved states found");
        }
    }

    string GetDayKey() => $"PlantNode_{nodeID}_LastSpawnDay";
    string GetStatesKey() => $"PlantNode_{nodeID}_PlantStates";

    #endregion

    #region Rarity Selection

    PlantDataSO SelectPlantByRarity()
    {
        List<PlantDataSO> commonPlants = new List<PlantDataSO>();
        List<PlantDataSO> uncommonPlants = new List<PlantDataSO>();
        List<PlantDataSO> rarePlants = new List<PlantDataSO>();

        // Categorize plants by rarity
        foreach (var plantData in plantPool)
        {
            if (plantData == null) continue;
            
            switch (plantData.rarity)
            {
                case PlantDataSO.PlantRarity.Common:
                    commonPlants.Add(plantData);
                    break;
                case PlantDataSO.PlantRarity.Uncommon:
                    uncommonPlants.Add(plantData);
                    break;
                case PlantDataSO.PlantRarity.Rare:
                    rarePlants.Add(plantData);
                    break;
            }
        }

        // Roll for rarity
        float roll = Random.value;
        float cumulative = 0f;

        // Check rare first
        cumulative += rareWeight;
        if (roll <= cumulative && rarePlants.Count > 0)
            return rarePlants[Random.Range(0, rarePlants.Count)];

        // Check uncommon
        cumulative += uncommonWeight;
        if (roll <= cumulative && uncommonPlants.Count > 0)
            return uncommonPlants[Random.Range(0, uncommonPlants.Count)];

        // Default to common
        if (commonPlants.Count > 0)
            return commonPlants[Random.Range(0, commonPlants.Count)];

        // Fallback to any plant
        if (plantPool.Count > 0)
            return plantPool[Random.Range(0, plantPool.Count)];

        return null;
    }

    #endregion

    #region Utilities

    void EnsureCollider(GameObject plantObject)
    {
        if (!addColliderToPlants) return;

        Collider existingCollider = plantObject.GetComponent<Collider>();
        if (existingCollider != null)
        {
            if (showDebugLogs)
                Debug.Log($"[PlantNode] '{plantObject.name}' already has a {existingCollider.GetType().Name}");
            return;
        }

        Renderer renderer = plantObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            AddColliderBasedOnBounds(plantObject, renderer.bounds);
        }
        else
        {
            AddDefaultCollider(plantObject);
        }
    }

    void AddColliderBasedOnBounds(GameObject plantObject, Bounds bounds)
    {
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        float height = bounds.size.y;
        Vector3 center = bounds.center - plantObject.transform.position;

        if (height > width * 1.5f)
        {
            CapsuleCollider capsule = plantObject.AddComponent<CapsuleCollider>();
            capsule.radius = width / 2f;
            capsule.height = height;
            capsule.center = center;
            
            if (showDebugLogs)
                Debug.Log($"[PlantNode] Added CapsuleCollider to '{plantObject.name}'");
        }
        else
        {
            SphereCollider sphere = plantObject.AddComponent<SphereCollider>();
            sphere.radius = Mathf.Max(width, height) / 2f;
            sphere.center = center;
            
            if (showDebugLogs)
                Debug.Log($"[PlantNode] Added SphereCollider to '{plantObject.name}'");
        }
    }

    void AddDefaultCollider(GameObject plantObject)
    {
        SphereCollider sphere = plantObject.AddComponent<SphereCollider>();
        sphere.radius = plantColliderRadius;
        
        if (showDebugLogs)
            Debug.Log($"[PlantNode] Added default SphereCollider to '{plantObject.name}'");
    }

    void ClearExistingPlants()
    {
        if (showDebugLogs && spawnedPlants.Count > 0)
            Debug.Log($"[PlantNode] Clearing {spawnedPlants.Count} existing plants");
        
        foreach (var plantObj in spawnedPlants)
        {
            if (plantObj != null)
                Destroy(plantObj);
        }
        
        spawnedPlants.Clear();
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Draw spawned plants
        if (Application.isPlaying && spawnedPlants != null)
        {
            DrawSpawnedPlants();
        }
        
        // Draw node center
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }

    void DrawSpawnedPlants()
    {
        Gizmos.color = Color.yellow;
        foreach (var plantObj in spawnedPlants)
        {
            if (plantObj == null) continue;

            Gizmos.DrawLine(transform.position, plantObj.transform.position);
            
            Collider col = plantObj.GetComponent<Collider>();
            if (col != null)
            {
                DrawColliderGizmo(plantObj, col);
            }
        }
    }

    void DrawColliderGizmo(GameObject plantObj, Collider col)
    {
        Gizmos.color = Color.green;
        
        if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(plantObj.transform.position + sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            Gizmos.DrawWireSphere(plantObj.transform.position + capsule.center, capsule.radius);
        }
        
        Gizmos.color = Color.yellow;
    }

    #endregion
}