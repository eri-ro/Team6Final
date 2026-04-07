using UnityEngine;

// Raycasts from the camera center to find a tagged wall, then changes GravityWorld.Up and snaps the player to that surface
// BasicPlayerController calls TryExecuteShift when GravityShift is the selected ability and the ability key is pressed
public class PlayerGravityShift : MonoBehaviour
{
    // How many ray hits we can collect in one cast
    const int RaycastBufferSize = 32;

    // Max ray length from the eye toward the crosshair
    public float wallInteractDistance = 4f;

    // If there is no camera, ray starts this high above the player's feet using transform.forward
    public float interactRayHeight = 1f;

    // Only colliders with this tag (on self or a parent) count as valid shift targets
    public string gravityShiftSurfaceTag = "GravitySurface";

    CharacterController _cc;
    BasicPlayerController _player;
    PlayerGravityMotor _motor;

    // Reused buffer for Physics.RaycastNonAlloc
    readonly RaycastHit[] _rayHits = new RaycastHit[RaycastBufferSize];

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _player = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    // Called from BasicPlayerController when the player uses the gravity shift ability
    public void TryExecuteShift()
    {
        // wall mode is off shifting is not allowed
        if (_player != null && !_player.enableGravityShift)
            return;

        Vector3 up = GravityWorld.Up;
        Vector3 origin;
        Vector3 dir;
        Camera cam = _player != null ? _player.playerCamera : null;
        float maxRayDistance = wallInteractDistance;

        if (cam != null)
        {
            // Ray from center of the screen
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            origin = ray.origin;
            dir = ray.direction.normalized;

            // Camera sits behind the player, so extend reach so walls in front are still hittable
            if (_player != null)
                maxRayDistance += _player.cameraDistance;
        }
        else
        {
            // Fallback: shoot from body height along flattened forward
            origin = transform.position + up * interactRayHeight;
            dir = GravityAlignment.FlattenOnSurface(transform.forward, up);
        }

        // Nothing valid hit
        if (!TryFindShiftTarget(origin, dir, maxRayDistance, out RaycastHit hit))
            return;

        // New "outward" normal from the wall; we want gravity to pull toward the wall, so up = away from inside the wall
        Vector3 oldUp = GravityWorld.Up;
        Vector3 newUp = hit.normal.normalized;

        // If the normal points the same way as the ray, flip it so we stick to the outside of the surface
        if (Vector3.Dot(newUp, dir) > 0f)
            newUp = -newUp;

        // Clean up noisy normals from mesh edges and align big planes to their transform when close enough
        newUp = ResampleWalkableNormal(hit.collider, hit.point, newUp);
        newUp = AlignWalkableUpToPlaneOrientation(hit.collider, newUp);

        // Tell the whole game which way is up and update Physics.gravity
        GravityWorld.SetGravityUp(newUp);

        Vector3 nu = GravityWorld.Up;

        // Pick a forward direction that lies on the new floor plane so the player does not spin wildly
        Vector3 fwd = ForwardOnTangentPlane(nu, oldUp, dir);
        ApplyRotationWithExactUp(fwd, nu);

        // Turning off the controller avoids fighting the teleport while we adjust position
        _cc.enabled = false;
        SnapCapsuleToSurface(hit, nu);
        _cc.enabled = true;

        // Old fall speed would feel wrong on the new wall
        _motor?.ClearVelocity();
        Physics.SyncTransforms();
    }

    // Fire a short ray inside the collider to get a stable normal
    static Vector3 ResampleWalkableNormal(Collider col, Vector3 hitPoint, Vector3 preliminaryOutward)
    {
        if (col == null)
            return preliminaryOutward.normalized;
        preliminaryOutward = preliminaryOutward.normalized;
        Vector3 rayOrigin = hitPoint + preliminaryOutward * 0.2f;
        if (!col.Raycast(new Ray(rayOrigin, -preliminaryOutward), out RaycastHit inner, 2f))
            return preliminaryOutward;
        Vector3 n = inner.normal.normalized;
        if (Vector3.Dot(n, preliminaryOutward) < 0f)
            n = -n;
        return n;
    }

    // If the collider's transform up almost matches physics normal, trust the transform
    static Vector3 AlignWalkableUpToPlaneOrientation(Collider col, Vector3 physicsUp)
    {
        if (col == null)
            return physicsUp;
        const float maxAngleDeg = 50f;
        Vector3 geomUp = (col.transform.rotation * Vector3.up).normalized;
        if (Vector3.Dot(geomUp, physicsUp) < 0f)
            geomUp = -geomUp;
        if (Vector3.Angle(geomUp, physicsUp) <= maxAngleDeg)
            return geomUp;
        return physicsUp;
    }

