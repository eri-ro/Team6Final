using UnityEngine;

// Small math helpers for walking on walls or slopes when up is not world Y.
public static class GravityAlignment
{
    // Checks this object and every parent until the root; returns true if any have the given tag.
    public static bool TransformOrAncestorsHaveTag(Transform t, string tag)
    {
        // Walk up the hierarchy; stop when t is null (past the root).
        for (; t != null; t = t.parent)
        {
            if (t.CompareTag(tag))
                return true;
        }
        return false;
    }

    // Removes the part of a vector that points along up, leaving a direction on the ground/wall plane.
    // If the result would be zero length, tries a few fallback directions so we never normalize zero.
    public static Vector3 FlattenOnSurface(Vector3 oldUp, Vector3 up)
    {
        // Project onto the plane perpendicular to up (same idea as "horizontal" when up is world Y).
        Vector3 f = Vector3.ProjectOnPlane(oldUp, up);

        // Near the pole or bad input, try other basis vectors until one gives a valid direction.
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.right, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.up, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.Cross(up, Vector3.forward);

        return f.normalized;
    }

    // Forward and right on the walk plane perpendicular to up (same basis as PlayerController / dash).
    public static void GetWalkForwardRight(Transform t, Vector3 up, out Vector3 forward, out Vector3 right)
    {
        up = up.normalized;
        Vector3 flatForward = FlattenOnSurface(t.forward, up);
        forward = flatForward.sqrMagnitude > 1e-8f ? flatForward.normalized : Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
        right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 1e-8f)
            right = FlattenOnSurface(t.right, up);
        right.Normalize();
    }
}
