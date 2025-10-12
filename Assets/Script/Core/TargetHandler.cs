using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class TargetHandler : MonoBehaviour {

    [SerializeField]
    private NavigationController navigationController;
    [SerializeField]
    private TMP_Dropdown targetDataDropdown;
    [SerializeField]
    private DropdownScrollBinder dropdownScrollBinder;
    
    [Header("JSON File Settings")]
    [SerializeField]
    private string jsonFileName = "TargetData.json";

    [SerializeField]
    private GameObject targetObjectPrefab;
    [SerializeField]
    private Transform[] targetObjectsParentTransforms;

    private List<TargetFacade> currentTargetItems = new List<TargetFacade>();
    private TargetListWrapper targetDataWrapper;

    // cache sprites created for dropdown to avoid recreating every frame
    private Dictionary<TargetFacade, Sprite> dropdownSpriteCache = new Dictionary<TargetFacade, Sprite>();

    private void Start() {
        Debug.Log("TargetHandler.Start()");
        LoadTargetData();
        GenerateTargetItems();
        FillDropdownWithTargetItems();
    }

    private void LoadTargetData() {
        Debug.Log("TargetHandler.LoadTargetData() - attempting to load JSON");
        string persistentPath = Path.Combine(Application.persistentDataPath, jsonFileName);
        if (File.Exists(persistentPath)) {
            try {
                string jsonContent = File.ReadAllText(persistentPath);
                targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContent);
                Debug.Log($"Loaded target data from persistent path: {persistentPath}");
                LogWrapperSummary();
                return;
            } catch (System.Exception e) {
                Debug.LogWarning($"Failed reading persistent JSON, will try StreamingAssets. Error: {e.Message}");
            }
        }

        string streamingPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        string jsonContentFromSA = ReadTextFromStreamingAssets(streamingPath);
        if (!string.IsNullOrEmpty(jsonContentFromSA)) {
            targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContentFromSA);
            Debug.Log($"Loaded target data from StreamingAssets (Android): {streamingPath}");
            LogWrapperSummary();
        } else {
            Debug.LogWarning($"Target data not found or empty at StreamingAssets (Android): {streamingPath}. Creating empty data.");
            targetDataWrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
        }
#else
        if (File.Exists(streamingPath)) {
            string jsonContent = File.ReadAllText(streamingPath);
            targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContent);
            Debug.Log($"Loaded target data from StreamingAssets: {streamingPath}");
            LogWrapperSummary();
        } else {
            Debug.LogWarning($"Target data file not found at: {streamingPath}. Creating empty data.");
            targetDataWrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
        }
#endif
    }

    // Helper to log loaded wrapper contents
    private void LogWrapperSummary()
    {
        if (targetDataWrapper == null || targetDataWrapper.TargetList == null) {
            Debug.Log("TargetHandler: No targets loaded (wrapper is null or empty).");
            return;
        }

        Debug.Log($"TargetHandler: Loaded {targetDataWrapper.TargetList.Count} targets:");
        for (int i = 0; i < targetDataWrapper.TargetList.Count; i++) {
            var t = targetDataWrapper.TargetList[i];
            if (t == null) {
                Debug.Log($"  [{i}] null entry");
                continue;
            }
            string pos = t.Position != null ? $"{t.Position.x},{t.Position.y},{t.Position.z}" : "null";
            Debug.Log($"  [{i}] Name='{t.Name}' Building='{t.Building}' Floor={t.FloorNumber} FloorId='{t.FloorId}' ImagePresent={(string.IsNullOrEmpty(t.Image) ? "no" : "yes")} Position={pos} CreatedAt='{t.CreatedAt}'");
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private string ReadTextFromStreamingAssets(string path) {
        using (UnityWebRequest request = UnityWebRequest.Get(path)) {
            var op = request.SendWebRequest();
            while (!op.isDone) { }
            bool failed = request.result == UnityWebRequest.Result.ConnectionError ||
                          request.result == UnityWebRequest.Result.ProtocolError ||
                          request.result == UnityWebRequest.Result.DataProcessingError;
            if (failed) {
                Debug.LogWarning($"Failed to read StreamingAssets JSON: {request.error}");
                return null;
            }
            return request.downloadHandler.text;
        }
    }
#endif

    private void GenerateTargetItems() {
        Debug.Log("TargetHandler.GenerateTargetItems() - clearing existing items and creating new ones");
        foreach (var item in currentTargetItems) {
            if (item != null && item.gameObject != null) {
                Destroy(item.gameObject);
            }
        }
        currentTargetItems.Clear();

        if (targetDataWrapper?.TargetList != null) {
            Debug.Log($"TargetHandler: Generating {targetDataWrapper.TargetList.Count} target items from wrapper.");
            foreach (TargetData targetData in targetDataWrapper.TargetList) {
                TargetFacade created = CreateTargetFacade(targetData);
                if (created != null) {
                    currentTargetItems.Add(created);
                    Debug.Log($"Created TargetFacade: '{created.Name}' (Floor {created.FloorNumber}) GameObject='{created.gameObject.name}'");
                } else {
                    Debug.LogWarning($"Skipped creating target facade for '{targetData?.Name}' (floor {targetData?.FloorNumber}).");
                }
            }
        } else {
            Debug.Log("TargetHandler: No targetDataWrapper or TargetList to generate from.");
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
        if (dropdownScrollBinder != null && targetDataWrapper != null && targetDataWrapper.TargetList != null)
        {
            // Convert TargetData to LocationData format
            LocationData[] locationDataArray = targetDataWrapper.TargetList.Select(targetData => new LocationData
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

    // Public method to refresh target data (call this after Firebase updates the JSON)
    public void RefreshTargetData() {
        Debug.Log("TargetHandler.RefreshTargetData() called — reloading JSON and rebuilding targets.");

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

        LoadTargetData();
        GenerateTargetItems();
        FillDropdownWithTargetItems();

        Debug.Log("Target data refreshed successfully!");
    }
}