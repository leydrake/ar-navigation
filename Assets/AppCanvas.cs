using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

	[Header("Popup Option Panel")]
	public GameObject optionsPanel; // Panel to show/hide
	public Button openOptionsButton; // Button to open panel
	public Button closeOptionsButton; // Button to close panel
	public RectTransform optionsContainer; // Parent container for option items
	public GameObject optionItemPrefab; // Prefab expected to have a Button and a Text/TMP_Text
	
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
            
            Debug.Log($"Click detected at: {inputPos}, Local point: {localPoint}, Handle rect: {dragHandle.rect}");
            
            if (dragHandle.rect.Contains(localPoint))
            {
                Debug.Log("Click is within first drag handle!");
                TogglePanelState(bottomPanel);
                return;
            }
        }
        
        // Check second drag handle
        if (dragHandle2 != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragHandle2, inputPos, null, out Vector2 localPoint);
            
            Debug.Log($"Click detected at: {inputPos}, Local point: {localPoint}, Handle2 rect: {dragHandle2.rect}");
            
            if (dragHandle2.rect.Contains(localPoint))
            {
                Debug.Log("Click is within second drag handle!");
                TogglePanelState(bottomPanel2);
                return;
            }
        }
        
        Debug.Log("Click is NOT within any drag handle");
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
            Debug.Log("Expanding to original position");
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
            Debug.Log("Collapsing panel");
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
        
        Debug.Log($"Drag ended. Delta: {dragDelta}, Threshold: 10f");
        
        // Check if it's a click (no movement) or a drag
        if (Mathf.Abs(dragDelta) < 10f) // Small threshold for click detection
        {
            Debug.Log("Treating as click");
            // It's a click - toggle the panel
            HandleClick(inputPos);
        }
        else
        {
            Debug.Log("Treating as drag");
            // It's a drag - use drag logic
            Debug.Log($"Current state - isCollapsed: {isCollapsed}");
            
            if (dragDelta < 0) // Dragged down (negative delta)
            {
                Debug.Log("Dragging down - collapsing");
                
                if (useCollider)
                {
                    // Check if panel is already at collider limit
                    float currentY = bottomPanel.anchoredPosition.y;
                    float colliderLimit = originalYPosition - colliderHeight;
                    
                    if (currentY <= colliderLimit)
                    {
                        Debug.Log("Hit collider limit - can't collapse further");
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
                Debug.Log("Dragging up - expanding");
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
        
        Debug.Log("Switched to Advanced Options");
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
        
        Debug.Log("Switched to Simple Options");
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
}
