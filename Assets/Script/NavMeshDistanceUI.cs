using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class NavMeshDistanceUI : MonoBehaviour
{
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private Transform player;
    [SerializeField] private TextMeshProUGUI distanceText;

    private NavMeshPath path;
    private bool hasArrived = false;
    private Vector3 previousTarget = Vector3.zero;
    private const float targetChangeEpsilonSqr = 0.0001f;

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
        // basic safety
        if (navigationController == null || player == null || distanceText == null)
            return;

        Vector3 targetPos = navigationController.TargetPosition;

        // detect target changed (including selecting a new target or clearing target)
        if ((targetPos - previousTarget).sqrMagnitude > targetChangeEpsilonSqr)
        {
            // if we had previously arrived to old target, clear arrival so UI updates for the new target
            if (hasArrived)
            {
                hasArrived = false;
                Debug.Log("NavMeshDistanceUI: target changed — clearing arrived state.");
            }

            previousTarget = targetPos;
        }

        if (hasArrived)
        {
            // keep the "You've Arrived!" message visible until target changes
            return;
        }

        if (targetPos == Vector3.zero)
        {
            distanceText.text = "<size=44><color=#FF0000CC>No location selected</color></size>";
            return;
        }

        float distance = GetPathLength(player.position, targetPos);

        // ✅ Stop *everything* instantly once within 1m
        if (distance <= 1f)
        {
            hasArrived = true;

            // Then show the message (won’t get overwritten anymore until target changes)
            distanceText.text =
                "<size=44><color=#00C853><b>You've Arrived!</b></color></size>";

            // NOTE: do NOT disable this component anymore — we need to detect future target changes.
            return;
        }

        // Normal distance display
        distanceText.text =
            $"<size=36><color=#000000>Distance</color></size>\n" +
            $"<size=64><b><color=#000000>{distance:F1} M</color></b></size>";
    }

    private float GetPathLength(Vector3 start, Vector3 end)
    {
        NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
        if (path.corners.Length < 2) return 0f;

        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }
}
