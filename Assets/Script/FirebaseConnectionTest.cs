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
       
        
        // Check if Firebase App is initialized
        if (FirebaseApp.DefaultInstance == null)
        {
            
            return;
        }
        else
        {
          
        }

        // Check if Firestore is available
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            if (db == null)
            {
               
                return;
            }
            

           
        }
        catch (System.Exception e)
        {
           
        }
    }
}
