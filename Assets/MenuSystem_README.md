# Unity Menu System - Dropdown & Sliding Panel

A comprehensive Unity menu system that provides both dropdown and sliding panel menu functionality with smooth animations and easy customization.

## Features

- **Two Menu Types**: Dropdown and Sliding Panel
- **Smooth Animations**: Customizable animation curves and durations
- **Multiple Slide Directions**: Top, Bottom, Left, Right
- **Interactive Menu Items**: Hover effects, click animations, and events
- **Easy Setup**: Simple drag-and-drop configuration
- **Extensible**: Easy to add new menu types and animations

## Scripts Included

1. **ToggleMenu.cs** - Main menu controller
2. **MenuItem.cs** - Individual menu item behavior
3. **MenuAnimation.cs** - Animation utilities
4. **MenuSetupGuide.cs** - Setup examples and helper methods

## Quick Setup Guide

### Method 1: Using Unity Editor (Recommended)

1. **Create the UI Canvas**:
   - Right-click in Hierarchy → UI → Canvas
   - Set Canvas Scaler to "Scale With Screen Size"
   - Set Reference Resolution to your target resolution

2. **Create the Burger Button**:
   - Right-click on Canvas → UI → Button
   - Rename to "BurgerButton"
   - Position it where you want (usually top-left)
   - Add an Image component and set the color/style

3. **Create the Menu Panel**:
   - Right-click on Canvas → UI → Panel
   - Rename to "MenuPanel"
   - Add a CanvasGroup component
   - Position it where you want the menu to appear
   - Set the initial size (e.g., 250x300 for sliding panel)

4. **Add the ToggleMenu Script**:
   - Create an empty GameObject and name it "MenuController"
   - Add the ToggleMenu script to it
   - Drag the BurgerButton to the "Burger Button" field
   - Drag the MenuPanel to the "Menu Panel" field
   - Configure the settings in the inspector

5. **Create Menu Items**:
   - Create UI Buttons as children of the MenuPanel
   - Add the MenuItem script to each button
   - Configure the item properties (name, icon, colors, etc.)

### Method 2: Programmatic Setup

Use the `MenuSetupGuide.cs` script to create menus programmatically:

```csharp
// Create a complete menu system
MenuSetupGuide guide = FindObjectOfType<MenuSetupGuide>();
guide.CreateCompleteMenuSystem();
```

## Configuration Options

### ToggleMenu Settings

- **Menu Type**: Choose between Dropdown or SlidingPanel
- **Animation Duration**: How long animations take (default: 0.3s)
- **Animation Curve**: Customize the animation easing
- **Slide Direction**: For sliding panels (Top, Bottom, Left, Right)
- **Slide Offset**: How far the panel slides from its position
- **Dropdown Height**: Height of the dropdown when open

### MenuItem Settings

- **Item Name**: Text displayed on the menu item
- **Item Icon**: Optional sprite icon
- **Item Description**: Tooltip or description text
- **Colors**: Normal, Hover, and Pressed colors
- **Animation**: Hover scale, press scale, and animation duration
- **Events**: OnClick, OnHover, OnHoverExit events

## Usage Examples

### Basic Menu Toggle

```csharp
ToggleMenu menuController = GetComponent<ToggleMenu>();

// Open the menu
menuController.OpenMenu();

// Close the menu
menuController.CloseMenu();

// Toggle the menu
menuController.ToggleMenuVisibility();

// Check if menu is open
bool isOpen = menuController.IsMenuOpen();
```

### Menu Item Events

```csharp
MenuItem menuItem = GetComponent<MenuItem>();

// Add click event
menuItem.OnItemClicked.AddListener(() => {
    });

// Add hover events
menuItem.OnItemHover.AddListener(() => {
    });

menuItem.OnItemHoverExit.AddListener(() => {
    });
```

### Dynamic Menu Creation

```csharp
MenuSetupGuide guide = GetComponent<MenuSetupGuide>();

// Create a menu item programmatically
GameObject newItem = guide.CreateMenuItem(
    menuPanel, 
    "New Item", 
    someIcon, 
    () => Debug.Log("New item clicked!")
);
```

## Animation Customization

### Using MenuAnimation Script

```csharp
MenuAnimation animator = GetComponent<MenuAnimation>();

// Fade animations
animator.FadeIn(canvasGroup, 0.5f);
animator.FadeOut(canvasGroup, 0.3f);

// Scale animations
animator.ScaleIn(transform, 0.4f);
animator.ScaleOut(transform, 0.2f);

// Staggered animations for children
animator.AnimateChildrenIn(parentTransform, 0.3f);
animator.AnimateChildrenOut(parentTransform, 0.2f);

// Bounce animations
animator.BounceIn(transform, 0.5f);
animator.BounceOut(transform, 0.3f);
```

## Advanced Features

### Custom Animation Curves

You can create custom animation curves in the Unity Editor or programmatically:

```csharp
AnimationCurve customCurve = new AnimationCurve();
customCurve.AddKey(0, 0);
customCurve.AddKey(0.5f, 1.2f); // Overshoot
customCurve.AddKey(1, 1);

menuController.animationCurve = customCurve;
```

### Menu Type Switching

```csharp
// Switch between menu types at runtime
menuController.SetMenuType(ToggleMenu.MenuType.Dropdown);
menuController.SetMenuType(ToggleMenu.MenuType.SlidingPanel);

// Change slide direction
menuController.SetSlideDirection(ToggleMenu.SlideDirection.FromRight);
```

### Menu Item Customization

```csharp
MenuItem item = GetComponent<MenuItem>();

// Change item properties
item.SetItemName("New Name");
item.SetItemIcon(newIcon);
item.SetItemDescription("New description");

// Change colors
item.SetColors(Color.white, Color.yellow, Color.red);

// Enable/disable interaction
item.SetInteractable(false);
```

## Troubleshooting

### Common Issues

1. **Menu doesn't appear**: Check that the MenuPanel has a CanvasGroup component
2. **Animations are choppy**: Reduce the animation duration or check frame rate
3. **Menu items don't respond**: Ensure the MenuItem script is attached and the Button component is properly configured
4. **Menu appears in wrong position**: Check the RectTransform anchors and positions

### Performance Tips

1. Use object pooling for frequently created/destroyed menu items
2. Disable unnecessary UI elements when not in use
3. Use Canvas Groups to batch UI updates
4. Consider using UI optimization tools like UI Profiler

## Extending the System

### Adding New Menu Types

1. Add new enum values to `MenuType` in ToggleMenu.cs
2. Implement the new menu behavior in `SetMenuState()` method
3. Add animation logic in `AnimateMenu()` method

### Adding New Animations

1. Add new methods to MenuAnimation.cs
2. Use the existing animation patterns as templates
3. Consider performance implications of complex animations

## License

This menu system is provided as-is for educational and commercial use. Feel free to modify and distribute according to your needs.

## Support

For questions or issues, please refer to the Unity documentation or create an issue in your project repository.