    // Builds a forward vector on the plane perpendicular to nu using camera/player direction
    Vector3 ForwardOnTangentPlane(Vector3 nu, Vector3 oldUp, Vector3 shiftRayDir)
    {
        nu = nu.normalized;

        // Pick a reference axis not parallel to nu so Cross products are stable
        Vector3 refAxis = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(refAxis, nu)) > 0.92f)
            refAxis = Vector3.right;
        if (Mathf.Abs(Vector3.Dot(refAxis, nu)) > 0.92f)
            refAxis = Vector3.forward;

        Vector3 t0 = Vector3.Cross(nu, refAxis);
        if (t0.sqrMagnitude < 1e-12f)
            t0 = Vector3.Cross(nu, Vector3.up);
        t0.Normalize();
        Vector3 t1 = Vector3.Cross(nu, t0);
        t1.Normalize();

        // Project a 3D direction onto the tangent plane spanned by t0, t1
        bool TryCombine(Vector3 v, out Vector3 onPlane)
        {
            Vector3 p = t0 * Vector3.Dot(v, t0) + t1 * Vector3.Dot(v, t1);
            if (p.sqrMagnitude > 1e-10f)
            {
                onPlane = p.normalized;
                return true;
            }
            onPlane = default;
            return false;
        }

        Camera cam = _player != null ? _player.playerCamera : null;
        if (cam != null && TryCombine(cam.transform.forward, out Vector3 f))
            return f;
        if (TryCombine(transform.forward, out f))
            return f;
        if (TryCombine(transform.right, out f))
            return f;
        if (TryCombine(oldUp, out f))
            return f;
        if (TryCombine(shiftRayDir, out f))
            return f;
        return t0;
    }

    // Sets rotation so transform.up matches the new gravity up and forward lies on the surface
    void ApplyRotationWithExactUp(Vector3 forwardOnPlane, Vector3 nu)
    {
        nu = nu.normalized;
        forwardOnPlane = Vector3.ProjectOnPlane(forwardOnPlane, nu);
        if (forwardOnPlane.sqrMagnitude < 1e-10f)
            forwardOnPlane = Vector3.Cross(nu, Vector3.up);
        forwardOnPlane.Normalize();

        transform.rotation = Quaternion.LookRotation(forwardOnPlane, nu);

        // Second pass fixes tiny numerical mismatch between up and nu
        if (Vector3.Angle(transform.up, nu) > 0.25f)
        {
            forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, nu);
            if (forwardOnPlane.sqrMagnitude > 1e-10f)
            {
                forwardOnPlane.Normalize();
                transform.rotation = Quaternion.LookRotation(forwardOnPlane, nu);
            }
        }
    }

    // Picks the closest valid hit along the ray that is tagged and not part of this player
    bool TryFindShiftTarget(Vector3 origin, Vector3 dir, float maxDistance, out RaycastHit bestHit)
    {
        bestHit = default;
        int count = Physics.RaycastNonAlloc(origin, dir, _rayHits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        if (count <= 0)
            return false;

        float bestDist = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = _rayHits[i];
            Collider col = h.collider;
            if (col == null)
                continue;
            if (IsPartOfLocalPlayer(col.transform))
                continue;
            if (!string.IsNullOrEmpty(gravityShiftSurfaceTag) &&
                !GravityAlignment.TransformOrAncestorsHaveTag(col.transform, gravityShiftSurfaceTag))
                continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit = h;
                found = true;
            }
        }

        return found;
    }

    // True if this collider belongs to the player hierarchy (ignore self-hits)
    bool IsPartOfLocalPlayer(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }

    // Moves the player along nu until the capsule bottom sits slightly above the hit plane
    void SnapCapsuleToSurface(RaycastHit hit, Vector3 nu)
    {
        nu = nu.normalized;
        Vector3 planeAnchor = hit.point;

        // Small padding so the capsule does not intersect the wall next physics frame
        float pad = _cc.radius + _cc.skinWidth * 2f + Physics.defaultContactOffset * 2f + 0.02f;

        for (int i = 0; i < 12; i++)
        {
            Vector3 axis = transform.up.normalized;
            Vector3 worldCenter = transform.position + transform.TransformVector(_cc.center);
            Vector3 bottomNow = worldCenter - axis * (_cc.height * 0.5f);

            float signed = Vector3.Dot(bottomNow - planeAnchor, nu);
            float shift = pad - signed;
            if (Mathf.Abs(shift) < 0.0005f)
                break;
            transform.position += nu * shift;
        }
    }
}
