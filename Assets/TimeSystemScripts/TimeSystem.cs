using UnityEngine;
using TMPro;

/// <summary>
/// Simple time system for plant collection phase.
/// Time advances through interactions, transitions to next scene at time limit.
/// </summary>
public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("Starting time in minutes (e.g., 360 = 6:00 AM)")]
    public int startTimeMinutes = 360; // 6:00 AM
    [Tooltip("Time limit in minutes (e.g., 600 = 10:00 AM)")]
    public int timeLimitMinutes = 600; // 10:00 AM
    [Tooltip("Use 24-hour format (false = 12-hour with AM/PM)")]
    public bool use24HourFormat = false;

    [Header("UI")]
    [Tooltip("TextMeshPro component to display time (top-left)")]
    public TextMeshProUGUI timeText;

    [Header("Scene Transition")]
    [Tooltip("Scene to load when time limit is reached")]
    public string nextSceneName = "ServingScene";

    [Header("Debug")]
    public bool showDebugLogs = false;

    private int currentTimeMinutes;
    private bool hasTransitioned = false;

    // Event for when time changes
    public delegate void OnTimeChanged(int minutes);
    public event OnTimeChanged onTimeChangedCallback;

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

        currentTimeMinutes = startTimeMinutes;
    }

    void Start()
    {
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Advance time by specified minutes
    /// </summary>
    public void AdvanceTime(int minutes)
    {
        if (minutes <= 0) return;

        if (showDebugLogs) Debug.Log($"⏰ [TimeSystem] Advancing time by {minutes} minutes");

        currentTimeMinutes += minutes;

        if (showDebugLogs) Debug.Log($"⏰ [TimeSystem] Current time: {GetTimeString(currentTimeMinutes)}");

        UpdateTimeDisplay();
        onTimeChangedCallback?.Invoke(currentTimeMinutes);

        // Check if time limit reached (always check, even if already transitioned)
        if (currentTimeMinutes >= timeLimitMinutes)
        {
            if (showDebugLogs)
            {
                if (hasTransitioned)
                {
                    Debug.LogWarning($"⚠️ [TimeSystem] Time limit exceeded ({GetTimeString(currentTimeMinutes)} >= {GetTimeString(timeLimitMinutes)}), retrying transition (previous attempt may have failed)");
                }
                else
                {
                    Debug.Log($"⏰ [TimeSystem] Time limit reached! ({GetTimeString(currentTimeMinutes)} >= {GetTimeString(timeLimitMinutes)})");
                }
            }
            TransitionToNextScene();
        }
    }

    void UpdateTimeDisplay()
    {
        if (timeText == null) return;
        timeText.text = GetTimeString(currentTimeMinutes);
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

    void TransitionToNextScene()
    {
        if (hasTransitioned)
        {
            if (showDebugLogs) Debug.Log("[TimeSystem] Transition already attempted, skipping duplicate");
            return;
        }
        
        hasTransitioned = true;

        Debug.Log($"⏰ [TimeSystem] Time limit reached! Transitioning to {nextSceneName}");

        // Load next scene
        // Uncomment when you have the serving scene ready:
        // UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);

        // For now, just log
        Debug.Log($"🎬 Scene transition disabled. Create '{nextSceneName}' scene and uncomment LoadScene() in TimeSystem.cs");
    }

    // ========== PUBLIC GETTERS ==========

    public int GetCurrentTimeMinutes() => currentTimeMinutes;
    
    public int GetRemainingMinutes() => Mathf.Max(0, timeLimitMinutes - currentTimeMinutes);

    public bool IsTimeUp() => currentTimeMinutes >= timeLimitMinutes;

    /// <summary>
    /// Reset time back to start (for testing or day resets)
    /// </summary>
    public void ResetTime()
    {
        currentTimeMinutes = startTimeMinutes;
        hasTransitioned = false;
        UpdateTimeDisplay();
        
        if (showDebugLogs) Debug.Log($"[TimeSystem] Time reset to {GetTimeString(startTimeMinutes)}");
    }
}