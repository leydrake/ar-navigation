using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class UIPageLoader : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Page UXML Files")]
    public VisualTreeAsset startPageUXML;
    public VisualTreeAsset termsPageUXML;
    public VisualTreeAsset permissionPageUXML;

    [Header("Next Scene")]
    public string nextSceneName = "SampleScene"; // set this in Inspector

    [Header("Tutorial URL")]
    public string tutorialURL = "https://navigatemycampus.capstone-two.com/tutorial";

    bool awaitingPermissionResult = false;

    void Start()
    {
        Debug.Log("[UIPageLoader] Start called on " + gameObject.name);
        if (uiDocument == null)
        {
            Debug.LogError("[UIPageLoader] UIDocument is not assigned.");
            return;
        }

        if (startPageUXML == null || termsPageUXML == null || permissionPageUXML == null)
        {
            Debug.LogWarning("[UIPageLoader] One or more VisualTreeAssets are not assigned.");
        }

        ShowPage(startPageUXML);
    }
    

    void ShowPage(VisualTreeAsset pageAsset)
    {
        Debug.Log("[UIPageLoader] ShowPage called with: " + (pageAsset != null ? pageAsset.name : "<null>"));

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
        {
            Debug.LogError("[UIPageLoader] rootVisualElement is null (is UIDocument active?).");
            return;
        }

        root.Clear();
        if (pageAsset != null)
        {
            pageAsset.CloneTree(root);
            Debug.Log("[UIPageLoader] Cloned UXML into root. Child count: " + root.childCount);
        }

		// 🔗 Always try to wire the 'info' VisualElement to open the tutorial scene
		TryWireClick(root, "info", () =>
		{
			Debug.Log("[UIPageLoader] 'info' clicked → loading 'tutorial' scene");
			SceneManager.LoadScene("tutorial");
		});

        // 🔘 Handle per-page navigation buttons
        if (pageAsset == startPageUXML)
        {
            TryWireClick(root, "StartButton", () => ShowPage(termsPageUXML));
        }
        else if (pageAsset == termsPageUXML)
        {
            TryWireClick(root, "AcceptTermsButton", () => ShowPage(permissionPageUXML));

            bool wiredBack =
                TryWireClick(root, "BackButton", () => ShowPage(startPageUXML)) ||
                TryWireClick(root, "Back", () => ShowPage(startPageUXML)) ||
                TryWireClick(root, "btnBack", () => ShowPage(startPageUXML));

            if (!wiredBack)
                Debug.LogWarning("[UIPageLoader] Back element not found on Terms page.");
        }
        else if (pageAsset == permissionPageUXML)
        {
            TryWireClick(root, "GrantPermissionButton", () => GrantPermission());

            bool wiredBack2 =
                TryWireClick(root, "BackButton", () => ShowPage(termsPageUXML)) ||
                TryWireClick(root, "Back", () => ShowPage(termsPageUXML)) ||
                TryWireClick(root, "btnBack", () => ShowPage(termsPageUXML));

            if (!wiredBack2)
                Debug.LogWarning("[UIPageLoader] Back element not found on Permission page.");
        }
    }

    bool TryWireClick(VisualElement root, string elementName, System.Action onClick)
{
    var button = root.Q<Button>(elementName);
    if (button != null)
    {
        button.clicked += onClick;
        return true;
    }

    var ve = root.Q<VisualElement>(elementName);
    if (ve != null)
    {
        ve.RegisterCallback<ClickEvent>(_ => onClick());
        return true;
    }

    return false;
}



    void GrantPermission()
    {
        Debug.Log("[UIPageLoader] GrantPermission invoked.");
#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            GoToNextScene();
            return;
        }

        Debug.Log("[UIPageLoader][Android] Requesting camera permission...");
        StartCoroutine(RequestCameraPermission());
#elif UNITY_IOS
        GoToNextScene();
#else
        GoToNextScene();
#endif
    }

    IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID
        awaitingPermissionResult = true;
        Permission.RequestUserPermission(Permission.Camera);
        yield return null;

        while (awaitingPermissionResult)
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                awaitingPermissionResult = false;
                GoToNextScene();
                yield break;
            }
            yield return null;
        }
#endif
        yield break;
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_ANDROID
        bool hasPerm = Permission.HasUserAuthorizedPermission(Permission.Camera);
        if (hasFocus && awaitingPermissionResult)
        {
            awaitingPermissionResult = false;
            if (hasPerm)
                GoToNextScene();
        }
#endif
    }

    void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[UIPageLoader] Next scene name is not set!");
        }
    }
}



