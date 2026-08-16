using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// OrganController - על איברים שמתפצצים לקבוצות (head, Lungs).
///
/// תמיכה ב-2 מצבי פיצוץ:
///   - GROUPS: רשימת AnatomyGroup. כל קבוצה זזה כיחידה אחת. תווית אחת לקבוצה.
///   - LEGACY (תאימות): אם אין קבוצות, מתפזר לכל ילד עם AnatomyPart.
///
/// E key (או VR primary button) מפעיל פיצוץ - אבל רק כשהעכבר/לייזר על האיבר.
/// </summary>
[DisallowMultipleComponent]
public class OrganController : MonoBehaviour
{
    [Header("── הגדרות פיצוץ ──")]
    [Tooltip("מרחק הפיצוץ של הקבוצות מהמרכז (במטרים)")]
    public float ExplosionDistance = 0.15f;
    [Tooltip("משך אנימציית הפיצוץ בשניות")]
    public float ExplodeDuration = 1.0f;

    [Header("── החזרה למקום ──")]
    public float SnapBackThreshold = 0.15f;
    public float SnapBackSpeed = 4f;

    [Header("── תיאור כללי של האיבר (כשלא מפוצץ) ──")]
    [TextArea(2, 6)]
    public string OverallDescription;

    [Header("── קבוצות פיצוץ (נטען אוטומטית ע\"י AnatomySetupTool) ──")]
    public List<AnatomyGroup> Groups = new List<AnatomyGroup>();

    [Header("── תוויות ──")]
    public Color labelColor = Color.white;
    public Color labelBgColor = new Color(0f, 0f, 0f, 0.7f);

    [Header("── אינטראקציה ──")]
    [Tooltip("מקשי מקלדת לבדיקה ב-Editor")]
    public KeyCode editorExplodeKey = KeyCode.E;
    public KeyCode editorPullOutKey = KeyCode.R;
    [Tooltip("האם לדרוש שהעכבר/לייזר יצביע על האיבר כדי שכפתורי המקלדת יעבדו")]
    public bool requireHoverForKeyboard = true;

    // ─── State ───
    public enum OrganState { Attached, PulledOut, Exploded }
    public OrganState State { get; private set; } = OrganState.Attached;

    Transform parentBody;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Vector3 originalScale;

    Coroutine explodeRoutine;
    Coroutine snapRoutine;
    bool isGrabbed { get { return grabbable != null && grabbable.IsGrabbed; } }

    InputDevice rightController, leftController;
    bool lastButtonState = false;

    // List of all child colliders that belong to this organ - for hover testing
    List<Collider> myColliders = new List<Collider>();

    void Awake()
    {
        parentBody = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalScale = transform.localScale;

        foreach (var g in Groups)
        {
            if (g != null) g.CacheOriginalPositions();
        }

        myColliders.AddRange(GetComponentsInChildren<Collider>(true));
        BuildLabels();
    }

    AnatomyGrabbable grabbable;

    void Start()
    {
        TryFindControllers();

        // Replace the old reflection-based XRGrabInteractable hook with our custom AnatomyGrabbable.
        grabbable = GetComponent<AnatomyGrabbable>();
        if (grabbable != null)
        {
            grabbable.OnGrabStart += OnGrabbed;
            grabbable.OnGrabEnd += OnReleased;
        }
        else
        {
            Debug.LogWarning($"[OrganController] {gameObject.name}: אין AnatomyGrabbable - תפיסת VR לא תעבוד. הרץ MedMeet Tools → Setup Anatomy Explosion");
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.OnGrabStart -= OnGrabbed;
            grabbable.OnGrabEnd -= OnReleased;
        }
    }

    void Update()
    {
        bool hovered = !requireHoverForKeyboard || IsHoveredByMouseOrLaser();

        // ── מקלדת ──
        if (hovered && Input.GetKeyDown(editorExplodeKey))
        {
            Debug.Log($"[OrganController] {gameObject.name} EXPLODE via keyboard");
            if (State == OrganState.Attached) State = OrganState.PulledOut;
            ToggleExplode();
        }

        if (hovered && Input.GetKeyDown(editorPullOutKey))
        {
            if (State == OrganState.Attached)
            {
                State = OrganState.PulledOut;
                transform.position += Vector3.up * 0.2f + Vector3.forward * 0.2f;
            }
            else
            {
                // Assemble first if exploded, then let AnatomyGrabbable snap the position back.
                if (snapRoutine != null) StopCoroutine(snapRoutine);
                snapRoutine = StartCoroutine(AssembleThenAttach());
                if (grabbable != null) grabbable.ReturnToOrigin();
            }
        }

        // ── כל כפתורי ה-VR פועלים על HOVER (לא חייב תפיסה) ──
        CheckYButtonReturn();  // Y שמאלי = חזרה
        CheckAButtonExplode(); // A ימני = פיצוץ
        CheckBButtonScale();   // B ימני = הגדלה
    }

