using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using Firebase.Firestore;
using System;

// Shared data classes - used by both Firebase and JsonUtility
[System.Serializable]
public class TargetData
{
    public string Name;
    public string Building;
    public string BuildingId;
    public int FloorNumber;
    public string FloorId;
    public string Image;
    public string CreatedAt;
    public TargetPosition Position;
    
    // Document ID from Firestore (not serialized to JSON)
    [System.NonSerialized]
    public string id;
}

[System.Serializable]
public class TargetPosition
{
    public float x;
    public float y;
    public float z;
}

public class TargetHandler : MonoBehaviour {

    [SerializeField]
    private NavigationController navigationController;
    [SerializeField]
    private TMP_Dropdown targetDataDropdown;
    [SerializeField]
    private DropdownScrollBinder dropdownScrollBinder;
    
    [Header("Firebase Settings")]
    [SerializeField]
    private string collectionName = "coordinates";

    [SerializeField]
    private bool fetchOnStart = true;

    [SerializeField]
    private float networkTimeoutSeconds = 30f;

    [SerializeField]
    private int maxRetryAttempts = 3;

    [Header("Target Object Settings")]
    [SerializeField]
    private GameObject targetObjectPrefab;
    [SerializeField]
    private Transform[] targetObjectsParentTransforms;

    private List<TargetFacade> currentTargetItems = new List<TargetFacade>();
    private List<TargetData> targets = new List<TargetData>();

    // cache sprites created for dropdown to avoid recreating every frame
    private Dictionary<TargetFacade, Sprite> dropdownSpriteCache = new Dictionary<TargetFacade, Sprite>();

    private FirebaseFirestore db;
    private int retryCount = 0;
    private bool isInitialized = false;
    private List<TargetData> pendingTargets = null;
    private bool hasPendingTargets = false;

    // Events
    public event Action<List<TargetData>> TargetsChanged;
    public event Action<bool> LoadingChanged;
    public event Action<string> ErrorOccurred;

    private void Start() {
        Debug.Log("TargetHandler.Start()");
        StartCoroutine(InitializeWithDelay());
    }

    private void Update()
    {
        // Process pending targets on main thread
        if (hasPendingTargets && pendingTargets != null)
        {
            hasPendingTargets = false;
            
            try
            {
                GenerateTargetItems();
                FillDropdownWithTargetItems();
                TargetsChanged?.Invoke(pendingTargets);
                try { LoadingChanged?.Invoke(false); } catch (Exception) {}
            }
            catch (Exception cbEx)
            {
                Debug.LogError($"[TargetHandler] Error while processing targets: {cbEx.Message}");
            }
            
            pendingTargets = null;
        }
    }

    private IEnumerator InitializeWithDelay()
    {
        // Wait a bit for Firebase to initialize on mobile
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            yield return new WaitForSeconds(1f);
        }
        
        TryInit();
        
