using UnityEngine;

// Runs only when PlayerController.enableGravityShift is true: yaw around GravityWorld.Up, pitch, orbit camera,
// and planar movement built with GravityAlignment (wall-walk presentation).
[DisallowMultipleComponent]
public class PlayerGravityOrientation : MonoBehaviour
{
    PlayerController _player;
    PlayerGravityMotor _motor;
    float _pitch;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        if (_player == null || !_player.enableGravityShift)
            return;

        Camera cam = _player.playerCamera;
        if (cam == null)
            return;

        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;
        Vector3 up = GravityWorld.Up;

        if (lookEnabled)
        {
            transform.Rotate(up, Input.GetAxis("Mouse X") * _player.lookSensitivity, Space.World);

            float mouseY = Input.GetAxis("Mouse Y") * _player.lookSensitivity;
            _pitch += _player.invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, _player.minPitch, _player.maxPitch);
        }

        Vector3 flatForward = GravityAlignment.FlattenOnSurface(transform.forward, up);
        Vector3 forward = flatForward.sqrMagnitude > 1e-8f ? flatForward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = GravityAlignment.FlattenOnSurface(transform.right, up);
        right.Normalize();

        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        Vector3 planarMove = (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")).normalized * (_player.moveSpeed * Time.deltaTime);
        _motor?.Tick(planarMove);

        Vector3 focus = transform.position + up * 0.9f;
        Vector3 camPos = focus - cameraLook * _player.cameraDistance + up * 0.15f;
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation((focus - camPos).normalized, up);
    }
}
