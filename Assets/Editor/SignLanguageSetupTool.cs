using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// One-click setup for AR Sign Language Recognition.
/// Run: MedMeet Tools → Setup Sign Language AR
///
/// Creates all required GameObjects, UI panels, and wires all references
/// so nothing needs to be configured manually in the Inspector.
/// </summary>
public static class SignLanguageSetupTool
{
    [MenuItem("MedMeet Tools/Setup Sign Language AR")]
    public static void SetupSignLanguage()
    {
        // ── 1. Find required scene objects ───────────────────────────────────
        VRARSwitcher switcher = Object.FindFirstObjectByType<VRARSwitcher>();
        if (switcher == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "VRARSwitcher not found in scene.\nOpen the VR meeting scene first.", "OK");
            return;
        }

        // Find the main Canvas — used for all UI in AR mode
        Canvas mainCanvas = FindMainCanvas(switcher);
        if (mainCanvas == null)
        {
            EditorUtility.DisplayDialog("Setup Failed",
                "Could not find a Canvas in the scene.\n" +
                "Assign mainCanvas in VRARSwitcher first.", "OK");
            return;
        }

        // ── 2. Remove old ARSignLanguageSystem if it exists ──────────────────
        ARSignLanguageSystem existing = Object.FindFirstObjectByType<ARSignLanguageSystem>();
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("Already Exists",
                "ARSignLanguageSystem already exists. Replace it?", "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing.gameObject);

            // Also remove old subtitle panel
            GameObject oldPanel = GameObject.Find("SL_SubtitlePanel");
            if (oldPanel != null) Undo.DestroyObjectImmediate(oldPanel);

            GameObject oldBtn = GameObject.Find("SL_ToggleButton");
            if (oldBtn != null) Undo.DestroyObjectImmediate(oldBtn);
        }

        Undo.SetCurrentGroupName("Setup Sign Language AR");
        int undoGroup = Undo.GetCurrentGroup();

        // ── 3. Create ARSignLanguageSystem Manager object ────────────────────
        GameObject managerGO = new GameObject("ARSignLanguageSystem");
        Undo.RegisterCreatedObjectUndo(managerGO, "Create ARSignLanguageSystem");
        ARSignLanguageSystem system = managerGO.AddComponent<ARSignLanguageSystem>();

        // ── 4. Create Subtitle Panel ─────────────────────────────────────────
        // Parent: main Canvas
        GameObject panelGO = CreateSubtitlePanel(mainCanvas.transform);

        // Get text component inside the panel
        TextMeshProUGUI subtitleTMP = panelGO.GetComponentInChildren<TextMeshProUGUI>();

        // ── 5. Create Toggle Button ───────────────────────────────────────────
        GameObject buttonGO = CreateToggleButton(mainCanvas.transform, system);
        SignLanguageToggleButton toggleBtn = buttonGO.GetComponent<SignLanguageToggleButton>();
        TextMeshProUGUI btnLabel = buttonGO.GetComponentInChildren<TextMeshProUGUI>();

        // ── 6. Wire ARSignLanguageSystem references ───────────────────────────
        Undo.RecordObject(system, "Wire SignLanguage references");
        system.subtitleText    = subtitleTMP;
        system.subtitlePanel   = panelGO;
        system.toggleButtonLabel = btnLabel;

        // ── 7. Wire Toggle Button ─────────────────────────────────────────────
        Undo.RecordObject(toggleBtn, "Wire ToggleButton");
        toggleBtn.signLanguageSystem = system;
        toggleBtn.buttonLabel        = btnLabel;

        // Hide button by default (visible only in AR mode via VRARSwitcher)
        buttonGO.SetActive(false);

        // ── 8. Wire VRARSwitcher ──────────────────────────────────────────────
        Undo.RecordObject(switcher, "Wire VRARSwitcher signLanguageButton");
        switcher.signLanguageButton = toggleBtn;

        // ── 9. Android camera permission ─────────────────────────────────────
        EnsureCameraPermission();

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(switcher);
        EditorUtility.SetDirty(system);

        EditorUtility.DisplayDialog(
            "Sign Language AR Setup Complete",
            "✅ All done! Here is what was created:\n\n" +
            "• ARSignLanguageSystem (manager GameObject)\n" +
            "• SL_SubtitlePanel (subtitle UI in canvas)\n" +
            "• SL_ToggleButton (AR-only toggle button)\n" +
            "• Camera permission added to AndroidManifest\n\n" +
            "HOW TO TEST:\n" +
            "1. Press Play\n" +
            "2. Switch to AR mode\n" +
            "3. Press 'Sign Language   OFF' button → turns green\n" +
            "4. On PC: press keys 1-9 to simulate signs\n" +
            "5. On Quest: point camera at person signing\n\n" +
            "Keys 1-9 = שלום / תודה / כן / לא / עזרה / רופא / כאב / מה שלומך / טוב",
            "OK");

