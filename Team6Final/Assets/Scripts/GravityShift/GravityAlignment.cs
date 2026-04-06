using UnityEngine;

// used when the floor is not the normal XZ plane
public static class GravityAlignment
{
    // Walks from this transform up through parents and returns true if any have the given tag
    public static bool TransformOrAncestorsHaveTag(Transform t, string tag)
    {
        // Stop when we run out of parents (t becomes null).
        for (; t != null; t = t.parent)
        {
            if (t.CompareTag(tag))
                return true;
        }
        return false;
    }

    // Takes any direction and removes the part that points along up leaving a direction on the surface.
    // If the result would be zero tries a few fallback axes
    public static Vector3 FlattenOnSurface(Vector3 oldUp, Vector3 up)
    {
        // Project onto the plane whose normal is up
        Vector3 f = Vector3.ProjectOnPlane(oldUp, up);

        // Near zero try other reference directions until one works
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.right, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.up, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.Cross(up, Vector3.forward);

        return f.normalized;
    }
}
