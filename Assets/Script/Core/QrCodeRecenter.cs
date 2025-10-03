using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;
using System.Collections.Generic;

public class QrCodeRecenter : MonoBehaviour {

    [SerializeField]
    private ARSession session;
    [SerializeField]
    private XROrigin sessionOrigin;
    [SerializeField]
    private ARCameraManager cameraManager;
    [SerializeField]
    private TargetHandler targetHandler;
    [SerializeField]
    private Transform indicatorSphere;
    [SerializeField]
    private GameObject qrCodeScanningPanel;
    [SerializeField]
    private Transform minimapCamera; // optional: top-down minimap camera
    [SerializeField]
    private float minimapHeight = 20f; // height above target for minimap camera
    [SerializeField]
    private bool minimapNorthUp = true; // keep minimap facing world north if true

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader
    {
        AutoRotate = true,
        TryInverted = true,
        Options = new DecodingOptions
        {
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            TryHarder = true,
            ReturnCodabarStartEnd = false,
        }
    }; // configured QR code reader instance
    private bool scanningEnabled = false;
public static QrCodeRecenter Instance { get; private set; }
    public bool hasScannedSuccessfully { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }
    private void OnEnable() {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable() {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private System.Collections.IEnumerator Start()
    {
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        // Show prompt and begin scanning at startup
        qrCodeScanningPanel.SetActive(true);
        StartScanning();
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs) {

        if (!scanningEnabled) {
            return;
        }

        if (!cameraManager.permissionGranted) {
            return;
        }

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) {
            return;
        }

        var conversionParams = new XRCpuImage.ConversionParams {
            // Get the entire image.
            inputRect = new RectInt(0, 0, image.width, image.height),

            // Downsample by 2.
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // Choose RGBA format.
            outputFormat = TextureFormat.RGBA32,

            // Flip across the vertical axis (mirror image).
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // See how many bytes you need to store the final image.
        int size = image.GetConvertedDataSize(conversionParams);

        // Allocate a buffer to store the image.
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // Extract the image data
        image.Convert(conversionParams, buffer);

        // The image was converted to RGBA32 format and written into the provided buffer
        // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
        image.Dispose();

        // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // In this example, you apply it to a texture to visualize it.

        // You've got the data; let's put it into a texture so you can visualize it.
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

        // Done with your temporary data, so you can dispose it.
        buffer.Dispose();

        // Detect and decode the barcode inside the bitmap
        var result = reader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

        // Do something with the result
        if (result != null) {
            hasScannedSuccessfully = true;
            SetQrCodeRecenterTarget(result.Text);
            StopScanning();
        }
    }

    private void SetQrCodeRecenterTarget(string targetText) {
        Transform targetTransform = ResolveTargetTransform(targetText);
        if (targetTransform == null) {
			Debug.LogWarning($"QrCodeRecenter: No target found for '{targetText}'. Ensure the QR text matches a GameObject name or handler mapping.");
            return;
        }

		// Do NOT reset AR session here; resetting can cause vertical (Y) shifts depending on provider

		// Move the AR camera to the target position (preserve current Y) and align forward horizontally
		// This computes and applies the necessary origin transform internally
		Vector3 currentCameraWorldPos = sessionOrigin.Camera != null ? sessionOrigin.Camera.transform.position : sessionOrigin.transform.position;
		Vector3 targetXZWithCurrentY = new Vector3(targetTransform.position.x, currentCameraWorldPos.y, targetTransform.position.z);
		sessionOrigin.MoveCameraToWorldLocation(targetXZWithCurrentY);
		Vector3 desiredForward = targetTransform.forward;
		desiredForward.y = 0f;
		if (desiredForward.sqrMagnitude < 0.0001f) {
			desiredForward = Vector3.forward; // fallback to world forward
		}
		desiredForward.Normalize();
		sessionOrigin.MatchOriginUpCameraForward(Vector3.up, desiredForward);

		Debug.Log($"QrCodeRecenter: Recentered to target '{targetTransform.name}'.");

		// Move indicator sphere to camera XZ (preserve its Y) so it matches what the user sees
		if (indicatorSphere != null) {
			Vector3 indicatorCurrent = indicatorSphere.position;
			indicatorSphere.position = new Vector3(currentCameraWorldPos.x, indicatorCurrent.y, currentCameraWorldPos.z);
			indicatorSphere.rotation = targetTransform.rotation;
		}

		// Position the minimap camera to camera XZ (preserve its Y) so it overlays correctly
		if (minimapCamera != null) {
			Vector3 miniCurrent = minimapCamera.position;
			minimapCamera.position = new Vector3(currentCameraWorldPos.x, miniCurrent.y, currentCameraWorldPos.z);
            if (minimapNorthUp) {
                minimapCamera.rotation = Quaternion.Euler(90f, 0f, 0f);
            } else {
                minimapCamera.rotation = Quaternion.LookRotation(Vector3.down, targetTransform.forward);
            }
        }
    }

    private Transform ResolveTargetTransform(string targetText)
    {
        // Try handler first
        if (targetHandler != null)
        {
            TargetFacade viaHandler = targetHandler.GetCurrentTargetByTargetText(targetText);
            if (viaHandler != null) return viaHandler.transform;
        }

        // Try exact GameObject name
        GameObject go = GameObject.Find(targetText);
        if (go != null) return go.transform;

        // If text is "Sogo", try "Target(Sogo)"
        string wrapped = "Target(" + targetText + ")";
        go = GameObject.Find(wrapped);
        if (go != null) return go.transform;

        // If text looks like "Target(Name)", also try just the inner name
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

    public void ChangeActiveFloor(string floorEntrance) {
        SetQrCodeRecenterTarget(floorEntrance);
    }

    public void StartScanning() {
        scanningEnabled = true;
        qrCodeScanningPanel.SetActive(true);
    }

    public void StopScanning() {
        scanningEnabled = false;
        qrCodeScanningPanel.SetActive(false);
    }

    public void ToggleScanning() {
        if (scanningEnabled) {
            StopScanning();
        } else {
            StartScanning();
        }
    }
#if UNITY_EDITOR
	private void Update() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			hasScannedSuccessfully = true;
			StopScanning();
			SetQrCodeRecenterTarget("Target(Sogo)");
		}
	}
#endif
}