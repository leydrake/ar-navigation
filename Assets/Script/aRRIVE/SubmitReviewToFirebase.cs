using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class SubmitReviewToFirestore : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField commentBox;       // TMP Input Field for comment
    public TMP_InputField studentNoBox;     // TMP Input Field for student number
    public Button[] stars;                  // Star buttons array (5 buttons)
    public Sprite filledStar;               // Filled star sprite
    public Sprite emptyStar;                // Empty star sprite

    [Header("Notification Panel")]
    public GameObject notificationPanel;       // Notification panel GameObject
    public TextMeshProUGUI notificationText;   // TMP Text inside panel
    public float notificationDuration = 2f;    // Duration to show notifications

    [Header("Firestore Settings")]
    public string collectionName = "reviews";

    private int rating = 0;
    private FirebaseFirestore db;

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
            }
            else
            {
                ShowNotification("❌ Firebase not available.");
            }
        });

        // Setup star button listeners
        for (int i = 0; i < stars.Length; i++)
        {
            int index = i;
            stars[i].onClick.AddListener(() => SetRating(index + 1));
        }

        if(notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    void SetRating(int value)
    {
        rating = value;
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].image.sprite = (i < rating) ? filledStar : emptyStar;
        }
    }

    public void SubmitReview()
    {
        // Clear previous notification
        if(notificationPanel != null)
            notificationPanel.SetActive(false);

        // Validate student number
        if(!ValidateStudentNo())
        {
            ShowNotification("❌ Student number must be exactly 10 digits.");
            return;
        }

        // Check if both comment and rating are empty
        if (rating == 0 && string.IsNullOrEmpty(commentBox.text))
        {
            ShowNotification("❌ Please provide a rating or a comment.");
            return;
        }

        // Firestore timestamp
        Timestamp timestamp = Timestamp.GetCurrentTimestamp();

        Dictionary<string, object> reviewData = new Dictionary<string, object>
        {
            { "comment", commentBox.text },
            { "rating", rating },
            { "studentNo", studentNoBox != null ? studentNoBox.text : "" },
            { "createdAt", timestamp }
        };

        // Submit to Firestore
        db.Collection(collectionName).AddAsync(reviewData).ContinueWithOnMainThread(task =>
        {
            if(task.IsCompleted)
            {
                ShowNotification("✅ Review submitted successfully!");
                ClearFields();

                // Go back to SampleScene after a short delay
                StartCoroutine(GoBackToSampleSceneAfterDelay(0.5f));
            }
            else
            {
                ShowNotification("❌ Failed to submit review. Try again.");
            }
        });
    }

    bool ValidateStudentNo()
    {
        // Allow empty/blank student number (optional)
        if (studentNoBox == null || string.IsNullOrWhiteSpace(studentNoBox.text))
            return true;

        // If provided, it must be exactly 10 digits
        return Regex.IsMatch(studentNoBox.text, @"^\d{10}$");
    }

    void ClearFields()
    {
        if(commentBox != null) commentBox.text = "";
        if(studentNoBox != null) studentNoBox.text = "";
        SetRating(0);
    }

    void ShowNotification(string message)
    {
        if(notificationPanel == null || notificationText == null) return;

        notificationText.text = message;
        notificationPanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideNotificationAfterDelay(notificationDuration));
    }

    IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(notificationPanel != null)
            notificationPanel.SetActive(false);
    }

    IEnumerator GoBackToSampleSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("SampleScene"); // Replace with your scene name exactly
    }
}
