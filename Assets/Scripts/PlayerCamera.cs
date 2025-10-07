using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;          // Player's head pivot
    public float distance = 4f;       // Current camera distance
    public float minDistance = 0.3f;  // First-person threshold
    public float maxDistance = 6f;

    public float zoomSpeed = 2f;
    public float orbitSpeed = 150f;
    public float lookSpeed = 2f;

    [Header("Collision Settings")]
    public float collisionRadius = 0.2f;      // Sphere collision radius for collision check
    public float collisionSmoothing = 5f;     // How fast camera adjusts to collisions
    public float collisionMinDistance = 1f;   // Minimum distance when colliding with terrain

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
        
        // Collide with everything EXCEPT PlayerModel layer the player pack is assigned to
        collisionLayerMask = ~playerModelMask;
        
        currentDistance = distance;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Zoom 
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        float blend = Mathf.InverseLerp(maxDistance, minDistance, distance);

        // Input handling
        if (blend > 0.95f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            // Hide the PlayerModel layer for 1st person
            cam.cullingMask = defaultCullingMask & ~playerModelMask;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                yaw += mouseX * orbitSpeed * Time.deltaTime;
                pitch -= mouseY * orbitSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, -40f, 75f);
            }

            // Show the PlayerModel layer
            cam.cullingMask = defaultCullingMask;
        }

        // Rotation 
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        //Camera Collision Detection 
        Vector3 desiredDirection = rotation * Vector3.back;
        float targetDistance = distance;

        // Only check collision in third-person mode
        if (blend < 0.95f)
        {
            RaycastHit hit;
            if (Physics.SphereCast(target.position, collisionRadius, desiredDirection, 
                out hit, distance, collisionLayerMask))
            {
                // Reduce distance to not below threshold for 1st person
                targetDistance = Mathf.Max(hit.distance - collisionRadius, collisionMinDistance);
            }
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * collisionSmoothing);

        //Third-person offset
        Vector3 thirdPersonPos = target.position + rotation * new Vector3(0, 0, -currentDistance);

        //First-person offset 
        Vector3 firstPersonPos = target.position;

        // Blend for going from 3rd to 1st person
        transform.position = Vector3.Lerp(thirdPersonPos, firstPersonPos, blend);
        transform.rotation = rotation;
    }

    // Helper for PlayerController
    public bool IsFirstPerson()
    {
        float blend = Mathf.InverseLerp(maxDistance, minDistance, distance);
        return blend > 0.95f;
    }
}