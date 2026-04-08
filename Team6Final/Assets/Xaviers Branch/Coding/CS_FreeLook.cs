using UnityEngine;

// Third-person camera that stays oriented to the player's gravity direction
// so it works on planets, cubes, whatever
public class CS_FreeLook : MonoBehaviour
{
    [Header("Targets")]
    public Transform meshTarget;  // the actual player mesh to follow and look at
    public Transform playerRoot;  // the root object with the controller on it

    [Header("Camera Settings")]
    public float distance = 5f;      // how far back the camera sits from the player
    public Vector3 offset = Vector3.zero; // extra offset if you want to shift the follow point

    [Header("Rotation")]
    public float sensitivityX = 200f; // horizontal mouse speed
    public float sensitivityY = 150f; // vertical mouse speed
    public float minYAngle = -30f;    // how far down you can look
    public float maxYAngle = 70f;     // how far up you can look
    public float smoothTime = 0.05f;  // how laggy the camera position is (lower = snappier)

    [Header("Transition")]
    public float upSmoothSpeed = 5f;  // how fast the camera tilts when entering a new gravity zone

    private Quaternion cameraYaw;         // current left/right rotation of the camera
    private float pitchAngle = 0f;        // current up/down angle
    private Vector3 currentVelocity;      // used internally by SmoothDamp
    private bool initialized = false;     // so we only do the first-frame setup once

    // Camera has its own smoothed "up" direction so it doesn't snap when gravity changes
    private Vector3 smoothedUp = Vector3.up;

    void Start()
    {
        if (playerRoot == null)
            playerRoot = meshTarget; // fall back to meshTarget if root wasn't assigned

        // Lock and hide the cursor so the mouse controls the camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void LateUpdate()
    {
        // LateUpdate runs AFTER all other Updates, which is correct for cameras
        if (meshTarget == null || playerRoot == null) return;

        CS_GravityCharacterController cc = playerRoot.GetComponent<CS_GravityCharacterController>();
        if (cc == null) return;

        // Get the player's current "up" direction and smooth the camera toward it
        Vector3 targetUp = cc.localUp;
        smoothedUp = Vector3.Slerp(smoothedUp, targetUp, upSmoothSpeed * Time.deltaTime).normalized;
        Vector3 upDir = smoothedUp;

        // --- 1. One-time initialization ---
        // Set the starting yaw so the camera faces the same direction as the player
        if (!initialized)
        {
            Vector3 initForward = Vector3.ProjectOnPlane(meshTarget.forward, upDir).normalized;
            if (initForward.sqrMagnitude < 0.001f)
                initForward = Vector3.ProjectOnPlane(Vector3.forward, upDir).normalized;

            cameraYaw   = Quaternion.LookRotation(initForward, upDir);
            initialized = true;
        }

        // --- 2. Mouse input ---
        float mouseX = Input.GetAxis("Mouse X") * sensitivityX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivityY * Time.deltaTime;

        // --- 3. Yaw (horizontal rotation) ---
        // Rotate the camera's yaw around the player's up direction
        cameraYaw = Quaternion.AngleAxis(mouseX, upDir) * cameraYaw;

        // Re-align the yaw so it stays perpendicular to the current up direction
        // This is what makes the camera correctly tilt as you walk around a planet
        Vector3 yawForward = Vector3.ProjectOnPlane(cameraYaw * Vector3.forward, upDir).normalized;
        if (yawForward.sqrMagnitude > 0.001f)
            cameraYaw = Quaternion.LookRotation(yawForward, upDir);

        // --- 4. Pitch (vertical rotation) ---
        // Subtract mouseY because moving mouse up should tilt camera up (inverted axis)
        pitchAngle = Mathf.Clamp(pitchAngle - mouseY, minYAngle, maxYAngle);

        // --- 5. Final rotation ---
        // Combine yaw and pitch into one rotation
        Quaternion finalRotation = cameraYaw * Quaternion.Euler(pitchAngle, 0f, 0f);

        // --- 6. Position ---
        // Place camera behind the player based on the final rotation
        Vector3 desiredPosition = meshTarget.position + finalRotation * (Vector3.back * distance + offset);

        // SmoothDamp gives a nice floaty camera movement instead of instant snapping
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition, ref currentVelocity, smoothTime);

        // --- 7. Look at player ---
        // Point the camera at the player, keeping the correct up orientation
        Vector3 lookDir = (meshTarget.position - transform.position).normalized;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDir, upDir);
    }
}