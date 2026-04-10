using System.Collections;
using UnityEngine;

// Temporary speed boost while dashing. Multiplies BasicPlayerController.moveSpeed for a short time.
// While dashing, planar direction is fixed at dash start — WASD does not steer until the dash ends.
// Put a trigger collider on the player if you want to break objects tagged Breakable while dashing on the ground.
public class DashAbility : MonoBehaviour
{
    BasicPlayerController playerController;

    PlayerGravityMotor _motor;

    float _successCooldownEndTime;

    // How many times stronger than normal speed the dash is.
    float dashBoost = 5f;

    float dashTime;

    bool isDashing;

    // Unit direction on the walk plane, captured when the dash starts (world space).
    Vector3 _lockedPlanarDirection;

    bool canBreakObstacles;

    void Awake()
    {
        playerController = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        bool grounded = _motor != null && _motor.IsGroundedForLogic();
        if (isDashing && grounded)
            canBreakObstacles = true;
        else
            canBreakObstacles = false;
    }

    // Returns true while dashing; outputs planar velocity = locked direction * current moveSpeed (already boosted).
    public bool TryGetLockedPlanarVelocity(out Vector3 planarVelocity)
    {
        if (!isDashing || playerController == null)
        {
            planarVelocity = default;
            return false;
        }

        planarVelocity = _lockedPlanarDirection * playerController.moveSpeed;
        return true;
    }

    public void UseAbility()
    {
        if (playerController == null)
            return;

        if (Time.time < _successCooldownEndTime)
            return;

        if (isDashing)
            return;

        dashTime = 0.5f;

        if (_motor != null && _motor.IsGroundedForLogic())
        {
            Debug.Log("Ground dash");
            BeginDash(dashTime);
        }
        else if (_motor != null && !_motor.IsGroundedForLogic())
        {
            Debug.Log("Air dash");
            BeginDash(dashTime);
        }
        else
        {
            Debug.Log("Dash!");
            BeginDash(dashTime);
        }
    }

    void BeginDash(float duration)
    {
        CaptureDashPlanarDirection();
        isDashing = true;
        _successCooldownEndTime = Time.time + playerController.abilitySuccessCooldownSeconds;
        playerController.moveSpeed *= dashBoost;
        StartCoroutine(EndDashAfterDelay(duration));
    }

    // Matches BasicPlayerController / PlayerGravityOrientation: forward/right on the walk plane from current aim + WASD.
    void CaptureDashPlanarDirection()
    {
        bool wallWalk = playerController != null && playerController.enableGravityShift;
        Vector3 up = wallWalk ? GravityWorld.Up.normalized : Vector3.up;

        Vector3 forward;
        Vector3 right;

        if (wallWalk)
        {
            Vector3 flatForward = GravityAlignment.FlattenOnSurface(transform.forward, up);
            forward = flatForward.sqrMagnitude > 1e-8f ? flatForward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            right = Vector3.Cross(up, forward);
            if (right.sqrMagnitude < 1e-8f)
                right = GravityAlignment.FlattenOnSurface(transform.right, up);
            right.Normalize();
        }
        else
        {
            forward = Vector3.ProjectOnPlane(transform.forward, up);
            if (forward.sqrMagnitude < 1e-8f)
                forward = Vector3.ProjectOnPlane(Vector3.forward, up);
            forward.Normalize();
            right = Vector3.Cross(up, forward).normalized;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = right * h + forward * v;
        if (dir.sqrMagnitude < 1e-6f)
            _lockedPlanarDirection = forward;
        else
            _lockedPlanarDirection = dir.normalized;
    }

    IEnumerator EndDashAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        playerController.moveSpeed /= dashBoost;
        isDashing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Breakable" && canBreakObstacles)
            Destroy(other.gameObject);
    }
}
