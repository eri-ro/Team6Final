using UnityEngine;

// Main player script: walking, jumping, mouse look, third-person camera, and one "use ability" button
// If enableGravityShift is true, this script stops moving the player and PlayerGravityOrientation + PlayerGravityMotor take over
public class BasicPlayerController : MonoBehaviour
{
    // Drag the scene camera here
    public Camera playerCamera;

    // How fast the player walks
    public float moveSpeed = 6f;

    // How strong mouse movement rotates the view
    public float lookSensitivity = 2f;

    // How far the orbit camera sits behind the focus point
    [Range(1f, 20f)]
    public float cameraDistance = 6f;

    // Height of the point the camera looks at, above the player's position
    public float focusHeight = 0.9f;

    // Small vertical offset so the camera is not exactly on the same height as the focus
    public float cameraHeightBias = 0.15f;

    // Upward push when jumping
    public float jumpForce = 6f;

    // Limits for looking up/down (degrees)
    public float minPitch = -75f;
    public float maxPitch = 75f;

    // If true moving the mouse up looks down
    public bool invertVerticalLook = true;

    // Turn on for levels or upgrades that allow walking on walls / shifting gravity
    public bool enableGravityShift = false;

    // Which key fires the currently selected ability (dash, gravity shift, etc.)
    public KeyCode abilityKey = KeyCode.Mouse0;

    // Which ability is active when you press the ability key
    public enum AbilityState
    {
        None,         // Ability button does nothing special
        Dash,         // Handled by DashAbility
        HighJump,     // Placeholder for a future script
        GravityShift  // Calls PlayerGravityShift.TryExecuteShift()
    }

    // Current ability (change with Z/X/C/V in ChangeAbility)
    public AbilityState ability;

    // Unity's built-in character controlelr 
    CharacterController _cc;

    // Fall speed when we are not using PlayerGravityMotor
    Vector3 _standaloneVelocity;

    // Vertical look angle in degrees
    float _pitch;

    // So we only print the "missing orientation" error once
    bool _loggedMissingOrient;

    // Cached ability components 
    PlayerGravityMotor _motor;
    PlayerGravityOrientation _orient;
    PlayerGravityShift _gravityShift;
    DashAbility _dash;

    // Runs once when the object loads. grabs references to other components
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _motor = GetComponent<PlayerGravityMotor>();
        _orient = GetComponent<PlayerGravityOrientation>();
        _gravityShift = GetComponent<PlayerGravityShift>();
        _dash = GetComponent<DashAbility>();
    }

    // First frame hide and lock the mouse
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Every frame movement, then abilities
    void Update()
    {
        Move();
        UseAbility();
        ChangeAbility();
    }

    // Handles escape key, mouse look, WASD, camera orbit
    void Move()
    {
        // Escape toggles cursor lock so you can click UI or leave the game window.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // Wall-walk mode: BasicPlayerController does not move or aim here. another script handles it
        if (enableGravityShift)
        {
            if (_orient == null && !_loggedMissingOrient)
            {
                _loggedMissingOrient = true;
                Debug.LogError("enableGravityShift is true but PlayerGravityOrientation is missing.", this);
            }
            return;
        }

        if (playerCamera == null || _cc == null)
            return;

        // Normal levels use world Y as up
        Vector3 up = Vector3.up;

        // Only rotate the camera when the cursor is locked
        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

        if (lookEnabled)
        {
            // Mouse X spins around the world's up axis
            transform.Rotate(up, Input.GetAxis("Mouse X") * lookSensitivity, Space.World);

            // Mouse Y changes pitch (up/down look), clamped so you cannot flip upside down
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch += invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Forward on the ground plane (ignore tilt so WASD stays on the floor)
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 1e-8f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        forward.Normalize();

        // Right is perpendicular to up and forward
        Vector3 right = Vector3.Cross(up, forward).normalized;

        // Apply pitch around the right axis to get the direction from camera toward the focus
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        // WASD from Input Manager
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 planar = (right * h + forward * v).normalized * (moveSpeed * Time.deltaTime);

        // Actually move the CharacterController
        TickMovement(planar);

        // Place the camera behind the player
        Vector3 focus = transform.position + up * focusHeight;
        Vector3 camPos = focus - cameraLook * cameraDistance + up * cameraHeightBias;
        playerCamera.transform.position = camPos;
        playerCamera.transform.rotation = Quaternion.LookRotation((focus - camPos).normalized, up);
    }

    // Applies horizontal motion plus jump/gravity for this frame
    void TickMovement(Vector3 planarDelta)
    {
        // If PlayerGravityMotor exists, it owns velocity, jump, and gravity along GravityWorld.Up
        if (_motor != null)
        {
            _motor.Tick(planarDelta);
            return;
        }

        if (_cc == null)
            return;

        Vector3 up = Vector3.up;
        bool grounded = _cc.isGrounded;

        // Jump only when touching the ground
        if (grounded && Input.GetButtonDown("Jump"))
            _standaloneVelocity += up * jumpForce;

        // Accelerate downward using the global gravity
        _standaloneVelocity += Physics.gravity * Time.deltaTime;

        // When grounded cancel downward velocity so we do not sink into the floor
        if (grounded)
        {
            float vg = Vector3.Dot(_standaloneVelocity, up);
            if (vg < 0f)
                _standaloneVelocity -= up * vg;
        }

        // One Move call: sideways from input, vertical from _standaloneVelocity
        _cc.Move(planarDelta + _standaloneVelocity * Time.deltaTime);

        // After landing remove any leftover downward speed along up
        if (_cc.isGrounded && Vector3.Dot(_standaloneVelocity, up) < 0f)
            _standaloneVelocity = Vector3.ProjectOnPlane(_standaloneVelocity, up);
    }

    // True on the frame the player presses the ability key or gamepad
    bool AbilityTriggerDown()
    {
        return Input.GetKeyDown(abilityKey) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    // Routes the ability key to the correct script based on the current AbilityState
    void UseAbility()
    {
        if (!AbilityTriggerDown())
            return;

        switch (ability)
        {
            case AbilityState.Dash:
                _dash?.UseAbility();
                break;
            case AbilityState.HighJump:
                // Hook up a high-jump script here later.
                break;
            case AbilityState.GravityShift:
                _gravityShift?.TryExecuteShift();
                break;
            default:
                break;
        }
    }

    // keyboard hotkeys to swap abilities
    void ChangeAbility()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            ability = AbilityState.Dash;
        if (Input.GetKeyDown(KeyCode.X))
            ability = AbilityState.HighJump;
        if (Input.GetKeyDown(KeyCode.C))
            ability = AbilityState.GravityShift;
        if (Input.GetKeyDown(KeyCode.V))
            ability = AbilityState.None;
    }
}
