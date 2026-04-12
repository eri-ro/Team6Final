using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAbility : MonoBehaviour
{
    // Declare variables
    
    BasicPlayerController playerController;     // Basic Player Controller script component
    CharacterController _cc;                    // The character controller component
    float dashBoost = 5f;                       // Speed multiplier
    float dashTime;                             // Dash duration
    bool isDashing;                             // If the player is dashing
    bool canBreakObstacles;                     // If the player can break obstacles with the dash

    // Awake is called when the script instance is being loaded
    void Awake()
    {
        playerController = GetComponent<BasicPlayerController>();
        _cc = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the player is on the ground while dashing. The player can only break obstacles while dashing on the ground.
        if (isDashing && _cc.isGrounded)
            canBreakObstacles = true;
        else
            canBreakObstacles = false;
    }

    public void UseAbility()
    {
        if (_cc == null || playerController == null)
            return;

        if (_cc.isGrounded)
        {
            dashTime = 0.5f;
            Debug.Log("Ground Dash!");
        }
        else if (!_cc.isGrounded)
        {
            dashTime = 0.5f;
            Debug.Log("Air Dash!");
        }
        StartDash(dashTime);
    }

    void StartDash(float duration)
    {
        IEnumerator DashRoutine = DashBoost(duration);
        StartCoroutine(DashRoutine);
    }

    /* leaving this code here incase we might need it
    void GroundDash()
    {
        dashTime = 0.5f;
        StartDash(dashTime);
    }

    void AirDash()
    {
        dashTime = 0.5f;
        StartDash(dashTime);
    }
    */

    IEnumerator DashBoost(float duration)
    {
        isDashing = true;
        playerController.moveSpeed *= dashBoost;
        yield return new WaitForSeconds(duration);
        playerController.moveSpeed /= dashBoost;
        isDashing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Breakable" && canBreakObstacles)
        {
            Destroy(other.gameObject);
        }
    }
}
