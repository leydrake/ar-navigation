using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using System.Collections.Generic;
using UnityEngine.UI;
using System; // added for Uri parsing

public class QrCodeRecenter : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARSession session;
    [SerializeField] private XROrigin sessionOrigin;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private ARAnchorManager anchorManager;

    [Header("UI Elements")]
    [SerializeField] private Transform indicatorSphere;
    [SerializeField] private GameObject qrCodeScanningPanel;
    [SerializeField] private Material maskMaterial;
    [SerializeField] private Button scanAgainButton;
    [SerializeField] private Button rescanButton;

    [Header("Minimap")]
    [SerializeField] private Transform minimapCamera;
    [SerializeField] private float minimapHeight = 20f;
    [SerializeField] private bool minimapNorthUp = true;

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader;
    private bool scanningEnabled = false;
    private ARAnchor currentAnchor;
    private bool isInitialized = false;
    
    public static QrCodeRecenter Instance { get; private set; }
    public bool hasScannedSuccessfully { get; private set; } = false;

    private readonly Vector3 defaultPosition = new Vector3(0, 1, 0);
    private readonly Quaternion defaultRotation = Quaternion.identity;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        reader = new BarcodeReader
        {
            AutoRotate = true,
            TryInverted = true,
            Options = new DecodingOptions
            {
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };

        if (scanAgainButton != null)
            scanAgainButton.onClick.AddListener(OnScanAgainClicked);

        if (rescanButton != null)
            rescanButton.onClick.AddListener(OnRescanClicked);
    }

    private void OnEnable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private System.Collections.IEnumerator Start()
    {
        // Wait for camera permission
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        yield return new WaitForSeconds(0.5f);

        InitializeDefaultPositions();
        isInitialized = true;

        if (qrCodeScanningPanel != null)
            qrCodeScanningPanel.SetActive(true);
            
        StartScanning();
    }

    private void InitializeDefaultPositions()
    {
        if (sessionOrigin != null)
        {
            sessionOrigin.transform.position = Vector3.zero;
            sessionOrigin.transform.rotation = Quaternion.identity;
            
            sessionOrigin.MoveCameraToWorldLocation(defaultPosition);
            sessionOrigin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
        }

        if (indicatorSphere != null)
        {
            indicatorSphere.position = defaultPosition;
            indicatorSphere.rotation = defaultRotation;
        }

        if (minimapCamera != null)
        {
            minimapCamera.position = new Vector3(defaultPosition.x, minimapHeight, defaultPosition.z);
            minimapCamera.rotation = minimapNorthUp 
                ? Quaternion.Euler(90f, 0f, 0f) 
                : Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!isInitialized || !scanningEnabled || !cameraManager.permissionGranted) return;
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

        if (cameraImageTexture != null) Destroy(cameraImageTexture);

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

    public void SetQrCodeRecenterTarget(string targetText)
    {
        // ✅ Extract target if the scanned text is a URL
        string extractedTarget = ExtractTargetFromUrl(targetText);

        Transform targetTransform = ResolveTargetTransform(extractedTarget);
        if (targetTransform == null)
        {
            Debug.LogWarning($"QR target '{extractedTarget}' not found.");
            return;
        }

        if (currentAnchor != null)
            Destroy(currentAnchor.gameObject);

        Vector3 cameraPos = sessionOrigin.Camera.transform.position;
        Vector3 targetXZ = new Vector3(targetTransform.position.x, cameraPos.y, targetTransform.position.z);

        if (anchorManager != null)
            currentAnchor = anchorManager.AddAnchor(new Pose(targetXZ, targetTransform.rotation));

        StartCoroutine(SmoothRecenter(targetXZ, targetTransform.forward));
        UpdateMinimapAndIndicator(targetTransform, cameraPos);
    }

    // ✅ New method to parse the "target" parameter from a full URL
    private string ExtractTargetFromUrl(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
        {
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query))
            {
                var queryParams = query.TrimStart('?').Split('&');
                foreach (var param in queryParams)
                {
                    var parts = param.Split('=');
                    if (parts.Length == 2 && parts[0].ToLower() == "target")
                    {
                        return parts[1];
                    }
                }
            }
        }
        // if not a URL, return the original text
        return input;
    }

    private System.Collections.IEnumerator SmoothRecenter(Vector3 targetPos, Vector3 forward)
    {
        Vector3 startPos = sessionOrigin.Camera.transform.position;
        Quaternion startRot = sessionOrigin.Camera.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

        float t = 0;
        const float duration = 0.3f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            sessionOrigin.MoveCameraToWorldLocation(Vector3.Lerp(startPos, targetPos, smoothT));
            sessionOrigin.MatchOriginUpCameraForward(Vector3.up, Quaternion.Slerp(startRot, targetRot, smoothT) * Vector3.forward);
            yield return null;
        }

        sessionOrigin.MoveCameraToWorldLocation(targetPos);
        sessionOrigin.MatchOriginUpCameraForward(Vector3.up, forward);
    }

    private void UpdateMinimapAndIndicator(Transform targetTransform, Vector3 cameraWorldPos)
    {
        if (indicatorSphere != null)
        {
            Vector3 indicatorPos = indicatorSphere.position;
            indicatorSphere.position = new Vector3(cameraWorldPos.x, indicatorPos.y, cameraWorldPos.z);
            indicatorSphere.rotation = targetTransform.rotation;
        }

        if (minimapCamera != null)
        {
            Vector3 miniPos = minimapCamera.position;
            minimapCamera.position = new Vector3(cameraWorldPos.x, miniPos.y, cameraWorldPos.z);
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

        GameObject go = GameObject.Find(targetText) ?? GameObject.Find($"Target({targetText})");

        if (go == null)
        {
            int openIdx = targetText.IndexOf('(');
            int closeIdx = targetText.IndexOf(')');
            if (openIdx >= 0 && closeIdx > openIdx)
            {
                string inner = targetText.Substring(openIdx + 1, closeIdx - openIdx - 1);
                go = GameObject.Find(inner);
            }
        }

        return go?.transform;
    }

    public void StartScanning()
    {
        if (!isInitialized) return;
        
        scanningEnabled = true;
        hasScannedSuccessfully = false;

        if (qrCodeScanningPanel != null)
        {
            qrCodeScanningPanel.SetActive(true);
            var img = qrCodeScanningPanel.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
        }

        if (maskMaterial != null)
        {
            Color c = maskMaterial.GetColor("_Color");
            c.a = 0.7f;
            maskMaterial.SetColor("_Color", c);
        }
    }

    public void StopScanning()
    {
        scanningEnabled = false;

        if (maskMaterial != null)
        {
            Color c = maskMaterial.GetColor("_Color");
            c.a = 0f;
            maskMaterial.SetColor("_Color", c);
        }

        if (qrCodeScanningPanel != null)
        {
            var img = qrCodeScanningPanel.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
            qrCodeScanningPanel.SetActive(false);
        }
    }

    private void OnScanAgainClicked() => StartScanning();

    private void OnRescanClicked()
    {
        hasScannedSuccessfully = false;
        scanningEnabled = false;

        if (currentAnchor != null)
            Destroy(currentAnchor.gameObject);

        if (session != null)
            session.Reset();

        StartCoroutine(ResetToDefaults());
    }

    private System.Collections.IEnumerator ResetToDefaults()
    {
        yield return new WaitForSeconds(0.2f);
        InitializeDefaultPositions();
        yield return new WaitForSeconds(0.1f);
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
            hasScannedSuccessfully = true;
            StopScanning();
            SetQrCodeRecenterTarget("Target(Gate)");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnRescanClicked();
        }
    }
#endif
}
