using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using System;
using System.Linq;

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

	public List<EventData> events = new List<EventData>();

	private FirebaseFirestore db;

	// Notifies listeners whenever the in-memory list is replaced after a fetch
	public event Action<List<EventData>> EventsChanged;

	// Notifies listeners when a fetch starts/ends
	public event Action<bool> LoadingChanged;

	void Start()
	{
		Debug.Log($"[EventsFetcher] Start() fetchOnStart={fetchOnStart}");
		TryInit();
		if (fetchOnStart)
		{
			Debug.Log("[EventsFetcher] Auto-fetch on Start");
			FetchAllEvents();
		}
	}

	private void TryInit()
	{
		try
		{
			db = FirebaseFirestore.DefaultInstance;
			if (db != null)
			{
				Debug.Log("[EventsFetcher] Firestore DefaultInstance acquired");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[EventsFetcher] Failed to get Firestore instance: {e.Message}");
		}
	}

	[ContextMenu("Fetch All Events")]
	public void FetchAllEvents()
	{
		if (db == null)
		{
			TryInit();
			if (db == null)
			{
				Debug.LogError("[EventsFetcher] Firestore not initialized. Ensure Firebase is set up and initialized.");
				return;
			}
		}

		Debug.Log("[EventsFetcher] Fetching all documents from 'events' collection...");
		try { LoadingChanged?.Invoke(true); } catch (Exception) {}
		db.Collection("events").GetSnapshotAsync().ContinueWith(task =>
		{
			if (task.IsFaulted || task.IsCanceled)
			{
				Debug.LogError($"[EventsFetcher] Failed to fetch events. Faulted={task.IsFaulted}, Canceled={task.IsCanceled}, Exception={task.Exception}");
				try { LoadingChanged?.Invoke(false); } catch (Exception) {}
				return;
			}

			QuerySnapshot snapshot = task.Result;
			Debug.Log($"[EventsFetcher] Snapshot received. Document count={snapshot?.Count}");
			List<EventData> loaded = new List<EventData>();

			foreach (var doc in snapshot.Documents)
			{
				try
				{
					Debug.Log($"[EventsFetcher] Parsing doc id={doc.Id}");
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

			// Log for visibility
			Debug.Log($"[EventsFetcher] Loaded {events.Count} event(s) from Firestore.");
			for (int i = 0; i < events.Count; i++)
			{
				var e = events[i];
				Debug.Log($"[EventsFetcher] [{i}] id={e.id}, title={e.title}, location={e.location}, image={e.image}");
			}

			// Notify any UI listeners on the next frame (Unity main thread)
			Debug.Log("[EventsFetcher] Dispatching EventsChanged on next frame");
			StartCoroutine(InvokeNextFrame(() =>
			{
				try
				{
					EventsChanged?.Invoke(new List<EventData>(events));
					try { LoadingChanged?.Invoke(false); } catch (Exception) {}
					Debug.Log("[EventsFetcher] EventsChanged invoked successfully");
				}
				catch (Exception cbEx)
				{
					Debug.LogError($"[EventsFetcher] Error while invoking EventsChanged: {cbEx.Message}");
				}
			}));
		});
	}

	private IEnumerator InvokeNextFrame(Action action)
	{
		yield return null;
		action?.Invoke();
	}
}
