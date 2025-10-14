using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using System.Collections.Generic;
using UnityEngine.UI;

public class QrCodeRecenter : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARSession session;
    [SerializeField] private XROrigin sessionOrigin;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private TargetHandler targetHandler;

    [Header("UI Elements")]
    [SerializeField] private Transform indicatorSphere;
    [SerializeField] private GameObject qrCodeScanningPanel;  // Panel with mask shader
    [SerializeField] private Material maskMaterial;           // Material using "UI/HoleMask"
    [SerializeField] private Button scanAgainButton;

    [Header("Minimap")]
    [SerializeField] private Transform minimapCamera;
    [SerializeField] private float minimapHeight = 20f;
    [SerializeField] private bool minimapNorthUp = true;

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader
    {
        AutoRotate = true,
        TryInverted = true,
        Options = new DecodingOptions
        {
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            TryHarder = true
        }
    };

    private bool scanningEnabled = false;
    public static QrCodeRecenter Instance { get; private set; }
    public bool hasScannedSuccessfully { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (scanAgainButton != null)
            scanAgainButton.onClick.AddListener(OnScanAgainClicked);
    }

    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private System.Collections.IEnumerator Start()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        qrCodeScanningPanel.SetActive(true);
        StartScanning();
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!scanningEnabled) return;
        if (!cameraManager.permissionGranted) return;
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();

        if (cameraImageTexture != null)
        {
            Destroy(cameraImageTexture);
            cameraImageTexture = null;
        }

        cameraImageTexture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);

        cameraImageTexture.LoadRawTextureData(buffer);
        cameraImageTexture.Apply();
        buffer.Dispose();

        var result = reader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

        if (result != null)
        {
            hasScannedSuccessfully = true;
            StopScanning();
            SetQrCodeRecenterTarget(result.Text);
        }
    }

    private void SetQrCodeRecenterTarget(string targetText)
    {
        Transform targetTransform = ResolveTargetTransform(targetText);
        if (targetTransform == null) return;

        Vector3 currentCameraWorldPos = sessionOrigin.Camera != null
            ? sessionOrigin.Camera.transform.position
            : sessionOrigin.transform.position;

        Vector3 targetXZWithCurrentY = new Vector3(targetTransform.position.x, currentCameraWorldPos.y, targetTransform.position.z);
        sessionOrigin.MoveCameraToWorldLocation(targetXZWithCurrentY);

        Vector3 desiredForward = targetTransform.forward;
        desiredForward.y = 0f;
        desiredForward = desiredForward.sqrMagnitude < 0.001f ? Vector3.forward : desiredForward.normalized;
        sessionOrigin.MatchOriginUpCameraForward(Vector3.up, desiredForward);

        if (indicatorSphere != null)
        {
            Vector3 indicatorCurrent = indicatorSphere.position;
            indicatorSphere.position = new Vector3(currentCameraWorldPos.x, indicatorCurrent.y, currentCameraWorldPos.z);
            indicatorSphere.rotation = targetTransform.rotation;
        }

        if (minimapCamera != null)
        {
            Vector3 miniCurrent = minimapCamera.position;
            minimapCamera.position = new Vector3(currentCameraWorldPos.x, miniCurrent.y, currentCameraWorldPos.z);
            minimapCamera.rotation = minimapNorthUp
                ? Quaternion.Euler(90f, 0f, 0f)
                : Quaternion.LookRotation(Vector3.down, targetTransform.forward);
        }
    }

    private Transform ResolveTargetTransform(string targetText)
    {
        if (targetHandler != null)
        {
            var viaHandler = targetHandler.GetCurrentTargetByTargetText(targetText);
            if (viaHandler != null) return viaHandler.transform;
        }

        GameObject go = GameObject.Find(targetText);
        if (go != null) return go.transform;

        string wrapped = "Target(" + targetText + ")";
        go = GameObject.Find(wrapped);
        if (go != null) return go.transform;

        int openIdx = targetText.IndexOf('(');
        int closeIdx = targetText.IndexOf(')');
        if (openIdx >= 0 && closeIdx > openIdx)
        {
            string inner = targetText.Substring(openIdx + 1, closeIdx - openIdx - 1);
            go = GameObject.Find(inner);
            if (go != null) return go.transform;
        }

        return null;
    }

public void StartScanning()
{
    scanningEnabled = true;
    hasScannedSuccessfully = false;

    // Show overlay
    if (qrCodeScanningPanel != null)
    {
        qrCodeScanningPanel.SetActive(true);

        // Restore raycast blocking
        var img = qrCodeScanningPanel.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
    }

    // Make dark overlay visible
    if (maskMaterial != null)
    {
        Color c = maskMaterial.GetColor("_Color");
        c.a = 0.7f; // semi-transparent
        maskMaterial.SetColor("_Color", c);
    }
}

public void StopScanning()
{
    scanningEnabled = false;

    // Fade mask out
    if (maskMaterial != null)
    {
        Color c = maskMaterial.GetColor("_Color");
        c.a = 0f;
        maskMaterial.SetColor("_Color", c);
    }

    // Allow clicks to pass through or hide the panel
    if (qrCodeScanningPanel != null)
    {
        var img = qrCodeScanningPanel.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        // Optional: fully hide it if you prefer
        qrCodeScanningPanel.SetActive(false);
    }
}
    private void OnScanAgainClicked()
    {
        StartScanning();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hasScannedSuccessfully = true;
            StopScanning();
            SetQrCodeRecenterTarget("Target(Sogo)");
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            hasScannedSuccessfully = false;
            StartScanning();
            SetQrCodeRecenterTarget("Target(Gate)");
        }
    }
#endif
}
