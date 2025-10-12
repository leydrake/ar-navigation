using UnityEngine;

public class LocationDataExample : MonoBehaviour
{
    [Header("References")]
    public DropdownScrollBinder dropdownScrollBinder;
    
    [Header("Test Data")]
    [TextArea(10, 20)]
    public string jsonData = @"{
        ""TargetList"": [
            {
                ""Name"": ""Canteen"",
                ""Building"": ""Canteen"",
                ""BuildingId"": ""6A739SNmQqou3TQtPSGg"",
                ""CreatedAt"": ""Timestamp: 2025-10-09T06:16:49.593Z"",
                ""FloorNumber"": 1,
                ""FloorId"": ""CAOW1j41KimfiU32ancR"",
                ""Image"": """",
                ""Position"": {
                    ""x"": 34.88999938964844,
                    ""y"": 0.0,
                    ""z"": -72.2699966430664
                }
            },
            {
                ""Name"": ""Library"",
                ""Building"": ""Pancho"",
                ""BuildingId"": ""316J7SmjQGwVcYPnV59P"",
                ""CreatedAt"": ""Timestamp: 2025-10-09T06:15:33.861Z"",
                ""FloorNumber"": 1,
                ""FloorId"": ""96BuvWYsL6g92wVrgWx8"",
                ""Image"": """",
                ""Position"": {
                    ""x"": 64.36599731445313,
                    ""y"": 0.0,
                    ""z"": 2.6500000953674318
                }
            },
            {
                ""Name"": ""monkeyboy"",
                ""Building"": ""Activity Center"",
                ""BuildingId"": ""4GK5t0sVlU1dkDUvxKYk"",
                ""CreatedAt"": ""Timestamp: 2025-10-11T14:03:35.811Z"",
                ""FloorNumber"": 1,
                ""FloorId"": ""iVnAT4wrKRd2WRXkk3Jv"",
                ""Image"": ""data:image/jpeg;base64,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"",
                ""Position"": {
                    ""x"": -0.5064449310302734,
                    ""y"": 0.7529616355895996,
                    ""z"": -1.5236217975616456
                }
            }
        ]
    }";

    void Start()
    {
        // Example: Load location data from file
        if (dropdownScrollBinder != null)
        {
            LoadLocationDataFromFile();
        }
    }

    [ContextMenu("Load Location Data")]
    public void LoadLocationDataFromJson()
    {
        if (dropdownScrollBinder != null)
        {
            dropdownScrollBinder.SetLocationDataFromJson(jsonData);
            Debug.Log("Location data loaded successfully!");
        }
        else
        {
            Debug.LogError("DropdownScrollBinder reference is not assigned!");
        }
    }

    [ContextMenu("Load Location Data From File")]
    public void LoadLocationDataFromFile()
    {
        if (dropdownScrollBinder != null)
        {
            string filePath = @"C:\Users\drans\AppData\LocalLow\leynard\CampusNavigator\TargetData.json";
            dropdownScrollBinder.LoadLocationDataFromFile(filePath);
            Debug.Log("Location data loaded from file successfully!");
        }
        else
        {
            Debug.LogError("DropdownScrollBinder reference is not assigned!");
        }
    }

    [ContextMenu("Test Base64 Image")]
    public void TestBase64Image()
    {
        // Test the base64 image utility
        string testBase64 = "data:image/jpeg;base64,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";
        
        Sprite testSprite = Base64ImageUtility.Base64ToSprite(testBase64);
        if (testSprite != null)
        {
            Debug.Log("Base64 image conversion successful!");
        }
        else
        {
            Debug.LogError("Base64 image conversion failed!");
        }
    }
}
