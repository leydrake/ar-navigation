using UnityEngine;

public class CameraFloorFollow : MonoBehaviour
{
    public Transform agent;             // The virtual agent (the one walking on NavMesh)
    public Camera topDownCamera;        // The minimap camera (orthographic, top-down)

    public float floor1Y = 10f;         // Height for first floor camera
    public float floor2Y = 15f;         // Height for second floor camera
    public float threshold = 2.5f;      // Y-position that separates floors

    private bool isOnSecondFloor = false;

    void Update()
    {
        if (agent == null || topDownCamera == null)
            return;

        float agentY = agent.position.y;

        // Floor switch logic
        if (agentY > threshold && !isOnSecondFloor)
        {
            // Agent moved to second floor
            MoveCameraToY(floor2Y);
            isOnSecondFloor = true;
        }
        else if (agentY <= threshold && isOnSecondFloor)
        {
            // Agent moved to first floor
            MoveCameraToY(floor1Y);
            isOnSecondFloor = false;
        }
    }

    private void MoveCameraToY(float targetY)
    {
        Vector3 camPos = topDownCamera.transform.position;
        camPos.y = targetY;
        topDownCamera.transform.position = camPos;
    }
}