        // Additional delay for mobile devices
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            yield return new WaitForSeconds(2f);
        }
        
        if (fetchOnStart)
        {
            FetchAllTargets();
        }
    }

    private void TryInit()
    {
        try
        {
            // Check internet connectivity first
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogError("[TargetHandler] No internet connection detected");
                ErrorOccurred?.Invoke("No internet connection");
                return;
            }

            db = FirebaseFirestore.DefaultInstance;
            if (db != null)
            {
                isInitialized = true;
                Debug.Log("[TargetHandler] Firebase initialized successfully");
            }
            else
            {
                Debug.LogError("[TargetHandler] Firestore DefaultInstance is null");
                ErrorOccurred?.Invoke("Firebase not initialized");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TargetHandler] Failed to get Firestore instance: {e.Message}");
            ErrorOccurred?.Invoke($"Firebase initialization failed: {e.Message}");
        }
    }

    [ContextMenu("Fetch All Targets")]
    public void FetchAllTargets()
    {
        StartCoroutine(FetchAllTargetsCoroutine());
    }

    private IEnumerator FetchAllTargetsCoroutine()
    {
        // Check internet connectivity
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("[TargetHandler] No internet connection for data fetch");
            ErrorOccurred?.Invoke("No internet connection");
            yield break;
        }

        if (db == null || !isInitialized)
        {
            TryInit();
            
            // Shorter delay for PC, longer for mobile
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            if (db == null || !isInitialized)
            {
                Debug.LogError("[TargetHandler] Firestore not initialized. Ensure Firebase is set up and initialized.");
                ErrorOccurred?.Invoke("Firebase not initialized");
                yield break;
            }
        }

        try { LoadingChanged?.Invoke(true); } catch (Exception) {}

        bool fetchCompleted = false;
        Exception fetchException = null;

        db.Collection(collectionName).GetSnapshotAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"[TargetHandler] Failed to fetch targets. Faulted={task.IsFaulted}, Canceled={task.IsCanceled}, Exception={task.Exception}");
                fetchException = task.Exception;
            }
            else
            {
                ProcessFetchResult(task.Result);
            }
            fetchCompleted = true;
        });

        // Wait for completion with timeout
        float timeout = networkTimeoutSeconds;
        while (!fetchCompleted && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (!fetchCompleted)
        {
            Debug.LogError($"[TargetHandler] Fetch timed out after {networkTimeoutSeconds} seconds");
            HandleFetchError("Request timed out");
        }
        else if (fetchException != null)
        {
            HandleFetchError(fetchException.Message);
        }
        else
        {
            // Deliver results on the main thread immediately
            if (pendingTargets != null)
            {
                try
                {
                    GenerateTargetItems();
                    FillDropdownWithTargetItems();
                    TargetsChanged?.Invoke(pendingTargets);
                }
                catch (Exception cbEx)
                {
                    Debug.LogError($"[TargetHandler] Error while processing targets (post-fetch): {cbEx.Message}");
                }
                finally
                {
                    pendingTargets = null;
                    hasPendingTargets = false;
                }
            }
            try { LoadingChanged?.Invoke(false); } catch (Exception) {}
        }
    }

    private void ProcessFetchResult(QuerySnapshot snapshot)
    {
        List<TargetData> loaded = new List<TargetData>();

        if (snapshot == null)
        {
            Debug.LogError("[TargetHandler] Snapshot is null");
            HandleFetchError("Received null snapshot from Firebase");
            return;
        }

        Debug.Log($"[TargetHandler] Processing {snapshot.Documents.Count()} documents from Firebase");

        foreach (var doc in snapshot.Documents)
        {
            try
            {
                // Parse using dictionary since we're using public fields, not properties
                var dict = doc.ToDictionary();
                
                var data = new TargetData
                {
                    id = doc.Id,
                    Name = dict.ContainsKey("name") ? dict["name"]?.ToString() : 
                           dict.ContainsKey("Name") ? dict["Name"]?.ToString() : string.Empty,
                    Building = dict.ContainsKey("building") ? dict["building"]?.ToString() : 
                               dict.ContainsKey("Building") ? dict["Building"]?.ToString() : string.Empty,
                    BuildingId = dict.ContainsKey("buildingId") ? dict["buildingId"]?.ToString() : 
                                 dict.ContainsKey("BuildingId") ? dict["BuildingId"]?.ToString() : string.Empty,
                    FloorNumber = GetInt(dict, "floor") != 0 ? GetInt(dict, "floor") : GetInt(dict, "FloorNumber"),
                    FloorId = dict.ContainsKey("floorId") ? dict["floorId"]?.ToString() : 
                              dict.ContainsKey("FloorId") ? dict["FloorId"]?.ToString() : string.Empty,
                    Image = dict.ContainsKey("image") ? dict["image"]?.ToString() : 
                            dict.ContainsKey("Image") ? dict["Image"]?.ToString() : string.Empty,
                    CreatedAt = dict.ContainsKey("createdAt") ? dict["createdAt"]?.ToString() : 
                                dict.ContainsKey("CreatedAt") ? dict["CreatedAt"]?.ToString() : string.Empty,
                    Position = new TargetPosition
                    {
                        x = GetFloat(dict, "x"),
                        y = GetFloat(dict, "y"),
                        z = GetFloat(dict, "z")
                    }
                };
                
                Debug.Log($"[TargetHandler] Document {doc.Id} - Name: '{data.Name}', Building: '{data.Building}', Floor: {data.FloorNumber}, Image: '{(string.IsNullOrEmpty(data.Image) ? "none" : "present")}'");
                Debug.Log($"[TargetHandler] Position: ({data.Position.x}, {data.Position.y}, {data.Position.z})");
                
                loaded.Add(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TargetHandler] Failed to parse document '{doc.Id}': {ex.Message}");
            }
        }

        // Replace in-memory list atomically
        targets = loaded;
        retryCount = 0; // Reset retry count on success

        Debug.Log($"[TargetHandler] Successfully loaded {loaded.Count} targets from Firebase");

        // Store targets to be processed on main thread
        pendingTargets = new List<TargetData>(targets);
        hasPendingTargets = true;
    }

    private void HandleFetchError(string errorMessage)
    {
        Debug.LogError($"[TargetHandler] Fetch error: {errorMessage}");
        ErrorOccurred?.Invoke(errorMessage);
        try { LoadingChanged?.Invoke(false); } catch (Exception) {}
        
        // Retry logic
        if (retryCount < maxRetryAttempts)
        {
            retryCount++;
            StartCoroutine(RetryFetch());
        }
        else
        {
            Debug.LogError("[TargetHandler] Max retry attempts reached. Giving up.");
        }
    }

    private IEnumerator RetryFetch()
    {
        yield return new WaitForSeconds(3f);
        FetchAllTargets();
    }

    private float GetFloat(Dictionary<string, object> dict, string key)
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

    private int GetInt(Dictionary<string, object> dict, string key)
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

    public void EmitCachedTargets()
    {
        if (targets != null && targets.Count > 0)
        {
            try
            {
                TargetsChanged?.Invoke(new List<TargetData>(targets));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TargetHandler] Error emitting cached targets: {ex.Message}");
            }
        }
    }

    private void GenerateTargetItems() {
        Debug.Log("TargetHandler.GenerateTargetItems() - clearing existing items and creating new ones");
        
        foreach (var item in currentTargetItems) {
            if (item != null && item.gameObject != null) {
                Destroy(item.gameObject);
            }
        }
        currentTargetItems.Clear();

        if (targets != null && targets.Count > 0) {
            Debug.Log($"TargetHandler: Generating {targets.Count} target items from Firebase.");
            foreach (TargetData targetData in targets) {
                TargetFacade created = CreateTargetFacade(targetData);
                if (created != null) {
                    currentTargetItems.Add(created);
                    Debug.Log($"Created TargetFacade: '{created.Name}' (Floor {created.FloorNumber}) GameObject='{created.gameObject.name}'");
                } else {
                    Debug.LogWarning($"Skipped creating target facade for '{targetData?.Name}' (floor {targetData?.FloorNumber}).");
                }
            }
        } else {
            Debug.Log("TargetHandler: No targets to generate from Firebase.");
        }
    }

    private TargetFacade CreateTargetFacade(TargetData targetData)
    {
        Debug.Log($"CreateTargetFacade() called for target '{targetData?.Name}'");
        if (targetData == null)
        {
            Debug.LogWarning("CreateTargetFacade called with null targetData");
            return null;
        }

        if (targetObjectPrefab == null)
        {
            Debug.LogError("Target prefab (targetObjectPrefab) is not assigned. Cannot create target objects.");
            return null;
        }

        string buildingName = string.IsNullOrEmpty(targetData.Building) ? "*" : targetData.Building;
        string targetName = string.IsNullOrEmpty(targetData.Name) ? "*" : targetData.Name;
        string floorText = (targetData.FloorNumber < 0) ? "*" : targetData.FloorNumber.ToString();

        Transform parentTransform = null;
        if (targetObjectsParentTransforms != null && targetObjectsParentTransforms.Length > 0)
        {
            int floor = targetData.FloorNumber;
            if (floor < 0 || floor >= targetObjectsParentTransforms.Length)
            {
                Debug.LogWarning($"FloorNumber {floor} for target '{targetData.Name}' is out of range. Clamping to valid range.");
                floor = Mathf.Clamp(floor, 0, targetObjectsParentTransforms.Length - 1);
            }
            parentTransform = targetObjectsParentTransforms[floor];
            Debug.Log($"Selected parent transform for floor {floor}: {(parentTransform != null ? parentTransform.name : "null")}");
        } else {
            Debug.Log("No targetObjectsParentTransforms assigned, instantiating at root of scene.");
        }

        float px = (targetData.Position != null && !float.IsNaN(targetData.Position.x)) ? targetData.Position.x : 0f;
        float py = (targetData.Position != null && !float.IsNaN(targetData.Position.y)) ? targetData.Position.y : 0f;
        float pz = (targetData.Position != null && !float.IsNaN(targetData.Position.z)) ? targetData.Position.z : 0f;
        Vector3 pos = new Vector3(px, py, pz);
        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
            float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
        {
            Debug.LogWarning($"Invalid position in target '{targetData.Name}', resetting to (0,0,0)");
            pos = Vector3.zero;
        }
        Debug.Log($"Target position for '{targetData.Name}': {pos}");

        GameObject targetObject = parentTransform != null
            ? Instantiate(targetObjectPrefab, parentTransform, false)
            : Instantiate(targetObjectPrefab);

        targetObject.name = $"{buildingName} - Floor {floorText} - {targetName}";
        targetObject.SetActive(true);
        targetObject.transform.localPosition = pos;
        targetObject.transform.localRotation = Quaternion.identity;

        TargetFacade facade = targetObject.GetComponent<TargetFacade>();
        if (facade == null)
            facade = targetObject.AddComponent<TargetFacade>();

        facade.Name = targetName;
        facade.FloorNumber = targetData.FloorNumber;
        facade.Building = buildingName;

        Debug.Log($"TargetFacade populated: Name='{facade.Name}' Building='{facade.Building}' Floor={facade.FloorNumber}");

        if (!string.IsNullOrEmpty(targetData.Image))
        {
            Debug.Log($"Target '{targetData.Name}' contains image data (length {targetData.Image.Length}). Attempting decode.");
            try
            {
                string base64 = targetData.Image;
                int comma = base64.IndexOf(',');
                if (comma >= 0)
                    base64 = base64.Substring(comma + 1);

                byte[] imageBytes = System.Convert.FromBase64String(base64);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageBytes))
                {
                    Debug.Log($"Decoded image for target '{targetData.Name}' -> texture {tex.width}x{tex.height}");
                    Renderer rend = targetObject.GetComponent<Renderer>();
                    if (rend != null && rend.material != null)
                    {
                        rend.material.mainTexture = tex;
                        Debug.Log($"Applied texture to Renderer on '{targetObject.name}'");
                    }

                    var t = facade.GetType();
                    var field = t.GetField("ImageTexture");
                    if (field != null && field.FieldType == typeof(Texture2D))
                    {
                        field.SetValue(facade, tex);
                        Debug.Log("Set TargetFacade.ImageTexture field.");
                    }
                    else
                    {
                        var prop = t.GetProperty("ImageTexture");
                        if (prop != null && prop.PropertyType == typeof(Texture2D) && prop.CanWrite)
                        {
                            prop.SetValue(facade, tex);
                            Debug.Log("Set TargetFacade.ImageTexture property.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to LoadImage from base64 for target '{targetData.Name}'.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to convert Base64 image for target '{targetData.Name}': {e.Message}");
            }
        } else {
            Debug.Log($"No image data for target '{targetData.Name}'.");
        }

        return facade;
    }

    private void FillDropdownWithTargetItems()
    {
        Debug.Log("TargetHandler.FillDropdownWithTargetItems()");
        if (targetDataDropdown == null)
        {
            Debug.LogWarning("Dropdown not assigned!");
            return;
        }

        var toRemove = dropdownSpriteCache.Keys.Except(currentTargetItems).ToList();
        foreach (var k in toRemove)
        {
            if (dropdownSpriteCache.TryGetValue(k, out var sp) && sp != null)
            {
                Destroy(sp);
            }
            dropdownSpriteCache.Remove(k);
        }

        List<TMP_Dropdown.OptionData> targetFacadeOptionData =
            currentTargetItems
                .Where(x => x != null)
                .Select(x =>
                {
                    string optionText = $"{x.Building} - Floor {x.FloorNumber} - {x.Name}";
                    Debug.Log($"Preparing dropdown option: {optionText}");
                    var option = new TMP_Dropdown.OptionData
                    {
                        text = optionText
                    };

                    Sprite sprite = GetSpriteForFacade(x);
                    if (sprite != null) {
                        option.image = sprite;
                        Debug.Log($" -> Option has image for '{x.Name}'");
                    } else {
                        Debug.Log($" -> Option has NO image for '{x.Name}'");
                    }

                    return option;
                })
                .ToList();

        targetDataDropdown.ClearOptions();
        targetDataDropdown.AddOptions(targetFacadeOptionData);
        Debug.Log($"Dropdown populated with {targetFacadeOptionData.Count} options.");

        // Also populate DropdownScrollBinder with location data if available
        if (dropdownScrollBinder != null && targets != null && targets.Count > 0)
        {
            // Convert TargetData to LocationData format
            LocationData[] locationDataArray = targets.Select(targetData => new LocationData
            {
                Name = targetData.Name,
                Building = targetData.Building,
                BuildingId = targetData.BuildingId,
                CreatedAt = targetData.CreatedAt,
                FloorNumber = targetData.FloorNumber,
                FloorId = targetData.FloorId,
                Image = targetData.Image,
                Position = new LocationPosition
                {
                    x = targetData.Position?.x ?? 0,
                    y = targetData.Position?.y ?? 0,
                    z = targetData.Position?.z ?? 0
                }
            }).ToArray();

            dropdownScrollBinder.SetLocationData(locationDataArray);
            Debug.Log($"DropdownScrollBinder populated with {locationDataArray.Length} location data items.");
        }
        else if (dropdownScrollBinder == null)
        {
            Debug.LogWarning("DropdownScrollBinder not assigned in TargetHandler - images will not be displayed in scroll list.");
        }
    }

    private Sprite GetSpriteForFacade(TargetFacade facade)
    {
        if (facade == null) return null;

        if (dropdownSpriteCache.TryGetValue(facade, out var cached) && cached != null)
            return cached;

        Texture2D tex = null;

        var t = facade.GetType();
        var field = t.GetField("ImageTexture");
        if (field != null && field.FieldType == typeof(Texture2D))
            tex = field.GetValue(facade) as Texture2D;
        else
        {
            var prop = t.GetProperty("ImageTexture");
            if (prop != null && prop.PropertyType == typeof(Texture2D) && prop.CanRead)
                tex = prop.GetValue(facade) as Texture2D;
        }

        if (tex == null)
        {
            var rend = facade.GetComponent<Renderer>();
            if (rend != null && rend.material != null && rend.material.mainTexture is Texture2D)
                tex = rend.material.mainTexture as Texture2D;
        }

        if (tex == null) {
            Debug.Log($"GetSpriteForFacade: No texture found for facade '{facade.Name}'");
            return null;
        }

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        dropdownSpriteCache[facade] = sp;
        Debug.Log($"GetSpriteForFacade: Created sprite for facade '{facade.Name}' size {tex.width}x{tex.height}");
        return sp;
    }

    private void OnDestroy()
    {
        foreach (var kv in dropdownSpriteCache)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        dropdownSpriteCache.Clear();
    }

    public void SetSelectedTargetPositionWithDropdown(int selectedValue) {
        Debug.Log($"SetSelectedTargetPositionWithDropdown(selectedValue={selectedValue})");
        navigationController.TargetPosition = GetCurrentlySelectedTarget(selectedValue);
    }

    private Vector3 GetCurrentlySelectedTarget(int selectedValue) {
        Debug.Log($"GetCurrentlySelectedTarget(selectedValue={selectedValue})");
        if (selectedValue < 0 || selectedValue >= currentTargetItems.Count) {
            Debug.LogWarning("Selected value out of range, returning Vector3.zero");
            return Vector3.zero;
        }

        var item = currentTargetItems[selectedValue];
        if (item == null || item.gameObject == null) {
            Debug.LogWarning("Selected item null or destroyed, returning Vector3.zero");
            return Vector3.zero;
        }

        Debug.Log($"Returning position for '{item.Name}' -> {item.transform.position}");
        return item.transform.position;
    }

    public TargetFacade GetCurrentTargetByTargetText(string targetText) {
        Debug.Log($"GetCurrentTargetByTargetText('{targetText}')");
        return currentTargetItems.Find(x =>
            x.Name.ToLower().Equals(targetText.ToLower()));
    }

    // Public method to refresh target data (call this to reload from Firebase)
    public void RefreshTargetData() {
        Debug.Log("TargetHandler.RefreshTargetData() called — fetching from Firebase and rebuilding targets.");

        if (currentTargetItems != null && currentTargetItems.Count > 0) {
            foreach (var item in currentTargetItems) {
                if (item != null && item.gameObject != null) {
#if UNITY_EDITOR
                    DestroyImmediate(item.gameObject);
#else
                    Destroy(item.gameObject);
#endif
                }
            }
            currentTargetItems.Clear();
        }

        FetchAllTargets();
    }
}   