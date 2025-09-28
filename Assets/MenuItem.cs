using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class MenuItem : MonoBehaviour
{
    [Header("Menu Item Settings")]
    public string itemName;
    public Sprite itemIcon;
    public string itemDescription;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public float hoverScale = 1.05f;
    public float pressScale = 0.95f;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Events")]
    public UnityEvent OnItemClicked;
    public UnityEvent OnItemHover;
    public UnityEvent OnItemHoverExit;
    
    private Button itemButton;
    private Image itemImage;
    private Text itemText;
    private Image itemIconImage;
    private Vector3 originalScale;
    private Coroutine currentAnimation;
    private bool isHovered = false;
    private bool isPressed = false;
    
    void Start()
    {
        // Get components
        itemButton = GetComponent<Button>();
        itemImage = GetComponent<Image>();
        itemText = GetComponentInChildren<Text>();
        itemIconImage = transform.Find("Icon")?.GetComponent<Image>();
        
        // Store original scale
        originalScale = transform.localScale;
        
        // Setup button events
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnClick);
        }
        
        // Setup UI
        SetupMenuItem();
    }
    
    private void SetupMenuItem()
    {
        // Set item name
        if (itemText != null && !string.IsNullOrEmpty(itemName))
        {
            itemText.text = itemName;
        }
        
        // Set item icon
        if (itemIconImage != null && itemIcon != null)
        {
            itemIconImage.sprite = itemIcon;
            itemIconImage.gameObject.SetActive(true);
        }
        else if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(false);
        }
        
        // Set initial color
        if (itemImage != null)
        {
            itemImage.color = normalColor;
        }
    }
    
    public void OnClick()
    {
        if (OnItemClicked != null)
        {
            OnItemClicked.Invoke();
        }
        
        // Add click animation
        StartCoroutine(ClickAnimation());
    }
    
    public void OnPointerEnter()
    {
        if (!isHovered)
        {
            isHovered = true;
            
            if (OnItemHover != null)
            {
                OnItemHover.Invoke();
            }
            
            // Start hover animation
            AnimateScale(hoverScale);
            AnimateColor(hoverColor);
        }
    }
    
    public void OnPointerExit()
    {
        if (isHovered)
        {
            isHovered = false;
            
            if (OnItemHoverExit != null)
            {
                OnItemHoverExit.Invoke();
            }
            
            // Return to normal state
            AnimateScale(1f);
            AnimateColor(normalColor);
        }
    }
    
    public void OnPointerDown()
    {
        if (!isPressed)
        {
            isPressed = true;
            AnimateScale(pressScale);
            AnimateColor(pressedColor);
        }
    }
    
    public void OnPointerUp()
    {
        if (isPressed)
        {
            isPressed = false;
            
            if (isHovered)
            {
                AnimateScale(hoverScale);
                AnimateColor(hoverColor);
            }
            else
            {
                AnimateScale(1f);
                AnimateColor(normalColor);
            }
        }
    }
    
    private void AnimateScale(float targetScale)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(ScaleAnimation(targetScale));
    }
    
    private void AnimateColor(Color targetColor)
    {
        if (itemImage != null)
        {
            StartCoroutine(ColorAnimation(targetColor));
        }
    }
    
    private IEnumerator ScaleAnimation(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = scaleCurve.Evaluate(progress);
            
            transform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            yield return null;
        }
        
        transform.localScale = endScale;
        currentAnimation = null;
    }
    
    private IEnumerator ColorAnimation(Color targetColor)
    {
        Color startColor = itemImage.color;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            itemImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        itemImage.color = targetColor;
    }
    
    private IEnumerator ClickAnimation()
    {
        // Quick press and release animation
        Vector3 originalScale = transform.localScale;
        Vector3 pressScale = originalScale * this.pressScale;
        
        // Press down
        float pressTime = animationDuration * 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < pressTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / pressTime;
            transform.localScale = Vector3.Lerp(originalScale, pressScale, progress);
            yield return null;
        }
        
        // Release
        elapsedTime = 0f;
        while (elapsedTime < pressTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / pressTime;
            transform.localScale = Vector3.Lerp(pressScale, originalScale, progress);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    // Public methods for external control
    public void SetItemName(string newName)
    {
        itemName = newName;
        if (itemText != null)
        {
            itemText.text = itemName;
        }
    }
    
    public void SetItemIcon(Sprite newIcon)
    {
        itemIcon = newIcon;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = itemIcon;
            itemIconImage.gameObject.SetActive(itemIcon != null);
        }
    }
    
    public void SetItemDescription(string newDescription)
    {
        itemDescription = newDescription;
    }
    
    public void SetInteractable(bool interactable)
    {
        if (itemButton != null)
        {
            itemButton.interactable = interactable;
        }
        
        // Update visual state
        Color targetColor = interactable ? normalColor : Color.gray;
        if (itemImage != null)
        {
            itemImage.color = targetColor;
        }
    }
    
    public void SetColors(Color normal, Color hover, Color pressed)
    {
        normalColor = normal;
        hoverColor = hover;
        pressedColor = pressed;
        
        if (itemImage != null && !isHovered && !isPressed)
        {
            itemImage.color = normalColor;
        }
    }
}
