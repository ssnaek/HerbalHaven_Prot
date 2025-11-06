using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Handles returning to the main menu from in-game.
/// Requires holding the button for 3 seconds to confirm.
/// Attach to a "Back to Main Menu" button in your Settings panel.
/// Uses CircleTransition if available.
/// </summary>
public class MainMenuButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scene Settings")]
    [Tooltip("Name of the main menu scene")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Hold Settings")]
    [Tooltip("How long to hold button (seconds)")]
    public float holdDuration = 3f;

    [Header("UI References")]
    [Tooltip("Button component (auto-finds if empty)")]
    public Button button;

    [Tooltip("Separate text component to show countdown")]
    public TextMeshProUGUI countdownText;
    
    [Tooltip("Separate text component to show note")]
    public TextMeshProUGUI NoteText;

    [Header("Countdown Display")]
    public string countdownFormat = "[{0}].."; // Shows as [3]..[2]..[1]..
    public string returningText = "Returning...";

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool isHolding = false;
    private float holdTimer = 0f;

    void Start()
    {
        // Auto-find button
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // Hide countdown text initially
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        if (NoteText != null)
        {
            NoteText.gameObject.SetActive(false);
        }

    }

    void Update()
    {
        if (isHolding)
        {
            // Increment timer
            holdTimer += Time.deltaTime;

            // Calculate remaining seconds (ceiling to show 3, 2, 1)
            int remainingSeconds = Mathf.CeilToInt(holdDuration - holdTimer);
            remainingSeconds = Mathf.Max(0, remainingSeconds);

            // Update countdown text
            if (countdownText != null && remainingSeconds > 0)
            {
                countdownText.text = string.Format(countdownFormat, remainingSeconds);
            }

            // Check if held long enough
            if (holdTimer >= holdDuration)
            {
                OnHoldComplete();
            }
        }
    }

    /// <summary>
    /// Called when pointer down (mouse/touch press)
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (showDebugLogs) Debug.Log("[MainMenuButton] Button pressed - starting hold timer");

        isHolding = true;
        holdTimer = 0f;

        // Show countdown text
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        if (NoteText != null)
        {
            NoteText.gameObject.SetActive(true);
        }

        PlaySound(sfxLibrary?.uiSelect);
    }

    /// <summary>
    /// Called when pointer up (mouse/touch release)
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHolding)
        {
            if (showDebugLogs) Debug.Log("[MainMenuButton] Button released - cancelling hold");

            // Reset
            isHolding = false;
            holdTimer = 0f;

            // Hide countdown text
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            if (NoteText != null)
            {
                NoteText.gameObject.SetActive(false);
            }

        }
    }

    /// <summary>
    /// Called when button held for full duration
    /// </summary>
    void OnHoldComplete()
    {
        if (showDebugLogs) Debug.Log("[MainMenuButton] Hold complete - returning to main menu");

        isHolding = false;

        // Show returning message
        if (countdownText != null)
        {
            countdownText.text = returningText;
        }

        PlaySound(sfxLibrary?.uiConfirm);

        ReturnToMainMenu();
    }

    /// <summary>
    /// Actually return to the main menu
    /// </summary>
    void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("[MainMenuButton] Main menu scene name is empty!");
            return;
        }

        // Use circle transition if available
        if (CircleTransition.Instance != null)
        {
            if (showDebugLogs) Debug.Log("[MainMenuButton] Using CircleTransition");
            
            CircleTransition.Instance.DoTransition(() =>
            {
                LoadMainMenu();
            });
        }
        else
        {
            // No transition available, load directly
            if (showDebugLogs) Debug.LogWarning("[MainMenuButton] No CircleTransition found, loading directly");
            LoadMainMenu();
        }
    }

    void LoadMainMenu()
    {
        if (showDebugLogs) Debug.Log($"[MainMenuButton] Loading main menu scene: {mainMenuSceneName}");
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}