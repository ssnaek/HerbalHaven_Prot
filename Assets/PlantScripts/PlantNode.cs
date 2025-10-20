using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Invisible node that spawns and manages a cluster of harvestable plants.
/// The node itself is NOT interactable - individual plants are.
/// </summary>
public class PlantNode : MonoBehaviour
{
    [System.Serializable]
    public class PlantData
    {
        public string plantName;
        public string plantID;  // Unique identifier for inventory
        public Sprite icon;
        public int maxStack = 99;
        public int yieldPerPick = 1;
        public GameObject plantPrefab;  // Complete prefab with everything set up
    }

    [Header("Plant Settings")]
    public PlantData plantInfo;
    public int maxDailyPlants = 5;
    public float spawnRadius = 4f;
    
    [Header("Spawn Settings")]
    public bool addColliderToPlants = true;  // Auto-add colliders if missing
    public float plantColliderRadius = 0.5f;  // Size of auto-generated collider

    [Header("Regrowth Settings")]
    public int overharvestThreshold = 3;
    public float regrowthDelay = 10f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<GameObject> spawnedPlants = new List<GameObject>();
    private int collectedToday;
    private bool regenerating = false;

    void Start()
    {
        // Validate plant ID
        if (string.IsNullOrEmpty(plantInfo.plantID))
        {
            plantInfo.plantID = plantInfo.plantName.Replace(" ", "_").ToLower();
            if (showDebugLogs) Debug.Log($"[PlantNode] Auto-generated plant ID: {plantInfo.plantID}");
        }

        SpawnPlants(maxDailyPlants);
    }

    // --- Public method for plants to call when collected ---
    public void CollectSpecificPlant(GameObject plant)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode] CollectSpecificPlant('{plant.name}') called");

        if (!spawnedPlants.Contains(plant))
        {
            Debug.LogWarning($"[PlantNode] Plant '{plant.name}' not found in spawned list!");
            return;
        }

        CollectPlant(plant);
    }

    // --- Core plant logic ---
    void SpawnPlants(int amount)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode '{plantInfo.plantName}'] ═══ SpawnPlants({amount}) ═══");
        
        ClearPlants();

        if (plantInfo.plantPrefab == null)
        {
            Debug.LogError($"[PlantNode '{gameObject.name}'] plantInfo.plantPrefab is NULL! Cannot spawn plants.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos.y = transform.position.y;

            GameObject plant = Instantiate(plantInfo.plantPrefab, spawnPos, Quaternion.identity, transform);
            plant.name = $"{plantInfo.plantName}_{i}";
            
            // Plant inherits layer from prefab - no need to set it here

            // Add InteractablePlant component
            InteractablePlant interactable = plant.GetComponent<InteractablePlant>();
            if (interactable == null)
            {
                interactable = plant.AddComponent<InteractablePlant>();
            }
            interactable.parentNode = this;
            interactable.showDebugLogs = showDebugLogs;

            // Ensure plant has a collider
            EnsureCollider(plant);

            spawnedPlants.Add(plant);
            
            if (showDebugLogs) Debug.Log($"[PlantNode] Spawned '{plant.name}' at {spawnPos}");
        }

        collectedToday = 0;
        regenerating = false;
        
        if (showDebugLogs) Debug.Log($"[PlantNode] ✓ Spawn complete. Total: {spawnedPlants.Count} plants");
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

        // Try to intelligently choose collider type based on plant structure
        Renderer renderer = plant.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = bounds.size.y;

            // If tall and narrow, use capsule
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
                // Otherwise use sphere
                SphereCollider sphere = plant.AddComponent<SphereCollider>();
                sphere.radius = Mathf.Max(width, height) / 2f;
                sphere.center = renderer.bounds.center - plant.transform.position;
                
                if (showDebugLogs) Debug.Log($"[PlantNode] Added SphereCollider to '{plant.name}'");
            }
        }
        else
        {
            // Fallback: simple sphere collider
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

    void CollectPlant(GameObject plant)
    {
        if (showDebugLogs) Debug.Log($"[PlantNode] ►►► CollectPlant('{plant.name}') ◄◄◄");
        
        bool removed = spawnedPlants.Remove(plant);
        if (showDebugLogs) Debug.Log($"[PlantNode] Removed from list: {removed}");
        
        Destroy(plant);
        if (showDebugLogs) Debug.Log($"[PlantNode] Plant GameObject destroyed");

        collectedToday++;
        
        // Send to inventory system (you'll implement this later)
        AddToInventory(plantInfo.plantID, plantInfo.plantName, plantInfo.yieldPerPick);

        if (showDebugLogs) 
        {
            Debug.Log($"[PlantNode] Remaining plants: {spawnedPlants.Count}");
            Debug.Log($"[PlantNode] Collected today: {collectedToday}");
        }

        if (spawnedPlants.Count == 0 && !regenerating)
        {
            if (showDebugLogs) Debug.Log("[PlantNode] All plants collected! Starting regrowth coroutine...");
            StartCoroutine(RegrowPlants());
        }
    }

    IEnumerator RegrowPlants()
    {
        regenerating = true;
        Debug.Log($"🌿 [{plantInfo.plantName}] Patch regenerating in {regrowthDelay} seconds...");

        yield return new WaitForSeconds(regrowthDelay);

        if (collectedToday >= overharvestThreshold)
        {
            maxDailyPlants = Mathf.Max(1, maxDailyPlants - 1);
            Debug.Log($"⚠️ Overharvested! Reducing max plants to {maxDailyPlants}");
        }
        else
        {
            maxDailyPlants = Mathf.Min(maxDailyPlants + 1, 10);
            Debug.Log($"✓ Good harvest! Increasing max plants to {maxDailyPlants}");
        }

        SpawnPlants(maxDailyPlants);
        Debug.Log($"🌱 [{plantInfo.plantName}] Patch regrown with {maxDailyPlants} plants!");
    }

    void AddToInventory(string itemID, string itemName, int quantity)
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(itemID, itemName, plantInfo.icon, quantity);
            
            if (showDebugLogs) Debug.Log($"✓ Added {quantity}x {itemName} to inventory");
        }
        else
        {
            Debug.LogError("[PlantNode] InventorySystem.Instance is null! Make sure InventorySystem exists in scene.");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw spawn radius (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Draw lines to spawned plants (yellow)
        if (Application.isPlaying && spawnedPlants != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var plant in spawnedPlants)
            {
                if (plant != null)
                {
                    Gizmos.DrawLine(transform.position, plant.transform.position);
                    
                    // Draw plant collider bounds in green
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
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}