using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Firestore;

public class ARAdminSaverTMP : MonoBehaviour
{
    [Header("References")]
    public Transform arCameraTransform;        // Your AR Camera
    public TMP_InputField locationNameInput;   // TMP Input Field for location name
    public Button saveButton;                  // Button to trigger save
    public TMP_Text statusText;                // TMP Text for feedback

    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        if (statusText != null)
            statusText.text = "Ready to save AR positions.";

        // Add listener to button
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentARPosition);
    }

    public void SaveCurrentARPosition()
    {
        if (arCameraTransform == null)
        {
            if (statusText != null) statusText.text = "AR Camera not assigned!";
            return;
        }

        string locationName = string.IsNullOrEmpty(locationNameInput.text) 
            ? "Unnamed Location" 
            : locationNameInput.text;

        Vector3 pos = arCameraTransform.position;
        Quaternion rot = arCameraTransform.rotation;

        var data = new
        {
            Name = locationName,
            PositionX = pos.x,
            PositionY = pos.y,
            PositionZ = pos.z,
            RotationX = rot.eulerAngles.x,
            RotationY = rot.eulerAngles.y,
            RotationZ = rot.eulerAngles.z,
            Timestamp = Timestamp.GetCurrentTimestamp()
        };

        db.Collection("ARLocations").AddAsync(data).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                if (statusText != null)
                    statusText.text = $"Saved '{locationName}' at ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})";
            }
            else
            {
                if (statusText != null)
                    statusText.text = "Error saving position.";
                Debug.LogError(task.Exception);
            }
        });
    }
}
