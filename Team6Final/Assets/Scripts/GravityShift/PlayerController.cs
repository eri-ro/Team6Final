using UnityEngine;

// First-person movement + look + orbit camera. Works alone with a CharacterController (world +Y, Physics.gravity).
// Optional: PlayerGravityMotor for shared gravity/jump logic; PlayerGravityOrientation when enableGravityShift is true.
public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public float moveSpeed = 6f;
    public float lookSensitivity = 2f;
    public float cameraDistance = 4.5f;
    public float minPitch = -75f;
    public float maxPitch = 75f;
    public bool invertVerticalLook = true;
    public bool enableGravityShift = false;

    public float jumpForce = 6f;

    PlayerGravityMotor _motor;
    PlayerGravityOrientation _gravityOrient;
    CharacterController _cc;
    Vector3 _standaloneVelocity;
    float _pitch;
    bool _loggedMissingOrient;

    void Awake()
    {
        _motor = GetComponent<PlayerGravityMotor>();
        _gravityOrient = GetComponent<PlayerGravityOrientation>();
        _cc = GetComponent<CharacterController>();
        if (_motor == null && _cc != null)
            _cc.slopeLimit = 90f;
    }

    void Start()
    {
        SetCursorLocked(true);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);

        if (enableGravityShift)
        {
            if (_gravityOrient == null && !_loggedMissingOrient)
            {
                _loggedMissingOrient = true;
                Debug.LogError("enableGravityShift is true but PlayerGravityOrientation is missing; add it or disable the flag.", this);
            }
            return;
        }

        if (playerCamera == null)
            return;

        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;
        Vector3 up = Vector3.up;

        if (lookEnabled)
        {
            transform.Rotate(up, Input.GetAxis("Mouse X") * lookSensitivity, Space.World);

            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch += invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 1e-8f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        forward.Normalize();
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        Vector3 planarMove = (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")).normalized * (moveSpeed * Time.deltaTime);
        TickMovement(planarMove);

        Vector3 focus = transform.position + up * 0.9f;
        Vector3 camPos = focus - cameraLook * cameraDistance + up * 0.15f;
        playerCamera.transform.position = camPos;
        playerCamera.transform.rotation = Quaternion.LookRotation((focus - camPos).normalized, up);
    }

    void TickMovement(Vector3 planarDelta)
    {
        if (_motor != null)
        {
            _motor.Tick(planarDelta);
            return;
        }

        if (_cc == null)
            return;

        Vector3 up = Vector3.up;
        bool grounded = _cc.isGrounded;

        if (grounded && Input.GetButtonDown("Jump"))
            _standaloneVelocity += up * jumpForce;

        _standaloneVelocity += Physics.gravity * Time.deltaTime;
        if (grounded)
        {
            float vg = Vector3.Dot(_standaloneVelocity, up);
            if (vg < 0f)
                _standaloneVelocity -= up * vg;
        }

        _cc.Move(planarDelta + _standaloneVelocity * Time.deltaTime);

        if (_cc.isGrounded && Vector3.Dot(_standaloneVelocity, up) < 0f)
            _standaloneVelocity = Vector3.ProjectOnPlane(_standaloneVelocity, up);
    }
}
