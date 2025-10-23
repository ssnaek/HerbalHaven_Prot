using UnityEngine;

/// <summary>
/// ScriptableObject for plant data.
/// Create one asset per plant type - much easier than manual entry!
/// </summary>
[CreateAssetMenu(fileName = "New Plant", menuName = "Plants/Plant Data")]
public class PlantDataSO : ScriptableObject
{
    [Header("Identity")]
    public string plantName;
    public string plantID;
    
    [Header("Visuals")]
    public Sprite icon;
    public GameObject plantPrefab;
    
    [Header("Harvest")]
    public int yieldPerPick = 1;
    public int maxStack = 99;
    
    [Header("Journal Entry")]
    [Tooltip("Link to the journal entry asset for this herb")]
    public JournalPlantData journalEntry;
    
    [Header("Rarity")]
    public PlantRarity rarity = PlantRarity.Common;
    
    [Header("Herbal Properties")]
    [Tooltip("What this herb can be used for (1-3 properties)")]
    public HerbUse[] uses = new HerbUse[0];
    
    public enum PlantRarity
    {
        Common,     // 70% chance
        Uncommon,   // 25% chance
        Rare        // 5% chance
    }
    
    public enum HerbUse
    {
        Healing,        // Treats wounds, injuries
        Calming,        // Reduces stress, anxiety
        Energizing,     // Boosts energy, stamina
        Digestive,      // Aids digestion
        AntiInflammatory, // Reduces inflammation
        Fever,          // Treats fevers
        Pain,           // Pain relief
        Respiratory,    // Helps breathing, coughs
        Immune,         // Boosts immune system
        Sleep           // Aids sleep
    }
}