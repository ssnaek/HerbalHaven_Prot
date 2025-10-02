using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 4f;   // base walk speed
    public float runSpeed = 6f;    // run speed
    public float gravity = -9.81f;

    private CharacterController controller;
    private float verticalVelocity = 0f;
    private Animator animator;

    private PlayerCamera playerCamera; //ref to cam

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(); // finds Animator on model
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * h + camForward * v;

        // Run toggle
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float moveSpeed = isRunning ? runSpeed : walkSpeed;

        // Rotate model towards direction it is moving
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
        } //first person camera orientation

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // Apply final movement
        Vector3 finalMove = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);

        // Set animation to current velocity
        float currentSpeed = new Vector3(move.x, 0, move.z).magnitude * moveSpeed;
        animator.SetFloat("Speed", currentSpeed);
    }
}
