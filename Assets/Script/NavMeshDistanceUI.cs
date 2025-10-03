using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NavMeshDistanceUI : MonoBehaviour
{
    [SerializeField] private NavigationController navigationController; // drag here
    [SerializeField] private Transform player;                          // AR Camera
    [SerializeField] private TextMeshProUGUI distanceText;              // UI text

    private NavMeshPath path;

    void Start()
    {
        path = new NavMeshPath();
        if (distanceText != null)
        {
            distanceText.richText = true;
            distanceText.enableAutoSizing = false;
        }
    }

    void Update()
    {
        // Only run if we have a valid target
        if (navigationController != null && player != null)
        {
            Vector3 targetPos = navigationController.TargetPosition; // your existing code sets this

            if (targetPos != Vector3.zero)
            {
                float distance = GetPathLength(player.position, targetPos);

                // Styled label and value using TMP rich-text
                distanceText.text = $"<size=36><color=#000000>Distance</color></size>\n<size=64><b><color=#000000>{distance:F1} M</color></b></size>";
            }
            else
            {
                distanceText.text = "<size=44><color=#FF0000CC>no location selected</color></size>";
            }
        }
    }

    private float GetPathLength(Vector3 start, Vector3 end)
    {
        NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);

        if (path.corners.Length < 2)
            return 0f;

        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }
}
