using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Slope Settings")]
    public float slopeLimit = 60f;

    public float CurrentSpeed { get; private set; }

    private CharacterController controller;
    private float verticalVelocity = 0f;
    private Animator animator;
    private PlayerCamera playerCamera;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerCamera = Camera.main ? Camera.main.GetComponent<PlayerCamera>() : null;

        controller.slopeLimit = slopeLimit;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleJumpAndGravity();
        UpdateAnimations();
    }

    // ---------------- Movement ----------------
    void HandleMovement()
    {
        // WASD only - no arrow keys
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;
        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * h + camForward * v;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        CurrentSpeed = new Vector3(move.x, 0f, move.z).magnitude * moveSpeed;

        Vector3 finalMove = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    // ---------------- Rotation ----------------
    void HandleRotation()
    {
        // WASD only for rotation direction too
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A)) h = -1f;
        if (Input.GetKey(KeyCode.D)) h = 1f;
        if (Input.GetKey(KeyCode.W)) v = 1f;
        if (Input.GetKey(KeyCode.S)) v = -1f;

        Vector3 inputDir = new Vector3(h, 0f, v);
        Vector3 moveDir = Camera.main.transform.TransformDirection(inputDir);
        moveDir.y = 0f;

        if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
        else if (playerCamera && playerCamera.IsFirstPerson())
        {
            Vector3 camDir = Camera.main.transform.forward;
            camDir.y = 0f;
            if (camDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camDir), 10f * Time.deltaTime);
        }
    }

    // ---------------- Jump + Gravity ----------------
    void HandleJumpAndGravity()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (Input.GetButtonDown("Jump") && !IsOnSteepSlope())
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    // ---------------- Animation ----------------
    void UpdateAnimations()
    {
        if (animator)
            animator.SetFloat("Speed", CurrentSpeed);
    }

    // ---------------- Slope Check ----------------
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