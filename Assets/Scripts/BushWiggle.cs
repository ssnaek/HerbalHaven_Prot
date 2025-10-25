using UnityEngine;

public class BushWiggle : MonoBehaviour
{
    [Header("Wiggle Settings")]
    public float wiggleAmount = 0.1f;
    public float wiggleSpeed = 10f;
    public float wiggleDuration = 0.5f;
    
    private Vector3 originalPosition;
    private bool isWiggling = false;
    private float wiggleTimer = 0f;
    
    void Start()
    {
        originalPosition = transform.position;
    }
    
    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            isWiggling = true;
            wiggleTimer = wiggleDuration;
        }
    }
    
    void Update()
    {
        if (isWiggling)
        {
            wiggleTimer -= Time.deltaTime;
            
            // Wiggle side to side
            float wiggleX = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAmount;
            float wiggleZ = Mathf.Cos(Time.time * wiggleSpeed * 0.7f) * wiggleAmount;
            
            transform.position = originalPosition + new Vector3(wiggleX, 0, wiggleZ);
            
            if (wiggleTimer <= 0f)
            {
                isWiggling = false;
                transform.position = originalPosition;
            }
        }
    }
}