using UnityEngine;
using TMPro;  // Required if using TextMeshPro
using UnityEngine.UI;
using System.Collections;

public class ARCalibrator : MonoBehaviour
{
    public GameObject overlayPanel; // Assign BlackOverlay
    public TextMeshProUGUI countdownText; // Assign the text

    public int countdownTime = 10;

    void Start()
    {
        overlayPanel.SetActive(true);
        StartCoroutine(CountdownCoroutine());
    }

    IEnumerator CountdownCoroutine()
    {
        int timer = countdownTime;

        while (timer > 0)
        {
            countdownText.text = "AR Calibrating\n" + timer;
            yield return new WaitForSeconds(1f);
            timer--;
        }

        countdownText.text = "AR Calibrating\n0";

        // Hide overlay after countdown
        overlayPanel.SetActive(false);
    }
}
