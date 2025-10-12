using System;
using UnityEngine;

[Serializable]
public class LocationData
{
    public string Name;
    public string Building;
    public string BuildingId;
    public string CreatedAt;
    public int FloorNumber;
    public string FloorId;
    public string Image; // Base64 encoded image data
    public LocationPosition Position;
}

[Serializable]
public class LocationPosition
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class TargetListData
{
    public LocationData[] TargetList;
}
