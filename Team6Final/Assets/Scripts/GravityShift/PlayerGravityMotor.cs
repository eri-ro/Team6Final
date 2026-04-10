using UnityEngine;

// Moves the player using a Rigidbody: applies walk velocity, jump, and gravity along GravityWorld.Up.
// Other scripts call SetMoveVelocity in Update with the desired horizontal speed (e.g. direction * moveSpeed).
public class PlayerGravityMotor : MonoBehaviour
{
    // Upward impulse when jumping.
    public float jumpForce = 6f;

    // Extra ray length past the feet so ground detection still works on small bumps.
    public float groundProbeExtraDistance = 0.18f;

    Rigidbody _rb;
    CapsuleCollider _cap;

    // Last requested horizontal velocity from the player controller.
    Vector3 _wishPlanarVelocity;

    // Set in Update when Space is pressed; consumed in FixedUpdate so physics always sees the jump.
    bool _jumpPressed;

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
        if (_rb == null)
        {
            Debug.LogError("PlayerGravityMotor needs a Rigidbody.", this);
            enabled = false;
            return;
        }

        // We apply gravity manually along GravityWorld.Up so Unity's default gravity does not fight wall walking.
        _rb.useGravity = false;
        // Mouse look rotates the transform; we do not want the Rigidbody to spin from collisions.
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Clears speed (used after a gravity shift so old fall velocity does not carry over).
    public void ClearVelocity()
    {
        if (_rb != null)
            _rb.velocity = Vector3.zero;
    }

    // Stores what the player wants for horizontal motion this frame (world space, along the walk plane).
    public void SetMoveVelocity(Vector3 planarWorldVelocity)
    {
        _wishPlanarVelocity = planarWorldVelocity;
        //Debug.Log($"wisvel out: {planarWorldVelocity.magnitude}");
    }

    // True if a short ray from the feet hits something below along -GravityWorld.Up.
    public bool IsGroundedForLogic()
    {
        Vector3 up = GravityWorld.Up;
        if (_rb == null || _cap == null)
            return false;

        // Bottom of the capsule along the body up axis.
        Vector3 axis = transform.up.normalized;
        Vector3 worldCenter = transform.TransformPoint(_cap.center);
        float half = _cap.height * 0.5f;
        Vector3 lowestOnCapsule = worldCenter - axis * half;
        Vector3 rayStart = lowestOnCapsule + up * 0.06f;
        // Ray must be long enough to reach the floor from the feet; too short = never grounded.
        float dist = groundProbeExtraDistance + Physics.defaultContactOffset * 2f + 0.12f;
        return Physics.Raycast(rayStart, -up, dist, ~0, QueryTriggerInteraction.Ignore);
    }

    // Physics step: apply gravity, horizontal wish, jump, then write velocity to the Rigidbody.
    void FixedUpdate()
    {
        if (_rb == null)
            return;

        Vector3 up = GravityWorld.Up;
        Vector3 vel = _rb.velocity;

        vel += Physics.gravity * Time.fixedDeltaTime;

        bool grounded = IsGroundedForLogic();
        if (grounded)
        {
            // Kill downward speed into the floor so we do not sink.
            float vUp = Vector3.Dot(vel, up);
            if (vUp < 0f)
                vel -= up * vUp;
        }

        // Replace horizontal part with wish; keep vertical speed for jump and fall.
        vel = _wishPlanarVelocity + up * Vector3.Dot(vel, up);

        if (_jumpPressed)
        {
            if (grounded)
                vel += up * jumpForce;
            _jumpPressed = false;
        }

        _rb.velocity = vel;

        // After landing, remove any leftover downward speed along up.
        if (IsGroundedForLogic() && Vector3.Dot(_rb.velocity, up) < 0f)
            _rb.velocity = Vector3.ProjectOnPlane(_rb.velocity, up);
    }
}
