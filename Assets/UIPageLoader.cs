using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

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
    public string nextSceneName = "MainApp"; // set this in Inspector

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

        // show the start page first
		ShowPage(startPageUXML);
    }

    void ShowPage(VisualTreeAsset pageAsset)
    {
        Debug.Log("[UIPageLoader] ShowPage called with: " + (pageAsset != null ? pageAsset.name : "<null>"));

        // rebuild current UI with selected page content
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
        else
        {
            Debug.LogWarning("[UIPageLoader] pageAsset is null; UI will be empty.");
        }

		if (pageAsset == startPageUXML)
        {
            var startButton = root.Q<Button>("StartButton");
            if (startButton != null)
            {
                Debug.Log("[UIPageLoader] Found StartButton; wiring click handler.");
				startButton.clicked += () =>
				{
					Debug.Log("[UIPageLoader] StartButton clicked.");
					ShowPage(termsPageUXML);
				};
            }
            else
            {
                Debug.LogWarning("[UIPageLoader] StartButton not found in startPageUXML.");
            }
        }
        else if (pageAsset == termsPageUXML)
        {
            var acceptButton = root.Q<Button>("AcceptTermsButton");
            if (acceptButton != null)
            {
                Debug.Log("[UIPageLoader] Found AcceptTermsButton; wiring click handler.");
                acceptButton.clicked += () =>
                {
                    Debug.Log("[UIPageLoader] AcceptTermsButton clicked.");
                    ShowPage(permissionPageUXML);
                };
            }
            else
            {
                Debug.LogWarning("[UIPageLoader] AcceptTermsButton not found in termsPageUXML.");
            }
        }
        else if (pageAsset == permissionPageUXML)
        {
            var grantButton = root.Q<Button>("GrantPermissionButton");
            if (grantButton != null)
            {
                Debug.Log("[UIPageLoader] Found GrantPermissionButton; wiring click handler.");
                grantButton.clicked += () =>
                {
                    Debug.Log("[UIPageLoader] GrantPermissionButton clicked.");
                    GrantPermission();
                };
            }
            else
            {
                Debug.LogWarning("[UIPageLoader] GrantPermissionButton not found in permissionPageUXML.");
            }
        }
    }

    void GrantPermission()
    {
        Debug.Log("[UIPageLoader] GrantPermission invoked.");
#if UNITY_ANDROID
        bool hasPerm = Permission.HasUserAuthorizedPermission(Permission.Camera);
        Debug.Log("[UIPageLoader][Android] Camera permission current: " + hasPerm);
        if (!hasPerm)
        {
            Debug.Log("[UIPageLoader][Android] Requesting camera permission.");
            Permission.RequestUserPermission(Permission.Camera);
        }
        else
        {
            Debug.Log("[UIPageLoader][Android] Permission already granted. Proceeding to next scene.");
            GoToNextScene();
        }
#elif UNITY_IOS
        // iOS will show its own prompt automatically
        Debug.Log("[UIPageLoader][iOS] Proceeding to next scene (system handles prompt).");
        GoToNextScene();
#else
        Debug.Log("[UIPageLoader] Non-mobile platform; proceeding to next scene.");
        GoToNextScene();
#endif
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_ANDROID
        bool hasPerm = Permission.HasUserAuthorizedPermission(Permission.Camera);
        Debug.Log("[UIPageLoader][Android] OnApplicationFocus: hasFocus=" + hasFocus + ", permission=" + hasPerm);
		// Prevent auto-navigation to the next scene on focus; only navigate via Start button
#endif
    }

    void GoToNextScene()
    {
        Debug.Log("[UIPageLoader] GoToNextScene called with nextSceneName='" + nextSceneName + "'");
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[UIPageLoader] Loading scene '" + nextSceneName + "'.");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[UIPageLoader] Next scene name is not set!");
        }
    }
}
