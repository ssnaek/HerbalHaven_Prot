using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central database that links plantIDs to their journal entries.
/// Attach to a GameObject in your scene (like InventorySystem).
/// </summary>
public class JournalDatabase : MonoBehaviour
{
    public static JournalDatabase Instance { get; private set; }

    [Header("Plant Entries")]
    [Tooltip("Add all your JournalPlantData assets here")]
    public List<JournalPlantData> plantEntries = new List<JournalPlantData>();

    private Dictionary<string, JournalPlantData> plantDatabase;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        BuildDatabase();
    }

    void BuildDatabase()
    {
        plantDatabase = new Dictionary<string, JournalPlantData>();

        foreach (var entry in plantEntries)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.plantID))
            {
                Debug.LogWarning($"[JournalDatabase] Plant entry '{entry.name}' has no plantID!");
                continue;
            }

            if (plantDatabase.ContainsKey(entry.plantID))
            {
                Debug.LogWarning($"[JournalDatabase] Duplicate plantID '{entry.plantID}' found!");
                continue;
            }

            plantDatabase.Add(entry.plantID, entry);
        }

        Debug.Log($"[JournalDatabase] Loaded {plantDatabase.Count} plant entries");
    }

    /// <summary>
    /// Get journal data for a specific plant by ID
    /// </summary>
    public JournalPlantData GetPlantData(string plantID)
    {
        if (plantDatabase.ContainsKey(plantID))
        {
            return plantDatabase[plantID];
        }

        Debug.LogWarning($"[JournalDatabase] No journal entry found for plantID '{plantID}'");
        return null;
    }

    /// <summary>
    /// Check if journal data exists for a plant
    /// </summary>
    public bool HasPlantData(string plantID)
    {
        return plantDatabase.ContainsKey(plantID);
    }
}