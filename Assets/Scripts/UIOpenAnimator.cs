using UnityEngine;
using System.Collections;

/// <summary>
/// Unified animator for Journal and Shop UI panels.
/// Handles slide-down animation with optional swaying effect.
/// </summary>
public class UIOpenAnimator : MonoBehaviour
{
    [Header("Animation Type")]
    [Tooltip("What type of UI is this animating?")]
    public UIType uiType = UIType.Journal;

    [Header("References")]
    public RectTransform closedIcon;       // UI Image for closed state (book cover, shop sign, etc.)
    public GameObject openPanel;           // The actual UI panel when open

    [Header("Slide Settings")]
    public float slideDuration = 0.6f;
    public Vector2 startPos = new Vector2(0, 800);
    public Vector2 endPos = new Vector2(0, 0);
    public float slideSwingAmplitude = 10f;     // degrees of swing during slide

    [Header("Panel Swing (for Journal only)")]
    [Tooltip("Enable swaying oscillation after opening (Journal only)")]
    public bool enablePanelSwing = true;
    public float panelSwingAmplitude = 1f;
    public float panelSwingFrequency = 2f;
    public float panelSwingDamping = 5f;
    public float panelMaxSwingDuration = 1.5f;

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.K;
    public bool useKeyInput = false;

    [Header("Cancel/QOL")]
    public float cancelReverseMin = 0.15f;
    public float cancelReverseMax = 0.4f;

    // Public runtime state
    public bool isOpen { get; private set; } = false;
    public bool isAnimating { get; private set; } = false;
    
    private bool cancelRequested = false;
    private Coroutine currentOpenCoroutine = null;
    private Coroutine currentCloseCoroutine = null;

    public enum UIType
    {
        Journal,
        Shop
    }

    void Start()
    {
        if (closedIcon == null) Debug.LogError($"[UIAnimator-{uiType}] closedIcon is not assigned!");
        if (openPanel == null) Debug.LogError($"[UIAnimator-{uiType}] openPanel is not assigned!");

        // Initialize
        if (closedIcon != null)
        {
            closedIcon.anchoredPosition = startPos;
            closedIcon.rotation = Quaternion.identity;
            closedIcon.gameObject.SetActive(false);
        }

        if (openPanel != null)
        {
            openPanel.SetActive(false);
            var panelRt = openPanel.GetComponent<RectTransform>();
            if (panelRt != null) panelRt.localRotation = Quaternion.identity;
        }

        Debug.Log($"[UIAnimator-{uiType}] Initialized. Panel hidden.");
    }

