using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DropdownListBinder : MonoBehaviour
{
	[SerializeField]
	private UIDocument uiDocument;

	[SerializeField]
	private string dropdownFieldName = "MyDropdown";

	[SerializeField]
	private string scrollViewName = "ListScrollView";

	[SerializeField]
	private int rowHeight = 44;

	[SerializeField]
	private int rowFontSize = 16;

	[SerializeField]
	private bool verboseLogging = false;

	private DropdownField dropdownField;
	private ScrollView listView;

	private void Awake()
	{
		if (uiDocument == null)
		{
			uiDocument = GetComponent<UIDocument>();
		}

		var root = uiDocument != null ? uiDocument.rootVisualElement : null;
		if (root == null)
		{
			Debug.LogWarning("[DropdownListBinder] UIDocument or rootVisualElement is missing.");
			return;
		}

		dropdownField = root.Q<DropdownField>(dropdownFieldName);
		listView = root.Q<ScrollView>(scrollViewName);

		if (dropdownField == null)
		{
			Debug.LogWarning($"[DropdownListBinder] DropdownField '{dropdownFieldName}' not found in UXML.");
		}

		if (listView == null)
		{
			Debug.LogWarning($"[DropdownListBinder] ScrollView '{scrollViewName}' not found in UXML.");
		}
		else
		{
			// Stabilize layout similar to EventsUIController
			listView.style.flexGrow = 1;
			listView.style.flexShrink = 1;
			listView.style.minHeight = 0;
		}

		RebuildListFromDropdown();

		// Keep list selection in sync if dropdown value changes elsewhere
		if (dropdownField != null)
		{
			dropdownField.RegisterValueChangedCallback(_ => HighlightSelected());
		}
	}

	/// <summary>
	/// Call this after you change dropdown choices at runtime.
	/// </summary>
	public void RebuildListFromDropdown()
	{
		if (listView == null)
		{
			return;
		}

		listView.Clear();

		List<string> choices = dropdownField != null && dropdownField.choices != null
			? dropdownField.choices
			: new List<string>();

		if (verboseLogging)
		{
			Debug.Log($"[DropdownListBinder] Building list with {choices.Count} items.");
		}

		foreach (var choice in choices)
		{
			var row = BuildRow(choice);
			listView.Add(row);
		}

		HighlightSelected();
	}

	private VisualElement BuildRow(string text)
	{
		var row = new VisualElement();
		row.style.flexDirection = FlexDirection.Row;
		row.style.alignItems = Align.Center;
		row.style.height = rowHeight;
		row.style.marginLeft = 12;
		row.style.marginRight = 12;
		row.style.marginBottom = 6;
		row.style.paddingLeft = 12;
		row.style.paddingRight = 12;
		row.style.backgroundColor = new Color(0.94f, 0.94f, 0.94f, 1f);
		row.style.borderTopLeftRadius = 8;
		row.style.borderTopRightRadius = 8;
		row.style.borderBottomLeftRadius = 8;
		row.style.borderBottomRightRadius = 8;

		// Subtle border for list style
		row.style.borderTopWidth = 1;
		row.style.borderBottomWidth = 1;
		row.style.borderLeftWidth = 1;
		row.style.borderRightWidth = 1;
		row.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		row.style.borderBottomColor = new Color(0.82f, 0.82f, 0.82f, 1f);
		row.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
		row.style.borderRightColor = new Color(0.82f, 0.82f, 0.82f, 1f);

		var label = new Label(string.IsNullOrEmpty(text) ? "(empty)" : text);
		label.style.fontSize = rowFontSize;
		label.style.color = Color.black;
		label.style.flexGrow = 1;

		row.Add(label);

		row.RegisterCallback<ClickEvent>(evt => OnRowClicked(text));

		return row;
	}

	private void OnRowClicked(string value)
	{
		if (dropdownField == null)
		{
			return;
		}

		if (verboseLogging)
		{
			Debug.Log($"[DropdownListBinder] Row clicked -> '{value}'");
		}

		// Ensure the value exists in choices; if not, add it
		if (dropdownField.choices == null)
		{
			dropdownField.choices = new List<string>();
		}
		if (!dropdownField.choices.Contains(value))
		{
			dropdownField.choices.Add(value);
		}

		// Update dropdown value without re-triggering unnecessary UI side effects
		dropdownField.value = value;
		HighlightSelected();
	}

	private void HighlightSelected()
	{
		if (listView == null || dropdownField == null)
		{
			return;
		}

		string selected = dropdownField.value;
		for (int i = 0; i < listView.childCount; i++)
		{
			var child = listView[i];
			bool isSelected = false;
			var label = child.Q<Label>();
			if (label != null)
			{
				isSelected = string.Equals(label.text, selected);
			}

			child.style.backgroundColor = isSelected
				? new Color(0.8f, 0.9f, 1f, 1f)
				: new Color(0.94f, 0.94f, 0.94f, 1f);
		}
	}

	/// <summary>
	/// Public helper to set dropdown choices then rebuild the list from them.
	/// </summary>
	public void SetChoicesAndRebuild(List<string> choices, string initialValue = null)
	{
		if (dropdownField == null)
		{
			return;
		}

		dropdownField.choices = choices ?? new List<string>();
		if (!string.IsNullOrEmpty(initialValue))
		{
			dropdownField.value = initialValue;
		}
		else if (dropdownField.choices.Count > 0)
		{
			dropdownField.value = dropdownField.choices[0];
		}

		RebuildListFromDropdown();
	}
}


