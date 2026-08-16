using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using Normal.Realtime;

public class EraserController : MonoBehaviour
{
    [Header("Eraser Settings")]
    [SerializeField] private float eraseRadius = 0.05f;
    [SerializeField] private LayerMask drawingLayer = -1; // Everything by default

    [Header("Eraser Tip Transform")]
    [SerializeField] private Transform eraserTip;

    [Header("Desktop Testing")]
    [SerializeField] private bool enableDesktopMode = true;
    [SerializeField] private Camera desktopCamera;
    [SerializeField] private float holdingDistance = 0.3f;

    [Header("VR Input Settings")]
    [SerializeField] private VREraseButton eraseButton = VREraseButton.OtherHandGrip;

    public enum VREraseButton
    {
        Grip,
        Trigger,
        PrimaryButton,
        SecondaryButton,
        OtherHandGrip
    }

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isErasing = false;
    private bool isGrabbed = false;

    // VR Input tracking
    private XRNode currentHand = XRNode.RightHand;
    private XRInputDevice currentDevice;
    private XRInputDevice otherHandDevice;

    // Desktop variables
    private bool desktopHoldingEraser = false;
    private Vector3 desktopEraserOffset;

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (desktopCamera == null)
        {
            desktopCamera = Camera.main;
        }

        if (eraserTip == null)
        {
            eraserTip = transform;
            Debug.LogWarning("Eraser Tip not assigned! Using eraser position instead.");
        }

        // Make sure eraser doesn't fall
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        // Desktop Mode
        if (enableDesktopMode && !isGrabbed)
        {
            HandleDesktopInput();
        }

        // VR Mode
        if (isGrabbed)
        {
            HandleVRInput();
        }

        // Perform erasing if active
        if (isErasing)
        {
            EraseNearbyLines();
        }
    }

    void HandleDesktopInput()
    {
        // Right click to grab eraser
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = desktopCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    desktopHoldingEraser = true;
                    desktopEraserOffset = transform.position - hit.point;
                }
            }
        }

        // Move eraser
        if (desktopHoldingEraser && Mouse.current.rightButton.isPressed)
        {
            Ray ray = desktopCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 targetPos = ray.origin + ray.direction * holdingDistance;
            transform.position = targetPos + desktopEraserOffset;
        }

        // Release eraser
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            desktopHoldingEraser = false;
            if (isErasing)
            {
                StopErasing();
            }
        }

        // Left click to erase (when holding)
        if (desktopHoldingEraser)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartErasing();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                StopErasing();
            }
        }
    }

    void HandleVRInput()
    {
        if (!currentDevice.isValid)
        {
            currentDevice = InputDevices.GetDeviceAtXRNode(currentHand);
            if (!currentDevice.isValid) return;
        }

        bool buttonPressed = false;

        switch (eraseButton)
        {
            case VREraseButton.Grip:
                float gripValue;
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.grip, out gripValue))
                {
                    buttonPressed = gripValue > 0.7f;
                }
                break;

            case VREraseButton.Trigger:
                float triggerValue;
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.trigger, out triggerValue))
                {
                    buttonPressed = triggerValue > 0.5f;
                }
                break;

            case VREraseButton.PrimaryButton:
                currentDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out buttonPressed);
                break;

            case VREraseButton.SecondaryButton:
                currentDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out buttonPressed);
                break;

            case VREraseButton.OtherHandGrip:
                XRNode otherHand = currentHand == XRNode.RightHand ? XRNode.LeftHand : XRNode.RightHand;

                if (!otherHandDevice.isValid)
                {
                    otherHandDevice = InputDevices.GetDeviceAtXRNode(otherHand);
                }

                if (otherHandDevice.isValid)
                {
                    float otherGrip;
                    if (otherHandDevice.TryGetFeatureValue(XRCommonUsages.grip, out otherGrip))
                    {
                        buttonPressed = otherGrip > 0.5f;
                    }
                }
                break;
        }

        if (buttonPressed && !isErasing)
        {
            StartErasing();
        }
        else if (!buttonPressed && isErasing)
        {
            StopErasing();
        }
    }

    void StartErasing()
    {
        isErasing = true;
        Debug.Log("Eraser: Started erasing");
    }

    void StopErasing()
    {
        isErasing = false;
        Debug.Log("Eraser: Stopped erasing");
    }

    void EraseNearbyLines()
    {
        Vector3 tipPosition = eraserTip.position;

        // Find all colliders near eraser tip
        Collider[] nearbyColliders = Physics.OverlapSphere(tipPosition, eraseRadius, drawingLayer);

        foreach (Collider col in nearbyColliders)
        {
            // Check if this is a drawn line
            LineRenderer line = col.GetComponent<LineRenderer>();
            if (line != null)
            {
                // Check if line is close enough to erase
                if (IsLineTouchingEraser(line, tipPosition))
                {
                    // Destroy via Realtime if it's a synced object
                    RealtimeView rtView = col.GetComponent<RealtimeView>();
                    if (rtView != null)
                    {
                        Realtime.Destroy(col.gameObject);
                        Debug.Log("Eraser: Destroyed synced line");
                    }
                    else
                    {
                        Destroy(col.gameObject);
                        Debug.Log("Eraser: Destroyed local line");
                    }
                }
            }
        }
    }

    bool IsLineTouchingEraser(LineRenderer line, Vector3 eraserPos)
    {
        // Check if any point in the line is within erase radius
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 pointPos = line.transform.TransformPoint(line.GetPosition(i));
            if (Vector3.Distance(pointPos, eraserPos) < eraseRadius)
            {
                return true;
            }
        }
        return false;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        desktopHoldingEraser = false;

        string interactorName = args.interactorObject.transform.name.ToLower();
        if (interactorName.Contains("left"))
        {
            currentHand = XRNode.LeftHand;
        }
        else if (interactorName.Contains("right"))
        {
            currentHand = XRNode.RightHand;
        }

        currentDevice = InputDevices.GetDeviceAtXRNode(currentHand);

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentDevice = new XRInputDevice();
        otherHandDevice = new XRInputDevice();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (isErasing)
        {
            StopErasing();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (eraserTip != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(eraserTip.position, eraseRadius);
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}