        Debug.Log("[SignLanguageSetupTool] Setup complete.");
    }

    // ── Subtitle Panel ───────────────────────────────────────────────────────

    private static GameObject CreateSubtitlePanel(Transform canvasParent)
    {
        // Panel background
        GameObject panelGO = new GameObject("SL_SubtitlePanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Create subtitle panel");
        panelGO.transform.SetParent(canvasParent, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        // Bottom strip — full width, 18% height
        panelRT.anchorMin = new Vector2(0f,  0f);
        panelRT.anchorMax = new Vector2(1f, 0.18f);
        panelRT.offsetMin = new Vector2(20f, 20f);
        panelRT.offsetMax = new Vector2(-20f, 0f);

        // Semi-transparent dark background
        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        // Subtitle text child
        GameObject textGO = new GameObject("SL_SubtitleText");
        Undo.RegisterCreatedObjectUndo(textGO, "Create subtitle text");
        textGO.transform.SetParent(panelGO.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 8f);
        textRT.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "";
        tmp.fontSize  = 42;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;

        // Use default TMP font (LiberationSans SDF — always available)
        var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null) tmp.font = defaultFont;

        panelGO.SetActive(false);  // hidden until text appears
        return panelGO;
    }

    // ── Toggle Button ────────────────────────────────────────────────────────

    private static GameObject CreateToggleButton(Transform canvasParent,
                                                  ARSignLanguageSystem system)
    {
        // Position: top-right corner of canvas, small button
        GameObject btnGO = new GameObject("SL_ToggleButton");
        Undo.RegisterCreatedObjectUndo(btnGO, "Create SL toggle button");
        btnGO.transform.SetParent(canvasParent, false);

        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 1f);
        btnRT.anchorMax = new Vector2(1f, 1f);
        btnRT.pivot     = new Vector2(1f, 1f);
        btnRT.anchoredPosition = new Vector2(-20f, -20f);
        btnRT.sizeDelta = new Vector2(280f, 70f);

        // Background — matches SignLanguageToggleButton.colorWhenOff (dark navy)
        Image btnImage = btnGO.AddComponent<Image>();
        btnImage.color = new Color(0.13f, 0.18f, 0.32f, 1f);

        // Button component
        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.20f, 0.26f, 0.44f, 1f);
        colors.pressedColor     = new Color(0.08f, 0.12f, 0.22f, 1f);
        btn.colors = colors;

        // Label child
        GameObject labelGO = new GameObject("Label");
        Undo.RegisterCreatedObjectUndo(labelGO, "Create button label");
        labelGO.transform.SetParent(btnGO.transform, false);

        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(8f, 4f);
        labelRT.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text      = "Sign Language   OFF";
        label.fontSize  = 22;
        label.color     = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;

        var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null) label.font = defaultFont;

        // Attach toggle script
        SignLanguageToggleButton toggleScript = btnGO.AddComponent<SignLanguageToggleButton>();
        toggleScript.signLanguageSystem = system;
        toggleScript.buttonLabel        = label;
        toggleScript.labelWhenOff       = "Sign Language   OFF";
        toggleScript.labelWhenOn        = "Sign Language   ON  ●";

        // Wire Button.onClick → toggle script
        btn.onClick.AddListener(toggleScript.OnButtonClick);

        return btnGO;
    }

    // ── Android camera permission ─────────────────────────────────────────────

    private static void EnsureCameraPermission()
    {
        string manifestDir  = "Assets/Plugins/Android";
        string manifestPath = manifestDir + "/AndroidManifest.xml";

        if (!System.IO.Directory.Exists(manifestDir))
            System.IO.Directory.CreateDirectory(manifestDir);

        string cameraPermission =
            "<uses-permission android:name=\"android.permission.CAMERA\" />";

        if (!System.IO.File.Exists(manifestPath))
        {
            // Create a minimal manifest with camera permission
            string manifest =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"\n" +
                "          package=\"com.medmeet.vr\">\n" +
                "  " + cameraPermission + "\n" +
                "  <uses-feature android:name=\"android.hardware.camera\" />\n" +
                "</manifest>\n";

            System.IO.File.WriteAllText(manifestPath, manifest);
            AssetDatabase.ImportAsset(manifestPath);
            Debug.Log("[SignLanguageSetupTool] Created AndroidManifest.xml with camera permission.");
        }
        else
        {
            string content = System.IO.File.ReadAllText(manifestPath);
            if (!content.Contains("android.permission.CAMERA"))
            {
                // Insert before </manifest>
                content = content.Replace("</manifest>",
                    "  " + cameraPermission + "\n  " +
                    "<uses-feature android:name=\"android.hardware.camera\" />\n</manifest>");
                System.IO.File.WriteAllText(manifestPath, content);
                AssetDatabase.ImportAsset(manifestPath);
                Debug.Log("[SignLanguageSetupTool] Added CAMERA permission to existing AndroidManifest.");
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Canvas FindMainCanvas(VRARSwitcher switcher)
    {
        // Try VRARSwitcher.mainCanvas first
        if (switcher.mainCanvas != null) return switcher.mainCanvas;

        // Fallback: find any World Space canvas
        Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in all)
            if (c.renderMode == RenderMode.WorldSpace) return c;

        // Last resort: any canvas
        return Object.FindFirstObjectByType<Canvas>();
    }
}
