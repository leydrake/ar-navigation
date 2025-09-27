using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using System.IO;

public class FirestoreToCustomJson : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Firestore Settings")]
    public string collectionName = "targets";

    [Header("Local Model JSON (Drag & Drop)")]
    [SerializeField] private TextAsset targetModelData; // Base JSON file
    
    [Header("JSON File Path")]
    [SerializeField] private string jsonFilePath = "StreamingAssets/TargetData.json"; // Path to save the updated JSON
    
    [Header("Target Handler Reference")]
    [SerializeField] private TargetHandler targetHandler; // Reference to TargetHandler to refresh data

    private TargetListWrapper wrapper;
    
    // Public property to access the current JSON data
    public string CurrentJsonData => JsonUtility.ToJson(wrapper, true);
    public TargetListWrapper CurrentWrapper => wrapper;

    void Start()
    {
        // Load base JSON (if assigned in Inspector)
        if (targetModelData != null)
        {
            wrapper = JsonUtility.FromJson<TargetListWrapper>(targetModelData.text);
            Debug.Log("Loaded base JSON model from TextAsset.");
        }
        else
        {
            wrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
            Debug.Log("Created new empty JSON wrapper.");
        }

        // Init Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                FetchData();
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
            }
        });
    }

    void FetchData()
    {
        db.Collection(collectionName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error fetching data: " + task.Exception);
                return;
            }

            QuerySnapshot snapshot = task.Result;

            // Replace with new data (if you want merge, remove this line)
            wrapper.TargetList.Clear();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                Dictionary<string, object> docDict = doc.ToDictionary();

                TargetData target = new TargetData
                {
                    Name = docDict.ContainsKey("name") ? docDict["name"].ToString() : "Unknown",
                    FloorNumber = docDict.ContainsKey("floor") ? GetInt(docDict, "floor") : 0,
                    Position = new PositionData
                    {
                        x = GetFloat(docDict, "x"),
                        y = GetFloat(docDict, "y"),
                        z = GetFloat(docDict, "z")
                    },
                    Rotation = new RotationData
                    {
                        x = docDict.ContainsKey("rotX") ? GetFloat(docDict, "rotX") : 0f,
                        y = docDict.ContainsKey("rotY") ? GetFloat(docDict, "rotY") : 0f,
                        z = docDict.ContainsKey("rotZ") ? GetFloat(docDict, "rotZ") : 0f
                    }
                };

                wrapper.TargetList.Add(target);
            }

            // Save updated JSON to file
            string json = JsonUtility.ToJson(wrapper, true);
            
            // Write the JSON data to file
            try
            {
                // Use Application.streamingAssetsPath for writable location
                string fullPath = Path.Combine(Application.streamingAssetsPath, "TargetData.json");
                
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write the JSON data to the file
                File.WriteAllText(fullPath, json);
                Debug.Log($"Successfully wrote updated JSON to: {fullPath}");
                Debug.Log($"Updated JSON data:\n{json}");
                
                // Refresh the TargetHandler if reference is available
                if (targetHandler != null) {
                    targetHandler.RefreshTargetData();
                } else {
                    Debug.LogWarning("TargetHandler reference not set. Target data won't be refreshed automatically.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write JSON file: {e.Message}");
            }
        });
    }

    float GetFloat(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key))
        {
            float result;
            if (float.TryParse(dict[key].ToString(), out result))
                return result;
        }
        return 0f;
    }

    int GetInt(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key))
        {
            int result;
            if (int.TryParse(dict[key].ToString(), out result))
                return result;
        }
        return 0;
    }
}

[System.Serializable]
public class TargetListWrapper
{
    public List<TargetData> TargetList;
}

[System.Serializable]
public class TargetData
{
    public string Name;
    public int FloorNumber;
    public PositionData Position;
    public RotationData Rotation;
}

[System.Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class RotationData
{
    public float x;
    public float y;
    public float z;
}
