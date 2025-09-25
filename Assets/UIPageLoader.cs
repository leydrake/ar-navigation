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

	// Tracks whether we are awaiting the Android permission dialog result
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
			if (!TryWireClick(root, "StartButton", () => { Debug.Log("[UIPageLoader] StartButton clicked."); ShowPage(termsPageUXML); }))
			{
				Debug.LogWarning("[UIPageLoader] StartButton not found as Button/VisualElement in startPageUXML.");
			}
        }
        else if (pageAsset == termsPageUXML)
        {
			if (!TryWireClick(root, "AcceptTermsButton", () => { Debug.Log("[UIPageLoader] AcceptTermsButton clicked."); ShowPage(permissionPageUXML); }))
			{
				Debug.LogWarning("[UIPageLoader] AcceptTermsButton not found as Button/VisualElement in termsPageUXML.");
			}

			// Wire Back button on Terms page -> back to Start page (support Button or VisualElement)
			bool wiredBack =
				TryWireClick(root, "BackButton", () => { Debug.Log("[UIPageLoader] BackButton (Terms) clicked."); ShowPage(startPageUXML); }) ||
				TryWireClick(root, "Back", () => { Debug.Log("[UIPageLoader] Back (Terms) clicked."); ShowPage(startPageUXML); }) ||
				TryWireClick(root, "btnBack", () => { Debug.Log("[UIPageLoader] btnBack (Terms) clicked."); ShowPage(startPageUXML); });
			if (!wiredBack)
			{
				Debug.LogWarning("[UIPageLoader] Back element not found on Terms page.");
			}
        }
        else if (pageAsset == permissionPageUXML)
        {
			if (!TryWireClick(root, "GrantPermissionButton", () => { Debug.Log("[UIPageLoader] GrantPermissionButton clicked."); GrantPermission(); }))
			{
				Debug.LogWarning("[UIPageLoader] GrantPermissionButton not found as Button/VisualElement in permissionPageUXML.");
			}

			// Wire Back button on Permission page -> back to Terms page (support Button or VisualElement)
			bool wiredBack2 =
				TryWireClick(root, "BackButton", () => { Debug.Log("[UIPageLoader] BackButton (Permission) clicked."); ShowPage(termsPageUXML); }) ||
				TryWireClick(root, "Back", () => { Debug.Log("[UIPageLoader] Back (Permission) clicked."); ShowPage(termsPageUXML); }) ||
				TryWireClick(root, "btnBack", () => { Debug.Log("[UIPageLoader] btnBack (Permission) clicked."); ShowPage(termsPageUXML); });
			if (!wiredBack2)
			{
				Debug.LogWarning("[UIPageLoader] Back element not found on Permission page.");
			}
        }
    }

	// Tries to find a clickable element by name as either Button or VisualElement and wire a click callback
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
        // Check if we already have camera permission
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("[UIPageLoader][Android] Camera permission already granted. Proceeding to SampleScene.");
            GoToNextScene();
            return;
        }

        // Request camera permission using coroutine for better handling
        Debug.Log("[UIPageLoader][Android] Requesting camera permission...");
        StartCoroutine(RequestCameraPermission());
#elif UNITY_IOS
        // iOS will show its own prompt automatically
        Debug.Log("[UIPageLoader][iOS] Proceeding to next scene (system handles prompt).");
        GoToNextScene();
#else
        Debug.Log("[UIPageLoader] Non-mobile platform; proceeding to next scene.");
        GoToNextScene();
#endif
    }

    IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID
        awaitingPermissionResult = true;
        
        // Request the permission
        Permission.RequestUserPermission(Permission.Camera);
        
        // Wait a frame for the permission dialog to appear
        yield return null;
        
        // Wait for the user to respond (check every frame)
        while (awaitingPermissionResult)
        {
            // Check if permission was granted
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("[UIPageLoader][Android] Permission granted! Proceeding to SampleScene.");
                awaitingPermissionResult = false;
                GoToNextScene();
                yield break;
            }
            
            yield return null;
        }
        
        // If we get here, permission was denied
        Debug.Log("[UIPageLoader][Android] Permission denied. Staying on Permission page.");
#endif
        yield break;
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_ANDROID
		bool hasPerm = Permission.HasUserAuthorizedPermission(Permission.Camera);
		Debug.Log("[UIPageLoader][Android] OnApplicationFocus: hasFocus=" + hasFocus + ", permission=" + hasPerm + ", awaitingPermissionResult=" + awaitingPermissionResult);
		if (hasFocus && awaitingPermissionResult)
		{
			awaitingPermissionResult = false;
			if (hasPerm)
			{
				Debug.Log("[UIPageLoader][Android] Permission granted from dialog. Proceeding to SampleScene.");
				GoToNextScene();
			}
			else
			{
				Debug.Log("[UIPageLoader][Android] Permission denied from dialog. Staying on Permission page.");
			}
		}
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
