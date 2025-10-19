using UnityEngine;
using System.Collections;
public class LeafSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swaySpeed = 1.5f;
    public float swayAmount = 0.08f;
    public Vector3 swayDirection = new Vector3(1, 0, 1); // X and Z movement
    
    private Vector3 startPosition;
    private float randomOffset;
    
    void Start()
    {
        startPosition = transform.localPosition; // Local to parent trunk!
        randomOffset = Random.Range(0f, 100f);
    }
    
    void Update()
    {
        float swayX = Mathf.Sin((Time.time + randomOffset) * swaySpeed) * swayAmount * swayDirection.x;
        float swayZ = Mathf.Cos((Time.time + randomOffset) * swaySpeed * 0.7f) * swayAmount * swayDirection.z;
        
        transform.localPosition = startPosition + new Vector3(swayX, 0, swayZ);
    }
}
