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

    private float yaw = 0f;
    private float pitch = 15f;

    private Camera cam;
    private int defaultCullingMask;
    private int playerModelMask;

    void Start()
    {
        cam = GetComponent<Camera>();
        defaultCullingMask = cam.cullingMask;
        playerModelMask = 1 << LayerMask.NameToLayer("PlayerModel"); 
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- Blend factor (0 = third person, 1 = first person) ---
        float blend = Mathf.InverseLerp(maxDistance, minDistance, distance);

        // --- Input handling ---
        if (blend > 0.95f) // practically in first-person
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -80f, 80f);

            // Hide the PlayerModel layer
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

        // --- Rotation ---
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // --- Third-person offset ---
        Vector3 thirdPersonPos = target.position + rotation * new Vector3(0, 0, -distance);

        // --- First-person offset (exact head) ---
        Vector3 firstPersonPos = target.position;

        // --- Blend between them ---
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
