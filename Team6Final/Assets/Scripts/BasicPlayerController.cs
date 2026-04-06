using UnityEngine;

public class BasicPlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public float moveSpeed = 6f;
    public float lookSensitivity = 2f;
    public float cameraDistance = 4.5f;
    public float focusHeight = 0.9f;
    public float cameraHeightBias = 0.15f;
    public float jumpForce = 6f;
    public float minPitch = -75f;
    public float maxPitch = 75f;
    public bool invertVerticalLook;

    CharacterController _cc;
    Vector3 _velocity;
    float _pitch;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (playerCamera == null || _cc == null)
            return;

        Vector3 up = Vector3.up;
        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 planar = (right * h + forward * v).normalized * (moveSpeed * Time.deltaTime);

        bool grounded = _cc.isGrounded;
        if (grounded && Input.GetButtonDown("Jump"))
            _velocity.y = jumpForce;

        _velocity.y += Physics.gravity.y * Time.deltaTime;
        if (grounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _cc.Move(planar + _velocity * Time.deltaTime);

        Vector3 focus = transform.position + up * focusHeight;
        Vector3 camPos = focus - cameraLook * cameraDistance + up * cameraHeightBias;
        playerCamera.transform.position = camPos;
        playerCamera.transform.rotation = Quaternion.LookRotation((focus - camPos).normalized, up);
    }
}
