using UnityEngine;
using Firebase.Firestore;
using Firebase;

public class FirebaseConnectionTest : MonoBehaviour
{
    void Start()
    {
        TestFirebaseConnection();
    }

    [ContextMenu("Test Firebase Connection")]
    public void TestFirebaseConnection()
    {
        Debug.Log("=== Testing Firebase Connection ===");
        
        // Check if Firebase App is initialized
        if (FirebaseApp.DefaultInstance == null)
        {
            Debug.LogError("❌ FirebaseApp.DefaultInstance is NULL!");
            Debug.LogError("Make sure FirebaseInit script is in your scene and running!");
            return;
        }
        else
        {
            Debug.Log("✅ FirebaseApp.DefaultInstance is available");
            Debug.Log($"Firebase Project ID: {FirebaseApp.DefaultInstance.Options.ProjectId}");
            Debug.Log($"Firebase App Name: {FirebaseApp.DefaultInstance.Name}");
            Debug.Log($"Firebase App Options: {FirebaseApp.DefaultInstance.Options}");
        }

        // Check if Firestore is available
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            if (db == null)
            {
                Debug.LogError("❌ FirebaseFirestore.DefaultInstance is NULL!");
                return;
            }
            else
            {
                Debug.Log("✅ FirebaseFirestore.DefaultInstance is available");
            }

            // // Try to save a simple test document
            // Debug.Log("Attempting to save test document...");
            
            // var testData = new
            // {
            //     message = "Firebase connection test",
            //     timestamp = System.DateTime.Now.ToString(),
            //     testValue = 123
            // };

            // Debug.Log($"About to save to coordinates/Anchor1 with data: {testData}");
            
            // try
            // {
            //     var task = db.Collection("coordinates").Document("Anchor1").SetAsync(testData);
            //     Debug.Log("SetAsync task created successfully");
                
            //     task.ContinueWith(t =>
            //     {
            //         Debug.Log($"Task callback triggered. IsCompleted: {t.IsCompleted}, IsFaulted: {t.IsFaulted}, IsCanceled: {t.IsCanceled}");
                    
            //         if (t.IsCompletedSuccessfully)
            //         {
            //             Debug.Log("✅ SUCCESS! Test document saved to Firestore!");
            //             Debug.Log("Check your Firebase Console - look for 'coordinates' collection with 'Anchor1' document");
                        
            //             // Try to read it back to verify
            //             Debug.Log("Attempting to read back the document to verify...");
            //             db.Collection("coordinates").Document("Anchor1").GetSnapshotAsync().ContinueWith(readTask =>
            //             {
            //                 if (readTask.IsCompletedSuccessfully)
            //                 {
            //                     var doc = readTask.Result;
            //                     if (doc.Exists)
            //                     {
            //                         Debug.Log("✅ VERIFIED! Document exists in Firestore");
            //                         Debug.Log($"Document data: {doc.ToDictionary()}");
            //                     }
            //                     else
            //                     {
            //                         Debug.LogError("❌ Document does not exist in Firestore!");
            //                     }
            //                 }
            //                 else
            //                 {
            //                     Debug.LogError($"❌ Failed to read back document: {readTask.Exception}");
            //                 }
            //             });
            //         }
            //         else
            //         {
            //             Debug.LogError("❌ FAILED to save test document!");
            //             Debug.LogError($"Task Status: Completed={t.IsCompleted}, Faulted={t.IsFaulted}, Canceled={t.IsCanceled}");
                        
            //             if (t.Exception != null)
            //             {
            //                 Debug.LogError($"Exception: {t.Exception}");
            //                 Debug.LogError($"Exception Message: {t.Exception.Message}");
            //                 if (t.Exception.InnerException != null)
            //                 {
            //                     Debug.LogError($"Inner Exception: {t.Exception.InnerException}");
            //                     Debug.LogError($"Inner Exception Message: {t.Exception.InnerException.Message}");
            //                 }
            //             }
            //             else
            //             {
            //                 Debug.LogError("No exception details available");
            //             }
            //         }
            //     });
                
            //     Debug.Log("ContinueWith callback registered");
            // }
            // catch (System.Exception ex)
            // {
            //     Debug.LogError($"Exception during SetAsync: {ex.Message}");
            // }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception while testing Firestore: {e.Message}");
        }
    }
}
