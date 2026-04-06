using UnityEngine;

// Moves and rotates the player character with gravity-aware controls
// Works with any gravity direction so it handles sphere planets, cube planets, etc.
[RequireComponent(typeof(Rigidbody))]
public class CS_GravityCharacterController : MonoBehaviour
{
    public CS_PlayerAbilities abilities; // reference to the player's abilities (for checking if glide/charge jump is active)
    [Header("Movement Settings")]
    public float walkSpeed = 5f;          // base movement speed
    public float sprintMultiplier = 2f;   // how much faster sprinting is
    public float jumpForce = 7f;          // impulse force applied when jumping

    [Header("Ground Check")]
    public float groundCheckDistance = 0.6f; // how far down to raycast to check if grounded
    public LayerMask groundLayer;            // which layers count as ground

    [Header("Rotation Settings")]
    public float rotationSpeed = 10f; // how fast the player body rotates to face the move direction

    [Header("Gravity Transition")]
    public float upAlignSpeed = 5f; // how fast localUp adjusts when entering a new gravity zone

    [Header("Camera (Optional)")]
    public Transform cameraTransform; // if assigned, movement is relative to the camera

    private Rigidbody rb;
    private Vector3 inputDirection; // the direction the player wants to move this frame

    public Vector3 localUp = Vector3.up;    // the player's current "up" direction (changes with gravity)
    private Vector3 targetUp = Vector3.up;  // the up direction we're smoothing toward
    private Vector3 gravityDir = Vector3.down; // opposite of localUp, used for raycasting and forces

    public CS_Orbital currentGravityField; // which gravity zone the player is currently in
    private bool isGrounded;               // true when the player is touching the ground
    private bool inSpace = false;          // true when there's no gravity field active

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;         // prevent physics from rotating the player
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // better collision for fast movement
        rb.interpolation = RigidbodyInterpolation.Interpolate;         // smoother visual movement
    }

    void Update()
    {
        // Read raw WASD/arrow key input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (cameraTransform != null)
        {
            // Move relative to the camera direction, flattened onto the player's gravity plane
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, localUp).normalized;
            Vector3 camRight   = Vector3.Cross(localUp, camForward).normalized;
            inputDirection     = (camRight * h + camForward * v).normalized;
        }
        else
        {
            // No camera — just move relative to the player's own forward/right
            inputDirection = (transform.right * h + transform.forward * v).normalized;
        }

        if (Input.GetKey(KeyCode.LeftShift))
            inputDirection *= sprintMultiplier; // sprint by scaling input

        // Raycast downward (in gravity direction) to check if we're on the ground
        isGrounded = Physics.Raycast(transform.position, gravityDir, groundCheckDistance, groundLayer);

        // Jump — only works if grounded
        if (Input.GetButtonDown("Jump") && isGrounded && !abilities.isChargeJumpEnabled)
            rb.AddForce(-gravityDir * jumpForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (currentGravityField != null)
        {
            // Figure out which way "up" should be by looking at the nearest planet surface point
            // This is what makes cube planet faces feel flat instead of weirdly angled
            Vector3 nearestPoint = currentGravityField.GetNearestSurfacePoint(transform.position);
            targetUp = (transform.position - nearestPoint).normalized;
            inSpace  = false;
        }
        else
        {
            targetUp = localUp; // in space — just hold whatever direction we had
            inSpace  = true;
        }

        // Smoothly rotate localUp toward targetUp (so transitions aren't instant snaps)
        localUp    = Vector3.Slerp(localUp, targetUp, upAlignSpeed * Time.fixedDeltaTime).normalized;
        gravityDir = -localUp; // gravity always pulls opposite to up

        // Rotate the player body to face the right direction
        if (cameraTransform != null)
        {
            // Face the direction the camera is pointing (projected onto the gravity plane)
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, localUp).normalized;
            if (camForward.sqrMagnitude < 0.001f)
                camForward = Vector3.ProjectOnPlane(transform.forward, localUp).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(camForward, localUp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // No camera — face the direction we're moving
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, localUp).normalized;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward, localUp);
        }

        // Apply movement forces
        if (inputDirection.magnitude > 0.01f)
        {
            Vector3 move = inputDirection * walkSpeed;

            if (inSpace)
            {
                // In space, use force-based movement with a speed cap (no ground to push off)
                rb.AddForce(move * 0.5f, ForceMode.Acceleration);
                float maxSpeed = walkSpeed * sprintMultiplier;
                if (rb.velocity.magnitude > maxSpeed)
                    rb.velocity = rb.velocity.normalized * maxSpeed;
            }
            else
            {
                // On a planet, directly set horizontal velocity but preserve vertical (gravity/jump)
                Vector3 verticalVelocity = Vector3.Project(rb.velocity, gravityDir);
                rb.velocity = move + verticalVelocity;
            }
        }
    }

    // Called by CS_Orbital when the player enters or exits a gravity zone
    public void SetGravityField(CS_Orbital field)
    {
        currentGravityField = field;

        if (field != null)
        {
            // Immediately recalculate targetUp for the new zone so the transition starts right
            Vector3 nearestPoint = field.GetNearestSurfacePoint(transform.position);
            targetUp = (transform.position - nearestPoint).normalized;
            inSpace  = false;
        }
        else
        {
            targetUp = localUp; // leaving all zones — freeze current up direction
            inSpace  = true;
        }
    }
}