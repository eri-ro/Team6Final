using System.Collections.Generic;
using UnityEngine;

// Supports any convex collider shape as a gravity body (sphere, cube, capsule, mesh, etc.)
// Gravity pulls toward the nearest point on the gravity body's surface, not its center.
// Attach this to the planet/object. The trigger collider (any shape) defines the gravity zone.
public class CS_Orbital : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravityStrength = 50f;
    public float maxGravitySpeed = 20f;

    [Header("Priority (Higher Wins)")]
    public int priority = 0;

    [Header("Gravity Body")]
    [Tooltip("The collider that represents the actual planet surface (used for nearest-point gravity). " +
             "Can be any shape. If left null, falls back to this object's first non-trigger collider.")]
    public Collider gravityBodyCollider;

    [Header("Trigger Zone")]
    [Tooltip("A separate trigger collider defining the zone of influence. " +
             "If null, this component will create a SphereCollider trigger automatically.")]
    public Collider triggerZoneCollider;

    [Header("Auto Sphere Zone (if no trigger assigned)")]
    public float autoZoneRadius = 20f;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;

    private static Dictionary<Rigidbody, CS_Orbital> activeGravityField =
        new Dictionary<Rigidbody, CS_Orbital>();
    private static Dictionary<Rigidbody, List<CS_Orbital>> overlappingFields =
        new Dictionary<Rigidbody, List<CS_Orbital>>();

    void Awake()
    {
        // Auto-setup trigger zone if none assigned
        if (triggerZoneCollider == null)
        {
            foreach (Collider col in GetComponents<Collider>())
            {
                if (col.isTrigger)
                {
                    triggerZoneCollider = col;
                    break;
                }
            }

            if (triggerZoneCollider == null)
            {
                SphereCollider sc = gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = autoZoneRadius;
                triggerZoneCollider = sc;
            }
        }

        triggerZoneCollider.isTrigger = true;

        // Auto-find gravity body collider (non-trigger surface collider)
        if (gravityBodyCollider == null)
        {
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                if (!col.isTrigger)
                {
                    gravityBodyCollider = col;
                    break;
                }
            }
        }
    }

    void FixedUpdate()
    {
        foreach (var pair in activeGravityField)
        {
            Rigidbody rb = pair.Key;
            CS_Orbital field = pair.Value;

            if (rb == null || field != this) continue;

            rb.useGravity = false;

            // Pull toward nearest surface point instead of center.
            // This is what makes cube/irregular planets feel correct underfoot.
            Vector3 pullTarget = GetNearestSurfacePoint(rb.position);
            Vector3 toSurface = pullTarget - rb.position;
            float dist = toSurface.magnitude;

            if (dist < 0.01f) continue;

            Vector3 direction = toSurface.normalized;

            float zoneSize = GetZoneRadius();
            float gravityFactor = Mathf.Clamp01(dist / zoneSize);
            Vector3 gravityForce = direction * gravityStrength * gravityFactor;

            rb.AddForce(gravityForce, ForceMode.Acceleration);

            if (rb.velocity.magnitude > maxGravitySpeed)
                rb.velocity = rb.velocity.normalized * maxGravitySpeed;

            if (dist < 0.5f)
                rb.velocity *= 0.9f;
        }
    }

    // Returns the closest point on the gravity body surface to a given world position.
    // This is the key method — ClosestPoint works on Box, Sphere, Capsule, and convex MeshColliders.
    public Vector3 GetNearestSurfacePoint(Vector3 worldPosition)
    {
        if (gravityBodyCollider != null)
            return gravityBodyCollider.ClosestPoint(worldPosition);

        return transform.position;
    }

    private float GetZoneRadius()
    {
        if (triggerZoneCollider is SphereCollider sc)
            return sc.radius * transform.lossyScale.x;

        return triggerZoneCollider.bounds.extents.magnitude;
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (!overlappingFields.ContainsKey(rb))
            overlappingFields[rb] = new List<CS_Orbital>();
        if (!overlappingFields[rb].Contains(this))
            overlappingFields[rb].Add(this);

        if (!activeGravityField.ContainsKey(rb) || priority >= activeGravityField[rb].priority)
        {
            activeGravityField[rb] = this;
            rb.useGravity = false;

            CS_GravityCharacterController cc = rb.GetComponent<CS_GravityCharacterController>();
            if (cc != null)
                cc.SetGravityField(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (overlappingFields.ContainsKey(rb))
            overlappingFields[rb].Remove(this);

        if (!activeGravityField.ContainsKey(rb) || activeGravityField[rb] != this)
            return;

        CS_Orbital best = null;
        if (overlappingFields.ContainsKey(rb))
        {
            foreach (CS_Orbital zone in overlappingFields[rb])
            {
                if (best == null || zone.priority > best.priority)
                    best = zone;
            }
        }

        CS_GravityCharacterController cc = rb.GetComponent<CS_GravityCharacterController>();

        if (best != null)
        {
            activeGravityField[rb] = best;
            rb.useGravity = false;
            if (cc != null) cc.SetGravityField(best);
        }
        else
        {
            activeGravityField.Remove(rb);
            overlappingFields.Remove(rb);
            rb.useGravity = false;
            if (cc != null) cc.SetGravityField(null);
        }
    }

    void OnDrawGizmos()
    {
        if (triggerZoneCollider == null) return;
        Gizmos.color = gizmoColor;

        if (triggerZoneCollider is SphereCollider sc)
        {
            Gizmos.DrawWireSphere(transform.position + sc.center,
                sc.radius * transform.lossyScale.x);
        }
        else
        {
            Gizmos.DrawWireCube(triggerZoneCollider.bounds.center,
                triggerZoneCollider.bounds.size * 1.01f);
        }
    }
}