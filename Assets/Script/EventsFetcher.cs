using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System;
using System.Linq;
using UnityEngine.Networking;

[FirestoreData]
public class EventData
{
	[FirestoreProperty]
	public string title { get; set; }

	[FirestoreProperty]
	public string location { get; set; }

	[FirestoreProperty]
	public string image { get; set; }

	// Convenience field for the Firestore document id
	public string id { get; set; }
}

public class EventsFetcher : MonoBehaviour
{
	[SerializeField]
	private bool fetchOnStart = true;

	[SerializeField]
	private float networkTimeoutSeconds = 30f;

	[SerializeField]
	private int maxRetryAttempts = 3;

	public List<EventData> events = new List<EventData>();

	private FirebaseFirestore db;
	private int retryCount = 0;
	private bool isInitialized = false;
	private List<EventData> pendingEvents = null;
	private bool hasPendingEvents = false;

	// Notifies listeners whenever the in-memory list is replaced after a fetch
	public event Action<List<EventData>> EventsChanged;

	// Notifies listeners when a fetch starts/ends
	public event Action<bool> LoadingChanged;

	// Notifies listeners of errors
	public event Action<string> ErrorOccurred;

	void Start()
	{
		StartCoroutine(InitializeWithDelay());
	}

	void Update()
	{
		// Process pending events on main thread
		if (hasPendingEvents && pendingEvents != null)
		{
			hasPendingEvents = false;
			
			try
			{
				EventsChanged?.Invoke(pendingEvents);
				try { LoadingChanged?.Invoke(false); } catch (Exception) {}
			}
			catch (Exception cbEx)
			{
				Debug.LogError($"[EventsFetcher] Error while invoking EventsChanged: {cbEx.Message}");
			}
			
			pendingEvents = null;
		}
	}

	private IEnumerator InitializeWithDelay()
	{
		// Wait a bit for Firebase to initialize on mobile
		if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
		{
			yield return new WaitForSeconds(1f);
		}
		
		TryInit();
		
		// Additional delay for mobile devices
		if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
		{
			yield return new WaitForSeconds(2f);
		}
		
		if (fetchOnStart)
		{
			FetchAllEvents();
		}
	}

	private void TryInit()
	{
		try
		{
			// Check internet connectivity first
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				Debug.LogError("[EventsFetcher] No internet connection detected");
				ErrorOccurred?.Invoke("No internet connection");
				return;
			}

			db = FirebaseFirestore.DefaultInstance;
			if (db != null)
			{
				isInitialized = true;
			}
			else
			{
				Debug.LogError("[EventsFetcher] Firestore DefaultInstance is null");
				ErrorOccurred?.Invoke("Firebase not initialized");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[EventsFetcher] Failed to get Firestore instance: {e.Message}");
			ErrorOccurred?.Invoke($"Firebase initialization failed: {e.Message}");
		}
	}

	[ContextMenu("Fetch All Events")]
	public void FetchAllEvents()
	{
		// Use coroutine approach for all platforms to ensure proper event handling
		StartCoroutine(FetchAllEventsCoroutine());
	}

	private IEnumerator FetchAllEventsCoroutine()
	{
		// Check internet connectivity
		if (Application.internetReachability == NetworkReachability.NotReachable)
		{
			Debug.LogError("[EventsFetcher] No internet connection for data fetch");
			ErrorOccurred?.Invoke("No internet connection");
			yield break;
		}

		if (db == null || !isInitialized)
		{
			TryInit();
			
			// Shorter delay for PC, longer for mobile
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				yield return new WaitForSeconds(1f);
			}
			else
			{
				yield return new WaitForSeconds(0.1f);
			}
			
			if (db == null || !isInitialized)
			{
				Debug.LogError("[EventsFetcher] Firestore not initialized. Ensure Firebase is set up and initialized.");
				ErrorOccurred?.Invoke("Firebase not initialized");
				yield break;
			}
		}

		try { LoadingChanged?.Invoke(true); } catch (Exception) {}

		bool fetchCompleted = false;
		Exception fetchException = null;

		db.Collection("events").GetSnapshotAsync().ContinueWith(task =>
		{
			if (task.IsFaulted || task.IsCanceled)
			{
				Debug.LogError($"[EventsFetcher] Failed to fetch events. Faulted={task.IsFaulted}, Canceled={task.IsCanceled}, Exception={task.Exception}");
				fetchException = task.Exception;
			}
			else
			{
				ProcessFetchResult(task.Result);
			}
			fetchCompleted = true;
		});

		// Wait for completion with timeout
		float timeout = networkTimeoutSeconds;
		while (!fetchCompleted && timeout > 0)
		{
			yield return new WaitForSeconds(0.1f);
			timeout -= 0.1f;
		}

		if (!fetchCompleted)
		{
			Debug.LogError($"[EventsFetcher] Fetch timed out after {networkTimeoutSeconds} seconds");
			HandleFetchError("Request timed out");
		}
		else if (fetchException != null)
		{
			HandleFetchError(fetchException.Message);
		}
	}

	private void ProcessFetchResult(QuerySnapshot snapshot)
	{
		List<EventData> loaded = new List<EventData>();

		if (snapshot == null)
		{
			Debug.LogError("[EventsFetcher] Snapshot is null");
			HandleFetchError("Received null snapshot from Firebase");
			return;
		}

		foreach (var doc in snapshot.Documents)
		{
			try
			{
				EventData data = doc.ConvertTo<EventData>();
				data.id = doc.Id;
				loaded.Add(data);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[EventsFetcher] Could not parse document '{doc.Id}': {ex.Message}. Falling back to dictionary parse.");
				var dict = doc.ToDictionary();
				var fallback = new EventData
				{
					id = doc.Id,
					title = dict.ContainsKey("title") ? dict["title"]?.ToString() : string.Empty,
					location = dict.ContainsKey("location") ? dict["location"]?.ToString() : string.Empty,
					image = dict.ContainsKey("image") ? dict["image"]?.ToString() : string.Empty
				};
				loaded.Add(fallback);
			}
		}

		// Replace in-memory list atomically
		events = loaded;
		retryCount = 0; // Reset retry count on success

		// Store events to be processed on main thread
		pendingEvents = new List<EventData>(events);
		hasPendingEvents = true;
	}

	private void HandleFetchError(string errorMessage)
	{
		Debug.LogError($"[EventsFetcher] Fetch error: {errorMessage}");
		ErrorOccurred?.Invoke(errorMessage);
		
		// Retry logic
		if (retryCount < maxRetryAttempts)
		{
			retryCount++;
			StartCoroutine(RetryFetch());
		}
		else
		{
			Debug.LogError("[EventsFetcher] Max retry attempts reached. Giving up.");
			try { LoadingChanged?.Invoke(false); } catch (Exception) {}
		}
	}

	private IEnumerator RetryFetch()
	{
		yield return new WaitForSeconds(3f);
		FetchAllEvents();
	}


	private IEnumerator InvokeNextFrame(Action action)
	{
		yield return null;
		action?.Invoke();
	}

}
