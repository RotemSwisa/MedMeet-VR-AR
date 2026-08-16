using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PenController : MonoBehaviour
{
    [Header("Drawing Settings")]
    [SerializeField] private GameObject lineRendererPrefab;
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

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private LineRenderer currentLine;
    private bool isDrawing = false;
    private Vector3 lastDrawnPosition;
    private bool isGrabbed = false;

    // VR Input tracking
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor currentInteractor;
    private bool wasActivatePressed = false;

    // Desktop variables
    private bool desktopHoldingPen = false;
    private Vector3 desktopPenOffset;

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
        if (isDrawing && currentLine != null)
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
            if (isDrawing)
            {
                StopDrawing();
            }
        }

        // Left click to draw (only when holding pen)
        if (desktopHoldingPen)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartDrawing();
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                StopDrawing();
            }

            // Toggle color picker with C key (when holding pen)
            if (Keyboard.current[Key.C].wasPressedThisFrame)
            {
                ToggleColorPicker();
            }
        }
    }

    void HandleVRInput()
    {
        if (currentInteractor == null) return;

        // Try to get input
        bool activatePressed = false;
        bool uiPressed = false;

        // Check if we have XRController
        if (currentInteractor.xrController != null)
        {
            var controller = currentInteractor.xrController;

            // Check trigger (activate)
            float activateValue = controller.activateInteractionState.value;
            activatePressed = activateValue > 0.5f;

            // Check A/X button (UI press)
            uiPressed = controller.uiPressInteractionState.activatedThisFrame;
        }

        // Handle drawing with trigger
        if (activatePressed && !wasActivatePressed)
        {
            // Just pressed
            if (!isDrawing)
            {
                StartDrawing();
                Debug.Log("VR: Started drawing");
            }
        }
        else if (!activatePressed && wasActivatePressed)
        {
            // Just released
            if (isDrawing)
            {
                StopDrawing();
                Debug.Log("VR: Stopped drawing");
            }
        }

        wasActivatePressed = activatePressed;

        // Handle color picker toggle
        if (uiPressed)
        {
            ToggleColorPicker();
        }
    }

    void StartDrawing()
    {
        if (lineRendererPrefab == null)
        {
            Debug.LogError("LineRenderer Prefab not assigned!");
            return;
        }

        GameObject lineObj = Instantiate(lineRendererPrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();

        if (currentLine != null)
        {
            currentLine.startColor = penColor;
            currentLine.endColor = penColor;
            currentLine.startWidth = penWidth;
            currentLine.endWidth = penWidth;
            currentLine.positionCount = 0;

            isDrawing = true;
            lastDrawnPosition = GetTipPosition();
            AddPointToLine(lastDrawnPosition);
        }
    }

    void StopDrawing()
    {
        isDrawing = false;
        currentLine = null;
    }

    void AddPointToLine(Vector3 position)
    {
        if (currentLine == null) return;

        int index = currentLine.positionCount;
        currentLine.positionCount = index + 1;
        currentLine.SetPosition(index, position);
    }

    Vector3 GetTipPosition()
    {
        return penTip != null ? penTip.position : transform.position;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        desktopHoldingPen = false; // Stop desktop mode when VR grabs

        // Store the interactor for input tracking
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
        {
            currentInteractor = controllerInteractor;
            Debug.Log($"VR: Pen grabbed by {controllerInteractor.name}");
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentInteractor = null;
        wasActivatePressed = false;

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