using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// מסך הסבר צף - מציג מידע על האיבר שהמצביע עליו ברגע נתון.
///
/// 2 מצבי תצוגה:
///   - SINGLE: איבר לא מתפוצץ (head/lungs לא מפוצצים, או arm bones / Shoulder muscles)
///             → פאנל יחיד גדול עם שם + הסבר
///   - QUAD:   איבר מתפוצץ (head או lungs במצב Exploded)
///             → ארבע ריבועים מסביב, כל אחד עם שם של קבוצה + הסבר
///
/// המסך מתעדכן בזמן אמת לפי AnatomyGrabbable.IsHovered.
/// </summary>
[DisallowMultipleComponent]
public class AnatomyInfoPanel : MonoBehaviour
{
    public static AnatomyInfoPanel Instance;

    [Header("── מיקום ──")]
    [Tooltip("Transform שהמסך יוצב יחסית אליו (אם null - יוצב במיקום של ה-GameObject הזה)")]
    public Transform anchor;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationEuler = Vector3.zero;
    [Tooltip("גודל המסך במטרים")]
    public Vector2 worldSize = new Vector2(2.6f, 1.7f);

    [Header("── התנהגות Billboard ──")]
    [Tooltip("אם דלוק - המסך תמיד יסתובב לכיוון המשתמש (מצלמה ראשית)")]
    public bool alwaysFaceCamera = true;
    [Tooltip("רק סיבוב על ציר Y (השאר את ההטיה האנכית). מומלץ true ל-VR.")]
    public bool lockToVerticalAxis = true;

    [Header("── עקיבה אחר האיבר ──")]
    [Tooltip("אם דלוק - המסך זז להופיע סמוך לאיבר שמצביעים עליו")]
    public bool followHoveredOrgan = true;
    [Tooltip("רווח (מטרים) בין הקצה הימני של האיבר לקצה השמאלי של המסך. ככל שיותר גדול - המסך יותר רחוק")]
    public float sideGapMeters = 0.35f;
    [Tooltip("הגבהה (מטרים) - המסך יופיע מעט מעל מרכז האיבר")]
    public float heightOffsetMeters = 0.2f;
    [Tooltip("עומק (מטרים) - דחיפה של המסך אחורה מהמשתמש (מקטין הסתרה של האיבר)")]
    public float depthOffsetMeters = 0.4f;
    [Tooltip("מהירות החלקה למיקום החדש (גבוה = מהיר יותר)")]
    public float followSpeed = 8f;

    [Header("── סגנון ──")]
    public Color bgColor = new Color(0.03f, 0.06f, 0.1f, 0.95f);
    public Color borderColor = new Color(0f, 0.78f, 1f, 1f);
    public Color titleColor = new Color(0f, 0.85f, 1f, 1f);
    public Color bodyColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public float fadeSpeed = 6f;

    // Runtime UI
    Canvas canvas;
    CanvasGroup canvasGroup;
    RectTransform rootRT;

    GameObject singleRoot;
    TextMeshProUGUI singleTitle, singleBody;

    GameObject quadRoot;
    TextMeshProUGUI[] quadTitles = new TextMeshProUGUI[4];
    TextMeshProUGUI[] quadBodies = new TextMeshProUGUI[4];

