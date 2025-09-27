using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirestoreToCustomJson : MonoBehaviour
{
    FirebaseFirestore db;

    [Header("Firestore Settings")]
    public string collectionName = "targets";

    [Header("Local Model JSON (Drag & Drop)")]
    [SerializeField] private TextAsset targetModelData; // Base JSON file

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

            // Save updated JSON to targetModelData
            string json = JsonUtility.ToJson(wrapper, true);
            
            // Update the targetModelData TextAsset with new JSON
            if (targetModelData != null)
            {
                // Note: TextAsset.text is read-only at runtime, so we'll store the JSON in a variable
                // You can access this data through the wrapper.TargetList property
                Debug.Log("Updated JSON data stored in targetModelData wrapper.");
                Debug.Log(json);
            }
            else
            {
                Debug.LogWarning("targetModelData is null. JSON data is available in wrapper.TargetList");
                Debug.Log(json);
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
