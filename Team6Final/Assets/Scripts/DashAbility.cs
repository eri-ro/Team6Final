using System.Collections;
using UnityEngine;

// Temporary speed boost while dashing. Multiplies BasicPlayerController.moveSpeed for a short time.
// Put a trigger collider on the player if you want to break objects tagged Breakable while dashing on the ground.
public class DashAbility : MonoBehaviour
{
    // Used to read and change move speed during the dash.
    BasicPlayerController playerController;

    // Used to know if the player is on the ground.
    PlayerGravityMotor _motor;

    // How many times stronger than normal speed the dash is.
    float dashBoost = 5f;

    // How long one dash lasts in seconds.
    float dashTime;

    // True while the coroutine is running so we know we are in the dash window.
    bool isDashing;

    // True when the cooldown is ended
    bool canDash = true;

    // Time before the ability can be triggered again
    float coolDown = 2f;

    void Awake()
    {
        playerController = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        // Ground check: same helper the motor uses for jump / gravity.
        bool grounded = _motor != null && _motor.IsGroundedForLogic();
        if (!canDash && grounded)
            StartCoroutine(DashCooldown());
    }

    // Called from BasicPlayerController when Dash is the selected ability and the ability key is pressed.
    public void UseAbility()
    {
        if (playerController == null)
            return;

        if (_motor != null && _motor.IsGroundedForLogic())
        {
            dashTime = 0.5f;
            Debug.Log("Ground Dash!");
        }
        else if (_motor != null && !_motor.IsGroundedForLogic())
        {
            dashTime = 0.5f;
            Debug.Log("Air Dash!");
        }
        else
        {
            dashTime = 0.5f;
            Debug.Log("Dash!");
        }

        StartDash(dashTime);
    }

    // Starts the timed speed boost coroutine.
    void StartDash(float duration)
    {
        if (canDash)
            StartCoroutine(DashBoost(duration));
    }

    // Waits for duration seconds, then restores move speed. While running, isDashing is true.
    IEnumerator DashBoost(float duration)
    {
        canDash = false;
        isDashing = true;
        playerController.controlLock = true;
        playerController.moveSpeed *= dashBoost;
        yield return new WaitForSeconds(duration);
        playerController.moveSpeed /= dashBoost;
        isDashing = false;
        playerController.controlLock = false;
        //StartCoroutine(DashCooldown());
    }

    // If another object has a trigger collider and the Breakable tag, destroy it when we dash into it on the ground.
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Breakable" && isDashing)
        {
            Destroy(other.gameObject);
        }
    }

    IEnumerator DashCooldown()
    {
        //while (_motor.IsGroundedForLogic())
        {
            yield return new WaitForSeconds(coolDown);
            canDash = true;
            //Debug.Log("Dash Cooldown Reset");
        }
    }
}
