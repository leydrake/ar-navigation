using UnityEngine;
using UnityEngine.AI;

public class NavigationController : MonoBehaviour
{
    public Vector3 TargetPosition { get; set; } = Vector3.zero;
    public NavMeshPath CalculatedPath { get; private set; }

    public bool IsActive { get; private set; } = true; // ✅ new flag

    private void Start()
    {
        CalculatedPath = new NavMeshPath();
    }

    private void Update()
    {
        if (!IsActive) return; // ✅ stop recalculating when frozen

        if (TargetPosition != Vector3.zero)
        {
            NavMesh.CalculatePath(transform.position, TargetPosition, NavMesh.AllAreas, CalculatedPath);
        }
    }

    public void StopNavigation()
    {
        IsActive = false;
    }
}
