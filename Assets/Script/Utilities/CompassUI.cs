using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    public RectTransform compassArrow;
    public float smoothSpeed = 5f; // Higher = faster, Lower = smoother

    void Start()
    {
        Input.compass.enabled = true;
        Input.location.Start();
    }

    void Update()
    {
        // Target heading (0 = North)
        float targetHeading = Input.compass.trueHeading;

        // Get current z rotation
        float currentZ = compassArrow.localEulerAngles.z;

        // Compute smooth rotation (interpolating angles properly)
        float newZ = Mathf.LerpAngle(currentZ, -targetHeading, Time.deltaTime * smoothSpeed);

        // Apply
        compassArrow.localEulerAngles = new Vector3(0, 0, newZ);
    }
}
