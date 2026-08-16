using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// RoomDecorator - פוסטר רפואי מעוצב לקיר. סגנון רגוע ומקצועי (לא ניאון בוהק).
///
/// איך להשתמש:
///   1. צור GameObject ריק בסצנה (שם: "Poster_Something")
///   2. גרור עליו את הסקריפט הזה
///   3. ב-Inspector בחר Poster Style מהתפריט
///   4. הצמד אותו לקיר (גרור ל-Scene view, סובב כך שיפנה לתוך החדר)
///   5. בשביל MedMeet Logo - גרור את התמונה לשדה "Brand Logo"
///   6. הרץ
///
/// אפשר ליצור הרבה פוסטרים שונים בסצנה (כל אחד עם סקריפט נפרד).
/// </summary>
[DisallowMultipleComponent]
public class RoomDecorator : MonoBehaviour
{
    public enum PosterStyle
    {
        MedMeetBranding,
        OperatingRoomSign,
        VitalSignsMonitor,
        SafetyGuidelines,
        AnatomyReference,
        TeamCredits,
        EmergencyExitSign,
    }

    [Header("── Style ──")]
    public PosterStyle style = PosterStyle.MedMeetBranding;

    [Header("── Positioning ──")]
    [Tooltip("ה-Transform של הקיר. אם null - יוצב במיקום של ה-GameObject הזה")]
    public Transform wallAnchor;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationEuler = Vector3.zero;
    public Vector2 worldSize = new Vector2(0.8f, 0.6f);

    [Header("── Content (style-specific) ──")]
    [Tooltip("Logo שיופיע ב-MedMeetBranding")]
    public Sprite brandLogo;
    [Tooltip("תמונת לב לפוסטר אנטומיה")]
    public Sprite heartSprite;
    [Tooltip("תמונת מוח לפוסטר אנטומיה")]
    public Sprite brainSprite;
    [Tooltip("תמונת ריאות לפוסטר אנטומיה")]
    public Sprite lungsSprite;
    [Tooltip("טקסט כותרת (לפוסטרים מסוימים)")]
    public string customTitle = "";
    [Tooltip("טקסט תוכן (לרשימות / פוסטרים גנריים)")]
    [TextArea(3, 8)]
    public string customContent = "";
    [Tooltip("מספר חדר ניתוח (לשלט OR)")]
    public string roomNumber = "4";

    [Header("── MedMeet Side Features (4 icons around the logo) ──")]
    [Tooltip("תמונה 1 (שמאל-עליון)")]
    public Sprite feature1Sprite;
    public string feature1Title = "STERILE";
    public string feature1Desc = "Hygiene\nProtocols";

    [Tooltip("תמונה 2 (שמאל-תחתון)")]
    public Sprite feature2Sprite;
    public string feature2Title = "3D MODELS";
    public string feature2Desc = "Interactive\nAnatomy";

    [Tooltip("תמונה 3 (ימין-עליון)")]
    public Sprite feature3Sprite;
    public string feature3Title = "VR TRAINING";
    public string feature3Desc = "Immersive\nEducation";

    [Tooltip("תמונה 4 (ימין-תחתון)")]
    public Sprite feature4Sprite;
    public string feature4Title = "SURGERY";
    public string feature4Desc = "Hands-on\nPractice";

    // ─────────────────────────────────────────
    // Muted Professional Medical Theme
    // (not bright/neon - calm hospital aesthetic)
    // ─────────────────────────────────────────

    static readonly Color BG_DEEP       = new Color(0.08f, 0.11f, 0.15f, 0.96f); // dark slate
    static readonly Color BG_PANEL      = new Color(0.12f, 0.16f, 0.20f, 1f);    // panel surface
    static readonly Color ACCENT_TEAL   = new Color(0.36f, 0.58f, 0.66f, 1f);    // muted teal
    static readonly Color ACCENT_SOFT   = new Color(0.36f, 0.58f, 0.66f, 0.25f); // teal glow
    static readonly Color TEXT_MAIN     = new Color(0.92f, 0.94f, 0.96f, 1f);    // off-white
    static readonly Color TEXT_DIM      = new Color(0.65f, 0.72f, 0.78f, 1f);    // muted gray
    static readonly Color TEXT_VDIM     = new Color(0.55f, 0.62f, 0.68f, 0.7f);  // very muted
    static readonly Color RED_SUBTLE    = new Color(0.78f, 0.32f, 0.32f, 1f);    // muted red
    static readonly Color GREEN_SUBTLE  = new Color(0.45f, 0.72f, 0.55f, 1f);    // muted green

    Canvas canvas;
    RectTransform rootRT;
    TextMeshProUGUI bpmTxt;
    TextMeshProUGUI bpTxt;
    TextMeshProUGUI o2Txt;
    Image heartPulseDot;
    List<RectTransform> ecgDots = new List<RectTransform>();
    Coroutine animLoop;

    void Awake()
    {
        BuildPoster();
    }

    void Start()
    {
        if (style == PosterStyle.VitalSignsMonitor && animLoop == null)
        {
            animLoop = StartCoroutine(VitalSignsLoop());
        }
    }

    void OnDestroy()
    {
        if (animLoop != null) StopCoroutine(animLoop);
    }

    // ════════════════════════════════════════════════════════════════════
    //  BUILD
    // ════════════════════════════════════════════════════════════════════

    bool IsPrintStyle(PosterStyle s)
    {
        // "Print" = wall poster (light bg, simple frame, no UI elements)
        // "Digital" = electronic screen (dark bg, corner brackets, sci-fi feel)
        switch (s)
        {
            case PosterStyle.MedMeetBranding:
            case PosterStyle.AnatomyReference:
            case PosterStyle.SafetyGuidelines:
            case PosterStyle.TeamCredits:
                return true;
            default:
                return false;
        }
    }

