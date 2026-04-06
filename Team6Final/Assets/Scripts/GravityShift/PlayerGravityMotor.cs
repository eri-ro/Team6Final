using UnityEngine;

// CharacterController move + gravity/jump using GravityWorld.Up and Physics.gravity.
// Default game state: GravityWorld.Up is +Y and Physics.gravity is (0,-9.81,0).
public class PlayerGravityMotor : MonoBehaviour
{
    public float jumpForce = 6f;
    // CharacterController.isGrounded is unreliable on steep surfaces; ray along -GravityWorld.Up.
    public float groundProbeExtraDistance = 0.18f;

    CharacterController _cc;
    Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_cc != null)
            _cc.slopeLimit = 90f;
    }

    public void ClearVelocity()
    {
        _velocity = Vector3.zero;
    }

    public bool IsGroundedForLogic()
    {
        Vector3 up = GravityWorld.Up;
        if (_cc == null)
            return false;
        if (_cc.isGrounded)
            return true;

        Vector3 center = transform.position + transform.TransformVector(_cc.center);
        float half = _cc.height * 0.5f;
        Vector3 rayStart = center - up * (half - _cc.radius) + up * 0.02f;
        float dist = _cc.skinWidth * 2f + groundProbeExtraDistance;
        return Physics.Raycast(rayStart, -up, dist, ~0, QueryTriggerInteraction.Ignore);
    }

    // planarDelta is world-space walk input for this frame (e.g. moveSpeed * dir * Time.deltaTime).
    public void Tick(Vector3 planarDelta)
    {
        if (_cc == null)
            return;

        Vector3 up = GravityWorld.Up;
        bool grounded = IsGroundedForLogic();

        if (grounded && Input.GetButtonDown("Jump"))
            _velocity += up * jumpForce;

        _velocity += Physics.gravity * Time.deltaTime;
        if (grounded)
        {
            float vg = Vector3.Dot(_velocity, up);
            if (vg < 0f)
                _velocity -= up * vg;
        }

        _cc.Move(planarDelta + _velocity * Time.deltaTime);

        if (IsGroundedForLogic() && Vector3.Dot(_velocity, up) < 0f)
            _velocity = Vector3.ProjectOnPlane(_velocity, up);
    }
}
