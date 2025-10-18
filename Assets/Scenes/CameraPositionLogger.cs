using UnityEngine;

public class CameraPositionLogger : MonoBehaviour
{
    public Transform arCamera; // Assign your AR Camera here

    void Update()
    {
        Vector3 pos = arCamera.position;
        Debug.Log($"AR Camera Position: X={pos.x:F4}, Y={pos.y:F4}, Z={pos.z:F4}");
    }
}
