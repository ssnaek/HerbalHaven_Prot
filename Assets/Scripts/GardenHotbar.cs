using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the Garden Hotbar UI that displays seeds from inventory.
/// Each seed type occupies one slot with its stack count displayed.
/// Only shows seeds (items purchased from shop), not harvested herbs.
/// </summary>
public class GardenHotbar : MonoBehaviour
{
    public static GardenHotbar Instance { get; private set; }
    
    [System.Serializable]
    public class HotbarSlot
    {
        [Tooltip("The slot's root GameObject")]
        public GameObject slotObject;
        
        [Tooltip("Image component to display seed icon")]
        public Image iconImage;
        
        [Tooltip("TextMeshPro to display quantity")]
        public TextMeshProUGUI quantityText;
        
        [Tooltip("Empty slot sprite (shown when no seed)")]
        public Sprite emptySprite;
        
        // Runtime data
        [HideInInspector] public string currentItemID;
        [HideInInspector] public bool isEmpty = true;
        [HideInInspector] public Button button;
    }
    
    [Header("Hotbar Slots")]
    [Tooltip("All hotbar slots in order")]
    public HotbarSlot[] hotbarSlots;
    
    [Header("Settings")]
    [Tooltip("Suffix to identify seeds in inventory (e.g., '_seed')")]
    public string seedIDSuffix = "_seed";
    
