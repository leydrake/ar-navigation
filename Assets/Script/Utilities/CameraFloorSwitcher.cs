using UnityEngine;

public class CameraFloorSwitcher : MonoBehaviour
{
    public float firstFloorY = 3f;
    public float secondFloorY = 9f;
    public float moveSpeed = 5f;

    private float targetY;

    private void Start()
    {
        // Start on the first floor
        targetY = firstFloorY;
        SetCameraY(targetY);
    }

    private void Update()
    {
        // Smoothly move to the target Y position
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = new Vector3(currentPosition.x, targetY, currentPosition.z);
        transform.position = Vector3.Lerp(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
    }

    // Call these from buttons
    public void GoToFirstFloor()
    {
        targetY = firstFloorY;
    }

    public void GoToSecondFloor()
    {
        targetY = secondFloorY;
    }

    private void SetCameraY(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
}
