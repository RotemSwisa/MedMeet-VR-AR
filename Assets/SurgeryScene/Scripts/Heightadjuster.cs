using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

// שים סקריפט זה על XR Origin (VR)
public class HeightAdjuster : MonoBehaviour
{
    [Header("Height Settings (כפתורי A/B ימין)")]
    public float heightStep = 0.1f;
    public float minHeight = -1f;
    public float maxHeight = 1f;

    [Header("Camera Tilt Settings (כפתורי X/Y שמאל)")]
    public float tiltStep = 10f;
    public float minTilt = -60f;
    public float maxTilt = 60f;

    private XROrigin xrOrigin;
    private GameObject cameraOffset;
    private float currentHeight = 0f;
    private float currentTilt = 0f;

    private bool aWasPressed = false;
    private bool bWasPressed = false;
    private bool xWasPressed = false;
    private bool yWasPressed = false;

    void Start()
    {
        xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin != null)
            cameraOffset = xrOrigin.CameraFloorOffsetObject;
    }

    void Update()
    {
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        // B - עלה
        right.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed);
        if (bPressed && !bWasPressed)
        {
            currentHeight = Mathf.Clamp(currentHeight + heightStep, minHeight, maxHeight);
            ApplyHeight();
        }
        bWasPressed = bPressed;

        // A - רד
        right.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed);
        if (aPressed && !aWasPressed)
        {
            currentHeight = Mathf.Clamp(currentHeight - heightStep, minHeight, maxHeight);
            ApplyHeight();
        }
        aWasPressed = aPressed;

        // Y - הסתכל למעלה
        left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yPressed);
        if (yPressed && !yWasPressed)
        {
            currentTilt = Mathf.Clamp(currentTilt - tiltStep, minTilt, maxTilt);
            ApplyTilt();
        }
        yWasPressed = yPressed;

        // X - הסתכל למטה
        left.TryGetFeatureValue(CommonUsages.primaryButton, out bool xPressed);
        if (xPressed && !xWasPressed)
        {
            currentTilt = Mathf.Clamp(currentTilt + tiltStep, minTilt, maxTilt);
            ApplyTilt();
        }
        xWasPressed = xPressed;
    }

    void ApplyHeight()
    {
        if (cameraOffset == null) return;
        Vector3 pos = cameraOffset.transform.localPosition;
        pos.y = currentHeight;
        cameraOffset.transform.localPosition = pos;
    }

    void ApplyTilt()
    {
        if (cameraOffset == null) return;
        // שומר על הזווית כמספר ישיר ומיישם על Camera Offset
        cameraOffset.transform.localEulerAngles = new Vector3(currentTilt,
            cameraOffset.transform.localEulerAngles.y,
            cameraOffset.transform.localEulerAngles.z);
    }
}