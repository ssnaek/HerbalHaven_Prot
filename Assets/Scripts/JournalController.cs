using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main journal controller. Handles opening/closing and coordinating between list and detail view.
/// Now uses ShopItemData as primary source, with fallback to JournalPlantData for wild plants.
/// </summary>
public class JournalController : MonoBehaviour
{
    public static JournalController Instance { get; private set; }

    [Header("Journal Panel")]
    [Tooltip("The entire journal UI (both pages)")]
    public GameObject journalPanel;
    
    [Header("Currency Display")]
    public TextMeshProUGUI currencyText;
    public int playerCurrency = 100;

    [Header("Left Page - Plant Details")]
    public Image plantPhoto;
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI plantDescriptionText;
    public TextMeshProUGUI scientificNameText;
    public TextMeshProUGUI habitatText;
    public TextMeshProUGUI usesText;
    public GameObject noSelectionMessage;

    [Header("Right Page - Plant List")]
    public Transform plantListContainer;
    public GameObject plantListItemPrefab;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.J;
    public bool useAnimator = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Events for InteractionDetector
    public delegate void OnJournalStateChanged();
    public event OnJournalStateChanged onJournalOpened;
    public event OnJournalStateChanged onJournalClosed;

    private UIOpenAnimator animator;
    private List<GameObject> listItems = new List<GameObject>();
    private string currentSelectedPlantID = null;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[Journal] Multiple JournalControllers detected! Using first instance.");
        }

        if (useAnimator)
        {
            animator = GetComponentInParent<UIOpenAnimator>();
            if (animator == null)
            {
                // Find all animators and get the Journal one specifically
                UIOpenAnimator[] animators = FindObjectsOfType<UIOpenAnimator>();
                foreach (var anim in animators)
                {
                    if (anim.uiType == UIOpenAnimator.UIType.Journal)
                    {
                        animator = anim;
                        break;
                    }
                }
            }
            
            if (animator == null && showDebugLogs)
            {
                Debug.LogWarning("[Journal] useAnimator is true but no UIOpenAnimator found!");
            }
            else if (showDebugLogs)
            {
                Debug.Log($"[Journal] Found animator: {animator.gameObject.name}, type: {animator.uiType}");
            }
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshPlantList;
        }

        InitializeLeftPage();
        UpdateCurrencyDisplay();
    }

    void InitializeLeftPage()
    {
        if (plantPhoto != null)
        {
            plantPhoto.gameObject.SetActive(false);
        }

        if (plantNameText != null)
        {
            plantNameText.text = "";
            plantNameText.gameObject.SetActive(false);
        }

        if (plantDescriptionText != null)
        {
            plantDescriptionText.text = "";
            plantDescriptionText.gameObject.SetActive(false);
        }

        if (scientificNameText != null)
        {
            scientificNameText.gameObject.SetActive(false);
        }

        if (habitatText != null)
        {
            habitatText.gameObject.SetActive(false);
        }

        if (usesText != null)
        {
            usesText.gameObject.SetActive(false);
        }

        if (noSelectionMessage != null)
        {
            noSelectionMessage.SetActive(true);
        }

        if (showDebugLogs) Debug.Log("[Journal] Left page initialized - showing only NoSelectionMessage");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (useAnimator && animator != null)
            {
                if (animator.isAnimating && !animator.isOpen)
                {
                    animator.RequestCancel();
                    if (showDebugLogs) Debug.Log("[Journal] Cancel requested during animation");
                    return;
                }
                
                if (!animator.isAnimating)
                {
                    if (!animator.isOpen)
                    {
                        animator.PlayOpen();
                        OnJournalOpened();
                    }
                    else
                    {
                        animator.PlayClose();
                        onJournalClosed?.Invoke(); // Notify listeners
                    }
                }
            }
            else
            {
                ToggleJournal();
            }
        }
    }

    void ToggleJournal()
    {
        bool newState = !journalPanel.activeSelf;
        journalPanel.SetActive(newState);

        if (newState)
        {
            OnJournalOpened();
        }
        else
        {
            onJournalClosed?.Invoke(); // Notify listeners
        }

        if (showDebugLogs) Debug.Log($"[Journal] Toggled {(newState ? "OPEN" : "CLOSED")}");
    }

    public void OnJournalOpened()
    {
        RefreshPlantList();
        UpdateCurrencyDisplay();
        
        if (currentSelectedPlantID == null)
        {
            ShowNoSelection();
        }

        onJournalOpened?.Invoke(); // Notify listeners

        if (showDebugLogs) Debug.Log("[Journal] OnJournalOpened - content refreshed");
    }

    public void RefreshPlantList()
    {
        if (showDebugLogs) Debug.Log("[Journal] Refreshing plant list");

        ClearList();

        if (InventorySystem.Instance == null) return;

        List<InventoryItemData> plants = InventorySystem.Instance.GetAllItems();

        foreach (var plant in plants)
        {
            CreateListItem(plant);
        }
    }

    void ClearList()
    {
        foreach (GameObject item in listItems)
        {
            Destroy(item);
        }
        listItems.Clear();
    }

    void CreateListItem(InventoryItemData plantData)
    {
        if (plantListItemPrefab == null || plantListContainer == null)
        {
            Debug.LogError("[Journal] plantListItemPrefab or plantListContainer is null!");
            return;
        }

        GameObject item = Instantiate(plantListItemPrefab, plantListContainer);
        listItems.Add(item);

        Image iconImage = item.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && plantData.icon != null)
        {
            iconImage.sprite = plantData.icon;
            iconImage.color = Color.white;
        }

        TextMeshProUGUI nameText = item.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = plantData.itemName;
        }

        TextMeshProUGUI quantityText = item.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (quantityText != null)
        {
            quantityText.text = $"x{plantData.quantity}";
        }

        Button button = item.GetComponent<Button>();
        if (button == null)
        {
            button = item.AddComponent<Button>();
        }

        string plantID = plantData.itemID;
        button.onClick.AddListener(() => OnPlantSelected(plantID));

        if (showDebugLogs) Debug.Log($"[Journal] Created list item for {plantData.itemName}");
    }

    public void OnPlantSelected(string plantID)
    {
        if (showDebugLogs) Debug.Log($"[Journal] Plant selected: {plantID}");

        currentSelectedPlantID = plantID;
        DisplayPlantDetails(plantID);
    }

    void DisplayPlantDetails(string plantID)
    {
        // Try to get herb data from inventory (works in any scene!)
        InventoryItemData inventoryItem = InventorySystem.Instance?.GetItemData(plantID);
        
        if (inventoryItem != null && inventoryItem.herbData != null)
        {
            // Use cached herb data from inventory
            PlantDataSO herbData = inventoryItem.herbData;
            
            if (herbData.journalEntry != null)
            {
                DisplayJournalPlantDetails(herbData.journalEntry);
                
                // Add medicinal uses from PlantDataSO
                if (usesText != null && herbData.uses != null && herbData.uses.Length > 0)
                {
                    string usesString = "Medicinal Uses: ";
                    for (int i = 0; i < herbData.uses.Length; i++)
                    {
                        usesString += herbData.uses[i].ToString();
                        if (i < herbData.uses.Length - 1)
                            usesString += ", ";
                    }
                    usesText.text = usesString;
                    usesText.gameObject.SetActive(true);
                }
            }
            else
            {
                DisplayHerbDetails(herbData);
            }
            return;
        }

        // Try to get data from ShopSystem (for shop-bought seeds)
        ShopItemData shopData = GetShopItemData(plantID);
        
        if (shopData != null)
        {
            DisplayShopItemDetails(shopData);
            return;
        }

        // Fallback to JournalPlantData (for wild-collected plants)
        if (JournalDatabase.Instance != null)
        {
            JournalPlantData journalData = JournalDatabase.Instance.GetPlantData(plantID);
            if (journalData != null)
            {
                DisplayJournalPlantDetails(journalData);
                return;
            }
        }

        // No data found
        Debug.LogWarning($"[Journal] No data found for '{plantID}'");
        ShowNoSelection();
    }

    // Remove GetHerbData() - no longer needed!

    ShopItemData GetShopItemData(string plantID)
    {
        if (ShopSystem.Instance == null) return null;

        foreach (var item in ShopSystem.Instance.shopItems)
        {
            if (item != null && item.plantID == plantID)
            {
                return item;
            }
        }
        return null;
    }

    void DisplayShopItemDetails(ShopItemData data)
    {
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        // Use icon for shop items (no separate photo)
        if (plantPhoto != null)
        {
            plantPhoto.sprite = data.icon;
            plantPhoto.gameObject.SetActive(data.icon != null);
        }

        if (plantNameText != null)
        {
            plantNameText.text = data.itemName;
            plantNameText.gameObject.SetActive(true);
        }

        if (plantDescriptionText != null)
        {
            plantDescriptionText.text = data.GetJournalDescription();
            plantDescriptionText.gameObject.SetActive(true);
        }

        if (scientificNameText != null)
        {
            if (!string.IsNullOrEmpty(data.scientificName))
            {
                scientificNameText.text = $"<i>{data.scientificName}</i>";
                scientificNameText.gameObject.SetActive(true);
            }
            else
            {
                scientificNameText.gameObject.SetActive(false);
            }
        }

        // Habitat not available in ShopItemData
        if (habitatText != null)
        {
            habitatText.gameObject.SetActive(false);
        }

        if (usesText != null)
        {
            if (!string.IsNullOrEmpty(data.uses))
            {
                usesText.text = data.uses;
                usesText.gameObject.SetActive(true);
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }

        if (showDebugLogs) Debug.Log($"[Journal] Displayed shop item details for {data.itemName}");
    }

    void DisplayHerbDetails(PlantDataSO herbData)
    {
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        // Use icon for herbs
        if (plantPhoto != null)
        {
            plantPhoto.sprite = herbData.icon;
            plantPhoto.gameObject.SetActive(herbData.icon != null);
        }

        if (plantNameText != null)
        {
            plantNameText.text = herbData.plantName;
            plantNameText.gameObject.SetActive(true);
        }

        if (plantDescriptionText != null)
        {
            // For now, herbs don't have descriptions in PlantDataSO
            // You can add this field later if needed
            plantDescriptionText.text = $"A {herbData.rarity.ToString().ToLower()} herb.";
            plantDescriptionText.gameObject.SetActive(true);
        }

        if (scientificNameText != null)
        {
            scientificNameText.gameObject.SetActive(false);
        }

        if (habitatText != null)
        {
            habitatText.gameObject.SetActive(false);
        }

        if (usesText != null)
        {
            if (herbData.uses != null && herbData.uses.Length > 0)
            {
                string usesString = "Uses: ";
                for (int i = 0; i < herbData.uses.Length; i++)
                {
                    usesString += herbData.uses[i].ToString();
                    if (i < herbData.uses.Length - 1)
                        usesString += ", ";
                }
                usesText.text = usesString;
                usesText.gameObject.SetActive(true);
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }

        if (showDebugLogs) Debug.Log($"[Journal] Displayed herb details for {herbData.plantName}");
    }

    void DisplayJournalPlantDetails(JournalPlantData plantData)
    {
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        if (plantPhoto != null)
        {
            plantPhoto.sprite = plantData.plantPhoto;
            plantPhoto.gameObject.SetActive(plantData.plantPhoto != null);
        }

        if (plantNameText != null)
        {
            plantNameText.text = plantData.displayName;
            plantNameText.gameObject.SetActive(true);
        }

        if (plantDescriptionText != null)
        {
            plantDescriptionText.text = plantData.description;
            plantDescriptionText.gameObject.SetActive(true);
        }

        if (scientificNameText != null)
        {
            if (!string.IsNullOrEmpty(plantData.scientificName))
            {
                scientificNameText.text = $"<i>{plantData.scientificName}</i>";
                scientificNameText.gameObject.SetActive(true);
            }
            else
            {
                scientificNameText.gameObject.SetActive(false);
            }
        }

        if (habitatText != null)
        {
            if (!string.IsNullOrEmpty(plantData.habitat))
            {
                habitatText.text = $"Habitat: {plantData.habitat}";
                habitatText.gameObject.SetActive(true);
            }
            else
            {
                habitatText.gameObject.SetActive(false);
            }
        }

        if (usesText != null)
        {
            if (!string.IsNullOrEmpty(plantData.uses))
            {
                usesText.text = plantData.uses;
                usesText.gameObject.SetActive(true);
            }
            else
            {
                usesText.gameObject.SetActive(false);
            }
        }

        if (showDebugLogs) Debug.Log($"[Journal] Displayed details for {plantData.displayName}");
    }

    void ShowNoSelection()
    {
        if (plantPhoto != null)
            plantPhoto.gameObject.SetActive(false);

        if (plantNameText != null)
        {
            plantNameText.text = "";
            plantNameText.gameObject.SetActive(false);
        }

        if (plantDescriptionText != null)
        {
            plantDescriptionText.text = "";
            plantDescriptionText.gameObject.SetActive(false);
        }

        if (scientificNameText != null)
            scientificNameText.gameObject.SetActive(false);

        if (habitatText != null)
            habitatText.gameObject.SetActive(false);

        if (usesText != null)
            usesText.gameObject.SetActive(false);

        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(true);
    }

    void UpdateCurrencyDisplay()
    {
        if (currencyText != null)
        {
            currencyText.text = $"${playerCurrency}";
        }
    }

    public void AddCurrency(int amount)
    {
        playerCurrency += amount;
        UpdateCurrencyDisplay();
        
        if (showDebugLogs) Debug.Log($"[Journal] Added ${amount}. Total: ${playerCurrency}");
    }

    public bool RemoveCurrency(int amount)
    {
        if (playerCurrency >= amount)
        {
            playerCurrency -= amount;
            UpdateCurrencyDisplay();
            
            if (showDebugLogs) Debug.Log($"[Journal] Spent ${amount}. Remaining: ${playerCurrency}");
            return true;
        }
        
        if (showDebugLogs) Debug.Log($"[Journal] Not enough currency. Need ${amount}, have ${playerCurrency}");
        return false;
    }

    public int GetCurrency() => playerCurrency;

    void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshPlantList;
        }
    }
}