using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class EventsUIController : MonoBehaviour
{
	[SerializeField]
	private EventsFetcher eventsFetcher;

	[SerializeField]
	private UIDocument uiDocument;

	[SerializeField]
	private string searchFieldName = "SearchInput";

	[SerializeField]
	private string scrollViewName = "EventsScrollView";

	private TextField searchField;
	private ScrollView listView;

	private List<EventData> current = new List<EventData>();

	[SerializeField]
	private bool showDebugBanner = true;

	private void Awake()
	{
		Debug.Log("[EventsUI] Awake start");
		if (uiDocument == null)
		{
			uiDocument = GetComponent<UIDocument>();
		}

		var root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogError("EventsUIController: UIDocument or rootVisualElement is missing.");
			return;
		}

		searchField = root.Q<TextField>(searchFieldName);
		listView = root.Q<ScrollView>(scrollViewName);
		Debug.Log($"[EventsUI] Queried elements: searchField={(searchField!=null)}, listView={(listView!=null)}");

		if (searchField == null)
		{
			Debug.LogWarning($"EventsUIController: TextField '{searchFieldName}' not found in UXML.");
		}

		if (listView == null)
		{
			Debug.LogWarning($"EventsUIController: ScrollView '{scrollViewName}' not found in UXML.");
		}

		if (searchField != null)
		{
			searchField.RegisterValueChangedCallback(_ => ApplyFilter());
		}

		// Visual debug banner to confirm rendering
		if (showDebugBanner && listView != null)
		{
			var banner = new Label("UI connected. Waiting for events...");
			banner.style.backgroundColor = new Color(1f, 1f, 0.2f, 1f);
			banner.style.color = Color.black;
			banner.style.marginBottom = 6;
			banner.style.paddingLeft = 6;
			banner.style.paddingTop = 2;
			banner.style.paddingBottom = 2;
			listView.Add(banner);
			Debug.Log("[EventsUI] Debug banner added to ScrollView.");
		}


		if (eventsFetcher == null)
		{
#if UNITY_2022_2_OR_NEWER
			eventsFetcher = FindFirstObjectByType<EventsFetcher>(FindObjectsInactive.Include);
#else
			eventsFetcher = FindObjectOfType<EventsFetcher>(true);
#endif
		}

		if (eventsFetcher == null)
		{
			Debug.LogWarning("EventsUIController: No EventsFetcher found in scene (active or inactive). UI will be empty.");
		}
		else
		{
			Debug.Log("EventsUIController: Found EventsFetcher. Subscribing to EventsChanged.");
		}

		if (eventsFetcher != null)
		{
			eventsFetcher.EventsChanged += OnEventsChanged;
			eventsFetcher.LoadingChanged += OnLoadingChanged;
			Debug.Log($"[EventsUI] Subscribed to EventsFetcher (instance={eventsFetcher.GetInstanceID()}). Current list count={(eventsFetcher.events==null?0:eventsFetcher.events.Count)}");
			if (eventsFetcher.events != null && eventsFetcher.events.Count > 0)
			{
				OnEventsChanged(new List<EventData>(eventsFetcher.events));
			}
			else
			{
				Debug.Log("[EventsUI] Events list empty on Awake; waiting for EventsChanged...");
			}
		}
		else
		{
			Debug.LogWarning("EventsUIController: No EventsFetcher found in scene.");
		}
	}

	private void OnDestroy()
	{
		if (eventsFetcher != null)
		{
			eventsFetcher.EventsChanged -= OnEventsChanged;
			eventsFetcher.LoadingChanged -= OnLoadingChanged;
		}
	}

	private void Start()
	{
		// Fallback: if event timing is missed, populate shortly after start
		StartCoroutine(PopulateFallback());
	}

	private IEnumerator PopulateFallback()
	{
		// wait a moment for fetch to complete
		yield return new WaitForSeconds(0.6f);
		if (eventsFetcher != null && (current == null || current.Count == 0) && eventsFetcher.events != null && eventsFetcher.events.Count > 0)
		{
			Debug.Log($"[EventsUI] Fallback populate. Count={eventsFetcher.events.Count}");
			OnEventsChanged(new List<EventData>(eventsFetcher.events));
		}
	}

	private void OnEventsChanged(List<EventData> list)
	{
		Debug.Log($"[EventsUI] OnEventsChanged received. Count={(list==null?0:list.Count)}");
		current = list ?? new List<EventData>();
		RebuildList(current);
	}

	private void ApplyFilter()
	{
		string query = searchField != null ? (searchField.value ?? string.Empty) : string.Empty;
		query = query.Trim();
		if (string.IsNullOrEmpty(query))
		{
			RebuildList(current);
			return;
		}

		string qLower = query.ToLowerInvariant();
		var filtered = current.Where(e =>
			(!string.IsNullOrEmpty(e.title) && e.title.ToLowerInvariant().Contains(qLower)) ||
			(!string.IsNullOrEmpty(e.location) && e.location.ToLowerInvariant().Contains(qLower))
		).ToList();

		RebuildList(filtered);
	}

	private void RebuildList(List<EventData> list)
	{
		if (listView == null)
		{
			Debug.LogWarning("[EventsUI] RebuildList called but listView is null.");
			return;
		}

		listView.Clear();
		Debug.Log($"[EventsUI] Building {list.Count} rows...");

		foreach (var item in list)
		{
			var row = BuildCard(item);
			listView.Add(row);
		}

		if (list.Count == 0)
		{
			var empty = new Label("No events found.");
			empty.style.unityTextAlign = TextAnchor.MiddleCenter;
			empty.style.color = new Color(0.4f, 0.4f, 0.4f);
			empty.style.marginTop = 16;
			listView.Add(empty);
			Debug.Log("[EventsUI] Displayed empty state message.");
		}

		Debug.Log($"[EventsUI] ScrollView child count after rebuild: {listView.childCount}");
	}

	private void ShowSkeletons(int count)
	{
		if (listView == null) return;
		listView.Clear();
		for (int i = 0; i < count; i++)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.style.height = 96;
			row.style.marginBottom = 8;
			row.style.backgroundColor = new Color(1f, 1f, 1f, 1f);
			row.style.borderBottomLeftRadius = 10;
			row.style.borderBottomRightRadius = 10;
			row.style.borderTopLeftRadius = 10;
			row.style.borderTopRightRadius = 10;
			row.style.paddingLeft = 8;
			row.style.paddingRight = 8;
			row.style.alignItems = Align.Center;

			var img = new VisualElement();
			img.style.width = 72;
			img.style.height = 72;
			img.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
			img.style.borderBottomLeftRadius = 8;
			img.style.borderBottomRightRadius = 8;
			img.style.borderTopLeftRadius = 8;
			img.style.borderTopRightRadius = 8;
			row.Add(img);

			var col = new VisualElement();
			col.style.flexGrow = 1;
			col.style.marginLeft = 8;
			col.style.flexDirection = FlexDirection.Column;

			var line1 = new VisualElement();
			line1.style.height = 16;
			line1.style.width = Length.Percent(60);
			line1.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
			line1.style.marginBottom = 6;

			var line2 = new VisualElement();
			line2.style.height = 12;
			line2.style.width = Length.Percent(40);
			line2.style.backgroundColor = new Color(0.92f, 0.92f, 0.92f);

			col.Add(line1);
			col.Add(line2);
			row.Add(col);

			listView.Add(row);
		}
	}

	private void OnLoadingChanged(bool isLoading)
	{
		Debug.Log($"[EventsUI] OnLoadingChanged: {isLoading}");
		if (isLoading)
		{
			ShowSkeletons(4);
		}
		else
		{
			// Do nothing here; list will be rebuilt by OnEventsChanged
		}
	}

	private VisualElement BuildCard(EventData data)
	{
		var row = new VisualElement();
		row.style.flexDirection = FlexDirection.Row;
		row.style.height = 96;
		row.style.marginBottom = 8;
		row.style.backgroundColor = new Color(1f, 1f, 1f, 1f);
		row.style.borderBottomLeftRadius = 10;
		row.style.borderBottomRightRadius = 10;
		row.style.borderTopLeftRadius = 10;
		row.style.borderTopRightRadius = 10;
		row.style.paddingLeft = 8;
		row.style.paddingRight = 8;
		row.style.alignItems = Align.Center;

		var img = new Image();
		img.style.width = 72;
		img.style.height = 72;
		img.scaleMode = ScaleMode.ScaleToFit;
		img.image = Texture2D.grayTexture;
		row.Add(img);

		var col = new VisualElement();
		col.style.flexGrow = 1;
		col.style.marginLeft = 8;
		col.style.flexDirection = FlexDirection.Column;

		var title = new Label(string.IsNullOrEmpty(data.title) ? "(untitled)" : data.title);
		title.style.unityFontStyleAndWeight = FontStyle.Bold;
		title.style.fontSize = 16;

		var location = new Label(string.IsNullOrEmpty(data.location) ? string.Empty : $"Location: {data.location}");
		location.style.fontSize = 12;
		location.style.color = new Color(0.25f, 0.25f, 0.25f);

		col.Add(title);
		col.Add(location);
		row.Add(col);

		if (!string.IsNullOrEmpty(data.image))
		{
			Debug.Log($"[EventsUI] Loading image for '{data.title}' from '{data.image}'");
			StartCoroutine(LoadImageInto(img, data.image));
		}

		return row;
	}

	private IEnumerator LoadImageInto(Image target, string url)
	{
		if (target == null || string.IsNullOrEmpty(url))
		{
			yield break;
		}

		using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
		{
			yield return req.SendWebRequest();

			bool failed = req.result == UnityWebRequest.Result.ConnectionError ||
						req.result == UnityWebRequest.Result.ProtocolError ||
						req.result == UnityWebRequest.Result.DataProcessingError;

			if (failed)
			{
				Debug.LogWarning($"[EventsUI] Failed to load image from '{url}': {req.error}");
				yield break;
			}

			Texture2D tex = DownloadHandlerTexture.GetContent(req);
			if (tex != null)
			{
				target.image = tex;
				Debug.Log($"[EventsUI] Image set successfully from '{url}'");
			}
		}
	}
}


