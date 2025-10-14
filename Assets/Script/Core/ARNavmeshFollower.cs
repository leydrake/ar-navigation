using UnityEngine;
using UnityEngine.AI;

public class ARNavmeshFollower : MonoBehaviour
{
    public Camera arCamera;
    public float maxCheckDistance = 1.0f;   // how far to search for valid NavMesh
    private Vector3 lastValidPosition;

    void Start()
    {
        if (arCamera != null)
            lastValidPosition = arCamera.transform.position;
    }

    void Update()
    {
        if (arCamera == null) return;

        Vector3 targetPos = arCamera.transform.position;
        NavMeshHit hit;

        // Check if the camera is still on NavMesh
        if (NavMesh.SamplePosition(targetPos, out hit, maxCheckDistance, NavMesh.AllAreas))
        {
            // It's valid — update the proxy and remember this spot
            transform.position = hit.position;
            lastValidPosition = hit.position;
        }
        else
        {
            // Out of bounds — lock camera back to last valid NavMesh position
            Vector3 offset = arCamera.transform.position - transform.position;
            arCamera.transform.position = lastValidPosition + offset;

            // Optional: also reset XR Origin position to prevent drift
            var origin = arCamera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
                origin.transform.position = lastValidPosition;

            Debug.Log("AR Camera clamped to NavMesh edge.");
        }
    }
}
