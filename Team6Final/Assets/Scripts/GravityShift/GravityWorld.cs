using UnityEngine;
using UnityEngine.SceneManagement;

// Global which way is up for the game. Physics.gravity is kept opposite to this so Unity physics matches the design.
public class GravityWorld
{
    static GravityWorld()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            ResetToDefaultWorld();
    }

    // World +Y gravity and camera/controls. Call after a gravity shift, or on level load
    public static void ResetToDefaultWorld()
    {
        Up = Vector3.up;
        ControlUp = Vector3.up;
        Physics.gravity = -Vector3.up * 9.81f;
    }

    // Current up direction in world space. Starts as normal Unity: Y+.
    public static Vector3 Up { get; private set; } = Vector3.up;

    // Smoothed toward Up for camera and input framing; physics (motor, gravity) uses Up immediately.
    public static Vector3 ControlUp { get; private set; } = Vector3.up;

    // Call when the player shifts to a new surface; updates Up and sets Physics.gravity to pull along -Up.
    public static void SetGravityUp(Vector3 worldUp)
    {
        // Normalize so math and gravity strength stay consistent.
        Up = worldUp.normalized;

        // Unity moves dynamic bodies with Physics.gravity; we match it to our custom up direction.
        Physics.gravity = -Up * 9.81f;
    }

    // Call every frame. Slowly rotates ControlUp toward the real physics Up so the camera and WASD feel smooth after a gravity shift, instead of snapping instantly.
    public static void TickControlUpAlignment(float deltaTime, float alignSpeed)
    {
        Vector3 target = Up.sqrMagnitude > 1e-10f ? Up.normalized : Vector3.up;
        if (alignSpeed <= 0f || deltaTime <= 0f)
        {
            ControlUp = target;
            return;
        }

        float t = Mathf.Clamp01(alignSpeed * deltaTime);
        ControlUp = Vector3.Slerp(ControlUp, target, t).normalized;
    }

    // Makes ControlUp match physics Up in one frame.
    public static void SnapControlUpToPhysicsUp()
    {
        Vector3 target = Up.sqrMagnitude > 1e-10f ? Up.normalized : Vector3.up;
        ControlUp = target;
    }
}
