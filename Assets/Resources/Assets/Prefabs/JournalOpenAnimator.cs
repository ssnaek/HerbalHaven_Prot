using UnityEngine;
using System.Collections;

public class JournalOpenAnimator : MonoBehaviour
{
    [Header("References")]
    public RectTransform closedBook;       // UI Image RectTransform for the closed book sprite
    public GameObject journalPanel;        // The actual journal UI panel (left/right pages)

    [Header("Slide settings")]
    public float slideDuration = 0.6f;
    public Vector2 startPos = new Vector2(0, 800);
    public Vector2 endPos = new Vector2(0, 0);
    public float swingAmplitude = 10f;     // degrees of swing for closedBook during slide

    [Header("Panel Swing (damped)")]
    public float panelSwingAmplitude = 1f;     // initial degrees for the opened journal panel swing
    public float panelSwingFrequency = 2f;     // oscillation frequency (Hz)
    public float panelSwingDamping = 5f;       // damping factor (larger = faster decay)
    public float panelMaxSwingDuration = 1.5f; // safety cap (seconds)

    [Header("Input")]
    public KeyCode toggleKey = KeyCode.K;
    public bool useKeyInput = false; // Disabled by default - let JournalController handle input

    [Header("Cancel / QOL")]
    [Tooltip("When canceling mid-open, the reverse animation will use a short duration interpolated between these two values depending on how far the open has progressed.")]
    public float cancelReverseMin = 0.15f;
    public float cancelReverseMax = 0.4f;

    // Public runtime state - accessible by JournalController
    public bool isOpen { get; private set; } = false;
    public bool isAnimating { get; private set; } = false;
    
    private bool cancelRequested = false;
    private Coroutine currentOpenCoroutine = null;
    private Coroutine currentCloseCoroutine = null;
    private JournalController journalController;

    void Start()
    {
        // Basic safety checks
        if (closedBook == null) Debug.LogError("[JournalAnimator] closedBook is not assigned!");
        if (journalPanel == null) Debug.LogError("[JournalAnimator] journalPanel is not assigned!");

        // Find journal controller
        journalController = GetComponentInChildren<JournalController>();
        if (journalController == null)
        {
            journalController = FindObjectOfType<JournalController>();
        }

        // Initialize transforms/states
        if (closedBook != null)
        {
            closedBook.anchoredPosition = startPos;
            closedBook.rotation = Quaternion.identity;
            closedBook.gameObject.SetActive(false);
        }

        if (journalPanel != null)
        {
            journalPanel.SetActive(false);
            var panelRt = journalPanel.GetComponent<RectTransform>();
            if (panelRt != null) panelRt.localRotation = Quaternion.identity;
        }

        Debug.Log("[JournalAnimator] Initialized. Closed book off-screen, journal hidden.");
    }

