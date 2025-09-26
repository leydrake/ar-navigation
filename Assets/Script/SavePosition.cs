using UnityEngine;
using Firebase.Firestore;

public class SavePosition : MonoBehaviour
{
    FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    public void SaveObjectPosition(GameObject obj, string objectName)
    {
        if (obj == null)
        {
            Debug.LogError("GameObject is null! Cannot save position.");
            return;
        }

        if (string.IsNullOrEmpty(objectName))
        {
            Debug.LogError("Object name is null or empty! Cannot save position.");
            return;
        }

        Vector3 pos = obj.transform.position;
        Vector3 rot = obj.transform.rotation.eulerAngles;

        var data = new
        {
            name = objectName,
            x = pos.x,
            y = pos.y,
            z = pos.z,
            rotationX = rot.x,
            rotationY = rot.y,
            rotationZ = rot.z,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        };

        Debug.Log($"Saving position for {objectName}: {pos}");

        db.Collection("coordinates").Document(objectName).SetAsync(data).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"✓ Position saved successfully for {objectName} at {pos}");
            }
            else
            {
                Debug.LogError($"✗ Failed to save position for {objectName}: {task.Exception}");
            }
        });
    }

    // Example usage method - you can call this from other scripts or UI buttons
    public void SaveAnchor1Position()
    {
        GameObject anchor1 = GameObject.Find("Anchor1");
        if (anchor1 != null)
        {
            SaveObjectPosition(anchor1, "Anchor1");
        }
        else
        {
            Debug.LogError("Anchor1 GameObject not found!");
        }
    }
}
