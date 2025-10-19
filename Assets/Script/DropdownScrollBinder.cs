using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DropdownScrollBinder : MonoBehaviour
{
	[Header("Source")]
	public Dropdown dropdown; // uGUI Dropdown (optional)
	public TMP_Dropdown tmpDropdown; // TMP Dropdown (optional)

	[Header("List Target")]
	public ScrollRect scrollRect;
	public RectTransform content;
	public GameObject rowPrefab; // Prefab with Image+Button on root and Text child

	[Header("Selected Label Target")]
	public TMP_Text selectedLabel; // Optional: show currently selected option text

	[Header("Style")]
	public Color normalColor = new Color(0.94f, 0.94f, 0.94f, 1f);
	public Color selectedColor = new Color(0.8f, 0.9f, 1f, 1f);

	[Header("Behavior")]
	public bool rebuildOnEnable = true;
	public bool delayOneFrameBeforeBuild = true;
	public bool verboseLogging = false;

	[Header("Data Source")]
	public bool useLocationData = false; // If true, use LocationData instead of EventData
	public LocationData[] locationDataArray; // Array of location data

	[Header("Default Image")]
	public Sprite defaultLocationSprite; // Default image for locations without image data

	private readonly List<GameObject> spawned = new List<GameObject>();

[SerializeField]
private AppCanvas appCanvas;

[SerializeField]
private EventsFetcher eventsFetcher; // Reference to get EventData
	void Awake()
	{
		if ((dropdown == null && tmpDropdown == null) || scrollRect == null || content == null || rowPrefab == null)
		{
			return;
		}

		if (!rebuildOnEnable)
		{
			RebuildFromDropdown();
		}
		SubscribeValueChanged();
	}

	void OnEnable()
	{
		if ((dropdown == null && tmpDropdown == null) || scrollRect == null || content == null || rowPrefab == null)
		{
			return;
		}
		if (rebuildOnEnable)
		{
			if (delayOneFrameBeforeBuild)
			{
				StartCoroutine(RebuildNextFrame());
			}
			else
			{
				RebuildFromDropdown();
			}
		}
	}

	void OnDisable()
	{
		UnsubscribeValueChanged();
	}

	public void RebuildFromDropdown()
	{
		

		for (int i = 0; i < spawned.Count; i++)
		{
			if (spawned[i] != null) Destroy(spawned[i]);
		}
		spawned.Clear();

		int count = GetOptionsCount();
		if (count <= 0)
		{
			if (verboseLogging)
			{
			}
			return;
		}

		for (int i = 0; i < count; i++)
		{
			var go = Instantiate(rowPrefab, content);
			spawned.Add(go);

			var btn = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
			var txt = go.GetComponentInChildren<Text>();
			var tmpTxt = go.GetComponentInChildren<TMP_Text>();
			string optionText = GetOptionText(i);
			if (txt != null) txt.text = optionText;
			if (tmpTxt != null) tmpTxt.text = optionText;

			// Set image for destination if using location data
			if (useLocationData && locationDataArray != null && i >= 0 && i < locationDataArray.Length)
			{
				var imageForDestination = FindImageForDestination(go);
				if (imageForDestination != null)
				{
					Sprite sprite = null;
					
					if (!string.IsNullOrEmpty(locationDataArray[i].Image))
					{
						var texture = Base64ToTexture2D(locationDataArray[i].Image);
						
						if (texture != null)
						{
							sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
							
							if (verboseLogging)
							{
							}
						}
						else
						{
						}
					}
					
					// Use default sprite if no custom image or conversion failed
					if (sprite == null)
					{
						if (defaultLocationSprite != null)
						{
							sprite = defaultLocationSprite;
							
							if (verboseLogging)
							{
							}
						}
						else
						{
							// Fallback to test sprite if no default sprite is assigned
							sprite = CreateTestSprite();
						}
					}
					
					if (sprite != null)
					{
						imageForDestination.sprite = sprite;
						
						// Force the image to be visible
						imageForDestination.color = Color.white;
						imageForDestination.enabled = true;
						
						// Check and fix RectTransform
						var rectTransform = imageForDestination.GetComponent<RectTransform>();
						if (rectTransform != null)
						{
							// Set a minimum size if it's too small
							if (rectTransform.sizeDelta.x < 10 || rectTransform.sizeDelta.y < 10)
							{
								rectTransform.sizeDelta = new Vector2(100, 100);
							}
						}
						
						
					}
				}
				
			}

			int idx = i;
			if (btn == null)
			{
				// Ensure the root can receive clicks
				var imgForButton = go.GetComponent<Image>();
				if (imgForButton == null)
				{
					imgForButton = go.AddComponent<Image>();
					imgForButton.color = normalColor;
				}
				imgForButton.raycastTarget = true;
				btn = go.AddComponent<Button>();
				btn.targetGraphic = imgForButton;
			}
			btn.onClick.AddListener(() => OnRowClicked(idx));
			

			var img = go.GetComponent<Image>();
			if (img != null) img.color = normalColor;
		}

		HighlightSelected();
		UpdateSelectedLabel();
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
	}

	private void OnRowClicked(int index)
	{
		
		SetSelectedIndex(index);
		UpdateSelectedLabel();

		appCanvas.CloseOptionsPanel();
	}

	private void HighlightSelected()
	{
		int selected = GetSelectedIndex();
		for (int i = 0; i < spawned.Count; i++)
		{
			var img = spawned[i] != null ? spawned[i].GetComponent<Image>() : null;
			if (img != null)
			{
				img.color = (i == selected) ? selectedColor : normalColor;
			}
		}
	}

	private IEnumerator RebuildNextFrame()
	{
		yield return null;
		RebuildFromDropdown();
	}

	private int GetOptionsCount()
	{
		if (useLocationData)
		{
			// Use LocationData array count
			return locationDataArray?.Length ?? 0;
		}
		else
		{
			// Use dropdown options count (original behavior)
			if (dropdown != null && dropdown.options != null) return dropdown.options.Count;
			if (tmpDropdown != null && tmpDropdown.options != null) return tmpDropdown.options.Count;
		}
		return 0;
	}

	private string GetOptionText(int index)
	{
		if (useLocationData)
		{
			// Use LocationData for text
			if (locationDataArray != null && index >= 0 && index < locationDataArray.Length)
			{
				return locationDataArray[index]?.Name ?? string.Empty;
			}
		}
		else
		{
			// Use dropdown options for text (original behavior)
			if (dropdown != null && dropdown.options != null && index >= 0 && index < dropdown.options.Count)
			{
				return dropdown.options[index].text;
			}
			if (tmpDropdown != null && tmpDropdown.options != null && index >= 0 && index < tmpDropdown.options.Count)
			{
				return tmpDropdown.options[index].text;
			}
		}
		return string.Empty;
	}

	private int GetSelectedIndex()
	{
		if (dropdown != null) return dropdown.value;
		if (tmpDropdown != null) return tmpDropdown.value;
		return -1;
	}

	private void SetSelectedIndex(int index)
	{
		if (dropdown != null)
		{
			dropdown.value = index;
		}
		if (tmpDropdown != null)
		{
			tmpDropdown.value = index;
		}
	}

	private void SubscribeValueChanged()
	{
		if (dropdown != null)
		{
			dropdown.onValueChanged.AddListener(OnUnityDropdownValueChanged);
		}
		if (tmpDropdown != null)
		{
			tmpDropdown.onValueChanged.AddListener(OnTmpDropdownValueChanged);
		}
	}

	private void UnsubscribeValueChanged()
	{
		if (dropdown != null)
		{
			dropdown.onValueChanged.RemoveListener(OnUnityDropdownValueChanged);
		}
		if (tmpDropdown != null)
		{
			tmpDropdown.onValueChanged.RemoveListener(OnTmpDropdownValueChanged);
		}
	}

	private void OnUnityDropdownValueChanged(int _)
	{
		HighlightSelected();
		UpdateSelectedLabel();
	}

	private void OnTmpDropdownValueChanged(int _)
	{
		HighlightSelected();
		UpdateSelectedLabel();
	}

	private void UpdateSelectedLabel()
	{
		if (selectedLabel == null) return;
		int idx = GetSelectedIndex();
		string text = GetOptionText(idx);
		selectedLabel.text = text;
	}

	/// <summary>
	/// Sets the location data array and enables location data mode
	/// </summary>
	/// <param name="locations">Array of location data</param>
	public void SetLocationData(LocationData[] locations)
	{
		locationDataArray = locations;
		useLocationData = true;
		
		if (verboseLogging)
		{
			
			// Debug each location's image data
			if (locations != null)
			{
				for (int i = 0; i < locations.Length; i++)
				{
					var location = locations[i];
					bool hasImage = !string.IsNullOrEmpty(location.Image);
					
				}
			}
		}
		
		// Rebuild the dropdown with new data
		RebuildFromDropdown();
	}

	/// <summary>
	/// Sets the location data from JSON string
	/// </summary>
	/// <param name="jsonString">JSON string containing TargetListData</param>
	public void SetLocationDataFromJson(string jsonString)
	{
		try
		{
			TargetListData targetListData = JsonUtility.FromJson<TargetListData>(jsonString);
			if (targetListData != null && targetListData.TargetList != null)
			{
				SetLocationData(targetListData.TargetList);
			}
			
		}
		catch (System.Exception ex)
		{
		}
	}

	/// <summary>
	/// Loads location data from JSON file path
	/// </summary>
	/// <param name="filePath">Path to the JSON file</param>
	public void LoadLocationDataFromFile(string filePath)
	{
		try
		{
			if (System.IO.File.Exists(filePath))
			{
				string jsonContent = System.IO.File.ReadAllText(filePath);
				SetLocationDataFromJson(jsonContent);
				
				
			}
			
		}
		catch (System.Exception ex)
		{
		}
	}

	/// <summary>
	/// Converts base64 string to Texture2D
	/// </summary>
	/// <param name="base64String">Base64 encoded image string</param>
	/// <returns>Texture2D or null if conversion fails</returns>
	private Texture2D Base64ToTexture2D(string base64String)
	{
		if (string.IsNullOrEmpty(base64String))
			return null;

		try
		{
			// Remove data URL prefix if present (e.g., "data:image/jpeg;base64,")
			if (base64String.StartsWith("data:"))
			{
				int commaIndex = base64String.IndexOf(',');
				if (commaIndex != -1)
				{
					base64String = base64String.Substring(commaIndex + 1);
				}
			}

			byte[] imageBytes = Convert.FromBase64String(base64String);
			Texture2D texture = new Texture2D(2, 2);
			texture.LoadImage(imageBytes);
			return texture;
		}
		catch (Exception ex)
		{
			return null;
		}
	}

	/// <summary>
	/// Finds the ImageForDestination component in the prefab
	/// </summary>
	/// <param name="prefab">The prefab GameObject to search in</param>
	/// <returns>Image component or null if not found</returns>
	private Image FindImageForDestination(GameObject prefab)
	{
		// First try to find by name
		Transform imageTransform = prefab.transform.Find("ImageForDestination");
		if (imageTransform != null)
		{
			var image = imageTransform.GetComponent<Image>();
			if (verboseLogging)
			{
			}
			return image;
		}

		// If not found by name, search recursively
		var allImages = prefab.GetComponentsInChildren<Image>();
		if (verboseLogging)
		{
			foreach (var img in allImages)
			{
			}
		}
		
		// Return the first Image component found
		return allImages.Length > 0 ? allImages[0] : null;
	}

	/// <summary>
	/// Creates a simple default sprite for locations without images
	/// </summary>
	/// <returns>A simple gray sprite with a location icon</returns>
	private Sprite CreateTestSprite()
	{
		Texture2D testTexture = new Texture2D(64, 64);
		Color[] pixels = new Color[64 * 64];
		
		// Fill with light gray background
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i] = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray
		}
		
		// Draw a simple location pin icon in the center
		int centerX = 32;
		int centerY = 32;
		int radius = 8;
		
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				int index = y * 64 + x;
				float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
				
				if (distance <= radius)
				{
					pixels[index] = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark gray circle
				}
				else if (distance <= radius + 2)
				{
					pixels[index] = new Color(0.5f, 0.5f, 0.5f, 1f); // Medium gray border
				}
			}
		}
		
		testTexture.SetPixels(pixels);
		testTexture.Apply();
		
		return Sprite.Create(testTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
	}
}