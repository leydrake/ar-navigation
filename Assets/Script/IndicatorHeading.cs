using UnityEngine;

public class IndicatorHeading : MonoBehaviour
{
    public Transform arCamera; // assign your AR camera here
    public float rotationSpeed = 10f; // smooth turning

    void Update()
    {
        // Get the camera's forward direction
        Vector3 forward = arCamera.forward;

        // Keep only horizontal rotation (ignore Y)
        forward.y = 0;

        if (forward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
