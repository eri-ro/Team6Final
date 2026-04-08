using UnityEngine;

// When BasicPlayerController.enableGravityShift is true, this script runs instead of BasicPlayerController.Move
// It rotates the player around GravityWorld.Up, handles mouse pitch, orbit camera, and sends walk input to the motor
[DisallowMultipleComponent]
public class PlayerGravityOrientation : MonoBehaviour
{
    BasicPlayerController _player;
    PlayerGravityMotor _motor;

    // Stored vertical look angle between frames (degrees)
    float _pitch;

    void Awake()
    {
        _player = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        // No player script or gravity mode off: do nothing
        if (_player == null || !_player.enableGravityShift)
            return;

        Camera cam = _player.playerCamera;
        if (cam == null)
            return;

        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

        // Up comes from GravityWorld, not always Vector3.up
        Vector3 up = GravityWorld.Up;

        if (lookEnabled)
        {
            // Spin around the current gravity up axis
            transform.Rotate(up, Input.GetAxis("Mouse X") * _player.lookSensitivity, Space.World);

            float mouseY = Input.GetAxis("Mouse Y") * _player.lookSensitivity;
            _pitch += _player.invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, _player.minPitch, _player.maxPitch);
        }

        // Build forward/right flat on the surface you are standing on
        Vector3 flatForward = GravityAlignment.FlattenOnSurface(transform.forward, up);
        Vector3 forward = flatForward.sqrMagnitude > 1e-8f ? flatForward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = GravityAlignment.FlattenOnSurface(transform.right, up);
        right.Normalize();

        // Pitch the view around the sideways axis for orbit camera direction
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        // Same WASD input as flat mode, but axes follow the wall
        Vector3 planarMove = (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")).normalized * (_player.moveSpeed * Time.deltaTime);
        _motor?.Tick(planarMove);

        // Third-person camera with wall collision (matches BasicPlayerController)
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
