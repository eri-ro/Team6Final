using System.Collections;
using UnityEngine;

// Temporary speed boost while dashing. Multiplies PlayerController.moveSpeed for a short time.
// While dashing, planar direction is fixed at dash start — WASD does not steer until the dash ends.
// Put a trigger collider on the player if you want to break objects tagged Breakable while dashing on the ground.
public class DashAbility : MonoBehaviour
{
    PlayerController playerController;

    PlayerMotor _motor;

    float _successCooldownEndTime;

    // How many times stronger than normal speed the dash is.
    public float dashBoost = 5f;

    float dashTime;

    bool isDashing;

    // Unit direction on the walk plane, captured when the dash starts (world space).
    Vector3 _lockedPlanarDirection;

    [SerializeField]
    ParticleSystem _dashParticleSystem;

    [SerializeField]
    int _dashParticleEmissinon = 10;

    TrailRenderer _playerTrail;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        _motor = GetComponent<PlayerMotor>();
        _playerTrail = GetComponent<TrailRenderer>();
    }

    void Update()
    {
        
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
            Debug.Log("Ground Dash!");
            BeginDash(dashTime);
        }
        else if (_motor != null && !_motor.IsGroundedForLogic())
        {
            Debug.Log("Air Dash!");
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
        //Emit Particles
        _dashParticleSystem.Emit(_dashParticleEmissinon);
        //Enable Trail and change its color
        _playerTrail.emitting = true;
        playerController.moveSpeed *= dashBoost;
        StartCoroutine(EndDashAfterDelay(duration));
    }

    // Same framing as PlayerController (ControlUp), locked direction on the physics Up plane for the motor.
    void CaptureDashPlanarDirection()
    {
        Vector3 physicsUp = GravityWorld.Up.normalized;
        Vector3 controlUp = GravityWorld.ControlUp.normalized;
        GravityAlignment.GetWalkForwardRight(transform, controlUp, out Vector3 forward, out Vector3 right);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = right * h + forward * v;
        if (dir.sqrMagnitude < 1e-6f)
            dir = forward;
        else
            dir = dir.normalized;

        dir = Vector3.ProjectOnPlane(dir, physicsUp);
        if (dir.sqrMagnitude < 1e-8f)
            dir = Vector3.ProjectOnPlane(forward, physicsUp);
        _lockedPlanarDirection = dir.normalized;
    }

    IEnumerator EndDashAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        playerController.moveSpeed /= dashBoost;
        isDashing = false;
        //Disable Player Trail
        _playerTrail.emitting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Breakable" && isDashing)
            Destroy(other.gameObject);
    }
}
