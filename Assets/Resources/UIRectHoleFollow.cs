using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIRectHoleFollow : MonoBehaviour
{
    public RectTransform holeTarget; // assign your Image 2 here
    private Material mat;
    private Canvas rootCanvas;

    void Start()
    {
        mat = GetComponent<Image>().material;
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (holeTarget == null || mat == null || rootCanvas == null)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, holeTarget.position);
        Vector2 canvasSize = rootCanvas.GetComponent<RectTransform>().sizeDelta;
        Vector2 holeCenter = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

        // Convert pixel size to normalized UV (500x500)
        Vector2 holeSize = new Vector2(500f / Screen.width, 500f / Screen.height);

        mat.SetVector("_HoleCenter", new Vector4(holeCenter.x, holeCenter.y, 0, 0));
        mat.SetVector("_HoleSize", new Vector4(holeSize.x, holeSize.y, 0, 0));
    }
}
