using UnityEngine;
using System.Collections;

/// <summary>
/// Pans camera horizontally between counter and crafting station (side-by-side workspace).
/// Attach to Main Camera in Clinic scene.
/// </summary>
public class ClinicCameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    [Tooltip("Camera position for counter view (left)")]
    public Transform counterViewPosition;
    
    [Tooltip("Camera position for crafting station view (right)")]
    public Transform craftingViewPosition;
    
    [Header("Animation")]
    public float panDuration = 0.6f;
    public AnimationCurve panCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    [Header("Debug")]
    public bool showDebugLogs = false;
    
    private bool isAnimating = false;
    private bool showingCrafting = false;
    
    void Start()
    {
        // Start at counter view (left side)
        if (counterViewPosition != null)
        {
            transform.position = counterViewPosition.position;
        }
        
        if (showDebugLogs) Debug.Log("[ClinicCamera] Starting at counter view");
    }
    
    /// <summary>
    /// Pan camera to crafting station (right)
    /// </summary>
    public void ShowCrafting()
    {
        if (isAnimating || showingCrafting || craftingViewPosition == null) return;
        
        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(PanToPosition(craftingViewPosition.position));
        showingCrafting = true;
        
        if (showDebugLogs) Debug.Log("[ClinicCamera] Panning to crafting station");
    }
    
    /// <summary>
    /// Pan camera back to counter (left)
    /// </summary>
    public void ShowCounter()
    {
        if (isAnimating || !showingCrafting || counterViewPosition == null) return;
        
        PlaySound(sfxLibrary?.uiSelect);
        StartCoroutine(PanToPosition(counterViewPosition.position));
        showingCrafting = false;
        
        if (showDebugLogs) Debug.Log("[ClinicCamera] Panning to counter");
    }
    
    /// <summary>
    /// Smooth horizontal pan to target position
    /// </summary>
    IEnumerator PanToPosition(Vector3 targetPosition)
    {
        isAnimating = true;
        Vector3 startPosition = transform.position;
        float elapsed = 0f;
        
        while (elapsed < panDuration)
        {
            elapsed += Time.deltaTime;
            float t = panCurve.Evaluate(elapsed / panDuration);
            
            // Only move horizontally, keep Y and Z the same
            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);
            newPos.y = startPosition.y; // Lock Y axis
            newPos.z = startPosition.z; // Lock Z axis
            
            transform.position = newPos;
            
            yield return null;
        }
        
        // Ensure exact final position
        Vector3 finalPos = targetPosition;
        finalPos.y = startPosition.y;
        finalPos.z = startPosition.z;
        transform.position = finalPos;
        
        isAnimating = false;
        
        if (showDebugLogs) Debug.Log($"[ClinicCamera] Pan complete. Position: {transform.position}");
    }
    
    void PlaySound(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    // Public getters
    public bool IsAnimating() => isAnimating;
    public bool IsShowingCrafting() => showingCrafting;
}
