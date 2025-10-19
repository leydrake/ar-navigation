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
       
        if (uiDocument == null)
        {
           
            return;
        }

       

        ShowPage(startPageUXML);
    }
    

    void ShowPage(VisualTreeAsset pageAsset)
    {
        

        var root = uiDocument != null ? uiDocument.rootVisualElement : null;
        if (root == null)
        {
            
            return;
        }

        root.Clear();
        if (pageAsset != null)
        {
            pageAsset.CloneTree(root);
           
        }

		// 🔗 Always try to wire the 'info' VisualElement to open the tutorial scene
		TryWireClick(root, "info", () =>
		{
			
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

            
        }
        else if (pageAsset == permissionPageUXML)
        {
            TryWireClick(root, "GrantPermissionButton", () => GrantPermission());

            bool wiredBack2 =
                TryWireClick(root, "BackButton", () => ShowPage(termsPageUXML)) ||
                TryWireClick(root, "Back", () => ShowPage(termsPageUXML)) ||
                TryWireClick(root, "btnBack", () => ShowPage(termsPageUXML));

           
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
       
#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            GoToNextScene();
            return;
        }

      
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
       
    }
}



