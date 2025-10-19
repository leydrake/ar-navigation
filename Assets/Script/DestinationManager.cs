using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DestinationManager : MonoBehaviour
{
    public static string SelectedLocation;
    public static float NavigationStartTime;

    [Header("Editor Simulation Settings")]
    [SerializeField] private string testDestinationName = "Library";
    [SerializeField] private string arrivalSceneName = "ArriveScene";

    private void Update()
    {
#if UNITY_EDITOR
        // Press P in Play mode to simulate arrival
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (string.IsNullOrEmpty(SelectedLocation))
                SelectedLocation = testDestinationName;

           
            
            SceneManager.LoadScene(arrivalSceneName);
        }
#endif
    }

    public void OnLocationSelected()
    {
        var button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (button == null)
        {
            return;
        }

        // Read the name from UI text
        string name = null;
        var text = button.GetComponentInChildren<Text>();
        if (text != null)
            name = text.text;
        else
        {
            var tmp = button.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                name = tmp.text;
        }

        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        SelectedLocation = name;
        NavigationStartTime = Time.time; // Set actual navigation start time
        Debug.Log($"📍 Started navigating to: {SelectedLocation}");

        // Load your navigation scene here if needed
        // SceneManager.LoadScene("NavigationSceneName");
    }

#if UNITY_EDITOR
    [ContextMenu("Simulate Arrival (Editor Only)")]
    public void SimulateArrivalEditor()
    {
        if (string.IsNullOrEmpty(SelectedLocation))
            SelectedLocation = testDestinationName;

       

        SceneManager.LoadScene(arrivalSceneName);
    }
#endif
}
