using UnityEngine;
using UnityEngine.AI;

public class NavMeshBoundsChecker : MonoBehaviour
{
    [Header("Out of Bounds")]
    public GameObject outOfBoundsUI;
    public float sampleDistance = 2f;
    
    [Header("Clamping Zones")]
    [Tooltip("Distance where soft clamping starts (gentle pull)")]
    public float softClampDistance = 0.3f;
    
    [Tooltip("Distance where strong clamping starts (stronger pull)")]
    public float strongClampDistance = 0.45f;
    
    [Tooltip("Distance where out of bounds triggers (NO clamping, just UI)")]
    public float outOfBoundsDistance = 0.5f;
    
    [Tooltip("Soft clamp strength")]
    [Range(0.01f, 0.3f)]
    public float softClampSpeed = 0.1f;
    
    [Tooltip("Strong clamp strength")]
    [Range(0.1f, 0.5f)]
    public float strongClampSpeed = 0.25f;

    void Update()
    {
        // Ignore height (Y), just check X/Z
        Vector3 checkPos = new Vector3(transform.position.x, 0f, transform.position.z);
        NavMeshHit hit;

        // Find nearest NavMesh point within sampleDistance
        bool onNavMesh = NavMesh.SamplePosition(checkPos, out hit, sampleDistance, NavMesh.AllAreas);

        if (onNavMesh)
        {
            Vector3 hitPosXZ = new Vector3(hit.position.x, 0f, hit.position.z);
            float distance = Vector3.Distance(hitPosXZ, checkPos);

            // FREE ZONE - within soft clamp distance
            if (distance <= softClampDistance)
            {
                HideOutOfBounds();
                // No clamping, free movement
            }
            // SOFT CLAMP ZONE
            else if (distance <= strongClampDistance)
            {
                HideOutOfBounds();
                // Gentle pull back to NavMesh
                Vector3 targetPos = new Vector3(hit.position.x, transform.position.y, hit.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPos, softClampSpeed);
                Debug.Log($"Soft clamping - Distance: {distance:F2}m");
            }
            // STRONG CLAMP ZONE
            else if (distance <= outOfBoundsDistance)
            {
                HideOutOfBounds();
                // Stronger pull back to NavMesh
                Vector3 targetPos = new Vector3(hit.position.x, transform.position.y, hit.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPos, strongClampSpeed);
                Debug.Log($"Strong clamping - Distance: {distance:F2}m");
            }
            // OUT OF BOUNDS - NO clamping, just show UI
            else
            {
                ShowOutOfBounds();
                // No clamping here! User must walk back manually
            }
        }
        else
        {
            // Completely off NavMesh
            ShowOutOfBounds();
        }
    }

    void ShowOutOfBounds()
    {
        if (outOfBoundsUI != null && !outOfBoundsUI.activeSelf)
            outOfBoundsUI.SetActive(true);
    }

    void HideOutOfBounds()
    {
        if (outOfBoundsUI != null && outOfBoundsUI.activeSelf)
            outOfBoundsUI.SetActive(false);
    }
}