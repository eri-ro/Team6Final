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

    // True only when dashing and grounded so breakable objects can be destroyed on trigger.
    bool canBreakObstacles;

    void Awake()
    {
        playerController = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        // Ground check: same helper the motor uses for jump / gravity.
        bool grounded = _motor != null && _motor.IsGroundedForLogic();
        if (isDashing && grounded)
            canBreakObstacles = true;
        else
            canBreakObstacles = false;
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
        StartCoroutine(DashBoost(duration));
    }

    // Waits for duration seconds, then restores move speed. While running, isDashing is true.
    IEnumerator DashBoost(float duration)
    {
        isDashing = true;
        playerController.moveSpeed *= dashBoost;
        yield return new WaitForSeconds(duration);
        playerController.moveSpeed /= dashBoost;
        isDashing = false;
    }

    // If another object has a trigger collider and the Breakable tag, destroy it when we dash into it on the ground.
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Breakable" && canBreakObstacles)
        {
            Destroy(other.gameObject);
        }
    }
}
