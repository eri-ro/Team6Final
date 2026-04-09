using UnityEngine;

// Raycasts from the camera, sets GravityWorld.Up, rotates the player, snaps the Rigidbody to the surface.
// Vector3.Slerp is best for blending unit directions (e.g. old gravity up -> new up). For world positions use Vector3.Lerp
// or Vector3.SmoothDamp. Quaternion.Slerp blends rotations smoothly. This script snaps instantly so physics stays consistent.
public class PlayerGravityShift : MonoBehaviour
{
    const int RaycastBufferSize = 32;

    public float wallInteractDistance = 4f;
    public float interactRayHeight = 1f;
    public string gravityShiftSurfaceTag = "GravitySurface";

    Rigidbody _rb;
    CapsuleCollider _cap;
    BasicPlayerController _player;
    PlayerGravityMotor _motor;
    readonly RaycastHit[] _rayHits = new RaycastHit[RaycastBufferSize];

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
        _player = GetComponent<BasicPlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    public void TryExecuteShift()
    {
        if (_player != null && !_player.enableGravityShift)
            return;

        Vector3 up = GravityWorld.Up;
        Vector3 origin;
        Vector3 dir;
        Camera cam = _player != null ? _player.playerCamera : null;
        float maxRayDistance = wallInteractDistance;

        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            origin = ray.origin;
            dir = ray.direction.normalized;
            if (_player != null)
                maxRayDistance += _player.cameraDistance;
        }
        else
        {
            origin = transform.position + up * interactRayHeight;
            dir = GravityAlignment.FlattenOnSurface(transform.forward, up);
        }

        if (!TryFindShiftTarget(origin, dir, maxRayDistance, out RaycastHit hit))
            return;

        Vector3 oldUp = GravityWorld.Up;
        Vector3 newUp = hit.normal.normalized;
        if (Vector3.Dot(newUp, dir) > 0f)
            newUp = -newUp;

        newUp = ResampleWalkableNormal(hit.collider, hit.point, newUp);
        newUp = AlignWalkableUpToPlaneOrientation(hit.collider, newUp);

        GravityWorld.SetGravityUp(newUp);

        Vector3 nu = GravityWorld.Up;
        Vector3 fwd = ForwardOnTangentPlane(nu, oldUp, dir);
        ApplyRotationWithExactUp(fwd, nu);

        SnapCapsuleToSurface(hit, nu);

        _motor?.ClearVelocity();
        Physics.SyncTransforms();
    }

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

    Vector3 ForwardOnTangentPlane(Vector3 nu, Vector3 oldUp, Vector3 shiftRayDir)
    {
        nu = nu.normalized;

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

    void ApplyRotationWithExactUp(Vector3 forwardOnPlane, Vector3 nu)
    {
        nu = nu.normalized;
        forwardOnPlane = Vector3.ProjectOnPlane(forwardOnPlane, nu);
        if (forwardOnPlane.sqrMagnitude < 1e-10f)
            forwardOnPlane = Vector3.Cross(nu, Vector3.up);
        forwardOnPlane.Normalize();

        transform.rotation = Quaternion.LookRotation(forwardOnPlane, nu);

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

    bool IsPartOfLocalPlayer(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }

    // Places feet on the collider face toward the player. Uses a short ray from inside the walkable volume
    // so thin meshes are not tunnelled through (plane-only math could land on the far side).
    void SnapCapsuleToSurface(RaycastHit hit, Vector3 nu)
    {
        if (_cap == null)
            return;

        nu = nu.normalized;
        Collider wall = hit.collider;
        if (wall == null)
            return;

        float standoff = Physics.defaultContactOffset * 2f + 0.02f;

        Vector3 axis = transform.up.normalized;
        Vector3 worldCenter = transform.TransformPoint(_cap.center);
        float half = _cap.height * 0.5f;
        Vector3 lowest = worldCenter - axis * half;

        // Start inside the "room" side of the surface so the ray hits the near face, not the far side of a thin wall.
        Vector3 rayOrigin = hit.point + nu * Mathf.Clamp(half + 0.4f, 0.5f, 3f);
        Vector3 rayDir = -nu;
        const float rayLen = 8f;

        int count = Physics.RaycastNonAlloc(rayOrigin, rayDir, _rayHits, rayLen, ~0, QueryTriggerInteraction.Ignore);
        RaycastHit? best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = _rayHits[i];
            if (h.collider != wall)
                continue;
            if (h.distance < bestD)
            {
                bestD = h.distance;
                best = h;
            }
        }

        Vector3 delta;
        if (best.HasValue)
        {
            Vector3 contact = best.Value.point;
            Vector3 outFromSurface = best.Value.normal.normalized;
            if (Vector3.Dot(outFromSurface, nu) < 0f)
                outFromSurface = -outFromSurface;
            Vector3 desiredLowest = contact + outFromSurface * standoff;
            delta = desiredLowest - lowest;
        }
        else
        {
            Vector3 onSurf = wall.ClosestPoint(rayOrigin + rayDir * (rayLen * 0.5f));
            Vector3 desiredLowest = onSurf + nu * standoff;
            delta = desiredLowest - lowest;
        }

        if (delta.sqrMagnitude > 25f)
            delta = delta.normalized * 5f;

        if (_rb != null)
            _rb.position += delta;
        else
            transform.position += delta;
    }
}
