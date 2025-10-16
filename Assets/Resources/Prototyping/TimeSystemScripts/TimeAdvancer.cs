using UnityEngine;

/// <summary>
/// Optional component that advances time when an object is interacted with.
/// Attach this to your prefab or GameObject to make interactions take time.
/// Works automatically with PlayerInteraction system.
/// </summary>
public class TimeAdvancer : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("How many minutes this interaction advances time by")]
    public int minutesToAdvance = 30;

    [Header("Optional Feedback")]
    [Tooltip("Message to display when time advances (leave empty for no message)")]
    public string timeAdvanceMessage = "";

    [Header("Debug")]
    public bool showDebugLogs = false;

    /// <summary>
    /// Call this method when an interaction occurs.
    /// PlayerInteraction automatically calls this if the component exists.
    /// </summary>
    public void AdvanceTime()
    {
        if (minutesToAdvance <= 0)
        {
            if (showDebugLogs) Debug.Log($"[TimeAdvancer] minutesToAdvance is 0 or negative, skipping");
            return;
        }

        if (TimeSystem.Instance != null)
        {
            if (showDebugLogs) Debug.Log($"[TimeAdvancer '{gameObject.name}'] Advancing time by {minutesToAdvance} minutes");
            
            TimeSystem.Instance.AdvanceTime(minutesToAdvance);

            // Optional message display
            if (!string.IsNullOrEmpty(timeAdvanceMessage))
            {
                Debug.Log(timeAdvanceMessage);
                // TODO: Display in UI when you have a message system
            }
        }
        else
        {
            Debug.LogWarning("[TimeAdvancer] TimeSystem.Instance is null! Make sure TimeSystem exists in the scene.");
        }
    }

    public int GetMinutesToAdvance() => minutesToAdvance;
}