using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple test UI to display inventory contents.
/// This is temporary for testing - your journal system will replace this.
/// </summary>
public class InventoryUIDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent container where item slots will be created")]
    public Transform itemContainer;

    [Tooltip("Prefab for displaying each item (should have Image and Text components)")]
    public GameObject itemSlotPrefab;
    
    [Tooltip("Main panel object that should be shown/hidden")]
    public GameObject inventoryPanel;  // <-- add this new line

    [Header("Settings")]
    [Tooltip("Toggle visibility with this key")]
    public KeyCode toggleKey = KeyCode.I;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private GameObject panel;
    private List<GameObject> itemSlots = new List<GameObject>();

    void Start()
    {
        // If not manually assigned, try to find a parent panel
        if (inventoryPanel == null)
        {
            // Try the parent first
            if (transform.parent != null)
                inventoryPanel = transform.parent.gameObject;
            else
                inventoryPanel = gameObject; // fallback

            if (showDebugLogs)
                Debug.Log($"[InventoryUI] inventoryPanel not assigned, auto-set to {inventoryPanel.name}");
        }

        panel = inventoryPanel;

        // Subscribe to inventory updates
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshDisplay;
        }
        else
        {
            Debug.LogError("[InventoryUI] InventorySystem.Instance is null! Make sure InventorySystem exists in scene.");
        }

        // Ensure this script's GameObject (UIController) is active
        // but keep the actual inventory panel hidden at start
        if (gameObject.activeSelf == false)
            gameObject.SetActive(true);

        if (panel != null)
            panel.SetActive(false);
    }


    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log("[InventoryUI] Toggle key pressed!");
            ToggleDisplay();
        }
    }

    void ToggleDisplay()
    {
        bool newState = !panel.activeSelf;
        panel.SetActive(newState);

        if (newState)
        {
            RefreshDisplay();
        }

        if (showDebugLogs) Debug.Log($"[InventoryUI] Toggled {(newState ? "ON" : "OFF")}");
    }

    public void RefreshDisplay()
    {
        if (showDebugLogs) Debug.Log("[InventoryUI] Refreshing display");

        // Clear existing slots
        ClearSlots();

        if (InventorySystem.Instance == null) return;

        // Get all items from inventory
        List<InventoryItemData> items = InventorySystem.Instance.GetAllItems();

        // Create a slot for each item
        foreach (var item in items)
        {
            CreateItemSlot(item);
        }
    }

    void ClearSlots()
    {
        foreach (GameObject slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();
    }

    void CreateItemSlot(InventoryItemData item)
    {
        if (itemSlotPrefab == null || itemContainer == null)
        {
            Debug.LogError("[InventoryUI] itemSlotPrefab or itemContainer is null!");
            return;
        }

        GameObject slot = Instantiate(itemSlotPrefab, itemContainer);
        itemSlots.Add(slot);

        // Set icon
        Image iconImage = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }

        // Set name
        TextMeshProUGUI nameText = slot.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        // Set quantity
        TextMeshProUGUI quantityText = slot.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (quantityText != null)
        {
            quantityText.text = $"x{item.quantity}";
        }

        if (showDebugLogs) Debug.Log($"[InventoryUI] Created slot for {item.itemName} x{item.quantity}");
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshDisplay;
        }
    }
}