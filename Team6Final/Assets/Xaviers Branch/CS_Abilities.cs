using UnityEngine;

public enum AbilityMode
{
    GlideAndChargeJump, // hold space to charge jump, hold space in air to glide
    Dash                // directional dash system
}

public enum DashMode
{
    Directional,  // dashes the way you're moving
    CameraForward // dashes where the camera points
}

[RequireComponent(typeof(Rigidbody))]
public class CS_PlayerAbilities : MonoBehaviour
{
    [Header("References")]
    public CS_GravityCharacterController controller;

    [Header("Ability Mode")]
    public bool isChargeJumpEnabled = true;
    public AbilityMode abilityMode = AbilityMode.GlideAndChargeJump;

    // ── Charge Jump ───────────────────────────────────────────────────────────

    [Header("Charge Jump Settings")]
    public float chargeJumpMin     = 5f;   // jump force if you barely tap space
    public float chargeJumpMax     = 25f;  // jump force at full charge
    public float chargeJumpMaxTime = 1.5f; // seconds to reach full charge

    // ── Glide ─────────────────────────────────────────────────────────────────

    [Header("Glide Settings")]
    public float glideGravityScale = 0.15f; // how much gravity applies while gliding
    public float glideMaxFallSpeed = 1.5f;  // terminal velocity while gliding

    // ── Dash ──────────────────────────────────────────────────────────────────

    [Header("Dash Settings")]
    public DashMode dashMode          = DashMode.Directional;
    public float    dashForce         = 20f;  // how hard the dash launches you
    public float    dashDuration      = 0.2f; // how long the dash lasts
    public float    dashCooldown      = 1f;   // seconds before you can dash again
    public int      maxAirDashes      = 1;    // air dashes allowed before landing
    public KeyCode  dashKey           = KeyCode.LeftControl;

    // ── Ground Check ──────────────────────────────────────────────────────────

    [Header("Ground Check")]
    public float     groundCheckDistance = 0.65f;
    public LayerMask groundLayer;

    // ── Internals ─────────────────────────────────────────────────────────────

    private Rigidbody rb;
    private bool      isGrounded = false;

    // Charge jump
    private bool  isCharging  = false;
    private float chargeTimer = 0f;
    private bool isGliding = false;

