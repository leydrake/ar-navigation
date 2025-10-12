using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Optimized centering: Only recalculates centered path when corners actually change
/// </summary>
public class PathLineVisualisation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavigationController navigationController;
    [SerializeField] private LineRenderer line;
    [SerializeField] private Slider navigationYOffset;

    [Header("Centering Settings")]
    [SerializeField] private bool enableCentering = true;
    [SerializeField] private float maxSweepDistance = 1.5f;
    [SerializeField] private float sweepStep = 0.1f; // Increased from 0.05 for performance
    [SerializeField] private float sampleRadius = 0.2f;

    [Header("Optimization")]
    [SerializeField] private float recenterInterval = 0.2f; // Only recenter every 0.2s

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool logSampling = false;

    private NavMeshPath path;
    private Vector3[] calculatedPathAndOffset;
    private Vector3[] lastPathCorners;
    private Vector3[] cachedCenteredPath;
    private float lastRecenterTime;

    private void Update()
    {
        path = navigationController.CalculatedPath;
        
        if (path == null || path.corners.Length == 0)
        {
            line.positionCount = 0;
            return;
        }

        UpdatePathWithOffset();
        UpdateLineRenderer();
    }

    private void UpdatePathWithOffset()
    {
        float yOffset = transform.position.y + navigationYOffset.value;

        // Check if we need to recalculate centering
        if (enableCentering && ShouldRecalculateCentering())
        {
            RecalculateCenteredPath();
        }

        // Use cached centered path or original corners
        Vector3[] baseCorners = (enableCentering && cachedCenteredPath != null) 
            ? cachedCenteredPath 
            : path.corners;

        calculatedPathAndOffset = new Vector3[baseCorners.Length];

        for (int i = 0; i < baseCorners.Length; i++)
        {
            Vector3 corner = baseCorners[i];
            calculatedPathAndOffset[i] = new Vector3(corner.x, yOffset, corner.z);
        }
    }

    private bool ShouldRecalculateCentering()
    {
        // Don't recalculate too frequently
        if (Time.time - lastRecenterTime < recenterInterval)
            return false;

        // Check if path corners actually changed
        if (lastPathCorners == null || lastPathCorners.Length != path.corners.Length)
            return true;

        // Check if any corner moved significantly (more than 0.1m)
        for (int i = 0; i < path.corners.Length; i++)
        {
            if (Vector3.Distance(lastPathCorners[i], path.corners[i]) > 0.1f)
                return true;
        }

        return false;
    }

    private void RecalculateCenteredPath()
    {
        lastRecenterTime = Time.time;
        lastPathCorners = (Vector3[])path.corners.Clone();

        List<Vector3> centeredPoints = new List<Vector3>();

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 corner = path.corners[i];

            // Compute tangent direction
            Vector3 dir;
            if (i < path.corners.Length - 1)
                dir = (path.corners[i + 1] - corner).normalized;
            else if (i > 0)
                dir = (corner - path.corners[i - 1]).normalized;
            else
                dir = Vector3.forward;

            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;

            // Perpendicular left/right
            Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized;

            // Sweep left and right to find walkable bounds
            bool foundLeft = false, foundRight = false;
            Vector3 lastLeft = corner, lastRight = corner;

            // Check center point first
            if (NavMesh.SamplePosition(corner, out NavMeshHit centerHit, sampleRadius, NavMesh.AllAreas))
            {
                lastLeft = lastRight = centerHit.position;
                foundLeft = foundRight = true;
            }

            // Sweep for bounds (optimized with larger steps)
            for (float d = sweepStep; d <= maxSweepDistance; d += sweepStep)
            {
                Vector3 testR = corner + perp * d;
                if (NavMesh.SamplePosition(testR, out NavMeshHit hitR, sampleRadius, NavMesh.AllAreas))
                {
                    lastRight = hitR.position;
                    foundRight = true;
                }

                Vector3 testL = corner - perp * d;
                if (NavMesh.SamplePosition(testL, out NavMeshHit hitL, sampleRadius, NavMesh.AllAreas))
                {
                    lastLeft = hitL.position;
                    foundLeft = true;
                }
            }

            // Determine centered position
            Vector3 chosen = corner;

            if (foundLeft && foundRight)
            {
                chosen = (lastLeft + lastRight) * 0.5f;
            }
            else if (foundLeft)
            {
                chosen = lastLeft + perp * (sweepStep * 0.5f);
            }
            else if (foundRight)
            {
                chosen = lastRight - perp * (sweepStep * 0.5f);
            }
            else
            {
                if (NavMesh.SamplePosition(corner, out NavMeshHit finalHit, sampleRadius * 2f, NavMesh.AllAreas))
                    chosen = finalHit.position;
                else
                    chosen = corner;
            }

            centeredPoints.Add(chosen);
        }

        cachedCenteredPath = centeredPoints.ToArray();
        
        if (logSampling)
            Debug.Log($"Recalculated centered path with {cachedCenteredPath.Length} points");
    }

    private void UpdateLineRenderer()
    {
        if (calculatedPathAndOffset == null || calculatedPathAndOffset.Length == 0) return;

        line.positionCount = calculatedPathAndOffset.Length;
        line.SetPositions(calculatedPathAndOffset);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || calculatedPathAndOffset == null) return;

        Gizmos.color = enableCentering ? Color.cyan : Color.yellow;
        for (int i = 0; i < calculatedPathAndOffset.Length; i++)
        {
            Gizmos.DrawSphere(calculatedPathAndOffset[i], 0.03f);
        }
    }
}