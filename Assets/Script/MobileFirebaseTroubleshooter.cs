using UnityEngine;
using System.Collections;
using Firebase;
using Firebase.Firestore;

public class MobileFirebaseTroubleshooter : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDetailedLogging = true;
    [SerializeField] private bool testFirebaseConnection = true;
    
    [Header("Mobile-Specific Settings")]
    [SerializeField] private float initializationDelay = 3f;
    [SerializeField] private bool checkPermissions = true;

    void Start()
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[MobileFirebaseTroubleshooter] Starting mobile Firebase diagnostics...");
            Debug.Log($"[MobileFirebaseTroubleshooter] Platform: {Application.platform}");
            Debug.Log($"[MobileFirebaseTroubleshooter] Unity Version: {Application.unityVersion}");
            Debug.Log($"[MobileFirebaseTroubleshooter] Internet Reachability: {Application.internetReachability}");
        }

        StartCoroutine(RunDiagnostics());
    }

    private IEnumerator RunDiagnostics()
    {
        yield return new WaitForSeconds(initializationDelay);

        // Check 1: Internet connectivity
        yield return StartCoroutine(CheckInternetConnectivity());

        // Check 2: Firebase initialization
        yield return StartCoroutine(CheckFirebaseInitialization());

        // Check 3: Firestore availability
        yield return StartCoroutine(CheckFirestoreAvailability());

        // Check 4: Test data fetch
        if (testFirebaseConnection)
        {
            yield return StartCoroutine(TestDataFetch());
        }
    }

    private IEnumerator CheckInternetConnectivity()
    {
        Debug.Log("[MobileFirebaseTroubleshooter] Checking internet connectivity...");
        
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogError("[MobileFirebaseTroubleshooter] ❌ No internet connection detected!");
            yield break;
        }

        // Test with a simple web request
        using (var request = UnityEngine.Networking.UnityWebRequest.Get("https://www.google.com"))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("[MobileFirebaseTroubleshooter] ✅ Internet connectivity confirmed");
            }
            else
            {
                Debug.LogError($"[MobileFirebaseTroubleshooter] ❌ Internet test failed: {request.error}");
            }
        }
    }

    private IEnumerator CheckFirebaseInitialization()
    {
        Debug.Log("[MobileFirebaseTroubleshooter] Checking Firebase initialization...");
        
        int attempts = 0;
        int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            try
            {
                var app = FirebaseApp.DefaultInstance;
                if (app != null)
                {
                    Debug.Log($"[MobileFirebaseTroubleshooter] ✅ Firebase app initialized: {app.Name}");
                    Debug.Log($"[MobileFirebaseTroubleshooter] Firebase options: {app.Options.ProjectId}");
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MobileFirebaseTroubleshooter] Firebase init attempt {attempts + 1} failed: {e.Message}");
            }
            
            attempts++;
            yield return new WaitForSeconds(1f);
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogError("[MobileFirebaseTroubleshooter] ❌ Firebase initialization failed after multiple attempts");
            Debug.LogError("[MobileFirebaseTroubleshooter] Check your google-services.json (Android) or GoogleService-Info.plist (iOS)");
        }
    }

    private IEnumerator CheckFirestoreAvailability()
    {
        Debug.Log("[MobileFirebaseTroubleshooter] Checking Firestore availability...");
        
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
            if (db != null)
            {
                Debug.Log("[MobileFirebaseTroubleshooter] ✅ Firestore instance available");
            }
            else
            {
                Debug.LogError("[MobileFirebaseTroubleshooter] ❌ Firestore instance is null");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MobileFirebaseTroubleshooter] ❌ Firestore error: {e.Message}");
        }
        
        yield return null;
    }

    private IEnumerator TestDataFetch()
    {
        Debug.Log("[MobileFirebaseTroubleshooter] Testing data fetch...");
        
        var db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
            Debug.LogError("[MobileFirebaseTroubleshooter] ❌ Cannot test fetch - Firestore not available");
            yield break;
        }

        bool fetchCompleted = false;
        bool fetchSuccessful = false;
        string errorMessage = "";

        try
        {
            db.Collection("events").GetSnapshotAsync().ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    errorMessage = task.Exception?.ToString() ?? "Unknown error";
                    Debug.LogError($"[MobileFirebaseTroubleshooter] ❌ Test fetch failed: {errorMessage}");
                }
                else
                {
                    var snapshot = task.Result;
                    Debug.Log($"[MobileFirebaseTroubleshooter] ✅ Test fetch successful - {snapshot?.Count ?? 0} documents");
                    fetchSuccessful = true;
                }
                fetchCompleted = true;
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MobileFirebaseTroubleshooter] ❌ Test fetch exception: {e.Message}");
            fetchCompleted = true;
        }

        // Wait for fetch with timeout
        float timeout = 15f;
        while (!fetchCompleted && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (!fetchCompleted)
        {
            Debug.LogError("[MobileFirebaseTroubleshooter] ❌ Test fetch timed out");
        }
        else if (!fetchSuccessful)
        {
            Debug.LogError($"[MobileFirebaseTroubleshooter] ❌ Test fetch failed: {errorMessage}");
        }
    }

    [ContextMenu("Run Full Diagnostics")]
    public void RunFullDiagnostics()
    {
        StartCoroutine(RunDiagnostics());
    }

    [ContextMenu("Check Mobile Permissions")]
    public void CheckMobilePermissions()
    {
        Debug.Log("[MobileFirebaseTroubleshooter] Mobile permissions check:");
        Debug.Log($"[MobileFirebaseTroubleshooter] Internet permission required: INTERNET");
        Debug.Log($"[MobileFirebaseTroubleshooter] Network state permission required: ACCESS_NETWORK_STATE");
        Debug.Log("[MobileFirebaseTroubleshooter] Make sure these are in your AndroidManifest.xml");
    }
}