    [Header("Selection Visual")]
    [Tooltip("Color to highlight selected slot")]
    public Color selectedColor = new Color(1f, 1f, 0.5f, 1f); // Yellow tint
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private Dictionary<string, int> seedInventory = new Dictionary<string, int>();
    private int selectedSlotIndex = -1; // -1 means no selection
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Subscribe to inventory changes
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshHotbar;
        }
        
        // Setup click handlers for slots
        SetupSlotButtons();
        
        // Initial refresh
        RefreshHotbar();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshHotbar;
        }
    }
    
    /// <summary>
    /// Refresh the hotbar display from inventory
    /// </summary>
    public void RefreshHotbar()
    {
        if (showDebugLogs) Debug.Log("[GardenHotbar] Refreshing hotbar");
        
        // Get all seeds from inventory
        CollectSeedsFromInventory();
        
        // Update hotbar slots
        UpdateHotbarSlots();
    }
    
    /// <summary>
    /// Collect all seeds from inventory
    /// </summary>
    void CollectSeedsFromInventory()
    {
        seedInventory.Clear();
        
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[GardenHotbar] InventorySystem not found!");
            return;
        }
        
        List<InventoryItemData> allItems = InventorySystem.Instance.GetAllItems();
        
        foreach (var item in allItems)
        {
            // Check if item is a seed (based on ID suffix or other criteria)
            if (IsSeedItem(item.itemID))
            {
                seedInventory[item.itemID] = item.quantity;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[GardenHotbar] Found seed: {item.itemName} x{item.quantity}");
                }
            }
        }
    }
    
    /// <summary>
    /// Check if an item is a seed
    /// </summary>
    bool IsSeedItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return false;
        
        // Seeds are identified by having "seed" in their ID (supports both "_seed" and "seed_" patterns)
        // This handles variations like "ginger_seed", "seed_lagundi", "seed_garlic", etc.
        return itemID.ToLower().Contains("seed");
    }
    
    /// <summary>
    /// Update all hotbar slot displays
    /// </summary>
    void UpdateHotbarSlots()
    {
        // Store current selection before clearing
        string previousSelectedID = GetSelectedSeedID();
        int previousSelectedIndex = selectedSlotIndex;
        
        // Clear all slots first
        foreach (var slot in hotbarSlots)
        {
            ClearSlot(slot);
        }
        
        // Clear selection
        selectedSlotIndex = -1;
        
        // Fill slots with seeds
        int slotIndex = 0;
        
        foreach (var seedEntry in seedInventory)
        {
            if (slotIndex >= hotbarSlots.Length)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[GardenHotbar] Not enough slots! Have {hotbarSlots.Length}, need {seedInventory.Count}");
                }
                break;
            }
            
            string seedID = seedEntry.Key;
            int quantity = seedEntry.Value;
            
            // Get item data from inventory
            InventoryItemData itemData = InventorySystem.Instance.GetItemData(seedID);
            
            if (itemData != null)
            {
                SetSlot(hotbarSlots[slotIndex], itemData, quantity);
                
                // Restore selection if this was the previously selected seed
                if (seedID == previousSelectedID && quantity > 0)
                {
                    SelectSlot(slotIndex);
                }
                
                slotIndex++;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GardenHotbar] Updated {slotIndex} slots with seeds");
        }
    }
    
    /// <summary>
    /// Setup button components and click handlers for all slots
    /// </summary>
    void SetupSlotButtons()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            int slotIndex = i; // Capture for closure
            
            if (hotbarSlots[i].slotObject == null) continue;
            
            // Get or add Button component
            Button button = hotbarSlots[i].slotObject.GetComponent<Button>();
            if (button == null)
            {
                button = hotbarSlots[i].slotObject.AddComponent<Button>();
            }
            
            hotbarSlots[i].button = button;
            
            // Remove existing listeners and add new one
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSlotClicked(slotIndex));
        }
    }
    
    /// <summary>
    /// Called when a slot is clicked
    /// </summary>
    void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            return;
        }
        
        HotbarSlot slot = hotbarSlots[slotIndex];
        
        if (slot.isEmpty)
        {
            if (showDebugLogs) Debug.Log($"[GardenHotbar] Slot {slotIndex} is empty, cannot select");
            return;
        }
        
        // Toggle selection (clicking same slot deselects)
        if (selectedSlotIndex == slotIndex)
        {
            ClearSelection();
        }
        else
        {
            SelectSlot(slotIndex);
        }
    }
    
    /// <summary>
    /// Select a slot
    /// </summary>
    void SelectSlot(int slotIndex)
    {
        // Clear previous selection visual
        if (selectedSlotIndex >= 0 && selectedSlotIndex < hotbarSlots.Length)
        {
            UpdateSlotVisual(selectedSlotIndex, false);
        }
        
        // Set new selection
        selectedSlotIndex = slotIndex;
        UpdateSlotVisual(selectedSlotIndex, true);
        
        if (showDebugLogs)
        {
            string seedID = hotbarSlots[slotIndex].currentItemID;
            Debug.Log($"[GardenHotbar] Selected slot {slotIndex} with seed: {seedID}");
        }
    }
    
    /// <summary>
    /// Clear selection
    /// </summary>
    public void ClearSelection()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < hotbarSlots.Length)
        {
            UpdateSlotVisual(selectedSlotIndex, false);
        }
        
        selectedSlotIndex = -1;
        
        if (showDebugLogs) Debug.Log("[GardenHotbar] Selection cleared");
    }
    
    /// <summary>
    /// Update visual state of a slot (selected or not)
    /// </summary>
    void UpdateSlotVisual(int slotIndex, bool isSelected)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length) return;
        
        HotbarSlot slot = hotbarSlots[slotIndex];
        
        if (slot.iconImage != null)
        {
            slot.iconImage.color = isSelected ? selectedColor : Color.white;
        }
    }
    
    /// <summary>
    /// Get the currently selected seed ID
    /// </summary>
    public string GetSelectedSeedID()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= hotbarSlots.Length)
        {
            return null;
        }
        
        return hotbarSlots[selectedSlotIndex].currentItemID;
    }
    
    /// <summary>
    /// Set a slot with seed data
    /// </summary>
    void SetSlot(HotbarSlot slot, InventoryItemData seedData, int quantity)
    {
        if (slot == null) return;
        
        slot.isEmpty = false;
        slot.currentItemID = seedData.itemID;
        
        // Set icon
        if (slot.iconImage != null && seedData.icon != null)
        {
            slot.iconImage.sprite = seedData.icon;
            slot.iconImage.color = Color.white;
            slot.iconImage.gameObject.SetActive(true);
        }
        
        // Set quantity text
        if (slot.quantityText != null)
        {
            slot.quantityText.text = quantity.ToString();
            slot.quantityText.gameObject.SetActive(true);
        }
        
        // Activate slot
        if (slot.slotObject != null)
        {
            slot.slotObject.SetActive(true);
        }
        
        // Update visual if this slot is selected
        int slotIndex = System.Array.IndexOf(hotbarSlots, slot);
        if (slotIndex == selectedSlotIndex)
        {
            UpdateSlotVisual(slotIndex, true);
        }
    }
    
    /// <summary>
    /// Clear a slot (make it empty)
    /// </summary>
    void ClearSlot(HotbarSlot slot)
    {
        if (slot == null) return;
        
        slot.isEmpty = true;
        slot.currentItemID = null;
        
        // Set to empty sprite
        if (slot.iconImage != null)
        {
            if (slot.emptySprite != null)
            {
                slot.iconImage.sprite = slot.emptySprite;
                slot.iconImage.color = new Color(1f, 1f, 1f, 0.3f); // Transparent
            }
            else
            {
                slot.iconImage.gameObject.SetActive(false);
            }
        }
        
        // Hide quantity text
        if (slot.quantityText != null)
        {
            slot.quantityText.text = "";
            slot.quantityText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get seed data from a specific slot
    /// </summary>
    public InventoryItemData GetSlotSeed(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            Debug.LogWarning($"[GardenHotbar] Invalid slot index: {slotIndex}");
            return null;
        }
        
        HotbarSlot slot = hotbarSlots[slotIndex];
        
        if (slot.isEmpty || string.IsNullOrEmpty(slot.currentItemID))
        {
            return null;
        }
        
        return InventorySystem.Instance?.GetItemData(slot.currentItemID);
    }
    
    /// <summary>
    /// Check if a slot has a seed
    /// </summary>
    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            return true;
        }
        
        return hotbarSlots[slotIndex].isEmpty;
    }
    
    /// <summary>
    /// Get the item ID from a slot
    /// </summary>
    public string GetSlotItemID(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Length)
        {
            return null;
        }
        
        return hotbarSlots[slotIndex].currentItemID;
    }
    
    /// <summary>
    /// Get total number of available hotbar slots
    /// </summary>
    public int GetSlotCount()
    {
        return hotbarSlots.Length;
    }
    
    /// <summary>
    /// Get number of occupied slots
    /// </summary>
    public int GetOccupiedSlotCount()
    {
        int count = 0;
        foreach (var slot in hotbarSlots)
        {
            if (!slot.isEmpty)
            {
                count++;
            }
        }
        return count;
    }
}
