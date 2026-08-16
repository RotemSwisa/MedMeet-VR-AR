using Normal.Realtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// MedMeet Tools → Setup Sustainability Showcase  (v4 — VR-readable redesign)
///
/// Builds three large, clean canvases:
///   1. SetupCanvas    — per-client participant rows + Calculate CTA
///   2. LoadingCanvas  — Earth + orbiting plane + progress bar
///   3. DashboardCanvas— Hero CO2 + 3 resource cards + journeys + donut
///
/// Design priorities (from VR field-testing):
///   • Big readable text (a single "scale" multiplier governs everything).
///   • Generous padding, fewer decorative elements.
///   • bg-dark.png from the Claude Design sits behind every screen.
///   • Canvas scale 0.0024 → ≈ 4.6m × 2.6m world-space (≈ 2.2× the old size).
/// </summary>
public static class DashboardSetupTool
{
    // ── Sprite paths ────────────────────────────────────────────────────────
    const string GlobePath    = SustainabilityTheme.PathGlobe;
    const string AirplanePath = SustainabilityTheme.PathAirplane;
    const string LogoPath     = SustainabilityTheme.PathLogo;
    const string BgPath       = "Assets/MedMeet/Dashboard/Sprites/bg-dark.png";

    // ── Canvas scale (world units per UI pixel). 0.0024 → 4.6m × 2.6m ──────
    const float CanvasScale = 0.0024f;

    static Sprite _globe, _airplane, _logo, _bg;
    static TMP_FontAsset _font;

