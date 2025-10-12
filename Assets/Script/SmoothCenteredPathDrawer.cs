using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class SmoothCenteredPathDrawer : MonoBehaviour
{
    [Header("References")]
    public Transform startPoint;      // e.g., your AR Camera or Player
    private Transform targetPoint;    // set dynamically from Firestore

    [Header("Line Settings")]
    public float heightOffset = 0.02f;
    public float sampleRadius = 0.5f;
    public int smoothness = 10;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
    }

    void Update()
    {
        if (!startPoint || !targetPoint)
            return;

        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(startPoint.position, targetPoint.position, NavMesh.AllAreas, path);

        if (!hasPath || path.corners.Length < 2)
        {
            line.positionCount = 0;
            return;
        }

        DrawSmoothCenteredPath(path);
    }

    void DrawSmoothCenteredPath(NavMeshPath path)
    {
        List<Vector3> centeredPoints = new List<Vector3>();

        foreach (var corner in path.corners)
        {
            Vector3 finalPos = corner;

            if (NavMesh.SamplePosition(corner, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
                finalPos = hit.position;

            centeredPoints.Add(finalPos + Vector3.up * heightOffset);
        }

        List<Vector3> smoothPoints = SmoothPath(centeredPoints, smoothness);

        line.positionCount = smoothPoints.Count;
        line.SetPositions(smoothPoints.ToArray());
    }

    List<Vector3> SmoothPath(List<Vector3> pts, int smoothness)
    {
        if (pts.Count < 2) return pts;

        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = (i + 2 < pts.Count) ? pts[i + 2] : pts[i + 1];

            for (int j = 0; j < smoothness; j++)
            {
                float t = j / (float)smoothness;
                Vector3 pos = GetCatmullRomPosition(t, p0, p1, p2, p3);
                result.Add(pos);
            }
        }

        result.Add(pts[pts.Count - 1]);
        return result;
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t
        );
    }

    // ✅ Call this from your Firestore or dropdown script
    public void SetTarget(Transform newTarget)
    {
        targetPoint = newTarget;
    }
}
