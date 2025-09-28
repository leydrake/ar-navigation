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

	private const string PlaceholderText = "Search events...";
	private bool isPlaceholderActive = false;

	[SerializeField]
	private string scrollViewName = "EventsScrollView";

	[SerializeField]
	private int titleFontSize = 16;

	[SerializeField]
	private int locationFontSize = 12;

	private TextField searchField;
	private ScrollView listView;
	private VisualElement refreshButton;
	private VisualElement searchFieldShadow;

	private List<EventData> current = new List<EventData>();

	// Modal elements
	private VisualElement modalOverlay;
	private VisualElement modalContent;
	private VisualElement modalImage;
	private Label modalTitle;
	private Label modalDescription;
	private Label modalLocation;
	private Label modalStartTime;
	private Label modalEndTime;
	private VisualElement closeButton;


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
		else
		{
			// Fix the scroll view to prevent layout shifts
			listView.style.flexGrow = 1;
			listView.style.flexShrink = 1;
			listView.style.minHeight = 0;
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
			// Set initial placeholder text
			SetPlaceholder();
			
			
			// Create shadow element
			CreateSearchFieldShadow();
			
			// Register focus event callbacks for placeholder functionality
			searchField.RegisterCallback<FocusInEvent>(evt => OnSearchFocusIn());
			searchField.RegisterCallback<FocusOutEvent>(evt => OnSearchFocusOut());
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

		// Create modal
		CreateModal();
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
		
		// Don't filter if it's just the placeholder text
		if (string.IsNullOrEmpty(query) || query == PlaceholderText)
		{
			RebuildList(current);
			return;
		}

		string qLower = query.ToLowerInvariant();
		var filtered = current.Where(e =>
			(!string.IsNullOrEmpty(e.name) && e.name.ToLowerInvariant().Contains(qLower)) ||
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
			row.style.height = 180;
			row.style.marginBottom = 16;
			row.style.marginLeft = 16;
			row.style.marginRight = 16;
			row.style.backgroundColor = new Color(224f/255f, 224f/255f, 224f/255f, 1f); // rgb(224, 224, 224)
			row.style.borderBottomLeftRadius = 8;
			row.style.borderBottomRightRadius = 8;
			row.style.borderTopLeftRadius = 8;
			row.style.borderTopRightRadius = 8;
			row.style.paddingLeft = 16;
			row.style.paddingRight = 16;
			row.style.paddingTop = 12;
			row.style.paddingBottom = 12;
			row.style.alignItems = Align.Center;

			var img = new VisualElement();
			img.style.width = 100;
			img.style.height = 100;
			img.style.backgroundColor = new Color(240f/255f, 240f/255f, 240f/255f, 1f); // rgb(240, 240, 240)
			img.style.borderBottomLeftRadius = 6;
			img.style.borderBottomRightRadius = 6;
			img.style.borderTopLeftRadius = 6;
			img.style.borderTopRightRadius = 6;
			row.Add(img);

			var col = new VisualElement();
			col.style.flexGrow = 1;
			col.style.marginLeft = 12;
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
			// Ensure header and search bar stay stable during loading
			StabilizeHeaderLayout();
			ShowSkeletons(4);
		}
		else
		{
			// Re-enable refresh button when loading is complete
			OnLoadingFinished();
		}
	}

	private void StabilizeHeaderLayout()
	{
		// Ensure scroll view doesn't affect header
		if (listView != null)
		{
			listView.style.flexGrow = 1;
			listView.style.flexShrink = 1;
			listView.style.minHeight = 0;
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
		row.style.height = 180;
		row.style.marginBottom = 25;
		row.style.marginLeft = 16;
		row.style.marginRight = 16;
		row.style.backgroundColor = new Color(224f/255f, 224f/255f, 224f/255f, 1f); // rgb(224, 224, 224)
		row.style.borderBottomLeftRadius = 10;
		row.style.borderBottomRightRadius = 10;
		row.style.borderTopLeftRadius = 10;
		row.style.borderTopRightRadius = 10;
		row.style.paddingLeft = 16;
		row.style.paddingRight = 16;
		row.style.paddingTop = 14;
		row.style.paddingBottom = 14;
		row.style.alignItems = Align.Center;

		// Add subtle border for depth
		row.style.borderTopWidth = 1;
		row.style.borderBottomWidth = 1;
		row.style.borderLeftWidth = 1;
		row.style.borderRightWidth = 1;
		row.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		row.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		row.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		row.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);

		// Make the card clickable
		row.RegisterCallback<ClickEvent>(evt => ShowEventModal(data));

		// Image container with question mark icon
		var imgContainer = new VisualElement();
		imgContainer.style.width = 100;
		imgContainer.style.height = 100;
		imgContainer.style.backgroundColor = new Color(240f/255f, 240f/255f, 240f/255f, 1f); // rgb(240, 240, 240)
		imgContainer.style.borderBottomLeftRadius = 6;
		imgContainer.style.borderBottomRightRadius = 6;
		imgContainer.style.borderTopLeftRadius = 6;
		imgContainer.style.borderTopRightRadius = 6;
		imgContainer.style.alignItems = Align.Center;
		imgContainer.style.justifyContent = Justify.Center;

		// Question mark icon
		var questionMark = new Label("?");
		questionMark.style.fontSize = 40;
		questionMark.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
		questionMark.style.unityFontStyleAndWeight = FontStyle.Bold;
		imgContainer.Add(questionMark);

		// Actual image (hidden by default, shown when loaded)
		var img = new Image();
		img.style.width = 100;
		img.style.height = 100;
		img.scaleMode = ScaleMode.ScaleToFit;
		img.style.display = DisplayStyle.None; // Hidden initially
		imgContainer.Add(img);

		row.Add(imgContainer);

		// Text content
		var col = new VisualElement();
		col.style.flexGrow = 1;
		col.style.marginLeft = 12;
		col.style.flexDirection = FlexDirection.Column;
		col.style.justifyContent = Justify.Center;

		// Title
		var title = new Label(string.IsNullOrEmpty(data.name) ? "(untitled)" : data.name);
		title.style.unityFontStyleAndWeight = FontStyle.Bold;
		title.style.fontSize = 24;
		title.style.color = new Color(0f, 0f, 0f, 1f);
		title.style.marginBottom = 4;

		// Location
		var location = new Label(string.IsNullOrEmpty(data.location) ? string.Empty : $"Location: {data.location}");
		location.style.fontSize = 20;
		location.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
		location.style.marginBottom = 4;

		// Time
		var time = new Label(string.IsNullOrEmpty(data.time) ? "No time set" : data.time);
		time.style.fontSize = 20;
		time.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);

		col.Add(title);
		col.Add(location);
		col.Add(time);
		row.Add(col);

		// Load image if available
		if (!string.IsNullOrEmpty(data.image))
		{
			Debug.Log($"[EventsUI] Loading image for '{data.name}' from '{data.image}'");
			StartCoroutine(LoadImageInto(img, imgContainer, questionMark, data.image));
		}

		return row;
	}

	private IEnumerator LoadImageInto(Image target, VisualElement container, VisualElement questionMark, string url)
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
				target.style.display = DisplayStyle.Flex; // Show the image
				questionMark.style.display = DisplayStyle.None; // Hide the question mark
				Debug.Log($"[EventsUI] Image set successfully from '{url}'");
			}
		}
	}

	private void OnSearchFocusIn()
	{
		if (isPlaceholderActive)
		{
			searchField.SetValueWithoutNotify(string.Empty);
			searchField.RemoveFromClassList("placeholder");
			isPlaceholderActive = false;
		}
	}

	private void OnSearchFocusOut()
	{
		if (string.IsNullOrEmpty(searchField.value))
		{
			SetPlaceholder();
		}
	}

	private void SetPlaceholder()
	{
		searchField.SetValueWithoutNotify(PlaceholderText);
		searchField.AddToClassList("placeholder");
		isPlaceholderActive = true;
	}

	private void CreateSearchFieldShadow()
	{
		if (searchField == null) return;

		// Create shadow element
		searchFieldShadow = new VisualElement();
		searchFieldShadow.style.position = Position.Absolute;
		searchFieldShadow.style.backgroundColor = new Color(0, 0, 0, 0.1f); // Light gray shadow
		searchFieldShadow.style.borderTopLeftRadius = 8;
		searchFieldShadow.style.borderTopRightRadius = 8;
		searchFieldShadow.style.borderBottomLeftRadius = 8;
		searchFieldShadow.style.borderBottomRightRadius = 8;
		
		// Position shadow slightly offset to create shadow effect
		searchFieldShadow.style.left = 2;
		searchFieldShadow.style.top = 2;
		
		// Insert shadow behind the search field
		var parent = searchField.parent;
		if (parent != null)
		{
			var searchFieldIndex = parent.IndexOf(searchField);
			parent.Insert(searchFieldIndex, searchFieldShadow);
		}
	}

	private void CreateModal()
{
    var root = uiDocument != null ? uiDocument.rootVisualElement : null;
    if (root == null) return;

    // Create modal overlay
    modalOverlay = new VisualElement();
    modalOverlay.style.position = Position.Absolute;
    modalOverlay.style.left = 0;
    modalOverlay.style.top = 0;
    modalOverlay.style.right = 0;
    modalOverlay.style.bottom = 0;
    modalOverlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
    modalOverlay.style.display = DisplayStyle.None;
    modalOverlay.RegisterCallback<ClickEvent>(evt => HideEventModal());

    // Modal content (double width/height)
    modalContent = new VisualElement();
    modalContent.style.position = Position.Absolute;
    modalContent.style.left = Length.Percent(50);
    modalContent.style.top = Length.Percent(50);
    modalContent.style.width = 800;   // was 400
    modalContent.style.height = 1000; // was 500
    modalContent.style.marginLeft = -400; // half of new width
    modalContent.style.marginTop = -500;  // half of new height
    modalContent.style.backgroundColor = Color.white;
    modalContent.style.borderTopLeftRadius = 24;
    modalContent.style.borderTopRightRadius = 24;
    modalContent.style.borderBottomLeftRadius = 24;
    modalContent.style.borderBottomRightRadius = 24;
    modalContent.style.paddingLeft = 40;
    modalContent.style.paddingRight = 40;
    modalContent.style.paddingTop = 40;
    modalContent.style.paddingBottom = 40;

    // Close button (double size)
    closeButton = new VisualElement();
    closeButton.style.position = Position.Absolute;
    closeButton.style.top = 20;   // was 10
    closeButton.style.right = 20; // was 10
    closeButton.style.width = 60; // was 30
    closeButton.style.height = 60; // was 30
    closeButton.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
    closeButton.style.borderTopLeftRadius = 30;
    closeButton.style.borderTopRightRadius = 30;
    closeButton.style.borderBottomLeftRadius = 30;
    closeButton.style.borderBottomRightRadius = 30;
    closeButton.style.alignItems = Align.Center;
    closeButton.style.justifyContent = Justify.Center;
    closeButton.RegisterCallback<ClickEvent>(evt => HideEventModal());

    var closeLabel = new Label("×");
    closeLabel.style.fontSize = 40; // was 20
    closeLabel.style.color = Color.black;
    closeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
    closeButton.Add(closeLabel);

    // Title (bigger font)
    modalTitle = new Label("Event Details");
    modalTitle.style.fontSize = 48; // was 24
    modalTitle.style.color = new Color(0.2f, 0.4f, 0.2f);
    modalTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
    modalTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
    modalTitle.style.marginBottom = 40; // was 20

    // Image placeholder (double height)
    modalImage = new VisualElement();
    modalImage.style.width = Length.Percent(100);
    modalImage.style.height = 300; // was 150
    modalImage.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f);
    modalImage.style.borderTopLeftRadius = 16;
    modalImage.style.borderTopRightRadius = 16;
    modalImage.style.borderBottomLeftRadius = 16;
    modalImage.style.borderBottomRightRadius = 16;
    modalImage.style.marginBottom = 40; // was 20
    modalImage.style.alignItems = Align.Center;
    modalImage.style.justifyContent = Justify.Center;

    var imagePlaceholder = new Label("No image available");
    imagePlaceholder.style.color = new Color(0.6f, 0.6f, 0.6f);
    imagePlaceholder.style.fontSize = 28; // was 14
    modalImage.Add(imagePlaceholder);

    // Event info labels (double font size + spacing)
    modalDescription = new Label();
    modalDescription.style.fontSize = 28; // was 14
    modalDescription.style.color = Color.black;
    modalDescription.style.marginBottom = 20; // was 10
    modalDescription.style.whiteSpace = WhiteSpace.Normal;

    modalLocation = new Label();
    modalLocation.style.fontSize = 28;
    modalLocation.style.color = Color.black;
    modalLocation.style.marginBottom = 20;
    modalLocation.style.whiteSpace = WhiteSpace.Normal;

    modalStartTime = new Label();
    modalStartTime.style.fontSize = 28;
    modalStartTime.style.color = Color.black;
    modalStartTime.style.marginBottom = 20;
    modalStartTime.style.whiteSpace = WhiteSpace.Normal;

    modalEndTime = new Label();
    modalEndTime.style.fontSize = 28;
    modalEndTime.style.color = Color.black;
    modalEndTime.style.marginBottom = 20;
    modalEndTime.style.whiteSpace = WhiteSpace.Normal;

    // Add elements
    modalContent.Add(closeButton);
    modalContent.Add(modalTitle);
    modalContent.Add(modalImage);
    modalContent.Add(modalDescription);
    modalContent.Add(modalLocation);
    modalContent.Add(modalStartTime);
    modalContent.Add(modalEndTime);

    modalOverlay.Add(modalContent);
    root.Add(modalOverlay);
}

	private string FormatDateTime(string dateTimeString)
	{
		if (string.IsNullOrEmpty(dateTimeString))
		{
			return "No time set";
		}

		try
		{
			// Try to parse as ISO 8601 format (e.g., "2025-10-02T05:53:00.000Z")
			if (DateTime.TryParse(dateTimeString, out DateTime dateTime))
			{
				// Format as "8:00PM 12/17/2003" style
				return dateTime.ToString("h:mmtt -  M/d/yyyy");
			}
			
			// If parsing fails, return the original string
			return dateTimeString;
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"[EventsUI] Failed to parse datetime '{dateTimeString}': {ex.Message}");
			return dateTimeString;
		}
	}

	private void ShowEventModal(EventData eventData)
	{
		if (modalOverlay == null) return;

		// Populate modal with event data
		modalTitle.text = string.IsNullOrEmpty(eventData.name) ? "Event Details" : eventData.name;
		modalDescription.text = $"Description: {(!string.IsNullOrEmpty(eventData.description) ? eventData.description : "No description provided")}";
		modalLocation.text = $"Location: {(!string.IsNullOrEmpty(eventData.location) ? eventData.location : "No location set")}";
		modalStartTime.text = $"Start Time: {FormatDateTime(eventData.startTime)}";
		modalEndTime.text = $"End Time: {FormatDateTime(eventData.endTime)}";

		// Load event image if available
		if (!string.IsNullOrEmpty(eventData.image))
		{
			StartCoroutine(LoadModalImage(eventData.image));
		}
		else
		{
			// Clear any existing image
			modalImage.Clear();
			var imagePlaceholder = new Label("No image available");
			imagePlaceholder.style.color = new Color(0.6f, 0.6f, 0.6f);
			imagePlaceholder.style.fontSize = 14;
			modalImage.Add(imagePlaceholder);
		}

		// Show modal
		modalOverlay.style.display = DisplayStyle.Flex;
	}

	private void HideEventModal()
	{
		if (modalOverlay != null)
		{
			modalOverlay.style.display = DisplayStyle.None;
		}
	}

	private IEnumerator LoadModalImage(string url)
	{
		if (string.IsNullOrEmpty(url)) yield break;

		using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
		{
			yield return req.SendWebRequest();

			bool failed = req.result == UnityWebRequest.Result.ConnectionError ||
						req.result == UnityWebRequest.Result.ProtocolError ||
						req.result == UnityWebRequest.Result.DataProcessingError;

			if (failed)
			{
				Debug.LogWarning($"[EventsUI] Failed to load modal image from '{url}': {req.error}");
				yield break;
			}

			Texture2D tex = DownloadHandlerTexture.GetContent(req);
			if (tex != null)
			{
				// Clear existing content
				modalImage.Clear();
				
				// Create image element
				var imageElement = new Image();
				imageElement.image = tex;
				imageElement.style.width = Length.Percent(100);
				imageElement.style.height = Length.Percent(100);
				imageElement.scaleMode = ScaleMode.ScaleToFit;
				modalImage.Add(imageElement);
			}
		}
	}
}


