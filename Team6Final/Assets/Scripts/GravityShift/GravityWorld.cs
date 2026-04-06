using UnityEngine;

// Current "up" direction; Physics.gravity pulls along -Up. Default matches standard Unity (+Y up, gravity -Y).
public class GravityWorld
{
    public static Vector3 Up { get; private set; } = Vector3.up;

    public static void SetGravityUp(Vector3 worldUp)
    {
        Up = worldUp.normalized;
        Physics.gravity = -Up * 9.81f; // can make 9.8 a variable to adjust gravity
    }
}