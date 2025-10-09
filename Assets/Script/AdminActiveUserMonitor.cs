using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class AdminActiveUserMonitor : MonoBehaviour
{
    private FirebaseFirestore db;
    private bool isInitialized = false;
    

    [Header("Settings")]
    public float updateInterval = 10f; // Update every 10 seconds
    public float sessionTimeoutMinutes = 5f; // Consider session inactive after 5 minutes
    
    // Current active users data
    private Dictionary<string, Dictionary<string, object>> activeUsers = new Dictionary<string, Dictionary<string, object>>();
    private int currentActiveUserCount = 0;

    // Events
    public static event Action<int> OnActiveUserCountChanged;

    void Start()
    {
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                
                Debug.Log("Admin Firebase initialized successfully");
                
                // Start monitoring active users
                StartCoroutine(MonitorActiveUsers());
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
            }
        });
    }

    // Monitor active users in real-time
    IEnumerator MonitorActiveUsers()
    {
        while (isInitialized)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (db != null)
            {
                yield return StartCoroutine(GetActiveUsers());
            }
        }
    }

    // Get all active users from Firebase
    IEnumerator GetActiveUsers()
    {
        bool taskCompleted = false;
        bool taskSuccess = false;
        QuerySnapshot snapshot = null;

        db.Collection("analytics")
            .Document("ar_sessions")
            .Collection("active")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"Failed to get active users: {task.Exception?.Flatten().Message}");
                    taskCompleted = true;
                    taskSuccess = false;
                }
                else
                {
                    snapshot = task.Result;
                    taskCompleted = true;
                    taskSuccess = true;
                }
            });

        yield return new WaitUntil(() => taskCompleted);

        if (!taskSuccess)
            yield break;

        var newActiveUsers = new Dictionary<string, Dictionary<string, object>>();
        int newActiveUserCount = 0;

        foreach (var document in snapshot.Documents)
        {
            var userData = document.ToDictionary();
            var sessionId = document.Id;
            
            // Check if session is still active (within timeout period)
            if (IsSessionActive(userData))
            {
                newActiveUsers[sessionId] = userData;
                newActiveUserCount++;
            }
        }

        // Update active users data
        activeUsers = newActiveUsers;
        
        // Notify if count changed
        if (currentActiveUserCount != newActiveUserCount)
        {
            currentActiveUserCount = newActiveUserCount;
            OnActiveUserCountChanged?.Invoke(currentActiveUserCount);
            Debug.Log($"Active users count: {currentActiveUserCount}");
        }
    }

    // Check if a session is still active based on last activity
    bool IsSessionActive(Dictionary<string, object> userData)
    {
        if (!userData.ContainsKey("lastActivity"))
            return false;

        var lastActivity = userData["lastActivity"];
        if (lastActivity is Timestamp timestamp)
        {
            var lastActivityTime = timestamp.ToDateTime();
            var timeSinceActivity = DateTime.UtcNow - lastActivityTime;
            return timeSinceActivity.TotalMinutes <= sessionTimeoutMinutes;
        }

        return false;
    }

    // Public methods
    public int GetActiveUserCount()
    {
        return currentActiveUserCount;
    }

    public Dictionary<string, Dictionary<string, object>> GetActiveUsersData()
    {
        return new Dictionary<string, Dictionary<string, object>>(activeUsers);
    }

    public void RefreshData()
    {
        if (isInitialized)
        {
            StartCoroutine(GetActiveUsers());
        }
    }

    void OnDestroy()
    {
        isInitialized = false;
    }
}
