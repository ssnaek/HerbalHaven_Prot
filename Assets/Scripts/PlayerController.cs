using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float CurrentSpeed { get; private set; } 
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Slope Settings")]
    public float slopeLimit = 60f;  // Maximum climb-able slope angle

    private CharacterController controller;
    private float verticalVelocity = 0f;
    private Animator animator;
    private PlayerCamera playerCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerCamera = Camera.main.GetComponent<PlayerCamera>();
        
        // Set the CharacterController's slope limit
        controller.slopeLimit = slopeLimit;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Camera-relative movement
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * h + camForward * v;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        // Rotate toward movement direction
        if (move.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
        else if (playerCamera && playerCamera.IsFirstPerson())
        {
            Vector3 camDir = Camera.main.transform.forward;
            camDir.y = 0f;
            if (camDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(camDir),
                    10f * Time.deltaTime
                );
            }
        }

        // Jump + Gravity
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f;

            // Only allow jumping on walkable slopes
            if (Input.GetButtonDown("Jump") && !IsOnSteepSlope())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply movement
        Vector3 finalMove = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);

        // Animate
        CurrentSpeed = new Vector3(move.x, 0, move.z).magnitude * moveSpeed;
        animator.SetFloat("Speed", CurrentSpeed);
    }

    bool IsOnSteepSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, controller.height / 2 + 0.5f))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            return angle > slopeLimit;
        }
        return false;
    }
}