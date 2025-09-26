using UnityEngine;
using UnityEngine.AI;

public class ARCameraTurnNavigator : MonoBehaviour
{
    [Header("References")]
    public Transform arCamera;         // Your AR camera
    public NavMeshAgent agentProxy;    // Invisible proxy agent
    public AudioSource audioSource;
    public AudioClip turnLeftClip;
    public AudioClip turnRightClip;
    public AudioClip straightClip;

    [Header("Navigation Display")]
    public Transform arrow;            // Optional AR arrow indicator
    public LineRenderer lineRenderer;  // Optional navigation line

    [Header("Settings")]
    public float turnDistanceThreshold = 2f;  // When to announce turn
    public float turnAngleThreshold = 30f;    // Minimum angle to consider a turn

    private bool voicePlayed = false;
    private int currentCornerIndex = 0;

    void Start()
    {
        // Ensure the proxy agent does not move the camera
        if (agentProxy != null)
        {
            agentProxy.updatePosition = false;
            agentProxy.updateRotation = false;
        }
    }

    void Update()
    {
        if (agentProxy == null || arCamera == null)
            return;

        // 1. Sync proxy with AR camera
        agentProxy.transform.position = arCamera.position;
        agentProxy.transform.rotation = arCamera.rotation;

        if (agentProxy.pathPending || agentProxy.path.corners.Length < 2)
            return;

        Vector3[] corners = agentProxy.path.corners;
        int nextCornerIndex = Mathf.Min(currentCornerIndex + 1, corners.Length - 1);

        // 2. Update arrow and/or LineRenderer
        UpdateNavigationDisplay(corners);

        // 3. Distance to next corner
        float distance = Vector3.Distance(arCamera.position, corners[nextCornerIndex]);

        if (!voicePlayed && distance < turnDistanceThreshold && currentCornerIndex < corners.Length - 2)
        {
            CalculateTurn(corners, currentCornerIndex);
            voicePlayed = true;
        }

        // 4. Move to next corner
        if (distance < 0.5f && currentCornerIndex < corners.Length - 2)
        {
            currentCornerIndex++;
            voicePlayed = false;
        }
    }

    void UpdateNavigationDisplay(Vector3[] corners)
    {
        // Arrow: point to next corner
        if (arrow != null && corners.Length > 1)
        {
            Vector3 nextCorner = corners[Mathf.Min(currentCornerIndex + 1, corners.Length - 1)];
            Vector3 dir = (nextCorner - arrow.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
                arrow.rotation = Quaternion.Slerp(arrow.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        // LineRenderer: show full path
        if (lineRenderer != null && corners.Length > 1)
        {
            lineRenderer.positionCount = corners.Length;
            lineRenderer.SetPositions(corners);
        }
    }

    void CalculateTurn(Vector3[] corners, int index)
    {
        Vector3 currentDir = (corners[index + 1] - corners[index]).normalized;
        Vector3 nextDir = (corners[index + 2] - corners[index + 1]).normalized;

        currentDir.y = 0;
        nextDir.y = 0;

        float angle = Vector3.SignedAngle(currentDir, nextDir, Vector3.up);

        if (angle > turnAngleThreshold)
            audioSource.PlayOneShot(turnRightClip);
        else if (angle < -turnAngleThreshold)
            audioSource.PlayOneShot(turnLeftClip);
        else
            audioSource.PlayOneShot(straightClip);
    }

    // Call this when setting a new destination
    public void SetDestination(Vector3 target)
    {
        if (agentProxy == null) return;

        agentProxy.SetDestination(target);
        currentCornerIndex = 0;
        voicePlayed = false;
    }

    // Optional: toggle arrow and line visibility
    public void ToggleNavigationDisplay(bool showArrow, bool showLine)
    {
        if (arrow != null)
            arrow.gameObject.SetActive(showArrow);

        if (lineRenderer != null)
            lineRenderer.enabled = showLine;
    }
}
    