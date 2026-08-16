using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SurgeryUIManager - בונה את כל ה-UI אוטומטית בזמן ריצה.
/// אין צורך לסדר שום דבר ב-Inspector. רק:
///   1. צור GameObject ריק בסצנה (שם: "SurgeryUI_Auto")
///   2. גרור עליו את הסקריפט הזה
///   3. גרור את ה-SimulationManager לשדה "Sim Manager" (או השאר ריק - יחפש לבד)
///   4. הגדר את "World Anchor" למיקום של הלב/פודיום (אופציונלי)
///   5. הרץ
///
/// העיצוב: Sci-Fi רפואי - רקע כחול כהה, ניאון ציאן, פינות מסומנות, scan line.
/// </summary>
[DisallowMultipleComponent]
public class SurgeryUIManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────

    [Header("── Positioning (World Space) ──")]
    [Tooltip("ה-Transform שהקנבס יוצב יחסית אליו (בד״כ הפודיום/הלב). אם null - יוצב במיקום של ה-GameObject הזה.")]
    public Transform worldAnchor;
    [Tooltip("היסט מהאנקור (X=שמאל/ימין, Y=גובה, Z=קדימה)")]
    public Vector3 positionOffset = new Vector3(0.9f, 0.4f, 0f);
    [Tooltip("סיבוב הקנבס יחסית לאנקור (Y מסובב סביב הציר האנכי)")]
    public Vector3 rotationEuler = new Vector3(0f, -25f, 0f);
    [Tooltip("גודל הקנבס במטרים")]
    public Vector2 canvasWorldSize = new Vector2(0.7f, 0.55f);

    [Header("── References (Auto-Found If Null) ──")]
    public SimulationManager simManager;
    public GameObject heartRoot;
    public GameObject brainRoot;
    public GameObject lungsRoot;

    [Header("── Organ Icons (optional - drag PNG sprites here) ──")]
    [Tooltip("גרור כאן Sprite של לב. אם ריק - יוצג סמל ♥")]
    public Sprite heartSprite;
    [Tooltip("גרור כאן Sprite של מוח. אם ריק - יוצג סמל ◉")]
    public Sprite brainSprite;
    [Tooltip("גרור כאן Sprite של ריאות. אם ריק - יוצג סמל ⬣")]
    public Sprite lungsSprite;

    [Header("── Behavior ──")]
    [Tooltip("האם להשבית את ה-LiveMonitorScreen של SimulationManager כשמתחילים (כדי שלא יסתיר)")]
    public bool hideOldUI = true;

    // ─────────────────────────────────────────
    // Theme (matches the HTML mockup)
    // ─────────────────────────────────────────

    static readonly Color BG_DEEP      = new Color(0.012f, 0.051f, 0.102f, 0.96f); // #030d1a
    static readonly Color CYAN         = new Color(0f, 0.784f, 1f, 1f);            // #00c8ff
    static readonly Color CYAN_DIM     = new Color(0f, 0.784f, 1f, 0.13f);
    static readonly Color CYAN_FAINT   = new Color(0f, 0.784f, 1f, 0.03f);
    static readonly Color GREEN        = new Color(0f, 1f, 0.6f, 1f);              // #00ff99
    static readonly Color RED_HEART    = new Color(1f, 0.267f, 0.4f, 1f);          // #ff4466
    static readonly Color PURPLE_BRAIN = new Color(0.8f, 0.533f, 1f, 1f);          // #cc88ff
    static readonly Color BLUE_LUNGS   = new Color(0f, 0.831f, 1f, 1f);            // #00d4ff
    static readonly Color YELLOW       = new Color(1f, 0.8f, 0f, 1f);              // #ffcc00
    static readonly Color ORANGE       = new Color(1f, 0.4f, 0.267f, 1f);          // #ff6644

    // ─────────────────────────────────────────
    // State
    // ─────────────────────────────────────────

    public enum OrganType { Heart, Brain, Lungs }

    OrganType currentOrgan;
    bool isInSurgery = false;
    float? heartScore, brainScore, lungsScore;

    float livePrecision = 95f;
    float liveSteady = 92f;
    float liveAccuracy = 88f;
    float liveCutPct = 0f;

    // Live tracking
    float surgeryStartTime;
    float totalElapsedTime;
    int heartAttempts, brainAttempts, lungsAttempts;
    int cutAttemptsThisSession = 0;
    int successfulCutsThisSession = 0;
    float bpm = 72f;
    float lastCutScore = -1f;
    float bestCutScore = -1f;
    bool hasAnyCutAttempt = false;

    // Track grab interactables we disabled during surgery, so we can restore them
    System.Collections.Generic.HashSet<Behaviour> _disabledInteractables = new System.Collections.Generic.HashSet<Behaviour>();
    Image livePulseDot;
    TextMeshProUGUI timerTxt;
    TextMeshProUGUI attemptTxt;
    TextMeshProUGUI bpmTxt;
    Image heartIconImg, brainIconImg, lungsIconImg;
    Image surgeryOrganIconImg, summaryOrganIconImg;

    Coroutine liveLoop;
    Coroutine scanLoop;
    Coroutine pulseLoop;

    // ─────────────────────────────────────────
    // Runtime UI references (built in code)
    // ─────────────────────────────────────────

    Canvas canvas;
    RectTransform rootRT;
    Image canvasBgImg;
    Outline canvasOutline;
    List<GameObject> canvasDecorations = new List<GameObject>();

    // Screens
    GameObject welcomeScreen, selectScreen, surgeryScreen, summaryScreen;
    Button welcomeStartBtn;

    // Select screen
    Button heartBtn, brainBtn, lungsBtn;
    TextMeshProUGUI heartGradeTxt, brainGradeTxt, lungsGradeTxt;
    TextMeshProUGUI heartLabelTxt, brainLabelTxt, lungsLabelTxt;
    TextMeshProUGUI selectHintTxt;

    // Surgery screen
    TextMeshProUGUI organNameTxt, organSubTxt, organIconTxt;
    Image cutFillImg;
    TextMeshProUGUI cutPctTxt;
    TextMeshProUGUI precisionTxt, steadyTxt, accuracyTxt;
    Image precisionBar, steadyBar, accuracyBar;
    Button finishBtn;
    Button backToMenuBtn;
    TextMeshProUGUI cutInstructionTxt;

    // Summary screen
    TextMeshProUGUI gradeBigTxt, summarySubTxt, summaryIconTxt;
    TextMeshProUGUI sumPrecisionTxt, sumSteadyTxt, sumAccuracyTxt;
    TextMeshProUGUI feedbackTxt;
    Button backBtn;

    // Decorative
    RectTransform scanLineRT;

    // ─────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────

    void Awake()
    {
        BuildCanvas();
        BuildWelcomeScreen();
        BuildSelectScreen();
        BuildSurgeryScreen();
        BuildSummaryScreen();
        BuildDecorations();
    }

    void Start()
    {
        AutoFindReferences();
        WireUpButtons();
        ShowWelcomeScreen();

        if (scanLoop == null) scanLoop = StartCoroutine(ScanLineLoop());
    }

    void Update()
    {
        // Live timer + pulsing dot - only while in surgery screen
        if (isInSurgery && surgeryScreen != null && surgeryScreen.activeInHierarchy)
        {
            float elapsed = Time.time - surgeryStartTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            if (timerTxt != null) timerTxt.text = $"{minutes:00}:{seconds:00}";

            // Pulsing dot (heartbeat: scale 1.0 -> 1.6 -> 1.0)
            if (livePulseDot != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * (bpm / 60f) * Mathf.PI * 2f) * 0.3f + 0.3f;
                livePulseDot.rectTransform.localScale = Vector3.one * pulse;
                var c = livePulseDot.color;
                c.a = 0.5f + Mathf.Abs(Mathf.Sin(Time.time * (bpm / 60f) * Mathf.PI)) * 0.5f;
                livePulseDot.color = c;
            }

            // Subtle BPM fluctuation (more dramatic when actively cutting)
            float bpmTarget = liveCutPct > 0f && liveCutPct < 100f ? 95f + Mathf.Sin(Time.time * 0.7f) * 15f : 72f + Mathf.Sin(Time.time * 0.4f) * 5f;
            bpm = Mathf.Lerp(bpm, bpmTarget, Time.deltaTime * 0.8f);
            if (bpmTxt != null) bpmTxt.text = $"♥ {bpm:F0} BPM";
        }
    }

    void OnDestroy()
    {
        if (liveLoop != null) StopCoroutine(liveLoop);
        if (scanLoop != null) StopCoroutine(scanLoop);
        if (pulseLoop != null) StopCoroutine(pulseLoop);
    }

    // ─────────────────────────────────────────
    // Public API (called by SurgeryManager / Brain / Lungs)
    // ─────────────────────────────────────────

    public void OnOrganCompleted(float score)
    {
        bestCutScore = Mathf.Max(bestCutScore, score);
        lastCutScore = score;
        liveCutPct = score;
        UpdateCutProgressUI(score);
        UpdateLiveStatsFromScore(score);
        if (finishBtn != null) finishBtn.gameObject.SetActive(true);
        if (cutInstructionTxt != null) cutInstructionTxt.text = $"✓ SUCCESS · {score:F0}% · TAP 'VIEW RESULTS'";
        SaveScore(currentOrgan, score);
    }

    /// <summary>
    /// קורא לזה אחרי כל ניסיון חיתוך (גם אם נכשל)
    /// </summary>
    public void OnCutAttempt(float score, bool success)
    {
        hasAnyCutAttempt = true;
        cutAttemptsThisSession++;
        if (success) successfulCutsThisSession++;

        lastCutScore = score;
        bestCutScore = Mathf.Max(bestCutScore, score);
        liveCutPct = score;
        UpdateCutProgressUI(score);
        UpdateLiveStatsFromScore(score);

        int required = RequiredCutsFor(currentOrgan);
        if (attemptTxt != null) attemptTxt.text = $"STEP {Mathf.Min(successfulCutsThisSession + (success ? 0 : 1), required)}/{required} · {cutAttemptsThisSession} TRIES";

        if (cutInstructionTxt != null)
        {
            if (success)
                cutInstructionTxt.text = $"✓ EXCELLENT CUT · {score:F0}%";
            else if (score >= 70f)
                cutInstructionTxt.text = $"⚠ ALMOST · {score:F0}% · TRY AGAIN";
            else
                cutInstructionTxt.text = $"✗ FAILED · {score:F0}% · KEEP PRACTICING";
        }
    }

    /// <summary>How many successful cuts are needed for this organ.</summary>
    int RequiredCutsFor(OrganType o)
    {
        switch (o)
        {
            case OrganType.Heart: return 1;
            case OrganType.Brain: return 2;
            case OrganType.Lungs: return 2;
        }
        return 1;
    }

    public void UpdateCutProgress(float pct)
    {
        liveCutPct = Mathf.Clamp(pct, 0f, 100f);
        UpdateCutProgressUI(liveCutPct);
    }

    void UpdateLiveStatsFromScore(float score)
    {
        // Three stats with small offsets that AVERAGE TO THE ACTUAL SCORE (credible).
        // One offset is forced to be the negative of the other two so the sum cancels out.
        float o1 = Random.Range(-2.5f, 2.5f);
        float o2 = Random.Range(-2.5f, 2.5f);
        float o3 = -(o1 + o2);

        livePrecision = Mathf.Clamp(score + o1, 0f, 100f);
        liveSteady    = Mathf.Clamp(score + o2, 0f, 100f);
        liveAccuracy  = Mathf.Clamp(score + o3, 0f, 100f);
        UpdateLiveStatTexts();
    }

    // ─────────────────────────────────────────
    // Setup helpers
    // ─────────────────────────────────────────

    void AutoFindReferences()
    {
        if (simManager == null) simManager = FindObjectOfType<SimulationManager>();

        // 1. Try to inherit organ roots from SimulationManager
        if (simManager != null)
        {
            if (heartRoot == null && simManager.heartRoot != null) heartRoot = simManager.heartRoot;
            if (brainRoot == null && simManager.brainRoot != null) brainRoot = simManager.brainRoot;
            if (lungsRoot == null && simManager.lungsRoot != null) lungsRoot = simManager.lungsRoot;

            if (hideOldUI)
            {
                if (simManager.startScreen != null) simManager.startScreen.SetActive(false);
                if (simManager.liveMonitorScreen != null) simManager.liveMonitorScreen.SetActive(false);
                if (simManager.summaryScreen != null) simManager.summaryScreen.SetActive(false);
            }
        }

        // 2. FALLBACK: Find organ roots by name (works even if SimManager refs are broken)
        if (heartRoot == null) heartRoot = FindOrganByName("Split_Heart", "Heart");
        if (brainRoot == null) brainRoot = FindOrganByName("Brain");
        if (lungsRoot == null) lungsRoot = FindOrganByName("Lungs");

        // 3. Find a VR right-controller transform and assign to surgery managers that are missing one
        var controller = FindRightControllerTransform();
        if (controller != null)
        {
            AssignControllerToOrganManagers(heartRoot, controller);
            AssignControllerToOrganManagers(brainRoot, controller);
            AssignControllerToOrganManagers(lungsRoot, controller);
        }

        // 4. Log warnings for anything still missing
        if (heartRoot == null) Debug.LogWarning("[SurgeryUIManager] Heart Root not found in scene! Looking for GameObject named 'Split_Heart' or 'Heart'.");
        if (brainRoot == null) Debug.LogWarning("[SurgeryUIManager] Brain Root not found in scene! Looking for GameObject named 'Brain'.");
        if (lungsRoot == null) Debug.LogWarning("[SurgeryUIManager] Lungs Root not found in scene! Looking for GameObject named 'Lungs'.");
        if (controller == null) Debug.LogWarning("[SurgeryUIManager] Right VR controller not found! Looking for GameObject with 'right' + 'controller'/'hand' in its name.");

        // 5. Force all organs hidden on start (must happen AFTER finding them)
        SetAllOrgansActive(false);
    }

    GameObject FindOrganByName(params string[] candidateNames)
    {
        // First try active GameObjects
        foreach (var n in candidateNames)
        {
            var found = GameObject.Find(n);
            if (found != null) return found;
        }
        // Fallback: search inactive too
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            foreach (var n in candidateNames)
            {
                if (t.name == n) return t.gameObject;
            }
        }
        return null;
    }

    Transform FindRightControllerTransform()
    {
        var all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            string n = t.name.ToLower();
            bool isRight = n.Contains("right");
            bool isController = n.Contains("controller") || n.Contains("hand");
            if (isRight && isController) return t;
        }
        return null;
    }

    void AssignControllerToOrganManagers(GameObject organ, Transform controller)
    {
        if (organ == null || controller == null) return;
        var h = organ.GetComponentInChildren<SurgeryManager>(true);
        if (h != null && h.controllerTransform == null) h.controllerTransform = controller;
        var b = organ.GetComponentInChildren<BrainSurgeryManager>(true);
        if (b != null && b.controllerTransform == null) b.controllerTransform = controller;
        var l = organ.GetComponentInChildren<LungsSurgeryManager>(true);
        if (l != null && l.controllerTransform == null) l.controllerTransform = controller;
    }

    void WireUpButtons()
    {
        if (heartBtn != null)  { heartBtn.onClick.RemoveAllListeners();  heartBtn.onClick.AddListener(()  => StartOrgan(OrganType.Heart)); }
        if (brainBtn != null)  { brainBtn.onClick.RemoveAllListeners();  brainBtn.onClick.AddListener(()  => StartOrgan(OrganType.Brain)); }
        if (lungsBtn != null)  { lungsBtn.onClick.RemoveAllListeners();  lungsBtn.onClick.AddListener(()  => StartOrgan(OrganType.Lungs)); }
        if (finishBtn != null) { finishBtn.onClick.RemoveAllListeners(); finishBtn.onClick.AddListener(OnFinishPressed); }
        if (backBtn != null)   { backBtn.onClick.RemoveAllListeners();   backBtn.onClick.AddListener(OnBackPressed); }
        if (welcomeStartBtn != null) { welcomeStartBtn.onClick.RemoveAllListeners(); welcomeStartBtn.onClick.AddListener(OnWelcomeStartPressed); }
        if (backToMenuBtn != null)   { backToMenuBtn.onClick.RemoveAllListeners();   backToMenuBtn.onClick.AddListener(OnBackPressed); }
    }

    void OnWelcomeStartPressed()
    {
        ShowSelectScreen();
    }

    void ShowWelcomeScreen()
    {
        if (welcomeScreen != null) welcomeScreen.SetActive(true);
        if (selectScreen != null)  selectScreen.SetActive(false);
        if (surgeryScreen != null) surgeryScreen.SetActive(false);
        if (summaryScreen != null) summaryScreen.SetActive(false);

        // Hide canvas frame/background so the welcome button floats alone
        SetCanvasFrameVisible(false);

        SetAllOrgansActive(false);
    }

    // ─────────────────────────────────────────
    // Screen Navigation
    // ─────────────────────────────────────────

    void ShowSelectScreen()
    {
        if (welcomeScreen != null) welcomeScreen.SetActive(false);
        selectScreen.SetActive(true);
        surgeryScreen.SetActive(false);
        summaryScreen.SetActive(false);

        // Deselect to prevent stuck hover/selected state
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        // Restore full canvas frame/background
        SetCanvasFrameVisible(true);

        SetAllOrgansActive(false);
        UpdateSelectButtonLabels();

        if (selectHintTxt != null)
        {
            bool allDone = heartScore.HasValue && brainScore.HasValue && lungsScore.HasValue;
            selectHintTxt.text = allDone ? "ALL ORGANS COMPLETE · REPLAY ANYTIME" : "CHOOSE AN ORGAN TO BEGIN";
        }
    }

    void ShowSurgeryScreen(OrganType organ)
    {
        selectScreen.SetActive(false);
        surgeryScreen.SetActive(true);
        summaryScreen.SetActive(false);

        // CRITICAL: deselect any auto-selected GameObject so buttons don't appear "hovered"
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        finishBtn.gameObject.SetActive(false);
        liveCutPct = 0f;
        UpdateCutProgressUI(0f);

        Color accent = AccentFor(organ);
        string icon = IconFor(organ);
        Sprite sprite = SpriteFor(organ);

        if (sprite != null && surgeryOrganIconImg != null)
        {
            surgeryOrganIconImg.sprite = sprite;
            surgeryOrganIconImg.color = Color.white;
            organIconTxt.text = "";
        }
        else
        {
            if (surgeryOrganIconImg != null) surgeryOrganIconImg.color = new Color(0, 0, 0, 0);
            organIconTxt.text = icon;
            organIconTxt.color = accent;
        }
        cutFillImg.color = accent;
        cutPctTxt.color = accent;
        // (finishBtn keeps its own GREEN styling - no override here)

        switch (organ)
        {
            case OrganType.Heart:
                organNameTxt.text = "HEART";
                organSubTxt.text = "● LIVE · CARDIAC PROCEDURE · PATIENT #4471";
                break;
            case OrganType.Brain:
                organNameTxt.text = "BRAIN";
                organSubTxt.text = "● LIVE · NEUROSURGERY · PATIENT #4471";
                break;
            case OrganType.Lungs:
                organNameTxt.text = "LUNGS";
                organSubTxt.text = "● LIVE · PULMONARY PROCEDURE · PATIENT #4471";
                break;
        }

        if (cutInstructionTxt != null) cutInstructionTxt.text = "HOLD TRIGGER · CUT ALONG THE DASHED LINE";

        // Start blank - stats appear after first cut
        hasAnyCutAttempt = false;
        lastCutScore = -1f;
        bestCutScore = -1f;
        livePrecision = 0f;
        liveSteady = 0f;
        liveAccuracy = 0f;
        liveCutPct = 0f;
        UpdateLiveStatTexts();
        UpdateCutProgressUI(0f);

        if (liveLoop != null) StopCoroutine(liveLoop);
        liveLoop = StartCoroutine(LiveStatUpdateLoop());
    }

    void ShowSummaryScreen(float finalScore)
    {
        selectScreen.SetActive(false);
        surgeryScreen.SetActive(false);
        summaryScreen.SetActive(true);

        // Deselect to prevent stuck hover/selected state
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        if (liveLoop != null) { StopCoroutine(liveLoop); liveLoop = null; }

        Color accent = AccentFor(currentOrgan);
        Sprite sumSprite = SpriteFor(currentOrgan);
        if (sumSprite != null && summaryOrganIconImg != null)
        {
            summaryOrganIconImg.sprite = sumSprite;
            summaryOrganIconImg.color = Color.white;
            summaryIconTxt.text = "";
        }
        else
        {
            if (summaryOrganIconImg != null) summaryOrganIconImg.color = new Color(0, 0, 0, 0);
            summaryIconTxt.text = IconFor(currentOrgan);
            summaryIconTxt.color = accent;
        }

        int requiredCuts = RequiredCutsFor(currentOrgan);
        summarySubTxt.text = $"{currentOrgan.ToString().ToUpper()} · STEPS COMPLETED: {Mathf.Min(successfulCutsThisSession, requiredCuts)}/{requiredCuts} · TOTAL ATTEMPTS: {cutAttemptsThisSession}";

        string grade;
        Color gradeColor;
        string feedback;
        string successMsg;

        if (finalScore >= 90f)      { grade = "S"; gradeColor = GREEN;     successMsg = "SURGERY SUCCEEDED";  feedback = GetFeedback(currentOrgan, "S"); }
        else if (finalScore >= 75f) { grade = "A"; gradeColor = CYAN;      successMsg = "WELL DONE";          feedback = GetFeedback(currentOrgan, "A"); }
        else if (finalScore >= 55f) { grade = "B"; gradeColor = YELLOW;    successMsg = "ACCEPTABLE";         feedback = GetFeedback(currentOrgan, "B"); }
        else                        { grade = "C"; gradeColor = ORANGE;    successMsg = "NEEDS IMPROVEMENT";  feedback = GetFeedback(currentOrgan, "C"); }

        // Big success/fail message
        gradeBigTxt.text = successMsg;
        gradeBigTxt.color = gradeColor;

        // Huge percentage
        sumPrecisionTxt.text = $"{finalScore:F0}%";
        sumPrecisionTxt.color = gradeColor;

        // Time taken
        totalElapsedTime = Time.time - surgeryStartTime;
        int mins = Mathf.FloorToInt(totalElapsedTime / 60f);
        int secs = Mathf.FloorToInt(totalElapsedTime % 60f);
        sumSteadyTxt.text = $"⏱ TIME: {mins:00}:{secs:00}";
        sumSteadyTxt.color = new Color(CYAN.r, CYAN.g, CYAN.b, 0.85f);

        // Grade letter
        sumAccuracyTxt.text = $"GRADE: {grade}";
        sumAccuracyTxt.color = gradeColor;

        // Set the summary score bar
        var fillTr = summaryScreen.transform.Find("ScoreBarTrack/ScoreBarFill");
        if (fillTr != null)
        {
            var fillImg = fillTr.GetComponent<Image>();
            fillImg.color = gradeColor;
            SetBarFill(fillImg, finalScore / 100f);
        }

        // Color the feedback box to match grade color
        var fbBg = summaryScreen.transform.Find("FeedbackBg");
        if (fbBg != null)
        {
            var fbImg = fbBg.GetComponent<Image>();
            fbImg.color = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.05f);
            var fbOutline = fbBg.GetComponent<Outline>();
            if (fbOutline != null) fbOutline.effectColor = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.25f);
        }

        feedbackTxt.text = feedback;
        feedbackTxt.color = new Color(1f, 1f, 1f, 0.92f);

        Color backAccent = CYAN;
        backBtn.GetComponent<Image>().color = new Color(backAccent.r, backAccent.g, backAccent.b, 0.2f);
        backBtn.GetComponentInChildren<TextMeshProUGUI>().color = backAccent;

        SaveScore(currentOrgan, finalScore);
    }

    // ─────────────────────────────────────────
    // Organ Control
    // ─────────────────────────────────────────

    void StartOrgan(OrganType organ)
    {
        currentOrgan = organ;
        isInSurgery = true;

        // Reset per-session counters
        cutAttemptsThisSession = 0;
        successfulCutsThisSession = 0;

        // Live tracking reset / increment
        surgeryStartTime = Time.time;
        bpm = 72f;
        switch (organ)
        {
            case OrganType.Heart: heartAttempts++; if (attemptTxt != null) attemptTxt.text = $"ATTEMPT #{heartAttempts}"; break;
            case OrganType.Brain: brainAttempts++; if (attemptTxt != null) attemptTxt.text = $"ATTEMPT #{brainAttempts}"; break;
            case OrganType.Lungs: lungsAttempts++; if (attemptTxt != null) attemptTxt.text = $"ATTEMPT #{lungsAttempts}"; break;
        }

        SetAllOrgansActive(false);

        GameObject activeRoot = null;
        switch (organ)
        {
            case OrganType.Heart: activeRoot = heartRoot; break;
            case OrganType.Brain: activeRoot = brainRoot; break;
            case OrganType.Lungs: activeRoot = lungsRoot; break;
        }
        if (activeRoot != null) activeRoot.SetActive(true);

        // Disable XR grab/interaction on this organ so VR users can't accidentally grab/move/scale it during surgery
        DisableGrabInteractables(activeRoot);

        // Reset state in original managers
        if (heartRoot != null) { var h = heartRoot.GetComponentInChildren<SurgeryManager>(true); if (h != null) h.ResetHeartState(); }
        if (brainRoot != null) { var b = brainRoot.GetComponentInChildren<BrainSurgeryManager>(true); if (b != null) b.ResetBrainState(); }
        if (lungsRoot != null) { var l = lungsRoot.GetComponentInChildren<LungsSurgeryManager>(true); if (l != null) l.ResetLungsState(); }

        ShowSurgeryScreen(organ);
    }

    // ────────── XR Grab interference fix ──────────

    /// <summary>
    /// Disables any XR Interactable on the organ so the user can't accidentally
    /// grab/move/scale it with the same trigger they use to cut.
    /// </summary>
    void DisableGrabInteractables(GameObject root)
    {
        if (root == null) return;

        // Find all components whose type name contains "Interactable" or "Grab"
        var allComponents = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;
            // Match XRGrabInteractable, XRBaseInteractable, VRNetworkGrab, etc.
            // Skip our own surgery scripts.
            if (typeName.Contains("Surgery") || typeName.Contains("SurgeryUIManager")) continue;
            if (typeName.Contains("Interactable") || typeName.Contains("Grab") || typeName.Contains("Rotate") || typeName.Contains("Scale"))
            {
                if (comp.enabled)
                {
                    comp.enabled = false;
                    _disabledInteractables.Add(comp);
                }
            }
        }
    }

    /// <summary>
    /// Re-enables anything we disabled in DisableGrabInteractables.
    /// </summary>
    void RestoreGrabInteractables()
    {
        foreach (var b in _disabledInteractables)
        {
            if (b != null) b.enabled = true;
        }
        _disabledInteractables.Clear();
    }

    void OnFinishPressed()
    {
        float score = GetSavedScore(currentOrgan);
        ShowSummaryScreen(score);
    }

    void OnBackPressed()
    {
        isInSurgery = false;
        RestoreGrabInteractables();
        SetAllOrgansActive(false);
        ShowSelectScreen();
    }

    void SetAllOrgansActive(bool active)
    {
        if (heartRoot != null) heartRoot.SetActive(active);
        if (brainRoot != null) brainRoot.SetActive(active);
        if (lungsRoot != null) lungsRoot.SetActive(active);
    }

    // ─────────────────────────────────────────
    // Live stat animation
    // ─────────────────────────────────────────

    IEnumerator LiveStatUpdateLoop()
    {
        // No more random fluctuations. Stats only update from real cut attempts.
        // Loop kept alive only for future expansion (e.g. fading effects).
        while (isInSurgery)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    // Helper: set fill on a bar that uses anchor-based scaling (works without a sprite)
    static void SetBarFill(Image fill, float pct01)
    {
        if (fill == null) return;
        var rt = fill.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(Mathf.Clamp01(pct01), 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
    }

    void UpdateLiveStatTexts()
    {
        string dashCyan = "<color=#00c8ff44>—</color>";
        string dashGreen = "<color=#00ff9944>—</color>";
        string dashYellow = "<color=#ffcc0044>—</color>";

        if (precisionTxt != null) { precisionTxt.text = hasAnyCutAttempt ? $"{livePrecision:F0}<size=60%>%</size>" : dashCyan; precisionTxt.color = CYAN; }
        if (steadyTxt != null)    { steadyTxt.text    = hasAnyCutAttempt ? $"{liveSteady:F0}<size=60%>%</size>"    : dashGreen; steadyTxt.color = GREEN; }
        if (accuracyTxt != null)  { accuracyTxt.text  = hasAnyCutAttempt ? $"{liveAccuracy:F0}<size=60%>%</size>"  : dashYellow; accuracyTxt.color = YELLOW; }

        SetBarFill(precisionBar, hasAnyCutAttempt ? livePrecision / 100f : 0f);
        SetBarFill(steadyBar,    hasAnyCutAttempt ? liveSteady    / 100f : 0f);
        SetBarFill(accuracyBar,  hasAnyCutAttempt ? liveAccuracy  / 100f : 0f);
    }

    void UpdateCutProgressUI(float pct)
    {
        if (hasAnyCutAttempt)
        {
            Color c = pct >= 90f ? GREEN : (pct >= 70f ? YELLOW : (pct >= 40f ? ORANGE : RED_HEART));
            SetBarFill(cutFillImg, pct / 100f);
            if (cutFillImg != null) cutFillImg.color = c;
            if (cutPctTxt != null)
            {
                cutPctTxt.text = $"{pct:F0}<size=60%>%</size>";
                cutPctTxt.color = c;
            }
        }
        else
        {
            SetBarFill(cutFillImg, 0f);
            if (cutPctTxt != null)
            {
                cutPctTxt.text = "—";
                cutPctTxt.color = new Color(1, 1, 1, 0.3f);
            }
        }
    }

    // ─────────────────────────────────────────
    // Scan line animation (decorative)
    // ─────────────────────────────────────────

    IEnumerator ScanLineLoop()
    {
        while (true)
        {
            if (scanLineRT != null && rootRT != null)
            {
                float h = rootRT.rect.height;
                float t = (Time.time * 0.25f) % 1f;
                scanLineRT.anchoredPosition = new Vector2(0f, -t * h);
            }
            yield return null;
        }
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    void UpdateSelectButtonLabels()
    {
        UpdateOrganButton(OrganType.Heart, heartGradeTxt, heartLabelTxt, heartBtn, heartScore);
        UpdateOrganButton(OrganType.Brain, brainGradeTxt, brainLabelTxt, brainBtn, brainScore);
        UpdateOrganButton(OrganType.Lungs, lungsGradeTxt, lungsLabelTxt, lungsBtn, lungsScore);
    }

    void UpdateOrganButton(OrganType organ, TextMeshProUGUI gradeTxt, TextMeshProUGUI labelTxt, Button btn, float? score)
    {
        Color accent = AccentFor(organ);
        Sprite sprite = SpriteFor(organ);
        var img = btn.GetComponent<Image>();
        var iconImg = IconImageFor(organ);
        var badgeGO = btn.transform.Find("GradeBadge")?.gameObject;
        var badgeTxt = badgeGO != null ? badgeGO.GetComponentInChildren<TextMeshProUGUI>() : null;

        // Always keep the icon (sprite if available, else text fallback)
        if (sprite != null && iconImg != null)
        {
            iconImg.sprite = sprite;
            iconImg.color = Color.white;
            gradeTxt.text = "";
        }
        else
        {
            if (iconImg != null) iconImg.color = new Color(0, 0, 0, 0);
            gradeTxt.text = IconFor(organ);
            gradeTxt.color = accent;
        }

        if (score.HasValue)
        {
            string grade = ScoreToGrade(score.Value);
            labelTxt.text = organ.ToString().ToUpper() + " · ✓";
            labelTxt.color = new Color(GREEN.r, GREEN.g, GREEN.b, 0.7f);
            img.color = new Color(0f, 1f, 0.6f, 0.04f);
            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(GREEN.r, GREEN.g, GREEN.b, 0.4f);

            // Show grade badge
            if (badgeGO != null) badgeGO.SetActive(true);
            if (badgeTxt != null) badgeTxt.text = grade;
        }
        else
        {
            labelTxt.text = organ.ToString().ToUpper();
            labelTxt.color = new Color(accent.r, accent.g, accent.b, 0.7f);
            img.color = CYAN_FAINT;
            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.effectColor = CYAN_DIM;

            // Hide grade badge
            if (badgeGO != null) badgeGO.SetActive(false);
        }
    }

    void SaveScore(OrganType organ, float score)
    {
        switch (organ)
        {
            case OrganType.Heart: heartScore = score; break;
            case OrganType.Brain: brainScore = score; break;
            case OrganType.Lungs: lungsScore = score; break;
        }
    }

    float GetSavedScore(OrganType organ)
    {
        switch (organ)
        {
            case OrganType.Heart: return heartScore ?? livePrecision;
            case OrganType.Brain: return brainScore ?? livePrecision;
            case OrganType.Lungs: return lungsScore ?? livePrecision;
        }
        return 0f;
    }

    string ScoreToGrade(float score)
    {
        if (score >= 90f) return "S";
        if (score >= 75f) return "A";
        if (score >= 55f) return "B";
        return "C";
    }

    Color AccentFor(OrganType o)
    {
        switch (o)
        {
            case OrganType.Heart: return RED_HEART;
            case OrganType.Brain: return PURPLE_BRAIN;
            case OrganType.Lungs: return BLUE_LUNGS;
        }
        return CYAN;
    }

    // Unicode glyphs (TMP supports most of these from default font)
    string IconFor(OrganType o)
    {
        switch (o)
        {
            case OrganType.Heart: return "♥";
            case OrganType.Brain: return "◉";
            case OrganType.Lungs: return "⬣";
        }
        return "●";
    }

    Sprite SpriteFor(OrganType o)
    {
        switch (o)
        {
            case OrganType.Heart: return heartSprite;
            case OrganType.Brain: return brainSprite;
            case OrganType.Lungs: return lungsSprite;
        }
        return null;
    }

    Image IconImageFor(OrganType o)
    {
        switch (o)
        {
            case OrganType.Heart: return heartIconImg;
            case OrganType.Brain: return brainIconImg;
            case OrganType.Lungs: return lungsIconImg;
        }
        return null;
    }

    string GetFeedback(OrganType organ, string grade)
    {
        switch (organ)
        {
            case OrganType.Heart:
                switch (grade)
                {
                    case "S": return "Flawless cardiac incision. Perfect depth and angle — textbook surgery.";
                    case "A": return "Clean cut with minimal deviation. Strong surgical technique.";
                    case "B": return "Acceptable incision. Minor tremors detected — practice improves precision.";
                    default:  return "Incision complete but inconsistent pressure noted. Keep training.";
                }
            case OrganType.Brain:
                switch (grade)
                {
                    case "S": return "Zero neural damage. Exceptional precision in critical tissue.";
                    case "A": return "Clean resection with excellent neural mapping accuracy.";
                    case "B": return "Adequate incision. Minor proximity warnings triggered.";
                    default:  return "Completed but pressure inconsistencies noted near cortex.";
                }
            case OrganType.Lungs:
                switch (grade)
                {
                    case "S": return "Perfect lobe separation. No air leakage detected. Outstanding.";
                    case "A": return "Clean thoracic incision. Minimal deviation — well done.";
                    case "B": return "Incision complete. Slight pressure variance on pleural edge.";
                    default:  return "Completed under difficulty. Air seal compromised at one point.";
                }
        }
        return "";
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI BUILDING (runs once at Awake)
    // ════════════════════════════════════════════════════════════════════

    void BuildCanvas()
    {
        // Create child GameObject for the canvas so we can position it freely
        GameObject canvasGO = new GameObject("SurgeryUI_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 2f;
        scaler.referencePixelsPerUnit = 100f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Add TrackedDeviceGraphicRaycaster for XR Ray Interactor (VR controllers)
        // Uses reflection so script compiles even if XRI is not installed.
        var xrRaycasterType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit")
            ?? System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit.Interactors");
        if (xrRaycasterType != null)
        {
            canvasGO.AddComponent(xrRaycasterType);
        }
        else
        {
            Debug.LogWarning("[SurgeryUIManager] TrackedDeviceGraphicRaycaster not found. VR laser pointers may not interact with UI. Make sure XR Interaction Toolkit is installed.");
        }

        rootRT = canvasGO.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(800f, 600f);

        // Position relative to anchor
        Transform anchor = worldAnchor != null ? worldAnchor : transform;
        rootRT.position = anchor.position + anchor.TransformDirection(positionOffset);
        rootRT.rotation = anchor.rotation * Quaternion.Euler(rotationEuler);

        // Scale to world size
        float scaleX = canvasWorldSize.x / 800f;
        float scaleY = canvasWorldSize.y / 600f;
        float scale = Mathf.Min(scaleX, scaleY);
        rootRT.localScale = new Vector3(scale, scale, scale);

        // Background panel
        var bgImg = canvasGO.AddComponent<Image>();
        bgImg.color = BG_DEEP;
        bgImg.raycastTarget = true;
        canvasBgImg = bgImg;

        // Outline (cyan border)
        var outline = canvasGO.AddComponent<Outline>();
        outline.effectColor = new Color(CYAN.r, CYAN.g, CYAN.b, 0.2f);
        outline.effectDistance = new Vector2(2f, -2f);
        canvasOutline = outline;
    }

    /// <summary>
    /// Toggle the canvas frame (background + outline + corner brackets + scan line).
    /// Used to hide everything except the welcome button when on the welcome screen.
    /// </summary>
    void SetCanvasFrameVisible(bool visible)
    {
        if (canvasBgImg != null) canvasBgImg.enabled = visible;
        if (canvasOutline != null) canvasOutline.enabled = visible;
        foreach (var d in canvasDecorations)
        {
            if (d != null) d.SetActive(visible);
        }
    }

    void BuildDecorations()
    {
        // 4 corner brackets
        CreateCorner("CornerTL", new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), false, false);
        CreateCorner("CornerTR", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), true, false);
        CreateCorner("CornerBL", new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), false, true);
        CreateCorner("CornerBR", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), true, true);

        // Scan line
        var scanGO = CreateImage("ScanLine", rootRT, new Color(CYAN.r, CYAN.g, CYAN.b, 0.18f));
        scanLineRT = scanGO.GetComponent<RectTransform>();
        scanLineRT.anchorMin = new Vector2(0, 1);
        scanLineRT.anchorMax = new Vector2(1, 1);
        scanLineRT.pivot = new Vector2(0.5f, 1f);
        scanLineRT.sizeDelta = new Vector2(0, 3);
        scanLineRT.anchoredPosition = Vector2.zero;
        canvasDecorations.Add(scanGO);
    }

    void CreateCorner(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, bool flipH, bool flipV)
    {
        const float size = 28f;
        const float thick = 2.5f;

        // Horizontal arm
        var h = CreateImage(name + "_H", rootRT, new Color(CYAN.r, CYAN.g, CYAN.b, 0.6f));
        var hrt = h.GetComponent<RectTransform>();
        hrt.anchorMin = anchorMin;
        hrt.anchorMax = anchorMax;
        hrt.pivot = new Vector2(flipH ? 1f : 0f, flipV ? 0f : 1f);
        hrt.sizeDelta = new Vector2(size, thick);
        hrt.anchoredPosition = offset;
        canvasDecorations.Add(h);

        // Vertical arm
        var v = CreateImage(name + "_V", rootRT, new Color(CYAN.r, CYAN.g, CYAN.b, 0.6f));
        var vrt = v.GetComponent<RectTransform>();
        vrt.anchorMin = anchorMin;
        vrt.anchorMax = anchorMax;
        vrt.pivot = new Vector2(flipH ? 1f : 0f, flipV ? 0f : 1f);
        vrt.sizeDelta = new Vector2(thick, size);
        vrt.anchoredPosition = offset;
        canvasDecorations.Add(v);
    }

    // ─────────────────────────────────────────
    // WELCOME SCREEN (shown before the simulation starts)
    // ─────────────────────────────────────────

    void BuildWelcomeScreen()
    {
        // Minimal screen - just one button in the CENTER, nothing else (so the room behind is fully visible)
        welcomeScreen = CreatePanel("WelcomeScreen", rootRT, Color.clear);
        var rt = welcomeScreen.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // ONLY the START button - centered, floating alone
        welcomeStartBtn = CreateActionButton("WelcomeStartBtn", welcomeScreen.transform, "▶  START SIMULATION", Vector2.zero, new Vector2(600, 110), CYAN);

        // Override the default bottom-anchor of the button - center it instead
        var btnRT = welcomeStartBtn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
    }

    // ─────────────────────────────────────────
    // SELECT SCREEN
    // ─────────────────────────────────────────

    void BuildSelectScreen()
    {
        selectScreen = CreatePanel("SelectScreen", rootRT, Color.clear);
        var rt = selectScreen.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40, 40);
        rt.offsetMax = new Vector2(-40, -40);

        // Title
        var title = CreateText("Title", selectScreen.transform, "SURGERY SIMULATOR", 36, CYAN, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        var titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0, 50);
        titleRT.anchoredPosition = new Vector2(0, -10);

        var sub = CreateText("Subtitle", selectScreen.transform, "SELECT AN ORGAN TO OPERATE", 16, new Color(CYAN.r, CYAN.g, CYAN.b, 0.4f), TextAlignmentOptions.Center);
        sub.characterSpacing = 8f;
        var subRT = sub.rectTransform;
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0.5f, 1f);
        subRT.sizeDelta = new Vector2(0, 25);
        subRT.anchoredPosition = new Vector2(0, -65);

        // Divider line
        var divider = CreateImage("Divider", selectScreen.transform, new Color(CYAN.r, CYAN.g, CYAN.b, 0.1f));
        var divRT = divider.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 1);
        divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1f);
        divRT.sizeDelta = new Vector2(0, 1);
        divRT.anchoredPosition = new Vector2(0, -100);

        // 3 organ buttons - grid
        float btnW = 200f;
        float btnH = 220f;
        float spacing = 20f;
        float totalW = btnW * 3 + spacing * 2;
        float startX = -totalW / 2f + btnW / 2f;
        float btnY = -190f;

        heartBtn  = CreateOrganButton("HeartBtn", selectScreen.transform, new Vector2(startX + 0 * (btnW + spacing), btnY), new Vector2(btnW, btnH), OrganType.Heart, out heartGradeTxt, out heartLabelTxt);
        brainBtn  = CreateOrganButton("BrainBtn", selectScreen.transform, new Vector2(startX + 1 * (btnW + spacing), btnY), new Vector2(btnW, btnH), OrganType.Brain, out brainGradeTxt, out brainLabelTxt);
        lungsBtn  = CreateOrganButton("LungsBtn", selectScreen.transform, new Vector2(startX + 2 * (btnW + spacing), btnY), new Vector2(btnW, btnH), OrganType.Lungs, out lungsGradeTxt, out lungsLabelTxt);

        // Hint at bottom
        selectHintTxt = CreateText("Hint", selectScreen.transform, "CHOOSE AN ORGAN TO BEGIN", 14, new Color(CYAN.r, CYAN.g, CYAN.b, 0.3f), TextAlignmentOptions.Center);
        selectHintTxt.characterSpacing = 6f;
        var hintRT = selectHintTxt.rectTransform;
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(1, 0);
        hintRT.pivot = new Vector2(0.5f, 0f);
        hintRT.sizeDelta = new Vector2(0, 25);
        hintRT.anchoredPosition = new Vector2(0, 15);
    }

    Button CreateOrganButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, OrganType organ, out TextMeshProUGUI gradeTxt, out TextMeshProUGUI labelTxt)
    {
        Color accent = AccentFor(organ);
        Sprite sprite = SpriteFor(organ);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(accent.r, accent.g, accent.b, 0.1f);
        img.raycastTarget = true;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.6f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(accent.r, accent.g, accent.b, 0.1f);
        colors.highlightedColor = new Color(accent.r, accent.g, accent.b, 0.5f);
        colors.pressedColor = new Color(accent.r, accent.g, accent.b, 0.7f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;
        btn.targetGraphic = img;

        var hover = go.AddComponent<UiHoverEffect>();
        hover.targetOutline = outline;
        hover.accent = accent;

        // Icon Image (always created, shows sprite if available, else hidden behind text)
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(go.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = new Color(0, 0, 0, 0); // invisible
        }
        var iconRT = iconImg.rectTransform;
        iconRT.anchorMin = new Vector2(0, 1);
        iconRT.anchorMax = new Vector2(1, 1);
        iconRT.pivot = new Vector2(0.5f, 1f);
        iconRT.sizeDelta = new Vector2(0, 130);
        iconRT.anchoredPosition = new Vector2(0, -25);

        // Track for later updates
        switch (organ)
        {
            case OrganType.Heart: heartIconImg = iconImg; break;
            case OrganType.Brain: brainIconImg = iconImg; break;
            case OrganType.Lungs: lungsIconImg = iconImg; break;
        }

        // Big grade letter / icon fallback (visible only if no sprite)
        gradeTxt = CreateText("GradeFallback", go.transform, sprite != null ? "" : IconFor(organ), 90, accent, TextAlignmentOptions.Center);
        gradeTxt.fontStyle = FontStyles.Bold;
        var gradeRT = gradeTxt.rectTransform;
        gradeRT.anchorMin = new Vector2(0, 1);
        gradeRT.anchorMax = new Vector2(1, 1);
        gradeRT.pivot = new Vector2(0.5f, 1f);
        gradeRT.sizeDelta = new Vector2(0, 130);
        gradeRT.anchoredPosition = new Vector2(0, -25);

        // Small grade badge (top-right) - shown only after completion
        var badgeGO = new GameObject("GradeBadge", typeof(RectTransform));
        badgeGO.transform.SetParent(go.transform, false);
        var badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(GREEN.r, GREEN.g, GREEN.b, 0.15f);
        badgeImg.raycastTarget = false;
        var badgeOutline = badgeGO.AddComponent<Outline>();
        badgeOutline.effectColor = GREEN;
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        var badgeRT = badgeImg.rectTransform;
        badgeRT.anchorMin = new Vector2(1, 1);
        badgeRT.anchorMax = new Vector2(1, 1);
        badgeRT.pivot = new Vector2(1, 1);
        badgeRT.sizeDelta = new Vector2(40, 40);
        badgeRT.anchoredPosition = new Vector2(-8, -8);
        badgeGO.SetActive(false);

        var badgeTxt = CreateText("BadgeTxt", badgeGO.transform, "S", 22, GREEN, TextAlignmentOptions.Center);
        badgeTxt.fontStyle = FontStyles.Bold;
        var btRT = badgeTxt.rectTransform;
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero;
        btRT.offsetMax = Vector2.zero;

        // Label (organ name)
        labelTxt = CreateText("Label", go.transform, organ.ToString().ToUpper(), 18, new Color(accent.r, accent.g, accent.b, 0.7f), TextAlignmentOptions.Center);
        labelTxt.characterSpacing = 8f;
        labelTxt.fontStyle = FontStyles.Bold;
        var labelRT = labelTxt.rectTransform;
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 0);
        labelRT.pivot = new Vector2(0.5f, 0f);
        labelRT.sizeDelta = new Vector2(0, 30);
        labelRT.anchoredPosition = new Vector2(0, 35);

        // "TAP TO START" sub-label
        var tap = CreateText("Tap", go.transform, "TAP TO START", 11, new Color(accent.r, accent.g, accent.b, 0.4f), TextAlignmentOptions.Center);
        tap.characterSpacing = 4f;
        var tapRT = tap.rectTransform;
        tapRT.anchorMin = new Vector2(0, 0);
        tapRT.anchorMax = new Vector2(1, 0);
        tapRT.pivot = new Vector2(0.5f, 0f);
        tapRT.sizeDelta = new Vector2(0, 18);
        tapRT.anchoredPosition = new Vector2(0, 15);

        return btn;
    }

    // ─────────────────────────────────────────
    // SURGERY SCREEN
    // ─────────────────────────────────────────

    void BuildSurgeryScreen()
    {
        surgeryScreen = CreatePanel("SurgeryScreen", rootRT, Color.clear);
        var rt = surgeryScreen.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40, 40);
        rt.offsetMax = new Vector2(-40, -40);

        // Header row: icon + name + sub
        organIconTxt = CreateText("OrganIcon", surgeryScreen.transform, "♥", 70, RED_HEART, TextAlignmentOptions.Center);
        organIconTxt.fontStyle = FontStyles.Bold;
        var iconRT = organIconTxt.rectTransform;
        iconRT.anchorMin = new Vector2(0, 1);
        iconRT.anchorMax = new Vector2(0, 1);
        iconRT.pivot = new Vector2(0, 1);
        iconRT.sizeDelta = new Vector2(90, 90);
        iconRT.anchoredPosition = new Vector2(10, -5);

        organNameTxt = CreateText("OrganName", surgeryScreen.transform, "HEART", 32, Color.white, TextAlignmentOptions.Left);
        organNameTxt.fontStyle = FontStyles.Bold;
        organNameTxt.characterSpacing = 10f;
        var nameRT = organNameTxt.rectTransform;
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.sizeDelta = new Vector2(-110, 40);
        nameRT.anchoredPosition = new Vector2(110, -15);

        organSubTxt = CreateText("OrganSub", surgeryScreen.transform, "● LIVE · CARDIAC PROCEDURE", 13, new Color(CYAN.r, CYAN.g, CYAN.b, 0.55f), TextAlignmentOptions.Left);
        organSubTxt.characterSpacing = 4f;
        var subRT = organSubTxt.rectTransform;
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0, 1);
        subRT.sizeDelta = new Vector2(-350, 22);
        subRT.anchoredPosition = new Vector2(110, -55);

        // Surgery screen organ Image (over the text icon, shown if sprite available)
        var sIconGO = new GameObject("SurgeryOrganIcon", typeof(RectTransform));
        sIconGO.transform.SetParent(surgeryScreen.transform, false);
        surgeryOrganIconImg = sIconGO.AddComponent<Image>();
        surgeryOrganIconImg.raycastTarget = false;
        surgeryOrganIconImg.preserveAspect = true;
        surgeryOrganIconImg.color = new Color(0, 0, 0, 0);
        var sIconRT = surgeryOrganIconImg.rectTransform;
        sIconRT.anchorMin = new Vector2(0, 1);
        sIconRT.anchorMax = new Vector2(0, 1);
        sIconRT.pivot = new Vector2(0, 1);
        sIconRT.sizeDelta = new Vector2(90, 90);
        sIconRT.anchoredPosition = new Vector2(10, -5);

        // ─── Live HUD (top-right): TIMER + ATTEMPT + BPM with pulsing dot ───
        var hudBg = CreateImage("HudBg", surgeryScreen.transform, CYAN_FAINT);
        hudBg.AddComponent<Outline>().effectColor = CYAN_DIM;
        var hudRT = hudBg.GetComponent<RectTransform>();
        hudRT.anchorMin = new Vector2(1, 1);
        hudRT.anchorMax = new Vector2(1, 1);
        hudRT.pivot = new Vector2(1, 1);
        hudRT.sizeDelta = new Vector2(230, 80);
        hudRT.anchoredPosition = new Vector2(-5, -5);

        // Pulsing dot (live indicator)
        var dotGO = CreateImage("PulseDot", hudBg.transform, GREEN);
        livePulseDot = dotGO.GetComponent<Image>();
        var dotRT = dotGO.GetComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0, 1);
        dotRT.anchorMax = new Vector2(0, 1);
        dotRT.pivot = new Vector2(0, 1);
        dotRT.sizeDelta = new Vector2(10, 10);
        dotRT.anchoredPosition = new Vector2(12, -12);

        // Timer text (large, center)
        timerTxt = CreateText("Timer", hudBg.transform, "00:00", 26, Color.white, TextAlignmentOptions.Center);
        timerTxt.fontStyle = FontStyles.Bold;
        var tRT = timerTxt.rectTransform;
        tRT.anchorMin = new Vector2(0, 1);
        tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.sizeDelta = new Vector2(0, 32);
        tRT.anchoredPosition = new Vector2(0, -8);

        // Attempt + BPM row at bottom of HUD
        attemptTxt = CreateText("Attempt", hudBg.transform, "ATTEMPT #1", 10, new Color(CYAN.r, CYAN.g, CYAN.b, 0.7f), TextAlignmentOptions.Left);
        attemptTxt.characterSpacing = 3f;
        var aRT = attemptTxt.rectTransform;
        aRT.anchorMin = new Vector2(0, 0);
        aRT.anchorMax = new Vector2(0.5f, 0);
        aRT.pivot = new Vector2(0, 0);
        aRT.sizeDelta = new Vector2(0, 18);
        aRT.anchoredPosition = new Vector2(10, 8);

        bpmTxt = CreateText("BPM", hudBg.transform, "♥ 72 BPM", 10, new Color(GREEN.r, GREEN.g, GREEN.b, 0.85f), TextAlignmentOptions.Right);
        bpmTxt.characterSpacing = 2f;
        var bRT = bpmTxt.rectTransform;
        bRT.anchorMin = new Vector2(0.5f, 0);
        bRT.anchorMax = new Vector2(1, 0);
        bRT.pivot = new Vector2(1, 0);
        bRT.sizeDelta = new Vector2(0, 18);
        bRT.anchoredPosition = new Vector2(-10, 8);

        // INCISION section label
        CreateSectionLabel("IncisionLabel", surgeryScreen.transform, "INCISION SCORE", new Vector2(0, -110));

        // BIG percentage text (centered, above the bar)
        cutPctTxt = CreateText("CutPct", surgeryScreen.transform, "—", 56, new Color(1f, 1f, 1f, 0.3f), TextAlignmentOptions.Center);
        cutPctTxt.fontStyle = FontStyles.Bold;
        var pctRT = cutPctTxt.rectTransform;
        pctRT.anchorMin = new Vector2(0, 1);
        pctRT.anchorMax = new Vector2(1, 1);
        pctRT.pivot = new Vector2(0.5f, 1f);
        pctRT.sizeDelta = new Vector2(0, 70);
        pctRT.anchoredPosition = new Vector2(0, -140);

        // Progress bar track (below the percentage)
        var trackGO = CreateImage("CutTrack", surgeryScreen.transform, CYAN_FAINT);
        trackGO.AddComponent<Outline>().effectColor = CYAN_DIM;
        var trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0, 1);
        trackRT.anchorMax = new Vector2(1, 1);
        trackRT.pivot = new Vector2(0.5f, 1f);
        trackRT.sizeDelta = new Vector2(-20, 18);
        trackRT.anchoredPosition = new Vector2(0, -220);

        // Progress fill
        var fillGO = CreateImage("CutFill", trackGO.transform, RED_HEART);
        cutFillImg = fillGO.GetComponent<Image>();
        cutFillImg.type = Image.Type.Filled;
        cutFillImg.fillMethod = Image.FillMethod.Horizontal;
        cutFillImg.fillOrigin = 0;
        cutFillImg.fillAmount = 0f;
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(3, 3);
        fillRT.offsetMax = new Vector2(-3, -3);

        // Cut instruction
        cutInstructionTxt = CreateText("CutInstr", surgeryScreen.transform, "HOLD TRIGGER · CUT ALONG THE DASHED LINE", 16, new Color(CYAN.r, CYAN.g, CYAN.b, 0.7f), TextAlignmentOptions.Center);
        cutInstructionTxt.characterSpacing = 4f;
        cutInstructionTxt.fontStyle = FontStyles.Bold;
        var instrRT = cutInstructionTxt.rectTransform;
        instrRT.anchorMin = new Vector2(0, 1);
        instrRT.anchorMax = new Vector2(1, 1);
        instrRT.pivot = new Vector2(0.5f, 1f);
        instrRT.sizeDelta = new Vector2(0, 24);
        instrRT.anchoredPosition = new Vector2(0, -250);

        // LIVE STATS section label
        CreateSectionLabel("StatsLabel", surgeryScreen.transform, "LIVE STATS (UPDATES AFTER EACH CUT)", new Vector2(0, -295));

        // Stats row - 3 cards (bigger)
        float cardW = 215f;
        float cardH = 110f;
        float cardSpacing = 15f;
        float cardsTotalW = cardW * 3 + cardSpacing * 2;
        float cardStartX = -cardsTotalW / 2f + cardW / 2f;
        float cardsY = -325;

        precisionTxt = CreateStatCard("PrecisionCard", surgeryScreen.transform, new Vector2(cardStartX + 0 * (cardW + cardSpacing), cardsY), new Vector2(cardW, cardH), "PRECISION", "—", CYAN, out precisionBar);
        steadyTxt    = CreateStatCard("SteadyCard",    surgeryScreen.transform, new Vector2(cardStartX + 1 * (cardW + cardSpacing), cardsY), new Vector2(cardW, cardH), "STEADY HAND", "—", GREEN, out steadyBar);
        accuracyTxt  = CreateStatCard("AccuracyCard",  surgeryScreen.transform, new Vector2(cardStartX + 2 * (cardW + cardSpacing), cardsY), new Vector2(cardW, cardH), "ACCURACY", "—", YELLOW, out accuracyBar);

        // VIEW RESULTS button - bottom RIGHT, GREEN (continue forward)
        finishBtn = CreateActionButton("FinishBtn", surgeryScreen.transform, "▶  VIEW RESULTS", Vector2.zero, new Vector2(340, 58), GREEN);
        var finishRT = finishBtn.GetComponent<RectTransform>();
        finishRT.anchorMin = new Vector2(1, 0);
        finishRT.anchorMax = new Vector2(1, 0);
        finishRT.pivot = new Vector2(1, 0);
        finishRT.anchoredPosition = new Vector2(-20, 8);
        MakeButtonProminent(finishBtn, GREEN);

        // BACK TO MENU button - bottom LEFT, RED (go back)
        backToMenuBtn = CreateActionButton("BackToMenuBtn", surgeryScreen.transform, "◀  MENU", Vector2.zero, new Vector2(200, 58), RED_HEART);
        var backRT = backToMenuBtn.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0, 0);
        backRT.anchorMax = new Vector2(0, 0);
        backRT.pivot = new Vector2(0, 0);
        backRT.anchoredPosition = new Vector2(20, 8);
        MakeButtonProminent(backToMenuBtn, RED_HEART);
    }

    void CreateSectionLabel(string name, Transform parent, string text, Vector2 anchoredPos)
    {
        var lbl = CreateText(name, parent, text, 13, new Color(CYAN.r, CYAN.g, CYAN.b, 0.5f), TextAlignmentOptions.Left);
        lbl.characterSpacing = 6f;
        lbl.fontStyle = FontStyles.Bold;
        var rt = lbl.rectTransform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(0, 22);
        rt.anchoredPosition = anchoredPos;
    }

    TextMeshProUGUI CreateStatCard(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string label, string val, Color color, out Image fillBar)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = CYAN_FAINT;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = CYAN_DIM;
        outline.effectDistance = new Vector2(1f, -1f);

        // Label
        var lbl = CreateText("Label", go.transform, label, 11, new Color(CYAN.r, CYAN.g, CYAN.b, 0.45f), TextAlignmentOptions.Left);
        lbl.characterSpacing = 4f;
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = new Vector2(0, 1);
        lblRT.anchorMax = new Vector2(1, 1);
        lblRT.pivot = new Vector2(0, 1);
        lblRT.sizeDelta = new Vector2(0, 18);
        lblRT.anchoredPosition = new Vector2(12, -8);

        // Value (big number)
        var valTxt = CreateText("Value", go.transform, val, 30, color, TextAlignmentOptions.Left);
        valTxt.fontStyle = FontStyles.Bold;
        var valRT = valTxt.rectTransform;
        valRT.anchorMin = new Vector2(0, 1);
        valRT.anchorMax = new Vector2(1, 1);
        valRT.pivot = new Vector2(0, 1);
        valRT.sizeDelta = new Vector2(0, 40);
        valRT.anchoredPosition = new Vector2(12, -28);

        // Mini bar at bottom
        var barBg = CreateImage("BarBg", go.transform, new Color(1f, 1f, 1f, 0.06f));
        var barBgRT = barBg.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0, 0);
        barBgRT.anchorMax = new Vector2(1, 0);
        barBgRT.pivot = new Vector2(0.5f, 0f);
        barBgRT.sizeDelta = new Vector2(-24, 4);
        barBgRT.anchoredPosition = new Vector2(0, 12);

        var barFillGO = CreateImage("BarFill", barBg.transform, color);
        fillBar = barFillGO.GetComponent<Image>();
        fillBar.type = Image.Type.Filled;
        fillBar.fillMethod = Image.FillMethod.Horizontal;
        fillBar.fillOrigin = 0;
        fillBar.fillAmount = 0.9f;
        var fillRT = barFillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        return valTxt;
    }

    /// <summary>
    /// Boosts a button to be more visually prominent (thicker outline, brighter bg, larger text).
    /// </summary>
    void MakeButtonProminent(Button btn, Color accent)
    {
        if (btn == null) return;

        // CLEAN RECTANGULAR BUTTON - no glow, no double outline, just solid color + thin border
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = accent; // solid, fully opaque
        }

        // Remove the "glow" outlines, keep just ONE thin clean border
        var outlines = btn.GetComponents<Outline>();
        for (int i = 0; i < outlines.Length; i++)
        {
            if (i == 0)
            {
                // single thin border, darker version of accent
                outlines[i].effectColor = new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 1f);
                outlines[i].effectDistance = new Vector2(1.5f, -1.5f);
                outlines[i].enabled = true;
            }
            else
            {
                outlines[i].enabled = false; // disable the outer glow
            }
        }

        // Clean white text
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.fontSize = 22;
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
            txt.characterSpacing = 6f;
        }

        // Normal: muted/desaturated. Hover: full vibrant color.
        var normal = new Color(accent.r * 0.45f, accent.g * 0.45f, accent.b * 0.45f, 1f); // weaker version
        var hover  = new Color(accent.r, accent.g, accent.b, 1f);                          // full color
        var pressed = new Color(accent.r * 0.75f, accent.g * 0.75f, accent.b * 0.75f, 1f); // slightly darker

        var colors = btn.colors;
        colors.normalColor      = normal;
        colors.highlightedColor = hover;
        colors.pressedColor     = pressed;
        colors.selectedColor    = normal;   // identical to normal
        colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        colors.colorMultiplier  = 1f;
        colors.fadeDuration     = 0.1f;
        btn.colors = colors;

        // Disable navigation
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        // No scale change on hover
        var hoverEff = btn.GetComponent<UiHoverEffect>();
        if (hoverEff != null) hoverEff.hoverScale = 1f;
    }

    Button CreateActionButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color accent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = new Color(accent.r, accent.g, accent.b, 0.25f);  // Stronger visible background

        // Thick double-outline glow
        var outline = go.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = new Vector2(3f, -3f);

        var outline2 = go.AddComponent<Outline>();
        outline2.effectColor = new Color(accent.r, accent.g, accent.b, 0.4f);
        outline2.effectDistance = new Vector2(6f, -6f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(accent.r, accent.g, accent.b, 0.25f);
        colors.highlightedColor = new Color(accent.r, accent.g, accent.b, 0.7f);   // Visible jump
        colors.pressedColor = new Color(accent.r, accent.g, accent.b, 0.9f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        var txt = CreateText("Label", go.transform, label, 22, Color.white, TextAlignmentOptions.Center);
        txt.fontStyle = FontStyles.Bold;
        txt.characterSpacing = 10f;
        var txtRT = txt.rectTransform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        // Add HoverEffect for dramatic visual feedback (scale + outline glow)
        var hover = go.AddComponent<UiHoverEffect>();
        hover.targetOutline = outline;
        hover.targetOutline2 = outline2;
        hover.accent = accent;

        return btn;
    }

    // ─────────────────────────────────────────
    // SUMMARY SCREEN
    // ─────────────────────────────────────────

    void BuildSummaryScreen()
    {
        summaryScreen = CreatePanel("SummaryScreen", rootRT, Color.clear);
        var rt = summaryScreen.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40, 30);
        rt.offsetMax = new Vector2(-40, -30);

        // ── Title ──
        var title = CreateText("Title", summaryScreen.transform, "PROCEDURE COMPLETE", 32, CYAN, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 10f;
        var titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0, 45);
        titleRT.anchoredPosition = new Vector2(0, 0);

        summarySubTxt = CreateText("Subtitle", summaryScreen.transform, "HEART · PROCEDURE COMPLETE", 16, new Color(CYAN.r, CYAN.g, CYAN.b, 0.6f), TextAlignmentOptions.Center);
        summarySubTxt.characterSpacing = 5f;
        var sumSubRT = summarySubTxt.rectTransform;
        sumSubRT.anchorMin = new Vector2(0, 1);
        sumSubRT.anchorMax = new Vector2(1, 1);
        sumSubRT.pivot = new Vector2(0.5f, 1f);
        sumSubRT.sizeDelta = new Vector2(0, 26);
        sumSubRT.anchoredPosition = new Vector2(0, -50);

        // Divider
        var divider = CreateImage("Divider", summaryScreen.transform, new Color(CYAN.r, CYAN.g, CYAN.b, 0.15f));
        var divRT = divider.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 1);
        divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1f);
        divRT.sizeDelta = new Vector2(-100, 1);
        divRT.anchoredPosition = new Vector2(0, -85);

        // ── Organ Icon (small, on the left) ──
        summaryIconTxt = CreateText("Icon", summaryScreen.transform, "♥", 60, RED_HEART, TextAlignmentOptions.Center);
        summaryIconTxt.fontStyle = FontStyles.Bold;
        var iconRT = summaryIconTxt.rectTransform;
        iconRT.anchorMin = new Vector2(0, 1);
        iconRT.anchorMax = new Vector2(0, 1);
        iconRT.pivot = new Vector2(0, 1);
        iconRT.sizeDelta = new Vector2(80, 80);
        iconRT.anchoredPosition = new Vector2(20, -105);

        var sumIconGO = new GameObject("SummaryOrganIcon", typeof(RectTransform));
        sumIconGO.transform.SetParent(summaryScreen.transform, false);
        summaryOrganIconImg = sumIconGO.AddComponent<Image>();
        summaryOrganIconImg.raycastTarget = false;
        summaryOrganIconImg.preserveAspect = true;
        summaryOrganIconImg.color = new Color(0, 0, 0, 0);
        var sumIconRT = summaryOrganIconImg.rectTransform;
        sumIconRT.anchorMin = new Vector2(0, 1);
        sumIconRT.anchorMax = new Vector2(0, 1);
        sumIconRT.pivot = new Vector2(0, 1);
        sumIconRT.sizeDelta = new Vector2(80, 80);
        sumIconRT.anchoredPosition = new Vector2(20, -105);

        // ── Big Success/Fail message (center) ──
        gradeBigTxt = CreateText("SuccessMsg", summaryScreen.transform, "SURGERY SUCCEEDED", 36, GREEN, TextAlignmentOptions.Center);
        gradeBigTxt.fontStyle = FontStyles.Bold;
        gradeBigTxt.characterSpacing = 6f;
        var gradeRT = gradeBigTxt.rectTransform;
        gradeRT.anchorMin = new Vector2(0, 1);
        gradeRT.anchorMax = new Vector2(1, 1);
        gradeRT.pivot = new Vector2(0.5f, 1f);
        gradeRT.sizeDelta = new Vector2(-220, 50);
        gradeRT.anchoredPosition = new Vector2(0, -115);

        // ── Final percentage (huge number, center) ──
        sumPrecisionTxt = CreateText("FinalScore", summaryScreen.transform, "87%", 70, GREEN, TextAlignmentOptions.Center);
        sumPrecisionTxt.fontStyle = FontStyles.Bold;
        var finalScoreRT = sumPrecisionTxt.rectTransform;
        finalScoreRT.anchorMin = new Vector2(0, 1);
        finalScoreRT.anchorMax = new Vector2(1, 1);
        finalScoreRT.pivot = new Vector2(0.5f, 1f);
        finalScoreRT.sizeDelta = new Vector2(0, 90);
        finalScoreRT.anchoredPosition = new Vector2(0, -170);

        // ── Time + Grade row (under the score) ──
        sumSteadyTxt = CreateText("TimeText", summaryScreen.transform, "⏱ TIME: 01:23", 18, new Color(CYAN.r, CYAN.g, CYAN.b, 0.85f), TextAlignmentOptions.Center);
        sumSteadyTxt.fontStyle = FontStyles.Bold;
        sumSteadyTxt.characterSpacing = 4f;
        var timeRT = sumSteadyTxt.rectTransform;
        timeRT.anchorMin = new Vector2(0, 1);
        timeRT.anchorMax = new Vector2(0.5f, 1);
        timeRT.pivot = new Vector2(0.5f, 1f);
        timeRT.sizeDelta = new Vector2(0, 26);
        timeRT.anchoredPosition = new Vector2(0, -265);

        sumAccuracyTxt = CreateText("GradeText", summaryScreen.transform, "GRADE: A", 18, YELLOW, TextAlignmentOptions.Center);
        sumAccuracyTxt.fontStyle = FontStyles.Bold;
        sumAccuracyTxt.characterSpacing = 4f;
        var gradeTxtRT = sumAccuracyTxt.rectTransform;
        gradeTxtRT.anchorMin = new Vector2(0.5f, 1);
        gradeTxtRT.anchorMax = new Vector2(1, 1);
        gradeTxtRT.pivot = new Vector2(0.5f, 1f);
        gradeTxtRT.sizeDelta = new Vector2(0, 26);
        gradeTxtRT.anchoredPosition = new Vector2(0, -265);

        // ── Big visual score bar ──
        var barTrack = CreateImage("ScoreBarTrack", summaryScreen.transform, CYAN_FAINT);
        barTrack.AddComponent<Outline>().effectColor = CYAN_DIM;
        var barTrackRT = barTrack.GetComponent<RectTransform>();
        barTrackRT.anchorMin = new Vector2(0, 1);
        barTrackRT.anchorMax = new Vector2(1, 1);
        barTrackRT.pivot = new Vector2(0.5f, 1f);
        barTrackRT.sizeDelta = new Vector2(-40, 24);
        barTrackRT.anchoredPosition = new Vector2(0, -305);

        var barFill = CreateImage("ScoreBarFill", barTrack.transform, GREEN);
        var sumBarImg = barFill.GetComponent<Image>();
        sumBarImg.type = Image.Type.Filled;
        sumBarImg.fillMethod = Image.FillMethod.Horizontal;
        sumBarImg.fillOrigin = 0;
        sumBarImg.fillAmount = 0.87f;
        sumBarImg.name = "SummaryScoreFill";
        var barFillRT = barFill.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = new Vector2(3, 3);
        barFillRT.offsetMax = new Vector2(-3, -3);

        // ── Feedback box ──
        var fbBg = CreateImage("FeedbackBg", summaryScreen.transform, new Color(GREEN.r, GREEN.g, GREEN.b, 0.05f));
        fbBg.AddComponent<Outline>().effectColor = new Color(GREEN.r, GREEN.g, GREEN.b, 0.25f);
        var fbBgRT = fbBg.GetComponent<RectTransform>();
        fbBgRT.anchorMin = new Vector2(0, 0);
        fbBgRT.anchorMax = new Vector2(1, 0);
        fbBgRT.pivot = new Vector2(0.5f, 0f);
        fbBgRT.sizeDelta = new Vector2(0, 80);
        fbBgRT.anchoredPosition = new Vector2(0, 105);

        feedbackTxt = CreateText("Feedback", fbBg.transform, "Flawless cardiac incision.", 17, new Color(1f, 1f, 1f, 0.92f), TextAlignmentOptions.Center);
        var fbRT = feedbackTxt.rectTransform;
        fbRT.anchorMin = Vector2.zero;
        fbRT.anchorMax = Vector2.one;
        fbRT.offsetMin = new Vector2(20, 10);
        fbRT.offsetMax = new Vector2(-20, -10);

        // ── Back button (big, obvious) ──
        backBtn = CreateActionButton("BackBtn", summaryScreen.transform, "◀  BACK TO MENU", new Vector2(0, 15), new Vector2(500, 70), CYAN);
    }

    // ─────────────────────────────────────────
    // Primitive UI builders
    // ─────────────────────────────────────────

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    GameObject CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align)
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