    // Dash
    private bool    isDashing         = false;
    private float   dashTimer         = 0f;
    private float   dashCooldownTimer = 0f;
    private int     airDashesUsed     = 0;
    private Vector3 dashDirection     = Vector3.forward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (controller == null)
            controller = GetComponent<CS_GravityCharacterController>();
    }

    void Update()
    {
        CheckGrounded();

        if (abilityMode == AbilityMode.GlideAndChargeJump)
            HandleChargeJumpInput();
        else
            HandleDashInput();
    }

    void FixedUpdate()
    {
        if (abilityMode == AbilityMode.GlideAndChargeJump)
            HandleGlidePhysics();
        else
            HandleDashPhysics();
    }

    // ── Ground Check ──────────────────────────────────────────────────────────

    void CheckGrounded()
    {
        Vector3 gravDir = controller != null ? -controller.localUp : Vector3.down;
        isGrounded = Physics.Raycast(transform.position, gravDir, groundCheckDistance, groundLayer);

        if (isGrounded)
        {
            airDashesUsed = 0;

            // Landed — reset glide eligibility
            isGliding          = false;
        }
    }

    // ── Charge Jump Input ─────────────────────────────────────────────────────

    void HandleChargeJumpInput()
    {
        if (isGrounded)
        {
            isGliding = false;

            if (Input.GetButtonDown("Jump"))
            {
                isCharging = true;
                chargeTimer = 0f;
            }

            if (Input.GetButton("Jump") && isCharging)
            {
                // Clamp charge so it never goes past the max
                chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, chargeJumpMaxTime);
            }

            if (Input.GetButtonUp("Jump") && isCharging)
            {
                LaunchChargeJump();
            }
        }
        else
        {
            // We're in the air
            // If we were charging when we left the ground somehow, cancel it
            if (isCharging)
            {
                isCharging  = false;
                chargeTimer = 0f;
            }

            // Glide — hold space any time we're in the air
            isGliding = Input.GetButton("Jump");
        }
    }

    void LaunchChargeJump()
    {
        // t = how close to full charge we got (0 to 1)
        float t         = Mathf.Clamp01(chargeTimer / chargeJumpMaxTime);

        // Lerp between min and max based on how long space was held
        float jumpForce = Mathf.Lerp(chargeJumpMin, chargeJumpMax, t);

        Vector3 upDir = controller != null ? controller.localUp : Vector3.up;

        // Kill any downward velocity before launching so jumps feel consistent
        Vector3 gravDir          = -upDir;
        Vector3 horizontalVel    = rb.velocity - Vector3.Project(rb.velocity, gravDir);
        rb.velocity              = horizontalVel;

        rb.AddForce(upDir * jumpForce, ForceMode.Impulse);

        isCharging         = false;
        chargeTimer        = 0f;
    }

    // ── Glide Physics ─────────────────────────────────────────────────────────

    void HandleGlidePhysics()
    {
        if (!isGliding) return;

        Vector3 gravDir = controller != null ? -controller.localUp : Vector3.down;

        float fallSpeed = Vector3.Dot(rb.velocity, gravDir);

        // Only slow the fall when we're actually falling, not while still rising
        if (fallSpeed > 0f)
        {
            // Push back against gravity proportionally to how much we're reducing it
            Vector3 glideForce = -gravDir * Physics.gravity.magnitude * (1f - glideGravityScale);
            rb.AddForce(glideForce, ForceMode.Acceleration);

            // Hard cap on fall speed while gliding
            if (fallSpeed > glideMaxFallSpeed)
            {
                Vector3 horizontalVelocity = rb.velocity - Vector3.Project(rb.velocity, gravDir);
                rb.velocity = horizontalVelocity + gravDir * glideMaxFallSpeed;
            }
        }
    }

    // ── Dash Input ────────────────────────────────────────────────────────────

    void HandleDashInput()
    {
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        bool canDash = dashCooldownTimer <= 0f
                    && !isDashing
                    && (isGrounded || airDashesUsed < maxAirDashes);

        if (Input.GetKeyDown(dashKey) && canDash)
        {
            Vector3 upDir = controller != null ? controller.localUp : Vector3.up;

            if (dashMode == DashMode.Directional)
            {
                Vector3 input = GetMovementInput();
                dashDirection = input.magnitude > 0.1f ? input.normalized : transform.forward;
            }
            else
            {
                if (controller != null && controller.cameraTransform != null)
                    dashDirection = Vector3.ProjectOnPlane(
                        controller.cameraTransform.forward, upDir).normalized;
                else
                    dashDirection = transform.forward;
            }

            isDashing         = true;
            dashTimer         = 0f;
            dashCooldownTimer = dashCooldown;

            if (!isGrounded)
                airDashesUsed++;

            rb.velocity = Vector3.zero; // clean slate so dash direction is crisp
        }
    }

    // ── Dash Physics ──────────────────────────────────────────────────────────

    void HandleDashPhysics()
    {
        if (!isDashing) return;

        dashTimer += Time.fixedDeltaTime;

        if (dashTimer <= dashDuration)
        {
            rb.AddForce(dashDirection * dashForce, ForceMode.Acceleration);
        }
        else
        {
            isDashing = false;
            dashTimer = 0f;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 GetMovementInput()
    {
        float h   = Input.GetAxisRaw("Horizontal");
        float v   = Input.GetAxisRaw("Vertical");
        Vector3 upDir = controller != null ? controller.localUp : Vector3.up;

        if (controller != null && controller.cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(
                controller.cameraTransform.forward, upDir).normalized;
            Vector3 camRight   = Vector3.Cross(upDir, camForward).normalized;
            return camRight * h + camForward * v;
        }

        return transform.right * h + transform.forward * v;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (abilityMode == AbilityMode.GlideAndChargeJump && isCharging)
        {
            float t = Mathf.Clamp01(chargeTimer / chargeJumpMaxTime);
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, t);
            Gizmos.DrawWireSphere(transform.position + (controller != null
                ? controller.localUp * 2f
                : Vector3.up * 2f), 0.2f + t * 0.4f);
        }

        if (abilityMode == AbilityMode.Dash && isDashing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, dashDirection * 2f);
        }
    }
}