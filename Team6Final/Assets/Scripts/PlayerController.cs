using UnityEngine;
using UnityEngine.SceneManagement;

// Main player script: mouse look, third-person camera, picking abilities, and sending move speed to the motor.
// Walk input and camera use GravityWorld.ControlUp (smooth after a shift). Physics uses GravityWorld.Up immediately.
public class PlayerController : MonoBehaviour
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

    // Legacy field: jump is handled inside PlayerMotor; kept for designers who might script against it.
    public float jumpForce = 6f;

    // Limits for looking up and down in degrees so you cannot flip upside down.
    public float minPitch = -75f;
    public float maxPitch = 75f;

    // If true, moving the mouse up makes the view look down (flight-sim style).
    public bool invertVerticalLook = true;

    // Mouse button or key that fires the currently selected ability.
    public KeyCode abilityKey = KeyCode.Mouse0;

    [Tooltip("Seconds after a successful Dash, High Jump, or Gravity Shift before that same ability can be used again.")]
    public float abilitySuccessCooldownSeconds = 1f;

    [Tooltip("How fast ControlUp (camera / WASD framing) catches up to physics up after a gravity shift. Higher = snappier.")]
    public float gravityControlUpAlignSpeed = 12f;

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

    bool _loggedMissingMotor;

    PlayerMotor _motor;
    PlayerGravityShift _gravityShift;
    DashAbility _dash;
    HighJumpAbility _highJump;

    AbilitySoundController _abilitySounds;

    // Grab references to other components on this GameObject.
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _motor = GetComponent<PlayerMotor>();
        _gravityShift = GetComponent<PlayerGravityShift>();
        _dash = GetComponent<DashAbility>();
        _highJump = GetComponent<HighJumpAbility>();
        _abilitySounds = GetComponent<AbilitySoundController>();
    }

    // Lock the cursor for mouse-look. Match world +Y.
    void Start()
    {
        GravityWorld.ResetToDefaultWorld();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // UseAbility before TickControlUpAlignment so a gravity shift updates physics Up the same frame ControlUp begins blending toward it.
    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.Tab))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
        UseAbility();
        GravityWorld.TickControlUpAlignment(Time.deltaTime, gravityControlUpAlignSpeed);
        Move();
        ChangeAbility();
    }

    // Orbit camera after movement; avoids jitter when physics and render rates differ.
    void LateUpdate()
    {
        UpdateOrbitCamera();
    }

    // Handles Escape, mouse look, WASD as a desired velocity, and move intent for the motor.
    void Move()
    {
        //// Escape toggles whether the cursor is locked.
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    bool locked = Cursor.lockState == CursorLockMode.Locked;
        //    Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        //    Cursor.visible = locked;
        //}

        if (playerCamera == null || _rb == null)
            return;

        if (_motor == null && !_loggedMissingMotor)
        {
            _loggedMissingMotor = true;
            Debug.LogError("Add PlayerMotor (and CapsuleCollider) for movement, or this player will not move.", this);
        }

        Vector3 controlUp = GravityWorld.ControlUp.normalized;
        Vector3 physicsUp = GravityWorld.Up.normalized;

        // Only rotate the camera when the cursor is locked.
        bool lookEnabled = Cursor.lockState == CursorLockMode.Locked;

        if (lookEnabled)
        {
            transform.Rotate(controlUp, Input.GetAxis("Mouse X") * lookSensitivity, Space.World);
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch += invertVerticalLook ? -mouseY : mouseY;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        GravityAlignment.GetWalkForwardRight(transform, controlUp, out Vector3 forward, out Vector3 right);

        // Apply pitch around the right axis to get the direction the camera should look along.
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        Vector3 wishVel;
        // During dash, direction is fixed at dash start — WASD does not steer until the dash ends.
        if (_dash != null && _dash.TryGetLockedPlanarVelocity(out Vector3 lockedDashVel))
            wishVel = lockedDashVel;
        else
        {
            // Raw axes are -1, 0, or 1 with no smoothing (good for snappy keyboard input).
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            wishVel = (right * h + forward * v).normalized * moveSpeed;
        }

        // Motor splits velocity along physics Up; keep horizontal wish on that plane while ControlUp blends.
        wishVel = Vector3.ProjectOnPlane(wishVel, physicsUp);
        if (wishVel.sqrMagnitude > 1e-8f)
            wishVel = wishVel.normalized * moveSpeed;

        // Tell the motor the desired horizontal speed; the motor runs in FixedUpdate and applies the Rigidbody.
        if (_motor != null)
            _motor.SetMoveVelocity(wishVel);
    }

    void UpdateOrbitCamera()
    {
        if (playerCamera == null || _rb == null)
            return;

        Vector3 controlUp = GravityWorld.ControlUp.normalized;
        GravityAlignment.GetWalkForwardRight(transform, controlUp, out Vector3 forward, out Vector3 right);
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, right);
        Vector3 cameraLook = (pitchRot * forward).normalized;

        Vector3 focus = transform.position + controlUp * focusHeight;
        PlayerOrbitCamera.Place(
            playerCamera,
            transform,
            focus,
            cameraLook,
            controlUp,
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

        bool success = false;

        switch (ability)
        {
            case AbilityState.Dash:
                if (_dash != null)
                    success = _dash.UseAbility();
                break;

            case AbilityState.HighJump:
                if (_highJump != null)
                    success = _highJump.UseAbility();
                break;

            case AbilityState.GravityShift:
                if (_gravityShift != null)
                    success = _gravityShift.TryExecuteShift();
                break;

            default:
                break;
        }

        if (success && _abilitySounds != null)
            _abilitySounds.PlayAbilitySound(ability);
    }

    // Simple keyboard hotkeys to change which ability is selected.
    void ChangeAbility()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ability = AbilityState.Dash;
            Debug.Log("Dash Selected");
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            ability = AbilityState.HighJump;
            Debug.Log("High Jump Selected");
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ability = AbilityState.GravityShift;
            Debug.Log("Gravity Shift Selected");
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            ability = AbilityState.None;
            Debug.Log("None Selected");
        }
    }

    public void ChangeAbility(int abilityValue)
    {
        switch (abilityValue)
        {
            case 0:
                ability = AbilityState.None;
                break;
            case 1:
                ability = AbilityState.Dash;
                break;
            case 2:
                ability = AbilityState.HighJump;
                break;
            case 3:
                ability = AbilityState.GravityShift;
                break;
        }
    }
}