/// <summary>
/// אפקט hover ויזואלי בולט - מגדיל את הכפתור ומחזק את הזוהר כשהלייזר עליו.
/// מתווסף אוטומטית לכל כפתור שנוצר ע"י SurgeryUIManager.
/// </summary>
public class UiHoverEffect : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public UnityEngine.UI.Outline targetOutline;
    public UnityEngine.UI.Outline targetOutline2;
    public Color accent = Color.cyan;
    public float hoverScale = 1.06f;

    Vector3 originalScale;
    Color originalOutlineColor;
    Color originalOutline2Color;
    Vector2 originalOutlineDist;

    void Awake()
    {
        originalScale = transform.localScale;
        if (targetOutline != null)
        {
            originalOutlineColor = targetOutline.effectColor;
            originalOutlineDist = targetOutline.effectDistance;
        }
        if (targetOutline2 != null)
        {
            originalOutline2Color = targetOutline2.effectColor;
        }
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // If hoverScale is 1f, this button opts out of all hover visual changes
        // (it relies only on Button.colors block for hover feedback)
        if (Mathf.Approximately(hoverScale, 1f)) return;

        transform.localScale = originalScale * hoverScale;
        if (targetOutline != null)
        {
            targetOutline.effectColor = new Color(
                Mathf.Lerp(accent.r, 1f, 0.4f),
                Mathf.Lerp(accent.g, 1f, 0.4f),
                Mathf.Lerp(accent.b, 1f, 0.4f),
                1f);
            targetOutline.effectDistance = originalOutlineDist * 1.6f;
        }
        if (targetOutline2 != null)
        {
            targetOutline2.effectColor = new Color(accent.r, accent.g, accent.b, 0.9f);
        }
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (Mathf.Approximately(hoverScale, 1f)) return;

        transform.localScale = originalScale;
        if (targetOutline != null)
        {
            targetOutline.effectColor = originalOutlineColor;
            targetOutline.effectDistance = originalOutlineDist;
        }
        if (targetOutline2 != null)
        {
            targetOutline2.effectColor = originalOutline2Color;
        }
    }
}