    /// <summary>
    /// A על הבקר הימני (או X על השמאלי) כשמצביעים על האיבר → פיצוץ/אסיפה.
    /// פועל גם אם לא תופסים את האיבר.
    /// </summary>
    void CheckAButtonExplode()
    {
        if (grabbable == null || !grabbable.IsHovered) { lastButtonState = false; return; }
        if (!rightController.isValid) TryFindControllers();

        bool primaryNow = false;
        if (rightController.isValid)
        {
            rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool a);
            if (a) primaryNow = true;
        }
        if (leftController.isValid)
        {
            leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool x);
            if (x) primaryNow = true;
        }

        if (primaryNow && !lastButtonState)
        {
            Debug.Log($"[OrganController] {gameObject.name} EXPLODE via A (VR)");
            ToggleExplode();
        }
        lastButtonState = primaryNow;
    }

    /// <summary>
    /// B על הבקר הימני כשמצביעים על האיבר → הגדלה/הקטנה.
    /// פועל גם אם לא תופסים את האיבר (רק hover).
    /// </summary>
    void CheckBButtonScale()
    {
        if (grabbable == null || !grabbable.IsHovered) { _lastSecondaryButton = false; return; }
        if (!rightController.isValid) TryFindControllers();
        if (!rightController.isValid) return;

        bool bNow = false;
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bNow);

        if (bNow && !_lastSecondaryButton)
        {
            Debug.Log($"[OrganController] {gameObject.name} SCALE via B (VR)");
            grabbable.ToggleScale();
        }
        _lastSecondaryButton = bNow;
    }

    bool _lastSecondaryButton = false;
    bool _lastYButton = false;

    /// <summary>
    /// כשמצביעים על האיבר עם הלייזר (לא חשוב באיזה בקר) ולוחצים Y בבקר השמאלי
    /// → האיבר חוזר למקום + לגודל המקורי.
    /// פועל גם בלי לתפוס - רק hover.
    /// </summary>
    void CheckYButtonReturn()
    {
        if (grabbable == null || !grabbable.IsHovered) { _lastYButton = false; return; }
        if (!leftController.isValid) TryFindControllers();
        if (!leftController.isValid) return;

        bool yNow = false;
        leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out yNow);

        if (yNow && !_lastYButton)
        {
            Debug.Log($"[OrganController] {gameObject.name} RETURN via Y (left controller)");
            grabbable.ReturnToOrigin();
        }
        _lastYButton = yNow;
    }

    /// <summary>
    /// בדיקה אם העכבר/לייזר VR מצביע על אחד הקוליידרים של האיבר.
    /// </summary>
    bool IsHoveredByMouseOrLaser()
    {
        var cam = Camera.main;
        if (cam == null) return false;

        // ניסיון 1: עכבר (Editor)
        Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit, 1000f))
        {
            if (IsHitOnMyColliders(hit.collider)) return true;
        }

        // ניסיון 2: ray מבקר ה-VR (אם קיים)
        // (לעתיד - כרגע מספיק העכבר ב-Editor)

        return false;
    }

    bool IsHitOnMyColliders(Collider c)
    {
        if (c == null) return false;
        // האם הקוליידר שייך לאיבר הזה (כולל ילדים)?
        return c.transform.IsChildOf(transform);
    }

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

    // ════════════════════════════════════════════════════════════════════
    //  GRAB / RELEASE callbacks (fired by AnatomyGrabbable)
    // ════════════════════════════════════════════════════════════════════

    void OnGrabbed()
    {
        Debug.Log($"[OrganController] {gameObject.name} GRABBED");
        if (snapRoutine != null) { StopCoroutine(snapRoutine); snapRoutine = null; }

        if (State == OrganState.Attached) State = OrganState.PulledOut;
    }

    void OnReleased()
    {
        Debug.Log($"[OrganController] {gameObject.name} RELEASED");

        // AnatomyGrabbable handles the position snap-back. Here we only handle the organ STATE:
        // if it's exploded, assemble it first; if it gets snapped back to origin, mark as Attached.
        Vector3 originalWorld = parentBody != null ? parentBody.TransformPoint(originalLocalPos) : transform.position;
        float distance = Vector3.Distance(transform.position, originalWorld);

        if (distance < SnapBackThreshold)
        {
            // Will be snapped back by AnatomyGrabbable. Assemble first if exploded.
            if (snapRoutine != null) StopCoroutine(snapRoutine);
            snapRoutine = StartCoroutine(AssembleThenAttach());
        }
        // Released far from origin → stays in air, keep current state (PulledOut or Exploded)
    }

    IEnumerator AssembleThenAttach()
    {
        if (State == OrganState.Exploded)
        {
            yield return AssembleCoroutine();
            ShowLabels(false);
        }
        State = OrganState.Attached;
        snapRoutine = null;
    }

    // Public for backward-compat (BodyManager etc. may call it). Just delegates to AnatomyGrabbable.
    public void RequestReturnToOrigin()
    {
        if (grabbable != null) grabbable.ReturnToOrigin();
    }

    // ════════════════════════════════════════════════════════════════════
    //  EXPLODE / ASSEMBLE - GROUPS-BASED
    // ════════════════════════════════════════════════════════════════════

    public void ToggleExplode()
    {
        if (explodeRoutine != null) return;
        if (State == OrganState.Exploded) explodeRoutine = StartCoroutine(AssembleRoutine());
        else explodeRoutine = StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        ShowLabels(true);
        State = OrganState.Exploded;

        if (Groups == null || Groups.Count == 0)
        {
            Debug.LogWarning($"[OrganController] {gameObject.name} - אין קבוצות מוגדרות. שום דבר לא יתפוצץ. הרץ את MedMeet Tools → Setup Anatomy Explosion");
            explodeRoutine = null;
            yield break;
        }

        // חשב מרכז כל האיבר
        Vector3 organCenter = CalculateOrganCenter();

        // לכל קבוצה - חשב כיוון פיצוץ ב-LOCAL space
        var groupOffsets = new Dictionary<AnatomyGroup, Vector3>();
        int idx = 0;
        foreach (var g in Groups)
        {
            if (g == null) { idx++; continue; }
            Vector3 worldDir;
            if (g.DirectionOverride != Vector3.zero)
            {
                worldDir = g.DirectionOverride.normalized;
            }
            else
            {
                Vector3 gc = g.ComputeWorldCenter();
                worldDir = (gc - organCenter).normalized;
                if (worldDir.sqrMagnitude < 0.01f)
                {
                    worldDir = UniformDir(idx, Groups.Count);
                }
            }
            Vector3 localDir = transform.InverseTransformDirection(worldDir);
            if (localDir.sqrMagnitude < 0.01f) localDir = Vector3.up;
            groupOffsets[g] = localDir.normalized * ExplosionDistance * Mathf.Max(0.01f, g.distanceMultiplier);
            idx++;
        }

        // אנימציה - הזז את כל החלקים בכל קבוצה לפי הכיוון של הקבוצה שלהם
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / ExplodeDuration;
            float st = Mathf.SmoothStep(0f, 1f, t);
            foreach (var g in Groups)
            {
                if (g == null || !groupOffsets.ContainsKey(g)) continue;
                Vector3 offset = groupOffsets[g];
                for (int i = 0; i < g.Parts.Count; i++)
                {
                    if (g.Parts[i] == null) continue;
                    if (i >= g.originalLocalPositions.Count) continue;
                    Vector3 orig = g.originalLocalPositions[i];
                    g.Parts[i].localPosition = Vector3.Lerp(orig, orig + offset, st);
                }
            }
            UpdateLabelPositions();
            yield return null;
        }
        UpdateLabelPositions();
        explodeRoutine = null;
    }

    IEnumerator AssembleRoutine()
    {
        yield return AssembleCoroutine();
        State = OrganState.PulledOut;
        explodeRoutine = null;
        ShowLabels(false);
    }

    IEnumerator AssembleCoroutine()
    {
        // קח snapshot של המצב הנוכחי
        var snapshot = new Dictionary<AnatomyGroup, List<Vector3>>();
        foreach (var g in Groups)
        {
            if (g == null) continue;
            var list = new List<Vector3>();
            foreach (var p in g.Parts) list.Add(p != null ? p.localPosition : Vector3.zero);
            snapshot[g] = list;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / ExplodeDuration;
            float st = Mathf.SmoothStep(0f, 1f, t);
            foreach (var g in Groups)
            {
                if (g == null || !snapshot.ContainsKey(g)) continue;
                var current = snapshot[g];
                for (int i = 0; i < g.Parts.Count; i++)
                {
                    if (g.Parts[i] == null) continue;
                    if (i >= g.originalLocalPositions.Count || i >= current.Count) continue;
                    g.Parts[i].localPosition = Vector3.Lerp(current[i], g.originalLocalPositions[i], st);
                }
            }
            UpdateLabelPositions();
            yield return null;
        }

        // נעילה מדויקת
        foreach (var g in Groups)
        {
            if (g == null) continue;
            for (int i = 0; i < g.Parts.Count; i++)
            {
                if (g.Parts[i] == null) continue;
                if (i >= g.originalLocalPositions.Count) continue;
                g.Parts[i].localPosition = g.originalLocalPositions[i];
            }
        }
    }

    Vector3 CalculateOrganCenter()
    {
        Bounds b = new Bounds();
        bool first = true;
        var rends = GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return first ? transform.position : b.center;
    }

    Vector3 UniformDir(int index, int total)
    {
        if (total <= 0) return Vector3.up;
        float phi = Mathf.Acos(1f - 2f * (index + 0.5f) / total);
        float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * index;
        return new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
    }

    // ════════════════════════════════════════════════════════════════════
    //  LABELS - אחת לקבוצה
    // ════════════════════════════════════════════════════════════════════

    GameObject overallLabelGO;

    void BuildLabels()
    {
        foreach (var g in Groups)
        {
            if (g == null) continue;
            var lbl = AnatomyLabelFactory.CreateGroupLabel(g, transform, labelColor, labelBgColor);
            g.labelGO = lbl;
            if (lbl != null) lbl.SetActive(false);
        }

        // תווית כללית של האיבר (מוצגת רק אחרי שזז מהמקור, ולא כשמפוצץ)
        var dummy = new AnatomyGroup
        {
            GroupName = char.ToUpper(gameObject.name[0]) + (gameObject.name.Length > 1 ? gameObject.name.Substring(1) : ""),
            Parts = new System.Collections.Generic.List<Transform> { transform }
        };
        overallLabelGO = AnatomyLabelFactory.CreateGroupLabel(dummy, transform, labelColor, labelBgColor);
        if (overallLabelGO != null) overallLabelGO.SetActive(false);
    }

    void ShowLabels(bool show)
    {
        foreach (var g in Groups)
        {
            if (g != null && g.labelGO != null) g.labelGO.SetActive(show);
        }
    }

    [Header("── תוויות (Labels) ──")]
    [Tooltip("מרחק התווית מעל הגג של הקבוצה (במטרים)")]
    public float labelHeightAboveGroup = 0.02f;

    void UpdateLabelPositions()
    {
        foreach (var g in Groups)
        {
            if (g == null || g.labelGO == null) continue;

            // עדכן את הטקסט - מאפשר שינוי שם בריצה דרך ה-Inspector
            if (g.labelText != null && g.labelText.text != g.GroupName)
            {
                g.labelText.text = string.IsNullOrEmpty(g.GroupName) ? "—" : g.GroupName;
            }

            // התווית ממש מעל הקבוצה - לפי bounds.max.y, לא לפי center
            var b = g.ComputeWorldBounds(out bool found);
            if (found)
            {
                g.labelGO.transform.position = new Vector3(b.center.x, b.max.y + labelHeightAboveGroup, b.center.z);
            }
            else
            {
                // fallback - מעל transform.position
                g.labelGO.transform.position = g.Parts.Count > 0 && g.Parts[0] != null
                    ? g.Parts[0].position + Vector3.up * labelHeightAboveGroup
                    : transform.position + Vector3.up * labelHeightAboveGroup;
            }
        }
    }

    void LateUpdate()
    {
        // ─── State auto-correction: אם החזירו אותי למקום, המצב חוזר ל-Attached ───
        // (תיקון לבאג שבו State נשאר PulledOut אחרי AnatomyGrabbable.ReturnToOrigin)
        if (State == OrganState.PulledOut && !HasMovedFromOrigin())
        {
            State = OrganState.Attached;
        }

        // ─── עדכון תוויות קבוצה (רק כשמפוצץ) ───
        if (State == OrganState.Exploded)
        {
            UpdateLabelPositions();
        }

        // ─── תווית כללית: מוצגת רק כשהאיבר זז ולא כשמפוצץ ולא כשבמקום ───
        if (overallLabelGO != null)
        {
            bool shouldShow = State != OrganState.Exploded && HasMovedFromOrigin();
            if (overallLabelGO.activeSelf != shouldShow)
                overallLabelGO.SetActive(shouldShow);

            if (shouldShow) UpdateOverallLabelPosition();
        }
    }

    void UpdateOverallLabelPosition()
    {
        // Position above the WHOLE organ's bounds
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        overallLabelGO.transform.position = new Vector3(b.center.x, b.max.y + labelHeightAboveGroup, b.center.z);
    }

    bool HasMovedFromOrigin()
    {
        if (parentBody == null) return false;
        Vector3 origWorld = parentBody.TransformPoint(originalLocalPos);
        return Vector3.Distance(transform.position, origWorld) > 0.02f;
    }
}
