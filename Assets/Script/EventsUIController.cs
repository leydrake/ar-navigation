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

	[SerializeField]
	private int titleFontSize = 16;

	[SerializeField]
	private int locationFontSize = 12;

	private TextField searchField;
	private ScrollView listView;
	private VisualElement refreshButton;

	private List<EventData> current = new List<EventData>();


	[SerializeField]
	private string refreshButtonName = "RefreshButton";

	private void Awake()
	{
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
		refreshButton = root.Q<VisualElement>(refreshButtonName);

		if (searchField == null)
		{
			Debug.LogWarning($"EventsUIController: TextField '{searchFieldName}' not found in UXML.");
		}

		if (listView == null)
		{
			Debug.LogWarning($"EventsUIController: ScrollView '{scrollViewName}' not found in UXML.");
		}

		if (refreshButton == null)
		{
			Debug.LogWarning($"EventsUIController: VisualElement '{refreshButtonName}' not found in UXML.");
		}
		else
		{
			// Register for click events on the VisualElement
			refreshButton.RegisterCallback<ClickEvent>(OnRefreshClicked);
		}

		if (searchField != null)
		{
			searchField.RegisterValueChangedCallback(_ => ApplyFilter());
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

		if (eventsFetcher != null)
		{
			eventsFetcher.EventsChanged += OnEventsChanged;
			eventsFetcher.LoadingChanged += OnLoadingChanged;
			if (eventsFetcher.events != null && eventsFetcher.events.Count > 0)
			{
				OnEventsChanged(new List<EventData>(eventsFetcher.events));
			}
		}
	}

	private void OnDestroy()
	{
		if (eventsFetcher != null)
		{
			eventsFetcher.EventsChanged -= OnEventsChanged;
			eventsFetcher.LoadingChanged -= OnLoadingChanged;
		}

		if (refreshButton != null)
		{
			refreshButton.UnregisterCallback<ClickEvent>(OnRefreshClicked);
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
			OnEventsChanged(new List<EventData>(eventsFetcher.events));
		}
	}

	private void OnEventsChanged(List<EventData> list)
	{
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
		}
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
		if (isLoading)
		{
			ShowSkeletons(4);
		}
		else
		{
			// Re-enable refresh button when loading is complete
			OnLoadingFinished();
		}
	}

	/// <summary>
	/// Called when the refresh button is clicked
	/// </summary>
	private void OnRefreshClicked(ClickEvent evt)
	{
		RefreshEvents();
	}

	/// <summary>
	/// Public method to refresh events data
	/// Can be called from other scripts or UI elements
	/// </summary>
	public void RefreshEvents()
	{
		if (eventsFetcher == null)
		{
			Debug.LogWarning("[EventsUI] Cannot refresh - EventsFetcher is null");
			return;
		}
		
		// Disable refresh button during loading to prevent multiple requests
		if (refreshButton != null)
		{
			refreshButton.SetEnabled(false);
		}
		
		// Force refresh by clearing current data first
		current.Clear();
		RebuildList(current);
		
		eventsFetcher.FetchAllEvents();
	}

	/// <summary>
	/// Re-enables the refresh button when loading is complete
	/// </summary>
	private void OnLoadingFinished()
	{
		if (refreshButton != null)
		{
			refreshButton.SetEnabled(true);
		}
	}

	private VisualElement BuildCard(EventData data)
	{
		var row = new VisualElement();
		row.style.flexDirection = FlexDirection.Row;
		row.style.height = 96;
		row.style.marginBottom = 12;
		row.style.marginLeft = 8;
		row.style.marginRight = 8;
		row.style.backgroundColor = new Color(1f, 1f, 1f, 1f);
		row.style.borderBottomLeftRadius = 10;
		row.style.borderBottomRightRadius = 10;
		row.style.borderTopLeftRadius = 10;
		row.style.borderTopRightRadius = 10;
		row.style.paddingLeft = 16;
		row.style.paddingRight = 16;
		row.style.paddingTop = 12;
		row.style.paddingBottom = 12;
		row.style.alignItems = Align.Center;

		var img = new Image();
		img.style.width = 72;
		img.style.height = 72;
		img.scaleMode = ScaleMode.ScaleToFit;
		img.image = Texture2D.grayTexture;
		row.Add(img);

		var col = new VisualElement();
		col.style.flexGrow = 1;
		col.style.marginLeft = 12;
		col.style.flexDirection = FlexDirection.Column;
		col.style.justifyContent = Justify.Center;

		var title = new Label(string.IsNullOrEmpty(data.title) ? "(untitled)" : data.title);
		title.style.unityFontStyleAndWeight = FontStyle.Bold;
		title.style.fontSize = titleFontSize;
		title.style.marginBottom = 4;

		var location = new Label(string.IsNullOrEmpty(data.location) ? string.Empty : $"Location: {data.location}");
		location.style.fontSize = locationFontSize;
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


