using Normal.Realtime;
using UnityEngine;
using UnityEngine.XR;

public class VRHandTracker : MonoBehaviour
{
    [Header("Avatar Hands - גרור מה-Hierarchy של ה-Avatar")]
    public Transform leftHand;   // Left Hand מתוך ה-Avatar שלך
    public Transform rightHand;  // Right Hand מתוך ה-Avatar שלך

    [Header("Offsets (התאמה עדינה)")]
    public Vector3 leftHandOffset = Vector3.zero;
    public Vector3 rightHandOffset = Vector3.zero;
    public Vector3 leftHandRotationOffset = Vector3.zero;
    public Vector3 rightHandRotationOffset = Vector3.zero;

    [Header("Smoothing")]
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 10f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Controllers שנמצא אוטומטית
    private Transform leftController;
    private Transform rightController;

    // XR Input
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    // ── Normcore ownership — only the local player's tracker should run ──
    private bool _isLocalPlayer = true;

    void Start()
    {
        // If there is a RealtimeView in the hierarchy and it belongs to a remote
        // player, disable all XR-device reads on this machine — those devices
        // belong to the LOCAL player, not to whoever owns this avatar.
        var rv = GetComponentInParent<RealtimeView>();
        if (rv != null)
        {
            _isLocalPlayer = rv.isOwnedLocallyInHierarchy;
            if (!_isLocalPlayer)
            {
                if (showDebugInfo)
                    Debug.Log($"[VRHandTracker] Remote avatar — tracker disabled on {gameObject.name}");
                enabled = false;   // no Update / LateUpdate will run for remote avatars
                return;
            }
        }

        FindControllers();
        InitializeXRDevices();
    }

    void FindControllers()
    {
        // חיפוש XR Origin/XR Rig בסצנה
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // חיפוש לפי שמות נפוצים
            if (obj.name.Contains("LeftHand") || obj.name.Contains("Left Hand Controller") || obj.name.Contains("LeftController"))
            {
                leftController = obj.transform;
                if (showDebugInfo) Debug.Log("Found Left Controller: " + obj.name);
            }

            if (obj.name.Contains("RightHand") || obj.name.Contains("Right Hand Controller") || obj.name.Contains("RightController"))
            {
                rightController = obj.transform;
                if (showDebugInfo) Debug.Log("Found Right Controller: " + obj.name);
            }
        }

        // אם לא מצאנו, נסה למצוא דרך XR Origin
        if (leftController == null || rightController == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("[XR Rig]") ?? GameObject.Find("XR Rig");

            if (xrOrigin != null)
            {
                Transform[] children = xrOrigin.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("left") && child.name.ToLower().Contains("hand"))
                        leftController = child;
                    if (child.name.ToLower().Contains("right") && child.name.ToLower().Contains("hand"))
                        rightController = child;
                }
            }
        }

        if (leftController == null && showDebugInfo)
            Debug.LogWarning("VRHandTracker: Left Controller not found!");
        if (rightController == null && showDebugInfo)
            Debug.LogWarning("VRHandTracker: Right Controller not found!");
    }

    void InitializeXRDevices()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        // וידוא שהמכשירים פעילים
        if (!leftDevice.isValid)
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightDevice.isValid)
            rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void LateUpdate()
    {
        if (leftController != null && leftHand != null)
        {
            UpdateHandTransform(leftHand, leftController, leftHandOffset, leftHandRotationOffset);
        }
        else if (leftDevice.isValid && leftHand != null)
        {
            UpdateHandFromXRDevice(leftHand, leftDevice, leftHandOffset, leftHandRotationOffset);
        }

        if (rightController != null && rightHand != null)
        {
            UpdateHandTransform(rightHand, rightController, rightHandOffset, rightHandRotationOffset);
        }
        else if (rightDevice.isValid && rightHand != null)
        {
            UpdateHandFromXRDevice(rightHand, rightDevice, rightHandOffset, rightHandRotationOffset);
        }
    }

    void UpdateHandTransform(Transform hand, Transform controller, Vector3 posOffset, Vector3 rotOffset)
    {
        // עדכון מיקום
        Vector3 targetPosition = controller.position + controller.TransformDirection(posOffset);
        hand.position = Vector3.Lerp(hand.position, targetPosition, Time.deltaTime * positionSmoothing);

        // עדכון סיבוב
        Quaternion targetRotation = controller.rotation * Quaternion.Euler(rotOffset);
        hand.rotation = Quaternion.Slerp(hand.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
    }

    void UpdateHandFromXRDevice(Transform hand, InputDevice device, Vector3 posOffset, Vector3 rotOffset)
    {
        Vector3 position;
        Quaternion rotation;

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
        {
            // המרה לקואורדינטות עולם
            Vector3 targetPosition = position + rotation * posOffset;
            hand.position = Vector3.Lerp(hand.position, targetPosition, Time.deltaTime * positionSmoothing);

            Quaternion targetRotation = rotation * Quaternion.Euler(rotOffset);
            hand.rotation = Quaternion.Slerp(hand.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
        }
    }
}