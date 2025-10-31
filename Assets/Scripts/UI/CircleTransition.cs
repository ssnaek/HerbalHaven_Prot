using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Circle wipe transition effect for scene transitions and teleports.
/// Each scene can have its own instance - no persistence needed.
/// Attach to a Canvas GameObject. TransitionOverlay child has the Image + Material.
/// </summary>
public class CircleTransition : MonoBehaviour
{
    // Static reference for easy access, but NOT a persistent singleton
    public static CircleTransition Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Image that will mask the screen (child GameObject with material)")]
    public Image transitionImage;

    [Header("Transition Settings")]
    [Tooltip("How long the transition takes (in seconds)")]
    public float transitionDuration = 0.5f;
    
    [Tooltip("Minimum radius value when circle is closed (try -0.1 to -0.3 if gap visible)")]
    public float minRadius = -0.1f;
    
    [Tooltip("Maximum radius value when circle is open")]
    public float maxRadius = 1.1f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    private Canvas canvas;
    private bool isTransitioning = false;
    private Material transitionMaterial;

    void Awake()
    {
        // Simple instance reference - no DontDestroyOnLoad, no persistence
        // Each scene will have its own instance
        Instance = this;

        // Setup Canvas
        SetupCanvas();

        if (showDebugLogs) Debug.Log($"[CircleTransition] Initialized in scene");
    }

    void SetupCanvas()
    {
        // Get Canvas component
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CircleTransition] No Canvas component found!");
            canvas = gameObject.AddComponent<Canvas>();
        }

        // Configure Canvas to overlay everything
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Always on top

        // Ensure CanvasScaler
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Ensure GraphicRaycaster
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Verify image reference
        if (transitionImage == null)
        {
            Debug.LogError("[CircleTransition] TransitionOverlay Image not assigned!");
        }
        else
        {
            // Make fullscreen
            RectTransform rect = transitionImage.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Create material instance
            if (transitionImage.material != null)
            {
                transitionMaterial = new Material(transitionImage.material);
                transitionImage.material = transitionMaterial;

                if (showDebugLogs) Debug.Log("[CircleTransition] Created material instance");
            }
        }
    }

    void Start()
    {
        if (transitionImage == null)
        {
            Debug.LogError("[CircleTransition] Transition Image not assigned!");
            return;
        }

        // Start fully open (screen visible)
        SetCircleSize(1f);

        if (showDebugLogs) Debug.Log("[CircleTransition] Ready - screen visible");
    }

    void OnDestroy()
    {
        // Clean up material instance
        if (transitionMaterial != null)
        {
            Destroy(transitionMaterial);
        }

        // Clear instance reference if this was the active instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Full transition: fade out, do action, fade in
    /// </summary>
    public void DoTransition(System.Action onTransitionMiddle)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionSequence(onTransitionMiddle));
        }
        else if (showDebugLogs)
        {
            Debug.Log("[CircleTransition] Already transitioning, ignoring request");
        }
    }

    /// <summary>
    /// Just fade out (close circle)
    /// </summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }
    }

    /// <summary>
    /// Just fade in (open circle)
    /// </summary>
    public void FadeIn(System.Action onComplete = null)
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeInCoroutine(onComplete));
        }
    }

    IEnumerator TransitionSequence(System.Action onMiddle)
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(FadeOutCoroutine(null));

        // Do action (load scene, teleport, etc)
        onMiddle?.Invoke();

        // Small delay
        yield return new WaitForSeconds(0.1f);

        // Fade in
        yield return StartCoroutine(FadeInCoroutine(null));

        isTransitioning = false;
    }

    IEnumerator FadeOutCoroutine(System.Action onComplete)
    {
        isTransitioning = true;

        // Play sound
        if (AudioManager.Instance != null && sfxLibrary != null && sfxLibrary.sceneTransitionStart != null)
        {
            AudioManager.Instance.PlaySFX(sfxLibrary.sceneTransitionStart);
        }

        if (showDebugLogs) Debug.Log("[CircleTransition] Fading out...");

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // Circle shrinks: 1 (visible) -> 0 (black)
            SetCircleSize(1f - t);

            yield return null;
        }

        SetCircleSize(0f);

        if (showDebugLogs) Debug.Log("[CircleTransition] Fade out complete");

        onComplete?.Invoke();
        isTransitioning = false;
    }

    IEnumerator FadeInCoroutine(System.Action onComplete)
    {
        isTransitioning = true;

        if (showDebugLogs) Debug.Log("[CircleTransition] Fading in...");

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            // Circle grows: 0 (black) -> 1 (visible)
            SetCircleSize(t);

            yield return null;
        }

        SetCircleSize(1f);

        if (showDebugLogs) Debug.Log("[CircleTransition] Fade in complete");

        onComplete?.Invoke();
        isTransitioning = false;
    }

    void SetCircleSize(float size)
    {
        if (transitionImage == null) return;

        // If using circle shader material
        if (transitionMaterial != null && transitionMaterial.HasProperty("_Radius"))
        {
            // size: 0 = covered (black), 1 = open (visible)
            // Use minRadius to maxRadius range for proper closure
            float radius = Mathf.Lerp(minRadius, maxRadius, size);
            transitionMaterial.SetFloat("_Radius", radius);

            // Keep image visible
            Color color = transitionImage.color;
            color.a = 1f;
            transitionImage.color = color;
        }
        else
        {
            // Fallback: simple fade
            Color color = transitionImage.color;
            color.a = 1f - size; // 0 = transparent, 1 = black
            transitionImage.color = color;
        }
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}