using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class ActiveUserTracker : MonoBehaviour
{
    private FirebaseFirestore db;
    private string sessionId;
    private string visitorId;
    private bool isSessionActive = false;
    

    void Start()
    {
        InitializeFirebase();
        GenerateSessionId();
        GenerateVisitorId();
        
        // Automatically start tracking when app opens
        StartCoroutine(StartTrackingWhenReady());
    }
    
    IEnumerator StartTrackingWhenReady()
    {
        // Wait for Firebase to initialize
        while (db == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Start tracking with default destination
        StartActiveSession("App Opened");
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
            }
           
        });
    }

    void GenerateSessionId()
    {
        sessionId = System.Guid.NewGuid().ToString();
    }

    void GenerateVisitorId()
    {
        visitorId = SystemInfo.deviceUniqueIdentifier;
    }

    // Start tracking active session
    public void StartActiveSession(string destination = "Unknown")
    {
        if (db == null) 
        {
            StartCoroutine(RetryStartSession(destination));
            return;
        }

        isSessionActive = true;
        TrackActiveSessionStart(destination);
    }

    IEnumerator RetryStartSession(string destination)
    {
        yield return new WaitForSeconds(1f);
        StartActiveSession(destination);
    }

    void TrackActiveSessionStart(string destination)
    {
        var sessionData = new Dictionary<string, object>
        {
            { "sessionId", sessionId },
            { "visitorId", visitorId },
            { "startTime", FieldValue.ServerTimestamp },
            { "lastActivity", FieldValue.ServerTimestamp },
            { "destination", destination },
            { "platform", "Unity" },
            { "deviceModel", SystemInfo.deviceModel },
            { "unityVersion", Application.unityVersion }
        };

        db.Collection("analytics")
            .Document("ar_sessions")
            .Collection("active")
            .Document(sessionId)
            .SetAsync(sessionData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                }
                else
                {
                    // Start periodic activity updates
                    StartCoroutine(UpdateLastActivity());
                }
            });
    }

    // Update last activity every 30 seconds
    IEnumerator UpdateLastActivity()
    {
        while (isSessionActive)
        {
            yield return new WaitForSeconds(30f);
            
            if (db != null && isSessionActive)
            {
                var updateData = new Dictionary<string, object>
                {
                    { "lastActivity", FieldValue.ServerTimestamp }
                };

                db.Collection("analytics")
                    .Document("ar_sessions")
                    .Collection("active")
                    .Document(sessionId)
                    .UpdateAsync(updateData);
            }
        }
    }

    // End active session
    public void EndActiveSession()
    {
        isSessionActive = false;
        
        if (db != null)
        {
            var endData = new Dictionary<string, object>
            {
                { "endTime", FieldValue.ServerTimestamp },
                { "lastActivity", FieldValue.ServerTimestamp }
            };

            db.Collection("analytics")
                .Document("ar_sessions")
                .Collection("active")
                .Document(sessionId)
                .UpdateAsync(endData);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            EndActiveSession();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            EndActiveSession();
        }
    }

    void OnDestroy()
    {
        EndActiveSession();
    }
}
