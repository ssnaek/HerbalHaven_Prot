/*using System.Collections.Generic;
using UnityEngine;

public class JournalSystem : MonoBehaviour
{
    [System.Serializable]
    public class JournalEntry
    {
        public string title;
        public string description;
        public bool completed;
    }

    public List<JournalEntry> entries = new List<JournalEntry>();
    private bool isJournalOpen = false;

    // Herb detection/collection
    public float herbDetectRadius = 2.0f;
    public LayerMask herbLayerMask;
    public string herbTag = "Herb";
    public string collectKey = "e";
    private GameObject currentHerbTarget;
    private string currentHerbName;
    private InventorySystem inventory;

	[System.Serializable]
	public class PlantRecord
	{
		public string name;
		public string treats; // illnesses treated
		public string remedy; // crafting/prep method
		public string growTips; // how to grow/cultivate
		public bool discovered;
		// public float appearanceChance; // TODO: placeholder for future tuning // no appearance chance yet so nono
	}

	[System.Serializable]
	public class SicknessRecord
	{
		public string name;
		public string description;
		public string recommendedPlants; // comma separated plant names
		public bool discovered;
		// public float appearanceChance // TODO: placeholder for future tuning // no appearance chance yet so nono
	}

	public List<PlantRecord> plants = new List<PlantRecord>();
	public List<SicknessRecord> sicknesses = new List<SicknessRecord>();

    void Awake()
    {
        inventory = FindObjectOfType<InventorySystem>();
		EnsureStaticData();
    }

    public string openJournalKey = "j";

    void Update()
    {
        if (Input.GetKeyDown(openJournalKey))
        {
            ToggleJournal();
        }

        DetectNearbyHerb();
        if (currentHerbTarget != null && Input.GetKeyDown(collectKey))
        {
            CollectCurrentHerb();
        }
    }

    public void ToggleJournal()
    {
        isJournalOpen = !isJournalOpen;
        Cursor.lockState = isJournalOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isJournalOpen;
        Debug.Log(isJournalOpen ? "Journal Opened" : "Journal Closed");
        // PLACEHOLDER: Enable/disable journal UI canvas here
    }

    public void AddEntry(string title, string description)
    {
        JournalEntry newEntry = new JournalEntry
        {
            title = title,
            description = description,
            completed = false
        };
        entries.Add(newEntry);
        Debug.Log($"Added journal entry: {title}");
    }

    public void CompleteEntry(string title)
    {
        JournalEntry entry = entries.Find(e => e.title == title);
        if (entry != null)
        {
            entry.completed = true;
            Debug.Log($"Journal entry completed: {title}");
        }
    }

    public void RemoveEntry(string title)
    {
        JournalEntry entry = entries.Find(e => e.title == title);
        if (entry != null)
        {
            entries.Remove(entry);
            Debug.Log($"Removed journal entry: {title}");
        }
    }

    //  Herb proximity and UI (when player is near a herb, it will be detected and the UI will be displayed) idk if this is relevant since i saw it on the doc
    void DetectNearbyHerb()
    {
        currentHerbTarget = null;
        currentHerbName = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, herbDetectRadius, herbLayerMask.value == 0 ? ~0 : herbLayerMask);
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (!string.IsNullOrEmpty(herbTag) && !go.CompareTag(herbTag))
                continue;
            float d = Vector3.SqrMagnitude(go.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                currentHerbTarget = go;
            }
        }

        if (currentHerbTarget != null)
        {
            currentHerbName = currentHerbTarget.name;
        }
    }

    void CollectCurrentHerb()
    {
        if (currentHerbTarget == null) return;
        string herbName = string.IsNullOrEmpty(currentHerbName) ? currentHerbTarget.name : currentHerbName;

        if (inventory == null)
            inventory = FindObjectOfType<InventorySystem>();
        if (inventory != null)
        {
            inventory.AddItem(herbName, 1);
        }

        JournalEntry entry = entries.Find(e => e.title == herbName);
        if (entry == null)
        {
            AddEntry(herbName, "");
            entry = entries.Find(e => e.title == herbName);
        }
        if (entry != null)
        {
            entry.completed = true;
            Debug.Log($"Unlocked journal entry for herb: {herbName}");
        }

		UnlockPlant(herbName);

        Destroy(currentHerbTarget);
        currentHerbTarget = null;
        currentHerbName = null;
    }

    void OnGUI()
    {
        if (currentHerbTarget != null && !string.IsNullOrEmpty(currentHerbName))
        {
            string text = $"{currentHerbName}\nPress {collectKey} to collect";
            Vector2 size = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 14 }.CalcSize(new GUIContent(text));
            Rect r = new Rect((Screen.width - size.x) / 2, Screen.height * 0.7f, size.x + 20, 50);
            GUI.Box(r, text);
        }

		if (isJournalOpen)
		{
			DrawJournalUI();
		}
    }

	// Static dataset & UI (not sure if grow tips is needed but added it for now)
	void EnsureStaticData()
	{
		if (plants.Count == 0)
		{
			plants.AddRange(new PlantRecord[] {
				new PlantRecord { name = "Lagundi", treats = "Cough, colds, asthma", remedy = "Boiled decoction / Herbal tea", growTips = "Full sun; well-drained soil; regular pruning." },
				new PlantRecord { name = "Ginger", treats = "Cough, sore throat, nausea", remedy = "Tea (salabat) crushed root", growTips = "Partial shade; moist soil; plant rhizomes shallow." },
				new PlantRecord { name = "Bayabas", treats = "Diarrhea, wounds, gum problems", remedy = "Leaf rinse / wound wash", growTips = "Full sun; moderate watering; prune to shape." },
				new PlantRecord { name = "Oregano", treats = "Cough, asthma, sore throat", remedy = "Steam inhalation / tea", growTips = "Full sun; light watering; avoid waterlogging." },
				new PlantRecord { name = "Garlic", treats = "High blood pressure", remedy = "Infusion", growTips = "Full sun; loose soil; plant cloves; keep soil slightly dry." },
				new PlantRecord { name = "Tanglad", treats = "Stomachache, fever", remedy = "Infusion", growTips = "Full sun; plenty water; divide clumps to propagate." },
				new PlantRecord { name = "Yerba Buena", treats = "Headache, body pain", remedy = "Infusion", growTips = "Partial shade; moist soil; trim to encourage growth." },
				new PlantRecord { name = "Ampalaya", treats = "Cough, diabetes", remedy = "Tea / juice / poultice", growTips = "Full sun; trellis support; regular harvest of leaves/fruit." },
				new PlantRecord { name = "Kamias", treats = "Cough, inflammation", remedy = "Crushed fruit / juice / gargle", growTips = "Full sun; well-drained soil; protect from frost." },
				new PlantRecord { name = "Malunggay", treats = "Wounds, eye pain", remedy = "Infusion", growTips = "Full sun; drought tolerant; prune to maintain height." },
				new PlantRecord { name = "Banaba", treats = "Kidney issues", remedy = "Boiled tea (infusion)", growTips = "Full sun; deep soil; space for large tree." },
				new PlantRecord { name = "Tawa-tawa", treats = "Inflammation", remedy = "Tea", growTips = "Full sun; light watering; common weed, easy to grow." },
				new PlantRecord { name = "Damong maria (Mugwort)", treats = "Menstrual cramps", remedy = "Infusion", growTips = "Full sun; well-drained soil; can be invasive—contain roots." },
			});
		}

		if (sicknesses.Count == 0)
		{
			sicknesses.AddRange(new SicknessRecord[] {
				new SicknessRecord { name = "Cough", description = "Persistent cough; respiratory irritation.", recommendedPlants = "Lagundi, Oregano, Ginger, Ampalaya, Kamias" },
				new SicknessRecord { name = "Colds", description = "Common cold; congestion, mild fever.", recommendedPlants = "Lagundi" },
				new SicknessRecord { name = "Asthma", description = "Bronchial constriction; wheezing.", recommendedPlants = "Lagundi, Oregano" },
				new SicknessRecord { name = "Sore throat", description = "Throat pain and irritation.", recommendedPlants = "Ginger, Oregano" },
				new SicknessRecord { name = "Nausea", description = "Stomach unease; urge to vomit.", recommendedPlants = "Ginger" },
				new SicknessRecord { name = "Diarrhea", description = "Loose, watery stools.", recommendedPlants = "Bayabas" },
				new SicknessRecord { name = "Wounds", description = "Cuts and abrasions.", recommendedPlants = "Bayabas, Malunggay" },
				new SicknessRecord { name = "Gum problems", description = "Gingival irritation or infection.", recommendedPlants = "Bayabas" },
				new SicknessRecord { name = "High blood pressure", description = "Elevated arterial blood pressure.", recommendedPlants = "Garlic" },
				new SicknessRecord { name = "Stomachache", description = "Abdominal pain.", recommendedPlants = "Tanglad" },
				new SicknessRecord { name = "Fever", description = "Elevated body temperature.", recommendedPlants = "Tanglad" },
				new SicknessRecord { name = "Headache", description = "Head pain.", recommendedPlants = "Yerba Buena" },
				new SicknessRecord { name = "Body pain", description = "Generalized body aches.", recommendedPlants = "Yerba Buena" },
				new SicknessRecord { name = "Diabetes", description = "High blood glucose levels.", recommendedPlants = "Ampalaya" },
				new SicknessRecord { name = "Inflammation", description = "Local swelling/redness.", recommendedPlants = "Kamias, Tawa-tawa" },
				new SicknessRecord { name = "Eye pain", description = "Eye discomfort/irritation.", recommendedPlants = "Malunggay" },
				new SicknessRecord { name = "Kidney issues", description = "Urinary complications.", recommendedPlants = "Banaba" },
				new SicknessRecord { name = "Menstrual cramps", description = "Painful menstruation.", recommendedPlants = "Damong maria (Mugwort)" },
			});
		}
	}

	public void UnlockPlant(string plantName)
	{
		var rec = plants.Find(p => p.name == plantName);
		if (rec != null)
		{
			rec.discovered = true;
		}
	}

	public void UnlockSickness(string sicknessName)
	{
		var rec = sicknesses.Find(s => s.name == sicknessName);
		if (rec != null)
		{
			rec.discovered = true;
		}
	}

	void DrawJournalUI()
	{
		int left = 20;
		int top = 60;
		int width = Screen.width - 40;
		int line = 0;
		GUI.Label(new Rect(left, 20, width, 24), "Journal — Plants & Sicknesses (discovered only)");

		GUI.Label(new Rect(left, top + 24 * line++, width, 22), "Plants:");
		for (int i = 0; i < plants.Count; i++)
		{
			var p = plants[i];
			if (!p.discovered) continue;
			GUI.Box(new Rect(left, top + 24 * line, width, 70), "");
			GUI.Label(new Rect(left + 8, top + 24 * line + 4, width - 16, 18), p.name);
			GUI.Label(new Rect(left + 8, top + 24 * line + 22, width - 16, 18), $"Treats: {p.treats}");
			GUI.Label(new Rect(left + 8, top + 24 * line + 40, width - 16, 18), $"Remedy: {p.remedy}");
			// Chance to appear: placeholder - see PlantRecord.appearanceChance
			line += 3;
		}

		line += 1;
		GUI.Label(new Rect(left, top + 24 * line++, width, 22), "Sicknesses:");
		for (int i = 0; i < sicknesses.Count; i++)
		{
			var s = sicknesses[i];
			if (!s.discovered) continue;
			GUI.Box(new Rect(left, top + 24 * line, width, 70), "");
			GUI.Label(new Rect(left + 8, top + 24 * line + 4, width - 16, 18), s.name);
			GUI.Label(new Rect(left + 8, top + 24 * line + 22, width - 16, 18), s.description);
			GUI.Label(new Rect(left + 8, top + 24 * line + 40, width - 16, 18), $"Use: {s.recommendedPlants}");
			// Chance to appear: placeholder — see SicknessRecord.appearanceChance
			line += 3;
		}
	}
}
*/