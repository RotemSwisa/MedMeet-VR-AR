using UnityEngine;
using UnityEngine.XR;

// מאפשר תנועה חופשית בזמן צפייה בReplay
public class FreeRoamController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 45f;
    public bool enableTeleport = true;
    public float teleportDistance = 5f;

    [Header("VR References")]
    public Transform cameraRig; // XR Origin או OVRCameraRig
    public Transform vrCamera;

    [Header("Controls")]
    public XRNode leftController = XRNode.LeftHand;
    public XRNode rightController = XRNode.RightHand;

    private bool isActive = false;
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    void Start()
    {
        // אם לא הוגדר, מצא אוטומטית
        if (cameraRig == null)
        {
            // חפש XR Origin או OVRCameraRig
            cameraRig = transform.root;
        }

        if (vrCamera == null)
        {
            vrCamera = Camera.main?.transform;
        }

        // קבל את הControllers
        leftDevice = InputDevices.GetDeviceAtXRNode(leftController);
        rightDevice = InputDevices.GetDeviceAtXRNode(rightController);
    }

    void Update()
    {
        if (!isActive) return;

        // וודא שיש לנו devices
        if (!leftDevice.isValid)
            leftDevice = InputDevices.GetDeviceAtXRNode(leftController);
        if (!rightDevice.isValid)
            rightDevice = InputDevices.GetDeviceAtXRNode(rightController);

        // תנועה עם Thumbstick שמאלי
        HandleMovement();

        // סיבוב עם Thumbstick ימני
        HandleRotation();

        // Teleport עם Grip ימני
        if (enableTeleport)
        {
            HandleTeleport();
        }
    }

    private void HandleMovement()
    {
        // קרא Thumbstick שמאלי
        Vector2 thumbstick;
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
        {
            if (thumbstick.magnitude > 0.1f)
            {
                // תנועה בכיוון המצלמה
                Vector3 forward = vrCamera.forward;
                forward.y = 0; // שמור רק תנועה אופקית
                forward.Normalize();

                Vector3 right = vrCamera.right;
                right.y = 0;
                right.Normalize();

                Vector3 movement = (forward * thumbstick.y + right * thumbstick.x) * moveSpeed * Time.deltaTime;
                cameraRig.position += movement;
            }
        }
    }

    private void HandleRotation()
    {
        // קרא Thumbstick ימני
        Vector2 thumbstick;
        if (rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
        {
            if (Mathf.Abs(thumbstick.x) > 0.5f)
            {
                // סיבוב snap (30 מעלות)
                float rotation = Mathf.Sign(thumbstick.x) * rotationSpeed * Time.deltaTime;
                cameraRig.RotateAround(vrCamera.position, Vector3.up, rotation);
            }
        }
    }

    private void HandleTeleport()
    {
        // לחיצה על Grip ימני
        bool gripPressed;
        if (rightDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripPressed))
        {
            if (gripPressed)
            {
                // טלפורט קדימה
                Vector3 forward = vrCamera.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 targetPosition = cameraRig.position + forward * teleportDistance;

                // בדיקת קרקע (אופציונלי)
                RaycastHit hit;
                if (Physics.Raycast(targetPosition + Vector3.up * 2f, Vector3.down, out hit, 5f))
                {
                    targetPosition.y = hit.point.y;
                }

                cameraRig.position = targetPosition;
            }
        }
    }

    public void EnableFreeRoam()
    {
        isActive = true;
        Debug.Log("FreeRoam: תנועה חופשית הופעלה");
    }

    public void DisableFreeRoam()
    {
        isActive = false;
        Debug.Log("FreeRoam: תנועה חופשית כובתה");
    }

    public void SetActive(bool active)
    {
        isActive = active;
    }
}