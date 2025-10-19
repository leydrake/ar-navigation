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
        
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield break;
        }

        // Test with a simple web request
        using (var request = UnityEngine.Networking.UnityWebRequest.Get("https://www.google.com"))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
            }
            
        }
    }

    private IEnumerator CheckFirebaseInitialization()
    {
        
        int attempts = 0;
        int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            try
            {
                var app = FirebaseApp.DefaultInstance;
                if (app != null)
                {
                    
                    break;
                }
            }
            catch (System.Exception e)
            {
            }
            
            attempts++;
            yield return new WaitForSeconds(1f);
        }

       
    }

    private IEnumerator CheckFirestoreAvailability()
    {
        
        try
        {
            var db = FirebaseFirestore.DefaultInstance;
           
        }
        catch (System.Exception e)
        {
        }
        
        yield return null;
    }

    private IEnumerator TestDataFetch()
    {
        
        var db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
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
                }
                else
                {
                    var snapshot = task.Result;
                    fetchSuccessful = true;
                }
                fetchCompleted = true;
            });
        }
        catch (System.Exception e)
        {
            fetchCompleted = true;
        }

        // Wait for fetch with timeout
        float timeout = 15f;
        while (!fetchCompleted && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
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
       
    }
}
