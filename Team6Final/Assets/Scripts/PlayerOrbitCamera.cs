using UnityEngine;

// Third-person orbit camera: pulls the camera closer when a wall blocks the line from the focus to the camera.
public static class PlayerOrbitCamera
{
    // Places the camera on the line from focus toward the camera, between min and max distance behind the focus.
    public static void Place(
        Camera cam,
        Transform playerRoot,
        Vector3 focusWorld,
        Vector3 lookDirFromCameraToFocus,
        Vector3 worldUp,
        float desiredDistance,
        float heightBiasAlongUp,
        float minDistanceFromFocus,
        float wallPadding,
        float castStartOffsetTowardCamera,
        LayerMask obstacleLayers)
    {
        lookDirFromCameraToFocus = lookDirFromCameraToFocus.normalized;
        worldUp = worldUp.normalized;

        // Direction from focus out toward where the camera should sit (behind the player in screen space).
        Vector3 towardCamera = -lookDirFromCameraToFocus;

        float actualDist = desiredDistance;

        // Start the ray slightly toward the camera from the focus so we do not hit the player capsule first.
        Vector3 castStart = focusWorld + towardCamera * castStartOffsetTowardCamera;
        float maxCast = desiredDistance - castStartOffsetTowardCamera;

        if (maxCast > 0.01f)
        {
            RaycastHit[] hits = Physics.RaycastAll(castStart, towardCamera, maxCast, obstacleLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Ignore hits on the player and the camera child so we do not pull in because of our own collider.
                if (IsUnderPlayer(hit.collider.transform, playerRoot))
                    continue;

                float fromFocus = castStartOffsetTowardCamera + hit.distance - wallPadding;
                actualDist = Mathf.Clamp(fromFocus, minDistanceFromFocus, desiredDistance);
                break;
            }
        }
        else
            actualDist = Mathf.Clamp(actualDist, minDistanceFromFocus, desiredDistance);

        Vector3 camPos = focusWorld - lookDirFromCameraToFocus * actualDist + worldUp * heightBiasAlongUp;
        cam.transform.position = camPos;
        cam.transform.rotation = Quaternion.LookRotation(lookDirFromCameraToFocus, worldUp);
    }

    // True if this transform is the player or a child (e.g. camera rig under the player).
    static bool IsUnderPlayer(Transform hitTransform, Transform playerRoot)
    {
        if (playerRoot == null)
            return false;
        return hitTransform == playerRoot || hitTransform.IsChildOf(playerRoot);
    }
}
