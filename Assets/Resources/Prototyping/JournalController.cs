using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main journal controller. Handles opening/closing and coordinating between list and detail view.
/// </summary>
public class JournalController : MonoBehaviour
{
    [Header("Journal Panel")]
    [Tooltip("The entire journal UI (both pages)")]
    public GameObject journalPanel;

    [Header("Left Page - Plant Details")]
    public Image plantPhoto;
    public TextMeshProUGUI plantNameText;
    public TextMeshProUGUI plantDescriptionText;
    public TextMeshProUGUI scientificNameText;
    public TextMeshProUGUI habitatText;
    public TextMeshProUGUI usesText;
    public GameObject noSelectionMessage; // "Select a plant to view details"

    [Header("Right Page - Plant List")]
    public Transform plantListContainer;
    public GameObject plantListItemPrefab;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.J;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<GameObject> listItems = new List<GameObject>();
    private string currentSelectedPlantID = null;

    void Start()
    {
        // Subscribe to inventory changes
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback += RefreshPlantList;
        }

        // Start closed
        journalPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleJournal();
        }
    }

    void ToggleJournal()
    {
        bool newState = !journalPanel.activeSelf;
        journalPanel.SetActive(newState);

        if (newState)
        {
            RefreshPlantList();
            
            // Show placeholder on left page if nothing selected
            if (currentSelectedPlantID == null)
            {
                ShowNoSelection();
            }
        }

        if (showDebugLogs) Debug.Log($"[Journal] Toggled {(newState ? "OPEN" : "CLOSED")}");
    }

    public void RefreshPlantList()
    {
        if (showDebugLogs) Debug.Log("[Journal] Refreshing plant list");

        // Clear existing items
        ClearList();

        if (InventorySystem.Instance == null) return;

        // Get all collected plants
        List<InventoryItemData> plants = InventorySystem.Instance.GetAllItems();

        // Create list item for each plant
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

        // Set up the visual components (same as inventory)
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

        // Add button functionality
        Button button = item.GetComponent<Button>();
        if (button == null)
        {
            button = item.AddComponent<Button>();
        }

        // Capture plantID for the button click
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
        if (JournalDatabase.Instance == null)
        {
            Debug.LogError("[Journal] JournalDatabase.Instance is null!");
            ShowNoSelection();
            return;
        }

        JournalPlantData plantData = JournalDatabase.Instance.GetPlantData(plantID);

        if (plantData == null)
        {
            Debug.LogWarning($"[Journal] No journal data found for '{plantID}'");
            ShowNoSelection();
            return;
        }

        // Hide "no selection" message
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(false);

        // Show plant details
        if (plantPhoto != null)
        {
            plantPhoto.sprite = plantData.plantPhoto;
            plantPhoto.gameObject.SetActive(plantData.plantPhoto != null);
        }

        if (plantNameText != null)
            plantNameText.text = plantData.displayName;

        if (plantDescriptionText != null)
            plantDescriptionText.text = plantData.description;

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
        // Hide all detail elements
        if (plantPhoto != null)
            plantPhoto.gameObject.SetActive(false);

        if (plantNameText != null)
            plantNameText.text = "";

        if (plantDescriptionText != null)
            plantDescriptionText.text = "";

        if (scientificNameText != null)
            scientificNameText.gameObject.SetActive(false);

        if (habitatText != null)
            habitatText.gameObject.SetActive(false);

        if (usesText != null)
            usesText.gameObject.SetActive(false);

        // Show placeholder message
        if (noSelectionMessage != null)
            noSelectionMessage.SetActive(true);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.onInventoryChangedCallback -= RefreshPlantList;
        }
    }
}