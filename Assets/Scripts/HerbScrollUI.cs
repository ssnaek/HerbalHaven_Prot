using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays collected herbs in a scrollview for crafting.
/// Clicking an herb adds it to the crafting slots (max 3).
/// Integrates with existing InventorySystem and PlantDataSO.
/// </summary>
public class HerbScrollUI : MonoBehaviour
{
    public static HerbScrollUI Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("ScrollView content container where herb items spawn")]
    public Transform scrollContent;
    
    [Tooltip("Prefab for each herb item in the scroll")]
    public GameObject herbItemPrefab;
    
    [Header("Crafting Manager")]
    [Tooltip("Reference to the medicine crafting system")]
    public MedicineCraftingManager craftingManager;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private List<GameObject> herbItems = new List<GameObject>();
    
    void Awake()
    {
        // Singleton pattern to prevent multiple instances
        if (Instance == null)
        {
            Instance = this;
            if (showDebugLogs) Debug.Log("[HerbScroll] Instance created");
        }
        else
        {
            Debug.LogWarning("[HerbScroll] Duplicate HerbScrollUI detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Subscribe to inventory changes
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshHerbList;
            if (showDebugLogs) Debug.Log("[HerbScroll] Subscribed to inventory changes");
        }
        
        // Initial populate
        RefreshHerbList();
    }
    
    void OnDestroy()
    {
        // Unsubscribe
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshHerbList;
        }
    }
    
    /// <summary>
    /// Refresh the herb list from inventory
    /// </summary>
    public void RefreshHerbList()
    {
        if (showDebugLogs) Debug.Log("[HerbScroll] Refreshing herb list");
        
        // Clear existing items BEFORE creating new ones
        ClearHerbList();
        
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[HerbScroll] InventorySystem not found!");
            return;
        }
        
        // Get all items from inventory
        List<InventoryItemData> items = InventorySystem.Instance.GetAllItems();
        
        if (showDebugLogs) Debug.Log($"[HerbScroll] Found {items.Count} total items in inventory");
        
        int herbCount = 0;
        
        // Create UI item for each HERB (items with herbData - not seeds)
        foreach (var item in items)
        {
            // FILTER: Only show items that have herbData (actual herbs, not seeds)
            if (item.herbData != null)
            {
                CreateHerbItem(item);
                herbCount++;
            }
            else
            {
                if (showDebugLogs) Debug.Log($"[HerbScroll] Skipping '{item.itemName}' - no herbData (probably a seed)");
            }
        }
        
        if (showDebugLogs) Debug.Log($"[HerbScroll] Created {herbCount} herb items (filtered from {items.Count} total items)");
    }
    
    void ClearHerbList()
    {
        if (showDebugLogs) Debug.Log($"[HerbScroll] Clearing {herbItems.Count} existing items");
        
        foreach (GameObject item in herbItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        herbItems.Clear();
        
        if (showDebugLogs) Debug.Log("[HerbScroll] Clear complete");
    }
    
    void CreateHerbItem(InventoryItemData herbData)
    {
        if (herbItemPrefab == null || scrollContent == null)
        {
            Debug.LogError("[HerbScroll] herbItemPrefab or scrollContent not assigned!");
            return;
        }
        
        GameObject itemObj = Instantiate(herbItemPrefab, scrollContent);
        herbItems.Add(itemObj);
        
        // Set icon
        Image iconImage = itemObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && herbData.icon != null)
        {
            iconImage.sprite = herbData.icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        
        // Set name
        TextMeshProUGUI nameText = itemObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = herbData.itemName;
        }
        
        // Set quantity
        TextMeshProUGUI quantityText = itemObj.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (quantityText != null)
        {
            quantityText.text = $"x{herbData.quantity}";
        }
        
        // Wire up button to add herb to crafting
        Button button = itemObj.GetComponent<Button>();
        if (button == null)
        {
            button = itemObj.AddComponent<Button>();
        }
        
        // Capture herbData for button click
        PlantDataSO plantData = herbData.herbData;
        button.onClick.AddListener(() => OnHerbClicked(plantData));
        
        if (showDebugLogs) Debug.Log($"[HerbScroll] Created item: {herbData.itemName} x{herbData.quantity}");
    }
    
    void OnHerbClicked(PlantDataSO herbData)
    {
        if (herbData == null)
        {
            Debug.LogWarning("[HerbScroll] Clicked herb has no PlantDataSO!");
            return;
        }
        
        if (craftingManager == null)
        {
            Debug.LogError("[HerbScroll] MedicineCraftingManager not assigned!");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"[HerbScroll] Herb clicked: {herbData.plantName}");
        
        // Add to crafting slots
        craftingManager.AddHerb(herbData);
    }
}