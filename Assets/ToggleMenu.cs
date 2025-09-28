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
    
    [Header("Animation Settings")]
    public float animationDuration = 0.3f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Sliding Panel Settings")]
    public SlideDirection slideDirection = SlideDirection.FromTop;
    public Vector2 slideOffset = new Vector2(0, 100);
    
    [Header("Dropdown Settings")]
    public float dropdownHeight = 200f;
    
    private bool isMenuOpen = false;
    private Coroutine currentAnimation;
    private Vector3 originalPosition;
    private Vector2 originalSize;
    private RectTransform menuRectTransform;
    private CanvasGroup menuCanvasGroup;
    
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
        
        // Store original values
        originalPosition = menuRectTransform.anchoredPosition;
        originalSize = menuRectTransform.sizeDelta;
        
        // Setup button listener
        if (burgerButton != null)
        {
            burgerButton.onClick.AddListener(ToggleMenuVisibility);
        }
        
        // Initially hide the menu
        SetMenuVisibility(false, false);
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
    
    private void SetMenuVisibility(bool visible, bool animate)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
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
    
    private void SetDropdownState(bool visible)
    {
        Vector2 targetSize = originalSize;
        targetSize.y = visible ? dropdownHeight : 0f;
        menuRectTransform.sizeDelta = targetSize;
    }
    
    private void SetSlidingPanelState(bool visible)
    {
        Vector3 targetPosition = originalPosition;
        
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
}
