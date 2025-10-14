using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    public GameObject menuPanel;
    public Button burgerButton;
    public MenuType menuType = MenuType.SlidingPanel;
    
    [Header("UI References")]
    public EventsUIController eventsUIController;
    public EventsUIController eventsUIPrefab;
    
    [Header("Canvas Sorting")]
    public int menuPanelSortingOrder = 100;
    public int burgerButtonSortingOrder = 101;
    public bool useCanvasSorting = true;
    
    [Header("Hierarchy Control")]
    public bool useTransformHierarchy = true;
    
    [Header("Auto Close Settings")]
    public bool autoCloseOnItemClick = true;
    
    [Header("Burger Button Control")]
    public bool keepBurgerButtonOnTop = true;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
	[Header("Click Blocking")]
	public bool blockOutsideClicks = true;
	public bool dimBackgroundWhileOpen = false;
	[Range(0f, 0.95f)] public float dimAlpha = 0.4f;
	
    [Header("Sliding Panel Settings")]
    public SlideDirection slideDirection = SlideDirection.FromTop;
    public Vector2 slideOffset = new Vector2(0, 100);
    public float panelWidth = -1f; // -1 means use original width, otherwise override
    
    [Header("Dropdown Settings")]
    public float dropdownHeight = 200f;
    
    private bool isMenuOpen = false;
    private Coroutine currentAnimation;
    private Vector3 originalPosition;
    private Vector2 originalSize;
    private RectTransform menuRectTransform;
    private CanvasGroup menuCanvasGroup;
    private Canvas menuPanelCanvas;
    private Canvas burgerButtonCanvas;
	
	// Runtime-created overlay to block clicks outside the menu
	private GameObject clickBlockerOverlay;
	private UnityEngine.UI.Image clickBlockerImage;
    
    public enum MenuType
    {
        Dropdown,
        SlidingPanel
    }
    
    public enum SlideDirection
    {
        FromTop,
        FromBottom,
        FromLeft,
        FromRight
    }
    
    void Start()
    {
        // Get components
        menuRectTransform = menuPanel.GetComponent<RectTransform>();
        menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
        
        // Get or create Canvas components for proper sorting
        SetupCanvasSorting();
        
        // Store original values
        originalPosition = menuRectTransform.anchoredPosition;
        originalSize = menuRectTransform.sizeDelta;
        
        // Setup button listener
        if (burgerButton != null)
        {
            burgerButton.onClick.AddListener(ToggleMenuVisibility);
        }
        
        // Setup auto-close for menu items
        if (autoCloseOnItemClick)
        {
            SetupMenuItemsAutoClose();
        }
        
		// Prepare click blocker overlay (created on demand)
		CreateOrSetupClickBlockerOverlay();
		
		// Initially hide the menu
        SetMenuVisibility(false, false);
    }

	private void CreateOrSetupClickBlockerOverlay()
	{
		if (!blockOutsideClicks) return;
		if (menuPanel == null) return;
		if (clickBlockerOverlay != null && clickBlockerImage != null) return;

		// Create a full-screen overlay as a sibling of the menu panel so we can control layering
		clickBlockerOverlay = new GameObject("ClickBlockerOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
		var overlayRect = clickBlockerOverlay.GetComponent<RectTransform>();
		clickBlockerImage = clickBlockerOverlay.GetComponent<UnityEngine.UI.Image>();

		// Parent it beside the menu panel for reliable sibling ordering
		clickBlockerOverlay.transform.SetParent(menuPanel.transform.parent, false);

		// Stretch to full parent
		overlayRect.anchorMin = Vector2.zero;
		overlayRect.anchorMax = Vector2.one;
		overlayRect.offsetMin = Vector2.zero;
		overlayRect.offsetMax = Vector2.zero;

		// Transparent by default but must be a raycast target to block clicks
		clickBlockerImage.color = new Color(0f, 0f, 0f, 0f);
		clickBlockerImage.raycastTarget = true;

		// Place it directly beneath the menu panel, so the panel remains clickable
		PositionOverlayBelowMenuPanel();

		// Do not show initially
		clickBlockerOverlay.SetActive(false);
	}

	private void PositionOverlayBelowMenuPanel()
	{
		if (clickBlockerOverlay == null || menuPanel == null) return;
		// Ensure overlay exists in the same hierarchy and sits just below the menu panel
		int menuIndex = menuPanel.transform.GetSiblingIndex();
		clickBlockerOverlay.transform.SetSiblingIndex(Mathf.Max(0, menuIndex));
		menuPanel.transform.SetSiblingIndex(clickBlockerOverlay.transform.GetSiblingIndex() + 1);
	}

	private void ShowClickBlocker()
	{
		if (!blockOutsideClicks) return;
		CreateOrSetupClickBlockerOverlay();
		if (clickBlockerOverlay == null) return;

		// Optional dim
		if (dimBackgroundWhileOpen)
		{
			clickBlockerImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(dimAlpha));
		}
		else
		{
			clickBlockerImage.color = new Color(0f, 0f, 0f, 0f);
		}

		PositionOverlayBelowMenuPanel();

		// Keep burger button on top above both panel and overlay
		EnsureBurgerButtonOnTop();

		clickBlockerOverlay.SetActive(true);
	}

	private void HideClickBlocker()
	{
		if (clickBlockerOverlay == null) return;
		clickBlockerOverlay.SetActive(false);
	}
    
    private void SetupMenuItemsAutoClose()
    {
        if (menuPanel == null) return;
        
        // Find all buttons in the menu panel
        Button[] menuButtons = menuPanel.GetComponentsInChildren<Button>();
        
        foreach (Button button in menuButtons)
        {
            // Skip the burger button itself if it's inside the menu panel
            if (button == burgerButton) continue;
            
            // Add listener to close menu when clicked
            button.onClick.AddListener(() => OnMenuItemClicked(button));
        }
        
        Debug.Log($"Setup auto-close for {menuButtons.Length} menu items");
    }
    
    private void OnMenuItemClicked(Button clickedButton)
    {
        if (autoCloseOnItemClick && isMenuOpen)
        {
            Debug.Log($"Menu item '{clickedButton.name}' clicked. Hiding both menu panel and burger button.");
            
            // Check if this is the events menu item
            if (clickedButton.name.ToLower().Contains("event") || clickedButton.name.ToLower().Contains("calendar"))
            {
                OpenEventsFromMenu();
            }
            
            HideMenuAndBurgerButton();
        }
    }
    
    private void SetupCanvasSorting()
    {
        if (!useCanvasSorting) return;
        
        // Find the root Canvas
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
        }
        
        if (rootCanvas != null)
        {
            // Simply adjust the root canvas sorting order to be higher
            // This ensures the entire UI hierarchy appears on top
            rootCanvas.sortingOrder = Mathf.Max(rootCanvas.sortingOrder, menuPanelSortingOrder);
        }
        
        // Try to find existing Canvas components on the UI elements
        menuPanelCanvas = menuPanel.GetComponentInChildren<Canvas>();
        if (burgerButton != null)
        {
            burgerButtonCanvas = burgerButton.GetComponentInChildren<Canvas>();
        }
    }
    
    public void ToggleMenuVisibility()
    {
        isMenuOpen = !isMenuOpen;
        SetMenuVisibility(isMenuOpen, true);
    }
    
    public void OpenMenu()
    {
        if (!isMenuOpen)
        {
            isMenuOpen = true;
            SetMenuVisibility(true, true);
        }
    }
    
    public void CloseMenu()
    {
        if (isMenuOpen)
        {
            isMenuOpen = false;
            SetMenuVisibility(false, true);
        }
    }
    
    public void CloseMenuPanelOnly()
    {
        if (isMenuOpen)
        {
            isMenuOpen = false;
            SetMenuPanelVisibility(false, true);
        }
    }
    
    public void HideMenuAndBurgerButton()
    {
        if (isMenuOpen)
        {
            isMenuOpen = false;
            SetMenuPanelVisibility(false, true);
            HideBurgerButton();
        }
    }
    
    public void ShowMenuAndBurgerButton()
    {
        ShowBurgerButton();
        if (!isMenuOpen)
        {
            isMenuOpen = true;
            SetMenuPanelVisibility(true, true);
        }
    }
    
    private void HideBurgerButton()
    {
        if (burgerButton != null)
        {
            CanvasGroup burgerCanvasGroup = burgerButton.GetComponent<CanvasGroup>();
            if (burgerCanvasGroup == null)
            {
                burgerCanvasGroup = burgerButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            burgerCanvasGroup.alpha = 0f;
            burgerCanvasGroup.interactable = false;
            burgerCanvasGroup.blocksRaycasts = false;
            
            Debug.Log("Burger button hidden");
        }
    }
    
    private void ShowBurgerButton()
    {
        if (burgerButton != null)
        {
            CanvasGroup burgerCanvasGroup = burgerButton.GetComponent<CanvasGroup>();
            if (burgerCanvasGroup == null)
            {
                burgerCanvasGroup = burgerButton.gameObject.AddComponent<CanvasGroup>();
            }
            
            burgerCanvasGroup.alpha = 1f;
            burgerCanvasGroup.interactable = true;
            burgerCanvasGroup.blocksRaycasts = true;
            
            Debug.Log("Burger button shown");
        }
    }
    
    private void SetMenuVisibility(bool visible, bool animate)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Only ensure proper sorting when opening the menu
		if (visible)
        {
            EnsureMenuOnTop();
            // Hide events UI when menu opens
            HideEventsUI();
			ShowClickBlocker();
        }
		else
		{
			HideClickBlocker();
		}
        
        if (animate)
        {
            currentAnimation = StartCoroutine(AnimateMenu(visible));
        }
        else
        {
            SetMenuState(visible);
        }
    }
    
    private void SetMenuPanelVisibility(bool visible, bool animate)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
		// Only hide/show the menu panel, keep burger button visible
        if (animate)
        {
            currentAnimation = StartCoroutine(AnimateMenuPanel(visible));
        }
        else
        {
            SetMenuPanelState(visible);
        }

		// Manage click blocker with panel-only toggles too
		if (visible)
		{
			ShowClickBlocker();
		}
		else
		{
			HideClickBlocker();
		}
    }
    
    private void SetMenuState(bool visible)
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = visible ? 1f : 0f;
            menuCanvasGroup.interactable = visible;
            menuCanvasGroup.blocksRaycasts = visible;
        }
        
        if (menuType == MenuType.Dropdown)
        {
            SetDropdownState(visible);
        }
        else
        {
            SetSlidingPanelState(visible);
        }
    }
    
    private void SetMenuPanelState(bool visible)
    {
        // Only control the menu panel visibility, not the burger button
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = visible ? 1f : 0f;
            menuCanvasGroup.interactable = visible;
            menuCanvasGroup.blocksRaycasts = visible;
        }
        
        if (menuType == MenuType.Dropdown)
        {
            SetDropdownState(visible);
        }
        else
        {
            SetSlidingPanelState(visible);
        }
    }
    
    private void SetDropdownState(bool visible)
    {
        Vector2 targetSize = originalSize;
        targetSize.y = visible ? dropdownHeight : 0f;
        menuRectTransform.sizeDelta = targetSize;
    }
    
    private void SetSlidingPanelState(bool visible)
    {
        Vector3 targetPosition = originalPosition;
        Vector2 targetSize = originalSize;
        
        // Apply custom width if specified
        if (panelWidth > 0)
        {
            targetSize.x = panelWidth;
        }
        
        if (!visible)
        {
            switch (slideDirection)
            {
                case SlideDirection.FromTop:
                    targetPosition.y += slideOffset.y;
                    break;
                case SlideDirection.FromBottom:
                    targetPosition.y -= slideOffset.y;
                    break;
                case SlideDirection.FromLeft:
                    targetPosition.x -= slideOffset.x;
                    break;
                case SlideDirection.FromRight:
                    targetPosition.x += slideOffset.x;
                    break;
            }
        }
        
        menuRectTransform.anchoredPosition = targetPosition;
        menuRectTransform.sizeDelta = targetSize;
    }
    
    private IEnumerator AnimateMenu(bool visible)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = menuRectTransform.anchoredPosition;
        Vector2 startSize = menuRectTransform.sizeDelta;
        float startAlpha = menuCanvasGroup != null ? menuCanvasGroup.alpha : 1f;
        
        Vector3 targetPosition;
        Vector2 targetSize;
        float targetAlpha;
        
        if (visible)
        {
            targetPosition = originalPosition;
            targetSize = originalSize;
            targetAlpha = 1f;
            
            // Apply custom width if specified
            if (panelWidth > 0)
            {
                targetSize.x = panelWidth;
            }
            
            if (menuType == MenuType.Dropdown)
            {
                targetSize.y = dropdownHeight;
            }
            
            // Enable interaction immediately when opening
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            targetPosition = startPosition;
            targetSize = startSize;
            targetAlpha = 0f;
            
            if (menuType == MenuType.Dropdown)
            {
                targetSize.y = 0f;
            }
            else
            {
                switch (slideDirection)
                {
                    case SlideDirection.FromTop:
                        targetPosition.y += slideOffset.y;
                        break;
                    case SlideDirection.FromBottom:
                        targetPosition.y -= slideOffset.y;
                        break;
                    case SlideDirection.FromLeft:
                        targetPosition.x -= slideOffset.x;
                        break;
                    case SlideDirection.FromRight:
                        targetPosition.x += slideOffset.x;
                        break;
                }
            }
        }
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            // Animate position
            menuRectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // Animate size (for dropdown)
            if (menuType == MenuType.Dropdown)
            {
                menuRectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, curveValue);
            }
            
            // Animate alpha
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            }
            
            yield return null;
        }
        
        // Ensure final state
        SetMenuState(visible);
        
        // Disable interaction when closing
        if (!visible && menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }
        
        currentAnimation = null;
    }
    
    private IEnumerator AnimateMenuPanel(bool visible)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = menuRectTransform.anchoredPosition;
        Vector2 startSize = menuRectTransform.sizeDelta;
        float startAlpha = menuCanvasGroup != null ? menuCanvasGroup.alpha : 1f;
        
        Vector3 targetPosition;
        Vector2 targetSize;
        float targetAlpha;
        
        if (visible)
        {
            targetPosition = originalPosition;
            targetSize = originalSize;
            targetAlpha = 1f;
            
            // Apply custom width if specified
            if (panelWidth > 0)
            {
                targetSize.x = panelWidth;
            }
            
            if (menuType == MenuType.Dropdown)
            {
                targetSize.y = dropdownHeight;
            }
            
            // Enable interaction immediately when opening
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }
        }
        else
        {
            targetPosition = startPosition;
            targetSize = startSize;
            targetAlpha = 0f;
            
            if (menuType == MenuType.Dropdown)
            {
                targetSize.y = 0f;
            }
            else
            {
                switch (slideDirection)
                {
                    case SlideDirection.FromTop:
                        targetPosition.y += slideOffset.y;
                        break;
                    case SlideDirection.FromBottom:
                        targetPosition.y -= slideOffset.y;
                        break;
                    case SlideDirection.FromLeft:
                        targetPosition.x -= slideOffset.x;
                        break;
                    case SlideDirection.FromRight:
                        targetPosition.x += slideOffset.x;
                        break;
                }
            }
        }
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            // Animate position
            menuRectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            // Animate size (for dropdown)
            if (menuType == MenuType.Dropdown)
            {
                menuRectTransform.sizeDelta = Vector2.Lerp(startSize, targetSize, curveValue);
            }
            
            // Animate alpha
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            }
            
            yield return null;
        }
        
        // Ensure final state
        SetMenuPanelState(visible);
        
        // Disable interaction when closing
        if (!visible && menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }
        
        currentAnimation = null;
    }
    
    // Public methods for external control
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
    
    public void SetMenuType(MenuType newType)
    {
        menuType = newType;
        SetMenuState(isMenuOpen);
    }
    
    public void SetSlideDirection(SlideDirection newDirection)
    {
        slideDirection = newDirection;
        SetMenuState(isMenuOpen);
    }
    
    public void SetPanelWidth(float newWidth)
    {
        panelWidth = newWidth;
        SetMenuState(isMenuOpen);
        Debug.Log($"Panel width set to {newWidth}");
    }
    
    // Canvas sorting control methods
    public void SetMenuPanelSortingOrder(int sortingOrder)
    {
        menuPanelSortingOrder = sortingOrder;
        if (menuPanelCanvas != null)
        {
            menuPanelCanvas.sortingOrder = sortingOrder;
        }
    }
    
    public void SetBurgerButtonSortingOrder(int sortingOrder)
    {
        burgerButtonSortingOrder = sortingOrder;
        if (burgerButtonCanvas != null)
        {
            burgerButtonCanvas.sortingOrder = sortingOrder;
        }
    }
    
    public void EnsureMenuOnTop()
    {
        if (!useCanvasSorting) return;
        
        // Find the root Canvas and ensure it's on top
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = FindObjectOfType<Canvas>();
        }
        
        if (rootCanvas != null)
        {
            // Ensure the root canvas has a high sorting order
            rootCanvas.sortingOrder = Mathf.Max(rootCanvas.sortingOrder, menuPanelSortingOrder);
        }
        
        // Ensure proper layering: menu panel first, then burger button on top
        EnsureBurgerButtonOnTop();
    }
    
    public void EnsureBurgerButtonOnTop()
    {
        if (!keepBurgerButtonOnTop) return;
        
        // Use Transform hierarchy approach (more reliable)
        if (useTransformHierarchy)
        {
            EnsureBurgerButtonOnTopTransform();
        }
        
        // Also try Canvas sorting approach
        if (useCanvasSorting)
        {
            EnsureBurgerButtonOnTopCanvas();
        }
    }
    
    private void EnsureBurgerButtonOnTopTransform()
    {
        if (burgerButton != null && menuPanel != null)
        {
            // Move burger button to the end of its parent's children list
            // This makes it render on top of other UI elements
            burgerButton.transform.SetAsLastSibling();
            
            // Also ensure menu panel is rendered before burger button
            menuPanel.transform.SetSiblingIndex(menuPanel.transform.GetSiblingIndex());
            
            Debug.Log("Burger button moved to top using Transform hierarchy");
        }
    }
    
    private void EnsureBurgerButtonOnTopCanvas()
    {
        // First ensure menu panel has its sorting order
        if (menuPanelCanvas != null)
        {
            menuPanelCanvas.sortingOrder = menuPanelSortingOrder;
        }
        
        // Then ensure burger button is on top of menu panel
        if (burgerButtonCanvas != null)
        {
            burgerButtonCanvas.sortingOrder = burgerButtonSortingOrder;
        }
        
        // Also try to find Canvas components on the actual GameObjects
        Canvas menuCanvas = menuPanel.GetComponent<Canvas>();
        if (menuCanvas != null)
        {
            menuCanvas.sortingOrder = menuPanelSortingOrder;
        }
        
        if (burgerButton != null)
        {
            Canvas buttonCanvas = burgerButton.GetComponent<Canvas>();
            if (buttonCanvas != null)
            {
                buttonCanvas.sortingOrder = burgerButtonSortingOrder;
            }
        }
        
        // Ensure burger button sorting order is always higher than menu panel
        if (burgerButtonSortingOrder <= menuPanelSortingOrder)
        {
            burgerButtonSortingOrder = menuPanelSortingOrder + 1;
            Debug.Log($"Adjusted burger button sorting order to {burgerButtonSortingOrder} to ensure it's above menu panel");
        }
    }
    
    // Emergency method to disable canvas sorting if it causes issues
    public void DisableCanvasSorting()
    {
        useCanvasSorting = false;
        Debug.Log("Canvas sorting disabled. Menu should now be visible.");
    }
    
    // Emergency method to disable transform hierarchy if it causes issues
    public void DisableTransformHierarchy()
    {
        useTransformHierarchy = false;
        Debug.Log("Transform hierarchy disabled.");
    }
    
    // Public method to force burger button on top (can be called from other scripts)
    public void ForceBurgerButtonOnTop()
    {
        EnsureBurgerButtonOnTop();
        Debug.Log("Burger button forced to top using all available methods");
    }
    
    // Simple method that just uses Transform.SetAsLastSibling (most reliable)
    public void MoveBurgerButtonToTop()
    {
        if (burgerButton != null)
        {
            burgerButton.transform.SetAsLastSibling();
            Debug.Log("Burger button moved to top using SetAsLastSibling");
        }
    }
    
    // Public methods for external control
    public void EnableAutoCloseOnItemClick()
    {
        autoCloseOnItemClick = true;
        SetupMenuItemsAutoClose();
        Debug.Log("Auto-close on item click enabled");
    }
    
    public void DisableAutoCloseOnItemClick()
    {
        autoCloseOnItemClick = false;
        Debug.Log("Auto-close on item click disabled");
    }
    
    // Method to manually close menu (can be called from other scripts)
    public void CloseMenuFromExternal()
    {
        if (isMenuOpen)
        {
            CloseMenu();
            Debug.Log("Menu closed from external call");
        }
    }
    
    // Method to refresh menu item listeners (useful if menu items are added dynamically)
    public void RefreshMenuItems()
    {
        if (autoCloseOnItemClick)
        {
            SetupMenuItemsAutoClose();
        }
    }
    
    // Method to disable burger button sorting (if it's causing issues)
    public void DisableBurgerButtonSorting()
    {
        keepBurgerButtonOnTop = false;
        Debug.Log("Burger button sorting disabled. Button should remain visible.");
    }
    
    // Method to enable burger button sorting
    public void EnableBurgerButtonSorting()
    {
        keepBurgerButtonOnTop = true;
        Debug.Log("Burger button sorting enabled.");
    }
    
    // Public methods for external control of menu and burger button visibility
    public void HideBothMenuAndBurger()
    {
        HideMenuAndBurgerButton();
        Debug.Log("Both menu and burger button hidden from external call");
    }
    
    public void ShowBothMenuAndBurger()
    {
        ShowMenuAndBurgerButton();
        Debug.Log("Both menu and burger button shown from external call");
    }
    
    public void HideOnlyBurgerButton()
    {
        HideBurgerButton();
        Debug.Log("Only burger button hidden from external call");
    }
    
    public void ShowOnlyBurgerButton()
    {
        ShowBurgerButton();
        Debug.Log("Only burger button shown from external call");
    }
    
    private void HideEventsUI()
    {
        if (eventsUIController != null)
        {
            eventsUIController.HideEventsUI();
        }
        else
        {
            // Fallback: try to find it automatically
            EventsUIController eventsUI = FindObjectOfType<EventsUIController>();
            if (eventsUI != null)
            {
                eventsUI.HideEventsUI();
            }
        }
    }
    
    private void ShowEventsUI()
    {
        EnsureEventsUI();
        if (eventsUIController == null) return;
        // Prefer a stronger refresh if available
        var forceRefreshMethod = eventsUIController.GetType().GetMethod("ForceRefreshEventsUI");
        if (forceRefreshMethod != null)
        {
            forceRefreshMethod.Invoke(eventsUIController, null);
        }
        else
        {
            eventsUIController.ShowEventsUI();
        }
    }

    public void OpenEventsFromMenu()
    {
        EnsureEventsUI();
        if (eventsUIController == null)
        {
            Debug.LogWarning("Cannot open events - EventsUIController missing");
            return;
        }
        eventsUIController.ShowEventsUI();
        // Close menu but keep burger hidden per your flow
        HideMenuAndBurgerButton();
    }

    private void EnsureEventsUI()
    {
        if (eventsUIController != null) return;
        // Try find existing instance first (even inactive)
#if UNITY_2022_2_OR_NEWER
        eventsUIController = FindFirstObjectByType<EventsUIController>(FindObjectsInactive.Include);
#else
        eventsUIController = FindObjectOfType<EventsUIController>(true);
#endif
        if (eventsUIController != null) return;

        // Instantiate from prefab if provided
        if (eventsUIPrefab != null)
        {
            var instance = Instantiate(eventsUIPrefab);
            eventsUIController = instance;
            Debug.Log("Instantiated EventsUIController from prefab");
        }
        else
        {
            Debug.LogWarning("No EventsUIController instance found and no prefab assigned. Assign 'eventsUIPrefab' in ToggleMenu.");
        }
    }
}