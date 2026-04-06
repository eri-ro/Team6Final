using UnityEngine;

// Shared helpers for movement and abilities that use GravityWorld.Up.
public static class GravityAlignment
{
    public static bool TransformOrAncestorsHaveTag(Transform t, string tag)
    {
        for (; t != null; t = t.parent)
        {
            if (t.CompareTag(tag))
                return true;
        }
        return false;
    }

    // kept getting errors when near zero
    public static Vector3 FlattenOnSurface(Vector3 oldUp, Vector3 up)
    {
        Vector3 f = Vector3.ProjectOnPlane(oldUp, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.right, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.ProjectOnPlane(Vector3.up, up);
        if (f.sqrMagnitude < 1e-10f)
            f = Vector3.Cross(up, Vector3.forward);
        return f.normalized;
    }
}
