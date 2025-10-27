using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Yarn.Unity;

/// <summary>
/// Day-based time system for plant collection phase.
/// Time advances through interactions via TimeAdvancer components.
/// Tracks day counter and total harvests for plant regeneration.
/// Now triggers YarnSpinner dialogue at day end before transitioning.
/// </summary>
public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("Starting time in minutes (360 = 6:00 AM)")]
    public int startTimeMinutes = 360; // 6:00 AM
    [Tooltip("Time limit in minutes (600 = 10:00 AM)")]
    public int timeLimitMinutes = 600; // 10:00 AM
    [Tooltip("Use 24-hour format (false = 12-hour with AM/PM)")]
    public bool use24HourFormat = false;

    [Header("UI")]
    [Tooltip("TextMeshPro component to display time")]
    public TextMeshProUGUI timeText;
    [Tooltip("TextMeshPro component to display day counter")]
    public TextMeshProUGUI dayText;

    [Header("Day System")]
    [Tooltip("Scene to load when day ends (empty = reload current scene)")]
    public string nextSceneName = "";
    
    [Header("Day End Dialogue")]
    [Tooltip("Yarn node name to play at day end (leave empty to skip dialogue)")]
    public string dayEndDialogueNode = "DayEndDialogue";
    
    [Tooltip("Wait for dialogue to complete before transitioning")]
    public bool waitForDialogue = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private int currentDay = 1;
    private int currentTimeMinutes;
    private int totalHarvestsToday = 0;
    private bool hasTransitioned = false;
    private DialogueRunner dialogueRunner;

    // Events
    public delegate void OnTimeChanged(int minutes);
    public event OnTimeChanged onTimeChangedCallback;

    public delegate void OnDayEnd(int day, int harvests);
    public event OnDayEnd onDayEndCallback;

    public delegate void OnNewDay(int day);
    public event OnNewDay onNewDayCallback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load persistent day counter
            currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentTimeMinutes = startTimeMinutes;
    }

    void OnEnable()
    {
        // Subscribe to scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-find UI references after scene reload
        RefindUIReferences();
        UpdateTimeDisplay();
        UpdateDayDisplay();
        
        // Re-find DialogueRunner
        dialogueRunner = FindObjectOfType<DialogueRunner>();
    }

    void Start()
    {
        RefindUIReferences();
        UpdateTimeDisplay();
        UpdateDayDisplay();
        
        // Find DialogueRunner
        dialogueRunner = FindObjectOfType<DialogueRunner>();
        
        // Notify listeners that a new day has started
        onNewDayCallback?.Invoke(currentDay);

        if (showDebugLogs) Debug.Log($"[TimeSystem] Day {currentDay} started at {GetTimeString(currentTimeMinutes)}");
    }

    void RefindUIReferences()
    {
        // Find Time text by name
        if (timeText == null)
        {
            GameObject timeObj = GameObject.Find("Time");
            if (timeObj != null)
                timeText = timeObj.GetComponent<TextMeshProUGUI>();
        }

        // Find Day text by name
        if (dayText == null)
        {
            GameObject dayObj = GameObject.Find("Day");
            if (dayObj != null)
                dayText = dayObj.GetComponent<TextMeshProUGUI>();
        }

        if (showDebugLogs && (timeText == null || dayText == null))
        {
            Debug.LogWarning("[TimeSystem] Could not find UI text references. Make sure GameObjects are named 'Time' and 'Day'");
        }
    }

    /// <summary>
    /// Advance time by specified minutes.
    /// Called by TimeAdvancer components on interactions.
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        if (minutes <= 0) return;

        if (showDebugLogs) Debug.Log($"⏰ [TimeSystem] Advancing time by {minutes} minutes");

        currentTimeMinutes += minutes;

        if (showDebugLogs) Debug.Log($"⏰ [TimeSystem] Current time: {GetTimeString(currentTimeMinutes)}");

        UpdateTimeDisplay();
        onTimeChangedCallback?.Invoke(currentTimeMinutes);

        // Check if time limit reached
        if (currentTimeMinutes >= timeLimitMinutes && !hasTransitioned)
        {
            if (showDebugLogs)
            {
                Debug.Log($"⏰ [TimeSystem] Time limit reached! ({GetTimeString(currentTimeMinutes)} >= {GetTimeString(timeLimitMinutes)})");
            }
            EndDay();
        }
    }

    /// <summary>
    /// Track a harvest (called by plant collection system).
    /// TimeAdvancer handles the time advancement, this just tracks the count.
    /// </summary>
    public void RegisterHarvest()
    {
        totalHarvestsToday++;
        if (showDebugLogs) Debug.Log($"🌿 [TimeSystem] Harvest registered. Total today: {totalHarvestsToday}");
    }

    void EndDay()
    {
        if (hasTransitioned) return;
        hasTransitioned = true;

        if (showDebugLogs) Debug.Log($"🌙 [TimeSystem] Day {currentDay} ended. Total harvests: {totalHarvestsToday}");

        // Save harvest count for plant regeneration
        PlayerPrefs.SetInt("PreviousDayHarvests", totalHarvestsToday);
        PlayerPrefs.Save();

        // Notify listeners before transitioning
        onDayEndCallback?.Invoke(currentDay, totalHarvestsToday);

        // Play day-end dialogue if configured
        if (!string.IsNullOrEmpty(dayEndDialogueNode) && dialogueRunner != null)
        {
            if (showDebugLogs) Debug.Log($"[TimeSystem] Starting day-end dialogue: {dayEndDialogueNode}");
            
            if (waitForDialogue)
            {
                // Subscribe to dialogue complete event
                dialogueRunner.onDialogueComplete.AddListener(OnDayEndDialogueComplete);
            }
            
            dialogueRunner.StartDialogue(dayEndDialogueNode);
            
            if (!waitForDialogue)
            {
                // Don't wait, transition immediately after starting dialogue
                TransitionToNextDay();
            }
        }
        else
        {
            // No dialogue, transition immediately
            TransitionToNextDay();
        }
    }

    void OnDayEndDialogueComplete()
    {
        if (showDebugLogs) Debug.Log("[TimeSystem] Day-end dialogue complete, transitioning...");
        
        // Unsubscribe
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.RemoveListener(OnDayEndDialogueComplete);
        }
        
        TransitionToNextDay();
    }

    void TransitionToNextDay()
    {
        // Increment day counter and save
        currentDay++;
        PlayerPrefs.SetInt("CurrentDay", currentDay);
        PlayerPrefs.Save();

        // Start new day
        StartNewDay();
    }

    void StartNewDay()
    {
        if (showDebugLogs) Debug.Log($"🌅 [TimeSystem] Starting Day {currentDay}...");

        // Reset daily stats
        currentTimeMinutes = startTimeMinutes;
        totalHarvestsToday = 0;
        hasTransitioned = false;

        // Reload scene
        if (string.IsNullOrEmpty(nextSceneName))
        {
            // Reload current scene
            Scene currentScene = SceneManager.GetActiveScene();
            
            if (showDebugLogs) Debug.Log($"🔄 [TimeSystem] Reloading scene: {currentScene.name}");
            
            SceneManager.LoadScene(currentScene.name);
        }
        else
        {
            // Load specified scene
            if (showDebugLogs) Debug.Log($"🎬 [TimeSystem] Loading scene: {nextSceneName}");
            
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void UpdateTimeDisplay()
    {
        if (timeText == null) return;
        timeText.text = GetTimeString(currentTimeMinutes);
    }

    void UpdateDayDisplay()
    {
        if (dayText == null) return;
        dayText.text = $"Day {currentDay}";
    }

    /// <summary>
    /// Convert minutes to time string
    /// </summary>
    string GetTimeString(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (use24HourFormat)
        {
            return $"{hours:D2}:{minutes:D2}";
        }
        else
        {
            // 12-hour format
            string period = hours >= 12 ? "PM" : "AM";
            int displayHour = hours % 12;
            if (displayHour == 0) displayHour = 12;
            return $"{displayHour}:{minutes:D2} {period}";
        }
    }

    // ========== PUBLIC GETTERS ==========

    public int GetCurrentTimeMinutes() => currentTimeMinutes;
    
    public int GetRemainingMinutes() => Mathf.Max(0, timeLimitMinutes - currentTimeMinutes);

    public bool IsTimeUp() => currentTimeMinutes >= timeLimitMinutes;

    public int GetCurrentDay() => currentDay;

    public int GetTotalHarvestsToday() => totalHarvestsToday;

    /// <summary>
    /// Get total harvests from previous day (for plant regeneration)
    /// </summary>
    public int GetPreviousDayHarvests()
    {
        return PlayerPrefs.GetInt("PreviousDayHarvests", 0);
    }

    /// <summary>
    /// Manual reset for testing - resets day counter to 1
    /// </summary>
    public void ResetDay()
    {
        currentDay = 1;
        PlayerPrefs.SetInt("CurrentDay", 1);
        PlayerPrefs.SetInt("PreviousDayHarvests", 0);
        PlayerPrefs.Save();
        
        currentTimeMinutes = startTimeMinutes;
        totalHarvestsToday = 0;
        hasTransitioned = false;
        UpdateTimeDisplay();
        UpdateDayDisplay();
        
        if (showDebugLogs) Debug.Log($"[TimeSystem] Day counter reset to 1, time reset to {GetTimeString(startTimeMinutes)}");
    }

    /// <summary>
    /// Force day transition (for testing)
    /// </summary>
    public void ForceEndDay()
    {
        if (showDebugLogs) Debug.Log("[TimeSystem] Force ending day...");
        EndDay();
    }
}