using UnityEngine;
using UnityEngine.UIElements;

public class OpenInfoPage : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Tutorial URL")]
    public string tutorialURL = "https://navigatemycampus.capstone-two.com/tutorial";

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[OpenInfoPage] UIDocument is not assigned!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[OpenInfoPage] rootVisualElement is null!");
            return;
        }

        // Query for the VisualElement named "info"
        var infoElem = root.Q<VisualElement>("info");
        if (infoElem != null)
        {
            infoElem.RegisterCallback<ClickEvent>(_ =>
            {
                Debug.Log("[OpenInfoPage] info clicked — opening URL: " + tutorialURL);
                Application.OpenURL(tutorialURL);
            });
        }
        else
        {
            Debug.LogWarning("[OpenInfoPage] Could not find VisualElement with name 'info'.");
        }
    }
}
