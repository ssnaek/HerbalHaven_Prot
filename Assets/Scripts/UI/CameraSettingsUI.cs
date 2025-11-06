using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls camera sensitivity UI in the Journal Settings panel.
/// Attach to your SettingsContent GameObject.
/// </summary>
public class CameraSettingsUI : MonoBehaviour
{
    [Header("Sensitivity Slider")]
    [Tooltip("Slider to adjust camera sensitivity (0.1 - 3.0)")]
    public Slider sensitivitySlider;
    
    [Tooltip("Text showing current sensitivity value")]
    public TextMeshProUGUI sensitivityValueText;
    
    [Tooltip("Optional label format (e.g., 'Sensitivity: {0:F1}x')")]
    public string valueFormat = "{0:F1}x";

    [Header("References")]
    [Tooltip("Player camera (auto-finds if empty)")]
    public PlayerCamera playerCamera;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Start()
    {
        // Auto-find PlayerCamera if not assigned
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<PlayerCamera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("[CameraSettingsUI] PlayerCamera not found in scene!");
            enabled = false;
            return;
        }

        // Setup slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 3.0f;
            sensitivitySlider.value = playerCamera.GetSensitivity();
            
            // Listen for slider changes
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        else
        {
            Debug.LogError("[CameraSettingsUI] Sensitivity slider not assigned!");
        }

        // Update display
        UpdateValueDisplay();

        if (showDebugLogs) Debug.Log($"[CameraSettingsUI] Initialized with sensitivity: {playerCamera.GetSensitivity():F2}x");
    }

    void OnDestroy()
    {
        // Unsubscribe from slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }
    }

    /// <summary>
    /// Called when slider value changes
    /// </summary>
    void OnSensitivityChanged(float newValue)
    {
        if (playerCamera != null)
        {
            playerCamera.SetSensitivity(newValue);
            UpdateValueDisplay();

            if (showDebugLogs) Debug.Log($"[CameraSettingsUI] Sensitivity changed to: {newValue:F2}x");
        }
    }

    /// <summary>
    /// Update the text display showing current value
    /// </summary>
    void UpdateValueDisplay()
    {
        if (sensitivityValueText != null && playerCamera != null)
        {
            float currentValue = playerCamera.GetSensitivity();
            sensitivityValueText.text = string.Format(valueFormat, currentValue);
        }
    }

    /// <summary>
    /// Reset sensitivity to default (1.0x)
    /// Call this from a "Reset to Default" button if desired
    /// </summary>
    public void ResetToDefault()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = 1.0f;
        }

        if (showDebugLogs) Debug.Log("[CameraSettingsUI] Reset to default sensitivity");
    }
}