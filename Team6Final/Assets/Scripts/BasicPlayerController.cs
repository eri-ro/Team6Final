using UnityEngine;

// Main player script: mouse look, third-person camera, picking abilities, and sending move speed to the motor.
// When enableGravityShift is true, this script skips movement here and PlayerGravityOrientation does it instead.
public class BasicPlayerController : MonoBehaviour
{
    // The camera that follows the player.
    public Camera playerCamera;

    // How fast the player moves in units per second.
    public float moveSpeed = 6f;

    // Mouse sensitivity for turning and looking up/down.
    public float lookSensitivity = 2f;

    // Default distance from the focus point to the camera along the view ray.
    [Range(1f, 20f)]
    public float cameraDistance = 6f;

    // How far above the player root the camera looks (roughly chest/head height).
    public float focusHeight = 0.9f;

    // Tiny height tweak so the camera is not exactly on the same line as the focus.
    public float cameraHeightBias = 0.15f;

    // When a wall blocks the camera, it can pull in this close (almost first person).
    public float cameraCollisionMinDistance = 0.12f;

    // Extra space left between the camera and the wall hit so the lens does not clip.
    public float cameraCollisionWallPadding = 0.12f;

    // Start the wall ray a bit toward the camera from the focus so the ray does not start inside the player capsule.
    public float cameraCollisionCastStart = 0.35f;

    // Which physics layers count as solid for camera collision (default: everything).
    public LayerMask cameraObstacleMask = ~0;

    // Legacy field: jump is handled inside PlayerGravityMotor; kept for designers who might script against it.
    public float jumpForce = 6f;

    // Limits for looking up and down in degrees so you cannot flip upside down.
    public float minPitch = -75f;
    public float maxPitch = 75f;

    // If true, moving the mouse up makes the view look down (flight-sim style).
    public bool invertVerticalLook = true;

    // When true, wall-walk mode is on and PlayerGravityOrientation must exist on the same object.
    public bool enableGravityShift = false;

    // Mouse button or key that fires the currently selected ability.
    public KeyCode abilityKey = KeyCode.Mouse0;

    // Which ability is active when you press abilityKey (change with Z/X/C/V).
    public enum AbilityState
    {
        None,
        Dash,
        HighJump,
        GravityShift
    }

    public AbilityState ability;

    // Cached Rigidbody on this player.
    Rigidbody _rb;

    // Current up/down look angle in degrees.
    float _pitch;

    // Only log missing orientation script once so the Console is not spammed.
    bool _loggedMissingOrient;
    bool _loggedMissingMotor;

    PlayerGravityMotor _motor;
    PlayerGravityOrientation _orient;
    PlayerGravityShift _gravityShift;
    DashAbility _dash;

    // Grab references to other components on this GameObject.
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _motor = GetComponent<PlayerGravityMotor>();
        _orient = GetComponent<PlayerGravityOrientation>();
        _gravityShift = GetComponent<PlayerGravityShift>();
        _dash = GetComponent<DashAbility>();
    }

    // Lock the cursor for mouse-look when the scene starts.
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Every frame: movement and camera, then abilities, then hotkeys to change ability.
    void Update()
    {
        Move();
        UseAbility();
        ChangeAbility();
    }

    // Handles Escape, mouse look, WASD as a desired velocity, and third-person camera placement.
    void Move()
    {
        // Escape toggles whether the cursor is locked.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // In gravity-shift mode we do not move here; PlayerGravityOrientation handles aim and the motor.
        if (enableGravityShift)
        {
            if (_orient == null && !_loggedMissingOrient)
            {
                _loggedMissingOrient = true;
                Debug.LogError("enableGravityShift is true but PlayerGravityOrientation is missing.", this);
            }
            return;
        }

        if (playerCamera == null || _rb == null)
            return;

        if (_motor == null && !_loggedMissingMotor)
        {
            _loggedMissingMotor = true;
            Debug.LogError("Add PlayerGravityMotor (and CapsuleCollider) for movement, or this player will not move.", this);
        }

        // Normal levels use world +Y as up.
        Vector3 up = Vector3.up;

        // Only rotate the camera when the cursor is locked.
        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

        if (lookEnabled)
        {
            // Mouse X spins the body around world up.
            transform.Rotate(up, Input.GetAxis("Mouse X") * lookSensitivity, Space.World);
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch += invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        // Forward on the horizontal plane (ignore tilt so WASD stays on the floor).
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, up);
        if (forward.sqrMagnitude < 1e-8f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        forward.Normalize();

        // Right vector for strafe.
        Vector3 right = Vector3.Cross(up, forward).normalized;

        // Apply pitch around the right axis to get the direction the camera should look along.
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        // Raw axes are -1, 0, or 1 with no smoothing (good for snappy keyboard input).
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 wishVel = (right * h + forward * v).normalized * moveSpeed;

        // Tell the motor the desired horizontal speed; the motor runs in FixedUpdate and applies the Rigidbody.
        if (_motor != null)
            _motor.SetMoveVelocity(wishVel);

        // Place the orbit camera behind the player, pulling in if a wall is in the way.
        Vector3 focus = transform.position + up * focusHeight;
        PlayerOrbitCamera.Place(
            playerCamera,
            transform,
            focus,
            cameraLook,
            up,
            cameraDistance,
            cameraHeightBias,
            cameraCollisionMinDistance,
            cameraCollisionWallPadding,
            cameraCollisionCastStart,
            cameraObstacleMask);
    }

    // True on the frame the player presses the ability key.
    bool AbilityTriggerDown()
    {
        return Input.GetKeyDown(abilityKey) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    // Calls the script that matches the current ability slot.
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
                // Hook up a high-jump script here.
                break;
            case AbilityState.GravityShift:
                _gravityShift?.TryExecuteShift();
                break;
            default:
                break;
        }
    }

    // Simple keyboard hotkeys to change which ability is selected.
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
