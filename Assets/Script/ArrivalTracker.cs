using UnityEngine;
using Firebase.Firestore;
using System;
using System.Threading.Tasks;
using TMPro;

public class ArrivalTracker : MonoBehaviour
{
    public TMP_Text destinationTMP;
    public TMP_Text timeTMP;

    private FirebaseFirestore db;

    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (string.IsNullOrEmpty(DestinationManager.SelectedLocation))
        {
            Debug.LogWarning("⚠️ No destination found — skipping analytics logging.");
            if (destinationTMP) destinationTMP.text = "Unknown Destination";
            if (timeTMP) timeTMP.text = "00/00/s";
            return;
        }

        await SaveAnalyticsAsync();
    }

    private async Task SaveAnalyticsAsync()
    {
        string location = DestinationManager.SelectedLocation;
        float duration = Time.time - DestinationManager.NavigationStartTime;

        // ✅ Format: m/s/s
        int minutes = Mathf.FloorToInt(duration / 60f);
        int seconds = Mathf.FloorToInt(duration % 60f);
        string formattedTime = $"{minutes:00}:{seconds:00} /s";

        // Update TMPs
        if (destinationTMP) destinationTMP.text = location;
        if (timeTMP) timeTMP.text = formattedTime;

        string sessionId = SystemInfo.deviceUniqueIdentifier + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        // Firestore references
        CollectionReference completionsRef = db.Collection("analytics").Document("data").Collection("completions");
        DocumentReference summaryRef = db.Collection("analytics").Document("data").Collection("summary").Document(location);

        var completionData = new
        {
            name = location,
            completedAt = Timestamp.GetCurrentTimestamp(),
            sessionId = sessionId,
            deviceId = SystemInfo.deviceUniqueIdentifier,
            travelTimeSeconds = Mathf.RoundToInt(duration),
            travelTimeFormatted = formattedTime
        };

        try
        {
            // 1️⃣ Add raw completion entry
            await completionsRef.AddAsync(completionData);
            Debug.Log($"📊 Added completion entry for {location}");

            // 2️⃣ Update or create summary doc
            await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(summaryRef);

                int newCount = 1;
                if (snapshot.Exists && snapshot.ContainsField("completedCount"))
                    newCount = snapshot.GetValue<int>("completedCount") + 1;

                transaction.Set(summaryRef, new
                {
                    name = location,
                    completedCount = newCount,
                    lastCompleted = Timestamp.GetCurrentTimestamp(),
                    lastTravelTime = Mathf.RoundToInt(duration)
                }, SetOptions.MergeAll);
            });

            Debug.Log($"✅ Summary updated for {location}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save analytics: {e.Message}");
        }
    }
}
