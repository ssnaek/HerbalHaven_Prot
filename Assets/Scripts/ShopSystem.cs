using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the shop UI and purchasing logic.
/// Left side: Scrollable item list
/// Right side: Selected item details with buy button
/// </summary>
public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    [Header("Shop Panel")]
    public GameObject shopPanel;

    [Header("Left Side - Item List")]
    public Transform shopItemListContainer;
    public GameObject shopItemTabPrefab;

    [Header("Right Side - Item Details")]
    public Image itemDetailImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI itemPriceText;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;
    public GameObject noSelectionMessage; // "Select an item" placeholder

    [Header("Currency Display")]
    public TextMeshProUGUI playerCurrencyText;

    [Header("Shop Inventory")]
    [Tooltip("All items available in the shop")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Settings")]
    public bool allowKeyboardToggle = false; // Disable to only open via NPC
    public KeyCode toggleKey = KeyCode.B;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<GameObject> itemTabs = new List<GameObject>();
    private ShopItemData currentSelectedItem = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Wire up buy button
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);

        // Start closed
        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Show "no selection" state
        ShowNoSelection();
    }

    void Update()
    {
        if (allowKeyboardToggle && Input.GetKeyDown(toggleKey))
        {
            ToggleShop();
        }
    }

    void ToggleShop()
    {
        bool newState = !shopPanel.activeSelf;
        shopPanel.SetActive(newState);

        if (newState)
        {
            RefreshShop();
        }

        if (showDebugLogs) Debug.Log($"[Shop] Toggled {(newState ? "OPEN" : "CLOSED")}");
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        RefreshShop();
        
        if (showDebugLogs) Debug.Log("[Shop] Opened by NPC");
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        
        if (showDebugLogs) Debug.Log("[Shop] Closed");
    }

    void RefreshShop()
    {
        // Check if currency system exists
        if (JournalController.Instance == null)
        {
            Debug.LogError("[Shop] JournalController.Instance is NULL! Make sure JournalUIController exists in scene with JournalController.cs");
        }

        // Update currency display
        UpdateCurrencyDisplay();

        // Clear existing tabs
        ClearItemTabs();

        // Create item tabs
        foreach (var item in shopItems)
        {
            CreateItemTab(item);
        }

        // Reset selection
        currentSelectedItem = null;
        ShowNoSelection();

        if (showDebugLogs) Debug.Log($"[Shop] Refreshed with {shopItems.Count} items");
    }

    void ClearItemTabs()
    {
        foreach (var tab in itemTabs)
        {
            Destroy(tab);
        }
        itemTabs.Clear();
    }

    void CreateItemTab(ShopItemData item)
    {
        if (shopItemTabPrefab == null || shopItemListContainer == null) return;

        GameObject tabObj = Instantiate(shopItemTabPrefab, shopItemListContainer);
        itemTabs.Add(tabObj);

        // Set icon
        Image iconImage = tabObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }

        // Set name
        TextMeshProUGUI nameText = tabObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        // Set price
        TextMeshProUGUI priceText = tabObj.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (priceText != null)
        {
            priceText.text = $"${item.price}";
        }

        // Add button functionality
        Button button = tabObj.GetComponent<Button>();
        if (button == null)
        {
            button = tabObj.AddComponent<Button>();
        }

        button.onClick.AddListener(() => OnItemTabClicked(item));
    }

    void OnItemTabClicked(ShopItemData item)
    {
        if (showDebugLogs) Debug.Log($"[Shop] Item tab clicked: {item.itemName}");

        currentSelectedItem = item;
        DisplayItemDetails(item);
    }

    void DisplayItemDetails(ShopItemData item)
    {
        // Hide "no selection" message
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        // Show item image
        if (itemDetailImage != null)
        {
            itemDetailImage.sprite = item.icon;
            itemDetailImage.gameObject.SetActive(item.icon != null);
        }

        // Show name
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        // Show description
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
            itemDescriptionText.gameObject.SetActive(true);
        }

        // Show price
        if (itemPriceText != null)
        {
            itemPriceText.text = $"${item.price}";
            itemPriceText.gameObject.SetActive(true);
        }

        // Update buy button
        UpdateBuyButton();
    }

    void ShowNoSelection()
    {
        // Hide detail elements
        if (itemDetailImage != null)
            itemDetailImage.gameObject.SetActive(false);

        if (itemNameText != null)
        {
            itemNameText.text = "";
            itemNameText.gameObject.SetActive(false);
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = "";
            itemDescriptionText.gameObject.SetActive(false);
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = "";
            itemPriceText.gameObject.SetActive(false);
        }

        if (buyButton != null)
            buyButton.gameObject.SetActive(false);

        // Show "no selection" message
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(true);
    }

    void UpdateBuyButton()
    {
        if (buyButton == null || currentSelectedItem == null) return;

        buyButton.gameObject.SetActive(true);

        // Check if player can afford
        int playerCurrency = 0;
        if (JournalController.Instance != null)
        {
            playerCurrency = JournalController.Instance.GetCurrency();
            if (showDebugLogs) Debug.Log($"[Shop] Player has ${playerCurrency}, item costs ${currentSelectedItem.price}");
        }
        else
        {
            Debug.LogError("[Shop] JournalController.Instance is NULL when checking currency!");
        }

        bool canAfford = playerCurrency >= currentSelectedItem.price;

        // Update button state
        buyButton.interactable = canAfford;

        // Update button text
        if (buyButtonText != null)
        {
            if (canAfford)
            {
                buyButtonText.text = $"Buy (${currentSelectedItem.price})";
            }
            else
            {
                buyButtonText.text = $"Not Enough Money (Need ${currentSelectedItem.price})";
            }
        }
    }

    void OnBuyButtonClicked()
    {
        if (currentSelectedItem == null) return;

        // Deduct currency
        if (JournalController.Instance != null)
        {
            bool success = JournalController.Instance.RemoveCurrency(currentSelectedItem.price);

            if (success)
            {
                // Give item to player
                GiveItemToPlayer(currentSelectedItem);

                // Update displays
                UpdateCurrencyDisplay();
                UpdateBuyButton(); // Update button state after purchase

                if (showDebugLogs) Debug.Log($"[Shop] Purchased {currentSelectedItem.itemName}");
            }
            else
            {
                if (showDebugLogs) Debug.Log("[Shop] Purchase failed - not enough money");
            }
        }
    }

    void GiveItemToPlayer(ShopItemData item)
    {
        switch (item.itemType)
        {
            case ShopItemType.Seed:
                // Add to inventory (using existing inventory system)
                if (InventorySystem.Instance != null && JournalDatabase.Instance != null)
                {
                    // Get plant data for icon
                    JournalPlantData plantData = JournalDatabase.Instance.GetPlantData(item.plantID);
                    Sprite icon = plantData != null ? plantData.icon : item.icon;

                    InventorySystem.Instance.AddItem(
                        item.plantID, 
                        item.itemName, 
                        icon, 
                        item.seedQuantity
                    );

                    if (showDebugLogs) Debug.Log($"[Shop] Added {item.seedQuantity}x {item.itemName} to inventory");
                }
                break;

            case ShopItemType.Tool:
            case ShopItemType.Decoration:
            case ShopItemType.Consumable:
                // Placeholder for future item types
                if (showDebugLogs) Debug.Log($"[Shop] Gave {item.itemName} ({item.itemType})");
                break;
        }
    }

    void UpdateCurrencyDisplay()
    {
        if (playerCurrencyText != null && JournalController.Instance != null)
        {
            playerCurrencyText.text = $"${JournalController.Instance.GetCurrency()}";
        }
    }

    /// <summary>
    /// Check if shop is currently open
    /// </summary>
    public bool IsShopOpen()
    {
        return shopPanel != null && shopPanel.activeSelf;
    }
}