using UnityEngine;
using UnityEngine.UI;

public class DropdownRowStyler : MonoBehaviour
{
    [Range(20f, 300f)]
    public float rowHeight = 300f;
    public int fontSize = 16;
    public float marginBottom = 30f;

    void OnValidate() => Apply();
    void Awake() => Apply();

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

        // Add bottom spacing by adjusting RectTransform offsets
        rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y - marginBottom);
    }
}