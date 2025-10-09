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
        if (db == null)
        {
            Debug.LogError("Firestore DB instance is null. FetchData aborted.");
            return;
        }

        Debug.Log($"Starting FetchData for collection '{collectionName}'");

        db.Collection(collectionName).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error fetching data: " + (task.Exception?.Flatten().Message ?? task.Exception?.Message ?? "unknown"));
                return;
            }

            try
            {
                QuerySnapshot snapshot = task.Result;
                Debug.Log($"Fetched {snapshot?.Count ?? 0} documents from '{collectionName}'.");

                if (wrapper == null) wrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
                if (wrapper.TargetList == null) wrapper.TargetList = new List<TargetData>();
                wrapper.TargetList.Clear();

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc == null)
                    {
                        Debug.LogWarning("Encountered null DocumentSnapshot, skipping.");
                        continue;
                    }

                    if (!doc.Exists)
                    {
                        Debug.LogWarning($"Document {doc.Id} does not exist, skipping.");
                        continue;
                    }

                    Debug.Log($"Processing doc: {doc.Id}");
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
                        Position = new PositionData
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
                Debug.Log($"Attempting to write TargetData.json to: {fullPath}");

                try
                {
                    File.WriteAllText(fullPath, json);
                    Debug.Log($"✅ Saved updated TargetData.json at: {fullPath} (length {json.Length})");

                    // verify write by reading back
                    if (File.Exists(fullPath))
                    {
                        string readBack = File.ReadAllText(fullPath);
                        Debug.Log($"Read-back length: {readBack.Length}");
                    }
                    else
                    {
                        Debug.LogWarning("Read-back failed: file missing after write.");
                    }

                    // ensure we have a TargetHandler reference and refresh it
                    if (targetHandler == null)
                    {
                        targetHandler = FindObjectOfType<TargetHandler>();
                        Debug.Log(targetHandler == null ? "No TargetHandler found to refresh." : "Found TargetHandler - refreshing it.");
                    }

                    targetHandler?.RefreshTargetData();
                }
                catch (System.Exception ioEx)
                {
                    Debug.LogError($"Failed to write/read JSON: {ioEx.Message}\n{ioEx.StackTrace}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unhandled error processing Firestore snapshot: {ex.Message}\n{ex.StackTrace}");
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

[System.Serializable]
public class TargetListWrapper
{
    public List<TargetData> TargetList;
}

[System.Serializable]
public class TargetData
{
    public string Name;
    public string Building;
    public string BuildingId;
    public string CreatedAt;
    public int FloorNumber;
    public string FloorId;
    public string Image;
    public PositionData Position;
}

[System.Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}
