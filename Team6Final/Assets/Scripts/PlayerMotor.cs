using UnityEngine;

// Moves the player using a Rigidbody: walk velocity from PlayerController, jump, gravity along GravityWorld.Up.
// Used on every level; gravity direction comes from GravityWorld (default world up until a shift changes it).
public class PlayerMotor : MonoBehaviour
{
    const int GroundProbeHitBufferSize = 8;

    // Upward impulse when jumping.
    public float jumpForce = 6f;

    // Extra ray length past the feet so ground detection still works on small bumps.
    public float groundProbeExtraDistance = 0.18f;

    // Peak height scales with v^2; sqrt(2) * jumpForce gives ~2x the height of a normal jump for the same gravity.
    [Tooltip("Multiplier on jumpForce for the high-jump ability impulse (sqrt(2) for 2x peak height).")]
    public float highJumpImpulseMultiplier = 1.4f;

    // While falling after a high jump, gravity along up is multiplied by this (lower = floatier glide).
    [Range(0.05f, 1f)]
    public float highJumpSlowFallGravityScale = 0.35f;

    [Tooltip("Ray hits whose normal is flatter than this vs gravity up are ignored (walls read as not ground).")]
    [Range(0.15f, 0.85f)]
    public float minGroundNormalDotUp = 0.35f;

    [Tooltip("Contacts closer to horizontal than this vs up are treated as walls — allows horizontal sliding along them.")]
    [Range(0.1f, 0.9f)]
    public float wallContactNormalDotThreshold = 0.55f;

    Rigidbody _rb;
    CapsuleCollider _cap;

    TrailRenderer _playerTrail;

    readonly RaycastHit[] _groundHits = new RaycastHit[GroundProbeHitBufferSize];

    // Last requested horizontal velocity from the player controller.
    Vector3 _wishPlanarVelocity;

    // Set in Update when Space is pressed; consumed in FixedUpdate so physics always sees the jump.
    bool _jumpPressed;

    // True after a high jump until we land; used only to soften downward acceleration (glide).
    bool _highJumpSlowFallActive;

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
        _playerTrail = GetComponent<TrailRenderer>();
        if (_rb == null)
        {
            Debug.LogError("PlayerMotor needs a Rigidbody.", this);
            enabled = false;
            return;
        }

        // We apply gravity manually along GravityWorld.Up so Unity's default gravity does not fight custom up.
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
        _highJumpSlowFallActive = false;
        if (_playerTrail != null)
            _playerTrail.emitting = false;
    }

    // Stores what the player wants for horizontal motion this frame (world space, along the walk plane).
    public void SetMoveVelocity(Vector3 planarWorldVelocity)
    {
        _wishPlanarVelocity = planarWorldVelocity;
    }

    // Adds upward impulse along GravityWorld.Up (regular jump + mid-air high jump). Starts slow-fall on the way down.
    public bool ApplyHighJumpImpulse()
    {
        if (_rb == null)
            return false;

        Vector3 up = GravityWorld.Up.normalized;
        float impulse = jumpForce * highJumpImpulseMultiplier;
        Vector3 vel = _rb.velocity;
        vel += up * impulse;
        _rb.velocity = vel;
        _highJumpSlowFallActive = true;
        if (_playerTrail != null)
            _playerTrail.emitting = true;
        return true;
    }

    // True if there is walkable ground under the feet.
    public bool IsGroundedForLogic()
    {
        Vector3 up = GravityWorld.Up.normalized;
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

        int count = Physics.RaycastNonAlloc(rayStart, -up, _groundHits, dist, ~0, QueryTriggerInteraction.Ignore);
        if (count <= 0)
            return false;

        float bestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = _groundHits[i];
            float nDot = Vector3.Dot(h.normal.normalized, up);
            if (nDot < minGroundNormalDotUp)
                continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                found = true;
            }
        }

        return found;
    }

    // Projects horizontal wish off wall normals using Physics.CapsuleCast.
    Vector3 SlideHorizontalAlongWalls(Vector3 horizontalWish, Vector3 up)
    {
        if (_cap == null || horizontalWish.sqrMagnitude < 1e-10f)
            return horizontalWish;

        Vector3 center = transform.TransformPoint(_cap.center);
        Vector3 axis = transform.up.normalized;
        float halfH = Mathf.Max(0.001f, _cap.height * 0.5f - _cap.radius);
        Vector3 p1 = center + axis * halfH;
        Vector3 p2 = center - axis * halfH;
        float r = _cap.radius * 0.99f;
        float probeDist = Mathf.Max(0.1f, _cap.radius * 0.35f);

        Vector3 dir = horizontalWish.normalized;
        for (int k = 0; k < 3; k++)
        {
            if (!Physics.CapsuleCast(p1, p2, r, dir, out RaycastHit hit, probeDist, ~0, QueryTriggerInteraction.Ignore))
                break;
            if (hit.collider != null && (hit.collider == _cap || hit.collider.transform.IsChildOf(transform)))
                break;
            if (Mathf.Abs(Vector3.Dot(hit.normal, up)) >= wallContactNormalDotThreshold)
                break;

            // Flatten normal to the walk plane
            Vector3 nFlat = Vector3.ProjectOnPlane(hit.normal, up);
            if (nFlat.sqrMagnitude < 1e-8f)
                break;
            nFlat.Normalize();

            float into = Vector3.Dot(horizontalWish, nFlat);
            if (into >= 0f)
                break;
            horizontalWish -= nFlat * into;
            horizontalWish = Vector3.ProjectOnPlane(horizontalWish, up);
            if (horizontalWish.sqrMagnitude < 1e-10f)
                break;
            dir = horizontalWish.normalized;
        }

        return Vector3.ProjectOnPlane(horizontalWish, up);
    }

    // Physics step: apply gravity, horizontal wish, jump, then write velocity to the Rigidbody.
    void FixedUpdate()
    {
        if (_rb == null)
            return;

        Vector3 up = GravityWorld.Up.normalized;
        Vector3 vel = _rb.velocity;

        // Softer gravity while falling after a high jump (glide). Ascent still uses full gravity.
        float gravScale = 1f;
        if (_highJumpSlowFallActive && Vector3.Dot(vel, up) < 0f)
            gravScale = highJumpSlowFallGravityScale;

        vel += Physics.gravity * gravScale * Time.fixedDeltaTime;

        bool grounded = IsGroundedForLogic();
        if (grounded)
        {
            _highJumpSlowFallActive = false;
            // Kill downward speed into the floor so we do not sink.
            if (_playerTrail != null)
                _playerTrail.emitting = false;
            float vUp = Vector3.Dot(vel, up);
            if (vUp < 0f)
                vel -= up * vUp;
        }

        // Replace horizontal part of velocity with wish; keep vertical speed for jump and fall.
        Vector3 horizontalWish = Vector3.ProjectOnPlane(_wishPlanarVelocity, up);
        horizontalWish = SlideHorizontalAlongWalls(horizontalWish, up);

        vel = horizontalWish + up * Vector3.Dot(vel, up);

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
