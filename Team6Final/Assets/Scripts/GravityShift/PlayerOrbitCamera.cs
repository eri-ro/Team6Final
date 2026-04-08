using UnityEngine;

// Pulls the orbit camera toward the player when a wall blocks the line from the focus to the camera (first-person when tight).
public static class PlayerOrbitCamera
{
    // Puts the camera on the look ray from focus, between minDistanceFromFocus and desiredDistance behind the focus point.
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
        Vector3 towardCamera = -lookDirFromCameraToFocus;

        float actualDist = desiredDistance;
        Vector3 castStart = focusWorld + towardCamera * castStartOffsetTowardCamera;
        float maxCast = desiredDistance - castStartOffsetTowardCamera;

        if (maxCast > 0.01f)
        {
            RaycastHit[] hits = Physics.RaycastAll(castStart, towardCamera, maxCast, obstacleLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
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

    static bool IsUnderPlayer(Transform hitTransform, Transform playerRoot)
    {
        if (playerRoot == null)
            return false;
        return hitTransform == playerRoot || hitTransform.IsChildOf(playerRoot);
    }
}
