using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Normal.Realtime;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class PenController_Alternative : MonoBehaviour
{
    [Header("Drawing Settings")]
    [SerializeField] private GameObject drawingDotPrefab; // NEW: Use dots instead of lines
    [SerializeField] private float minDistanceBetweenPoints = 0.01f;
    [SerializeField] private Color penColor = Color.blue;
    [SerializeField] private float penWidth = 0.01f;

    [Header("Pen Tip Transform")]
    [SerializeField] private Transform penTip;

    [Header("Desktop Testing")]
    [SerializeField] private bool enableDesktopMode = true;
    [SerializeField] private Camera desktopCamera;
    [SerializeField] private float drawingDistance = 0.3f;

    [Header("Color Picker UI")]
    [SerializeField] private PenColorPickerUI colorPickerUI;

    [Header("VR Input Settings")]
    [SerializeField] private VRDrawButton drawButton = VRDrawButton.OtherHandGrip;

    public enum VRDrawButton
    {
        Grip,           // Side button (same as grab)
        Trigger,        // Index finger trigger
        PrimaryButton,  // A (right) or X (left)
        SecondaryButton, // B (right) or Y (left)
        OtherHandGrip   // Grip button on the OTHER hand (not holding pen)
    }

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private LineRenderer currentLine;
    private bool isDrawing = false;
    private Vector3 lastDrawnPosition;
    private bool isGrabbed = false;

    // Realtime reference
    private Realtime realtimeInstance;

    // VR Input tracking
    private XRNode currentHand = XRNode.RightHand;
    private XRInputDevice currentDevice;
    private XRInputDevice otherHandDevice;

    // Desktop variables
    private bool desktopHoldingPen = false;
    private Vector3 desktopPenOffset;

    // Drawing container
    private int currentStrokeID;
    private int currentPointIndex;

    void Start()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Find Realtime instance
        realtimeInstance = FindObjectOfType<Realtime>();

        Debug.Log("=== PEN CONTROLLER DEBUG ===");
        Debug.Log($"Realtime found: {realtimeInstance != null}");

        if (realtimeInstance == null)
        {
            Debug.LogError("PenController: No Realtime instance found in scene!");
        }
        else
        {
            Debug.Log($"Realtime connected: {realtimeInstance.connected}");
            Debug.Log($"Realtime room: {realtimeInstance.room?.name}");
            Debug.Log($"Realtime client ID: {realtimeInstance.clientID}");
        }

        //Debug.Log($"Line Prefab: {lineRendererPrefab?.name}");
        //Debug.Log($"Line Prefab path: Assets/Resources/{lineRendererPrefab?.name}.prefab");

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (desktopCamera == null)
        {
            desktopCamera = Camera.main;
        }

        if (penTip == null)
        {
            Debug.LogWarning("Pen Tip not assigned! Using pen position instead.");
        }

        // Make sure pen doesn't fall
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        // Desktop Mode (for Editor testing)
        if (enableDesktopMode && !isGrabbed)
        {
            HandleDesktopInput();
        }

        // VR Mode
        if (isGrabbed)
        {
            HandleVRInput();
        }

        // Continue drawing if active
        if (isDrawing)
        {
            Vector3 tipPosition = GetTipPosition();

            if (Vector3.Distance(tipPosition, lastDrawnPosition) > minDistanceBetweenPoints)
            {
                AddPointToLine(tipPosition);
                lastDrawnPosition = tipPosition;
            }
        }
    }

    void HandleDesktopInput()
    {
        // Right click to grab pen
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Ray ray = desktopCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    desktopHoldingPen = true;
                    desktopPenOffset = transform.position - hit.point;
                    Debug.Log("Desktop: Grabbed pen");
                }
            }
        }

        // Move pen while holding right click
        if (desktopHoldingPen && Mouse.current.rightButton.isPressed)
        {
            Ray ray = desktopCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Vector3 targetPos = ray.origin + ray.direction * drawingDistance;
            transform.position = targetPos + desktopPenOffset;
        }

        // Release pen
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            desktopHoldingPen = false;
            Debug.Log("Desktop: Released pen");
            if (isDrawing)
            {
                StopDrawing();
            }
        }

        // M key to draw (when holding pen)
        if (desktopHoldingPen)
        {
            // M key for drawing
            if (Keyboard.current[Key.M].wasPressedThisFrame)
            {
                StartDrawing();
                Debug.Log("Desktop: Started drawing with M key");
            }

            if (Keyboard.current[Key.M].wasReleasedThisFrame)
            {
                StopDrawing();
                Debug.Log("Desktop: Stopped drawing");
            }

            // C key for color picker
            if (Keyboard.current[Key.C].wasPressedThisFrame)
            {
                ToggleColorPicker();
            }
        }
    }

    void HandleVRInput()
    {
        if (!currentDevice.isValid)
        {
            currentDevice = InputDevices.GetDeviceAtXRNode(currentHand);
            if (!currentDevice.isValid)
            {
                Debug.LogWarning($"No valid device found for {currentHand}");
                return;
            }
        }

        // Choose which button to use for drawing based on setting
        float drawValue = 0f;
        bool buttonPressed = false;
        string buttonName = "";

        switch (drawButton)
        {
            case VRDrawButton.Grip:
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.grip, out drawValue))
                {
                    buttonPressed = drawValue > 0.7f; // Higher threshold for grip
                    buttonName = "GRIP";
                }
                break;

            case VRDrawButton.Trigger:
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.trigger, out drawValue))
                {
                    buttonPressed = drawValue > 0.5f;
                    buttonName = "TRIGGER";
                }
                break;

            case VRDrawButton.PrimaryButton:
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out buttonPressed))
                {
                    buttonName = currentHand == XRNode.RightHand ? "A Button" : "X Button";
                }
                break;

            case VRDrawButton.SecondaryButton:
                if (currentDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out buttonPressed))
                {
                    buttonName = currentHand == XRNode.RightHand ? "B Button" : "Y Button";
                }
                break;

            case VRDrawButton.OtherHandGrip:
                // Get the OTHER hand's device
                XRNode otherHand = currentHand == XRNode.RightHand ? XRNode.LeftHand : XRNode.RightHand;

                if (!otherHandDevice.isValid)
                {
                    otherHandDevice = InputDevices.GetDeviceAtXRNode(otherHand);
                }

                if (otherHandDevice.isValid)
                {
                    if (otherHandDevice.TryGetFeatureValue(XRCommonUsages.grip, out drawValue))
                    {
                        buttonPressed = drawValue > 0.5f; // Lower threshold since it's just a trigger
                        buttonName = $"OTHER HAND ({otherHand}) GRIP";
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not find other hand device: {otherHand}");
                }
                break;
        }

        // Handle drawing state
        if (buttonPressed && !isDrawing)
        {
            StartDrawing();
            Debug.Log($"VR: Started drawing via {buttonName}");
        }
        else if (!buttonPressed && isDrawing)
        {
            StopDrawing();
            Debug.Log($"VR: Stopped drawing");
        }

        // Primary button (A/X) for color picker - only if not using it for drawing
        if (drawButton != VRDrawButton.PrimaryButton)
        {
            bool colorPickerButton;
            if (currentDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out colorPickerButton))
            {
                if (colorPickerButton)
                {
                    ToggleColorPicker();
                    Debug.Log("VR: Toggled color picker");
                }
            }
        }
    }

    void StartDrawing()
    {
        if (drawingDotPrefab == null)
        {
            Debug.LogError("DrawingDot Prefab not assigned!");
            return;
        }

        if (realtimeInstance == null)
        {
            Debug.LogError("Realtime instance not found!");
            return;
        }

        if (!realtimeInstance.connected)
        {
            Debug.LogWarning("Not connected to Realtime room.");
            return;
        }

        // Create unique stroke ID using timestamp + random
        currentStrokeID = (int)(System.DateTime.Now.Ticks % 100000000) + Random.Range(0, 1000);
        currentPointIndex = 0;

        isDrawing = true;
        lastDrawnPosition = GetTipPosition();

        // Add first dot
        AddPointToLine(lastDrawnPosition);

        Debug.Log($"Started drawing stroke {currentStrokeID}!");
    }

    void StopDrawing()
    {
        isDrawing = false;
        currentLine = null;
        currentPointIndex = 0;
        currentStrokeID = 0; // ← Reset stroke ID so next drawing is a new stroke!
        Debug.Log("Stopped drawing - stroke ended");
    }

    void AddPointToLine(Vector3 position)
    {
        if (!isDrawing || drawingDotPrefab == null || realtimeInstance == null)
            return;

        // Create a dot at this position
        GameObject dotObj = Realtime.Instantiate(
            prefabName: drawingDotPrefab.name,
            position: position,
            rotation: Quaternion.identity,
            ownedByClient: true,
            preventOwnershipTakeover: false,
            useInstance: realtimeInstance
        );

        if (dotObj != null)
        {
            // Set color, size, and stroke info
            DrawingDot dot = dotObj.GetComponent<DrawingDot>();
            if (dot != null)
            {
                dot.SetColor(penColor);
                dot.SetSize(penWidth);
                dot.SetStrokeInfo(currentStrokeID, currentPointIndex);

                Debug.Log($"Created dot: {dotObj.name}, StrokeID: {currentStrokeID}, Index: {currentPointIndex}");
            }
            else
            {
                Debug.LogError($"Dot created but no DrawingDot component found on {dotObj.name}!");
            }

            // ✨ הקלט את הנקודה!
            if (DynamicObjectRecorder.Instance != null && DynamicObjectRecorder.Instance.IsRecording)
            {
                DynamicObjectRecorder.Instance.RecordDrawingDot(
                    position,
                    penColor,
                    penWidth,
                    currentStrokeID,
                    currentPointIndex
                );
            }
            // ✨ הקלט את הנקודה!
            if (DynamicObjectRecorder.Instance != null && DynamicObjectRecorder.Instance.IsRecording)
            {
                DynamicObjectRecorder.Instance.RecordDrawingDot(
                    position,
                    penColor,
                    penWidth,
                    currentStrokeID,
                    currentPointIndex
                );
            }
            currentPointIndex++;
        }
        else
        {
            Debug.LogError("Failed to instantiate dot!");
        }
    }

    Vector3 GetTipPosition()
    {
        return penTip != null ? penTip.position : transform.position;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        desktopHoldingPen = false;

        // Determine which hand grabbed it
        string interactorName = args.interactorObject.transform.name.ToLower();
        if (interactorName.Contains("left"))
        {
            currentHand = XRNode.LeftHand;
            Debug.Log("VR: Grabbed with LEFT hand");
        }
        else if (interactorName.Contains("right"))
        {
            currentHand = XRNode.RightHand;
            Debug.Log("VR: Grabbed with RIGHT hand");
        }

        // Get the device
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
        otherHandDevice = new XRInputDevice(); // Reset other hand too

        Debug.Log("VR: Pen released");

        // Hide color picker when releasing pen
        if (colorPickerUI != null)
        {
            colorPickerUI.HideUI();
        }

        // Make pen "float" where released
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Stop drawing if was drawing
        if (isDrawing)
        {
            StopDrawing();
        }
    }

    void ToggleColorPicker()
    {
        if (colorPickerUI != null)
        {
            colorPickerUI.ToggleUI();
        }
    }

    // Public methods for color picker
    public void SetPenColor(Color newColor)
    {
        penColor = newColor;
    }

    public void SetPenWidth(float newWidth)
    {
        penWidth = Mathf.Clamp(newWidth, 0.001f, 0.1f);
    }

    public Color GetCurrentColor()
    {
        return penColor;
    }

    public float GetCurrentWidth()
    {
        return penWidth;
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