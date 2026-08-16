using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// סקריפט תפיסה מותאם לאיברים אנטומיים.
/// מחליף את XRGrabInteractable כדי להימנע מהקפיצה הענקית בעת תפיסה.
///
/// איך זה עובד:
///   1. בעת תפיסה (selectEntered) - שומר את ה-OFFSET בין העצם לבקר
///   2. ב-LateUpdate בזמן תפיסה - שומר על אותו offset כל פריים
///   3. → אין טלפורט. העצם נשאר במקום שהיה כשתפסת אותו
///   4. ב-release - אם קרוב למקור, חוזר. אחרת נשאר באוויר
///
/// נחוצים על אותו GameObject:
///   - XRSimpleInteractable (אותו interactable שמתפקד רק כמקור אירועים, בלי תנועה אוטומטית)
///   - Rigidbody (kinematic)
///   - Collider אחד או יותר (יוקצו ל-XRSimpleInteractable.colliders)
///
/// אירועים שאחרים מאזינים להם (OrganController, DraggableOrgan):
///   - OnGrabStart   - נורה כשתפסו את העצם
///   - OnGrabEnd     - נורה כששחררו
///   - IsGrabbed     - מצב נוכחי
/// </summary>
[DisallowMultipleComponent]
public class AnatomyGrabbable : MonoBehaviour
{
    [Header("── Snap back ──")]
    [Tooltip("האם לחזור למקום המקורי כשמשחררים קרוב למקור")]
    public bool snapBackOnRelease = true;

    [Tooltip("מרחק (מטרים) שמתחתיו האיבר חוזר אוטומטית למקום המקורי")]
    public float snapBackThreshold = 0.15f;

    [Tooltip("מהירות אנימציית החזרה")]
    public float snapBackSpeed = 4f;

    [Header("── Hover feedback (פידבק ויזואלי) ──")]
    [Tooltip("הוסף גוון לחומרי הרינדור כשהלייזר על העצם")]
    public bool tintOnHover = true;

    [Tooltip("צבע ההוברר (נמהל עם הצבע הבסיסי)")]
    public Color hoverTint = Color.white;

    [Range(0f, 1f)]
    [Tooltip("כמה לערבב את צבע ההוברר (0 = ללא, 1 = מלא)")]
    public float hoverTintAmount = 0.45f;

    [Header("── הגדלה / הקטנה ──")]
    [Tooltip("פקטור ההגדלה כשמפעילים את ToggleScale")]
    public float scaleMultiplier = 2f;

    [Tooltip("מהירות אנימציית ההגדלה (גבוה = מהיר יותר)")]
    public float scaleSpeed = 6f;

    [Tooltip("מרחק (מטרים) שמעליו האיבר נחשב 'זז' - תנאי להגדלה")]
    public float scaleMovedThreshold = 0.02f;

    // ─── Public API ───
    public event Action OnGrabStart;
    public event Action OnGrabEnd;
    public bool IsGrabbed => _isGrabbed;
    public bool IsHovered => _isHovered;
    public bool IsScaledUp => _isScaledUp;
    public Vector3 OriginalLocalPosition => _originalLocalPos;
    public Quaternion OriginalLocalRotation => _originalLocalRot;
    public Vector3 OriginalLocalScale => _originalLocalScale;

    // ─── Internal ───
    XRSimpleInteractable _interactable;
    IXRSelectInteractor _currentInteractor;
    Transform _attachTransform;
    Vector3 _grabPositionOffset;
    Quaternion _grabRotationOffset;

    Transform _originalParent;
    Vector3 _originalLocalPos;
    Quaternion _originalLocalRot;
    Vector3 _originalLocalScale;
    bool _isGrabbed;
    bool _isHovered;
    bool _isScaledUp;
    Coroutine _snapRoutine;
    Coroutine _scaleRoutine;

    // Hover tint state
    List<Renderer> _tintRenderers;
    List<Color[]> _originalColors;
    bool _tintInitialized;

    void Awake()
    {
        _originalParent = transform.parent;
        _originalLocalPos = transform.localPosition;
        _originalLocalRot = transform.localRotation;
        _originalLocalScale = transform.localScale;
    }

    void Start()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        if (_interactable == null)
        {
            Debug.LogError($"[AnatomyGrabbable] {name}: חסר XRSimpleInteractable! הרץ MedMeet Tools → Setup Anatomy Explosion.");
            return;
        }

