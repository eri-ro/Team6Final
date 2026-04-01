using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public float moveSpeed = 6f;
    public float lookSensitivity = 2f;
    public float wallInteractDistance = 4f;
    public float interactRayHeight = 1f;
    public float cameraDistance = 4.5f;

    CharacterController _cc;
    Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        transform.Rotate(GravityWorld.Up, Input.GetAxis("Mouse X") * lookSensitivity, Space.World);
        TryShiftGravity();

        Vector3 up = GravityWorld.Up;
        Vector3 forward = FlattenOnSurface(transform.forward, up);
        Vector3 right = FlattenOnSurface(transform.right, up);
        Vector3 move = (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")).normalized * (moveSpeed * Time.deltaTime);

        _velocity += Physics.gravity * Time.deltaTime;
        if (_cc.isGrounded)
        {
            float vg = Vector3.Dot(_velocity, GravityWorld.Up);
            if (vg < 0f)
                _velocity -= GravityWorld.Up * vg;
        }

        _cc.Move(move + _velocity * Time.deltaTime);

        if (_cc.isGrounded && Vector3.Dot(_velocity, GravityWorld.Up) < 0f)
            _velocity = Vector3.ProjectOnPlane(_velocity, GravityWorld.Up);

        Vector3 focus = transform.position + up * 0.9f;
        playerCamera.transform.position = focus - transform.forward * cameraDistance + up * 0.15f;
        playerCamera.transform.LookAt(focus, up);
    }

    void TryShiftGravity()
    {
        if (!Input.GetKeyDown(KeyCode.E) && !Input.GetKeyDown(KeyCode.JoystickButton0))
            return;

        Vector3 up = GravityWorld.Up;
        Vector3 origin = transform.position + up * interactRayHeight;
        Vector3 dir = FlattenOnSurface(transform.forward, up);

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, wallInteractDistance, ~0, QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider.GetComponentInParent<GravitySurface>() == null)
            return;

        Vector3 oldUp = GravityWorld.Up;
        GravityWorld.SetGravityUp(hit.normal);

        Vector3 nu = GravityWorld.Up;
        Vector3 fwd = FlattenOnSurface(oldUp, nu);
        transform.rotation = Quaternion.LookRotation(fwd, nu);

        _velocity = Vector3.zero;
        Physics.SyncTransforms();
    }

    // kept getting errors when near zero
    static Vector3 FlattenOnSurface(Vector3 oldUp, Vector3 up)
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
