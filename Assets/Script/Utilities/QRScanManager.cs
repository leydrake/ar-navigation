using UnityEngine;

public class QRScanManager : MonoBehaviour
{
    public GameObject warningPanel;

    void Update()
    {
        // Wait for QRCodeRecenter to exist
        if (QrCodeRecenter.Instance == null) return;

        // Show warning if not scanned yet
        bool scanned = QrCodeRecenter.Instance.hasScannedSuccessfully;
        warningPanel.SetActive(!scanned);
    }
}
