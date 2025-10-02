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
    
    [Header("JSON File Settings")]
    [SerializeField]
    private string jsonFileName = "TargetData.json";

    [SerializeField]
    private GameObject targetObjectPrefab;
    [SerializeField]
    private Transform[] targetObjectsParentTransforms;

    private List<TargetFacade> currentTargetItems = new List<TargetFacade>();
    private TargetListWrapper targetDataWrapper;

    private void Start() {
        LoadTargetData();
        GenerateTargetItems();
        FillDropdownWithTargetItems();
    }

    private void LoadTargetData() {
        // Prefer persistentDataPath if a file exists there (mobile-friendly and writable)
        string persistentPath = Path.Combine(Application.persistentDataPath, jsonFileName);
        if (File.Exists(persistentPath)) {
            try {
                string jsonContent = File.ReadAllText(persistentPath);
                targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContent);
                Debug.Log($"Loaded target data from persistent path: {persistentPath}");
                return;
            } catch (System.Exception e) {
                Debug.LogWarning($"Failed reading persistent JSON, will try StreamingAssets. Error: {e.Message}");
            }
        }

        // Fallback to StreamingAssets. On Android, StreamingAssets is inside the APK and not directly accessible via File IO.
        string streamingPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Use UnityWebRequest to read from jar file
        string jsonContentFromSA = ReadTextFromStreamingAssets(streamingPath);
        if (!string.IsNullOrEmpty(jsonContentFromSA)) {
            targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContentFromSA);
            Debug.Log($"Loaded target data from StreamingAssets (Android): {streamingPath}");
        } else {
            Debug.LogWarning($"Target data not found or empty at StreamingAssets (Android): {streamingPath}. Creating empty data.");
            targetDataWrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
        }
#else
        if (File.Exists(streamingPath)) {
            string jsonContent = File.ReadAllText(streamingPath);
            targetDataWrapper = JsonUtility.FromJson<TargetListWrapper>(jsonContent);
            Debug.Log($"Loaded target data from StreamingAssets: {streamingPath}");
        } else {
            Debug.LogWarning($"Target data file not found at: {streamingPath}. Creating empty data.");
            targetDataWrapper = new TargetListWrapper { TargetList = new List<TargetData>() };
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private string ReadTextFromStreamingAssets(string path) {
        // Blocking read; safe for small JSON file during Start()
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
        // Clear existing target items
        foreach (var item in currentTargetItems) {
            if (item != null && item.gameObject != null) {
                Destroy(item.gameObject);
            }
        }
        currentTargetItems.Clear();

        // Generate new target items from loaded data
        if (targetDataWrapper?.TargetList != null) {
            foreach (TargetData targetData in targetDataWrapper.TargetList) {
                currentTargetItems.Add(CreateTargetFacade(targetData));
            }
        }
    }

    private TargetFacade CreateTargetFacade(TargetData targetData) {
        GameObject targetObject = Instantiate(targetObjectPrefab, targetObjectsParentTransforms[targetData.FloorNumber], false);
        targetObject.SetActive(true);
        targetObject.name = $"{targetData.FloorNumber} - {targetData.Name}";
        targetObject.transform.localPosition = new Vector3(targetData.Position.x, targetData.Position.y, targetData.Position.z);
        targetObject.transform.localRotation = Quaternion.Euler(targetData.Rotation.x, targetData.Rotation.y, targetData.Rotation.z);

        TargetFacade targetFacade = targetObject.GetComponent<TargetFacade>();
        targetFacade.Name = targetData.Name;
        targetFacade.FloorNumber = targetData.FloorNumber;

        return targetFacade;
    }

    private void FillDropdownWithTargetItems() {
        List<TMP_Dropdown.OptionData> targetFacadeOptionData =
            currentTargetItems.Select(x => new TMP_Dropdown.OptionData {
                text = $"{x.FloorNumber} - {x.Name}"
            }).ToList();

        targetDataDropdown.ClearOptions();
        targetDataDropdown.AddOptions(targetFacadeOptionData);
    }

    public void SetSelectedTargetPositionWithDropdown(int selectedValue) {
        navigationController.TargetPosition = GetCurrentlySelectedTarget(selectedValue);
    }

    private Vector3 GetCurrentlySelectedTarget(int selectedValue) {
        if (selectedValue >= currentTargetItems.Count) {
            return Vector3.zero;
        }

        return currentTargetItems[selectedValue].transform.position;
    }

    public TargetFacade GetCurrentTargetByTargetText(string targetText) {
        return currentTargetItems.Find(x =>
            x.Name.ToLower().Equals(targetText.ToLower()));
    }

    // Public method to refresh target data (call this after Firebase updates the JSON)
    public void RefreshTargetData() {
        LoadTargetData();
        GenerateTargetItems();
        FillDropdownWithTargetItems();
        Debug.Log("Target data refreshed successfully!");
    }
}