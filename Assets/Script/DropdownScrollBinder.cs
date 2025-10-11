using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

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

	[Header("Image Loading")]
	public bool loadImages = true;
	public Sprite defaultImage; // Fallback image when loading fails or no image URL
	public float imageLoadTimeout = 10f; // Timeout for image loading in seconds

	private readonly List<GameObject> spawned = new List<GameObject>();
	private readonly Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();


[SerializeField]
private AppCanvas appCanvas;

[SerializeField]
private EventsFetcher eventsFetcher; // Reference to get EventData for image loading
	void Awake()
	{
		if ((dropdown == null && tmpDropdown == null) || scrollRect == null || content == null || rowPrefab == null)
		{
			Debug.LogWarning("[DropdownScrollBinder] Assign Dropdown or TMP_Dropdown, plus ScrollRect, Content, and RowPrefab in Inspector.");
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
		if (verboseLogging)
		{
			Debug.Log("[DropdownScrollBinder] RebuildFromDropdown() called");
		}

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
				Debug.Log("[DropdownScrollBinder] No options found on assigned dropdown.");
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

			// Load image for this row if enabled
			if (loadImages)
			{
				LoadImageForRow(go, i);
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
			if (verboseLogging)
			{
				Debug.Log($"[DropdownScrollBinder] Wired click for row {idx} -> '{optionText}'");
			}

			var img = go.GetComponent<Image>();
			if (img != null) img.color = normalColor;
		}

		HighlightSelected();
		UpdateSelectedLabel();
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
	}

	private void OnRowClicked(int index)
	{
		if (verboseLogging)
		{
			Debug.Log($"[DropdownScrollBinder] Row clicked index={index}");
		}
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
		if (dropdown != null && dropdown.options != null) return dropdown.options.Count;
		if (tmpDropdown != null && tmpDropdown.options != null) return tmpDropdown.options.Count;
		return 0;
	}

	private string GetOptionText(int index)
	{
		if (dropdown != null && dropdown.options != null && index >= 0 && index < dropdown.options.Count)
		{
			return dropdown.options[index].text;
		}
		if (tmpDropdown != null && tmpDropdown.options != null && index >= 0 && index < tmpDropdown.options.Count)
		{
			return tmpDropdown.options[index].text;
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

	private void LoadImageForRow(GameObject rowObject, int index)
	{
		// Find the ImageForDestination component in the row
		Transform imageTransform = rowObject.transform.Find("ImageForDestination");
		if (imageTransform == null)
		{
			if (verboseLogging)
			{
				Debug.LogWarning($"[DropdownScrollBinder] ImageForDestination not found in row {index}");
			}
			return;
		}

		Image imageComponent = imageTransform.GetComponent<Image>();
		if (imageComponent == null)
		{
			if (verboseLogging)
			{
				Debug.LogWarning($"[DropdownScrollBinder] Image component not found on ImageForDestination for row {index}");
			}
			return;
		}

		// Get the image URL from EventData
		string imageUrl = GetImageUrlForIndex(index);
		if (string.IsNullOrEmpty(imageUrl))
		{
			if (verboseLogging)
			{
				Debug.Log($"[DropdownScrollBinder] No image URL for row {index}, using default image");
			}
			imageComponent.sprite = defaultImage;
			return;
		}

		// Check if image is already cached
		if (imageCache.ContainsKey(imageUrl))
		{
			imageComponent.sprite = imageCache[imageUrl];
			if (verboseLogging)
			{
				Debug.Log($"[DropdownScrollBinder] Using cached image for row {index}");
			}
			return;
		}

		// Load image asynchronously
		StartCoroutine(LoadImageCoroutine(imageUrl, imageComponent, index));
	}

	private string GetImageUrlForIndex(int index)
	{
		if (eventsFetcher == null || eventsFetcher.events == null || index < 0 || index >= eventsFetcher.events.Count)
		{
			return string.Empty;
		}

		EventData eventData = eventsFetcher.events[index];
		return eventData?.image ?? string.Empty;
	}

	private IEnumerator LoadImageCoroutine(string imageUrl, Image imageComponent, int rowIndex)
	{
		if (verboseLogging)
		{
			Debug.Log($"[DropdownScrollBinder] Loading image for row {rowIndex}: {imageUrl}");
		}

		using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
		{
			request.timeout = (int)imageLoadTimeout;
			
			yield return request.SendWebRequest();

			if (request.result == UnityWebRequest.Result.Success)
			{
				Texture2D texture = DownloadHandlerTexture.GetContent(request);
				if (texture != null)
				{
					Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
					
					// Cache the sprite
					imageCache[imageUrl] = sprite;
					
					// Set the sprite on the image component
					imageComponent.sprite = sprite;
					
					if (verboseLogging)
					{
						Debug.Log($"[DropdownScrollBinder] Successfully loaded image for row {rowIndex}");
					}
				}
				else
				{
					if (verboseLogging)
					{
						Debug.LogWarning($"[DropdownScrollBinder] Failed to create texture for row {rowIndex}");
					}
					imageComponent.sprite = defaultImage;
				}
			}
			else
			{
				if (verboseLogging)
				{
					Debug.LogWarning($"[DropdownScrollBinder] Failed to load image for row {rowIndex}: {request.error}");
				}
				imageComponent.sprite = defaultImage;
			}
		}
	}

	/// <summary>
	/// Clears the image cache. Call this when you want to force reload all images.
	/// </summary>
	public void ClearImageCache()
	{
		imageCache.Clear();
		if (verboseLogging)
		{
			Debug.Log("[DropdownScrollBinder] Image cache cleared");
		}
	}

	/// <summary>
	/// Preloads images for all current events. Useful for improving performance.
	/// </summary>
	public void PreloadAllImages()
	{
		if (eventsFetcher == null || eventsFetcher.events == null)
		{
			return;
		}

		StartCoroutine(PreloadImagesCoroutine());
	}

	private IEnumerator PreloadImagesCoroutine()
	{
		if (verboseLogging)
		{
			Debug.Log("[DropdownScrollBinder] Starting image preload");
		}

		foreach (EventData eventData in eventsFetcher.events)
		{
			if (!string.IsNullOrEmpty(eventData.image) && !imageCache.ContainsKey(eventData.image))
			{
				yield return StartCoroutine(LoadImageToCache(eventData.image));
			}
		}

		if (verboseLogging)
		{
			Debug.Log("[DropdownScrollBinder] Image preload completed");
		}
	}

	private IEnumerator LoadImageToCache(string imageUrl)
	{
		using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
		{
			request.timeout = (int)imageLoadTimeout;
			
			yield return request.SendWebRequest();

			if (request.result == UnityWebRequest.Result.Success)
			{
				Texture2D texture = DownloadHandlerTexture.GetContent(request);
				if (texture != null)
				{
					Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
					imageCache[imageUrl] = sprite;
					
					if (verboseLogging)
					{
						Debug.Log($"[DropdownScrollBinder] Preloaded image: {imageUrl}");
					}
				}
			}
		}
	}
}


