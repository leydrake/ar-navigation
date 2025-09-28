using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// This script provides examples and setup instructions for the menu system.
/// It demonstrates how to create both dropdown and sliding panel menus.
/// </summary>
public class MenuSetupGuide : MonoBehaviour
{
    [Header("Example Menu Items")]
    public List<MenuExampleItem> exampleItems = new List<MenuExampleItem>();
    
    [System.Serializable]
    public class MenuExampleItem
    {
        public string itemName;
        public Sprite itemIcon;
        public string description;
        public System.Action onClickAction;
    }
    
    void Start()
    {
        // Example of how to setup menu items programmatically
        SetupExampleMenuItems();
    }
    
    private void SetupExampleMenuItems()
    {
        // Add some example menu items
        exampleItems.Add(new MenuExampleItem
        {
            itemName = "Home",
            description = "Go to home screen",
            onClickAction = () => Debug.Log("Home clicked!")
        });
        
        exampleItems.Add(new MenuExampleItem
        {
            itemName = "Settings",
            description = "Open settings menu",
            onClickAction = () => Debug.Log("Settings clicked!")
        });
        
        exampleItems.Add(new MenuExampleItem
        {
            itemName = "Profile",
            description = "View user profile",
            onClickAction = () => Debug.Log("Profile clicked!")
        });
        
        exampleItems.Add(new MenuExampleItem
        {
            itemName = "Help",
            description = "Get help and support",
            onClickAction = () => Debug.Log("Help clicked!")
        });
    }
    
    /// <summary>
    /// Example method showing how to create a menu item programmatically
    /// </summary>
    public GameObject CreateMenuItem(GameObject parent, string itemName, Sprite icon, System.Action onClickAction)
    {
        // Create the menu item GameObject
        GameObject menuItem = new GameObject(itemName + "Item");
        menuItem.transform.SetParent(parent.transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = menuItem.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        // Add Image component for background
        Image backgroundImage = menuItem.AddComponent<Image>();
        backgroundImage.color = Color.white;
        
        // Add Button component
        Button button = menuItem.AddComponent<Button>();
        
        // Add MenuItem script
        MenuItem menuItemScript = menuItem.AddComponent<MenuItem>();
        menuItemScript.itemName = itemName;
        menuItemScript.itemIcon = icon;
        
        // Setup button click event
        button.onClick.AddListener(() => onClickAction?.Invoke());
        
        // Create text for item name
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(menuItem.transform, false);
        
        Text text = textObject.AddComponent<Text>();
        text.text = itemName;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Create icon if provided
        if (icon != null)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(menuItem.transform, false);
            
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.sizeDelta = new Vector2(30, 30);
            iconRect.anchoredPosition = new Vector2(25, 0);
            
            // Adjust text position to make room for icon
            textRect.offsetMin = new Vector2(50, 0);
        }
        
        return menuItem;
    }
    
    /// <summary>
    /// Example method showing how to create a complete menu system
    /// </summary>
    public void CreateCompleteMenuSystem()
    {
        // This is an example of how you would set up the complete menu system in code
        // In practice, you would typically set this up in the Unity Editor
        
        // 1. Create the main menu container
        GameObject menuContainer = new GameObject("MenuContainer");
        Canvas canvas = menuContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = menuContainer.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        GraphicRaycaster raycaster = menuContainer.AddComponent<GraphicRaycaster>();
        
        // 2. Create the burger button
        GameObject burgerButton = new GameObject("BurgerButton");
        burgerButton.transform.SetParent(menuContainer.transform, false);
        
        RectTransform burgerRect = burgerButton.AddComponent<RectTransform>();
        burgerRect.anchorMin = new Vector2(0, 1);
        burgerRect.anchorMax = new Vector2(0, 1);
        burgerRect.sizeDelta = new Vector2(60, 60);
        burgerRect.anchoredPosition = new Vector2(30, -30);
        
        Image burgerImage = burgerButton.AddComponent<Image>();
        burgerImage.color = Color.blue;
        
        Button burgerButtonComponent = burgerButton.AddComponent<Button>();
        
        // 3. Create the menu panel
        GameObject menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(menuContainer.transform, false);
        
        RectTransform menuRect = menuPanel.AddComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0, 1);
        menuRect.anchorMax = new Vector2(0, 1);
        menuRect.sizeDelta = new Vector2(250, 300);
        menuRect.anchoredPosition = new Vector2(125, -150);
        
        Image menuImage = menuPanel.AddComponent<Image>();
        menuImage.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);
        
        CanvasGroup menuCanvasGroup = menuPanel.AddComponent<CanvasGroup>();
        
        // 4. Add the ToggleMenu script
        ToggleMenu toggleMenu = menuContainer.AddComponent<ToggleMenu>();
        toggleMenu.menuPanel = menuPanel;
        toggleMenu.burgerButton = burgerButtonComponent;
        toggleMenu.menuType = ToggleMenu.MenuType.SlidingPanel;
        toggleMenu.slideDirection = ToggleMenu.SlideDirection.FromTop;
        
        // 5. Create menu items
        for (int i = 0; i < exampleItems.Count; i++)
        {
            GameObject item = CreateMenuItem(menuPanel, exampleItems[i].itemName, exampleItems[i].itemIcon, exampleItems[i].onClickAction);
            
            // Position the item
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.sizeDelta = new Vector2(0, 50);
            itemRect.anchoredPosition = new Vector2(0, -25 - (i * 55));
        }
    }
}
