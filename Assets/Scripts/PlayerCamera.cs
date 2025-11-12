using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;          // Player's head pivot
    public float distance = 4f;       // Current camera distance
    public float minDistance = 0.3f;  // First-person threshold
    public float maxDistance = 6f;

    public float zoomSpeed = 2f;
    
    [Header("Sensitivity Settings")]
    [Tooltip("Base orbit speed (third-person)")]
    public float baseOrbitSpeed = 150f;
    
    [Tooltip("Base look speed (first-person)")]
    public float baseLookSpeed = 2f;
    
    [Tooltip("Current sensitivity multiplier (0.1 - 3.0)")]
    [Range(0.1f, 3.0f)]
    public float sensitivityMultiplier = 1f;

    [Header("Collision Settings")]
    public float collisionRadius = 0.2f;      
    public float collisionSmoothing = 5f;     
    public float collisionMinDistance = 1f;   

    private float orbitSpeed;
    private float lookSpeed;

    private float yaw = 0f;
    private float pitch = 15f;
    private float currentDistance; 
    private Camera cam;
    private int defaultCullingMask;
    private int playerModelMask;
    private LayerMask collisionLayerMask;

    void Start()
    {
        cam = GetComponent<Camera>();
        defaultCullingMask = cam.cullingMask;
        playerModelMask = 1 << LayerMask.NameToLayer("PlayerModel");

        // Collide with everything EXCEPT PlayerModel layer
        collisionLayerMask = ~playerModelMask;
        
        currentDistance = distance;

        LoadSensitivity();
        UpdateSpeeds();
    }

    void LateUpdate()
    {
        if (!target) return;

        // Zoom input
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Determine blend between 3rd and 1st person
        float blend = Mathf.InverseLerp(maxDistance, minDistance, distance);

        // ================= FIRST-PERSON =================
        if (blend > 0.95f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            // Hide player model for first-person
            cam.cullingMask = defaultCullingMask & ~playerModelMask;
        }
        // ================= THIRD-PERSON =================
        else
        {
            // Hide cursor only while holding Right Mouse Button (RMB)
            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                yaw += mouseX * orbitSpeed * Time.deltaTime;
                pitch -= mouseY * orbitSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, -40f, 75f);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Show the PlayerModel layer in third-person
            cam.cullingMask = defaultCullingMask;
        }

        // ================= ROTATION & POSITION =================
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredDirection = rotation * Vector3.back;
        float targetDistance = distance;

        // Collision check (third-person only)
        if (blend < 0.95f)
        {
            if (Physics.SphereCast(target.position, collisionRadius, desiredDirection, out RaycastHit hit, distance, collisionLayerMask))
            {
                targetDistance = Mathf.Max(hit.distance - collisionRadius, collisionMinDistance);
            }
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * collisionSmoothing);

        // Calculate positions
        Vector3 thirdPersonPos = target.position + rotation * new Vector3(0, 0, -currentDistance);
        Vector3 firstPersonPos = target.position;

        // Blend between third and first person
        transform.position = Vector3.Lerp(thirdPersonPos, firstPersonPos, blend);
        transform.rotation = rotation;
    }

    // ================= HELPER METHODS =================
    public bool IsFirstPerson()
    {
        float blend = Mathf.InverseLerp(maxDistance, minDistance, distance);
        return blend > 0.95f;
    }

    public void SetSensitivity(float sensitivity)
    {
        sensitivityMultiplier = Mathf.Clamp(sensitivity, 0.1f, 3.0f);
        UpdateSpeeds();
        SaveSensitivity();
    }

    public float GetSensitivity()
    {
        return sensitivityMultiplier;
    }

    void UpdateSpeeds()
    {
        orbitSpeed = baseOrbitSpeed * sensitivityMultiplier;
        lookSpeed = baseLookSpeed * sensitivityMultiplier;
    }

    void SaveSensitivity()
    {
        PlayerPrefs.SetFloat("CameraSensitivity", sensitivityMultiplier);
        PlayerPrefs.Save();
    }

    void LoadSensitivity()
    {
        if (PlayerPrefs.HasKey("CameraSensitivity"))
        {
            sensitivityMultiplier = PlayerPrefs.GetFloat("CameraSensitivity");
        }
    }
}
