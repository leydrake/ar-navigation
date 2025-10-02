using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;

public class ARAdminSaverTMP : MonoBehaviour
{
    [Header("References")]
    public Transform arCameraTransform;        // Your AR Camera
    public TMP_InputField locationNameInput;   // TMP Input Field for location name
    public Button saveButton;                  // Button to trigger save
    public TMP_Text statusText;                // TMP Text for feedback

    private FirebaseFirestore db;
    private bool firebaseReady = false;

    void Start()
    {
        Debug.Log("[ARAdminSaver] Starting ARAdminSaver...");
        Debug.Log($"[ARAdminSaver] Platform: {Application.platform}");
        Debug.Log($"[ARAdminSaver] Device model: {SystemInfo.deviceModel}");
        Debug.Log($"[ARAdminSaver] Unity version: {Application.unityVersion}");
        
        // Initialize Firebase properly
        InitializeFirebase();
        
        if (statusText != null)
            statusText.text = "Initializing Firebase...";

        // Add listener to button
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentARPosition);
        else
            Debug.LogError("[ARAdminSaver] Save button not assigned!");
            
        // Add debug button for testing
        if (saveButton != null)
            saveButton.onClick.AddListener(LogDebugInfo);
    }
    
    public void LogDebugInfo()
    {
        Debug.Log("=== ARAdminSaver Debug Info ===");
        Debug.Log($"Firebase ready: {firebaseReady}");
        Debug.Log($"Database instance: {(db != null ? "Available" : "Null")}");
        Debug.Log($"AR Camera: {(arCameraTransform != null ? "Assigned" : "Not assigned")}");
        Debug.Log($"Location input: {(locationNameInput != null ? "Assigned" : "Not assigned")}");
        Debug.Log($"Status text: {(statusText != null ? "Assigned" : "Not assigned")}");
        Debug.Log($"Save button: {(saveButton != null ? "Assigned" : "Not assigned")}");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Internet reachability: {Application.internetReachability}");
        Debug.Log($"Device model: {SystemInfo.deviceModel}");
        Debug.Log($"Unity version: {Application.unityVersion}");
        
        if (arCameraTransform != null)
        {
            Debug.Log($"Camera position: {arCameraTransform.position}");
            Debug.Log($"Camera rotation: {arCameraTransform.rotation.eulerAngles}");
        }
        Debug.Log("=== End Debug Info ===");
    }

    private void InitializeFirebase()
    {
        Debug.Log("[ARAdminSaver] Starting Firebase initialization...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log($"[ARAdminSaver] Firebase dependency check completed. Status: {task.Result}");
            
            if (task.Result == DependencyStatus.Available)
            {
                try
                {
                    db = FirebaseFirestore.DefaultInstance;
                    firebaseReady = true;
                    
                    if (statusText != null)
                        statusText.text = "Ready to save AR positions.";
                        
                    Debug.Log("[ARAdminSaver] Firebase initialized successfully");
                    Debug.Log($"[ARAdminSaver] Firebase app name: {FirebaseApp.DefaultInstance.Name}");
                    Debug.Log($"[ARAdminSaver] Firebase app options: {FirebaseApp.DefaultInstance.Options}");
                }
                catch (System.Exception ex)
                {
                    firebaseReady = false;
                    if (statusText != null)
                        statusText.text = $"Firebase setup error: {ex.Message}";
                    Debug.LogError($"[ARAdminSaver] Firebase setup exception: {ex}");
                }
            }
            else
            {
                firebaseReady = false;
                string errorMsg = $"Firebase initialization failed: {task.Result}";
                if (statusText != null)
                    statusText.text = errorMsg;
                    
                Debug.LogError($"[ARAdminSaver] {errorMsg}");
                
                // Additional debugging info
                Debug.LogError($"[ARAdminSaver] Platform: {Application.platform}");
                Debug.LogError($"[ARAdminSaver] Device model: {SystemInfo.deviceModel}");
                Debug.LogError($"[ARAdminSaver] Unity version: {Application.unityVersion}");
            }
        });
    }

    public void SaveCurrentARPosition()
    {
        Debug.Log("[ARAdminSaver] SaveCurrentARPosition called");
        
        // Check Firebase readiness first
        if (!firebaseReady)
        {
            string msg = "Firebase not ready yet. Please wait...";
            if (statusText != null) 
                statusText.text = msg;
            Debug.LogWarning($"[ARAdminSaver] {msg}");
            return;
        }

        if (db == null)
        {
            string msg = "Firebase database not available!";
            if (statusText != null) 
                statusText.text = msg;
            Debug.LogError($"[ARAdminSaver] {msg}");
            return;
        }

        if (arCameraTransform == null)
        {
            string msg = "AR Camera not assigned!";
            if (statusText != null) 
                statusText.text = msg;
            Debug.LogError($"[ARAdminSaver] {msg}");
            return;
        }

        // Check network connectivity
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            string msg = "No internet connection!";
            if (statusText != null)
                statusText.text = msg;
            Debug.LogError($"[ARAdminSaver] {msg}");
            return;
        }

        string locationName = string.IsNullOrEmpty(locationNameInput.text) 
            ? "Unnamed Location" 
            : locationNameInput.text;

        Vector3 pos = arCameraTransform.position;
        Quaternion rot = arCameraTransform.rotation;

        Debug.Log($"[ARAdminSaver] Camera position: {pos}");
        Debug.Log($"[ARAdminSaver] Camera rotation: {rot.eulerAngles}");

        if (statusText != null)
            statusText.text = "Saving position...";

        try
        {
            var data = new
            {
                Name = locationName,
                PositionX = pos.x,
                PositionY = pos.y,
                PositionZ = pos.z,
                RotationX = rot.eulerAngles.x,
                RotationY = rot.eulerAngles.y,
                RotationZ = rot.eulerAngles.z,
                Timestamp = Timestamp.GetCurrentTimestamp(),
                Platform = Application.platform.ToString(),
                DeviceModel = SystemInfo.deviceModel,
                UnityVersion = Application.unityVersion,
                NetworkReachability = Application.internetReachability.ToString()
            };

            Debug.Log($"[ARAdminSaver] Attempting to save position: {locationName} at ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            Debug.Log($"[ARAdminSaver] Data to save - Name: {data.Name}, Pos: ({data.PositionX:F2}, {data.PositionY:F2}, {data.PositionZ:F2}), Rot: ({data.RotationX:F2}, {data.RotationY:F2}, {data.RotationZ:F2})");

            db.Collection("ARLocations").AddAsync(data).ContinueWithOnMainThread(task =>
            {
                Debug.Log($"[ARAdminSaver] Save task completed. Status: {task.Status}");
                
                if (task.IsCompletedSuccessfully)
                {
                    string successMsg = $"✅ Saved '{locationName}' at ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})";
                    if (statusText != null)
                        statusText.text = successMsg;
                    Debug.Log($"[ARAdminSaver] Successfully saved position: {locationName}");
                }
                else if (task.IsFaulted)
                {
                    string errorMsg = "❌ Error saving position.";
                    if (statusText != null)
                        statusText.text = errorMsg;
                    
                    Debug.LogError($"[ARAdminSaver] Save failed: {task.Exception?.GetBaseException()?.Message}");
                    Debug.LogError($"[ARAdminSaver] Full exception: {task.Exception}");
                    
                    // Log additional context for mobile debugging
                    Debug.LogError($"[ARAdminSaver] Platform: {Application.platform}");
                    Debug.LogError($"[ARAdminSaver] Internet reachability: {Application.internetReachability}");
                    Debug.LogError($"[ARAdminSaver] Device model: {SystemInfo.deviceModel}");
                }
                else if (task.IsCanceled)
                {
                    string cancelMsg = "❌ Save operation was cancelled.";
                    if (statusText != null)
                        statusText.text = cancelMsg;
                    Debug.LogWarning("[ARAdminSaver] Save operation was cancelled");
                }
            });
        }
        catch (System.Exception ex)
        {
            string msg = $"❌ Exception during save: {ex.Message}";
            if (statusText != null)
                statusText.text = msg;
            Debug.LogError($"[ARAdminSaver] Exception in SaveCurrentARPosition: {ex}");
        }
    }
}