    void BuildPoster()
    {
        var canvasGO = new GameObject(style.ToString() + "_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2f;

        canvasGO.AddComponent<GraphicRaycaster>();

        rootRT = canvasGO.GetComponent<RectTransform>();
        // Canvas internal size matches world aspect ratio (so poster doesn't have empty sides)
        float aspect = worldSize.x / worldSize.y;
        float canvasH = 600f;
        float canvasW = Mathf.Round(canvasH * aspect);
        rootRT.sizeDelta = new Vector2(canvasW, canvasH);

        Transform anchor = wallAnchor != null ? wallAnchor : transform;
        rootRT.position = anchor.position + anchor.TransformDirection(positionOffset);
        rootRT.rotation = anchor.rotation * Quaternion.Euler(rotationEuler);

        // Uniform scale based on height
        float scale = worldSize.y / canvasH;
        rootRT.localScale = new Vector3(scale, scale, scale);

        bool printStyle = IsPrintStyle(style);

        if (printStyle)
        {
            // PRINT POSTER STYLE: soft off-white (not blindingly bright)
            var bgImg = canvasGO.AddComponent<Image>();
            bgImg.color = new Color(0.78f, 0.78f, 0.76f, 1f); // muted off-white (less bright)
            bgImg.raycastTarget = false;

            // Thin dark frame (like a picture frame)
            var outline = canvasGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.13f, 0.18f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Inner subtle border
            var innerBorder = NewImg("InnerBorder", rootRT, Color.clear);
            var ibImg = innerBorder;
            ibImg.color = Color.clear;
            var ibOutline = ibImg.gameObject.AddComponent<Outline>();
            ibOutline.effectColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
            ibOutline.effectDistance = new Vector2(1f, -1f);
            var ibRT = ibImg.rectTransform;
            ibRT.anchorMin = Vector2.zero; ibRT.anchorMax = Vector2.one;
            ibRT.offsetMin = new Vector2(15, 15); ibRT.offsetMax = new Vector2(-15, -15);
        }
        else
        {
            // DIGITAL DISPLAY STYLE: dark sci-fi panel with corner brackets (for monitors/signs)
            var bgImg = canvasGO.AddComponent<Image>();
            bgImg.color = BG_DEEP;
            bgImg.raycastTarget = false;

            var outline = canvasGO.AddComponent<Outline>();
            outline.effectColor = new Color(ACCENT_TEAL.r, ACCENT_TEAL.g, ACCENT_TEAL.b, 0.4f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            AddCornerBrackets();
        }

        // Build content based on style
        switch (style)
        {
            case PosterStyle.MedMeetBranding:   BuildMedMeetBranding();   break;
            case PosterStyle.OperatingRoomSign: BuildOperatingRoomSign(); break;
            case PosterStyle.VitalSignsMonitor: BuildVitalSignsMonitor(); break;
            case PosterStyle.SafetyGuidelines:  BuildSafetyGuidelines();  break;
            case PosterStyle.AnatomyReference:  BuildAnatomyReference();  break;
            case PosterStyle.TeamCredits:       BuildTeamCredits();       break;
            case PosterStyle.EmergencyExitSign: BuildEmergencyExitSign(); break;
        }
    }

    void AddCornerBrackets()
    {
        Color c = new Color(ACCENT_TEAL.r, ACCENT_TEAL.g, ACCENT_TEAL.b, 0.5f);
        MakeCorner("TL", new Vector2(0, 1), new Vector2(10, -10), false, false, c);
        MakeCorner("TR", new Vector2(1, 1), new Vector2(-10, -10), true, false, c);
        MakeCorner("BL", new Vector2(0, 0), new Vector2(10, 10), false, true, c);
        MakeCorner("BR", new Vector2(1, 0), new Vector2(-10, 10), true, true, c);
    }

    void MakeCorner(string suffix, Vector2 anchor, Vector2 offset, bool flipH, bool flipV, Color c)
    {
        const float size = 28f, thick = 2f;
        var h = NewImg("Corner_" + suffix + "_H", rootRT, c);
        var hRT = h.rectTransform;
        hRT.anchorMin = anchor; hRT.anchorMax = anchor;
        hRT.pivot = new Vector2(flipH ? 1f : 0f, flipV ? 0f : 1f);
        hRT.sizeDelta = new Vector2(size, thick);
        hRT.anchoredPosition = offset;

        var v = NewImg("Corner_" + suffix + "_V", rootRT, c);
        var vRT = v.rectTransform;
        vRT.anchorMin = anchor; vRT.anchorMax = anchor;
        vRT.pivot = new Vector2(flipH ? 1f : 0f, flipV ? 0f : 1f);
        vRT.sizeDelta = new Vector2(thick, size);
        vRT.anchoredPosition = offset;
    }

    // ─────────────────────────────────────────
    // MedMeet Branding poster
    // ─────────────────────────────────────────

    // ─── Print-friendly colors (dark text on cream paper) ───
    static readonly Color PRINT_TITLE   = new Color(0.10f, 0.16f, 0.22f, 1f); // dark navy
    static readonly Color PRINT_BODY    = new Color(0.18f, 0.22f, 0.28f, 1f); // charcoal
    static readonly Color PRINT_ACCENT  = new Color(0.27f, 0.46f, 0.55f, 1f); // muted teal
    static readonly Color PRINT_MUTED   = new Color(0.45f, 0.50f, 0.55f, 1f); // gray
    static readonly Color PRINT_LINE    = new Color(0.30f, 0.40f, 0.48f, 0.4f);

    void BuildMedMeetBranding()
    {
        // ─── Layout: Top label, side feature columns, centered logo, bottom text ───

        float canvasH = rootRT.sizeDelta.y;

        // Top accent label
        var accentLabel = MakeText("AccentLabel", rootRT, "—  MEDICAL TECHNOLOGY  ·  EST. 2025  —", 18, PRINT_ACCENT, TextAlignmentOptions.Center);
        accentLabel.fontStyle = FontStyles.Bold;
        accentLabel.characterSpacing = 12f;
        var alRT = accentLabel.rectTransform;
        alRT.anchorMin = new Vector2(0, 1); alRT.anchorMax = new Vector2(1, 1);
        alRT.pivot = new Vector2(0.5f, 1f);
        alRT.sizeDelta = new Vector2(0, 28);
        alRT.anchoredPosition = new Vector2(0, -28);
        alRT.localScale = new Vector3(1f, 2f, 1f); // stretch Y to compensate for poster compression

        var topLine = NewImg("TopLine", rootRT, PRINT_LINE);
        var tlRT = topLine.rectTransform;
        tlRT.anchorMin = new Vector2(0, 1); tlRT.anchorMax = new Vector2(1, 1);
        tlRT.pivot = new Vector2(0.5f, 1f);
        tlRT.sizeDelta = new Vector2(-100, 1);
        tlRT.anchoredPosition = new Vector2(0, -70);

        // ─── LEFT side: 2 feature columns ───
        AddFeatureColumn(0.04f, 0.20f,
            feature1Title, feature1Desc, feature1Sprite, "✚", PRINT_ACCENT);

        AddFeatureSubItem(0.04f, 0.20f,
            feature2Title, feature2Desc, feature2Sprite, "◇", PRINT_ACCENT, 0.42f);

        // ─── RIGHT side: 2 feature columns ───
        AddFeatureColumn(0.80f, 0.96f,
            feature3Title, feature3Desc, feature3Sprite, "✦", PRINT_ACCENT);

        AddFeatureSubItem(0.80f, 0.96f,
            feature4Title, feature4Desc, feature4Sprite, "✚", PRINT_ACCENT, 0.42f);

        // ─── LOGO in CENTER (square, preserves aspect) ───
        // Smaller logo so it doesn't appear stretched in wider posters
        float logoSize = canvasH * 0.42f;

        if (brandLogo != null)
        {
            var logoGO = new GameObject("Logo", typeof(RectTransform));
            logoGO.transform.SetParent(rootRT, false);
            var logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = brandLogo;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;
            var logoRT = logoImg.rectTransform;
            logoRT.anchorMin = new Vector2(0.5f, 0.5f); logoRT.anchorMax = new Vector2(0.5f, 0.5f);
            logoRT.pivot = new Vector2(0.5f, 0.5f);
            logoRT.sizeDelta = new Vector2(logoSize, logoSize);
            logoRT.anchoredPosition = new Vector2(0, 18);
            logoRT.localScale = new Vector3(0.8f, 1.4f, 1f); // compensate for poster stretching
        }
        else
        {
            var iconTxt = MakeText("Icon", rootRT, "✚", 280, PRINT_ACCENT, TextAlignmentOptions.Center);
            iconTxt.fontStyle = FontStyles.Bold;
            var iRT = iconTxt.rectTransform;
            iRT.anchorMin = new Vector2(0.5f, 0.5f); iRT.anchorMax = new Vector2(0.5f, 0.5f);
            iRT.pivot = new Vector2(0.5f, 0.5f);
            iRT.sizeDelta = new Vector2(logoSize, logoSize);
            iRT.anchoredPosition = new Vector2(0, 18);
            iRT.localScale = new Vector3(0.8f, 1.4f, 1f); // compensate for poster stretching
        }

        // ─── BELOW logo: brand name + tagline ───
        var botLine = NewImg("BotLine", rootRT, PRINT_LINE);
        var blnRT = botLine.rectTransform;
        blnRT.anchorMin = new Vector2(0, 0); blnRT.anchorMax = new Vector2(1, 0);
        blnRT.pivot = new Vector2(0.5f, 0f);
        blnRT.sizeDelta = new Vector2(-100, 1);
        blnRT.anchoredPosition = new Vector2(0, 120);

        // Brand name (medium)
        var brandName = MakeText("BrandName", rootRT, "MedMeet", 50, PRINT_TITLE, TextAlignmentOptions.Center);
        brandName.fontStyle = FontStyles.Bold;
        brandName.enableAutoSizing = true;
        brandName.fontSizeMin = 24;
        brandName.fontSizeMax = 68;
        brandName.enableWordWrapping = false;
        var bnRT = brandName.rectTransform;
        bnRT.anchorMin = new Vector2(0.25f, 0); bnRT.anchorMax = new Vector2(0.75f, 0);
        bnRT.pivot = new Vector2(0.5f, 0f);
        bnRT.sizeDelta = new Vector2(0, 60);
        bnRT.anchoredPosition = new Vector2(0, 75);

        // Tagline
        var tagline = MakeText("Tagline", rootRT, "AR & VR Medical Team Meeting Platform", 20, PRINT_BODY, TextAlignmentOptions.Center);
        tagline.characterSpacing = 3f;
        tagline.enableAutoSizing = true;
        tagline.fontSizeMin = 12;
        tagline.fontSizeMax = 24;
        var tRT = tagline.rectTransform;
        tRT.anchorMin = new Vector2(0.22f, 0); tRT.anchorMax = new Vector2(0.78f, 0);
        tRT.pivot = new Vector2(0.5f, 0f);
        tRT.sizeDelta = new Vector2(0, 24);
        tRT.anchoredPosition = new Vector2(0, 46);

        // Footer
        var sub = MakeText("Sub", rootRT, "'BEST TEAM' COMPANY", 12, PRINT_MUTED, TextAlignmentOptions.Center);
        sub.characterSpacing = 8f;
        sub.enableAutoSizing = true;
        sub.fontSizeMin = 9;
        sub.fontSizeMax = 16;
        var sRT = sub.rectTransform;
        sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0);
        sRT.pivot = new Vector2(0.5f, 0f);
        sRT.sizeDelta = new Vector2(0, 18);
        sRT.anchoredPosition = new Vector2(0, 20);
    }

    /// <summary>
    /// Adds a feature column on the side - icon on top + label + description
    /// </summary>
    void AddFeatureColumn(float xMin, float xMax, string label, string desc, Sprite sprite, string fallbackGlyph, Color accent)
    {
        // Icon (square area)
        var iconContainer = new GameObject("FeatureIcon_" + label, typeof(RectTransform));
        iconContainer.transform.SetParent(rootRT, false);
        var icRT = iconContainer.GetComponent<RectTransform>();
        icRT.anchorMin = new Vector2(xMin, 0.62f); icRT.anchorMax = new Vector2(xMax, 0.85f);
        icRT.offsetMin = Vector2.zero; icRT.offsetMax = Vector2.zero;

        if (sprite != null)
        {
            var img = iconContainer.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            var glyph = MakeText("Glyph", iconContainer.transform, fallbackGlyph, 90, accent, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            var gRT = glyph.rectTransform;
            gRT.anchorMin = Vector2.zero; gRT.anchorMax = Vector2.one;
            gRT.offsetMin = Vector2.zero; gRT.offsetMax = Vector2.zero;
        }

        // Label below icon
        var lbl = MakeText("Label_" + label, rootRT, label, 16, accent, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 6f;
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 10; lbl.fontSizeMax = 20;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(xMin, 0.55f); lblRT.anchorMax = new Vector2(xMax, 0.62f);
        lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

        // Description below label
        var d = MakeText("Desc_" + label, rootRT, desc, 12, PRINT_MUTED, TextAlignmentOptions.Center);
        d.lineSpacing = 4f;
        d.enableAutoSizing = true;
        d.fontSizeMin = 8; d.fontSizeMax = 14;
        var dRT = d.rectTransform;
        dRT.anchorMin = new Vector2(xMin, 0.46f); dRT.anchorMax = new Vector2(xMax, 0.56f);
        dRT.offsetMin = Vector2.zero; dRT.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Smaller sub-item beneath a feature column
    /// </summary>
    void AddFeatureSubItem(float xMin, float xMax, string label, string desc, Sprite sprite, string fallbackGlyph, Color accent, float yMax)
    {
        float gapY = 0.10f;

        var iconContainer = new GameObject("SubIcon_" + label, typeof(RectTransform));
        iconContainer.transform.SetParent(rootRT, false);
        var icRT = iconContainer.GetComponent<RectTransform>();
        icRT.anchorMin = new Vector2(xMin, yMax - 0.16f); icRT.anchorMax = new Vector2(xMax, yMax);
        icRT.offsetMin = Vector2.zero; icRT.offsetMax = Vector2.zero;

        if (sprite != null)
        {
            var img = iconContainer.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            var glyph = MakeText("Glyph", iconContainer.transform, fallbackGlyph, 60, accent, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            var gRT = glyph.rectTransform;
            gRT.anchorMin = Vector2.zero; gRT.anchorMax = Vector2.one;
            gRT.offsetMin = Vector2.zero; gRT.offsetMax = Vector2.zero;
        }

        var lbl = MakeText("Label", rootRT, label, 14, accent, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 5f;
        lbl.enableAutoSizing = true;
        lbl.fontSizeMin = 8; lbl.fontSizeMax = 18;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(xMin, yMax - 0.22f); lblRT.anchorMax = new Vector2(xMax, yMax - 0.17f);
        lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

        var d = MakeText("Desc", rootRT, desc, 10, PRINT_MUTED, TextAlignmentOptions.Center);
        d.lineSpacing = 4f;
        d.enableAutoSizing = true;
        d.fontSizeMin = 7; d.fontSizeMax = 12;
        var dRT = d.rectTransform;
        dRT.anchorMin = new Vector2(xMin, yMax - 0.32f); dRT.anchorMax = new Vector2(xMax, yMax - 0.23f);
        dRT.offsetMin = Vector2.zero; dRT.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────
    // Operating Room Sign
    // ─────────────────────────────────────────

    void BuildOperatingRoomSign()
    {
        var subLabel = MakeText("SubLabel", rootRT, "OPERATING ROOM", 28, ACCENT_TEAL, TextAlignmentOptions.Center);
        subLabel.fontStyle = FontStyles.Bold;
        subLabel.characterSpacing = 14f;
        var slRT = subLabel.rectTransform;
        slRT.anchorMin = new Vector2(0, 1); slRT.anchorMax = new Vector2(1, 1);
        slRT.pivot = new Vector2(0.5f, 1f);
        slRT.sizeDelta = new Vector2(0, 40);
        slRT.anchoredPosition = new Vector2(0, -60);

        // Huge room number
        var num = MakeText("Number", rootRT, "#" + roomNumber, 280, TEXT_MAIN, TextAlignmentOptions.Center);
        num.fontStyle = FontStyles.Bold;
        var nRT = num.rectTransform;
        nRT.anchorMin = new Vector2(0.5f, 0.5f); nRT.anchorMax = new Vector2(0.5f, 0.5f);
        nRT.pivot = new Vector2(0.5f, 0.5f);
        nRT.sizeDelta = new Vector2(800, 320);
        nRT.anchoredPosition = new Vector2(0, 10);

        // Status indicators at bottom
        var statusBg = NewImg("StatusBg", rootRT, new Color(GREEN_SUBTLE.r, GREEN_SUBTLE.g, GREEN_SUBTLE.b, 0.18f));
        var statusRT = statusBg.rectTransform;
        statusRT.anchorMin = new Vector2(0, 0); statusRT.anchorMax = new Vector2(1, 0);
        statusRT.pivot = new Vector2(0.5f, 0f);
        statusRT.sizeDelta = new Vector2(-80, 50);
        statusRT.anchoredPosition = new Vector2(0, 50);

        var dot = NewImg("StatusDot", statusBg.transform, GREEN_SUBTLE);
        var dotRT = dot.rectTransform;
        dotRT.anchorMin = new Vector2(0, 0.5f); dotRT.anchorMax = new Vector2(0, 0.5f);
        dotRT.pivot = new Vector2(0, 0.5f);
        dotRT.sizeDelta = new Vector2(12, 12);
        dotRT.anchoredPosition = new Vector2(20, 0);

        var statusTxt = MakeText("StatusTxt", statusBg.transform, "AVAILABLE · STERILIZED · READY", 18, GREEN_SUBTLE, TextAlignmentOptions.Center);
        statusTxt.fontStyle = FontStyles.Bold;
        statusTxt.characterSpacing = 6f;
        var sttRT = statusTxt.rectTransform;
        sttRT.anchorMin = Vector2.zero; sttRT.anchorMax = Vector2.one;
        sttRT.offsetMin = Vector2.zero; sttRT.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────
    // Vital Signs Monitor (subtle ECG animation)
    // ─────────────────────────────────────────

    void BuildVitalSignsMonitor()
    {
        // ─── Title bar ───
        var title = MakeText("Title", rootRT, "PATIENT MONITOR", 26, ACCENT_TEAL, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        var titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.sizeDelta = new Vector2(0, 32);
        titleRT.anchoredPosition = new Vector2(30, -25);

        // Pulsing live dot
        var statusDot = NewImg("LiveDot", rootRT, GREEN_SUBTLE);
        heartPulseDot = statusDot;
        var sdRT = statusDot.rectTransform;
        sdRT.anchorMin = new Vector2(1, 1); sdRT.anchorMax = new Vector2(1, 1);
        sdRT.pivot = new Vector2(1, 1);
        sdRT.sizeDelta = new Vector2(14, 14);
        sdRT.anchoredPosition = new Vector2(-65, -35);

        var liveLbl = MakeText("LiveLbl", rootRT, "LIVE", 14, GREEN_SUBTLE, TextAlignmentOptions.Right);
        liveLbl.fontStyle = FontStyles.Bold;
        liveLbl.characterSpacing = 4f;
        var llRT = liveLbl.rectTransform;
        llRT.anchorMin = new Vector2(1, 1); llRT.anchorMax = new Vector2(1, 1);
        llRT.pivot = new Vector2(1, 1);
        llRT.sizeDelta = new Vector2(40, 20);
        llRT.anchoredPosition = new Vector2(-25, -32);

        // ─── ECG box ───
        var ecgBg = NewImg("ECGBg", rootRT, new Color(0.04f, 0.07f, 0.10f, 1f));
        var ecgBgOutline = ecgBg.gameObject.AddComponent<Outline>();
        ecgBgOutline.effectColor = ACCENT_SOFT;
        var ecgRT = ecgBg.rectTransform;
        ecgRT.anchorMin = new Vector2(0, 1); ecgRT.anchorMax = new Vector2(1, 1);
        ecgRT.pivot = new Vector2(0.5f, 1f);
        ecgRT.sizeDelta = new Vector2(-60, 210);
        ecgRT.anchoredPosition = new Vector2(0, -75);

        // Grid lines (horizontal baseline + dotted vertical)
        var baseline = NewImg("Baseline", ecgBg.transform, new Color(GREEN_SUBTLE.r, GREEN_SUBTLE.g, GREEN_SUBTLE.b, 0.12f));
        var blRT = baseline.rectTransform;
        blRT.anchorMin = new Vector2(0, 0.5f); blRT.anchorMax = new Vector2(1, 0.5f);
        blRT.pivot = new Vector2(0.5f, 0.5f);
        blRT.sizeDelta = new Vector2(0, 1);
        blRT.anchoredPosition = Vector2.zero;

        // Inner container for dots
        var ecgContainer = new GameObject("ECGContainer", typeof(RectTransform));
        ecgContainer.transform.SetParent(ecgBg.transform, false);
        var contRT = ecgContainer.GetComponent<RectTransform>();
        contRT.anchorMin = Vector2.zero;
        contRT.anchorMax = Vector2.one;
        contRT.offsetMin = new Vector2(15, 15);
        contRT.offsetMax = new Vector2(-15, -15);

        // Create ECG dots (scrolling waveform)
        const int dotCount = 100;
        ecgDots.Clear();
        for (int i = 0; i < dotCount; i++)
        {
            var dot = NewImg("Dot_" + i, ecgContainer.transform, GREEN_SUBTLE);
            var dotRT = dot.rectTransform;
            float xPct = (float)i / (dotCount - 1);
            dotRT.anchorMin = new Vector2(xPct, 0.5f);
            dotRT.anchorMax = new Vector2(xPct, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.sizeDelta = new Vector2(4, 4);
            dotRT.anchoredPosition = Vector2.zero;
            ecgDots.Add(dotRT);
        }

        // ─── 3 vital metric rows ───
        AddVitalRow("HEART RATE", "BPM", "72", GREEN_SUBTLE, -300);
        AddVitalRow("BLOOD PRESSURE", "mmHg", "120/80", ACCENT_TEAL, -380);
        AddVitalRow("OXYGEN SAT.", "%", "98", new Color(0.7f, 0.85f, 0.7f, 1f), -460);

        // Cache references for animation
        bpmTxt = rootRT.Find("Vital_HEART RATE/ValueText")?.GetComponent<TextMeshProUGUI>();
        bpTxt  = rootRT.Find("Vital_BLOOD PRESSURE/ValueText")?.GetComponent<TextMeshProUGUI>();
        o2Txt  = rootRT.Find("Vital_OXYGEN SAT./ValueText")?.GetComponent<TextMeshProUGUI>();
    }

    void AddVitalRow(string label, string unit, string value, Color valColor, float y)
    {
        var row = new GameObject("Vital_" + label, typeof(RectTransform));
        row.transform.SetParent(rootRT, false);
        var rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1); rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(-60, 60);
        rowRT.anchoredPosition = new Vector2(0, y);

        var lbl = MakeText("LabelText", row.transform, label, 18, TEXT_DIM, TextAlignmentOptions.Left);
        lbl.characterSpacing = 4f;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0, 0); lblRT.anchorMax = new Vector2(0.55f, 1);
        lblRT.offsetMin = new Vector2(20, 0); lblRT.offsetMax = Vector2.zero;

        var val = MakeText("ValueText", row.transform, value, 38, valColor, TextAlignmentOptions.Right);
        val.fontStyle = FontStyles.Bold;
        var valRT = val.rectTransform;
        valRT.anchorMin = new Vector2(0.55f, 0); valRT.anchorMax = new Vector2(0.95f, 1);
        valRT.offsetMin = Vector2.zero; valRT.offsetMax = Vector2.zero;

        var u = MakeText("UnitText", row.transform, unit, 16, TEXT_VDIM, TextAlignmentOptions.Left);
        var uRT = u.rectTransform;
        uRT.anchorMin = new Vector2(0.95f, 0.2f); uRT.anchorMax = new Vector2(1, 0.6f);
        uRT.offsetMin = new Vector2(4, 0); uRT.offsetMax = Vector2.zero;
    }

    IEnumerator VitalSignsLoop()
    {
        float t = 0f;
        float bpmUpdateTimer = 0f;
        float bpUpdateTimer = 0f;

        while (true)
        {
            float dt = Time.deltaTime;
            t += dt * 22f; // scroll speed

            // Update each dot's Y position based on heartbeat function
            // The waveform appears to scroll from right to left
            int n = ecgDots.Count;
            for (int i = 0; i < n; i++)
            {
                float phase = (t + (n - 1 - i) * 0.6f) % 30f;
                float y = HeartbeatY(phase);
                ecgDots[i].anchoredPosition = new Vector2(0, y);
            }

            // Pulsing live dot (synced roughly to heartbeat ~72 BPM)
            if (heartPulseDot != null)
            {
                float pulsePhase = (Time.time * (72f / 60f)) % 1f;
                float scale = 1f + Mathf.Exp(-pulsePhase * 8f) * 0.8f;
                heartPulseDot.rectTransform.localScale = Vector3.one * scale;
                var c = heartPulseDot.color;
                c.a = 0.4f + Mathf.Exp(-pulsePhase * 6f) * 0.6f;
                heartPulseDot.color = c;
            }

            // Heart rate updates (slow fluctuation)
            bpmUpdateTimer += dt;
            if (bpmUpdateTimer > 0.4f)
            {
                bpmUpdateTimer = 0f;
                if (bpmTxt != null)
                {
                    int bpm = Mathf.RoundToInt(72f + Mathf.Sin(Time.time * 0.4f) * 4f + Random.Range(-1f, 1f));
                    bpmTxt.text = bpm.ToString();
                }
                if (o2Txt != null)
                {
                    int o2 = Mathf.RoundToInt(98f + Mathf.Sin(Time.time * 0.3f) * 1f);
                    o2Txt.text = o2.ToString();
                }
            }

            // BP updates rarely
            bpUpdateTimer += dt;
            if (bpUpdateTimer > 4f)
            {
                bpUpdateTimer = 0f;
                if (bpTxt != null)
                {
                    int systolic = Random.Range(118, 124);
                    int diastolic = Random.Range(78, 84);
                    bpTxt.text = $"{systolic}/{diastolic}";
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// פונקציית ECG - מחזירה את ערך ה-Y בכל נקודה בגל הלב (P, Q, R, S, T waves).
    /// phase: 0-30 = מחזור פעימה אחד
    /// </summary>
    float HeartbeatY(float phase)
    {
        if (phase < 0) phase += 30f;

        // P wave (small upward bump): phase 0-2
        if (phase < 2.0f)
            return Mathf.Sin(phase / 2f * Mathf.PI) * 7f;

        // PR segment (flat): phase 2-3
        if (phase < 3.0f)
            return 0f;

        // Q wave (small dip down): phase 3-3.5
        if (phase < 3.5f)
            return -((phase - 3.0f) / 0.5f) * 10f;

        // R spike (huge up): phase 3.5-4.0
        if (phase < 4.0f)
            return -10f + ((phase - 3.5f) / 0.5f) * 80f;

        // S dip (down past baseline): phase 4.0-4.5
        if (phase < 4.5f)
            return 70f - ((phase - 4.0f) / 0.5f) * 90f;

        // Return to baseline: phase 4.5-5.5
        if (phase < 5.5f)
            return -20f + ((phase - 4.5f) / 1.0f) * 20f;

        // ST segment (flat): phase 5.5-7
        if (phase < 7.0f)
            return 0f;

        // T wave (medium bump up): phase 7-11
        if (phase < 11.0f)
            return Mathf.Sin((phase - 7f) / 4f * Mathf.PI) * 14f;

        // Rest (flat baseline): phase 11-30
        return 0f;
    }

    // ─────────────────────────────────────────
    // Safety Guidelines
    // ─────────────────────────────────────────

    void BuildSafetyGuidelines()
    {
        // Small accent label
        var lbl = MakeText("Lbl", rootRT, "— PROTOCOL", 16, PRINT_ACCENT, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 12f;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0, 1); lblRT.anchorMax = new Vector2(1, 1);
        lblRT.pivot = new Vector2(0.5f, 1f);
        lblRT.sizeDelta = new Vector2(0, 22);
        lblRT.anchoredPosition = new Vector2(0, -35);

        var title = MakeText("Title", rootRT, "Safety Guidelines", 42, PRINT_TITLE, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0, 55);
        tRT.anchoredPosition = new Vector2(0, -65);

        var div = NewImg("Div", rootRT, PRINT_LINE);
        var dRT = div.rectTransform;
        dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1);
        dRT.pivot = new Vector2(0.5f, 1f);
        dRT.sizeDelta = new Vector2(-200, 1);
        dRT.anchoredPosition = new Vector2(0, -130);

        string content = string.IsNullOrEmpty(customContent)
            ? "①  Wash hands thoroughly before procedure\n\n②  Wear full sterile protective equipment\n\n③  Verify patient identity and procedure\n\n④  Check all instruments before starting\n\n⑤  Maintain calm and steady hand"
            : customContent;

        var body = MakeText("Body", rootRT, content, 22, PRINT_BODY, TextAlignmentOptions.Left);
        body.lineSpacing = 8f;
        var bRT = body.rectTransform;
        bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
        bRT.offsetMin = new Vector2(60, 50); bRT.offsetMax = new Vector2(-60, -155);
    }

    // ─────────────────────────────────────────
    // Anatomy Reference
    // ─────────────────────────────────────────

    void BuildAnatomyReference()
    {
        var lbl = MakeText("Lbl", rootRT, "— REFERENCE", 16, PRINT_ACCENT, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 12f;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0, 1); lblRT.anchorMax = new Vector2(1, 1);
        lblRT.pivot = new Vector2(0.5f, 1f);
        lblRT.sizeDelta = new Vector2(0, 22);
        lblRT.anchoredPosition = new Vector2(0, -30);

        var title = MakeText("Title", rootRT, "Human Anatomy", 40, PRINT_TITLE, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0, 52);
        tRT.anchoredPosition = new Vector2(0, -60);

        var div = NewImg("Div", rootRT, PRINT_LINE);
        var dRT = div.rectTransform;
        dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1);
        dRT.pivot = new Vector2(0.5f, 1f);
        dRT.sizeDelta = new Vector2(-200, 1);
        dRT.anchoredPosition = new Vector2(0, -125);

        // 3 anatomy cards with images
        AddAnatomyCard("Heart",  "Pumps oxygen-rich blood\nthrough the cardiovascular\nsystem", -260, new Color(0.62f, 0.28f, 0.30f, 1f), heartSprite);
        AddAnatomyCard("Brain",  "Controls all bodily functions\nand cognitive processes",         0, new Color(0.45f, 0.40f, 0.58f, 1f), brainSprite);
        AddAnatomyCard("Lungs",  "Oxygen and CO2 exchange\nwith the bloodstream",                260, new Color(0.32f, 0.50f, 0.60f, 1f), lungsSprite);
    }

    void AddAnatomyCard(string name, string desc, float x, Color accent, Sprite organSprite)
    {
        // Light card (matches print poster style)
        var card = NewImg("Card_" + name, rootRT, new Color(1f, 1f, 1f, 0.6f));
        var cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(accent.r, accent.g, accent.b, 0.5f);
        cardOutline.effectDistance = new Vector2(1f, -1f);
        var cRT = card.rectTransform;
        cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(230, 330);
        cRT.anchoredPosition = new Vector2(x, -60);

        // Top accent stripe
        var stripe = NewImg("Stripe", card.transform, accent);
        var stripeRT = stripe.rectTransform;
        stripeRT.anchorMin = new Vector2(0, 1); stripeRT.anchorMax = new Vector2(1, 1);
        stripeRT.pivot = new Vector2(0.5f, 1f);
        stripeRT.sizeDelta = new Vector2(0, 6);
        stripeRT.anchoredPosition = Vector2.zero;

        // Organ image (top of card)
        if (organSprite != null)
        {
            var imgGO = new GameObject("OrganImage", typeof(RectTransform));
            imgGO.transform.SetParent(card.transform, false);
            var organImg = imgGO.AddComponent<Image>();
            organImg.sprite = organSprite;
            organImg.preserveAspect = true;
            organImg.raycastTarget = false;
            var oRT = organImg.rectTransform;
            oRT.anchorMin = new Vector2(0.5f, 1f); oRT.anchorMax = new Vector2(0.5f, 1f);
            oRT.pivot = new Vector2(0.5f, 1f);
            oRT.sizeDelta = new Vector2(140, 140);
            oRT.anchoredPosition = new Vector2(0, -15);
            oRT.localScale = new Vector3(0.5f, 1f, 1f); // compensate for poster stretching
        }
        else
        {
            // Fallback: organ name as large symbol
            string sym = name == "Heart" ? "♥" : name == "Brain" ? "◉" : "⬣";
            var symTxt = MakeText("Symbol", card.transform, sym, 110, accent, TextAlignmentOptions.Center);
            symTxt.fontStyle = FontStyles.Bold;
            var symRT = symTxt.rectTransform;
            symRT.anchorMin = new Vector2(0.5f, 1f); symRT.anchorMax = new Vector2(0.5f, 1f);
            symRT.pivot = new Vector2(0.5f, 1f);
            symRT.sizeDelta = new Vector2(140, 140);
            symRT.anchoredPosition = new Vector2(0, -10);
            symRT.localScale = new Vector3(0.5f, 1f, 1f); // compensate for poster stretching
        }

        // Name
        var nameTxt = MakeText("Name", card.transform, name, 26, accent, TextAlignmentOptions.Center);
        nameTxt.fontStyle = FontStyles.Bold;
        var nRT = nameTxt.rectTransform;
        nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
        nRT.pivot = new Vector2(0.5f, 1f);
        nRT.sizeDelta = new Vector2(0, 36);
        nRT.anchoredPosition = new Vector2(0, -165);

        // Divider
        var divider = NewImg("Div", card.transform, new Color(accent.r, accent.g, accent.b, 0.3f));
        var divRT = divider.rectTransform;
        divRT.anchorMin = new Vector2(0, 1); divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1f);
        divRT.sizeDelta = new Vector2(-50, 1);
        divRT.anchoredPosition = new Vector2(0, -205);

        // Description
        var descTxt = MakeText("Desc", card.transform, desc, 13, PRINT_BODY, TextAlignmentOptions.Center);
        descTxt.lineSpacing = 6f;
        var descRT = descTxt.rectTransform;
        descRT.anchorMin = new Vector2(0, 0); descRT.anchorMax = new Vector2(1, 1);
        descRT.offsetMin = new Vector2(12, 15); descRT.offsetMax = new Vector2(-12, -215);
    }

    // ─────────────────────────────────────────
    // Team Credits
    // ─────────────────────────────────────────

    void BuildTeamCredits()
    {
        var lbl = MakeText("Lbl", rootRT, "— TEAM", 16, PRINT_ACCENT, TextAlignmentOptions.Center);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 14f;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0, 1); lblRT.anchorMax = new Vector2(1, 1);
        lblRT.pivot = new Vector2(0.5f, 1f);
        lblRT.sizeDelta = new Vector2(0, 22);
        lblRT.anchoredPosition = new Vector2(0, -35);

        string title = string.IsNullOrEmpty(customTitle) ? "MedMeet Team" : customTitle;
        var t = MakeText("Title", rootRT, title, 46, PRINT_TITLE, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
        var tRT = t.rectTransform;
        tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0, 60);
        tRT.anchoredPosition = new Vector2(0, -65);

        var subtitle = MakeText("Subtitle", rootRT, "'Best Team' Company", 18, PRINT_MUTED, TextAlignmentOptions.Center);
        subtitle.characterSpacing = 4f;
        var sRT = subtitle.rectTransform;
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.sizeDelta = new Vector2(0, 26);
        sRT.anchoredPosition = new Vector2(0, -130);

        var div = NewImg("Div", rootRT, PRINT_LINE);
        var dRT = div.rectTransform;
        dRT.anchorMin = new Vector2(0, 1); dRT.anchorMax = new Vector2(1, 1);
        dRT.pivot = new Vector2(0.5f, 1f);
        dRT.sizeDelta = new Vector2(-200, 1);
        dRT.anchoredPosition = new Vector2(0, -175);

        string content = string.IsNullOrEmpty(customContent)
            ? "David  ·  Team Member  ·  Team Member\n\nTeam Member  ·  Team Member  ·  Team Member"
            : customContent;

        var body = MakeText("Body", rootRT, content, 22, PRINT_BODY, TextAlignmentOptions.Center);
        body.lineSpacing = 14f;
        var bRT = body.rectTransform;
        bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
        bRT.offsetMin = new Vector2(40, 60); bRT.offsetMax = new Vector2(-40, -200);

        var footer = MakeText("Footer", rootRT, "AR & VR MEDICAL PLATFORM", 14, PRINT_ACCENT, TextAlignmentOptions.Center);
        footer.fontStyle = FontStyles.Bold;
        footer.characterSpacing = 12f;
        var fRT = footer.rectTransform;
        fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(1, 0);
        fRT.pivot = new Vector2(0.5f, 0f);
        fRT.sizeDelta = new Vector2(0, 22);
        fRT.anchoredPosition = new Vector2(0, 30);
    }

    // ─────────────────────────────────────────
    // Emergency Exit Sign
    // ─────────────────────────────────────────

    void BuildEmergencyExitSign()
    {
        var bg = NewImg("EmergencyBg", rootRT, new Color(GREEN_SUBTLE.r, GREEN_SUBTLE.g, GREEN_SUBTLE.b, 0.15f));
        var bgRT = bg.rectTransform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(20, 20); bgRT.offsetMax = new Vector2(-20, -20);

        var arrow = MakeText("Arrow", rootRT, "→", 200, GREEN_SUBTLE, TextAlignmentOptions.Center);
        arrow.fontStyle = FontStyles.Bold;
        var aRT = arrow.rectTransform;
        aRT.anchorMin = new Vector2(0, 0.5f); aRT.anchorMax = new Vector2(0.4f, 0.5f);
        aRT.pivot = new Vector2(0.5f, 0.5f);
        aRT.sizeDelta = new Vector2(0, 240);
        aRT.anchoredPosition = new Vector2(0, 0);

        var exitTxt = MakeText("Exit", rootRT, "EXIT", 100, GREEN_SUBTLE, TextAlignmentOptions.Center);
        exitTxt.fontStyle = FontStyles.Bold;
        exitTxt.characterSpacing = 12f;
        var eRT = exitTxt.rectTransform;
        eRT.anchorMin = new Vector2(0.4f, 0.4f); eRT.anchorMax = new Vector2(1, 0.7f);
        eRT.offsetMin = Vector2.zero; eRT.offsetMax = Vector2.zero;

        var sub = MakeText("Sub", rootRT, "EMERGENCY", 24, new Color(GREEN_SUBTLE.r, GREEN_SUBTLE.g, GREEN_SUBTLE.b, 0.7f), TextAlignmentOptions.Center);
        sub.characterSpacing = 12f;
        var subRT = sub.rectTransform;
        subRT.anchorMin = new Vector2(0.4f, 0.25f); subRT.anchorMax = new Vector2(1, 0.4f);
        subRT.offsetMin = Vector2.zero; subRT.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────
    // Primitives
    // ─────────────────────────────────────────

    Image NewImg(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align)
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
        return tmp;
    }
}
