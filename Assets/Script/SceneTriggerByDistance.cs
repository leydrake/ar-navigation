using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.XR.ARFoundation;

public class SceneTriggerByDistance : MonoBehaviour
{
    [Header("Settings")]
    public TMP_Text distanceText;
    public float arrivalDistance = 1f;
    public string nextSceneName = "ArriveScene";
    public float delayBeforeLoad = 2f;

    [Header("Optional References")]
    public GameObject xrCamera;           // assign AR camera
    public MonoBehaviour distanceUpdater; // script that updates distance

    private bool sceneLoaded = false;

    void Update()
    {
        if (sceneLoaded || distanceText == null)
            return;

        // Manual tester
        if (Input.GetKeyDown(KeyCode.P))
        {
            HandleArrival();
            return;
        }

        // Parse TMP text to get numeric distance
        string cleanText = Regex.Replace(distanceText.text, "<.*?>", ""); // remove tags
        cleanText = Regex.Replace(cleanText, "[^0-9.]", "");              // keep digits/decimal

        if (float.TryParse(cleanText, out float currentDistance))
        {
            if (currentDistance <= arrivalDistance)
            {
                HandleArrival();
            }
        }
    }

    void HandleArrival()
    {
        if (sceneLoaded)
            return;

        sceneLoaded = true;

        // Disable navigation & camera updates
        DisableNavigation();

        // Show arrival message
        distanceText.text = "<size=72><b><color=#00AA00>You've arrived!</color></b></size>";

        // Start delayed scene load
        StartCoroutine(LoadNextScene());
    }

    void DisableNavigation()
    {
        if (distanceUpdater != null && distanceUpdater.enabled)
        {
            distanceUpdater.enabled = false;
            Debug.Log("🛑 Distance updater script disabled instantly.");
        }

        if (xrCamera != null)
        {
            var pose = xrCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            if (pose != null) pose.enabled = false;

            var camMgr = xrCamera.GetComponent<ARCameraManager>();
            if (camMgr != null) camMgr.enabled = false;

            var origin = xrCamera.GetComponentInParent<ARSessionOrigin>();
            if (origin != null) origin.enabled = false;

            Debug.Log("📷 XR camera movement disabled instantly.");
        }
    }

    IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(nextSceneName);
    }
}
