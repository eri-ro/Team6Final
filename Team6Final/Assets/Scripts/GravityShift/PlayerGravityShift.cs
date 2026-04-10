using UnityEngine;

// Ability: shoot a ray where you're looking, find a tagged "gravity surface", then make that surface your new floor.
// Updates global gravity, rotates the player, and moves the body so feet sit on the wall/floor.
public class PlayerGravityShift : MonoBehaviour
{
    // Reused array so we don't allocate garbage every raycast.
    const int RaycastBufferSize = 32;

    // How far the shift ray can travel to hit a surface.
    public float wallInteractDistance = 4f;
    // If there's no camera, ray starts this high above the player.
    public float interactRayHeight = 1f;
    // Only colliders with this tag (or a parent with the tag) can be shifted onto.
    public string gravityShiftSurfaceTag = "GravitySurface";

    Rigidbody _rb;
    CapsuleCollider _cap;
    PlayerController _player;
    PlayerMotor _motor;
    readonly RaycastHit[] _rayHits = new RaycastHit[RaycastBufferSize];

    // After a successful shift, wait this long before another (actual seconds come from PlayerController).
    float _successCooldownEndTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
        _player = GetComponent<PlayerController>();
        _motor = GetComponent<PlayerMotor>();
    }

    // Called when the player presses the gravity-shift ability. Does nothing if still on cooldown or no valid target.
    public void TryExecuteShift()
    {
        if (Time.time < _successCooldownEndTime)
            return;

        Vector3 up = GravityWorld.Up;
        Vector3 origin;
        Vector3 dir;
        Camera cam = _player != null ? _player.playerCamera : null;
        float maxRayDistance = wallInteractDistance;

        // Aim from screen center through the camera, or from the player body if no camera.
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

        // Need a hit on a valid tagged surface; otherwise we exit with no cooldown (player can try again).
        if (!TryFindShiftTarget(origin, dir, maxRayDistance, out RaycastHit hit))
            return;

        // Remember old "up" so we can pick a sensible forward direction on the new surface.
        Vector3 oldUp = GravityWorld.Up;
        // Surface normal from the ray hit; that becomes our new ceiling-to-floor direction.
        Vector3 newUp = hit.normal.normalized;
        // Normal should point "out" from the wall toward the player, not into the wall.
        if (Vector3.Dot(newUp, dir) > 0f)
            newUp = -newUp;

        // Clean up the normal using the collider (helps on curved or messy meshes).
        newUp = ResampleWalkableNormal(hit.collider, hit.point, newUp);
        newUp = AlignWalkableUpToPlaneOrientation(hit.collider, newUp);

        // This updates global gravity and Physics.gravity right away (physics does not wait for smoothing).
        GravityWorld.SetGravityUp(newUp);

        Vector3 nu = GravityWorld.Up;
        // Face roughly the same way you were looking, but flattened onto the new floor.
        Vector3 fwd = ForwardOnTangentPlane(nu, oldUp, dir);
        ApplyRotationWithExactUp(fwd, nu);

        // Move the capsule so feet touch the surface without overlapping it.
        SnapCapsuleToSurface(hit, nu);

        _motor?.ClearVelocity();
        Physics.SyncTransforms();

        float cd = _player != null ? _player.abilitySuccessCooldownSeconds : 1f;
        _successCooldownEndTime = Time.time + cd;
    }

    // Cast a short ray inside the collider to read a cleaner normal at the hit point.
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

    // If the mesh's "design up" is close to the physics normal, prefer it so walking feels aligned with the art.
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

    // Pick a forward vector that lies flat on the new surface (perpendicular to new up).
    Vector3 ForwardOnTangentPlane(Vector3 nu, Vector3 oldUp, Vector3 shiftRayDir)
    {
        nu = nu.normalized;

        // Build two tangent axes on the surface plane so we can flatten camera/player directions onto it.
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

        // Prefer camera forward, then player forward, etc., so orientation stays intuitive.
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

    // Set rotation so transform.up matches the new gravity up and you look along forwardOnPlane.
    void ApplyRotationWithExactUp(Vector3 forwardOnPlane, Vector3 nu)
    {
        nu = nu.normalized;
        forwardOnPlane = Vector3.ProjectOnPlane(forwardOnPlane, nu);
        if (forwardOnPlane.sqrMagnitude < 1e-10f)
            forwardOnPlane = Vector3.Cross(nu, Vector3.up);
        forwardOnPlane.Normalize();

        transform.rotation = Quaternion.LookRotation(forwardOnPlane, nu);

        // Tiny fix-up if LookRotation drifted from exact up (rare edge case).
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

    // Raycast and return the closest valid hit: not self, has the right tag, solid collider.
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

    // True if this transform is the player root or a child (so we don't stick to our own collider).
    bool IsPartOfLocalPlayer(Transform t)
    {
        return t == transform || t.IsChildOf(transform);
    }

    // Move the rigidbody so the bottom of the capsule sits just above the surface (no clipping, no floating).
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
            // Fallback ask the collider for the closest point if the ray missed (thick or odd geometry).
            Vector3 onSurf = wall.ClosestPoint(rayOrigin + rayDir * (rayLen * 0.5f));
            Vector3 desiredLowest = onSurf + nu * standoff;
            delta = desiredLowest - lowest;
        }

        // Don't teleport huge distances if something goes wrong.
        if (delta.sqrMagnitude > 25f)
            delta = delta.normalized * 5f;

        if (_rb != null)
            _rb.position += delta;
        else
            transform.position += delta;
    }
}
