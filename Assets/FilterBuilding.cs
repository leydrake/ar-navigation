using UnityEngine;

/// <summary>
/// Component to store location data on each location item GameObject
/// Attach this to each location item prefab or dynamically when creating location items
/// </summary>
public class FilterBuilding : MonoBehaviour
{
    [Header("Location Information")]
    public string locationName;
    public string building;
    public string buildingId;
    public int floorNumber;
    public string floorId;
    public string imageUrl;
    public string createdAt;
    
    [Header("3D Position")]
    public Vector3 position;
    
    /// <summary>
    /// Initialize location data from your data source (Firestore/JSON)
    /// Call this when creating/spawning location items
    /// </summary>
    public void SetLocationData(string name, string buildingName, string buildingID, 
                                int floor, string floorID, string image, 
                                string created, Vector3 pos)
    {
        locationName = name;
        building = buildingName;
        buildingId = buildingID;
        floorNumber = floor;
        floorId = floorID;
        imageUrl = image;
        createdAt = created;
        position = pos;
    }
    
    /// <summary>
    /// Quick method to set just the essential data
    /// </summary>
    public void SetBasicData(string name, string buildingName)
    {
        locationName = name;
        building = buildingName;
    }
    
    /// <summary>
    /// Get display name for this location (format: "Building - Location")
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(building) && !string.IsNullOrEmpty(locationName))
        {
            return $"{building} - {locationName}";
        }
        return locationName;
    }
}