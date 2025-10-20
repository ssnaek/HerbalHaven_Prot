using UnityEngine;

/// <summary>
/// ScriptableObject that stores journal information for each plant type.
/// Create one for each plant and link it by matching the plantID.
/// </summary>
[CreateAssetMenu(fileName = "New Plant Journal Entry", menuName = "Journal/Plant Entry")]
public class JournalPlantData : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Must match the plantID from PlantNode")]
    public string plantID;
    
    [Header("Display Info")]
    public string displayName;
    public Sprite icon;
    
    [Header("Journal Content")]
    [Tooltip("Real-life reference image of the plant")]
    public Sprite plantPhoto;
    
    [TextArea(5, 10)]
    [Tooltip("Description shown in journal left page")]
    public string description;
    
    [Header("Optional Info")]
    public string scientificName;
    public string habitat;
    
    [TextArea(2, 5)]
    public string uses;
}