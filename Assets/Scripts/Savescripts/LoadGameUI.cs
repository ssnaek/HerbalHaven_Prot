using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for Load Game screen.
/// Displays list of available saves with info.
/// </summary>
public class LoadGameUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadGamePanel;
    public Transform saveListContainer;
    public GameObject saveSlotPrefab;
    public Button backButton;
    public TextMeshProUGUI noSavesText;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<GameObject> saveSlotObjects = new List<GameObject>();

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Hide panel initially
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
    }

    /// <summary>
    /// Show Load Game panel and refresh save list.
    /// </summary>
    public void ShowLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(true);

        RefreshSaveList();

        if (showDebugLogs) Debug.Log("[LoadGameUI] Load game panel shown");
    }

    /// <summary>
    /// Hide Load Game panel.
    /// </summary>
    public void HideLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);

        if (showDebugLogs) Debug.Log("[LoadGameUI] Load game panel hidden");
    }

    /// <summary>
    /// Refresh the list of save files.
    /// </summary>
    public void RefreshSaveList()
    {
        // Clear existing slots
        ClearSaveSlots();

        if (SaveLoadManager.Instance == null)
        {
            Debug.LogError("[LoadGameUI] SaveLoadManager not found!");
            return;
        }

        // Get all saves
        List<SaveData> saves = SaveLoadManager.Instance.GetAllSaves();

        if (saves.Count == 0)
        {
            // No saves found
            if (noSavesText != null)
            {
                noSavesText.gameObject.SetActive(true);
                noSavesText.text = "No save files found.\nStart a new game!";
            }

            if (showDebugLogs) Debug.Log("[LoadGameUI] No save files found");
            return;
        }

        // Hide "no saves" text
        if (noSavesText != null)
            noSavesText.gameObject.SetActive(false);

        // Create a slot for each save
        foreach (SaveData save in saves)
        {
            CreateSaveSlot(save);
        }

        if (showDebugLogs) Debug.Log($"[LoadGameUI] Created {saves.Count} save slots");
    }

    void CreateSaveSlot(SaveData save)
    {
        if (saveSlotPrefab == null || saveListContainer == null)
        {
            Debug.LogError("[LoadGameUI] saveSlotPrefab or saveListContainer not assigned!");
            return;
        }

        GameObject slotObj = Instantiate(saveSlotPrefab, saveListContainer);
        saveSlotObjects.Add(slotObj);

        // Find UI elements (adjust names based on your prefab)
        TextMeshProUGUI nameText = slotObj.transform.Find("SaveName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI dayText = slotObj.transform.Find("DayText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI currencyText = slotObj.transform.Find("CurrencyText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI herbsText = slotObj.transform.Find("HerbsText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timestampText = slotObj.transform.Find("TimestampText")?.GetComponent<TextMeshProUGUI>();
        Button loadButton = slotObj.transform.Find("LoadButton")?.GetComponent<Button>();
        Button deleteButton = slotObj.transform.Find("DeleteButton")?.GetComponent<Button>();

        // Set text
        if (nameText != null)
            nameText.text = save.saveName;

        if (dayText != null)
            dayText.text = $"Day {save.dayNumber}";

        if (currencyText != null)
            currencyText.text = $"${save.playerCurrency}";

        if (herbsText != null)
            herbsText.text = $"{save.totalHerbsCollected} herbs";

        if (timestampText != null)
        {
            // Format timestamp nicely
            if (System.DateTime.TryParse(save.lastSavedTimestamp, out System.DateTime dt))
            {
                timestampText.text = dt.ToString("MMM dd, yyyy h:mm tt");
            }
            else
            {
                timestampText.text = save.lastSavedTimestamp;
            }
        }

        // Wire up buttons
        if (loadButton != null)
        {
            string saveID = save.saveID; // Capture for lambda
            loadButton.onClick.AddListener(() => OnLoadButtonClicked(saveID));
        }

        if (deleteButton != null)
        {
            string saveID = save.saveID; // Capture for lambda
            deleteButton.onClick.AddListener(() => OnDeleteButtonClicked(saveID, slotObj));
        }
    }

    void ClearSaveSlots()
    {
        foreach (GameObject slot in saveSlotObjects)
        {
            Destroy(slot);
        }
        saveSlotObjects.Clear();
    }

    void OnLoadButtonClicked(string saveID)
    {
        if (showDebugLogs) Debug.Log($"[LoadGameUI] Loading save: {saveID}");

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.LoadSave(saveID);
            HideLoadGamePanel();
        }
        else
        {
            Debug.LogError("[LoadGameUI] SaveLoadManager not found!");
        }
    }

    void OnDeleteButtonClicked(string saveID, GameObject slotObj)
    {
        if (showDebugLogs) Debug.Log($"[LoadGameUI] Deleting save: {saveID}");

        if (SaveLoadManager.Instance != null)
        {
            bool success = SaveLoadManager.Instance.DeleteSave(saveID);
            
            if (success)
            {
                // Remove slot from UI
                saveSlotObjects.Remove(slotObj);
                Destroy(slotObj);

                // Check if no saves left
                if (saveSlotObjects.Count == 0 && noSavesText != null)
                {
                    noSavesText.gameObject.SetActive(true);
                    noSavesText.text = "No save files found.\nStart a new game!";
                }
            }
        }
        else
        {
            Debug.LogError("[LoadGameUI] SaveLoadManager not found!");
        }
    }

    void OnBackClicked()
    {
        HideLoadGamePanel();

        // Return to main menu
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            controller.BackToMainMenu();
        }
    }
}