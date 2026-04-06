using UnityEngine;

// Moves the CharacterController using GravityWorld.Up for jump and falling
// Other scripts call Tick() each frame with how far to walk on the plane
public class PlayerGravityMotor : MonoBehaviour
{
    // How strong a jump is
    public float jumpForce = 6f;

    // Extra ray length past the capsule bottom to decide if we are really grounded on steep meshes
    public float groundProbeExtraDistance = 0.18f;

    CharacterController _cc;

    // Current velocity
    Vector3 _velocity;

    // Grab the CharacterController on the same GameObject
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    // Reset speed so old fall speed does not carry over
    public void ClearVelocity()
    {
        _velocity = Vector3.zero;
    }

    // True if Unity says grounded OR a short ray hits ground under the feet along -GravityWorld.Up
    public bool IsGroundedForLogic()
    {
        Vector3 up = GravityWorld.Up;
        if (_cc == null)
            return false;

        if (_cc.isGrounded)
            return true;

        // Ray from near the bottom of the capsule downward. helps on ramps and walls
        Vector3 center = transform.position + transform.TransformVector(_cc.center);
        float half = _cc.height * 0.5f;
        Vector3 rayStart = center - up * (half - _cc.radius) + up * 0.02f;
        float dist = _cc.skinWidth * 2f + groundProbeExtraDistance;
        return Physics.Raycast(rayStart, -up, dist, ~0, QueryTriggerInteraction.Ignore);
    }

    // Call once per frame: planarDelta is how far to walk along the surface (from WASD * speed * deltaTime)
    public void Tick(Vector3 planarDelta)
    {
        if (_cc == null)
            return;

        Vector3 up = GravityWorld.Up;
        bool grounded = IsGroundedForLogic();

        // Jump adds velocity along custom up
        if (grounded && Input.GetButtonDown("Jump"))
            _velocity += up * jumpForce;

        // Apply global gravity
        _velocity += Physics.gravity * Time.deltaTime;

        // On ground, do not keep sinking: remove downward part of velocity along up
        if (grounded)
        {
            float vg = Vector3.Dot(_velocity, up);
            if (vg < 0f)
                _velocity -= up * vg;
        }

        // Single CharacterController step: walk + gravity
        _cc.Move(planarDelta + _velocity * Time.deltaTime);

        // After move, if still grounded and falling, flatten velocity onto the floor plane
        if (IsGroundedForLogic() && Vector3.Dot(_velocity, up) < 0f)
            _velocity = Vector3.ProjectOnPlane(_velocity, up);
    }
}
