using UnityEngine;
using UnityEngine.UI;

public class DropdownRowStyler : MonoBehaviour
{
	[Range(20f, 120f)]
	public float rowHeight = 44f;

	public int fontSize = 16;

	void OnValidate()
	{
		Apply();
	}

	void Awake()
	{
		Apply();
	}

	private void Apply()
	{
		var rt = GetComponent<RectTransform>();
		if (rt != null)
		{
			var size = rt.sizeDelta;
			size.y = rowHeight;
			rt.sizeDelta = size;
		}

		var text = GetComponentInChildren<Text>();
		if (text != null)
		{
			text.fontSize = fontSize;
		}
	}
}


