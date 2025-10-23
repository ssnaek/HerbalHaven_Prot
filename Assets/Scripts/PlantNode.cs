using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plant node that spawns multiple plant types based on rarity weights.
/// Respawns daily based on GlobalPlantManager's regeneration multiplier.
/// Now uses PlantDataSO assets for easy configuration!
/// </summary>
public class PlantNode : MonoBehaviour
{
    [Header("Plant Pool")]
    [Tooltip("Drag PlantDataSO assets here - much easier!")]
    public List<PlantDataSO> plantPool = new List<PlantDataSO>();
    
    [Header("Spawn Settings")]
    [Tooltip("Base number of plants to spawn (affected by regeneration multiplier)")]
    public int basePlantCount = 3;
    public float spawnRadius = 4f;
    public bool addColliderToPlants = true;
    public float plantColliderRadius = 0.5f;

    [Header("Rarity Weights")]
    [Range(0f, 1f)]
    public float commonWeight = 0.70f;
    [Range(0f, 1f)]
    public float uncommonWeight = 0.25f;
    [Range(0f, 1f)]
    public float rareWeight = 0.05f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<GameObject> spawnedPlants = new List<GameObject>();
    private int collectedToday;

    void Start()
    {
        if (plantPool.Count == 0)
        {
            Debug.LogError($"[PlantNode '{gameObject.name}'] Plant pool is empty! Add PlantDataSO assets.");
            return;
        }

        // Validate plant data
        foreach (var plant in plantPool)
        {
            if (plant == null)
            {
                Debug.LogWarning($"[PlantNode '{gameObject.name}'] Null plant in pool!");
                continue;
            }
            
            if (string.IsNullOrEmpty(plant.plantID))
            {
                Debug.LogWarning($"[PlantNode] Plant '{plant.plantName}' has no plantID!");
            }
        }

        SpawnPlantsForNewDay();
    }

    void SpawnPlantsForNewDay()
    {
        int spawnAmount = basePlantCount;

        if (GlobalPlantManager.Instance != null)
        {
            spawnAmount = GlobalPlantManager.Instance.CalculateSpawnAmount(basePlantCount);
            
            if (showDebugLogs) 
            {
                Debug.Log($"[PlantNode '{gameObject.name}'] Base: {basePlantCount}, Adjusted: {spawnAmount}");
            }
        }
        else
        {
            Debug.LogWarning("[PlantNode] GlobalPlantManager not found! Using base spawn amount.");
        }

        SpawnPlants(spawnAmount);
    }