    void Update()
    {
        if (!useKeyInput) return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (isAnimating && !isOpen)
            {
                Debug.Log($"[UIAnimator-{uiType}] Cancel requested mid-open.");
                cancelRequested = true;
                return;
            }

            if (!isAnimating)
            {
                if (!isOpen) PlayOpen();
                else PlayClose();
            }
        }
    }

    public void PlayOpen()
    {
        if (isAnimating)
        {
            Debug.Log($"[UIAnimator-{uiType}] PlayOpen() called but animator is busy.");
            return;
        }

        if (currentOpenCoroutine != null) StopCoroutine(currentOpenCoroutine);
        currentOpenCoroutine = StartCoroutine(OpenUI());
    }

    public void RequestCancel()
    {
        if (isAnimating && !isOpen)
        {
            cancelRequested = true;
            Debug.Log($"[UIAnimator-{uiType}] Cancel requested");
        }
    }

    public void PlayClose()
    {
        if (isAnimating)
        {
            Debug.Log($"[UIAnimator-{uiType}] PlayClose() called but animator is busy.");
            return;
        }

        if (currentCloseCoroutine != null) StopCoroutine(currentCloseCoroutine);
        currentCloseCoroutine = StartCoroutine(CloseUI());
    }

    IEnumerator OpenUI()
    {
        if (closedIcon == null || openPanel == null) yield break;

        Debug.Log($"[UIAnimator-{uiType}] Starting OPEN animation...");
        isAnimating = true;
        cancelRequested = false;

        closedIcon.gameObject.SetActive(true);
        openPanel.SetActive(false);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (cancelRequested)
            {
                Debug.Log($"[UIAnimator-{uiType}] Cancel detected. Reversing...");
                yield return StartCoroutine(CancelOpenAnimation(elapsed));
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);

            closedIcon.anchoredPosition = Vector2.Lerp(startPos, endPos, bounce);
            float angle = Mathf.Sin(t * Mathf.PI) * slideSwingAmplitude;
            closedIcon.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // Finalize open
        closedIcon.rotation = Quaternion.identity;
        closedIcon.gameObject.SetActive(false);
        openPanel.SetActive(true);

        Debug.Log($"[UIAnimator-{uiType}] OPEN animation complete.");

        // Notify controllers
        NotifyOpened();

        // Start panel swing only if enabled (Journal only)
        if (enablePanelSwing && uiType == UIType.Journal)
        {
            StartCoroutine(SwingPanel());
        }

        isOpen = true;
        isAnimating = false;
        currentOpenCoroutine = null;
    }

    IEnumerator CancelOpenAnimation(float elapsedSoFar)
    {
        float progress = Mathf.Clamp01(elapsedSoFar / Mathf.Max(0.0001f, slideDuration));
        float reverseDuration = Mathf.Lerp(cancelReverseMin, cancelReverseMax, progress);

        Vector2 currentPos = closedIcon.anchoredPosition;
        Quaternion currentRot = closedIcon.rotation;

        float elapsed = 0f;
        while (elapsed < reverseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reverseDuration);
            closedIcon.anchoredPosition = Vector2.Lerp(currentPos, startPos, Mathf.SmoothStep(0f, 1f, t));
            closedIcon.rotation = Quaternion.Lerp(currentRot, Quaternion.identity, t);
            yield return null;
        }

        closedIcon.anchoredPosition = startPos;
        closedIcon.rotation = Quaternion.identity;
        closedIcon.gameObject.SetActive(false);

        cancelRequested = false;
        isAnimating = false;
        isOpen = false;
        currentOpenCoroutine = null;

        Debug.Log($"[UIAnimator-{uiType}] Open cancelled.");
    }

    IEnumerator CloseUI()
    {
        if (closedIcon == null || openPanel == null) yield break;

        Debug.Log($"[UIAnimator-{uiType}] Starting CLOSE animation...");
        isAnimating = true;

        // Reset panel rotation
        var panelRt = openPanel.GetComponent<RectTransform>();
        if (panelRt != null) panelRt.localRotation = Quaternion.identity;

        openPanel.SetActive(false);
        closedIcon.gameObject.SetActive(true);
        closedIcon.anchoredPosition = endPos;
        closedIcon.rotation = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            closedIcon.anchoredPosition = Vector2.Lerp(endPos, startPos, Mathf.SmoothStep(0f, 1f, t));
            float angle = Mathf.Sin(t * Mathf.PI) * -slideSwingAmplitude;
            closedIcon.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        closedIcon.rotation = Quaternion.identity;
        closedIcon.gameObject.SetActive(false);

        Debug.Log($"[UIAnimator-{uiType}] CLOSE animation complete.");

        isOpen = false;
        isAnimating = false;
        currentCloseCoroutine = null;
    }

    IEnumerator SwingPanel()
    {
        if (openPanel == null) yield break;

        RectTransform panelRt = openPanel.GetComponent<RectTransform>();
        if (panelRt == null) yield break;

        Debug.Log($"[UIAnimator-{uiType}] Starting panel swing...");

        float elapsed = 0f;
        float omega = 2f * Mathf.PI * panelSwingFrequency;
        float initialAmp = panelSwingAmplitude;

        while (elapsed < panelMaxSwingDuration)
        {
            elapsed += Time.deltaTime;
            float decay = Mathf.Exp(-panelSwingDamping * elapsed);
            float angle = initialAmp * decay * Mathf.Sin(omega * elapsed);
            panelRt.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (initialAmp * decay < 0.05f) break;

            yield return null;
        }

        panelRt.localRotation = Quaternion.identity;
        Debug.Log($"[UIAnimator-{uiType}] Panel swing complete.");
    }

    void NotifyOpened()
    {
        if (uiType == UIType.Journal)
        {
            JournalController controller = GetComponentInChildren<JournalController>();
            if (controller == null) controller = FindObjectOfType<JournalController>();
            if (controller != null) controller.OnJournalOpened();
        }
        else if (uiType == UIType.Shop)
        {
            // Shop doesn't need notification (RefreshShop is called in OpenShop)
        }
    }
}
