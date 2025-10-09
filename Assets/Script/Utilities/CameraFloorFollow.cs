using UnityEngine;

public class CameraFloorFollow : MonoBehaviour
{
    public Transform agent;          // The virtual agent (the one walking on NavMesh)
    public Camera topDownCamera;     // The top-down or minimap camera
    public Vector3 offset = new Vector3(0, 10f, 0);  // Height offset from the agent
    public float smoothSpeed = 5f;   // Smooth follow speed

    void LateUpdate()
    {
        if (agent == null || topDownCamera == null)
            return;

        // Desired position — follows agent in X, Y, and Z (with offset)
        Vector3 desiredPosition = agent.position + offset;

        // Smoothly interpolate camera position for fluid motion
        topDownCamera.transform.position = Vector3.Lerp(
            topDownCamera.transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Optional: Keep rotation fixed (top-down view)
        topDownCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
