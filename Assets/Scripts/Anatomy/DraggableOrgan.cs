using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// DraggableOrgan - איבר שניתן להזיז ולסובב אבל לא להתפצץ.
/// משמש ל-arm bones / Shoulder muscles.
///
/// משתמש ב-AnatomyGrabbable לתפיסה (אין יותר רפלקציה, אין יותר קפיצות).
///
/// תכונות:
///   - תפיסה דרך AnatomyGrabbable (יוצא ל-snap-back אוטומטי קרוב למקור)
///   - תווית "תמיד פעילה" מעל האיבר
///   - R במקלדת או VR secondary button (B/Y) להחזיר ידנית למקום המקורי
/// </summary>
[DisallowMultipleComponent]
public class DraggableOrgan : MonoBehaviour
{
    [Header("── תוכן ──")]
    public string OrganName;
    [TextArea(2, 6)]
    public string Description;

    [Header("── תווית ──")]
    public Color labelColor = Color.white;
    public Color labelBgColor = new Color(0f, 0f, 0f, 0.7f);
    [Tooltip("גובה התווית מעל מרכז האיבר (במטרים)")]
    public float labelHeight = 0.05f;
    [Tooltip("אם דלוק - התווית תוצג רק אחרי שהאיבר זז מהמקור (אחרת תוסתר)")]
    public bool showLabelOnlyWhenMoved = true;
    [Tooltip("מרחק (במטרים) שמעליו נחשב 'זז'")]
    public float movedThreshold = 0.02f;

    [Header("── החזרה למקום ──")]
    public KeyCode editorReturnKey = KeyCode.R;
    [Tooltip("אם דלוק - חייבים להצביע עם העכבר/לייזר כדי שמקש R יעבוד")]
    public bool requireHoverForReturn = true;

    // ─── Runtime ───
    GameObject labelGO;
    AnatomyGrabbable grabbable;
    Transform parentBody;
    bool lastYButton = false;
    InputDevice rightController, leftController;
    // legacy field (לא בשימוש, נשמר כדי לא לשבור serialization של scenes ישנים)

    void Awake()
    {
        parentBody = transform.parent;
        if (string.IsNullOrEmpty(OrganName))
        {
            OrganName = AnatomyPart.CleanName(gameObject.name);
        }
    }

    void Start()
    {
        labelGO = CreateAlwaysOnLabel();
        if (labelGO != null) labelGO.SetActive(false); // יוסתר עד שהאיבר זז

        TryFindControllers();

        grabbable = GetComponent<AnatomyGrabbable>();
        if (grabbable == null)
        {
            Debug.LogWarning($"[DraggableOrgan] {name}: אין AnatomyGrabbable - תפיסת VR לא תעבוד. הרץ MedMeet Tools → Setup Anatomy Explosion");
        }
    }

    void Update()
    {
        // Show/hide label based on whether the organ moved from origin
        bool moved = HasMovedFromOrigin();
        if (labelGO != null)
        {
            bool shouldShow = !showLabelOnlyWhenMoved || moved;
            if (labelGO.activeSelf != shouldShow) labelGO.SetActive(shouldShow);
            if (shouldShow) UpdateLabelPosition();
        }

        // ── Y בבקר השמאלי כשמצביעים → חזרה למקום ──
        CheckYButtonReturn();

        // ── B בבקר הימני כשמצביעים → הגדלה/הקטנה (לא חייב תפיסה!) ──
        CheckBButtonScale();

        bool hovered = !requireHoverForReturn || IsHoveredByMouse();

        // R במקלדת
        if (hovered && Input.GetKeyDown(editorReturnKey))
        {
            ReturnToOrigin();
        }

        // (B בימני מטופל ע"י CheckBButtonScale למעלה)
    }

    bool IsHoveredByMouse()
    {
        var cam = Camera.main;
        if (cam == null) return false;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

    bool HasMovedFromOrigin()
    {
        if (grabbable == null) grabbable = GetComponent<AnatomyGrabbable>();
        if (grabbable == null || parentBody == null) return false;
        Vector3 origWorld = parentBody.TransformPoint(grabbable.OriginalLocalPosition);
        return Vector3.Distance(transform.position, origWorld) > movedThreshold;
    }

    /// <summary>
    /// כשמצביעים על האיבר עם הלייזר (ימני) ולוחצים Y בבקר השמאלי → חזרה למקום + לגודל.
    /// פועל בלי תפיסה - רק hover (גם אם תופסים בימני).
    /// </summary>
    void CheckYButtonReturn()
    {
        if (grabbable == null || !grabbable.IsHovered) { lastYButton = false; return; }
        if (!leftController.isValid) TryFindControllers();
        if (!leftController.isValid) return;

        bool yNow = false;
        leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out yNow);
        if (yNow && !lastYButton)
        {
            Debug.Log($"[DraggableOrgan] {name} RETURN via Y (left controller)");
            grabbable.ReturnToOrigin();
        }
        lastYButton = yNow;
    }

    bool lastSecondaryBtnRight = false;

    /// <summary>
    /// B על הבקר הימני כשמצביעים על האיבר → הגדלה/הקטנה.
    /// פועל גם בלי תפיסה (רק hover).
    /// </summary>
    void CheckBButtonScale()
    {
        if (grabbable == null || !grabbable.IsHovered) { lastSecondaryBtnRight = false; return; }
        if (!rightController.isValid) TryFindControllers();
        if (!rightController.isValid) return;

        bool bNow = false;
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bNow);
        if (bNow && !lastSecondaryBtnRight)
        {
            Debug.Log($"[DraggableOrgan] {name} SCALE via B (VR)");
            grabbable.ToggleScale();
        }
        lastSecondaryBtnRight = bNow;
    }

    public void ReturnToOrigin()
    {
        if (grabbable != null) grabbable.ReturnToOrigin();
        else Debug.LogWarning($"[DraggableOrgan] {name}: אין AnatomyGrabbable להחזיר את המיקום");
    }

    // ─── Label ───

    GameObject CreateAlwaysOnLabel()
    {
        var dummyGroup = new AnatomyGroup
        {
            GroupName = OrganName,
            Description = Description,
            Parts = new List<Transform> { transform }
        };
        return AnatomyLabelFactory.CreateGroupLabel(dummyGroup, transform, labelColor, labelBgColor);
    }

    void UpdateLabelPosition()
    {
        if (labelGO == null) return;
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            labelGO.transform.position = transform.position + Vector3.up * labelHeight;
            return;
        }
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        labelGO.transform.position = new Vector3(b.center.x, b.max.y + labelHeight, b.center.z);
    }

    // ─── XR controllers ───

    void TryFindControllers()
    {
        var r = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, r);
        if (r.Count > 0) rightController = r[0];

        var l = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, l);
        if (l.Count > 0) leftController = l[0];
    }
}
