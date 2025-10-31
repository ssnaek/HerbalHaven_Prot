using UnityEngine;
using System.Collections;

/// <summary>
/// Slides a UI panel in/out from edge of screen.
/// Shows/hides panel while keeping toggle button visible.
/// Attach to the panel you want to slide.
/// </summary>
public class UISlideToggle : MonoBehaviour
{
    [Header("Slide Settings")]
    [Tooltip("How much of panel stays visible when hidden (for button)")]
    public float visiblePortionWhenHidden = 50f;
    
    [Tooltip("Slide duration")]
    public float slideDuration = 0.3f;
    
    [Tooltip("Slide from which edge")]
    public SlideDirection slideDirection = SlideDirection.Left;

    [Header("References")]
    [Tooltip("Button to toggle (optional - can call Toggle() from any button)")]
    public UnityEngine.UI.Button toggleButton;

    [Header("Audio (Optional)")]
    public SFXLibrary sfxLibrary;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private RectTransform rectTransform;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private bool isVisible = true;
    private bool isAnimating = false;

    public enum SlideDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform == null)
        {
            Debug.LogError("[UISlideToggle] No RectTransform found!");
            enabled = false;
            return;
        }

        // Store visible position (current position)
        visiblePosition = rectTransform.anchoredPosition;

        // Calculate hidden position
        CalculateHiddenPosition();

        // Wire up button if assigned
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }

        if (showDebugLogs) Debug.Log("[UISlideToggle] Initialized");
    }

    void CalculateHiddenPosition()
    {
        Rect rect = rectTransform.rect;
        hiddenPosition = visiblePosition;

        switch (slideDirection)
        {
            case SlideDirection.Left:
                // Slide left, keeping right portion visible
                hiddenPosition.x = visiblePosition.x - (rect.width - visiblePortionWhenHidden);
                break;
                
            case SlideDirection.Right:
                // Slide right, keeping left portion visible
                hiddenPosition.x = visiblePosition.x + (rect.width - visiblePortionWhenHidden);
                break;
                
            case SlideDirection.Top:
                // Slide up, keeping bottom portion visible
                hiddenPosition.y = visiblePosition.y + (rect.height - visiblePortionWhenHidden);
                break;
                
            case SlideDirection.Bottom:
                // Slide down, keeping top portion visible
                hiddenPosition.y = visiblePosition.y - (rect.height - visiblePortionWhenHidden);
                break;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[UISlideToggle] Visible pos: {visiblePosition}, Hidden pos: {hiddenPosition}");
        }
    }

    /// <summary>
    /// Toggle between visible and hidden
    /// </summary>
    public void Toggle()
    {
        if (isAnimating) return;

        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Show the panel
    /// </summary>
    public void Show()
    {
        if (isAnimating || isVisible) return;

        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(SlideToPosition(visiblePosition, true));
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void Hide()
    {
        if (isAnimating || !isVisible) return;

        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(SlideToPosition(hiddenPosition, false));
    }

    IEnumerator SlideToPosition(Vector2 targetPosition, bool showing)
    {
        isAnimating = true;
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        isVisible = showing;
        isAnimating = false;

        if (showDebugLogs) Debug.Log($"[UISlideToggle] Slide complete - isVisible: {isVisible}");
    }

    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    // Public getters
    public bool IsVisible() => isVisible;
    public bool IsAnimating() => isAnimating;
}
