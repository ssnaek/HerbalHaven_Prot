using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Circle wipe transition effect.
/// Can be used for teleports, scene transitions, etc.
/// Attach to a Canvas GameObject.
/// </summary>
public class CircleTransition : MonoBehaviour
{
    public static CircleTransition Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Image that will mask the screen (should be full screen)")]
    public Image transitionImage;

    [Header("Transition Settings")]
    [Tooltip("How long the transition takes (in seconds)")]
    public float transitionDuration = 0.5f;
    
    [Tooltip("Circle transition material (use UI/Default if you don't have custom)")]
    public Material circleMaterial;

    [Header("Debug")]
    public bool showDebugLogs = false;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;

    private bool isTransitioning = false;

    void Awake()
    {
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
    }

    void Start()
    {
        if (transitionImage == null)
        {
            Debug.LogError("[CircleTransition] Transition Image not assigned!");
            return;
        }

        // Force material instantiation so we're not modifying the shared material
        if (transitionImage.material != null)
        {
            transitionImage.material = new Material(transitionImage.material);
            if (showDebugLogs) Debug.Log("[CircleTransition] Created material instance");
        }

        // Start fully OPEN (circle at max size so screen is visible)
        SetCircleSize(1f); // 1 = fully open
        
        if (showDebugLogs) Debug.Log("[CircleTransition] Initialized - screen should be visible");
    }

    /// <summary>
    /// Fade out (circle closes), do action, fade in (circle opens)
    /// </summary>
    public void DoTransition(System.Action onTransitionMiddle)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionSequence(onTransitionMiddle));
        }
    }

    /// <summary>
    /// Just fade out (circle closes)
    /// </summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }
    }

    /// <summary>
    /// Just fade in (circle opens)
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

        // Fade out (close circle)
        yield return StartCoroutine(FadeOutCoroutine(null));

        // Do the action (teleport, load scene, etc)
        onMiddle?.Invoke();

        // Small delay
        yield return new WaitForSeconds(0.1f);

        // Fade in (open circle)
        yield return StartCoroutine(FadeInCoroutine(null));

        isTransitioning = false;
    }

    IEnumerator FadeOutCoroutine(System.Action onComplete)
    {
        isTransitioning = true;

        if (AudioManager.Instance != null && sfxLibrary != null)
        {
            AudioManager.Instance.PlaySFX(sfxLibrary.sceneTransitionStart);
        }

        if (showDebugLogs) Debug.Log("[CircleTransition] Fading out...");

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            
            // Circle shrinks from 1 (full screen visible) to 0 (black screen)
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
            
            // Circle grows from 0 (black screen) to 1 (full screen visible)
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
        if (transitionImage.material != null && transitionImage.material.HasProperty("_Radius"))
        {
            // size: 0 = screen covered (black), 1 = fully open (transparent)
            // _Radius: 0 = black screen, 1.0 = full screen visible (diagonal corner distance is ~0.707)
            float radius = size * 1.0f;
            transitionImage.material.SetFloat("_Radius", radius);
            
            // Keep image fully visible
            Color color = transitionImage.color;
            color.a = 1f;
            transitionImage.color = color;
        }
        else
        {
            // Fallback: simple fade if shader not set up
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