using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppCanvas : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform bottomPanel;
    public RectTransform dragHandle;
    
    [Header("Collider Settings")]
    public bool useCollider = true;
    public float colliderHeight = 50f; // Height of the collider area
    
    [Header("Panel States")]
    public float originalYPosition = 0f;    // Original position (where it starts)
    public float collapsedYPosition = -550f; // Position when collapsed
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private bool isCollapsed = false;
    private bool isDragging = false;
    private bool isAnimating = false;
    private Vector2 dragStartPosition;
    private float dragStartYPosition;
    private Coroutine currentAnimation;
    
    void Start()
    {
        // Initialize the panel - preserve your manual Y position as the original
        if (bottomPanel != null)
        {
            // Use the current Y position as the original position
            originalYPosition = bottomPanel.anchoredPosition.y;
        }
    }
    
    void Update()
    {
        HandleDragInput();
    }
    
    private void HandleDragInput()
    {
        if (dragHandle == null || bottomPanel == null) return;
        
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragHandle, inputPos, null, out Vector2 localPoint);
        
        if (dragHandle.rect.Contains(localPoint))
        {
            isDragging = true;
            dragStartPosition = inputPos;
            dragStartYPosition = bottomPanel.anchoredPosition.y;
        }
    }
    
    private void HandleClick(Vector2 inputPos)
    {
        if (dragHandle == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragHandle, inputPos, null, out Vector2 localPoint);
        
        Debug.Log($"Click detected at: {inputPos}, Local point: {localPoint}, Handle rect: {dragHandle.rect}");
        
        if (dragHandle.rect.Contains(localPoint))
        {
            Debug.Log("Click is within drag handle!");
            
            // Toggle between collapsed and original position
            if (isCollapsed)
            {
                Debug.Log("Expanding to original position");
                // Expand to original position with animation
                isCollapsed = false;
                AnimateToPosition(originalYPosition);
            }
            else
            {
                Debug.Log("Collapsing to -550");
                // Collapse to -550 with animation
                isCollapsed = true;
                AnimateToPosition(collapsedYPosition);
            }
        }
        else
        {
            Debug.Log("Click is NOT within drag handle");
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
                isCollapsed = true;
                AnimateToPosition(collapsedYPosition);
            }
            else if (dragDelta > 0) // Dragged up (positive delta)
            {
                Debug.Log("Dragging up - expanding");
                // Always expand when dragging up
                isCollapsed = false;
                AnimateToPosition(originalYPosition);
            }
        }
    }
    
    private void AnimateToPosition(float targetYPosition)
    {
        if (isAnimating) return;
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(AnimatePanel(targetYPosition));
    }
    
    private IEnumerator AnimatePanel(float targetYPosition)
    {
        isAnimating = true;
        
        Vector2 startPosition = bottomPanel.anchoredPosition;
        Vector2 endPosition = new Vector2(startPosition.x, targetYPosition);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            bottomPanel.anchoredPosition = Vector2.Lerp(startPosition, endPosition, curveValue);
            
            yield return null;
        }
        
        bottomPanel.anchoredPosition = endPosition;
        isAnimating = false;
        currentAnimation = null;
    }
}
