using UnityEngine;

// E / gamepad: raycast to tagged surfaces, align gravity, snap CharacterController to the plane.
public class PlayerGravityShift : MonoBehaviour
{
    const int RaycastBufferSize = 32;

    // Max reach along the cast from the ray origin. When casting from the orbit camera, we add
    // PlayerController.cameraDistance so walls in front of the player are still in range (camera sits behind you).
    public float wallInteractDistance = 4f;
    // Used only if PlayerController has no camera assigned (ray from body instead of view).
    public float interactRayHeight = 1f;
    // Hit collider or any parent must have this tag (add it in Tags & Layers). Empty = no filter.
    public string gravityShiftSurfaceTag = "GravitySurface";

    CharacterController _cc;
    PlayerController _player;
    PlayerGravityMotor _motor;
    readonly RaycastHit[] _rayHits = new RaycastHit[RaycastBufferSize];

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _player = GetComponent<PlayerController>();
        _motor = GetComponent<PlayerGravityMotor>();
    }

    void Update()
    {
        if (_player != null && !_player.enableGravityShift)
            return;

        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetKeyDown(KeyCode.JoystickButton0))
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

        _cc.enabled = false;
        SnapCapsuleToSurface(hit, nu);
        _cc.enabled = true;

        _motor?.ClearVelocity();
        Physics.SyncTransforms();
    }

    // Mesh hit.normal can be wrong at tri edges; re-raycast into this collider only for a stable normal.
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

    // Unity plane mesh (10209) faces +local Y. Compound rotations (e.g. Wall_West) or tilted quads (Wall_South)
    // can make triangle hit.normal differ from the slab orientation; CC then slides or sinks. When physics
    // agrees within maxAngleDeg, use rotation-only normal so gravity matches the Transform, not a single tri.
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

    // Orthonormal tangent basis from nu + a world reference (avoids fragile Cross(nu, oldUp) / world.forward fallbacks).
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

    // Bottom of capsule uses transform.up (matches CharacterController). Anchor stays at hit.point:
    // Dot(bottom - P, nu) is the same for any P on the plane, so a fixed anchor is stable. Recomputing
    // ClosestPoint each iteration can jump to a far mesh edge on large quads and teleport the player.
    void SnapCapsuleToSurface(RaycastHit hit, Vector3 nu)
    {
        nu = nu.normalized;
        Vector3 planeAnchor = hit.point;
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
