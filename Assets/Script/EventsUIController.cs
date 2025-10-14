using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
	private VisualElement backButton;
	private VisualElement healthButton;
	private VisualElement searchFieldShadow;
	private bool uiWired = false;
	[SerializeField]
	private bool verboseLogging = false;

	private List<EventData> current = new List<EventData>();
	private EventData currentEventData; // Store current event for navigation

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
	private VisualElement navigationButton;


	[SerializeField]
	private string refreshButtonName = "RefreshButton";

	[SerializeField]
	private string backButtonName = "back-button";

	[SerializeField]
	private string healthButtonName = "health-button";

	[Header("Navigation Integration")]
	[SerializeField]
	private TargetHandler targetHandler;
	
	[Header("App Canvas Integration")]
	[SerializeField]
	private AppCanvas appCanvas;

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
		backButton = root.Q<VisualElement>(backButtonName);

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

		if (backButton == null)
		{
			Debug.LogWarning($"EventsUIController: VisualElement '{backButtonName}' not found in UXML.");
		}
		else
		{
			// Register for click events on the back button
			backButton.RegisterCallback<ClickEvent>(OnBackButtonClicked);
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

		// Find TargetHandler if not assigned
		if (targetHandler == null)
		{
#if UNITY_2022_2_OR_NEWER
			targetHandler = FindFirstObjectByType<TargetHandler>(FindObjectsInactive.Include);
#else
			targetHandler = FindObjectOfType<TargetHandler>(true);
#endif
		}

		// Find AppCanvas if not assigned
		if (appCanvas == null)
		{
#if UNITY_2022_2_OR_NEWER
			appCanvas = FindFirstObjectByType<AppCanvas>(FindObjectsInactive.Include);
#else
			appCanvas = FindObjectOfType<AppCanvas>(true);
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

		if (backButton != null)
		{
			backButton.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
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
		Debug.Log($"=== OnEventsChanged called with {list?.Count ?? -1} events ===");
		current = list ?? new List<EventData>();
		Debug.Log($"Current data set to {current.Count} events");
		RebuildList(current);
		Debug.Log("RebuildList completed");            
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
		Debug.Log($"=== RebuildList called with {list?.Count ?? -1} items ===");
		
		if (listView == null)
		{
			Debug.LogWarning("[EventsUI] RebuildList called but listView is null.");
			return;
		}

		Debug.Log("Clearing listView");
		listView.Clear();

		Debug.Log($"Adding {list.Count} items to listView");
		foreach (var item in list)
		{
			var row = BuildCard(item);
			listView.Add(row);
		}

		if (list.Count == 0)
		{
			Debug.Log("No items found, adding empty state message");
			var empty = new Label("No events found.");
			empty.style.unityTextAlign = TextAnchor.MiddleCenter;
			empty.style.color = new Color(0.4f, 0.4f, 0.4f);
			empty.style.marginTop = 16;
			listView.Add(empty);
		}
		
		Debug.Log("RebuildList completed successfully");
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
	/// Called when the back button is clicked
	/// </summary>
	private void OnBackButtonClicked(ClickEvent evt)
	{
		GoBackToMenu();
	}

	/// <summary>
	/// Public method to refresh events data
	/// Can be called from other scripts or UI elements
	/// </summary>
	public void RefreshEvents()
	{
		Debug.Log("=== RefreshEvents called ===");
		
		if (eventsFetcher == null)
		{
			Debug.LogWarning("[EventsUI] Cannot refresh - EventsFetcher is null");
			// Try to show existing data if available
			if (current != null && current.Count > 0)
			{
				Debug.Log($"Showing {current.Count} existing events data");
				RebuildList(current);
			}
			else
			{
				Debug.Log("No existing data to show");
			}
			return;
		}
		
		Debug.Log("EventsFetcher found, proceeding with refresh");
		
		// Disable refresh button during loading to prevent multiple requests
		if (refreshButton != null)
		{
			refreshButton.SetEnabled(false);
			Debug.Log("Refresh button disabled");
		}
		
		// Force refresh by clearing current data first
		Debug.Log($"Clearing {current.Count} current items");
		current.Clear();
		RebuildList(current);
		
		Debug.Log("Calling eventsFetcher.FetchAllEvents()");
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

	/// <summary>
	/// Called when the back button is clicked - returns to the main menu
	/// </summary>
	public void GoBackToMenu()
	{
		// Simply hide the events UI; keep data so it can re-show quickly next time
		HideEventsUI();
		
		// Find the ToggleMenu component
		ToggleMenu toggleMenu = FindObjectOfType<ToggleMenu>();
		
		if (toggleMenu != null)
		{
			toggleMenu.ShowBothMenuAndBurger();
			Debug.Log("Back button clicked - showing menu panel and burger button, events UI reset");
		}
		else
		{
			Debug.LogWarning("ToggleMenu component not found! Please make sure it exists in the scene.");
		}
	}

	/// <summary>
	/// Hide the events UI
	/// </summary>
	public void HideEventsUI()
	{
		if (uiDocument != null)
		{
			uiDocument.rootVisualElement.style.display = DisplayStyle.None;
			Debug.Log("Events UI hidden");
		}
	}

	/// <summary>
	/// Show the events UI and refresh data
	/// </summary>
	public void ShowEventsUI()
	{
		Debug.Log("=== ShowEventsUI called ===");
		
		if (uiDocument != null)
		{
			Debug.Log("UIDocument found, setting display to Flex");
			uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
			
			// Recreate modal every time UI is shown to ensure it works
			CreateModal();
			
			// Rewire UI every time in case the visual tree was rebuilt
			WireUi();
			
			// Reset search field if needed
			if (searchField != null)
			{
				Debug.Log("Resetting search field");
				searchField.SetValueWithoutNotify(string.Empty);
				SetPlaceholder();
			}
			
			// Always show whatever data we currently have immediately
			if (current != null)
			{
				RebuildList(current);
			}
			
			// Check current data state
			Debug.Log($"Current data count: {(current != null ? current.Count : -1)}");
			Debug.Log($"EventsFetcher is null: {eventsFetcher == null}");
			
			// Refresh the events data when showing the UI
			if (eventsFetcher != null)
			{
				Debug.Log("EventsFetcher found, calling RefreshEvents");
				RefreshEvents();
			}
			else
			{
				Debug.Log("EventsFetcher is null, trying to show existing data");
				// If no fetcher, try to populate with existing data
				if (current != null && current.Count > 0)
				{
					Debug.Log($"Rebuilding list with {current.Count} existing items");
					RebuildList(current);
				}
				else
				{
					Debug.Log("No existing data to show, showing empty state");
					RebuildList(new List<EventData>());
				}
			}
			
			Debug.Log("Events UI shown and data refreshed");
		}
		else
		{
			Debug.LogError("UIDocument is null!");
		}
	}

	private void WireUi()
	{
		var root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[EventsUI] WireUi called but root is null");
			return;
		}

		// Re-query elements (UI Toolkit can rebuild the visual tree)
		var newSearch = root.Q<TextField>(searchFieldName);
		var newList = root.Q<ScrollView>(scrollViewName);
		var newRefresh = root.Q<VisualElement>(refreshButtonName);
		var newBack = root.Q<VisualElement>(backButtonName);
		var newHealth = root.Q<VisualElement>(healthButtonName);

		// Update references
		searchField = newSearch ?? searchField;
		listView = newList ?? listView;
		refreshButton = newRefresh ?? refreshButton;
		backButton = newBack ?? backButton;
		healthButton = newHealth ?? healthButton;

		// Ensure basic listView layout settings again
		if (listView != null)
		{
			listView.style.flexGrow = 1;
			listView.style.flexShrink = 1;
			listView.style.minHeight = 0;
		}

		// Safe re-registration of callbacks
		if (refreshButton != null)
		{
			refreshButton.UnregisterCallback<ClickEvent>(OnRefreshClicked);
			refreshButton.RegisterCallback<ClickEvent>(OnRefreshClicked);
			refreshButton.SetEnabled(true);
		}

		if (backButton != null)
		{
			backButton.UnregisterCallback<ClickEvent>(OnBackButtonClicked);
			backButton.RegisterCallback<ClickEvent>(OnBackButtonClicked);
		}

		if (healthButton != null)
		{
			healthButton.UnregisterCallback<ClickEvent>(OnHealthClicked);
			healthButton.RegisterCallback<ClickEvent>(OnHealthClicked);
		}

		// Placeholder handling for search
		if (searchField != null && !uiWired)
		{
			searchField.RegisterCallback<FocusInEvent>(evt => OnSearchFocusIn());
			searchField.RegisterCallback<FocusOutEvent>(evt => OnSearchFocusOut());
			searchField.RegisterValueChangedCallback(_ => ApplyFilter());
		}

		uiWired = true;
	}

	private void OnHealthClicked(ClickEvent evt)
	{
		HealthCheck();
	}

	[ContextMenu("Events UI - HealthCheck")]
	public void HealthCheck()
	{
		try
		{
			Debug.Log("[EventsUI] === HealthCheck ===");
			Debug.Log($"UIDocument: {(uiDocument != null)} | Root: {(uiDocument != null && uiDocument.rootVisualElement != null)}");
			Debug.Log($"Refs -> search:{(searchField!=null)} list:{(listView!=null)} refresh:{(refreshButton!=null)} back:{(backButton!=null)} health:{(healthButton!=null)}");
			Debug.Log($"Current items: {(current!=null?current.Count:-1)}");
			var fetcher = eventsFetcher;
			if (fetcher == null)
			{
#if UNITY_2022_2_OR_NEWER
				fetcher = FindFirstObjectByType<EventsFetcher>(FindObjectsInactive.Include);
#else
				fetcher = FindObjectOfType<EventsFetcher>(true);
#endif
			}
			Debug.Log($"EventsFetcher present: {(fetcher!=null)}");
			Debug.Log($"AppCanvas present: {(appCanvas!=null)}");
			if (fetcher != null)
			{
				fetcher.EmitCachedEvents();
				Debug.Log("[EventsUI] Emitted cached events.");
			}
			// Rebuild view with whatever we have
			if (current != null)
			{
				RebuildList(current);
				Debug.Log("[EventsUI] Rebuilt list from current cache.");
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogError($"[EventsUI] HealthCheck error: {ex.Message}");
		}
	}

	public void SetVerboseLogging(bool on)
	{
		verboseLogging = on;
		Debug.Log($"[EventsUI] verboseLogging set to {on}");
	}

	private VisualElement BuildCard(EventData data)
	{
		var row = new VisualElement();
		row.style.flexDirection = FlexDirection.Row;
		row.style.height = 300;
		row.style.marginBottom = 30;
		row.style.marginLeft = 16;
		row.style.marginRight = 16;
		row.style.backgroundColor = new Color(0.851f, 0.851f, 0.851f, 1f); // rgb(224, 224, 224)
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
		row.RegisterCallback<ClickEvent>(evt => {
			Debug.Log($"[EventsUI] Event card clicked for: {data.name}");
			ShowEventModal(data);
		});

		// Image container with question mark icon
		var imgContainer = new VisualElement();
		imgContainer.style.width = 161;
		imgContainer.style.height = 210;
		imgContainer.style.backgroundColor = new Color(240f/255f, 240f/255f, 240f/255f, 1f); // rgb(240, 240, 240)
		imgContainer.style.borderBottomLeftRadius = 6;
		imgContainer.style.borderBottomRightRadius = 6;
		imgContainer.style.borderTopLeftRadius = 6;
		imgContainer.style.borderTopRightRadius = 6;
		imgContainer.style.alignItems = Align.Center;
		imgContainer.style.justifyContent = Justify.Center;

		// Question mark icon (no image loading)
		var questionMark = new Label("?");
		questionMark.style.fontSize = 40;
		questionMark.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
		questionMark.style.unityFontStyleAndWeight = FontStyle.Bold;
		imgContainer.Add(questionMark);

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
		title.style.fontSize = 36;
		title.style.color = new Color(0f, 0f, 0f, 1f);

		// Location
		var location = new Label(string.IsNullOrEmpty(data.location) ? string.Empty : $"Location: {data.location}");
		location.style.fontSize = 32;
		location.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);

		// Time
		var time = new Label(string.IsNullOrEmpty(data.time) ? "No time set" : data.time);
		time.style.fontSize = 32;
		time.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);

		col.Add(title);
		col.Add(location);
		col.Add(time);
		row.Add(col);

		// Image loading removed to prevent UriFormatException

		return row;
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
		searchFieldShadow.style.left = 5;
		searchFieldShadow.style.top = 5;
		
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
    Debug.Log("[EventsUI] CreateModal called");
    var root = uiDocument != null ? uiDocument.rootVisualElement : null;
    if (root == null) 
    {
        Debug.LogError("[EventsUI] CreateModal: UIDocument or rootVisualElement is null!");
        return;
    }

    // Remove existing modal if it exists to prevent duplicates
    if (modalOverlay != null && modalOverlay.parent != null)
    {
        Debug.Log("[EventsUI] Removing existing modal");
        modalOverlay.RemoveFromHierarchy();
    }

    Debug.Log("[EventsUI] Creating modal overlay");
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
    modalContent.style.height = 1100; // was 500
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
    closeButton.style.width = 70; // was 30
    closeButton.style.height = 70; // was 30
    closeButton.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
    closeButton.style.borderTopLeftRadius = 30;
    closeButton.style.borderTopRightRadius = 30;
    closeButton.style.borderBottomLeftRadius = 30;
    closeButton.style.borderBottomRightRadius = 30;
    closeButton.style.alignItems = Align.Center;
    closeButton.style.justifyContent = Justify.Center;
    closeButton.RegisterCallback<ClickEvent>(evt => HideEventModal());

    var closeLabel = new Label("x");
    closeLabel.style.fontSize = 45; // was 20
    closeLabel.style.color = Color.black;
    closeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
    closeButton.Add(closeLabel);

    // Title (bigger font)
    modalTitle = new Label("Event Details");
    modalTitle.style.fontSize = 64; // was 24
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
    modalDescription.style.fontSize = 32; // was 14
    modalDescription.style.color = Color.black;
    modalDescription.style.marginBottom = 20; // was 10
    modalDescription.style.whiteSpace = WhiteSpace.Normal;

    modalLocation = new Label();
    modalLocation.style.fontSize = 32;
    modalLocation.style.color = Color.black;
    modalLocation.style.marginBottom = 20;
    modalLocation.style.whiteSpace = WhiteSpace.Normal;

    modalStartTime = new Label();
    modalStartTime.style.fontSize = 32;
    modalStartTime.style.color = Color.black;
    modalStartTime.style.marginBottom = 20;
    modalStartTime.style.whiteSpace = WhiteSpace.Normal;

    modalEndTime = new Label();
    modalEndTime.style.fontSize = 32;
    modalEndTime.style.color = Color.black;
    modalEndTime.style.marginBottom = 20;
    modalEndTime.style.whiteSpace = WhiteSpace.Normal;

    // Navigation button
    navigationButton = new VisualElement();
    navigationButton.style.width = Length.Percent(100);
    navigationButton.style.height = 120;
    navigationButton.style.backgroundColor = new Color(0.1059f, 0.3725f, 0.1843f, 1f); // Green color
    navigationButton.style.borderTopLeftRadius = 12;
    navigationButton.style.borderTopRightRadius = 12;
    navigationButton.style.borderBottomLeftRadius = 12;
    navigationButton.style.borderBottomRightRadius = 12;
    navigationButton.style.marginTop = 50;
    navigationButton.style.alignItems = Align.Center;
    navigationButton.style.justifyContent = Justify.Center;
    navigationButton.style.cursor = StyleKeyword.Auto;

    var navigationLabel = new Label("Navigate to Event Location");
    navigationLabel.style.fontSize = 32;
    navigationLabel.style.color = Color.white;
    navigationLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
    navigationLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
    navigationButton.Add(navigationLabel);

    // Register click event for navigation button
    navigationButton.RegisterCallback<ClickEvent>(evt => OnNavigationButtonClicked());

    // Add elements
    modalContent.Add(closeButton);
    modalContent.Add(modalTitle);
    modalContent.Add(modalImage);
    modalContent.Add(modalDescription);
    modalContent.Add(modalLocation);
    modalContent.Add(modalStartTime);
    modalContent.Add(modalEndTime);
    modalContent.Add(navigationButton);

    modalOverlay.Add(modalContent);
    root.Add(modalOverlay);
    Debug.Log("[EventsUI] Modal created and added to root successfully");
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
		Debug.Log($"[EventsUI] ShowEventModal called for: {eventData?.name}");
		
		// Ensure modal exists before trying to show it
		EnsureModalExists();
		
		Debug.Log($"[EventsUI] Modal overlay is null: {modalOverlay == null}");
		
		if (modalOverlay == null) 
		{
			Debug.LogError("[EventsUI] Modal overlay is null! Modal was not created properly.");
			return;
		}

		// Store current event data for navigation
		currentEventData = eventData;

		// Populate modal with event data
		modalTitle.text = string.IsNullOrEmpty(eventData.name) ? "Event Details" : eventData.name;
		modalDescription.text = $"Description: {(!string.IsNullOrEmpty(eventData.description) ? eventData.description : "No description provided")}";
		modalLocation.text = $"Location: {(!string.IsNullOrEmpty(eventData.location) ? eventData.location : "No location set")}";
		modalStartTime.text = $"Start Time: {FormatDateTime(eventData.startTime)}";
		modalEndTime.text = $"End Time: {FormatDateTime(eventData.endTime)}";

		// Image loading removed to prevent UriFormatException
		// Clear any existing image and show placeholder
		modalImage.Clear();
		var imagePlaceholder = new Label("No image available");
		imagePlaceholder.style.color = new Color(0.6f, 0.6f, 0.6f);
		imagePlaceholder.style.fontSize = 14;
		modalImage.Add(imagePlaceholder);

		// Show modal
		Debug.Log("[EventsUI] Setting modal display to Flex");
		modalOverlay.style.display = DisplayStyle.Flex;
		Debug.Log("[EventsUI] Modal should now be visible");
	}

	private void HideEventModal()
	{
		if (modalOverlay != null)
		{
			modalOverlay.style.display = DisplayStyle.None;
		}
	}

	/// <summary>
	/// Ensures modal is properly created and ready to use
	/// </summary>
	private void EnsureModalExists()
	{
		if (modalOverlay == null || modalOverlay.parent == null)
		{
			Debug.Log("[EventsUI] Modal missing, recreating...");
			CreateModal();
		}
	}

	private void OnNavigationButtonClicked()
	{
		if (currentEventData == null)
		{
			Debug.LogWarning("[EventsUI] No current event data available for navigation");
			return;
		}

		if (targetHandler == null)
		{
			Debug.LogWarning("[EventsUI] TargetHandler not found. Cannot navigate to event location.");
			return;
		}

		Debug.Log($"[EventsUI] Navigation button clicked for event: {currentEventData.name}");
		Debug.Log($"[EventsUI] Event location data - Location: '{currentEventData.location}'");

		// Try to find matching target in dropdown
		bool found = FindAndSetDropdownValue(currentEventData);
		
		if (found)
		{
			Debug.Log("[EventsUI] Successfully set dropdown to event location");
			
			// Update the name label in AppCanvas with the event name
			if (appCanvas != null)
			{
				appCanvas.UpdateNameLabel(currentEventData.name);
				Debug.Log($"[EventsUI] Updated name label with event: {currentEventData.name}");
			}
			else
			{
				Debug.LogWarning("[EventsUI] AppCanvas not found, cannot update name label");
			}
			
			// Use existing back button functionality to close events UI and show menu/burger
			GoBackToMenu();
		}
		else
		{
			Debug.LogWarning("[EventsUI] Could not find matching location in dropdown for navigation");
		}
	}

	private bool FindAndSetDropdownValue(EventData eventData)
	{
		if (targetHandler == null) return false;

		// Get the dropdown from TargetHandler
		var dropdown = targetHandler.GetComponent<TargetHandler>();
		if (dropdown == null)
		{
			Debug.LogWarning("[EventsUI] Could not get TargetHandler component");
			return false;
		}

		// Use reflection to access the private targetDataDropdown field
		var dropdownField = typeof(TargetHandler).GetField("targetDataDropdown", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		
		if (dropdownField == null)
		{
			Debug.LogWarning("[EventsUI] Could not access targetDataDropdown field");
			return false;
		}

		var targetDropdown = dropdownField.GetValue(targetHandler) as TMPro.TMP_Dropdown;
		if (targetDropdown == null)
		{
			Debug.LogWarning("[EventsUI] Target dropdown is null");
			return false;
		}

		// Search for matching option based on event location data
		string eventLocation = eventData.location ?? "";

		Debug.Log($"[EventsUI] Searching for location: '{eventLocation}'");

		// Try different matching strategies
		for (int i = 0; i < targetDropdown.options.Count; i++)
		{
			var option = targetDropdown.options[i];
			string optionText = option.text.ToLowerInvariant();
			
			Debug.Log($"[EventsUI] Checking option {i}: '{option.text}'");

			// Strategy 1: Exact location match
			if (!string.IsNullOrEmpty(eventLocation) && optionText.Contains(eventLocation.ToLowerInvariant()))
			{
				Debug.Log($"[EventsUI] Found exact location match at index {i}: '{option.text}'");
				targetDropdown.value = i;
				targetDropdown.RefreshShownValue();
				return true;
			}

			// Strategy 2: Partial location match (split by common separators)
			if (!string.IsNullOrEmpty(eventLocation))
			{
				string[] locationParts = eventLocation.Split(new char[] { ' ', '-', ',', '_' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string part in locationParts)
				{
					if (!string.IsNullOrEmpty(part) && optionText.Contains(part.ToLowerInvariant()))
					{
						Debug.Log($"[EventsUI] Found partial location match at index {i}: '{option.text}' (matched part: '{part}')");
						targetDropdown.value = i;
						targetDropdown.RefreshShownValue();
						return true;
					}
				}
			}
		}

		Debug.LogWarning("[EventsUI] No matching location found in dropdown options");
		return false;
	}

	
	/// <summary>
	/// Force refresh the events UI - use this if normal refresh isn't working
	/// </summary>
	public void ForceRefreshEventsUI()
	{
		Debug.Log("=== ForceRefreshEventsUI called ===");
		
		// Show UI first
		if (uiDocument != null)
		{
			uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
		}
		
		// Wait a frame then refresh
		StartCoroutine(DelayedRefresh());
	}
	
	private IEnumerator DelayedRefresh()
	{
		yield return null; // Wait one frame
		Debug.Log("Performing delayed refresh");
		RefreshEvents();
	}
}