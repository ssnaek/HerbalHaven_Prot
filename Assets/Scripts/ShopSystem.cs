using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the shop UI and purchasing logic.
/// Left side: Scrollable item list
/// Right side: Selected item details with buy button
/// Now supports UIOpenAnimator for slide-down animation.
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
    public GameObject noSelectionMessage;

    [Header("Currency Display")]
    public TextMeshProUGUI playerCurrencyText;

    [Header("Shop Inventory")]
    [Tooltip("All items available in the shop")]
    public List<ShopItemData> shopItems = new List<ShopItemData>();

    [Header("Settings")]
    public bool allowKeyboardToggle = false;
    public KeyCode toggleKey = KeyCode.B;
    public bool useAnimator = false;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Events for InteractionDetector
    public delegate void OnShopStateChanged();
    public event OnShopStateChanged onShopOpened;
    public event OnShopStateChanged onShopClosed;

    private UIOpenAnimator animator;
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
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);

        if (shopPanel != null)
            shopPanel.SetActive(false);

        ShowNoSelection();

        if (useAnimator)
        {
            animator = GetComponentInParent<UIOpenAnimator>();
            if (animator == null)
            {
                // Try to find it by looking for Shop type
                UIOpenAnimator[] animators = FindObjectsOfType<UIOpenAnimator>();
                foreach (var anim in animators)
                {
                    if (anim.uiType == UIOpenAnimator.UIType.Shop)
                    {
                        animator = anim;
                        break;
                    }
                }
            }

            if (animator == null && showDebugLogs)
            {
                Debug.LogWarning("[Shop] useAnimator is true but no UIOpenAnimator found!");
            }
        }
    }

    void Update()
    {
        if (allowKeyboardToggle && Input.GetKeyDown(toggleKey))
        {
            if (useAnimator && animator != null)
            {
                if (!animator.isAnimating)
                {
                    if (!animator.isOpen)
                    {
                        animator.PlayOpen();
                        RefreshShop();
                    }
                    else
                    {
                        animator.PlayClose();
                    }
                }
            }
            else
            {
                ToggleShop();
            }
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
        if (useAnimator && animator != null)
        {
            if (!animator.isAnimating && !animator.isOpen)
            {
                animator.PlayOpen();
                RefreshShop();
            }
        }
        else
        {
            shopPanel.SetActive(true);
            RefreshShop();
        }
        
        onShopOpened?.Invoke(); // Notify listeners
        
        if (showDebugLogs) Debug.Log("[Shop] Opened by NPC");
    }

    public void CloseShop()
    {
        if (useAnimator && animator != null)
        {
            if (!animator.isAnimating && animator.isOpen)
            {
                animator.PlayClose();
            }
        }
        else
        {
            shopPanel.SetActive(false);
        }
        
        onShopClosed?.Invoke(); // Notify listeners
        
        if (showDebugLogs) Debug.Log("[Shop] Closed");
    }

    void RefreshShop()
    {
        if (JournalController.Instance == null)
        {
            Debug.LogError("[Shop] JournalController.Instance is NULL! Make sure JournalUIController exists in scene with JournalController.cs");
        }

        UpdateCurrencyDisplay();
        ClearItemTabs();

        foreach (var item in shopItems)
        {
            CreateItemTab(item);
        }

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

        Image iconImage = tabObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }

        TextMeshProUGUI nameText = tabObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        TextMeshProUGUI priceText = tabObj.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (priceText != null)
        {
            priceText.text = $"${item.price}";
        }

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
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        if (itemDetailImage != null)
        {
            itemDetailImage.sprite = item.icon;
            itemDetailImage.gameObject.SetActive(item.icon != null);
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
            itemDescriptionText.gameObject.SetActive(true);
        }

        if (itemPriceText != null)
        {
            itemPriceText.text = $"${item.price}";
            itemPriceText.gameObject.SetActive(true);
        }

        UpdateBuyButton();
    }

    void ShowNoSelection()
    {
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

        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(true);
    }

    void UpdateBuyButton()
    {
        if (buyButton == null || currentSelectedItem == null) return;

        buyButton.gameObject.SetActive(true);

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

        buyButton.interactable = canAfford;

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

        if (JournalController.Instance != null)
        {
            bool success = JournalController.Instance.RemoveCurrency(currentSelectedItem.price);

            if (success)
            {
                GiveItemToPlayer(currentSelectedItem);
                UpdateCurrencyDisplay();
                UpdateBuyButton();

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
                if (InventorySystem.Instance != null && JournalDatabase.Instance != null)
                {
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

    public bool IsShopOpen()
    {
        if (useAnimator && animator != null)
        {
            return animator.isOpen;
        }
        return shopPanel != null && shopPanel.activeSelf;
    }
}