using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MedMeet Tools → Add TTS Speak Button
///
/// Adds one small "🔊 Read aloud" button to the AI canvas and wires it to
/// ClinicalAdvisorUI.OnSpeakLastResponsePressed(). Also drops the
/// AndroidTTSPlayer component into the scene if it isn't there yet.
///
/// This tool ONLY adds new GameObjects — it never modifies existing buttons,
/// the chat, the input field or any visual properties the user has tuned.
/// You can freely move / resize / restyle the new button in the Inspector
/// after it's been added.
///
/// Safe to run multiple times — if the button or the TTS player already
/// exist they're reused instead of duplicated.
/// </summary>
public static class AddTTSSpeakButtonTool
{
    private const string SpeakButtonName = "TTSSpeakButton";
    private const string StopButtonName = "TTSStopButton";
    private const string TTSPlayerName = "AndroidTTSPlayer";

    [MenuItem("MedMeet Tools/Add TTS Speak Button")]
    public static void Run()
    {
        var ui = Object.FindFirstObjectByType<ClinicalAdvisorUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("TTS Speak Button",
                "ClinicalAdvisorUI not found in the open scene.", "OK");
            return;
        }

        var canvas = FindCanvas(ui);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("TTS Speak Button",
                "Couldn't find a Canvas anywhere related to ClinicalAdvisorUI.\n\n" +
                "Open the Console — I've printed the parent chain so we can see why.",
                "OK");
            Debug.LogError(
                $"[TTSSpeakButton] No Canvas found.\n" +
                $"  ClinicalAdvisorUI GameObject: {ui.gameObject.name}\n" +
                $"  Parent chain: {DescribeParents(ui.transform)}", ui.gameObject);
            return;
        }

        Undo.SetCurrentGroupName("Add TTS Speak Button");
        int group = Undo.GetCurrentGroup();

        var log = new System.Text.StringBuilder();
        log.AppendLine("=== Add TTS Speak Button ===");

        EnsureTTSPlayer(log);
        var speakBtn = EnsureButton(canvas.transform, SpeakButtonName, "🔊  Read aloud",
            new Color(0.117f, 0.122f, 0.137f, 1f),                  // charcoal
            new Vector2(-280f, 38f), new Vector2(180f, 70f), log);
        var stopBtn = EnsureButton(canvas.transform, StopButtonName, "■  Stop",
            new Color(0.078f, 0.082f, 0.094f, 1f),                  // graphite
            new Vector2(-90f, 38f), new Vector2(110f, 70f), log);

        // Wire button → ClinicalAdvisorUI methods
        WireButton(speakBtn, ui, nameof(ClinicalAdvisorUI.OnSpeakLastResponsePressed), log);
        WireButton(stopBtn, ui, nameof(ClinicalAdvisorUI.OnStopSpeakingPressed), log);

        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(ui);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log(log);
        EditorUtility.DisplayDialog("TTS Speak Button",
            log + "\n\nThe new buttons sit at the bottom-right corner of the canvas. " +
            "Drag them in the Inspector to wherever fits your design. Press Ctrl+S to save.",
            "OK");
    }

    // ── Canvas locator (robust — searches parents, children, scene roots,
    //  and finally any Canvas in the scene that has a ClinicalAdvisorUI
    //  as a descendant). Same logic that fixed AICanvasPolishTool. ─────────
    static Canvas FindCanvas(ClinicalAdvisorUI ui)
    {
        // 1) Parents (most common)
        var c = ui.GetComponentInParent<Canvas>(includeInactive: true);
        if (c != null) return c;

        // 2) Children (some prefabs nest the Canvas)
        c = ui.GetComponentInChildren<Canvas>(includeInactive: true);
        if (c != null) return c;

        // 3) Topmost root of the same hierarchy
        var root = ui.transform.root;
        c = root.GetComponentInChildren<Canvas>(includeInactive: true);
        if (c != null) return c;

        // 4) Any Canvas in the scene that contains a ClinicalAdvisorUI
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                            FindObjectsSortMode.None))
        {
            if (cv.GetComponentInChildren<ClinicalAdvisorUI>(includeInactive: true) != null)
                return cv;
        }

        // 5) Last-ditch: GameObject named like the prefab
        foreach (string n in new[] { "AI-Canvas", "AICanvas", "AI_Canvas" })
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                var cc = go.GetComponent<Canvas>() ?? go.GetComponentInChildren<Canvas>(includeInactive: true);
                if (cc != null) return cc;
            }
        }
        return null;
    }

    static string DescribeParents(Transform t)
    {
        var parts = new System.Collections.Generic.List<string>();
        var cur = t;
        int hops = 0;
        while (cur != null && hops++ < 12)
        {
            string tags = "";
            if (cur.GetComponent<Canvas>() != null) tags += "[Canvas]";
            if (cur.GetComponent<RectTransform>() != null) tags += "[Rect]";
            parts.Add($"{cur.name}{tags}");
            cur = cur.parent;
        }
        return string.Join("  →  ", parts);
    }

    // ── Sub-builders ───────────────────────────────────────────────────────
    static void EnsureTTSPlayer(System.Text.StringBuilder log)
    {
        if (Object.FindFirstObjectByType<AndroidTTSPlayer>(FindObjectsInactive.Include) != null)
        { log.AppendLine("✓ AndroidTTSPlayer already in scene"); return; }

        var go = new GameObject(TTSPlayerName);
        Undo.RegisterCreatedObjectUndo(go, "Create AndroidTTSPlayer");
        Undo.AddComponent<AndroidTTSPlayer>(go);
        log.AppendLine($"✓ Created {TTSPlayerName} GameObject (Android TTS engine)");
    }

    static Button EnsureButton(Transform canvasRoot, string name, string label, Color bg,
                                Vector2 anchoredPos, Vector2 size,
                                System.Text.StringBuilder log)
    {
        // Reuse existing one if present
        var existing = canvasRoot.Find(name);
        Button btn;
        if (existing != null)
        {
            btn = existing.GetComponent<Button>();
            if (btn == null) btn = existing.gameObject.AddComponent<Button>();
            log.AppendLine($"✓ Reused existing {name}");
            return btn;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(canvasRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = bg;
        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (rounded != null) { img.sprite = rounded; img.type = Image.Type.Sliced; }

        btn = go.GetComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.18f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.30f);
        cb.fadeDuration = 0.15f;
        btn.colors = cb;

        // Subtle border so it reads against any background
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.10f);
        outline.effectDistance = new Vector2(1, -1);
        outline.useGraphicAlpha = false;

        // Label
        var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(lblGO, "Button label");
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8, 4);
        lrt.offsetMax = new Vector2(-8, -4);
        var tmp = lblGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = new Color(0.92f, 0.94f, 0.96f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        // Try to use the same font the rest of the canvas uses
        var anyTMP = canvasRoot.GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (anyTMP != null && anyTMP.font != null) tmp.font = anyTMP.font;

        log.AppendLine($"✓ Created {name}");
        return btn;
    }

    static void WireButton(Button btn, ClinicalAdvisorUI target, string methodName,
                           System.Text.StringBuilder log)
    {
        if (btn == null || target == null) return;

        // Clear any leftover listeners (idempotent across runs).
        // RemovePersistentListener throws if the list is empty — so we must
        // check the count BEFORE asking it to remove index 0.
        Undo.RecordObject(btn, "Wire TTS button");
        while (btn.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);

        var method = typeof(ClinicalAdvisorUI).GetMethod(methodName);
        if (method == null)
        {
            log.AppendLine($"✗ Method ClinicalAdvisorUI.{methodName} not found");
            return;
        }
        var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), target, method);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
        EditorUtility.SetDirty(btn);
        log.AppendLine($"✓ Wired {btn.name} → ClinicalAdvisorUI.{methodName}");
    }
}