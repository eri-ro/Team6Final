using UnityEngine;

// High jump: one big upward boost (about 2x the height of a normal jump), usable in mid-air after a regular jump.
// After you use it, you cannot use it again until you land. While falling from that jump, gravity is reduced for a short glide.
public class HighJumpAbility : MonoBehaviour
{
    BasicPlayerController _player;
    PlayerGravityMotor _motor;

    float _successCooldownEndTime;

    // After a high jump we stay locked until the player touches ground again.
    bool _airborneFromHighJump;

    // Previous frame was not grounded — used so we only clear the lock when landing, not while still on the ground after takeoff.
    bool _wasAirborneLastFrame;

    void Awake()
    {
        _player = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        if (_motor == null)
            return;

        bool grounded = _motor.IsGroundedForLogic();

        // Landed after being in the air: unlock high jump for the next use.
        if (grounded && _wasAirborneLastFrame)
            _airborneFromHighJump = false;

        _wasAirborneLastFrame = !grounded;
    }

    // Called from BasicPlayerController when HighJump is selected and the ability key is pressed.
    public void UseAbility()
    {
        if (_motor == null)
            return;

        if (Time.time < _successCooldownEndTime)
            return;

        bool grounded = _motor.IsGroundedForLogic();

        // Still in the air from a previous high jump — no second use until landing.
        if (_airborneFromHighJump && !grounded)
            return;

        if (!_motor.ApplyHighJumpImpulse())
            return;

        float cd = _player != null ? _player.abilitySuccessCooldownSeconds : 1f;
        _successCooldownEndTime = Time.time + cd;

        _airborneFromHighJump = true;
    }
}
