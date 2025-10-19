using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PathArrowVisualisation : MonoBehaviour
{
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private GameObject arrow;
    [SerializeField] private Slider navigationYOffset;
    [SerializeField] private float moveOnDistance = 1.0f;

    [Header("Mode Settings")]
    [SerializeField] private bool isWorldMode = true; // true = on-ground mode, false = front-facing mode

    private NavMeshPath path;
    private float currentDistance;
    private Vector3[] pathOffset;
    private Vector3 nextNavigationPoint = Vector3.zero;

    void Update()
    {
        if (arrow == null || navigationController == null) return;

        path = navigationController.CalculatedPath;

        AddOffsetToPath();
        SelectNextNavigationPoint();

        if (isWorldMode)
        {
            UpdateWorldArrow();
        }
        else
        {
            UpdateFrontArrow();
        }
    }

    // --- WORLD MODE (arrow on ground following NavMesh) ---
    private void UpdateWorldArrow()
    {
        AddArrowOffset();
        arrow.transform.LookAt(nextNavigationPoint);
    }

    // --- FRONT MODE (arrow in front of camera) ---
    private void UpdateFrontArrow()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Keep arrow always in front of camera
        Vector3 forwardPosition = cam.transform.position + cam.transform.forward * 1.5f;
        float yOffset = navigationYOffset != null ? navigationYOffset.value : 0.5f;
        forwardPosition.y = cam.transform.position.y + yOffset;

        arrow.transform.position = forwardPosition;

        // Rotate arrow toward next navigation point
        arrow.transform.rotation = Quaternion.LookRotation(nextNavigationPoint - cam.transform.position);
    }

    private void AddOffsetToPath()
    {
        if (path == null || path.corners.Length == 0) return;

        pathOffset = new Vector3[path.corners.Length];
        for (int i = 0; i < path.corners.Length; i++)
        {
            pathOffset[i] = new Vector3(path.corners[i].x, transform.position.y, path.corners[i].z);
        }
    }

    private void SelectNextNavigationPoint()
    {
        nextNavigationPoint = SelectNextNavigationPointWithinDistance();
    }

    private Vector3 SelectNextNavigationPointWithinDistance()
    {
        if (pathOffset == null || pathOffset.Length == 0)
            return navigationController.TargetPosition;

        for (int i = 0; i < pathOffset.Length; i++)
        {
            currentDistance = Vector3.Distance(transform.position, pathOffset[i]);
            if (currentDistance > moveOnDistance)
            {
                return pathOffset[i];
            }
        }
        return navigationController.TargetPosition;
    }

    private void AddArrowOffset()
    {
        if (arrow == null) return;

        Vector3 pos = arrow.transform.position;
        if (navigationYOffset != null)
        {
            pos.y = transform.position.y + navigationYOffset.value;
        }
        arrow.transform.position = pos;
    }

    // --- BUTTON HANDLER ---
    public void ToggleArrowMode()
    {
        isWorldMode = !isWorldMode;
        Debug.Log("Arrow Mode: " + (isWorldMode ? "World-Ground" : "Front-Camera"));
    }
}
