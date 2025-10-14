using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class ARCameraKeyboardSimulator : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float moveSpeed = 2f;          // Movement speed (m/s)
    public float rotationSpeed = 70f;     // Turn speed (degrees/s)
    public bool useRotation = true;       // Toggle for turning camera

    void Update()
    {
#if UNITY_EDITOR
        // Only simulate in the editor (won't affect builds)
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;

        // Move relative to camera facing direction
        transform.Translate(move, Space.Self);

        if (useRotation)
        {
            if (Input.GetKey(KeyCode.Q))
                transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);

            if (Input.GetKey(KeyCode.E))
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
#endif
    }
}
