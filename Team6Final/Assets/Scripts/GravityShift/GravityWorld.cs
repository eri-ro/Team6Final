using UnityEngine;

// Global which way is up for the game. Physics.gravity is kept opposite to this so Unity physics matches the design.
public class GravityWorld
{
    // Current up direction in world space. Starts as normal Unity: Y+.
    public static Vector3 Up { get; private set; } = Vector3.up;

    // Call when the player shifts to a new surface; updates Up and sets Physics.gravity to pull along -Up.
    public static void SetGravityUp(Vector3 worldUp)
    {
        // Normalize so math and gravity strength stay consistent.
        Up = worldUp.normalized;

        // Unity moves dynamic bodies with Physics.gravity; we match it to our custom up direction.
        Physics.gravity = -Up * 9.81f;
    }
}
