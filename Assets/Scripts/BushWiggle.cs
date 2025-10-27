using UnityEngine;

/// <summary>
/// Makes bush wiggle when player collides with it.
/// Now uses rotation instead of position for more realistic movement.
/// Attach to bush GameObject (parent of the 4 planes).
/// </summary>
public class BushWiggle : MonoBehaviour
{
    [Header("Wiggle Settings")]
    [Tooltip("How much the bush tilts (in degrees)")]
    public float wiggleAmount = 10f;
    
    [Tooltip("How fast the bush wiggles")]
    public float wiggleSpeed = 15f;
    
    [Tooltip("How long the wiggle lasts (seconds)")]
    public float wiggleDuration = 0.5f;

    [Header("Damping")]
    [Tooltip("How quickly wiggle fades out")]
    public float dampingSpeed = 5f;

    [Header("Audio")]
    public SFXLibrary sfxLibrary;
    
    private Quaternion originalRotation;
    private bool isWiggling = false;
    private float wiggleTimer = 0f;
    
    void Start()
    {
        originalRotation = transform.rotation;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isWiggling = true;
            wiggleTimer = wiggleDuration;

             if (AudioManager.Instance != null && sfxLibrary != null)
            {
                Debug.Log("Playing audio clip");
                AudioManager.Instance.PlaySFX(sfxLibrary.bushRustle, 0.5f);
            }
        }
    }
    
    void Update()
    {
        if (isWiggling)
        {
            wiggleTimer -= Time.deltaTime;
            
            // Calculate fade-out multiplier as wiggle ends
            float fadeMultiplier = Mathf.Clamp01(wiggleTimer / wiggleDuration);
            
            // Wiggle rotation on X and Z axes
            float wiggleX = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAmount * fadeMultiplier;
            float wiggleZ = Mathf.Cos(Time.time * wiggleSpeed * 0.7f) * wiggleAmount * fadeMultiplier;
            
            // Apply rotation from original rotation
            transform.rotation = originalRotation * Quaternion.Euler(wiggleX, 0, wiggleZ);
            
            // Stop wiggling when timer ends
            if (wiggleTimer <= 0f)
            {
                isWiggling = false;
            }
        }
        else
        {
            // Smoothly return to original rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * dampingSpeed);
        }
    }
}