    AnatomyGrabbable[] _grabbables;
    float _refreshTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
        BuildSingleLayout();
        BuildQuadLayout();
        singleRoot.SetActive(false);
        quadRoot.SetActive(false);
        canvasGroup.alpha = 0f;
    }

    void Start()
    {
        RefreshGrabbables();
    }

    void RefreshGrabbables()
    {
        _grabbables = FindObjectsByType<AnatomyGrabbable>(FindObjectsSortMode.None);
    }

    void Update()
    {
        // רענון כל 2 שניות במקרה ויוצרים איברים חדשים
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer > 2f) { RefreshGrabbables(); _refreshTimer = 0f; }

        var hover = FindHovered();
        if (hover != null)
        {
            ShowForHovered(hover);
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);

            // עקוב אחר האיבר - הצב את המסך לידו
            if (followHoveredOrgan) UpdatePanelPositionNearOrgan(hover);
        }
        else
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            if (canvasGroup.alpha <= 0.01f)
            {
                singleRoot.SetActive(false);
                quadRoot.SetActive(false);
            }
        }
    }

    void UpdatePanelPositionNearOrgan(AnatomyGrabbable hover)
    {
        var cam = Camera.main;
        if (cam == null || rootRT == null) return;

        // חשב bounds של האיבר (מרכז + רוחב כפי שנראה מנקודת מבט המצלמה)
        Vector3 organCenter = hover.transform.position;
        Vector3 organHalfExtents = Vector3.one * 0.15f; // fallback
        var rends = hover.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            organCenter = b.center;
            organHalfExtents = b.extents;
        }

        Vector3 camRight = cam.transform.right;
        Vector3 camForward = cam.transform.forward;

        // חישוב "חצי-רוחב" של האיבר כפי שנראה ע"י המצלמה - הקרנה של ה-extents על camRight.
        // ככל שהאיבר רחב יותר → ה-organHalfWidth גדול יותר → המסך זז אוטומטית יותר ימינה.
        float organHalfWidth =
            Mathf.Abs(organHalfExtents.x * camRight.x) +
            Mathf.Abs(organHalfExtents.y * camRight.y) +
            Mathf.Abs(organHalfExtents.z * camRight.z);

        // חצי-רוחב של המסך עצמו במטרים
        float panelHalfWidth = worldSize.x * 0.5f;

        // המרחק הכולל מהמרכז: חצי-רוחב האיבר + רווח + חצי-רוחב המסך
        float distance = organHalfWidth + sideGapMeters + panelHalfWidth;

        Vector3 desiredPos = organCenter
                             + camRight * distance
                             + Vector3.up * heightOffsetMeters
                             + camForward * depthOffsetMeters;

        // החלקה - בזמן ריצה ישתה בקצב, אם נכבה (alpha=0) ישר קופץ
        if (canvasGroup.alpha < 0.05f)
        {
            rootRT.position = desiredPos;
        }
        else
        {
            rootRT.position = Vector3.Lerp(rootRT.position, desiredPos, Time.deltaTime * followSpeed);
        }
    }

    void LateUpdate()
    {
        // Billboard - תמיד מסתובב לכיוון המצלמה הראשית של המשתמש
        if (!alwaysFaceCamera || rootRT == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 toCam = rootRT.position - cam.transform.position;
        if (toCam.sqrMagnitude < 0.0001f) return;

        if (lockToVerticalAxis)
        {
            // השאר רק את ציר ה-Y (לא להטות למעלה/למטה)
            toCam.y = 0;
            if (toCam.sqrMagnitude < 0.0001f) return;
        }

        rootRT.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }

    AnatomyGrabbable FindHovered()
    {
        if (_grabbables == null) return null;
        foreach (var g in _grabbables)
        {
            if (g != null && g.IsHovered) return g;
        }
        return null;
    }

    void ShowForHovered(AnatomyGrabbable g)
    {
        var oc = g.GetComponent<OrganController>();
        if (oc != null)
        {
            if (oc.State == OrganController.OrganState.Exploded)
            {
                ShowQuad(oc);
            }
            else
            {
                ShowSingle(PrettifyName(oc.name), oc.OverallDescription);
            }
            return;
        }
        var d = g.GetComponent<DraggableOrgan>();
        if (d != null)
        {
            ShowSingle(d.OrganName, d.Description);
        }
    }

    void ShowSingle(string title, string body)
    {
        singleRoot.SetActive(true);
        quadRoot.SetActive(false);
        if (singleTitle != null) singleTitle.text = string.IsNullOrEmpty(title) ? "—" : title;
        if (singleBody != null) singleBody.text = string.IsNullOrEmpty(body)
            ? "<i><color=#888888>אין תיאור עדיין</color></i>"
            : body;
    }

    void ShowQuad(OrganController oc)
    {
        singleRoot.SetActive(false);
        quadRoot.SetActive(true);
        for (int i = 0; i < 4; i++)
        {
            if (oc.Groups != null && i < oc.Groups.Count && oc.Groups[i] != null)
            {
                var g = oc.Groups[i];
                if (quadTitles[i] != null) quadTitles[i].text = string.IsNullOrEmpty(g.GroupName) ? "—" : g.GroupName;
                if (quadBodies[i] != null) quadBodies[i].text = string.IsNullOrEmpty(g.Description) ? "" : g.Description;
            }
            else
            {
                if (quadTitles[i] != null) quadTitles[i].text = "";
                if (quadBodies[i] != null) quadBodies[i].text = "";
            }
        }
    }

    string PrettifyName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        if (raw.Length > 0) return char.ToUpper(raw[0]) + (raw.Length > 1 ? raw.Substring(1) : "");
        return raw;
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILDING
    // ════════════════════════════════════════════════════════════════════

    void BuildCanvas()
    {
        var canvasGO = new GameObject("InfoPanelCanvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        rootRT = canvasGO.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(800f, 540f);

        Transform a = anchor != null ? anchor : transform;
        rootRT.position = a.position + a.TransformDirection(positionOffset);
        rootRT.rotation = a.rotation * Quaternion.Euler(rotationEuler);

        // scale uniform by smaller dim
        float sx = worldSize.x / 800f;
        float sy = worldSize.y / 540f;
        float s = Mathf.Min(sx, sy);
        rootRT.localScale = new Vector3(s, s, s);

        // background
        var bgImg = canvasGO.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        var outline = canvasGO.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(3f, -3f);
    }

    void BuildSingleLayout()
    {
        singleRoot = new GameObject("SingleLayout", typeof(RectTransform));
        singleRoot.transform.SetParent(rootRT, false);
        var srt = singleRoot.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = new Vector2(40, 40);
        srt.offsetMax = new Vector2(-40, -40);

        // Title - auto-resize כדי שלא יזלוג מהפאנל
        singleTitle = CreateText("Title", singleRoot.transform, "", 72, titleColor, TextAlignmentOptions.Center, true);
        singleTitle.characterSpacing = 4f;
        singleTitle.enableAutoSizing = true;
        singleTitle.fontSizeMin = 36;
        singleTitle.fontSizeMax = 84;
        singleTitle.enableWordWrapping = false;
        singleTitle.overflowMode = TextOverflowModes.Ellipsis;
        var trt = singleTitle.rectTransform;
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(-20, 100);
        trt.anchoredPosition = new Vector2(0, -10);

        // Divider line
        var div = new GameObject("Divider", typeof(RectTransform));
        div.transform.SetParent(singleRoot.transform, false);
        var divImg = div.AddComponent<Image>();
        divImg.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.35f);
        divImg.raycastTarget = false;
        var drt = divImg.rectTransform;
        drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1);
        drt.pivot = new Vector2(0.5f, 1f);
        drt.sizeDelta = new Vector2(-60, 2);
        drt.anchoredPosition = new Vector2(0, -120);

        // Body - auto-resize שלא יזלוג מהפאנל
        singleBody = CreateText("Body", singleRoot.transform, "", 30, bodyColor, TextAlignmentOptions.TopLeft, false);
        singleBody.lineSpacing = 8f;
        singleBody.enableAutoSizing = true;
        singleBody.fontSizeMin = 18;
        singleBody.fontSizeMax = 32;
        singleBody.overflowMode = TextOverflowModes.Ellipsis;
        var brt = singleBody.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(30, 20);
        brt.offsetMax = new Vector2(-30, -140);
    }

    void BuildQuadLayout()
    {
        quadRoot = new GameObject("QuadLayout", typeof(RectTransform));
        quadRoot.transform.SetParent(rootRT, false);
        var qrt = quadRoot.GetComponent<RectTransform>();
        qrt.anchorMin = Vector2.zero;
        qrt.anchorMax = Vector2.one;
        qrt.offsetMin = new Vector2(25, 25);
        qrt.offsetMax = new Vector2(-25, -25);

        // grid 2x2
        var positions = new[]
        {
            new Vector2(0, 1),   // top-left  → 0
            new Vector2(1, 1),   // top-right → 1
            new Vector2(0, 0),   // bot-left  → 2
            new Vector2(1, 0)    // bot-right → 3
        };
        var pivots = new[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(0, 0),
            new Vector2(1, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            // each quadrant cell ~ half canvas minus padding
            var cell = new GameObject($"Quad_{i}", typeof(RectTransform));
            cell.transform.SetParent(quadRoot.transform, false);
            var crt = cell.GetComponent<RectTransform>();
            crt.anchorMin = positions[i];
            crt.anchorMax = positions[i];
            crt.pivot = pivots[i];
            crt.sizeDelta = new Vector2(360, 230);
            // small inner padding
            float pad = 8f;
            float ox = pivots[i].x == 0 ? pad : -pad;
            float oy = pivots[i].y == 0 ? pad : -pad;
            crt.anchoredPosition = new Vector2(ox, oy);

            // mini bg
            var miniBg = cell.AddComponent<Image>();
            miniBg.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.06f);
            miniBg.raycastTarget = false;
            var miniOutline = cell.AddComponent<Outline>();
            miniOutline.effectColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.6f);
            miniOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // title - auto-resize שיתאים לתא
            quadTitles[i] = CreateText("Title", cell.transform, "", 32, titleColor, TextAlignmentOptions.Left, true);
            quadTitles[i].characterSpacing = 2f;
            quadTitles[i].enableAutoSizing = true;
            quadTitles[i].fontSizeMin = 16;
            quadTitles[i].fontSizeMax = 36;
            quadTitles[i].enableWordWrapping = false;
            quadTitles[i].overflowMode = TextOverflowModes.Ellipsis;
            var ttrt = quadTitles[i].rectTransform;
            ttrt.anchorMin = new Vector2(0, 1); ttrt.anchorMax = new Vector2(1, 1);
            ttrt.pivot = new Vector2(0.5f, 1f);
            ttrt.sizeDelta = new Vector2(-20, 44);
            ttrt.anchoredPosition = new Vector2(0, -8);

            // body - auto-resize שלא יזלוג מהתא
            quadBodies[i] = CreateText("Body", cell.transform, "", 18, bodyColor, TextAlignmentOptions.TopLeft, false);
            quadBodies[i].lineSpacing = 4f;
            quadBodies[i].enableAutoSizing = true;
            quadBodies[i].fontSizeMin = 11;
            quadBodies[i].fontSizeMax = 20;
            quadBodies[i].overflowMode = TextOverflowModes.Ellipsis;
            var bbrt = quadBodies[i].rectTransform;
            bbrt.anchorMin = Vector2.zero; bbrt.anchorMax = Vector2.one;
            bbrt.offsetMin = new Vector2(12, 10);
            bbrt.offsetMax = new Vector2(-12, -56);
        }
    }

    TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
