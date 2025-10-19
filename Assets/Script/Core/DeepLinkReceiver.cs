using UnityEngine;
using System.Collections;

public class DeepLinkReceiver : MonoBehaviour
{
    private string pendingData = null;
    private bool deepLinkProcessed = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.deepLinkActivated += OnDeepLinkActivated;

        // Handle deep link if app started with one
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    private void OnDeepLinkActivated(string url)
    {
        Debug.Log($"[DeepLinkReceiver] Deep link opened: {url}");
        pendingData = ParseDeepLink(url);
        deepLinkProcessed = false;

        if (!string.IsNullOrEmpty(pendingData))
            StartCoroutine(WaitForRecenterAndSet());
    }

    private string ParseDeepLink(string url)
    {
        // Adjust to your own URL paths
        if (url.Contains("navigatemycampus.capstone-two.com/gate"))
            return "Gate";
        if (url.Contains("navigatemycampus.capstone-two.com/open"))
            return "Gate";
        if (url.Contains("navigatemycampus.capstone-two.com/library"))
            return "Library";

        return null;
    }

    private IEnumerator WaitForRecenterAndSet()
    {
        // Keep waiting until QrCodeRecenter.Instance exists
        while (QrCodeRecenter.Instance == null)
        {
            yield return null;
        }

        if (!deepLinkProcessed && QrCodeRecenter.Instance != null && !string.IsNullOrEmpty(pendingData))
        {
            deepLinkProcessed = true;
            Debug.Log($"[DeepLinkReceiver] Sending deep link data to QrCodeRecenter: {pendingData}");
            QrCodeRecenter.Instance.SetQrCodeRecenterTarget($"Target({pendingData})");
        }
    }
}
