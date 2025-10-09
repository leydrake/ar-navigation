using UnityEngine;
using UnityEngine.AI;

public class NavMeshBoundsCheck : MonoBehaviour
{
    public GameObject outOfBoundsUI;   // assign your popup
    public float sampleDistance = 2f;  // distance to search NavMesh

    void Update()
    {
        // Ignore height (Y), just check X/Z
        Vector3 checkPos = new Vector3(transform.position.x, 0f, transform.position.z);
        NavMeshHit hit;

        // Find nearest NavMesh point within sampleDistance
        bool onNavMesh = NavMesh.SamplePosition(checkPos, out hit, sampleDistance, NavMesh.AllAreas);

        // If nearest point is too far, consider out of bounds
        if (!onNavMesh || Vector3.Distance(new Vector3(hit.position.x, 0f, hit.position.z), checkPos) > 0.5f)
        {
            ShowOutOfBounds();
        }
        else
        {
            HideOutOfBounds();
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
