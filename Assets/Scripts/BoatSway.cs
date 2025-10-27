using UnityEngine;

/// <summary>
/// Makes boat sway gently as if floating on water.
/// Attach to boat GameObject.
/// Similar to LeafSway but tuned for boat movement.
/// </summary>
public class BoatSway : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [Tooltip("How fast the boat bobs up and down")]
    public float bobSpeed = 0.5f;
    
    [Tooltip("How much the boat moves up/down")]
    public float bobAmount = 0.1f;

    [Header("Rocking Settings")]
    [Tooltip("How fast the boat rocks side to side")]
    public float rockSpeed = 0.3f;
    
    [Tooltip("How much the boat tilts (in degrees)")]
    public float rockAmount = 2f;

    [Header("Rotation Settings")]
    [Tooltip("Boat slowly rotates around Y axis")]
    public bool enableSlowRotation = true;
    
    [Tooltip("How fast boat rotates")]
    public float rotationSpeed = 0.1f;
    
    [Tooltip("Max rotation angle")]
    public float rotationAmount = 3f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float randomOffset;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Random offset so multiple boats don't sync
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Bobbing up and down (Y axis)
        float bobY = Mathf.Sin((Time.time + randomOffset) * bobSpeed) * bobAmount;
        
        // Rocking side to side (X rotation)
        float rockX = Mathf.Sin((Time.time + randomOffset) * rockSpeed) * rockAmount;
        
        // Gentle forward/back tilt (Z rotation)
        float rockZ = Mathf.Cos((Time.time + randomOffset) * rockSpeed * 0.7f) * rockAmount * 0.5f;
        
        // Optional slow rotation
        float rotateY = 0f;
        if (enableSlowRotation)
        {
            rotateY = Mathf.Sin((Time.time + randomOffset) * rotationSpeed) * rotationAmount;
        }
        
        // Apply movement
        transform.position = startPosition + new Vector3(0, bobY, 0);
        
        // Apply rotation
        transform.rotation = startRotation * Quaternion.Euler(rockX, rotateY, rockZ);
    }

    // Visualize start position in editor
    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.2f);
        
        // Draw bobbing range
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawLine(pos + Vector3.up * bobAmount, pos - Vector3.up * bobAmount);
    }
}