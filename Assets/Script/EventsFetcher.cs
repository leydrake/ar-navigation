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
	public string name { get; set; }

	[FirestoreProperty]
	public string location { get; set; }

	[FirestoreProperty]
	public string image { get; set; }

	[FirestoreProperty]
	public string time { get; set; }

	[FirestoreProperty]
	public string description { get; set; }

	[FirestoreProperty]
	public string startTime { get; set; }

	[FirestoreProperty]
	public string endTime { get; set; }

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
				ErrorOccurred?.Invoke("Firebase not initialized");
			}
		}
		catch (Exception e)
		{
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
			HandleFetchError("Request timed out");
		}
		else if (fetchException != null)
		{
			HandleFetchError(fetchException.Message);
		}
		else
		{
			// Deliver results on the main thread immediately (do not rely on Update)
			if (pendingEvents != null)
			{
				try
				{
					EventsChanged?.Invoke(pendingEvents);
				}
				catch (Exception cbEx)
				{
				}
				finally
				{
					pendingEvents = null;
					hasPendingEvents = false;
				}
			}
			try { LoadingChanged?.Invoke(false); } catch (Exception) {}
		}
	}

	private void ProcessFetchResult(QuerySnapshot snapshot)
	{
		List<EventData> loaded = new List<EventData>();

		if (snapshot == null)
		{
			HandleFetchError("Received null snapshot from Firebase");
			return;
		}

		foreach (var doc in snapshot.Documents)
		{
			try
			{
				EventData data = doc.ConvertTo<EventData>();
				data.id = doc.Id;
				
				// Debug: Log the actual data being received
				
				loaded.Add(data);
			}
			catch (Exception ex)
			{
				var dict = doc.ToDictionary();
				var fallback = new EventData
				{
					id = doc.Id,
					name = dict.ContainsKey("name") ? dict["name"]?.ToString() : string.Empty,
					location = dict.ContainsKey("location") ? dict["location"]?.ToString() : string.Empty,
					image = dict.ContainsKey("image") ? dict["image"]?.ToString() : string.Empty,
					time = dict.ContainsKey("time") ? dict["time"]?.ToString() : string.Empty,
					description = dict.ContainsKey("description") ? dict["description"]?.ToString() : string.Empty,
					startTime = dict.ContainsKey("startTime") ? dict["startTime"]?.ToString() : string.Empty,
					endTime = dict.ContainsKey("endTime") ? dict["endTime"]?.ToString() : string.Empty
				};
				
				// Debug: Log the fallback data
				
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
		ErrorOccurred?.Invoke(errorMessage);
		try { LoadingChanged?.Invoke(false); } catch (Exception) {}
		
		// Retry logic
		if (retryCount < maxRetryAttempts)
		{
			retryCount++;
			StartCoroutine(RetryFetch());
		}
		
	}

	/// <summary>
	/// Emits the currently cached events to listeners immediately.
	/// Useful when reopening a UI that subscribes after a previous fetch.
	/// </summary>
	public void EmitCachedEvents()
	{
		if (events != null)
		{
			try
			{
				EventsChanged?.Invoke(new List<EventData>(events));
			}
			catch (Exception ex)
			{
			}
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
