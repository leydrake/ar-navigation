using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenItems : MonoBehaviour
{
    [Header("Scene Management")]
    public GameObject[] sceneGameObjects; // Array of game objects that represent different scenes
    public string[] sceneNames; // Names for each scene (optional, for debugging)
    
    [Header("UI References")]
    public Button[] menuItemButtons; // Buttons in your menu panel
    public ToggleMenu toggleMenu; // Reference to your menu toggle script
    
    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool closeMenuAfterSelection = true;
    
    [Header("Scene Control")]
    public bool hideOtherScenesOnOpen = true;
    public bool showSceneOnStart = false;
    public int defaultSceneIndex = 0;
    
    private int currentSceneIndex = -1;
    private Coroutine currentTransition;
    private Dictionary<Button, int> buttonToSceneMap;
    
    void Start()
    {
        InitializeSceneSystem();
        SetupButtonListeners();
        
        if (showSceneOnStart && sceneGameObjects.Length > 0)
        {
            OpenScene(defaultSceneIndex, false);
        }
    }
    
    private void InitializeSceneSystem()
    {
        // Create mapping between buttons and scenes
        buttonToSceneMap = new Dictionary<Button, int>();
        
        // Initially hide all scenes
        for (int i = 0; i < sceneGameObjects.Length; i++)
        {
            if (sceneGameObjects[i] != null)
            {
                sceneGameObjects[i].SetActive(false);
            }
        }
        
        // Validate scene names array
        if (sceneNames == null || sceneNames.Length != sceneGameObjects.Length)
        {
            sceneNames = new string[sceneGameObjects.Length];
            for (int i = 0; i < sceneGameObjects.Length; i++)
            {
                sceneNames[i] = sceneGameObjects[i] != null ? sceneGameObjects[i].name : $"Scene_{i}";
            }
        }
    }
    
    private void SetupButtonListeners()
    {
        // Map each button to a scene index
        for (int i = 0; i < menuItemButtons.Length && i < sceneGameObjects.Length; i++)
        {
            if (menuItemButtons[i] != null)
            {
                int sceneIndex = i; // Capture the index for the closure
                buttonToSceneMap[menuItemButtons[i]] = sceneIndex;
                menuItemButtons[i].onClick.AddListener(() => OnMenuItemClicked(sceneIndex));
            }
        }
    }
    
    public void OnMenuItemClicked(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < sceneGameObjects.Length)
        {
            // If it's the same scene, toggle it off
            if (currentSceneIndex == sceneIndex)
            {
                CloseAllScenes();
            }
            else
            {
                OpenScene(sceneIndex, true);
            }
            
            // Close menu after selection if enabled
            if (closeMenuAfterSelection && toggleMenu != null)
            {
                toggleMenu.CloseMenu();
            }
        }
       
    }
    
    public void OpenScene(int sceneIndex, bool animate = true)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneGameObjects.Length)
        {
            return;
        }
        
        if (sceneGameObjects[sceneIndex] == null)
        {
            return;
        }
        
        // If opening the same scene, do nothing
        if (currentSceneIndex == sceneIndex)
        {
            return;
        }
        
        
        if (animate)
        {
            StartCoroutine(TransitionToScene(sceneIndex));
        }
        else
        {
            SetSceneActive(sceneIndex);
        }
    }
    
    private IEnumerator TransitionToScene(int targetSceneIndex)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        currentTransition = StartCoroutine(PerformSceneTransition(targetSceneIndex));
        yield return currentTransition;
    }
    
    private IEnumerator PerformSceneTransition(int targetSceneIndex)
    {
        float elapsedTime = 0f;
        
        // Get current scene components for fade out
        CanvasGroup currentCanvasGroup = null;
        if (currentSceneIndex >= 0 && currentSceneIndex < sceneGameObjects.Length)
        {
            currentCanvasGroup = sceneGameObjects[currentSceneIndex].GetComponent<CanvasGroup>();
        }
        
        // Get target scene components for fade in
        CanvasGroup targetCanvasGroup = sceneGameObjects[targetSceneIndex].GetComponent<CanvasGroup>();
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = sceneGameObjects[targetSceneIndex].AddComponent<CanvasGroup>();
        }
        
        // Start fade out of current scene
        if (currentCanvasGroup != null)
        {
            while (elapsedTime < transitionDuration / 2)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / (transitionDuration / 2);
                float curveValue = transitionCurve.Evaluate(progress);
                
                currentCanvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
                yield return null;
            }
        }
        
        // Switch scenes
        SetSceneActive(targetSceneIndex);
        
        // Reset elapsed time for fade in
        elapsedTime = 0f;
        
        // Start fade in of target scene
        while (elapsedTime < transitionDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / (transitionDuration / 2);
            float curveValue = transitionCurve.Evaluate(progress);
            
            targetCanvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
            yield return null;
        }
        
        // Ensure final state
        targetCanvasGroup.alpha = 1f;
        
        currentTransition = null;
    }
    
    private void SetSceneActive(int sceneIndex)
    {
        // Hide other scenes if enabled
        if (hideOtherScenesOnOpen)
        {
            for (int i = 0; i < sceneGameObjects.Length; i++)
            {
                if (sceneGameObjects[i] != null)
                {
                    sceneGameObjects[i].SetActive(i == sceneIndex);
                }
            }
        }
        else
        {
            // Just activate the target scene
            if (sceneGameObjects[sceneIndex] != null)
            {
                sceneGameObjects[sceneIndex].SetActive(true);
            }
        }
        
        currentSceneIndex = sceneIndex;
    }
    
    // Public methods for external control
    public void CloseAllScenes()
    {
        for (int i = 0; i < sceneGameObjects.Length; i++)
        {
            if (sceneGameObjects[i] != null)
            {
                sceneGameObjects[i].SetActive(false);
            }
        }
        currentSceneIndex = -1;
    }
    
    public int GetCurrentSceneIndex()
    {
        return currentSceneIndex;
    }
    
    public string GetCurrentSceneName()
    {
        if (currentSceneIndex >= 0 && currentSceneIndex < sceneNames.Length)
        {
            return sceneNames[currentSceneIndex];
        }
        return "No Scene Active";
    }
    
    public void OpenNextScene()
    {
        int nextIndex = (currentSceneIndex + 1) % sceneGameObjects.Length;
        OpenScene(nextIndex, true);
    }
    
    public void OpenPreviousScene()
    {
        int prevIndex = currentSceneIndex - 1;
        if (prevIndex < 0) prevIndex = sceneGameObjects.Length - 1;
        OpenScene(prevIndex, true);
    }
    
    // Method to add a new scene at runtime
    public void AddScene(GameObject newScene, string sceneName = "")
    {
        System.Array.Resize(ref sceneGameObjects, sceneGameObjects.Length + 1);
        System.Array.Resize(ref sceneNames, sceneNames.Length + 1);
        
        int newIndex = sceneGameObjects.Length - 1;
        sceneGameObjects[newIndex] = newScene;
        sceneNames[newIndex] = string.IsNullOrEmpty(sceneName) ? newScene.name : sceneName;
        
        // Initially hide the new scene
        if (newScene != null)
        {
            newScene.SetActive(false);
        }
        
    }
    
    // Method to remove a scene at runtime
    public void RemoveScene(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneGameObjects.Length) return;
        
        // If removing the current scene, close it first
        if (currentSceneIndex == sceneIndex)
        {
            CloseAllScenes();
        }
        
        // Shift arrays to remove the element
        for (int i = sceneIndex; i < sceneGameObjects.Length - 1; i++)
        {
            sceneGameObjects[i] = sceneGameObjects[i + 1];
            sceneNames[i] = sceneNames[i + 1];
        }
        
        System.Array.Resize(ref sceneGameObjects, sceneGameObjects.Length - 1);
        System.Array.Resize(ref sceneNames, sceneNames.Length - 1);
        
    }
}
