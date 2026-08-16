using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Normal.Realtime;

[RequireComponent(typeof(RealtimeView))]
[RequireComponent(typeof(RealtimeTransform))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class NormcorePhysicsGrabbable : MonoBehaviour
{
    private RealtimeView _realtimeView;
    private RealtimeTransform _realtimeTransform;
    private Rigidbody _rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

    // מצבים
    private bool _isGrabbedLocally = false;
    private bool _isRightHandHovering = false;

    [Header("Scaling Settings")]
    [Tooltip("כמה מהר זה יגדל/יקטן?")]
    public float scaleSpeed = 1.0f;

    // גבולות
    private float _originalScaleVal;
    private float _minScaleLimit;
    private float _maxScaleLimit;

    [Header("Debug")]
    public bool enableKeyboardDebug = true;

    void Awake()
    {
        _realtimeView = GetComponent<RealtimeView>();
        _realtimeTransform = GetComponent<RealtimeTransform>();
        _rb = GetComponent<Rigidbody>();
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Start()
    {
        // 1. שמירת הגודל המקורי
        _originalScaleVal = transform.localScale.x;

        // 2. הגדרת גבולות (מינימום 70% מהמקור)
        _minScaleLimit = _originalScaleVal * 0.7f;

        // מקסימום פי 5
        _maxScaleLimit = _originalScaleVal * 5.0f;

        // --- התיקון כאן: הוספתי UnityEngine לפני ה-Debug ---
        UnityEngine.Debug.Log($"Scaling Setup | Original: {_originalScaleVal} | Min (70%): {_minScaleLimit} | Max: {_maxScaleLimit}");
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
        _grab.hoverEntered.AddListener(OnHoverEnter);
        _grab.hoverExited.AddListener(OnHoverExit);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
        _grab.hoverEntered.RemoveListener(OnHoverEnter);
        _grab.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (IsRightHand(args.interactorObject)) _isRightHandHovering = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (IsRightHand(args.interactorObject)) _isRightHandHovering = false;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbedLocally = true;
        _realtimeView.RequestOwnership();
        _realtimeTransform.RequestOwnership();

        if (IsRightHand(args.interactorObject))
        {
            _grab.trackPosition = false;
            _grab.trackRotation = true;
        }
        else
        {
            _grab.trackPosition = true;
            _grab.trackRotation = false;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbedLocally = false;
        _grab.trackPosition = true;
        _grab.trackRotation = true;
    }

    void Update()
    {
        // בדיקת הגדלה/הקטנה רק אם מצביעים על המודל
        if (_isRightHandHovering)
        {
            HandleVRButtonScaling();
            if (enableKeyboardDebug) HandleKeyboardScaling();
        }

        ManageNetworkPhysics();
    }

    private void HandleKeyboardScaling()
    {
        if (Input.GetKey(KeyCode.A))
        {
            RequestOwnershipIfNeeded();
            ChangeScale(1.0f + (scaleSpeed * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.B))
        {
            RequestOwnershipIfNeeded();
            ChangeScale(1.0f - (scaleSpeed * Time.deltaTime));
        }
    }

    private void HandleVRButtonScaling()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid) return;

        bool isAPressed, isBPressed;

        // A - להגדיל
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out isAPressed) && isAPressed)
        {
            RequestOwnershipIfNeeded();
            ChangeScale(1.0f + (scaleSpeed * Time.deltaTime));
        }

        // B - להקטין
        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out isBPressed) && isBPressed)
        {
            RequestOwnershipIfNeeded();
            ChangeScale(1.0f - (scaleSpeed * Time.deltaTime));
        }
    }

    private void RequestOwnershipIfNeeded()
    {
        if (!_realtimeView.isOwnedLocallySelf)
        {
            _realtimeView.RequestOwnership();
            _realtimeTransform.RequestOwnership();
        }
    }

    private void ChangeScale(float multiplier)
    {
        float currentX = transform.localScale.x;
        float nextX = currentX * multiplier;
        float clampedX = Mathf.Clamp(nextX, _minScaleLimit, _maxScaleLimit);
        transform.localScale = Vector3.one * clampedX;
    }

    private void ManageNetworkPhysics()
    {
        if (!_isGrabbedLocally && !_realtimeView.isOwnedLocallySelf)
        {
            if (!_rb.isKinematic) _rb.isKinematic = true;
        }
        else if (_realtimeView.isOwnedLocallySelf && !_isGrabbedLocally)
        {
            if (!_rb.isKinematic) _rb.isKinematic = true;
        }
    }

    private bool IsRightHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor) => IsRightHandTransform(interactor.transform);
    private bool IsRightHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor interactor) => IsRightHandTransform(interactor.transform);

    private bool IsRightHandTransform(Transform t)
    {
        string name = t.name;
        string parent = t.parent ? t.parent.name : "";
        return name.Contains("Right") || parent.Contains("Right");
    }
}