    // ════════════════════════════════════════════════════════════════════════
    //  Menu entry
    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("MedMeet Tools/Setup Sustainability Showcase")]
    public static void Run()
    {
        var sceneSetup = Object.FindFirstObjectByType<DashboardSceneSetup>();
        if (sceneSetup == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "DashboardSceneSetup not found in the scene.", "OK");
            return;
        }

        // Auto-create any missing canvases using the first assigned one as template.
        GameObject template = sceneSetup.setupCanvas
                           ?? sceneSetup.dashboardCanvas
                           ?? sceneSetup.loadingCanvas;
        if (template == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "Assign at least one Canvas to DashboardSceneSetup first.", "OK");
            return;
        }
        var createdList = new System.Collections.Generic.List<string>();
        if (sceneSetup.setupCanvas == null)
            sceneSetup.setupCanvas = FindOrCreateCanvasShell(template, "SetupCanvas", createdList);
        if (sceneSetup.loadingCanvas == null)
            sceneSetup.loadingCanvas = FindOrCreateCanvasShell(template, "LoadingCanvas", createdList);
        if (sceneSetup.dashboardCanvas == null)
            sceneSetup.dashboardCanvas = FindOrCreateCanvasShell(template, "DashboardCanvas", createdList);
        if (createdList.Count > 0) EditorUtility.SetDirty(sceneSetup);

        FixSpriteImports(GlobePath, AirplanePath, LogoPath, BgPath);

        _globe    = LoadSprite(GlobePath);
        _airplane = LoadSprite(AirplanePath);
        _logo     = LoadSprite(LogoPath);
        _bg       = LoadSprite(BgPath);
        _font     = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        Undo.SetCurrentGroupName("Setup Sustainability Showcase v4");
        int undoGroup = Undo.GetCurrentGroup();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Sustainability Showcase build ===");
        if (createdList.Count > 0)
            report.AppendLine("AUTO-CREATED: " + string.Join(", ", createdList));
        report.AppendLine($"Sprites — logo:{(_logo != null ? "OK" : "MISSING")}, " +
                          $"globe:{(_globe != null ? "OK" : "MISSING")}, " +
                          $"airplane:{(_airplane != null ? "OK" : "MISSING")}, " +
                          $"bg:{(_bg != null ? "OK" : "MISSING")}");

        try
        {
            FixRaycaster(sceneSetup.setupCanvas);
            FixRaycaster(sceneSetup.loadingCanvas);
            FixRaycaster(sceneSetup.dashboardCanvas);

            NormaliseCanvasSize(sceneSetup.setupCanvas);
            NormaliseCanvasSize(sceneSetup.loadingCanvas);
            NormaliseCanvasSize(sceneSetup.dashboardCanvas);

            BuildSetupCanvas    (sceneSetup.setupCanvas);     report.AppendLine("✓ Setup built");
            BuildLoadingCanvas  (sceneSetup.loadingCanvas);   report.AppendLine("✓ Loading built");
            BuildDashboardCanvas(sceneSetup.dashboardCanvas); report.AppendLine("✓ Dashboard built");

            SetupDashboardSyncObject(sceneSetup);
            report.AppendLine("✓ DashboardSync ready");

            PositionCanvasesAtAnchor(sceneSetup.setupCanvas,
                                     sceneSetup.loadingCanvas,
                                     sceneSetup.dashboardCanvas);
            report.AppendLine($"✓ Canvases scaled to {CanvasScale:F4} (≈ {1920 * CanvasScale:F2}m × {1080 * CanvasScale:F2}m)");

            sceneSetup.setupCanvas    .SetActive(false);
            sceneSetup.loadingCanvas  .SetActive(false);
            sceneSetup.dashboardCanvas.SetActive(false);
        }
        catch (System.Exception ex)
        {
            report.AppendLine("✗ BUILD FAILED: " + ex.Message);
            Debug.LogException(ex);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(sceneSetup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log(report);
        EditorUtility.DisplayDialog("Sustainability Showcase v4", report + "\n\nCtrl+S to save, then Play.", "OK");
    }

    [MenuItem("MedMeet Tools/Setup Dashboard UI")]
    public static void RunLegacyAlias() => Run();

    // ════════════════════════════════════════════════════════════════════════
    //  DashboardSync (Normcore) scene object
    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("MedMeet Tools/Setup Dashboard Sync (Normcore)")]
    public static void SetupDashboardSyncMenu()
    {
        var sceneSetup = Object.FindFirstObjectByType<DashboardSceneSetup>();
        if (sceneSetup == null) { EditorUtility.DisplayDialog("Failed", "Missing DashboardSceneSetup", "OK"); return; }
        SetupDashboardSyncObject(sceneSetup);
        EditorUtility.DisplayDialog("Dashboard Sync ✅", "Sync object ready.", "OK");
    }

    private static void SetupDashboardSyncObject(DashboardSceneSetup sceneSetup)
    {
        const string SyncObjectName = "DashboardSyncManager";
        var existing = Object.FindFirstObjectByType<DashboardSync>();
        GameObject syncGO = existing != null ? existing.gameObject : GameObject.Find(SyncObjectName);
        if (syncGO == null)
        {
            syncGO = new GameObject(SyncObjectName);
            Undo.RegisterCreatedObjectUndo(syncGO, "Create DashboardSyncManager");
        }
        if (syncGO.GetComponent<RealtimeView>() == null)
            Undo.AddComponent<RealtimeView>(syncGO);
        var sync = syncGO.GetComponent<DashboardSync>() ?? Undo.AddComponent<DashboardSync>(syncGO);
        Undo.RecordObject(sync, "Wire DashboardSync");
        sync.sceneSetup = sceneSetup;
        sync.realtime   = Object.FindFirstObjectByType<Realtime>();
        EditorUtility.SetDirty(sync);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CANVAS 1 — Setup  (clean, large rows)
    // ════════════════════════════════════════════════════════════════════════
    static void BuildSetupCanvas(GameObject canvasGO)
    {
        WipeChildren(canvasGO);
        var root = canvasGO.transform;
        BuildBackground(root);

        // Header band (148 px) — logo on left, brand text, "Sustainability" tag on right
        var header = MakePanel(root, "Header",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg0, 0.55f),
            V(0, 1), V(1, 1), V(0, -148), V(0, 0));
        BuildHeader(header.transform);

        // ───────── Body grid ─────────
        // Left col 1.4fr (participants) | Right col 1fr (live summary)
        var leftCol = MakeRT(root, "LeftCol",
            V(0, 0), V(0.60f, 1), V(80, 64), V(-24, -180));
        var rightCol = MakeRT(root, "RightCol",
            V(0.60f, 0), V(1, 1), V(24, 64), V(-80, -180));

        // ── LEFT — title + rows ──────────────────────────────────────────
        MakeText(leftCol.transform, "Eyebrow", "STEP 1   ·   MAP THE MEETING",
            26, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(0, -40), V(0, -4),
            TextAlignmentOptions.Left, letterSpacing: 22f);

        MakeText(leftCol.transform, "Title", "Where is everyone joining from?",
            72, true, Color.white,
            V(0, 1), V(1, 1), V(0, -160), V(0, -52),
            TextAlignmentOptions.TopLeft);

        MakeText(leftCol.transform, "Subtitle",
            "Each participant adds themselves and picks the city they'd otherwise " +
            "travel from. We compare that to meeting inside <b><color=#7fe6e0>MedMeet VR</color></b>.",
            32, false, SustainabilityTheme.InkSoft,
            V(0, 1), V(1, 1), V(0, -260), V(0, -170),
            TextAlignmentOptions.TopLeft, wrap: true);

        // Rows scroll area (huge, room for 6 rows)
        var rowsScroll = MakeRT(leftCol.transform, "RowsScroll",
            V(0, 0), V(1, 1), V(0, 230), V(0, -290));
        var rowsContent = MakeRT(rowsScroll.transform, "Content",
            V(0, 1), V(1, 1), V(0, -400), V(0, 0));
        var vlg = rowsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20; vlg.padding = new RectOffset(0, 16, 0, 0);
        vlg.childControlWidth = true;  vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        rowsContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit
            = ContentSizeFitter.FitMode.PreferredSize;

        // "Add me" dashed button under the rows
        var addBtn = MakeButton(leftCol.transform, "AddParticipantBtn", "＋  Add me",
            32, SustainabilityTheme.Teal,
            SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.10f),
            V(0, 0), V(1, 0), V(0, 144), V(0, 222));
        TintBorder(addBtn.gameObject, SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.55f), thickness: 2);

        // Footer — demos stepper + readiness + Calculate
        var footer = MakeRT(leftCol.transform, "Footer",
            V(0, 0), V(1, 0), V(0, 0), V(0, 122));

        // Demos card (cleaner — single label, big number, two buttons)
        var demosCard = MakePanel(footer.transform, "DemosCard",
            SustainabilityTheme.Card, V(0, 0), V(0.40f, 1), V(0, 8), V(0, -8));
        TintBorder(demosCard, SustainabilityTheme.Line);
        MakeText(demosCard.transform, "Lbl", "HANDS-ON DEMOS",
            18, true, SustainabilityTheme.InkFaint,
            V(0, 0.55f), V(1, 1), V(24, 0), V(-12, -10),
            TextAlignmentOptions.BottomLeft, letterSpacing: 12f);

        var minusBtn = MakeButton(demosCard.transform, "Minus", "−",
            38, SustainabilityTheme.Teal, SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.12f),
            V(0, 0), V(0, 1), V(20, 10), V(80, -42));
        var demosLabel = MakeText(demosCard.transform, "Count", "3",
            48, true, Color.white,
            V(0, 0), V(1, 1), V(90, 10), V(-90, -42),
            TextAlignmentOptions.Center);
        var plusBtn = MakeButton(demosCard.transform, "Plus", "+",
            38, SustainabilityTheme.Teal, SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.12f),
            V(1, 0), V(1, 1), V(-80, 10), V(-20, -42));

        var readinessLabel = MakeText(footer.transform, "Readiness",
            "Add at least 2 participants", 22, true, SustainabilityTheme.Clay,
            V(0.42f, 0.20f), V(0.66f, 0.80f), V(8, 0), V(-8, 0),
            TextAlignmentOptions.MidlineRight);

        var calcBtn = MakeButton(footer.transform, "CalculateBtn", "Calculate impact  →",
            30, new Color(0.02f, 0.13f, 0.15f),
            SustainabilityTheme.Teal, V(0.66f, 0), V(1, 1), V(8, 8), V(0, -8));

        // ── RIGHT — summary panel (clean) ────────────────────────────────
        var rightPanel = MakePanel(rightCol.transform, "RightPanel",
            SustainabilityTheme.Card, V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        TintBorder(rightPanel, SustainabilityTheme.Line);

        // Logo at top
        if (_logo != null)
        {
            var logoImg = MakeImageGO(rightPanel.transform, "Logo", _logo,
                V(0.5f, 1), V(0.5f, 1), V(-110, -260), V(110, -40));
            logoImg.GetComponent<Image>().preserveAspect = true;
        }
        MakeText(rightPanel.transform, "BrandName", "MedMeet",
            54, true, Color.white,
            V(0, 1), V(1, 1), V(0, -340), V(0, -270),
            TextAlignmentOptions.Center);
        MakeText(rightPanel.transform, "BrandSub", "SUSTAINABILITY SHOWCASE",
            20, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(0, -376), V(0, -344),
            TextAlignmentOptions.Center, letterSpacing: 14f);

        MakeText(rightPanel.transform, "JourneysHeader", "Journeys you're replacing",
            28, true, Color.white,
            V(0, 1), V(1, 1), V(40, -440), V(-40, -390),
            TextAlignmentOptions.Left);

        var journeysScroll  = MakeRT(rightPanel.transform, "JourneysScroll",
            V(0, 0), V(1, 1), V(32, 180), V(-32, -450));
        var journeysContent = MakeRT(journeysScroll.transform, "Content",
            V(0, 1), V(1, 1), V(0, -200), V(0, 0));
        var jVlg = journeysContent.gameObject.AddComponent<VerticalLayoutGroup>();
        jVlg.spacing = 14; jVlg.childControlWidth = true; jVlg.childControlHeight = false;
        jVlg.childForceExpandWidth = true;
        journeysContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit
            = ContentSizeFitter.FitMode.PreferredSize;

        var journeysEmpty = MakeText(journeysScroll.transform, "EmptyMessage",
            "Each participant adds themselves to map the journeys you're saving.",
            24, false, SustainabilityTheme.InkSoft,
            V(0, 0), V(1, 1), V(20, 20), V(-20, -20),
            TextAlignmentOptions.Center, wrap: true);

        // Tally row
        var tallyRow = MakeRT(rightPanel.transform, "Tallies",
            V(0, 0), V(1, 0), V(24, 28), V(-24, 160));
        var tHlg = tallyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tHlg.spacing = 14; tHlg.childControlWidth = true; tHlg.childControlHeight = true;
        tHlg.childForceExpandWidth = true; tHlg.childForceExpandHeight = true;

        var tLoc = BuildTally(tallyRow.transform, "TallyLocations", "LOCATIONS",     "0",  SustainabilityTheme.IconPin);
        var tRou = BuildTally(tallyRow.transform, "TallyRoutes",    "ROUTES",        "0",  SustainabilityTheme.IconRoute);
        var tKm  = BuildTally(tallyRow.transform, "TallyKm",        "ROUND-TRIP KM", "~0", SustainabilityTheme.IconRoad);

        // ── Prefabs spawned at runtime ────────────────────────────────────
        var rowPrefab     = BuildParticipantRowPrefab(canvasGO.transform);
        var journeyPrefab = BuildJourneyRowPrefab(canvasGO.transform);
        var popup         = BuildCitySelectPopup(canvasGO.transform);

        // ── Wire controller ───────────────────────────────────────────────
        var ui = canvasGO.GetComponent<ParticipantSetupUI>()
              ?? Undo.AddComponent<ParticipantSetupUI>(canvasGO);
        Undo.RecordObject(ui, "Wire ParticipantSetupUI");
        ui.rowsContainer        = rowsContent;
        ui.participantRowPrefab = rowPrefab;
        ui.addParticipantButton = addBtn;
        ui.calculateButton      = calcBtn;
        ui.readinessLabel       = readinessLabel;
        ui.demosLabel           = demosLabel;
        ui.demosMinusButton     = minusBtn;
        ui.demosPlusButton      = plusBtn;
        ui.journeysContainer    = journeysContent;
        ui.journeyRowPrefab     = journeyPrefab;
        ui.journeysEmptyLabel   = journeysEmpty;
        ui.tallyLocationsValue  = tLoc.value;
        ui.tallyRoutesValue     = tRou.value;
        ui.tallyKmValue         = tKm.value;
        ui.citySelectPopup      = popup;
        ui.stepDots             = null;
        EditorUtility.SetDirty(ui);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CANVAS 2 — Loading  (minimal, centred)
    // ════════════════════════════════════════════════════════════════════════
    static void BuildLoadingCanvas(GameObject canvasGO)
    {
        WipeChildren(canvasGO);
        var root = canvasGO.transform;
        BuildBackground(root);

        var header = MakePanel(root, "Header",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg0, 0.55f),
            V(0, 1), V(1, 1), V(0, -148), V(0, 0));
        BuildHeader(header.transform);

        var stage = MakeRT(root, "Stage",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-660, -440), V(660, 380));

        MakeText(stage.transform, "Eyebrow", "STEP 2  ·  COMPUTING",
            26, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(0, -50), V(0, 0),
            TextAlignmentOptions.Center, letterSpacing: 22f);
        var title = MakeText(stage.transform, "Title", "Your results are arriving…",
            58, true, Color.white,
            V(0, 1), V(1, 1), V(0, -120), V(0, -54),
            TextAlignmentOptions.Center);

        // Orbit area — square 700, no concentric rings (cleaner)
        var orbit = MakeRT(stage.transform, "OrbitArea",
            V(0.5f, 1), V(0.5f, 1), V(-360, -880), V(360, -160));

        // ── Earth glow stack (drawn outermost → innermost). Calmer palette to
        // sit nicely against the dark background; the inner backplate sharpens
        // Earth's silhouette so it doesn't bleed into the glow. ─────────────

        // 1) Soft outer halo (bottom layer, biggest)
        var glow = MakeRT(orbit.transform, "EarthGlow",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-310, -310), V(310, 310));
        var glowImg = glow.gameObject.AddComponent<Image>();
        glowImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        glowImg.color = new Color(0.150f, 0.340f, 0.420f, 0.22f);  // soft slate
        glowImg.preserveAspect = true;
        glowImg.raycastTarget = false;

        // 2) Thin teal ring — crisp circular outline
        var ringHolder = MakeRT(orbit.transform, "EarthRing",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-258, -258), V(258, 258));
        var ringImg = ringHolder.gameObject.AddComponent<Image>();
        ringImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        ringImg.color = new Color(0.180f, 0.360f, 0.450f, 0.55f);  // muted teal
        ringImg.preserveAspect = true;
        ringImg.raycastTarget = false;

        // 3) Backplate — deep navy disc behind Earth itself, sharpens the edge
        var backplate = MakeRT(orbit.transform, "EarthBackplate",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-248, -248), V(248, 248));
        var backImg = backplate.gameObject.AddComponent<Image>();
        backImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        backImg.color = new Color(0.058f, 0.137f, 0.196f, 0.92f);
        backImg.preserveAspect = true;
        backImg.raycastTarget = false;

        // Earth root
        var earthRoot = MakeRT(orbit.transform, "EarthRoot",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-220, -220), V(220, 220));
        if (_globe != null)
            MakeImageGO(earthRoot.transform, "EarthImage", _globe,
                V(0, 0), V(1, 1), V(0, 0), V(0, 0));

        // Plane orbit
        var orbitRoot = MakeRT(orbit.transform, "OrbitRoot",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(0, 0));
        var airplaneRoot = MakeRT(orbitRoot.transform, "AirplaneRoot",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-72, 290), V(72, 434));
        if (_airplane != null)
            MakeImageGO(airplaneRoot.transform, "AirplaneImage", _airplane,
                V(0, 0), V(1, 1), V(0, 0), V(0, 0));

        // Progress + message
        var progArea = MakeRT(stage.transform, "ProgArea",
            V(0, 0), V(1, 0), V(80, 0), V(-80, 140));

        var message = MakeText(progArea.transform, "Message",
            "Mapping the journeys you replaced…",
            30, true, SustainabilityTheme.TealSoft,
            V(0, 0.55f), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.Center);

        var barBg = MakePanel(progArea.transform, "BarTrack",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f),
            V(0, 0.30f), V(1, 0.45f), V(80, 0), V(-80, 0));
        TintBorder(barBg, SustainabilityTheme.LineSoft);

        var barFill = MakePanel(barBg.transform, "BarFill",
            SustainabilityTheme.Teal, V(0, 0), V(1, 1), V(3, 3), V(-3, -3));
        barFill.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        var pctLabel = MakeText(progArea.transform, "Percent", "0%",
            24, true, Color.white,
            V(0, 0), V(1, 0.25f), V(0, 0), V(0, 0),
            TextAlignmentOptions.Center, letterSpacing: 4f);

        var ctrl = canvasGO.GetComponent<LoadingCanvasController>()
                ?? Undo.AddComponent<LoadingCanvasController>(canvasGO);
        Undo.RecordObject(ctrl, "Wire LoadingCanvasController");
        ctrl.earthRoot     = earthRoot;
        ctrl.glowRoot      = glow;
        ctrl.glowImage     = glowImg;
        ctrl.orbitRoot     = orbitRoot;
        ctrl.airplaneRoot  = airplaneRoot;
        ctrl.progressFill  = barFill.GetComponent<RectTransform>();
        ctrl.progressLabel = pctLabel;
        ctrl.messageLabel  = message;
        ctrl.titleLabel    = title;
        ctrl.stepDots      = null;
        ctrl.planeOrbitRadius = 360f;   // matches the airplaneRoot offset
        EditorUtility.SetDirty(ctrl);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CANVAS 3 — Dashboard  (cleaner two-column, larger numbers)
    // ════════════════════════════════════════════════════════════════════════
    static void BuildDashboardCanvas(GameObject canvasGO)
    {
        WipeChildren(canvasGO);
        var root = canvasGO.transform;
        BuildBackground(root);

        var header = MakePanel(root, "Header",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg0, 0.55f),
            V(0, 1), V(1, 1), V(0, -148), V(0, 0));
        BuildHeader(header.transform);

        var body = MakeRT(root, "Body",
            V(0, 0), V(1, 1), V(0, 36), V(0, -180));
        var leftCol  = MakeRT(body.transform, "HeroHolder",
            V(0, 0), V(0.40f, 1), V(64, 0), V(8, 0));
        var rightCol = MakeRT(body.transform, "RightCol",
            V(0.40f, 0), V(1, 1), V(8, 0), V(-64, 0));

        // ── LEFT — hero CO2 panel ─────────────────────────────────────────
        var hero = MakePanel(leftCol.transform, "HeroPanel",
            SustainabilityTheme.Tint(new Color(0.058f, 0.180f, 0.247f), 0.96f),
            V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        TintBorder(hero, SustainabilityTheme.Line);

        // Decorative Earth in the bottom-right corner of the hero (subtle, no raycast)
        if (_globe != null)
        {
            var heroEarth = MakeImageGO(hero.transform, "HeroEarth", _globe,
                V(1, 0), V(1, 0), V(-260, 30), V(-10, 280),
                new Color(1f, 1f, 1f, 0.18f));
            var heroEarthImg = heroEarth.GetComponent<Image>();
            heroEarthImg.preserveAspect = true;
            heroEarthImg.raycastTarget = false;
        }

        if (_logo != null)
            MakeImageGO(hero.transform, "HeroLogo", _logo,
                V(0, 1), V(0, 1), V(40, -130), V(130, -40))
                .GetComponent<Image>().preserveAspect = true;

        MakeText(hero.transform, "HeroBrand", "MedMeet",
            40, true, Color.white,
            V(0, 1), V(1, 1), V(150, -90), V(-30, -40),
            TextAlignmentOptions.BottomLeft);
        MakeText(hero.transform, "HeroBrandSub", "IMPACT REPORT",
            18, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(150, -128), V(-30, -94),
            TextAlignmentOptions.TopLeft, letterSpacing: 14f);

        MakeText(hero.transform, "Eyebrow", "CO₂ EMISSIONS AVOIDED",
            22, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(40, -180), V(-30, -148),
            TextAlignmentOptions.Left, letterSpacing: 18f);

        var co2Val = MakeText(hero.transform, "Co2Value", "0",
            150, true, Color.white,
            V(0, 1), V(0.78f, 1), V(40, -360), V(-10, -180),
            TextAlignmentOptions.TopLeft);
        var co2Unit = MakeText(hero.transform, "Co2Unit", "kg",
            42, true, SustainabilityTheme.TealSoft,
            V(0.6f, 1), V(1, 1), V(0, -340), V(-30, -260),
            TextAlignmentOptions.BottomLeft);

        var pillBg = MakePanel(hero.transform, "TreesPill",
            SustainabilityTheme.MintWash,
            V(0, 1), V(1, 1), V(40, -420), V(-180, -370));
        TintBorder(pillBg, SustainabilityTheme.Tint(SustainabilityTheme.Mint, 0.35f));
        MakeIcon(pillBg.transform, "LeafIcon", SustainabilityTheme.IconLeaf,
            SustainabilityTheme.Mint,
            V(0, 0.5f), V(0, 0.5f), V(16, -14), V(44, 14));
        var treesVal = MakeText(pillBg.transform, "TreesValue",
            "≈ 0 trees working for a year",
            22, true, SustainabilityTheme.MintSoft,
            V(0, 0), V(1, 1), V(52, 0), V(-12, 0),
            TextAlignmentOptions.MidlineLeft);

        // Two hero stats (travel + time) — each with an icon in the corner
        var travel = MakePanel(hero.transform, "TravelStat",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f),
            V(0, 1), V(0.5f, 1), V(40, -560), V(-12, -450));
        TintBorder(travel, SustainabilityTheme.Line);
        MakeIcon(travel.transform, "Icon", SustainabilityTheme.IconRoad,
            SustainabilityTheme.Teal,
            V(0, 1), V(0, 1), V(18, -38), V(48, -10));
        MakeText(travel.transform, "Label", "TRAVEL AVOIDED",
            16, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(54, -32), V(-12, -10),
            TextAlignmentOptions.MidlineLeft, letterSpacing: 12f);
        var travelVal = MakeText(travel.transform, "Value", "0",
            44, true, Color.white,
            V(0, 0), V(0.7f, 1), V(18, 16), V(0, -50),
            TextAlignmentOptions.BottomLeft);
        var travelUnit = MakeText(travel.transform, "Unit", "km",
            22, true, SustainabilityTheme.InkSoft,
            V(0.7f, 0), V(1, 1), V(0, 22), V(-14, -54),
            TextAlignmentOptions.BottomLeft);

        var time = MakePanel(hero.transform, "TimeStat",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f),
            V(0.5f, 1), V(1, 1), V(12, -560), V(-30, -450));
        TintBorder(time, SustainabilityTheme.Line);
        MakeIcon(time.transform, "Icon", SustainabilityTheme.IconClock,
            SustainabilityTheme.Teal,
            V(0, 1), V(0, 1), V(18, -38), V(48, -10));
        MakeText(time.transform, "Label", "TIME SAVED",
            16, true, SustainabilityTheme.Teal,
            V(0, 1), V(1, 1), V(54, -32), V(-12, -10),
            TextAlignmentOptions.MidlineLeft, letterSpacing: 12f);
        var timeVal = MakeText(time.transform, "Value", "0",
            44, true, Color.white,
            V(0, 0), V(0.7f, 1), V(18, 16), V(0, -50),
            TextAlignmentOptions.BottomLeft);
        var timeUnit = MakeText(time.transform, "Unit", "h",
            22, true, SustainabilityTheme.InkSoft,
            V(0.7f, 0), V(1, 1), V(0, 22), V(-14, -54),
            TextAlignmentOptions.BottomLeft);

        // Meta footer at the bottom of hero panel — icon + text per item
        var metaRow = MakeRT(hero.transform, "MetaFooter",
            V(0, 0), V(1, 0), V(40, 36), V(-30, 110));
        var mHlg = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        mHlg.spacing = 26;
        mHlg.childControlWidth = true; mHlg.childControlHeight = true;
        mHlg.childForceExpandWidth = true; mHlg.childForceExpandHeight = true;

        var metaP = BuildMetaItem(metaRow.transform, "Participants",
            SustainabilityTheme.IconUsers, "0 participants");
        var metaH = BuildMetaItem(metaRow.transform, "Host",
            SustainabilityTheme.IconPin,   "Host · —");
        var metaD = BuildMetaItem(metaRow.transform, "Demos",
            SustainabilityTheme.IconGloves,"0 hands-on demos");

        // ── RIGHT — top row 3 cards, then journeys + donut, then equivalences
        var cardsRow = MakeRT(rightCol.transform, "CardsRow",
            V(0, 1), V(1, 1), V(0, -240), V(0, 0));
        var (fuelV, fuelU, fuelS)     = BuildMetricCard(cardsRow.transform, "FuelCard",
                                            SustainabilityTheme.Clay, "0", "L",
                                            "Fuel saved", "—",
                                            V(0, 0), V(0.333f, 1), SustainabilityTheme.IconCar);
        var (sheetsV, sheetsU, sheetsS) = BuildMetricCard(cardsRow.transform, "PaperCard",
                                            SustainabilityTheme.Sky, "0", "sheets",
                                            "Paper avoided", "—",
                                            V(0.333f, 0), V(0.666f, 1), SustainabilityTheme.IconDrop);
        var (glovesV, glovesU, glovesS) = BuildMetricCard(cardsRow.transform, "GlovesCard",
                                            SustainabilityTheme.Mint, "0", "pairs",
                                            "Gloves saved", "—",
                                            V(0.666f, 0), V(1, 1), SustainabilityTheme.IconGloves);

        var middlePanel = MakePanel(rightCol.transform, "MiddlePanel",
            SustainabilityTheme.Card,
            V(0, 0), V(1, 1), V(0, 230), V(0, -260));
        TintBorder(middlePanel, SustainabilityTheme.Line);

        var journeysHalf = MakeRT(middlePanel.transform, "JourneysHalf",
            V(0, 0), V(0.58f, 1), V(28, 20), V(-12, -20));
        MakeText(journeysHalf.transform, "Header", "Round trips replaced",
            28, true, Color.white,
            V(0, 1), V(1, 1), V(0, -50), V(0, 0),
            TextAlignmentOptions.MidlineLeft);

        var jScroll = MakeRT(journeysHalf.transform, "Scroll",
            V(0, 0), V(1, 1), V(0, 4), V(-4, -64));
        var jContent = MakeRT(jScroll.transform, "Content",
            V(0, 1), V(1, 1), V(0, -300), V(0, 0));
        var jVlg = jContent.gameObject.AddComponent<VerticalLayoutGroup>();
        jVlg.spacing = 16; jVlg.childControlWidth = true; jVlg.childControlHeight = false;
        jVlg.childForceExpandWidth = true;
        jContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit
            = ContentSizeFitter.FitMode.PreferredSize;

        var donutHalf = MakeRT(middlePanel.transform, "DonutHalf",
            V(0.58f, 0), V(1, 1), V(24, 20), V(-28, -20));
        MakePanel(donutHalf.transform, "Divider",
            SustainabilityTheme.Line,
            V(0, 0.10f), V(0, 0.90f), V(-14, 0), V(-12, 0));
        MakeText(donutHalf.transform, "DonutHeader", "CO₂ by source",
            26, true, Color.white,
            V(0, 1), V(1, 1), V(0, -50), V(0, 0),
            TextAlignmentOptions.MidlineLeft);

        var (donut, donutTotal, donutUnit, legAir, legRoad, legVR) = BuildDonut(donutHalf.transform);

        var bottom = MakeRT(rightCol.transform, "Bottom",
            V(0, 0), V(1, 0), V(0, 0), V(0, 210));
        var equivPanel = MakePanel(bottom.transform, "Equivalences",
            SustainabilityTheme.Card, V(0, 0), V(0.78f, 1), V(0, 12), V(-12, -12));
        TintBorder(equivPanel, SustainabilityTheme.Line);

        MakeText(equivPanel.transform, "Tag", "THAT'S LIKE",
            18, true, SustainabilityTheme.Teal,
            V(0, 0), V(0.20f, 1), V(24, 0), V(0, 0),
            TextAlignmentOptions.Center, letterSpacing: 14f);

        var equivWater = BuildEquiv(equivPanel.transform, "EqWater", SustainabilityTheme.Sky,
            "0", "L of water", "paper production",
            V(0.20f, 0), V(0.45f, 1), SustainabilityTheme.IconDrop);
        var equivCar   = BuildEquiv(equivPanel.transform, "EqCar", SustainabilityTheme.Mint,
            "0", "km not driven", "CO₂ equivalent",
            V(0.45f, 0), V(0.72f, 1), SustainabilityTheme.IconCar);
        var equivPhone = BuildEquiv(equivPanel.transform, "EqPhone", SustainabilityTheme.Clay,
            "0", "phone charges", "CO₂ equivalent",
            V(0.72f, 0), V(1, 1), SustainabilityTheme.IconSpark);

        var resetBtn = MakeButton(bottom.transform, "NewCalcBtn", "←  New calculation",
            22, Color.white,
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f),
            V(0.78f, 0), V(1, 1), V(12, 12), V(0, -12));
        TintBorder(resetBtn.gameObject, SustainabilityTheme.Line);

        var journeyBarPrefab = BuildJourneyBarPrefab(canvasGO.transform);

        var dash = canvasGO.GetComponent<DashboardUIController>()
                ?? Undo.AddComponent<DashboardUIController>(canvasGO);
        Undo.RecordObject(dash, "Wire DashboardUIController");
        dash.co2HeadlineValue     = co2Val;
        dash.co2HeadlineUnit      = co2Unit;
        dash.treesPillValue       = treesVal;
        dash.travelAvoidedValue   = travelVal;
        dash.travelAvoidedUnit    = travelUnit;
        dash.timeSavedValue       = timeVal;
        dash.timeSavedUnit        = timeUnit;
        dash.metaParticipants     = metaP;
        dash.metaHost             = metaH;
        dash.metaDemos            = metaD;
        dash.fuelValue   = fuelV;   dash.fuelUnit   = fuelU;   dash.fuelSub   = fuelS;
        dash.sheetsValue = sheetsV; dash.sheetsUnit = sheetsU; dash.sheetsSub = sheetsS;
        dash.glovesValue = glovesV; dash.glovesUnit = glovesU; dash.glovesSub = glovesS;
        dash.journeyBarsContainer = jContent;
        dash.journeyBarPrefab     = journeyBarPrefab;
        dash.donut           = donut;
        dash.donutTotalLabel = donutTotal;
        dash.donutUnitLabel  = donutUnit;
        dash.donutAirLegend  = legAir;
        dash.donutRoadLegend = legRoad;
        dash.donutVRLegend   = legVR;
        dash.equivWaterValue = equivWater;
        dash.equivCarValue   = equivCar;
        dash.equivPhoneValue = equivPhone;
        dash.newCalculationButton = resetBtn;
        dash.stepDots             = null;
        EditorUtility.SetDirty(dash);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Shared sub-builders
    // ════════════════════════════════════════════════════════════════════════
    static void BuildBackground(Transform root)
    {
        // Solid base
        MakePanel(root, "BG_Solid", SustainabilityTheme.Bg0,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        // Image overlay if available
        if (_bg != null)
        {
            var img = MakeImageGO(root, "BG_Image", _bg,
                V(0, 0), V(1, 1), V(0, 0), V(0, 0),
                tint: new Color(1f, 1f, 1f, 0.55f));
            var i = img.GetComponent<Image>();
            i.preserveAspect = false;
            i.raycastTarget = false;
        }
    }

    static void BuildHeader(Transform header)
    {
        if (_logo != null)
            MakeImageGO(header, "BarLogo", _logo,
                V(0, 0), V(0, 1), V(44, 22), V(126, -22))
                .GetComponent<Image>().preserveAspect = true;

        MakeText(header, "BarTitle", "MedMeet",
            30, true, Color.white,
            V(0, 0), V(0.5f, 1), V(146, 32), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        MakeText(header, "BarSub", "AR & VR MEDICAL PLATFORM",
            16, true, SustainabilityTheme.Teal,
            V(0, 0), V(0.5f, 1), V(146, -36), V(0, -6),
            TextAlignmentOptions.MidlineLeft, letterSpacing: 14f);

        // Centre tag — leaf icon + label
        var pill = MakePanel(header, "TagPill",
            SustainabilityTheme.Tint(SustainabilityTheme.Mint, 0.12f),
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-280, -34), V(280, 34));
        TintBorder(pill, SustainabilityTheme.Tint(SustainabilityTheme.Mint, 0.35f));
        MakeIcon(pill.transform, "LeafIcon", SustainabilityTheme.IconLeaf,
            SustainabilityTheme.Mint,
            V(0, 0.5f), V(0, 0.5f), V(22, -16), V(54, 16));
        MakeText(pill.transform, "PillLabel", "Sustainability Showcase",
            26, true, Color.white,
            V(0, 0), V(1, 1), V(60, 0), V(-10, 0),
            TextAlignmentOptions.MidlineLeft);
    }

    static (TextMeshProUGUI value, TextMeshProUGUI label) BuildTally(
        Transform parent, string name, string label, string value, string iconPath = null)
    {
        var p = MakePanel(parent, name,
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f),
            V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        TintBorder(p, SustainabilityTheme.LineSoft);
        var le = p.AddComponent<LayoutElement>();
        le.flexibleWidth = 1; le.preferredHeight = 100;

        // Top row: icon (16×16 area) + label
        float labelLeft = 16;
        if (iconPath != null)
        {
            MakeIcon(p.transform, "Icon", iconPath, SustainabilityTheme.Teal,
                V(0, 1), V(0, 1), V(16, -34), V(38, -12));
            labelLeft = 44;
        }
        var lblTxt = MakeText(p.transform, "Label", label,
            17, true, SustainabilityTheme.InkFaint,
            V(0, 1), V(1, 1), V(labelLeft, -34), V(-12, -10),
            TextAlignmentOptions.MidlineLeft, letterSpacing: 10f);
        var valTxt = MakeText(p.transform, "Value", value,
            38, true, Color.white,
            V(0, 0), V(1, 1), V(16, 8), V(-12, -38),
            TextAlignmentOptions.MidlineLeft);
        return (valTxt, lblTxt);
    }

    static (TextMeshProUGUI value, TextMeshProUGUI unit, TextMeshProUGUI sub)
        BuildMetricCard(Transform parent, string name, Color accent,
                        string value, string unit, string label, string sub,
                        Vector2 anMin, Vector2 anMax, string iconPath = null)
    {
        var card = MakePanel(parent, name, SustainabilityTheme.Card,
            anMin, anMax, V(10, 0), V(-10, 0));
        TintBorder(card, SustainabilityTheme.Line);

        // Coloured icon tile in the top-left corner
        if (iconPath != null)
        {
            var tile = MakePanel(card.transform, "IconTile",
                SustainabilityTheme.Tint(accent, 0.18f),
                V(0, 1), V(0, 1), V(28, -88), V(88, -28));
            TintBorder(tile, SustainabilityTheme.Tint(accent, 0.35f));
            MakeIcon(tile.transform, "Icon", iconPath, accent,
                V(0, 0), V(1, 1), V(14, 14), V(-14, -14));
        }

        // Thin top accent strip
        MakePanel(card.transform, "TopBar", accent,
            V(0, 1), V(1, 1), V(0, -6), V(0, 0));

        var valTxt = MakeText(card.transform, "Value", value,
            62, true, Color.white,
            V(0, 1), V(0.7f, 1), V(28, -176), V(0, -100),
            TextAlignmentOptions.BottomLeft);
        var unitTxt = MakeText(card.transform, "Unit", unit,
            24, true, SustainabilityTheme.InkFaint,
            V(0.5f, 1), V(1, 1), V(0, -166), V(-22, -122),
            TextAlignmentOptions.BottomLeft);

        var lblTxt = MakeText(card.transform, "Label", label,
            22, true, SustainabilityTheme.Ink,
            V(0, 0), V(1, 1), V(28, 56), V(-22, -188),
            TextAlignmentOptions.TopLeft);

        var subTxt = MakeText(card.transform, "Sub", sub,
            17, false, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 0), V(28, 18), V(-22, 52),
            TextAlignmentOptions.TopLeft);
        return (valTxt, unitTxt, subTxt);
    }

    static (DonutChartUI donut, TextMeshProUGUI total, TextMeshProUGUI unit,
            TextMeshProUGUI legAir, TextMeshProUGUI legRoad, TextMeshProUGUI legVR)
        BuildDonut(Transform parent)
    {
        var donutHolder = MakeRT(parent, "Donut",
            V(0, 0.18f), V(0.55f, 0.95f), V(8, 0), V(0, 0));
        var bg = MakeRadialImage(donutHolder.transform, "BgRing",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f), 1f);
        var vr   = MakeRadialImage(donutHolder.transform, "VR",   SustainabilityTheme.Mint, 0f);
        var road = MakeRadialImage(donutHolder.transform, "Road", SustainabilityTheme.Teal, 0f);
        var air  = MakeRadialImage(donutHolder.transform, "Air",  SustainabilityTheme.Sky,  0f);

        var hole = MakeRT(donutHolder.transform, "Hole",
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-72, -72), V(72, 72));
        var holeImg = hole.gameObject.AddComponent<Image>();
        holeImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        holeImg.color = SustainabilityTheme.Card;
        holeImg.preserveAspect = true;
        holeImg.raycastTarget = false;

        var totalTxt = MakeText(donutHolder.transform, "Total", "0",
            42, true, Color.white,
            V(0, 0), V(1, 1), V(0, 18), V(0, -18),
            TextAlignmentOptions.Center);
        var unitTxt = MakeText(donutHolder.transform, "Unit", "kg CO₂",
            16, true, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 0.5f), V(0, 20), V(0, -22),
            TextAlignmentOptions.Center);

        var legendCol = MakeRT(parent, "Legend",
            V(0.55f, 0.18f), V(1, 0.95f), V(12, 0), V(-8, 0));
        var vlg = legendCol.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16; vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleLeft;

        var legAir  = BuildLegendItem(legendCol.transform, "AirLegend",  SustainabilityTheme.Sky,  "Air travel\n0 kg · 0%");
        var legRoad = BuildLegendItem(legendCol.transform, "RoadLegend", SustainabilityTheme.Teal, "Road travel\n0 kg · 0%");
        var legVR   = BuildLegendItem(legendCol.transform, "VRLegend",   SustainabilityTheme.Mint, "VR demos\n0 kg · 0%");

        var donut = donutHolder.gameObject.AddComponent<DonutChartUI>();
        donut.backgroundRing = bg;
        donut.airImage  = air;
        donut.roadImage = road;
        donut.vrImage   = vr;
        return (donut, totalTxt, unitTxt, legAir, legRoad, legVR);
    }

    static TextMeshProUGUI BuildLegendItem(Transform parent, string name, Color dotColor, string txt)
    {
        var row = MakeRT(parent, name, V(0, 0), V(1, 0), V(0, 0), V(0, 0));
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 56;

        MakePanel(row.transform, "Dot", dotColor,
            V(0, 0.5f), V(0, 0.5f), V(0, -9), V(18, 9));
        var label = MakeText(row.transform, "Label", txt,
            20, true, Color.white,
            V(0, 0), V(1, 1), V(28, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft, wrap: false);
        label.richText = true;
        return label;
    }

    /// <summary>Builds an "icon + label" row item for the dashboard hero meta strip.</summary>
    static TextMeshProUGUI BuildMetaItem(Transform parent, string name, string iconPath, string text)
    {
        var holder = MakeRT(parent, name, V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        MakeIcon(holder.transform, "Icon", iconPath, SustainabilityTheme.Teal,
            V(0, 0.5f), V(0, 0.5f), V(0, -13), V(26, 13));
        var lbl = MakeText(holder.transform, "Label", text,
            19, true, SustainabilityTheme.InkSoft,
            V(0, 0), V(1, 1), V(36, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        return lbl;
    }

    static TextMeshProUGUI BuildEquiv(Transform parent, string name, Color iconColor,
                                       string value, string unit, string sub,
                                       Vector2 anMin, Vector2 anMax, string iconPath = null)
    {
        var holder = MakeRT(parent, name, anMin, anMax, V(14, 14), V(-14, -14));
        // Icon: real sprite if available, otherwise small colored square fallback
        if (iconPath != null)
        {
            MakeIcon(holder.transform, "Icon", iconPath, iconColor,
                V(0, 0.5f), V(0, 0.5f), V(0, -18), V(36, 18));
        }
        else
        {
            MakePanel(holder.transform, "Dot", iconColor,
                V(0, 0.5f), V(0, 0.5f), V(0, -8), V(16, 8));
        }
        var val = MakeText(holder.transform, "Value", value,
            34, true, Color.white,
            V(0, 0.5f), V(1, 1), V(46, 0), V(0, -6),
            TextAlignmentOptions.BottomLeft);
        MakeText(holder.transform, "Unit", unit,
            18, true, SustainabilityTheme.InkSoft,
            V(0, 0.40f), V(1, 0.70f), V(46, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        MakeText(holder.transform, "Sub", sub,
            15, false, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 0.40f), V(46, 8), V(0, 0),
            TextAlignmentOptions.TopLeft);
        return val;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Runtime prefabs
    // ════════════════════════════════════════════════════════════════════════
    static GameObject BuildParticipantRowPrefab(Transform parent)
    {
        var go = new GameObject("ParticipantRow_Prefab");
        Undo.RegisterCreatedObjectUndo(go, "Create ParticipantRow_Prefab");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 96);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 96;

        var bgImg = go.AddComponent<Image>();
        bgImg.color = SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.30f);

        // Index badge (huge, 76 px)
        var badge = MakePanel(go.transform, "IndexBadge",
            SustainabilityTheme.Teal,
            V(0, 0.5f), V(0, 0.5f), V(12, -38), V(88, 38));
        var badgeLbl = MakeText(badge.transform, "IndexLabel", "1",
            32, true, Color.black,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.Center);

        // Name input (editable when mine)
        var nameBg = MakePanel(go.transform, "NameInputBg",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f),
            V(0, 0), V(0, 1), V(100, 10), V(440, -10));
        TintBorder(nameBg, SustainabilityTheme.Line);
        var input = nameBg.AddComponent<TMP_InputField>();
        var inputViewport = MakeRT(nameBg.transform, "Viewport",
            V(0, 0), V(1, 1), V(20, 6), V(-18, -6));
        inputViewport.gameObject.AddComponent<RectMask2D>();
        var inputText = MakeText(inputViewport.transform, "Text", "",
            26, true, SustainabilityTheme.Ink,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        var placeholder = MakeText(inputViewport.transform, "Placeholder", "Your name",
            26, false, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        input.textViewport = inputViewport;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.fontAsset = _font;
        // VR-friendly: keep mobile/quest keyboard open while typing
        input.shouldHideMobileInput = false;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 30;
        input.contentType = TMP_InputField.ContentType.Standard;

        // Name read-only label (shown when not mine — replaces input)
        var nameRO = MakeText(nameBg.transform, "NameReadonly", "—",
            26, true, SustainabilityTheme.Ink,
            V(0, 0), V(1, 1), V(20, 0), V(-18, 0),
            TextAlignmentOptions.MidlineLeft);
        nameRO.gameObject.SetActive(false);

        // City button
        var cityBtnGO = new GameObject("CityButton");
        Undo.RegisterCreatedObjectUndo(cityBtnGO, "CityButton");
        cityBtnGO.transform.SetParent(go.transform, false);
        var cityRT = cityBtnGO.AddComponent<RectTransform>();
        cityRT.anchorMin = V(0, 0); cityRT.anchorMax = V(0, 1);
        cityRT.offsetMin = V(456, 10); cityRT.offsetMax = V(916, -10);
        var cityImg = cityBtnGO.AddComponent<Image>();
        cityImg.color = SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f);
        TintBorder(cityBtnGO, SustainabilityTheme.Line);
        var cityBtn = cityBtnGO.AddComponent<Button>();
        var cityLbl = MakeText(cityBtnGO.transform, "Label", "Pick city",
            26, true, SustainabilityTheme.InkFaint,
            V(0, 0), V(0.72f, 1), V(24, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        var cityCty = MakeText(cityBtnGO.transform, "Country", "",
            20, true, SustainabilityTheme.Teal,
            V(0.72f, 0), V(1, 1), V(0, 0), V(-24, 0),
            TextAlignmentOptions.MidlineRight);

        // Host pill
        var hostPill = MakePanel(go.transform, "HostPill",
            SustainabilityTheme.TealWash,
            V(0, 0.5f), V(0, 0.5f), V(936, -26), V(1058, 26));
        TintBorder(hostPill, SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.45f));
        MakeText(hostPill.transform, "HostLbl", "HOST",
            20, true, SustainabilityTheme.TealSoft,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.Center, letterSpacing: 12f);

        // "YOU" pill (overlapping host pill area; only one visible)
        var youPill = MakePanel(go.transform, "YouPill",
            SustainabilityTheme.MintWash,
            V(0, 0.5f), V(0, 0.5f), V(1072, -26), V(1156, 26));
        TintBorder(youPill, SustainabilityTheme.Tint(SustainabilityTheme.Mint, 0.45f));
        MakeText(youPill.transform, "YouLbl", "YOU",
            18, true, SustainabilityTheme.MintSoft,
            V(0, 0), V(1, 1), V(0, 0), V(0, 0),
            TextAlignmentOptions.Center, letterSpacing: 10f);

        // Remove button
        var removeBtnGO = new GameObject("RemoveButton");
        Undo.RegisterCreatedObjectUndo(removeBtnGO, "RemoveButton");
        removeBtnGO.transform.SetParent(go.transform, false);
        var removeRT = removeBtnGO.AddComponent<RectTransform>();
        removeRT.anchorMin = V(1, 0.5f); removeRT.anchorMax = V(1, 0.5f);
        removeRT.offsetMin = V(-90, -36); removeRT.offsetMax = V(-16, 36);
        var removeImg = removeBtnGO.AddComponent<Image>();
        removeImg.color = SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f);
        TintBorder(removeBtnGO, SustainabilityTheme.Line);
        var removeBtn = removeBtnGO.AddComponent<Button>();
        MakeIcon(removeBtnGO.transform, "Icon", SustainabilityTheme.IconTrash,
            SustainabilityTheme.InkFaint,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-16, -16), V(16, 16));
        removeBtnGO.SetActive(false);

        var rowUI = go.AddComponent<ParticipantRowUI>();
        rowUI.indexLabel       = badgeLbl;
        rowUI.indexBackground  = badge.GetComponent<Image>();
        rowUI.nameInput        = input;
        rowUI.nameReadonly     = nameRO;
        rowUI.cityButton       = cityBtn;
        rowUI.cityLabel        = cityLbl;
        rowUI.cityCountryLabel = cityCty;
        rowUI.hostPill         = hostPill;
        rowUI.youPill          = youPill;
        rowUI.removeButton     = removeBtn;

        go.SetActive(false);
        return go;
    }

    static GameObject BuildJourneyRowPrefab(Transform parent)
    {
        var go = new GameObject("JourneyRow_Prefab");
        Undo.RegisterCreatedObjectUndo(go, "JourneyRow_Prefab");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 76);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 76;
        var bg = go.AddComponent<Image>();
        bg.color = SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.35f);
        TintBorder(go, SustainabilityTheme.LineSoft);

        var route = MakeText(go.transform, "Route", "City → Host",
            24, true, Color.white,
            V(0, 0.5f), V(1, 1), V(24, 0), V(-130, -8),
            TextAlignmentOptions.BottomLeft);
        var mode = MakeText(go.transform, "Mode", "Flight avoided",
            16, false, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 0.5f), V(24, 6), V(-130, 0),
            TextAlignmentOptions.TopLeft);
        var km = MakeText(go.transform, "Km", "0",
            26, true, SustainabilityTheme.TealSoft,
            V(0.65f, 0), V(1, 1), V(0, 0), V(-58, 0),
            TextAlignmentOptions.MidlineRight);
        var kmUnit = MakeText(go.transform, "KmUnit", "km",
            18, true, SustainabilityTheme.InkFaint,
            V(0.65f, 0), V(1, 1), V(0, 0), V(-22, 0),
            TextAlignmentOptions.MidlineRight);

        var row = go.AddComponent<JourneyRowUI>();
        row.routeLabel = route; row.modeLabel = mode; row.kmLabel = km; row.kmUnitLabel = kmUnit;
        go.SetActive(false);
        return go;
    }

    static GameObject BuildJourneyBarPrefab(Transform parent)
    {
        var go = new GameObject("JourneyBar_Prefab");
        Undo.RegisterCreatedObjectUndo(go, "JourneyBar_Prefab");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 96);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 96;

        var route = MakeText(go.transform, "Route", "City ⇆ Host",
            24, true, Color.white,
            V(0, 1), V(0.7f, 1), V(0, -36), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        var km = MakeText(go.transform, "Km", "0 km",
            24, true, SustainabilityTheme.TealSoft,
            V(0.65f, 1), V(1, 1), V(0, -36), V(0, 0),
            TextAlignmentOptions.MidlineRight);

        var track = MakePanel(go.transform, "Track",
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.40f),
            V(0, 0.30f), V(1, 0.55f), V(0, 0), V(0, 0));
        var fill = MakePanel(track.transform, "Fill",
            SustainabilityTheme.Teal, V(0, 0), V(1, 1), V(2, 2), V(-2, -2));
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.pivot = new Vector2(0f, 0.5f);

        var sub = MakeText(go.transform, "Sub", "Flight · 0 kg CO₂ · 0 L fuel",
            16, false, SustainabilityTheme.InkFaint,
            V(0, 0), V(1, 0.30f), V(0, 0), V(0, 0),
            TextAlignmentOptions.TopLeft);

        var bar = go.AddComponent<JourneyBarUI>();
        bar.routeLabel = route; bar.kmLabel = km; bar.barFill = fillRT;
        bar.barFillImage = fill.GetComponent<Image>(); bar.subLabel = sub;
        go.SetActive(false);
        return go;
    }

    static CitySelectPopup BuildCitySelectPopup(Transform parent)
    {
        var root = new GameObject("CitySelectPopup_Root");
        Undo.RegisterCreatedObjectUndo(root, "CitySelectPopup");
        root.transform.SetParent(parent, false);
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.anchorMin = V(0, 0); rootRT.anchorMax = V(1, 1);
        rootRT.offsetMin = V(0, 0); rootRT.offsetMax = V(0, 0);
        var dimImg = root.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.82f);

        var card = MakePanel(root.transform, "Card",
            SustainabilityTheme.CardSolid,
            V(0.5f, 0.5f), V(0.5f, 0.5f), V(-340, -440), V(340, 440));
        TintBorder(card, SustainabilityTheme.Line);

        MakeText(card.transform, "Title", "Choose city",
            34, true, Color.white,
            V(0, 1), V(1, 1), V(32, -80), V(-80, -16),
            TextAlignmentOptions.MidlineLeft);

        var closeBtn = MakeButton(card.transform, "Close", "✕",
            28, Color.white,
            SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.50f),
            V(1, 1), V(1, 1), V(-70, -70), V(-16, -16));

        var scroll = MakeRT(card.transform, "Scroll",
            V(0, 0), V(1, 1), V(24, 24), V(-24, -100));
        var content = MakeRT(scroll.transform, "Content",
            V(0, 1), V(1, 1), V(0, -800), V(0, 0));
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8; vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit
            = ContentSizeFitter.FitMode.PreferredSize;

        var itemPrefab = new GameObject("CityItem_Prefab");
        Undo.RegisterCreatedObjectUndo(itemPrefab, "CityItem_Prefab");
        itemPrefab.transform.SetParent(root.transform, false);
        var itemRT = itemPrefab.AddComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0, 64);
        var itemLE = itemPrefab.AddComponent<LayoutElement>();
        itemLE.preferredHeight = 64;
        var itemImg = itemPrefab.AddComponent<Image>();
        itemImg.color = SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f);
        itemPrefab.AddComponent<Button>();
        MakeText(itemPrefab.transform, "Name", "City",
            24, true, Color.white,
            V(0, 0), V(0.78f, 1), V(28, 0), V(0, 0),
            TextAlignmentOptions.MidlineLeft);
        MakeText(itemPrefab.transform, "Country", "XX",
            20, true, SustainabilityTheme.Teal,
            V(0.78f, 0), V(1, 1), V(0, 0), V(-28, 0),
            TextAlignmentOptions.MidlineRight);
        itemPrefab.SetActive(false);

        var popup = root.AddComponent<CitySelectPopup>();
        popup.root             = root;
        popup.contentContainer = content;
        popup.cityButtonPrefab = itemPrefab;
        popup.closeButton      = closeBtn;
        root.SetActive(false);
        return popup;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Utilities
    // ════════════════════════════════════════════════════════════════════════
    static GameObject FindOrCreateCanvasShell(
        GameObject template, string name, System.Collections.Generic.List<string> createdList)
    {
        var existing = GameObject.Find(name);
        if (existing != null && existing != template)
        {
            createdList.Add($"{name} (reused)");
            return existing;
        }
        var go = CloneCanvasShell(template, name);
        createdList.Add($"{name} (created)");
        return go;
    }

    static GameObject CloneCanvasShell(GameObject template, string newName)
    {
        var go = new GameObject(newName);
        Undo.RegisterCreatedObjectUndo(go, $"Create {newName}");
        go.transform.SetParent(template.transform.parent, false);
        go.transform.position   = template.transform.position;
        go.transform.rotation   = template.transform.rotation;
        go.transform.localScale = template.transform.localScale;

        var canvas = go.AddComponent<Canvas>();
        var templateCanv = template.GetComponent<Canvas>();
        if (templateCanv != null)
        {
            canvas.renderMode   = templateCanv.renderMode;
            canvas.sortingOrder = templateCanv.sortingOrder;
            canvas.worldCamera  = templateCanv.worldCamera;
        }
        else canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<TrackedDeviceGraphicRaycaster>();
        var rt = go.GetComponent<RectTransform>();
        var trt = template.GetComponent<RectTransform>();
        if (rt != null && trt != null) rt.sizeDelta = trt.sizeDelta;
        return go;
    }

    static void WipeChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(go.transform.GetChild(i).gameObject);
    }

    static void NormaliseCanvasSize(GameObject canvasGO)
    {
        var canvas = canvasGO.GetComponent<Canvas>() ?? canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<CanvasScaler>()?.GetType();
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(SustainabilityTheme.CanvasW, SustainabilityTheme.CanvasH);
    }

    static void FixRaycaster(GameObject canvasGO)
    {
        if (canvasGO == null) return;
        var std = canvasGO.GetComponent<GraphicRaycaster>();
        if (std != null) Undo.DestroyObjectImmediate(std);
        if (canvasGO.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvasGO);
    }

    static void FixSpriteImports(params string[] paths)
    {
        bool changed = false;
        foreach (var p in paths)
        {
            if (!System.IO.File.Exists(p)) continue;
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
            if (dirty) { AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); changed = true; }
        }
        if (changed) AssetDatabase.Refresh();
    }

    static Sprite LoadSprite(string path)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f), 100f);
    }

    static void PositionCanvasesAtAnchor(GameObject setupCanvas, GameObject loadingCanvas, GameObject dashboardCanvas)
    {
        Transform anchor = null;
        foreach (string n in new[] { "DashboardAnchor", "vitre", "Vitre", "Window", "DashWindow", "Glass" })
        {
            var go = GameObject.Find(n);
            if (go != null) { anchor = go.transform; break; }
        }
        if (anchor == null)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                string l = go.name.ToLower();
                if (l == "vitre" || l == "dashboardanchor" || l == "window" || l == "glass")
                { anchor = go.transform; break; }
            }
        }

        Vector3 pos = anchor != null ? anchor.position + anchor.forward * 0.06f : setupCanvas.transform.position;
        Quaternion rot = anchor != null ? anchor.rotation : setupCanvas.transform.rotation;

        foreach (var c in new[] { setupCanvas, loadingCanvas, dashboardCanvas })
        {
            Undo.RecordObject(c.transform, $"Position {c.name}");
            c.transform.position   = pos;
            c.transform.rotation   = rot;
            c.transform.localScale = Vector3.one * CanvasScale;
        }
    }

    // ── Atomic helpers ─────────────────────────────────────────────────────
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static RectTransform MakeRT(Transform parent, string name,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anMin; rt.anchorMax = anMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        return rt;
    }

    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax)
    {
        var rt = MakeRT(parent, name, anMin, anMax, offMin, offMax);
        rt.gameObject.AddComponent<Image>().color = color;
        return rt.gameObject;
    }

    static void TintBorder(GameObject go, Color outline, float thickness = 1f)
    {
        var ol = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
        ol.effectColor = outline;
        ol.effectDistance = new Vector2(thickness, -thickness);
        ol.useGraphicAlpha = false;
    }

    static GameObject MakeImageGO(Transform parent, string name, Sprite sprite,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax, Color? tint = null)
    {
        var rt = MakeRT(parent, name, anMin, anMax, offMin, offMax);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        if (tint.HasValue) img.color = tint.Value;
        return rt.gameObject;
    }

    static Image MakeRadialImage(Transform parent, string name, Color color, float fillAmount)
    {
        var rt = MakeRT(parent, name, V(0, 0), V(1, 1), V(0, 0), V(0, 0));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int) Image.Origin360.Top;
        img.fillClockwise = true;
        img.fillAmount = fillAmount;
        img.color = color;
        img.preserveAspect = true;
        return img;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float fontSize, bool bold, Color color,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax,
        TextAlignmentOptions align, float letterSpacing = 0f, bool wrap = false)
    {
        var rt = MakeRT(parent, name, anMin, anMax, offMin, offMax);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.characterSpacing = letterSpacing;
        tmp.enableWordWrapping = wrap;
        tmp.richText = true;
        if (_font != null) tmp.font = _font;
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label,
        float fontSize, Color textColor, Color bgColor,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax)
    {
        var rt = MakeRT(parent, name, anMin, anMax, offMin, offMax);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = bgColor;
        var btn = rt.gameObject.AddComponent<Button>();
        // VR-friendly button feedback — stronger highlight + smooth transition
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.30f);
        cb.pressedColor     = Color.Lerp(bgColor, Color.black, 0.25f);
        cb.selectedColor    = Color.Lerp(bgColor, Color.white, 0.20f);
        cb.disabledColor    = new Color(bgColor.r, bgColor.g, bgColor.b, bgColor.a * 0.35f);
        cb.colorMultiplier  = 1f;
        cb.fadeDuration     = 0.12f;
        btn.colors = cb;
        MakeText(rt, "Label", label, fontSize, true, textColor,
            V(0, 0), V(1, 1), V(12, 6), V(-12, -6),
            TextAlignmentOptions.Center);
        return btn;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Icon helpers — load a PNG from icons/ folder and tint it at runtime.
    //  Icons must be whitened first via MedMeet Tools → Whiten Showcase Icons.
    // ════════════════════════════════════════════════════════════════════════
    static readonly System.Collections.Generic.Dictionary<string, Sprite> _iconCache
        = new System.Collections.Generic.Dictionary<string, Sprite>();

    static Sprite LoadIcon(string path)
    {
        if (_iconCache.TryGetValue(path, out var cached)) return cached;
        if (!System.IO.File.Exists(path)) { _iconCache[path] = null; return null; }
        FixSpriteImports(path);
        var sp = LoadSprite(path);
        _iconCache[path] = sp;
        return sp;
    }

    /// <summary>
    /// Create an Image that displays an icon sprite tinted by `color`. Falls
    /// back to a small coloured square when the file is missing.
    /// </summary>
    static GameObject MakeIcon(Transform parent, string name, string iconPath, Color color,
        Vector2 anMin, Vector2 anMax, Vector2 offMin, Vector2 offMax)
    {
        var rt = MakeRT(parent, name, anMin, anMax, offMin, offMax);
        var img = rt.gameObject.AddComponent<Image>();
        var sprite = LoadIcon(iconPath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.preserveAspect = true;
        }
        img.color = color;
        img.raycastTarget = false;
        return rt.gameObject;
    }
}
