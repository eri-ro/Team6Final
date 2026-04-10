using UnityEngine;

// When BasicPlayerController.enableGravityShift is true, this runs instead of BasicPlayerController.Move.
// It uses GravityWorld.Up for yaw, pitch, orbit camera, and sends walk velocity to PlayerGravityMotor.
[DisallowMultipleComponent]
public class PlayerGravityOrientation : MonoBehaviour
{
    BasicPlayerController _player;
    PlayerGravityMotor _motor;
    DashAbility _dash;

    // Up/down look angle in degrees, stored between frames.
    float _pitch;

    void Awake()
    {
        _player = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
        _dash = GetComponent<DashAbility>();
    }

    void Update()
    {
        if (_player == null || !_player.enableGravityShift)
            return;

        Camera cam = _player.playerCamera;
        if (cam == null)
            return;

        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

        // After a gravity shift, "up" is not always Vector3.up.
        Vector3 up = GravityWorld.Up;

        if (lookEnabled)
        {
            // Yaw around the current gravity up axis.
            transform.Rotate(up, Input.GetAxis("Mouse X") * _player.lookSensitivity, Space.World);

            float mouseY = Input.GetAxis("Mouse Y") * _player.lookSensitivity;
            _pitch += _player.invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, _player.minPitch, _player.maxPitch);
        }

        // Build walk directions that lie flat on the surface you are standing on.
        Vector3 flatForward = GravityAlignment.FlattenOnSurface(transform.forward, up);
        Vector3 forward = flatForward.sqrMagnitude > 1e-8f ? flatForward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = GravityAlignment.FlattenOnSurface(transform.right, up);
        right.Normalize();

        // Orbit camera direction: pitch around the sideways axis.
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        Vector3 wishVel;
        // During dash, movement direction is locked — same as BasicPlayerController flat mode.
        if (_dash != null && _dash.TryGetLockedPlanarVelocity(out Vector3 lockedDashVel))
            wishVel = lockedDashVel;
        else
            wishVel = (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")).normalized * _player.moveSpeed;

        _motor?.SetMoveVelocity(wishVel);

        // Third-person camera with wall collision.
        Vector3 focus = transform.position + up * _player.focusHeight;
        PlayerOrbitCamera.Place(
            cam,
            transform,
            focus,
            cameraLook,
            up,
            _player.cameraDistance,
            _player.cameraHeightBias,
            _player.cameraCollisionMinDistance,
            _player.cameraCollisionWallPadding,
            _player.cameraCollisionCastStart,
            _player.cameraObstacleMask);
    }
}