    void Update()
    {
        if (!useKeyInput) return;

        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[JournalAnimator] Key pressed. isAnimating={isAnimating}, isOpen={isOpen}");

            // If we're currently opening and user presses again, request cancel
            if (isAnimating && !isOpen)
            {
                Debug.Log("[JournalAnimator] Cancel requested mid-open.");
                cancelRequested = true;
                return;
            }

            // If not busy, toggle normally
            if (!isAnimating)
            {
                if (!isOpen) PlayOpen();
                else PlayClose();
            }
        }
    }

    /// <summary>
    /// Public call to start opening the journal animation.
    /// </summary>
    public void PlayOpen()
    {
        if (isAnimating)
        {
            Debug.Log("[JournalAnimator] PlayOpen() called but animator is busy.");
            return;
        }

        if (currentOpenCoroutine != null) StopCoroutine(currentOpenCoroutine);
        currentOpenCoroutine = StartCoroutine(OpenJournal());
    }

    /// <summary>
    /// Request cancellation of current opening animation
    /// </summary>
    public void RequestCancel()
    {
        if (isAnimating && !isOpen)
        {
            cancelRequested = true;
            Debug.Log("[JournalAnimator] Cancel requested");
        }
    }

    /// <summary>
    /// Public call to start closing the journal animation.
    /// </summary>
    public void PlayClose()
    {
        if (isAnimating)
        {
            Debug.Log("[JournalAnimator] PlayClose() called but animator is busy.");
            return;
        }

        if (currentCloseCoroutine != null) StopCoroutine(currentCloseCoroutine);
        currentCloseCoroutine = StartCoroutine(CloseJournalAnim());
    }

    IEnumerator OpenJournal()
    {
        if (closedBook == null || journalPanel == null)
        {
            Debug.LogError("[JournalAnimator] Missing references; aborting OpenJournal.");
            yield break;
        }

        Debug.Log("[JournalAnimator] Starting OPEN animation...");
        isAnimating = true;
        cancelRequested = false;

        closedBook.gameObject.SetActive(true);
        journalPanel.SetActive(false);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            // If cancel requested, gracefully reverse and finish closed.
            if (cancelRequested)
            {
                Debug.Log("[JournalAnimator] Cancel detected during open. Playing cancel reverse animation.");
                yield return StartCoroutine(CancelOpenAnimation(elapsed));
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);

            // position and swing
            closedBook.anchoredPosition = Vector2.Lerp(startPos, endPos, bounce);
            float angle = Mathf.Sin(t * Mathf.PI) * swingAmplitude;
            closedBook.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // finalize open
        closedBook.rotation = Quaternion.identity;
        closedBook.gameObject.SetActive(false);
        journalPanel.SetActive(true);

        Debug.Log("[JournalAnimator] OPEN animation complete. Showing journal panel.");

        // Notify JournalController that journal is now open
        if (journalController != null)
        {
            journalController.OnJournalOpened();
        }

        // start non-blocking panel swing
        StartCoroutine(SwingJournalPanel());

        isOpen = true;
        isAnimating = false;
        currentOpenCoroutine = null;
    }

    IEnumerator CancelOpenAnimation(float elapsedSoFar)
    {
        // elapsedSoFar = how long we've been opening. Calculate a short reverse duration proportional to progress
        float progress = Mathf.Clamp01(elapsedSoFar / Mathf.Max(0.0001f, slideDuration));
        float reverseDuration = Mathf.Lerp(cancelReverseMin, cancelReverseMax, progress);

        Vector2 currentPos = closedBook.anchoredPosition;
        Quaternion currentRot = closedBook.rotation;

        float elapsed = 0f;
        while (elapsed < reverseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / reverseDuration);
            closedBook.anchoredPosition = Vector2.Lerp(currentPos, startPos, Mathf.SmoothStep(0f, 1f, t));
            closedBook.rotation = Quaternion.Lerp(currentRot, Quaternion.identity, t);
            yield return null;
        }

        // finalize cancel
        closedBook.anchoredPosition = startPos;
        closedBook.rotation = Quaternion.identity;
        closedBook.gameObject.SetActive(false);

        cancelRequested = false;
        isAnimating = false;
        isOpen = false;
        currentOpenCoroutine = null;

        Debug.Log("[JournalAnimator] Open cancelled and reverted to closed state.");
    }

    IEnumerator CloseJournalAnim()
    {
        if (closedBook == null || journalPanel == null)
        {
            Debug.LogError("[JournalAnimator] Missing references; aborting CloseJournalAnim.");
            yield break;
        }

        Debug.Log("[JournalAnimator] Starting CLOSE animation...");
        isAnimating = true;

        // reset panel rotation if swing coroutine ran
        var panelRt = journalPanel.GetComponent<RectTransform>();
        if (panelRt != null) panelRt.localRotation = Quaternion.identity;

        journalPanel.SetActive(false);
        closedBook.gameObject.SetActive(true);
        closedBook.anchoredPosition = endPos;
        closedBook.rotation = Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            // slide up
            closedBook.anchoredPosition = Vector2.Lerp(endPos, startPos, Mathf.SmoothStep(0f, 1f, t));

            // reverse swing
            float angle = Mathf.Sin(t * Mathf.PI) * -swingAmplitude;
            closedBook.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        closedBook.rotation = Quaternion.identity;
        closedBook.gameObject.SetActive(false);

        Debug.Log("[JournalAnimator] CLOSE animation complete. Journal hidden.");

        isOpen = false;
        isAnimating = false;
        currentCloseCoroutine = null;
    }

    // Damped oscillation applied to the visible journalPanel after opening.
    IEnumerator SwingJournalPanel()
    {
        if (journalPanel == null)
        {
            Debug.LogWarning("[JournalAnimator] SwingJournalPanel called but journalPanel is null.");
            yield break;
        }

        RectTransform panelRt = journalPanel.GetComponent<RectTransform>();
        if (panelRt == null)
        {
            Debug.LogWarning("[JournalAnimator] journalPanel has no RectTransform; cannot swing.");
            yield break;
        }

        Debug.Log("[JournalAnimator] Starting panel swing (damped oscillation)...");

        float elapsed = 0f;
        float omega = 2f * Mathf.PI * panelSwingFrequency; // angular frequency
        float initialAmp = panelSwingAmplitude;

        while (elapsed < panelMaxSwingDuration)
        {
            elapsed += Time.deltaTime;
            float decay = Mathf.Exp(-panelSwingDamping * elapsed); // exponential decay
            float angle = initialAmp * decay * Mathf.Sin(omega * elapsed);
            panelRt.localRotation = Quaternion.Euler(0f, 0f, angle);

            // break early if decay is negligible
            if (initialAmp * decay < 0.05f) break;

            yield return null;
        }

        // ensure final reset
        panelRt.localRotation = Quaternion.identity;
        Debug.Log("[JournalAnimator] Panel swing complete. Rotation reset.");
    }
}