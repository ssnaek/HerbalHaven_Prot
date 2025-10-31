using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

/// <summary>
/// Popup UI for creating a new save file with slide-in animation.
/// Player enters a name, then game starts.
/// </summary>
public class NewSavePopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_InputField saveNameInputField;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI errorText;

    [Header("Settings")]
    public int maxNameLength = 30;
    public string defaultSaveName = "New Save";

    [Header("Animation")]
    [Tooltip("Duration of slide animation in seconds")]
    public float slideDuration = 0.3f;
    [Tooltip("Animation curve for smooth motion")]
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float hiddenOffset = 500f;  

    [Header("Debug")]
    public bool showDebugLogs = false;

    private RectTransform popupRectTransform;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private bool isAnimating = false;

    void Start()
    {
        // Wire up buttons
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        // Set input field settings
        if (saveNameInputField != null)
        {
            saveNameInputField.characterLimit = maxNameLength;
            saveNameInputField.text = defaultSaveName;
        }

        // Hide error text initially
        if (errorText != null)
            errorText.gameObject.SetActive(false);

        // Setup animation positions
        if (popupPanel != null)
        {
            popupRectTransform = popupPanel.GetComponent<RectTransform>();
            
            if (popupRectTransform != null)
            {
                // Store visible position (current position)
                visiblePosition = popupRectTransform.anchoredPosition;
                
                // Calculate hidden position (below screen)
                // Move down by panel height + extra margin
                float panelHeight = popupRectTransform.rect.height;
                hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - panelHeight - hiddenOffset);
                
                // Start hidden
                popupRectTransform.anchoredPosition = hiddenPosition;
                popupPanel.SetActive(false);
                
                if (showDebugLogs) 
                    Debug.Log($"[NewSavePopup] Animation setup - Visible: {visiblePosition}, Hidden: {hiddenPosition}");
            }
        }
        else
        {
            Debug.LogError("[NewSavePopup] popupPanel is not assigned!");
        }
    }

    /// <summary>
    /// Show the popup with slide-in animation
    /// </summary>
    public void ShowPopup()
    {
        if (popupPanel == null || isAnimating) return;

        popupPanel.SetActive(true);

        // Reset input field
        if (saveNameInputField != null)
        {
            saveNameInputField.text = defaultSaveName;
            saveNameInputField.Select();
            saveNameInputField.ActivateInputField();
        }

        // Hide error
        if (errorText != null)
            errorText.gameObject.SetActive(false);

        // Start slide-in animation
        StartCoroutine(SlideIn());

        if (showDebugLogs) Debug.Log("[NewSavePopup] Popup showing with animation");
    }

    /// <summary>
    /// Hide the popup with slide-out animation
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel == null || isAnimating) return;

        // Start slide-out animation
        StartCoroutine(SlideOut());

        if (showDebugLogs) Debug.Log("[NewSavePopup] Popup hiding with animation");
    }

    IEnumerator SlideIn()
    {
        if (popupRectTransform == null) yield break;

        isAnimating = true;
        float elapsed = 0f;

        // Start from hidden position
        popupRectTransform.anchoredPosition = hiddenPosition;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float curveT = slideCurve.Evaluate(t);

            // Lerp from hidden to visible
            popupRectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, curveT);

            yield return null;
        }

        // Ensure final position is exact
        popupRectTransform.anchoredPosition = visiblePosition;
        isAnimating = false;

        if (showDebugLogs) Debug.Log("[NewSavePopup] Slide-in complete");
    }

    IEnumerator SlideOut()
    {
        if (popupRectTransform == null) yield break;

        isAnimating = true;
        float elapsed = 0f;

        // Start from visible position
        popupRectTransform.anchoredPosition = visiblePosition;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float curveT = slideCurve.Evaluate(t);

            // Lerp from visible to hidden
            popupRectTransform.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, curveT);

            yield return null;
        }

        // Ensure final position is exact
        popupRectTransform.anchoredPosition = hiddenPosition;
        popupPanel.SetActive(false);
        isAnimating = false;

        if (showDebugLogs) Debug.Log("[NewSavePopup] Slide-out complete");
    }

    void OnConfirmClicked()
    {
        string saveName = saveNameInputField != null ? saveNameInputField.text.Trim() : defaultSaveName;

        // Validate name
        if (string.IsNullOrEmpty(saveName))
        {
            ShowError("Save name cannot be empty!");
            return;
        }

        if (saveName.Length < 3)
        {
            ShowError("Save name must be at least 3 characters!");
            return;
        }

        // Check for invalid characters
        if (saveName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ShowError("Save name contains invalid characters!");
            return;
        }

        if (showDebugLogs) Debug.Log($"[NewSavePopup] Creating new save: {saveName}");

        // Create new save
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.CreateNewSave(saveName);
            HidePopup();
        }
        else
        {
            Debug.LogError("[NewSavePopup] SaveLoadManager not found!");
            ShowError("Save system not available!");
        }
    }

    void OnCancelClicked()
    {
        if (showDebugLogs) Debug.Log("[NewSavePopup] New save cancelled");
        HidePopup();
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        if (showDebugLogs) Debug.LogWarning($"[NewSavePopup] Error: {message}");
    }
}