using UnityEngine;

/// <summary>
/// Makes UI elements sway gently.
/// Uses anchored position to respect UI layout system.
/// Attach to any UI element (Text, Image, Panel, etc.)
/// </summary>
public class UISway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("How fast it sways")]
    public float swaySpeed = 1f;
    
    [Tooltip("How much it moves up/down (pixels)")]
    public float swayAmountY = 5f;
    
    [Tooltip("How much it rotates (degrees)")]
    public float swayRotation = 2f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Quaternion originalRotation;
    private float timeOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (rectTransform == null)
        {
            Debug.LogError("[UISway] No RectTransform found!");
            enabled = false;
            return;
        }
        
        // Store original anchored position and rotation
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
        
        // Random offset so multiple elements don't sync
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (rectTransform == null) return;

        float time = Time.time + timeOffset;
        
        // Calculate sway
        float yOffset = Mathf.Sin(time * swaySpeed) * swayAmountY;
        float rotation = Mathf.Sin(time * swaySpeed * 0.7f) * swayRotation;
        
        // Apply anchored position (respects anchors!)
        rectTransform.anchoredPosition = originalAnchoredPosition + new Vector2(0, yOffset);
        
        // Apply rotation
        rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotation);
    }
}