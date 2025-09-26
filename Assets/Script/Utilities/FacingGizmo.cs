using UnityEngine;

public class FacingGizmo : MonoBehaviour
{
    public Color arrowColor = Color.red;   // Change arrow color if you want
    public float arrowLength = 2f;         // How long the arrow should be

    private void OnDrawGizmos()
    {
        Gizmos.color = arrowColor;
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * arrowLength;

        // Draw the main arrow line
        Gizmos.DrawLine(start, end);

        // Draw arrow head
        Vector3 right = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;

        Gizmos.DrawLine(end, end + right * 0.5f);
        Gizmos.DrawLine(end, end + left * 0.5f);
    }
}
