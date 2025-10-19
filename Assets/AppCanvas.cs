using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum SearchMethod
{
	Name,       // Search by GameObject name only
	Text,       // Search by Text component only
	TMP_Text,   // Search by TMP_Text component only
	All         // Search by all available text sources
}

public enum SearchMode
{
	Contains,    // Search term can be anywhere in the text
	StartsWith,  // Text must start with search term
	Exact        // Text must exactly match search term
}

public class AppCanvas : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform bottomPanel;
    public RectTransform dragHandle;
    public RectTransform bottomPanel2; // Second bottom panel reference
    public RectTransform dragHandle2; // Second drag handle reference
    
    [Header("Container References")]
    public RectTransform simpleContainer;    // Container with Destination and Advance Options buttons
    public RectTransform advancedContainer;  // Container with current advanced settings
    
    [Header("Collider Settings")]
    public bool useCollider = true;
    public float colliderHeight = 50f; // Height of the collider area
    
    [Header("Panel States")]
    public float originalYPosition = 0f;    // Original position (where it starts)
    public float collapsedYPosition = -550f; // Position when collapsed
    
    [Header("Advanced Panel States")]
    public float advancedOriginalYPosition = 0f;    // Original position for advanced panel
    public float advancedCollapsedYPosition = -550f; // Position when advanced panel is collapsed
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private bool isCollapsed = false;
    private bool isAdvancedCollapsed = false; // Separate collapse state for advanced panel
    private bool isDragging = false;
    private bool isAnimating = false;
    private Vector2 dragStartPosition;
    private float dragStartYPosition;
    private Coroutine currentAnimation;
    
    [Header("Search UI")]
    public Dropdown myDropdown;
    public InputField searchInput;

	[Header("Search UI - TextMeshPro (optional)")]
	public TMP_Dropdown myTMPDropdown;
	public TMP_InputField searchTMPInput;
	
	[Header("Name Label")]
	public TMP_Text nameLabel; // Label to display the selected event name

	[Header("Search UI - Options Panel (optional)")]
	public InputField panelSearchInput; // Search field inside the popup panel (UGUI)
	public TMP_InputField panelSearchTMPInput; // Search field inside the popup panel (TMP)

	[Header("Popup Option Panel")]
	public GameObject optionsPanel; // Panel to show/hide
	public Button openOptionsButton; // Button to open panel
	public Button closeOptionsButton; // Button to close panel
	public RectTransform optionsContainer; // Parent container for option items
	public GameObject optionItemPrefab; // Prefab expected to have a Button and a Text/TMP_Text
	
	[Header("Search Filtering")]
	public RectTransform locationsContainer; // Container with location items to filter
	[Tooltip("How to search for text in location items: Name, Text, TMP_Text, or All")]
	public SearchMethod searchMethod = SearchMethod.All;
	[Tooltip("Search in children recursively (not just direct children)")]
	public bool searchRecursively = true;
	[Tooltip("Case sensitive search")]
	public bool caseSensitive = false;
	[Tooltip("Search mode: Contains (anywhere), StartsWith, or Exact")]
	public SearchMode searchMode = SearchMode.Contains;

	[Header("Locations Item Size")]
	[Tooltip("Desired width for each location item (px)")]
	public float locationItemWidth = 400f;
	[Tooltip("Desired height for each location item (px)")]
	public float locationItemHeight = 80f;
	[Tooltip("Apply item sizes automatically on Start()")]
	public bool applySizeOnStart = false;
	
	[Tooltip("When opening, move panel to end of sibling order and (optionally) bump sorting.")]
	public bool ensureOptionsPanelOnTop = true;
	[Tooltip("If a Canvas exists on the panel, temporarily override sorting with this order while open.")]
	public bool overridePanelSortingWhileOpen = false;
	public int optionsPanelSortingOrder = 1000;

	private readonly List<GameObject> spawnedOptionItems = new List<GameObject>();
	private bool savedPanelOverrideSorting;
	private int savedPanelSortingOrder;
	private bool hasSavedSortingState = false;
    
    [Header("Container State")]
    private bool showingAdvancedOptions = false;
    
    void Start()
    {
        // Initialize the panel - preserve your manual Y position as the original
        if (bottomPanel != null)
        {
            // Use the current Y position as the original position
            originalYPosition = bottomPanel.anchoredPosition.y;
        }
        
        // Initialize container visibility
        InitializeContainers();

        // Wire dropdown -> search bar sync
		if (myDropdown != null)
		{
			myDropdown.onValueChanged.AddListener(OnDropdownChanged);
		}
		if (myTMPDropdown != null)
		{
			myTMPDropdown.onValueChanged.AddListener(OnTMPDropdownChanged);
		}
		SyncSearchInputToCurrentSelection();

		// Wire popup panel open/close
		if (openOptionsButton != null)
		{
			openOptionsButton.onClick.AddListener(OpenOptionsPanel);
		}
		if (closeOptionsButton != null)
		{
			closeOptionsButton.onClick.AddListener(CloseOptionsPanel);
		}
		// Ensure panel starts hidden if assigned
		if (optionsPanel != null)
		{
			optionsPanel.SetActive(false);
		}

		// Optional: live search filtering while panel is open
		if (searchInput != null)
		{
			searchInput.onValueChanged.AddListener(OnSearchValueChanged);
		}
		if (searchTMPInput != null)
		{
			searchTMPInput.onValueChanged.AddListener(OnTMPSearchValueChanged);
		}
		// Wire panel-local search inputs (live filter only when panel is open)
		if (panelSearchInput != null)
		{
			panelSearchInput.onValueChanged.AddListener(OnPanelSearchValueChanged);
		}
		if (panelSearchTMPInput != null)
		{
			panelSearchTMPInput.onValueChanged.AddListener(OnPanelTMPSearchValueChanged);
		}

		// Optionally size location items on start
		if (applySizeOnStart)
		{
			ApplyLocationItemSizes();
		}
    }
    
    private void InitializeContainers()
    {
        // Start with simple container visible and advanced container hidden
        if (simpleContainer != null)
        {
            simpleContainer.gameObject.SetActive(true);
        }
        
        if (advancedContainer != null)
        {
            advancedContainer.gameObject.SetActive(false);
        }
        
        // Initialize panel states
        showingAdvancedOptions = false;
        isCollapsed = false;
        isAdvancedCollapsed = false;
        
        // Initialize panel positions
        if (bottomPanel != null)
        {
            bottomPanel.anchoredPosition = new Vector2(bottomPanel.anchoredPosition.x, originalYPosition);
        }
        
        if (bottomPanel2 != null)
        {
            bottomPanel2.anchoredPosition = new Vector2(bottomPanel2.anchoredPosition.x, advancedOriginalYPosition);
        }
    }
    
    void Update()
    {
        HandleDragInput();
    }
    
    private void HandleDragInput()
    {
        // Check if we have at least one drag handle and panel
        if ((dragHandle == null && dragHandle2 == null) || (bottomPanel == null && bottomPanel2 == null)) return;
        
        Vector2 inputPos = Vector2.zero;
        
        // Check for touch input (Android)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPos = touch.position;
            
            if (touch.phase == TouchPhase.Began)
            {
                HandleDragStart(inputPos);
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                HandleDragMove(inputPos);
            }
            else if (touch.phase == TouchPhase.Ended && isDragging)
            {
                HandleDragEnd(inputPos);
            }
        }
        // Check for mouse input (Editor/Desktop)
        else
        {
            inputPos = Input.mousePosition;
            
            if (Input.GetMouseButtonDown(0))
            {
                HandleDragStart(inputPos);
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                HandleDragMove(inputPos);
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                HandleDragEnd(inputPos);
            }
        }
    }
    
    private void HandleDragStart(Vector2 inputPos)
    {
        // Check first drag handle
        if (dragHandle != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragHandle, inputPos, null, out Vector2 localPoint);
            
            if (dragHandle.rect.Contains(localPoint))
            {
                isDragging = true;
                dragStartPosition = inputPos;
                dragStartYPosition = bottomPanel != null ? bottomPanel.anchoredPosition.y : 0f;
                return;
            }
        }
        
        // Check second drag handle
        if (dragHandle2 != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragHandle2, inputPos, null, out Vector2 localPoint);
            
            if (dragHandle2.rect.Contains(localPoint))
            {
                isDragging = true;
                dragStartPosition = inputPos;
                dragStartYPosition = bottomPanel2 != null ? bottomPanel2.anchoredPosition.y : 0f;
            }
        }
    }
    
    private void HandleClick(Vector2 inputPos)
    {
        // Check first drag handle
        if (dragHandle != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragHandle, inputPos, null, out Vector2 localPoint);
            
            if (dragHandle.rect.Contains(localPoint))
            {
                TogglePanelState(bottomPanel);
                return;
            }
        }
        
        // Check second drag handle
        if (dragHandle2 != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragHandle2, inputPos, null, out Vector2 localPoint);
            
            if (dragHandle2.rect.Contains(localPoint))
            {
                TogglePanelState(bottomPanel2);
                return;
            }
        }
    }
    
    private void TogglePanelState(RectTransform panel)
    {
        if (panel == null) return;
        
        // Determine which panel states to use based on the panel
        float originalPos, collapsedPos;
        bool currentCollapsedState;
        
        if (panel == bottomPanel2) // Advanced panel
        {
            originalPos = advancedOriginalYPosition;
            collapsedPos = advancedCollapsedYPosition;
            currentCollapsedState = isAdvancedCollapsed;
        }
        else // Simple panel
        {
            originalPos = originalYPosition;
            collapsedPos = collapsedYPosition;
            currentCollapsedState = isCollapsed;
        }
        
        // Toggle between collapsed and original position
        if (currentCollapsedState)
        {
            // Expand to original position with animation
            if (panel == bottomPanel2)
            {
                isAdvancedCollapsed = false;
            }
            else
            {
                isCollapsed = false;
            }
            AnimateToPosition(originalPos, panel);
        }
        else
        {
            // Collapse with animation
            if (panel == bottomPanel2)
            {
                isAdvancedCollapsed = true;
            }
            else
            {
                isCollapsed = true;
            }
            AnimateToPosition(collapsedPos, panel);
        }
    }
    
    private void HandleDragMove(Vector2 inputPos)
    {
        if (!isDragging) return;
        
        // No real-time dragging - panel stays at current position during drag
        // Only snap to final position when drag ends
    }
    
    private void HandleDragEnd(Vector2 inputPos)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        float dragDelta = inputPos.y - dragStartPosition.y;
        
        // Check if it's a click (no movement) or a drag
        if (Mathf.Abs(dragDelta) < 10f) // Small threshold for click detection
        {
            // It's a click - toggle the panel
            HandleClick(inputPos);
        }
        else
        {
            // It's a drag - use drag logic
            if (dragDelta < 0) // Dragged down (negative delta)
            {
                if (useCollider)
                {
                    // Check if panel is already at collider limit
                    float currentY = bottomPanel.anchoredPosition.y;
                    float colliderLimit = originalYPosition - colliderHeight;
                    
                    if (currentY <= colliderLimit)
                    {
                        // Don't collapse, stay at current position
                        return;
                    }
                }
                
                // Always collapse when dragging down
                RectTransform activePanel = GetCurrentActivePanel();
                if (activePanel == bottomPanel2) // Advanced panel
                {
                    isAdvancedCollapsed = true;
                    AnimateToPosition(advancedCollapsedYPosition, activePanel);
                }
                else // Simple panel
                {
                    isCollapsed = true;
                    AnimateToPosition(collapsedYPosition, activePanel);
                }
            }
            else if (dragDelta > 0) // Dragged up (positive delta)
            {
                // Always expand when dragging up
                RectTransform activePanel = GetCurrentActivePanel();
                if (activePanel == bottomPanel2) // Advanced panel
                {
                    isAdvancedCollapsed = false;
                    AnimateToPosition(advancedOriginalYPosition, activePanel);
                }
                else // Simple panel
                {
                    isCollapsed = false;
                    AnimateToPosition(originalYPosition, activePanel);
                }
            }
        }
    }
    
    private void AnimateToPosition(float targetYPosition, RectTransform panel = null)
    {
        if (isAnimating) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Use the specified panel, or default to bottomPanel
        RectTransform targetPanel = panel != null ? panel : bottomPanel;
        currentAnimation = StartCoroutine(AnimatePanel(targetYPosition, targetPanel));
    }
    
    private IEnumerator AnimatePanel(float targetYPosition, RectTransform panel)
    {
        isAnimating = true;
        
        Vector2 startPosition = panel.anchoredPosition;
        Vector2 endPosition = new Vector2(startPosition.x, targetYPosition);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            panel.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curveValue);
            
            yield return null;
        }
        
        panel.anchoredPosition = endPosition;
        isAnimating = false;
        currentAnimation = null;
    }
    
    private void OnDropdownChanged(int index)
    {
		SyncSearchInputToCurrentSelection();
    }

	private void OnTMPDropdownChanged(int index)
	{
		SyncSearchInputToCurrentSelection();
	}

	private void SyncSearchInputToCurrentSelection()
	{
		string selectedText = GetSelectedOptionText();
		if (string.IsNullOrEmpty(selectedText)) return;
		// Prefer TMP input if assigned; otherwise use legacy UI input
		if (searchTMPInput != null)
		{
			searchTMPInput.text = selectedText;
		}
		else if (searchInput != null)
		{
			searchInput.text = selectedText;
		}
	}

	private string GetSelectedOptionText()
	{
		if (myTMPDropdown != null)
		{
			int idx = myTMPDropdown.value;
			if (idx >= 0 && idx < myTMPDropdown.options.Count)
			{
				return myTMPDropdown.options[idx].text;
			}
		}
		if (myDropdown != null)
		{
			int idx = myDropdown.value;
			if (idx >= 0 && idx < myDropdown.options.Count)
			{
				return myDropdown.options[idx].text;
			}
		}
		return null;
	}

	// ===== Popup Panel Logic =====
	public void OpenOptionsPanel()
	{
		if (optionsPanel == null || optionsContainer == null || optionItemPrefab == null) return;
		optionsPanel.SetActive(true);
		// Seed the panel search input with the current main search text (if any)
		string seedFilter = GetMainSearchFilter();
		if (panelSearchTMPInput != null)
		{
			panelSearchTMPInput.text = seedFilter;
			panelSearchTMPInput.ActivateInputField();
		}
		else if (panelSearchInput != null)
		{
			panelSearchInput.text = seedFilter;
			panelSearchInput.ActivateInputField();
		}
		if (ensureOptionsPanelOnTop)
		{
			// Bring to front within its parent hierarchy
			optionsPanel.transform.SetAsLastSibling();
			// Optionally bump sorting if a Canvas is present on the panel
			if (overridePanelSortingWhileOpen)
			{
				Canvas panelCanvas = optionsPanel.GetComponent<Canvas>();
				if (panelCanvas != null)
				{
					if (!hasSavedSortingState)
					{
						savedPanelOverrideSorting = panelCanvas.overrideSorting;
						savedPanelSortingOrder = panelCanvas.sortingOrder;
						hasSavedSortingState = true;
					}
					panelCanvas.overrideSorting = true;
					panelCanvas.sortingOrder = optionsPanelSortingOrder;
				}
			}
		}
		BuildOptionsList(GetCurrentSearchFilter());
	}

	public void CloseOptionsPanel()
	{
		if (optionsPanel == null) return;
		optionsPanel.SetActive(false);
		// Restore sorting state if we changed it
		if (overridePanelSortingWhileOpen)
		{
			Canvas panelCanvas = optionsPanel.GetComponent<Canvas>();
			if (panelCanvas != null && hasSavedSortingState)
			{
				panelCanvas.overrideSorting = savedPanelOverrideSorting;
				panelCanvas.sortingOrder = savedPanelSortingOrder;
				hasSavedSortingState = false;
			}
		}
		ClearSpawnedOptions();
	}

	private void BuildOptionsList(string filter)
	{
		ClearSpawnedOptions();
		
		// If we have a locations container, filter its children instead of dropdown options
		if (locationsContainer != null)
		{
            FilterLocationsContainer(filter);
			return;
		}
		
		// Fallback to dropdown options filtering
		List<string> optionTexts = GetAllOptionTexts();
		if (optionTexts == null || optionTexts.Count == 0) return;

		string filterLower = string.IsNullOrEmpty(filter) ? null : filter.ToLowerInvariant();
		for (int i = 0; i < optionTexts.Count; i++)
		{
			string text = optionTexts[i];
			if (filterLower != null && (text == null || !text.ToLowerInvariant().Contains(filterLower)))
			{
				continue;
			}
			int optionIndex = i; // capture
			GameObject item = Instantiate(optionItemPrefab, optionsContainer);
			SetLabelTextOnItem(item, text);
			Button btn = item.GetComponent<Button>();
			if (btn != null)
			{
				btn.onClick.AddListener(() => OnOptionItemClicked(optionIndex));
			}
			spawnedOptionItems.Add(item);
		}
	}
	
	private void FilterLocationsContainer(string filter)
	{
		if (locationsContainer == null) return;
		
		// Get content under ScrollRect and filter its children
		RectTransform content = GetLocationsContentRect();
		if (content == null) return;
		List<GameObject> itemsToFilter = new List<GameObject>();
		CollectFilterableItems(content, itemsToFilter);
		
        // Apply filter to each item
        foreach (GameObject item in itemsToFilter)
        {
            bool shouldShow = ShouldShowItem(item, filter);
            item.SetActive(shouldShow);
        }
        
        // Fix scroll bounds after filtering since content visibility changed
        FixLocationsScrollBounds();
	}
	
	private void CollectFilterableItems(Transform parent, List<GameObject> items)
	{
		if (searchRecursively)
		{
			// Collect all children recursively
			CollectAllChildren(parent, items);
		}
		else
		{
			// Collect only direct children
			for (int i = 0; i < parent.childCount; i++)
			{
				items.Add(parent.GetChild(i).gameObject);
			}
		}
	}

	public void ApplyLocationItemSizes()
	{
		if (locationsContainer == null) return;

		// Target the ScrollRect content under locationsContainer > Viewport > Content
		RectTransform content = GetLocationsContentRect();
		if (content == null) return;

		// If a GridLayoutGroup is present on the content, set cellSize only
		var grid = content.GetComponent<GridLayoutGroup>();
		if (grid != null)
		{
			grid.cellSize = new Vector2(locationItemWidth, locationItemHeight);
			// Fix scroll bounds after setting grid size
			FixLocationsScrollBounds();
			return;
		}

		// Otherwise, set each direct child's RectTransform size only (no LayoutElement)
		for (int i = 0; i < content.childCount; i++)
		{
			RectTransform rt = content.GetChild(i) as RectTransform;
			if (rt == null) continue;
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, locationItemWidth);
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, locationItemHeight);
		}
		
		// Fix scroll bounds after setting individual item sizes
		FixLocationsScrollBounds();
	}

	private RectTransform GetLocationsContentRect()
	{
		// Prefer ScrollRect.content if present
		var scroll = locationsContainer.GetComponentInParent<ScrollRect>();
		if (scroll != null && scroll.content != null)
		{
			return scroll.content;
		}

		// Fallback: find child by common names
		Transform viewport = locationsContainer.Find("Viewport");
		if (viewport == null && locationsContainer.childCount > 0)
		{
			// Try to locate a child that has a Mask and is commonly the viewport
			for (int i = 0; i < locationsContainer.childCount; i++)
			{
				var child = locationsContainer.GetChild(i);
				if (child.name.ToLowerInvariant().Contains("viewport"))
				{
					viewport = child;
					break;
				}
			}
		}
		if (viewport != null)
		{
			Transform content = viewport.Find("Content");
			if (content == null && viewport.childCount > 0)
			{
				// Heuristic: first child often is content
				content = viewport.GetChild(0);
			}
			return content as RectTransform;
		}

		// Last resort: if the container itself is the content
		return locationsContainer;
	}

	/// <summary>
	/// Fixes scrolling bounds for the locations container to prevent unlimited scrolling
	/// </summary>
	public void FixLocationsScrollBounds()
	{
		if (locationsContainer == null) return;

		// Get the ScrollRect component
		var scrollRect = locationsContainer.GetComponentInParent<ScrollRect>();
		if (scrollRect == null) return;

		// Get the content rect
		RectTransform content = GetLocationsContentRect();
		if (content == null) return;

		// Force layout rebuild to get accurate content size
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);

		// Just ensure the ScrollRect has proper movement type to prevent unlimited scrolling
		// Don't override horizontal/vertical settings as they might be intentionally configured
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		
		// Set deceleration rate to prevent excessive momentum
		scrollRect.decelerationRate = 0.135f; // Unity's default value
	}

	
	private void CollectAllChildren(Transform parent, List<GameObject> items)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			items.Add(child.gameObject);
			CollectAllChildren(child, items); // Recursive call
		}
	}
	
	private bool ShouldShowItem(GameObject item, string filter)
	{
		if (string.IsNullOrEmpty(filter))
		{
			return true; // Show all when no filter
		}
		
		string searchableText = GetSearchableText(item);
		if (string.IsNullOrEmpty(searchableText))
		{
			return false; // Hide items with no searchable text
		}
		
		// Apply case sensitivity
		string textToSearch = caseSensitive ? searchableText : searchableText.ToLowerInvariant();
		string filterToUse = caseSensitive ? filter : filter.ToLowerInvariant();
		
		// Apply search mode
		switch (searchMode)
		{
			case SearchMode.Contains:
				return textToSearch.Contains(filterToUse);
			case SearchMode.StartsWith:
				return textToSearch.StartsWith(filterToUse);
			case SearchMode.Exact:
				return textToSearch == filterToUse;
			default:
				return textToSearch.Contains(filterToUse);
		}
	}
	
	private string GetSearchableText(GameObject obj)
	{
		switch (searchMethod)
		{
			case SearchMethod.Name:
				return obj.name;
				
			case SearchMethod.Text:
				Text uiText = obj.GetComponentInChildren<Text>();
				return uiText != null ? uiText.text : "";
				
			case SearchMethod.TMP_Text:
				TMP_Text tmpText = obj.GetComponentInChildren<TMP_Text>();
				return tmpText != null ? tmpText.text : "";
				
			case SearchMethod.All:
			default:
				// Try all methods and combine results
				List<string> texts = new List<string>();
				
				// Add object name
				if (!string.IsNullOrEmpty(obj.name))
					texts.Add(obj.name);
				
				// Add Text component
				Text textComp = obj.GetComponentInChildren<Text>();
				if (textComp != null && !string.IsNullOrEmpty(textComp.text))
					texts.Add(textComp.text);
				
				// Add TMP_Text component
				TMP_Text tmpComp = obj.GetComponentInChildren<TMP_Text>();
				if (tmpComp != null && !string.IsNullOrEmpty(tmpComp.text))
					texts.Add(tmpComp.text);
				
				// Combine all texts with space separator
				return string.Join(" ", texts);
		}
	}

	private void ClearSpawnedOptions()
	{
		for (int i = 0; i < spawnedOptionItems.Count; i++)
		{
			if (spawnedOptionItems[i] != null)
			{
				Destroy(spawnedOptionItems[i]);
			}
		}
		spawnedOptionItems.Clear();
	}

	private void OnOptionItemClicked(int index)
	{
		// Prefer TMP dropdown if present, else legacy UI
		if (myTMPDropdown != null)
		{
			if (index >= 0 && index < myTMPDropdown.options.Count)
			{
				myTMPDropdown.value = index;
				myTMPDropdown.RefreshShownValue();
			}
		}
		else if (myDropdown != null)
		{
			if (index >= 0 && index < myDropdown.options.Count)
			{
				myDropdown.value = index;
				myDropdown.RefreshShownValue();
			}
		}
		SyncSearchInputToCurrentSelection();
		CloseOptionsPanel();
	}

	private void SetLabelTextOnItem(GameObject item, string text)
	{
		// Try TMP_Text first
		TMP_Text tmpLabel = item.GetComponentInChildren<TMP_Text>();
		if (tmpLabel != null)
		{
			tmpLabel.text = text;
			return;
		}
		// Fallback to legacy Text
		Text uiLabel = item.GetComponentInChildren<Text>();
		if (uiLabel != null)
		{
			uiLabel.text = text;
		}
	}

	private List<string> GetAllOptionTexts()
	{
		if (myTMPDropdown != null)
		{
			List<string> list = new List<string>(myTMPDropdown.options.Count);
			for (int i = 0; i < myTMPDropdown.options.Count; i++)
			{
				list.Add(myTMPDropdown.options[i].text);
			}
			return list;
		}
		if (myDropdown != null)
		{
			List<string> list = new List<string>(myDropdown.options.Count);
			for (int i = 0; i < myDropdown.options.Count; i++)
			{
				list.Add(myDropdown.options[i].text);
			}
			return list;
		}
		return null;
	}

	private string GetCurrentSearchFilter()
	{
		// Prefer panel-local inputs when the panel is open
		if (optionsPanel != null && optionsPanel.activeSelf)
		{
			if (panelSearchTMPInput != null) return panelSearchTMPInput.text;
			if (panelSearchInput != null) return panelSearchInput.text;
		}
		if (searchTMPInput != null) return searchTMPInput.text;
		if (searchInput != null) return searchInput.text;
		return null;
	}

	private string GetMainSearchFilter()
	{
		if (searchTMPInput != null) return searchTMPInput.text;
		if (searchInput != null) return searchInput.text;
		return null;
	}

	private void OnSearchValueChanged(string _)
	{
		if (optionsPanel != null && optionsPanel.activeSelf)
		{
			BuildOptionsList(GetCurrentSearchFilter());
		}
	}

	private void OnTMPSearchValueChanged(string _)
	{
		if (optionsPanel != null && optionsPanel.activeSelf)
		{
			BuildOptionsList(GetCurrentSearchFilter());
		}
	}

	private void OnPanelSearchValueChanged(string _)
	{
		if (optionsPanel != null && optionsPanel.activeSelf)
		{
			BuildOptionsList(GetCurrentSearchFilter());
		}
	}

	private void OnPanelTMPSearchValueChanged(string _)
	{
		if (optionsPanel != null && optionsPanel.activeSelf)
		{
			BuildOptionsList(GetCurrentSearchFilter());
		}
	}

    private void OnDestroy()
    {
        if (myDropdown != null)
        {
            myDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }
		if (myTMPDropdown != null)
		{
			myTMPDropdown.onValueChanged.RemoveListener(OnTMPDropdownChanged);
		}
		if (openOptionsButton != null)
		{
			openOptionsButton.onClick.RemoveListener(OpenOptionsPanel);
		}
		if (closeOptionsButton != null)
		{
			closeOptionsButton.onClick.RemoveListener(CloseOptionsPanel);
		}
		if (searchInput != null)
		{
			searchInput.onValueChanged.RemoveListener(OnSearchValueChanged);
		}
		if (searchTMPInput != null)
		{
			searchTMPInput.onValueChanged.RemoveListener(OnTMPSearchValueChanged);
		}
		if (panelSearchInput != null)
		{
			panelSearchInput.onValueChanged.RemoveListener(OnPanelSearchValueChanged);
		}
		if (panelSearchTMPInput != null)
		{
			panelSearchTMPInput.onValueChanged.RemoveListener(OnPanelTMPSearchValueChanged);
		}
    }
    
    // Public method to be called by the Advance Options button
    public void OnAdvanceOptionsClicked()
    {
        ShowAdvancedOptions();
    }
    
    // Public method to be called by a back button in advanced container
    public void OnBackToSimpleClicked()
    {
        ShowSimpleOptions();
    }
    
    private void ShowAdvancedOptions()
    {
        if (showingAdvancedOptions) return;
        
        showingAdvancedOptions = true;
        
        // Hide simple container and show advanced container
        if (simpleContainer != null)
        {
            simpleContainer.gameObject.SetActive(false);
        }
        
        if (advancedContainer != null)
        {
            advancedContainer.gameObject.SetActive(true);
        }
        
    }
    
    private void ShowSimpleOptions()
    {
        if (!showingAdvancedOptions) return;
        
        showingAdvancedOptions = false;
        
        // Hide advanced container and show simple container
        if (advancedContainer != null)
        {
            advancedContainer.gameObject.SetActive(false);
        }
        
        if (simpleContainer != null)
        {
            simpleContainer.gameObject.SetActive(true);
        }
        
    }
    
    private RectTransform GetCurrentActivePanel()
    {
        // Return the panel that corresponds to the currently active container
        if (showingAdvancedOptions && bottomPanel2 != null)
        {
            return bottomPanel2;
        }
        else if (bottomPanel != null)
        {
            return bottomPanel;
        }
        
        // Fallback to first panel
        return bottomPanel;
    }
    
    /// <summary>
    /// Updates the name label with the provided event name
    /// </summary>
    /// <param name="eventName">The name of the event to display</param>
    public void UpdateNameLabel(string eventName)
    {
        if (nameLabel != null)
        {
            nameLabel.text = string.IsNullOrEmpty(eventName) ? "No event selected" : eventName;
        }
    }
}