        _interactable.hoverEntered.AddListener(OnHoverEnter);
        _interactable.hoverExited.AddListener(OnHoverExit);
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);

        Debug.Log($"[AnatomyGrabbable] {name} ready (listening to XRSimpleInteractable)");
    }

    void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.hoverEntered.RemoveListener(OnHoverEnter);
            _interactable.hoverExited.RemoveListener(OnHoverExit);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  GRAB / RELEASE - capture offset, maintain it every frame
    // ════════════════════════════════════════════════════════════════════

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        _currentInteractor = args.interactorObject;
        if (_currentInteractor == null) return;

        _attachTransform = _currentInteractor.GetAttachTransform(_interactable);
        if (_attachTransform == null)
        {
            Debug.LogWarning($"[AnatomyGrabbable] {name}: ל-interactor אין attachTransform - לא ניתן לתפוס");
            return;
        }

        // CRITICAL: capture the WORLD-space offset between the controller's attach point and the organ.
        // We will maintain this exact offset every frame - so the organ stays exactly where the user grabbed it,
        // it does NOT teleport to the controller's position.
        _grabPositionOffset = transform.position - _attachTransform.position;
        _grabRotationOffset = Quaternion.Inverse(_attachTransform.rotation) * transform.rotation;

        if (_snapRoutine != null) { StopCoroutine(_snapRoutine); _snapRoutine = null; }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        _isGrabbed = true;
        Debug.Log($"[AnatomyGrabbable] {name} GRAB at offset {_grabPositionOffset.magnitude:F3}m");
        OnGrabStart?.Invoke();
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (!_isGrabbed) return;

        _isGrabbed = false;
        _currentInteractor = null;
        _attachTransform = null;

        Debug.Log($"[AnatomyGrabbable] {name} RELEASE");
        OnGrabEnd?.Invoke();

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (snapBackOnRelease)
        {
            Vector3 origWorld = _originalParent != null
                ? _originalParent.TransformPoint(_originalLocalPos)
                : transform.position;
            float dist = Vector3.Distance(transform.position, origWorld);
            if (dist < snapBackThreshold)
            {
                ReturnToOrigin();
            }
        }
    }

    void LateUpdate()
    {
        if (!_isGrabbed || _attachTransform == null) return;

        // Re-apply the offset captured at grab. Result: object follows the controller motion
        // without ever teleporting to the controller's position.
        transform.position = _attachTransform.position + _grabPositionOffset;
        transform.rotation = _attachTransform.rotation * _grabRotationOffset;
    }

    // ════════════════════════════════════════════════════════════════════
    //  HOVER FEEDBACK
    // ════════════════════════════════════════════════════════════════════

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        SetHovered(true);
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        // XRI may report multiple hover sources; only clear when no hovers remain
        if (_interactable != null && _interactable.isHovered) return;
        SetHovered(false);
    }

    void SetHovered(bool on)
    {
        if (_isHovered == on) return;
        _isHovered = on;
        if (tintOnHover) ApplyHoverTint(on);
    }

    void Update()
    {
        // Editor mouse hover - so the user can see hover tint without a VR headset
        if (Application.isEditor && (_interactable == null || !_interactable.isHovered))
        {
            bool mouseOver = IsMouseHovering();
            // אם הלייזר VR לא על העצם - הסתמך על העכבר
            if (mouseOver != _isHovered) SetHovered(mouseOver);
        }
    }

    bool IsMouseHovering()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 1000f);
        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.transform == transform) return true;
            if (h.collider.transform.IsChildOf(transform)) return true;
        }
        return false;
    }

    void InitTintCache()
    {
        if (_tintInitialized) return;
        _tintRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>(includeInactive: true));
        _originalColors = new List<Color[]>(_tintRenderers.Count);
        foreach (var r in _tintRenderers)
        {
            if (r == null) { _originalColors.Add(System.Array.Empty<Color>()); continue; }
            var mats = r.materials; // instance materials so we don't pollute shared
            var orig = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) { orig[i] = Color.white; continue; }
                if (mats[i].HasProperty("_BaseColor")) orig[i] = mats[i].GetColor("_BaseColor");
                else if (mats[i].HasProperty("_Color")) orig[i] = mats[i].GetColor("_Color");
                else orig[i] = Color.white;
            }
            _originalColors.Add(orig);
        }
        _tintInitialized = true;
    }

    void ApplyHoverTint(bool on)
    {
        InitTintCache();
        for (int i = 0; i < _tintRenderers.Count; i++)
        {
            var r = _tintRenderers[i];
            if (r == null) continue;
            var mats = r.materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] == null || j >= _originalColors[i].Length) continue;
                Color baseC = _originalColors[i][j];
                Color target = on ? Color.Lerp(baseC, hoverTint, hoverTintAmount) : baseC;
                if (mats[j].HasProperty("_BaseColor")) mats[j].SetColor("_BaseColor", target);
                else if (mats[j].HasProperty("_Color")) mats[j].SetColor("_Color", target);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  SNAP BACK
    // ════════════════════════════════════════════════════════════════════

    public void ReturnToOrigin()
    {
        if (_snapRoutine != null) StopCoroutine(_snapRoutine);
        _snapRoutine = StartCoroutine(SnapBackCoroutine());
    }

    /// <summary>
    /// מחליף בין גודל מקורי לגודל מוגדל (פי scaleMultiplier).
    /// פועל רק אחרי שהאיבר זז מהמקור.
    /// </summary>
    public void ToggleScale()
    {
        if (!HasMovedFromOriginInternal())
        {
            Debug.Log($"[AnatomyGrabbable] {name}: לא ניתן להגדיל - האיבר עדיין במיקום המקורי. הזז אותו קודם.");
            return;
        }

        _isScaledUp = !_isScaledUp;
        float target = _isScaledUp ? scaleMultiplier : 1f;
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(AnimateScale(target));
        Debug.Log($"[AnatomyGrabbable] {name}: scale → {target:F1}x");
    }

    bool HasMovedFromOriginInternal()
    {
        if (_originalParent == null) return false;
        Vector3 origWorld = _originalParent.TransformPoint(_originalLocalPos);
        return Vector3.Distance(transform.position, origWorld) > scaleMovedThreshold;
    }

    IEnumerator AnimateScale(float targetMultiplier)
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = _originalLocalScale * targetMultiplier;

        // CRITICAL: ה-pivot של רוב הרשתות לא נמצא במרכז הוויזואלי שלהן.
        // אם פשוט נעלה את ה-localScale - האיבר "יעוף" לכיוון של ה-pivot.
        // הפתרון: לפני כל פריים שומרים את המרכז הוויזואלי, אחרי הזזת ה-scale
        // מזיזים את ה-position כך שהמרכז יישאר באותו מקום בעולם.
        Vector3 initialCenter = ComputeVisualWorldCenter();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            float st = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, st);

            // אחרי שהגודל השתנה, חשב את המרכז החדש ותקן את ה-position
            Vector3 newCenter = ComputeVisualWorldCenter();
            Vector3 delta = initialCenter - newCenter;
            if (delta.sqrMagnitude > 0.000001f)
            {
                transform.position += delta;
                // אם אוחזים את האיבר עכשיו, עדכן גם את ה-offset כדי שה-LateUpdate
                // של ה-grab לא יבטל את התיקון בפריים הבא
                if (_isGrabbed) _grabPositionOffset += delta;
            }
            yield return null;
        }

        transform.localScale = targetScale;
        // תיקון סופי במקרה של floating point drift
        Vector3 finalCenter = ComputeVisualWorldCenter();
        Vector3 finalDelta = initialCenter - finalCenter;
        if (finalDelta.sqrMagnitude > 0.000001f)
        {
            transform.position += finalDelta;
            if (_isGrabbed) _grabPositionOffset += finalDelta;
        }
        _scaleRoutine = null;
    }

    /// <summary>
    /// מחזיר את המרכז הוויזואלי האמיתי של האיבר (לפי bounds של כל הרינדורים).
    /// שונה מ-transform.position שמייצג את ה-pivot שיכול להיות בכל מקום.
    /// </summary>
    Vector3 ComputeVisualWorldCenter()
    {
        var rends = GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return transform.position;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.center;
    }

    IEnumerator SnapBackCoroutine()
    {
        Vector3 origWorld = _originalParent != null
            ? _originalParent.TransformPoint(_originalLocalPos)
            : transform.position;
        Quaternion origRot = _originalParent != null
            ? _originalParent.rotation * _originalLocalRot
            : _originalLocalRot;

        Vector3 sPos = transform.position;
        Quaternion sRot = transform.rotation;
        Vector3 sScale = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * snapBackSpeed;
            float st = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(sPos, origWorld, st);
            transform.rotation = Quaternion.Slerp(sRot, origRot, st);
            transform.localScale = Vector3.Lerp(sScale, _originalLocalScale, st);
            yield return null;
        }

        transform.position = origWorld;
        transform.rotation = origRot;
        if (_originalParent != null)
        {
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.localPosition = _originalLocalPos;
            transform.localRotation = _originalLocalRot;
            transform.localScale = _originalLocalScale;
        }
        // איפוס state הגודל - אחרי snap-back, האיבר תמיד במקור
        _isScaledUp = false;
        if (_scaleRoutine != null) { StopCoroutine(_scaleRoutine); _scaleRoutine = null; }
        _snapRoutine = null;
    }
}