    void SpawnPlants(int amount)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode '{gameObject.name}'] ═══ SpawnPlants({amount}) ═══");
        
        ClearPlants();

        for (int i = 0; i < amount; i++)
        {
            PlantDataSO selectedPlant = SelectPlantByRarity();
            
            if (selectedPlant == null || selectedPlant.plantPrefab == null)
            {
                Debug.LogError($"[PlantNode '{gameObject.name}'] Selected plant is null or has no prefab!");
                continue;
            }

            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos.y = transform.position.y;

            GameObject plant = Instantiate(selectedPlant.plantPrefab, spawnPos, Quaternion.identity, transform);
            plant.name = $"{selectedPlant.plantName}_{i}";

            InteractablePlant interactable = plant.GetComponent<InteractablePlant>();
            if (interactable == null)
            {
                interactable = plant.AddComponent<InteractablePlant>();
            }
            interactable.parentNode = this;
            interactable.plantData = selectedPlant;
            interactable.showDebugLogs = showDebugLogs;

            EnsureCollider(plant);

            spawnedPlants.Add(plant);
            
            if (showDebugLogs) 
            {
                Debug.Log($"[PlantNode] Spawned '{plant.name}' ({selectedPlant.rarity}) at {spawnPos}");
            }
        }

        collectedToday = 0;
        
        if (showDebugLogs) Debug.Log($"[PlantNode] ✓ Spawn complete. Total: {spawnedPlants.Count} plants");
    }

    PlantDataSO SelectPlantByRarity()
    {
        List<PlantDataSO> commonPlants = new List<PlantDataSO>();
        List<PlantDataSO> uncommonPlants = new List<PlantDataSO>();
        List<PlantDataSO> rarePlants = new List<PlantDataSO>();

        foreach (var plant in plantPool)
        {
            if (plant == null) continue;
            
            switch (plant.rarity)
            {
                case PlantDataSO.PlantRarity.Common:
                    commonPlants.Add(plant);
                    break;
                case PlantDataSO.PlantRarity.Uncommon:
                    uncommonPlants.Add(plant);
                    break;
                case PlantDataSO.PlantRarity.Rare:
                    rarePlants.Add(plant);
                    break;
            }
        }

        float roll = Random.value;
        float cumulative = 0f;

        cumulative += rareWeight;
        if (roll <= cumulative && rarePlants.Count > 0)
        {
            return rarePlants[Random.Range(0, rarePlants.Count)];
        }

        cumulative += uncommonWeight;
        if (roll <= cumulative && uncommonPlants.Count > 0)
        {
            return uncommonPlants[Random.Range(0, uncommonPlants.Count)];
        }

        if (commonPlants.Count > 0)
        {
            return commonPlants[Random.Range(0, commonPlants.Count)];
        }

        if (plantPool.Count > 0)
        {
            return plantPool[Random.Range(0, plantPool.Count)];
        }

        return null;
    }

    public void CollectSpecificPlant(GameObject plant, PlantDataSO plantData)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode] CollectSpecificPlant('{plant.name}') called");

        if (!spawnedPlants.Contains(plant))
        {
            Debug.LogWarning($"[PlantNode] Plant '{plant.name}' not found in spawned list!");
            return;
        }

        CollectPlant(plant, plantData);
    }

    void CollectPlant(GameObject plant, PlantDataSO plantData)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode] ►►► CollectPlant('{plant.name}') ◄◄◄");
        
        bool removed = spawnedPlants.Remove(plant);
        if (showDebugLogs) Debug.Log($"[PlantNode] Removed from list: {removed}");
        
        Destroy(plant);
        if (showDebugLogs) Debug.Log($"[PlantNode] Plant GameObject destroyed");

        collectedToday++;

        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.RegisterHarvest();
        }
        else
        {
            Debug.LogWarning("[PlantNode] TimeSystem.Instance is null! Cannot register harvest.");
        }
        
        AddToInventory(plantData.plantID, plantData.plantName, plantData.icon, plantData.yieldPerPick);

        if (showDebugLogs) 
        {
            Debug.Log($"[PlantNode] Remaining plants: {spawnedPlants.Count}");
            Debug.Log($"[PlantNode] Collected today: {collectedToday}");
        }
    }

    void AddToInventory(string itemID, string itemName, Sprite icon, int quantity)
    {
        if (InventorySystem.Instance != null)
        {
            // Get the PlantDataSO for this plant
            PlantDataSO herbData = null;
            foreach (var plant in plantPool)
            {
                if (plant != null && plant.plantID == itemID)
                {
                    herbData = plant;
                    break;
                }
            }
            
            // Add with herb data reference for cross-scene access
            InventorySystem.Instance.AddItem(itemID, itemName, icon, quantity, herbData);
            
            if (showDebugLogs) Debug.Log($"✓ Added {quantity}x {itemName} to inventory with herb data");
        }
        else
        {
            Debug.LogError("[PlantNode] InventorySystem.Instance is null!");
        }
    }

    void EnsureCollider(GameObject plant)
    {
        if (!addColliderToPlants) return;

        Collider existingCollider = plant.GetComponent<Collider>();
        if (existingCollider != null)
        {
            if (showDebugLogs) Debug.Log($"[PlantNode] '{plant.name}' already has a {existingCollider.GetType().Name}");
            return;
        }

        Renderer renderer = plant.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = bounds.size.y;

            if (height > width * 1.5f)
            {
                CapsuleCollider capsule = plant.AddComponent<CapsuleCollider>();
                capsule.radius = width / 2f;
                capsule.height = height;
                capsule.center = renderer.bounds.center - plant.transform.position;
                
                if (showDebugLogs) Debug.Log($"[PlantNode] Added CapsuleCollider to '{plant.name}'");
            }
            else
            {
                SphereCollider sphere = plant.AddComponent<SphereCollider>();
                sphere.radius = Mathf.Max(width, height) / 2f;
                sphere.center = renderer.bounds.center - plant.transform.position;
                
                if (showDebugLogs) Debug.Log($"[PlantNode] Added SphereCollider to '{plant.name}'");
            }
        }
        else
        {
            SphereCollider sphere = plant.AddComponent<SphereCollider>();
            sphere.radius = plantColliderRadius;
            
            if (showDebugLogs) Debug.Log($"[PlantNode] Added default SphereCollider to '{plant.name}'");
        }
    }

    void ClearPlants()
    {
        if (showDebugLogs) Debug.Log($"[PlantNode] Clearing {spawnedPlants.Count} existing plants");
        
        foreach (var plant in spawnedPlants)
        {
            if (plant != null)
            {
                if (showDebugLogs) Debug.Log($"[PlantNode] Destroying '{plant.name}'");
                Destroy(plant);
            }
        }
        spawnedPlants.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        if (Application.isPlaying && spawnedPlants != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var plant in spawnedPlants)
            {
                if (plant != null)
                {
                    Gizmos.DrawLine(transform.position, plant.transform.position);
                    
                    Collider col = plant.GetComponent<Collider>();
                    if (col != null)
                    {
                        Gizmos.color = Color.green;
                        if (col is SphereCollider sphere)
                        {
                            Gizmos.DrawWireSphere(plant.transform.position + sphere.center, sphere.radius);
                        }
                        else if (col is CapsuleCollider capsule)
                        {
                            Gizmos.DrawWireSphere(plant.transform.position + capsule.center, capsule.radius);
                        }
                        Gizmos.color = Color.yellow;
                    }
                }
            }
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}