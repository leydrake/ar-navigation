using UnityEngine;
using UnityEngine.AI;

public class NavMeshBoundaryLimiter : MonoBehaviour
{
    [Header("References")]
    public Transform xrCamera;  // Assign your AR Camera
    public float checkDistance = 0.3f;  // How far to test around camera
    public float correctionStrength = 0.8f; // How strong to push back near border

    private Vector3 lastValidPos;

    void Start()
    {
        if (xrCamera == null)
            xrCamera = Camera.main.transform;

        if (NavMesh.SamplePosition(xrCamera.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            lastValidPos = hit.position;
        else
            lastValidPos = xrCamera.position;
    }

    void LateUpdate()
    {
        Vector3 camPos = xrCamera.position;

        // Always track last valid NavMesh point
        if (NavMesh.SamplePosition(camPos, out NavMeshHit hit, checkDistance, NavMesh.AllAreas))
        {
            lastValidPos = hit.position;
        }
        else
        {
            // If AR tries to move camera outside the NavMesh,
            // push it softly back toward the valid boundary
            Vector3 directionBack = (lastValidPos - camPos).normalized;
            float distanceBack = Vector3.Distance(camPos, lastValidPos);

            // Apply a smooth push back to XR Origin
            transform.position += directionBack * distanceBack * correctionStrength * Time.deltaTime;
        }
    }
}
