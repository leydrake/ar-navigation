using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class tutorialManager : MonoBehaviour
{
    [Header("Steps Container (parent with step1..step10 children)")]
    public Transform stepsContainer;

    [Header("Navigation Buttons (optional global)")]
    public Button nextButton;      // Optional: global Next
    public Button previousButton;  // Optional: global Previous
    public Button finishButton;    // Optional: global Finish

    [Header("Scene To Load On Finish")]
    public string sceneOnFinish; // set in Inspector

    private readonly List<GameObject> steps = new List<GameObject>();
    private int currentIndex = 0;

    void Awake()
    {
        if (stepsContainer == null)
        {
            Debug.LogError("[tutorialManager] stepsContainer is not assigned.");
            return;
        }

        steps.Clear();
        for (int i = 0; i < stepsContainer.childCount; i++)
        {
            var child = stepsContainer.GetChild(i).gameObject;
            steps.Add(child);
        }

        if (nextButton != null) nextButton.onClick.AddListener(Next);
        if (previousButton != null) previousButton.onClick.AddListener(Previous);
        if (finishButton != null) finishButton.onClick.AddListener(Finish);
    }

    void Start()
    {
        ShowStep(0);
    }

    private void ShowStep(int index)
    {
        if (steps.Count == 0)
        {
            Debug.LogWarning("[tutorialManager] No steps found under container.");
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, steps.Count - 1);

        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].SetActive(i == currentIndex);
        }

        // Auto-wire buttons that live inside the active step
        WireStepLocalButtons(steps[currentIndex]);

        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        bool isFirst = currentIndex == 0;
        bool isLast = currentIndex == steps.Count - 1;

        if (previousButton != null) previousButton.interactable = !isFirst;
        if (nextButton != null) nextButton.gameObject.SetActive(!isLast);
        if (finishButton != null) finishButton.gameObject.SetActive(isLast);
    }

    private void WireStepLocalButtons(GameObject stepRoot)
    {
        // Find any Button components within this step and map by name
        var buttons = stepRoot.GetComponentsInChildren<Button>(true);

        Button localNext = null;
        Button localPrev = null;
        Button localFinish = null;

        foreach (var b in buttons)
        {
            string n = b.name.ToLowerInvariant();
            if (localNext == null && (n.Contains("next") || n == "btnnext")) localNext = b;
            else if (localPrev == null && (n.Contains("prev") || n.Contains("back") || n == "btnprevious")) localPrev = b;
            else if (localFinish == null && (n.Contains("finish") || n.Contains("done") || n.Contains("close"))) localFinish = b;
        }

        if (localNext != null)
        {
            localNext.onClick.RemoveAllListeners();
            localNext.onClick.AddListener(Next);
            localNext.gameObject.SetActive(currentIndex < steps.Count - 1);
        }

        if (localPrev != null)
        {
            localPrev.onClick.RemoveAllListeners();
            localPrev.onClick.AddListener(Previous);
            localPrev.interactable = currentIndex > 0;
        }

        if (localFinish != null)
        {
            localFinish.onClick.RemoveAllListeners();
            localFinish.onClick.AddListener(Finish);
            localFinish.gameObject.SetActive(currentIndex == steps.Count - 1);
        }
    }

    public void Next()
    {
        if (currentIndex < steps.Count - 1)
        {
            ShowStep(currentIndex + 1);
        }
    }

    public void Previous()
    {
        if (currentIndex > 0)
        {
            ShowStep(currentIndex - 1);
        }
    }

    public void Finish()
    {
        if (!string.IsNullOrEmpty(sceneOnFinish))
        {
            SceneManager.LoadScene(sceneOnFinish);
        }
        else
        {
            Debug.LogWarning("[tutorialManager] sceneOnFinish is empty. Staying on current scene.");
        }
    }
}
