using UnityEngine;

// Stores which direction is up for the whole game right now
// Physics.gravity is set to point opposite that direction so the player falls the right way on walls
public class GravityWorld
{
    // Current up direction in world space starts as normal Y+
    public static Vector3 Up { get; private set; } = Vector3.up;

    // Call this when the player shifts to a new surface. updates Up and global gravity
    public static void SetGravityUp(Vector3 worldUp)
    {
        // Normalize so length is always 1
        Up = worldUp.normalized;

        // sets global gravity using custom up
        Physics.gravity = -Up * 9.81f;
    }
}
