using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARCameraStabilizer : MonoBehaviour
{
    [SerializeField] private Camera arCamera;           // Drag Main Camera here
    [SerializeField] private ARAnchorManager anchorManager; // Drag AR Session Origin's ARAnchorManager here
    [SerializeField] private float smoothFactor = 0.1f;

    private ARAnchor cameraAnchor;
    private Vector3 smoothedLocalPos;

    void Start()
    {
        Pose anchorPose = new Pose(arCamera.transform.position, arCamera.transform.rotation);
        cameraAnchor = anchorManager.AddAnchor(anchorPose);

        if (cameraAnchor == null)
        {
            Debug.LogError("Failed to create ARAnchor!");
            return;
        }

        arCamera.transform.SetParent(cameraAnchor.transform);
        smoothedLocalPos = arCamera.transform.localPosition;
    }

    void LateUpdate()
    {
        smoothedLocalPos = Vector3.Lerp(smoothedLocalPos, arCamera.transform.localPosition, smoothFactor);
        smoothedLocalPos.y = 0f; // optional Y-lock
        arCamera.transform.localPosition = smoothedLocalPos;
    }
}
 