using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
