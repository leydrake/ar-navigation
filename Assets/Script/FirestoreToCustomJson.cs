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
    [SerializeField] private TextAsset targetModelData;

    [Header("Target Handler Reference")]
    [SerializeField] private TargetHandler targetHandler;

    private TargetListWrapper wrapper;

    void Start()
    {
        // Load base JSON
        if (targetModelData != null)
        {
            wrapper = JsonUtility.FromJson<TargetListWrapper>(targetModelData.text);
        }
        else
        {
            wrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
        }

        // Init Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                FetchData();
            }
            
        });
    }

    void FetchData()
    {
        if (db == null)
        {
            return;
        }


        db.Collection(collectionName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                return;
            }

            try
            {
                QuerySnapshot snapshot = task.Result;

                if (wrapper == null) wrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
                if (wrapper.TargetList == null) wrapper.TargetList = new List<TargetData>();
                wrapper.TargetList.Clear();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc == null)
                    {
                        continue;
                    }

                    if (!doc.Exists)
                    {
                        continue;
                    }

                    Dictionary<string, object> data = doc.ToDictionary() ?? new Dictionary<string, object>();

                    TargetData target = new TargetData
                    {
                        Name = data.ContainsKey("name") && data["name"] != null ? data["name"].ToString() : "Unknown",
                        Building = data.ContainsKey("building") && data["building"] != null ? data["building"].ToString() : "",
                        BuildingId = data.ContainsKey("buildingId") && data["buildingId"] != null ? data["buildingId"].ToString() : "",
                        FloorNumber = GetInt(data, "floor"),
                        FloorId = data.ContainsKey("floorId") && data["floorId"] != null ? data["floorId"].ToString() : "",
                        Image = data.ContainsKey("image") && data["image"] != null ? data["image"].ToString() : "",
                        CreatedAt = data.ContainsKey("createdAt") && data["createdAt"] != null ? data["createdAt"].ToString() : "",
                        Position = new TargetPosition
                        {
                            x = GetFloat(data, "x"),
                            y = GetFloat(data, "y"),
                            z = GetFloat(data, "z")
                        }
                    };

                    wrapper.TargetList.Add(target);
                }

                string json = JsonUtility.ToJson(wrapper, true);
                string fileName = "TargetData.json";
                string fullPath = Path.Combine(Application.persistentDataPath, fileName);

                try
                {
                    File.WriteAllText(fullPath, json);

                    // verify write by reading back
                    if (File.Exists(fullPath))
                    {
                        string readBack = File.ReadAllText(fullPath);
                    }
                    else
                    {
                    }

                    // ensure we have a TargetHandler reference and refresh it
                    if (targetHandler == null)
                    {
                        targetHandler = FindObjectOfType<TargetHandler>();
                    }

                    targetHandler?.RefreshTargetData();
                }
                catch (System.Exception ioEx)
                {
                }
            }
            catch (System.Exception ex)
            {
            }
        });
    }

    float GetFloat(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key) || dict[key] == null) return 0f;

        object val = dict[key];

        if (val is float f) return f;
        if (val is double d) return (float)d;
        if (val is long l) return (float)l;
        if (val is int i) return (float)i;

        string s = val.ToString();
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
            return parsed;

        return 0f;
    }

    int GetInt(Dictionary<string, object> dict, string key)
    {
        if (dict == null || !dict.ContainsKey(key) || dict[key] == null) return 0;

        object val = dict[key];

        if (val is int i) return i;
        if (val is long l) return (int)l;
        if (val is double d) return Mathf.RoundToInt((float)d);

        string s = val.ToString();
        if (int.TryParse(s, out int parsed)) return parsed;
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float fp)) return Mathf.RoundToInt(fp);

        return 0;
    }
}

// Keep only TargetListWrapper here since TargetData and TargetPosition are now in TargetHandler.cs
[System.Serializable]
public class TargetListWrapper
{
    public List<TargetData> TargetList;
}