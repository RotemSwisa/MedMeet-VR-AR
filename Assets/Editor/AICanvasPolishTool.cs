using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MedMeet Tools → Polish AI Canvas
///
/// Visual-only upgrade for the existing AI-Canvas in the scene. Critically,
/// this does NOT remove or rename any GameObject the runtime references
/// (ClinicalAdvisorUI keeps all of its wiring). It only:
///   1. Inserts a Starfield background panel as the first child (behind the chat).
///   2. Restyles existing panels and buttons (colours, corner radius, fonts).
///   3. Scales the canvas up by 1.3× in world-space so it reads better in VR.
///   4. Adds ChatScrollLock to the existing ScrollRect for snap-free scrolling.
///
/// Run from the menu after the scene is open. Safe to run multiple times —
/// existing extra GameObjects (StarfieldBackground, ChatScrollLock) get
/// reused instead of duplicated.
/// </summary>
public static class AICanvasPolishTool
{
    [MenuItem("MedMeet Tools/Polish AI Canvas")]
    public static void Polish()
    {
        var ui = Object.FindFirstObjectByType<ClinicalAdvisorUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("AI Canvas Polish",
                "ClinicalAdvisorUI not found in the open scene.\n\n" +
                "Make sure the AI-Canvas prefab is instantiated in the scene first.",
                "OK");
            return;
        }

        var canvasGO = FindCanvasGO(ui.gameObject);
        if (canvasGO == null)
        {
            // Fallback: look for any GameObject named like the prefab and use that.
            // (Allows polish to proceed even when no Canvas component is present
            //  on the AI-Canvas root — for example when it's a WorldSpace canvas
            //  embedded inside another canvas hierarchy.)
            foreach (string n in new[] { "AI-Canvas", "AICanvas", "AI_Canvas" })
            {
                var go = GameObject.Find(n);
                if (go != null) { canvasGO = go; break; }
            }
            if (canvasGO == null)
            {
                Debug.LogError(
                    $"[AICanvasPolish] Couldn't find Canvas root.\n" +
                    $"  ClinicalAdvisorUI GameObject: {ui.gameObject.name}\n" +
                    $"  Parent chain: {DescribeParents(ui.transform)}",
                    ui.gameObject);
                EditorUtility.DisplayDialog("AI Canvas Polish",
                    "Couldn't locate the Canvas root for AI-Canvas.\n\n" +
                    "Open the Console — I've printed the parent chain of " +
                    "ClinicalAdvisorUI so we can see why the search failed.",
                    "OK");
                return;
            }
            Debug.Log($"[AICanvasPolish] Using fallback root: {canvasGO.name}");
        }

        Undo.SetCurrentGroupName("Polish AI Canvas");
        int group = Undo.GetCurrentGroup();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== AI Canvas polish ===");
        report.AppendLine($"Canvas root: {canvasGO.name}");

        try
        {
            // 1. Starfield backdrop
            EnsureStarfield(canvasGO, report);

            // 2. ChatScrollLock on the ScrollRect (+ make scroll usable)
            EnsureScrollLock(ui, report);

            // 3. Restyle buttons
            RestyleButtons(ui, report);

            // 4. Restyle panels (input area, status, etc.)
            RestylePanels(ui, report);

            // 5. Refine any title/headline text in the canvas
            RestyleTitles(canvasGO, ui, report);

            // Note: previous versions auto-bumped the canvas scale by ×1.3,
            // which compounded on re-runs. The scale is now left to the user
            // — drag the canvas root in Scene view to resize, or set localScale
            // manually on the AI-Canvas GameObject.

            EditorUtility.SetDirty(ui.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        catch (System.Exception ex)
        {
            report.AppendLine("✗ FAILED: " + ex.Message);
            Debug.LogException(ex);
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("AI Canvas Polish", report + "\n\nPress Ctrl+S to save.", "OK");
    }

    // ── Sub-builders ───────────────────────────────────────────────────────
    static string DescribeParents(Transform t)
    {
        var parts = new List<string>();
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

    static GameObject FindCanvasGO(GameObject anyChild)
    {
        // Try parent chain first (most common — Canvas is at the root)
        var canvas = anyChild.GetComponentInParent<Canvas>(includeInactive: true);
        if (canvas != null) return canvas.gameObject;

        // Then look down the hierarchy (some prefabs nest the Canvas one level deeper)
        canvas = anyChild.GetComponentInChildren<Canvas>(includeInactive: true);
        if (canvas != null) return canvas.gameObject;

        // Finally, walk up to the scene root and check siblings
        var root = anyChild.transform.root;
        canvas = root.GetComponentInChildren<Canvas>(includeInactive: true);
        if (canvas != null) return canvas.gameObject;

        // Last-ditch: any Canvas in the open scene that has ClinicalAdvisorUI on it or in its children
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.GetComponentInChildren<ClinicalAdvisorUI>(includeInactive: true) != null)
                return c.gameObject;
        }
        return null;
    }

    static void EnsureStarfield(GameObject canvasGO, System.Text.StringBuilder report)
    {
        // Reuse if already exists
        var existing = canvasGO.transform.Find("StarfieldBG");
        GameObject bg = existing != null ? existing.gameObject : null;
        if (bg == null)
        {
            bg = new GameObject("StarfieldBG", typeof(RectTransform), typeof(Image), typeof(StarfieldBackground));
            Undo.RegisterCreatedObjectUndo(bg, "Create StarfieldBG");
            bg.transform.SetParent(canvasGO.transform, false);
            report.AppendLine("✓ StarfieldBG created");
        }
        else
        {
            if (bg.GetComponent<Image>() == null) Undo.AddComponent<Image>(bg);
            if (bg.GetComponent<StarfieldBackground>() == null) Undo.AddComponent<StarfieldBackground>(bg);
            report.AppendLine("✓ StarfieldBG reused");
        }

        // Resize using the canvas RectTransform's actual rect so it ALWAYS
        // covers everything — even when the canvas isn't a simple full-rect.
        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
        bg.transform.SetAsFirstSibling();   // behind everything

        // Solid black image so the starfield reads cleanly; never blocks clicks
        var img = bg.GetComponent<Image>();
        img.color = new Color(0.020f, 0.020f, 0.024f, 1f);   // near-black
        img.raycastTarget = false;

        // Configure the StarfieldBackground component itself — calmer + wider
        var sf = bg.GetComponent<StarfieldBackground>();
        if (sf != null)
        {
            sf.backgroundColor = new Color(0.020f, 0.020f, 0.024f, 1f);
            sf.starColor       = new Color(1f, 1f, 1f, 0.75f);
            sf.trailColor      = new Color(0.78f, 0.82f, 0.92f, 0.30f);
            sf.starCount       = 110;
            sf.speedRangeY     = new Vector2(40f, 140f);
            sf.driftRangeX     = new Vector2(-8f, 8f);
            sf.sizeRange       = new Vector2(2f, 5f);
            sf.lifeRange       = new Vector2(4f, 8f);
            sf.autoFitToRect   = true;
            EditorUtility.SetDirty(sf);
        }
    }

    static void EnsureScrollLock(ClinicalAdvisorUI ui, System.Text.StringBuilder report)
    {
        if (ui.chatScrollRect == null) { report.AppendLine("⚠️ ScrollRect missing — scroll lock skipped"); return; }

        // Force healthy ScrollRect settings so drag-to-scroll actually works
        var sr = ui.chatScrollRect;
        Undo.RecordObject(sr, "ScrollRect settings");
        sr.vertical          = true;
        sr.horizontal        = false;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.inertia           = false;
        sr.scrollSensitivity = Mathf.Max(sr.scrollSensitivity, 35f);
        EditorUtility.SetDirty(sr);

        var lockComp = sr.GetComponent<ChatScrollLock>();
        if (lockComp == null)
        {
            Undo.AddComponent<ChatScrollLock>(sr.gameObject);
            report.AppendLine("✓ ChatScrollLock attached + ScrollRect tuned");
        }
        else
        {
            report.AppendLine("✓ ChatScrollLock already present (ScrollRect re-tuned)");
        }
    }

    static void RestyleButtons(ClinicalAdvisorUI ui, System.Text.StringBuilder report)
    {
        // Calm grayscale palette — slightly different intensities so the
        // three buttons are still distinguishable without using bright hues.
        Color charcoal = new Color(0.118f, 0.122f, 0.137f, 1f);   // primary buttons
        Color graphite = new Color(0.078f, 0.082f, 0.094f, 1f);   // secondary
        Color slate    = new Color(0.156f, 0.164f, 0.184f, 1f);   // tertiary

        int styled = 0;
        styled += StyleButton(ui.startQuestionButton, charcoal, new Color(0.92f, 0.94f, 0.96f, 1f));
        styled += StyleButton(ui.sendVoiceButton,     graphite, new Color(0.85f, 0.87f, 0.90f, 1f));
        styled += StyleButton(ui.sendTextButton,      slate,    new Color(0.92f, 0.94f, 0.96f, 1f));
        report.AppendLine($"✓ Restyled {styled} buttons (charcoal palette)");
    }

    static int StyleButton(Button btn, Color bg, Color textColor)
    {
        if (btn == null) return 0;
        Undo.RecordObject(btn, "Style AI button");

        // Background — solid dark with rounded sprite + subtle white outline
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            Undo.RecordObject(img, "Button image");
            img.color = bg;
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (rounded != null) img.sprite = rounded;
            img.type = Image.Type.Sliced;
        }
        // Thin pale border via Outline component for a refined edge
        var outline = btn.GetComponent<Outline>() ?? btn.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.10f);
        outline.effectDistance = new Vector2(1, -1);
        outline.useGraphicAlpha = false;

        // ColorBlock — subtle highlight, no bright colors
        var cb = btn.colors;
        cb.normalColor      = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);   // very gentle hover
        cb.pressedColor     = Color.Lerp(bg, Color.black, 0.30f);
        cb.selectedColor    = Color.Lerp(bg, Color.white, 0.08f);
        cb.disabledColor    = new Color(bg.r, bg.g, bg.b, bg.a * 0.4f);
        cb.fadeDuration     = 0.15f;
        cb.colorMultiplier  = 1f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        // Label — keep existing text, just tune colour + weight
        var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (lbl != null)
        {
            Undo.RecordObject(lbl, "Button label colour");
            lbl.color = textColor;
            lbl.fontStyle = FontStyles.Bold;
            lbl.characterSpacing = 2f;   // a little breathing room
        }

        EditorUtility.SetDirty(btn);
        return 1;
    }

    static void RestylePanels(ClinicalAdvisorUI ui, System.Text.StringBuilder report)
    {
        // Status label — soft pale grey, refined
        if (ui.statusText != null)
        {
            Undo.RecordObject(ui.statusText, "Status colour");
            ui.statusText.color = new Color(0.62f, 0.66f, 0.72f, 1f);
            ui.statusText.fontStyle = FontStyles.Normal;   // not bold — calmer
            ui.statusText.characterSpacing = 2f;
            EditorUtility.SetDirty(ui.statusText);
        }

        // Chat history text — clean off-white, NEVER blocks raycast
        if (ui.chatHistoryText != null)
        {
            Undo.RecordObject(ui.chatHistoryText, "Chat colour");
            ui.chatHistoryText.color = new Color(0.88f, 0.90f, 0.93f, 1f);
            ui.chatHistoryText.raycastTarget = false;   // ← THE scroll fix
            EditorUtility.SetDirty(ui.chatHistoryText);
        }

        // Input field background — graphite with thin border
        if (ui.textInputField != null)
        {
            var img = ui.textInputField.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Input background");
                img.color = new Color(0.094f, 0.098f, 0.110f, 1f);
                EditorUtility.SetDirty(img);
            }
            // Subtle border
            var ol = ui.textInputField.GetComponent<Outline>() ?? ui.textInputField.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(1f, 1f, 1f, 0.10f);
            ol.effectDistance = new Vector2(1, -1);
            ol.useGraphicAlpha = false;

            // VR-friendly settings — keep keyboard alive while typing
            Undo.RecordObject(ui.textInputField, "Input field VR options");
            ui.textInputField.shouldHideMobileInput = false;
            ui.textInputField.lineType = TMP_InputField.LineType.SingleLine;
            ui.textInputField.characterLimit = 300;
            EditorUtility.SetDirty(ui.textInputField);
        }

        // ScrollRect viewport — transparent so the starfield shows through.
        // CRITICAL: the viewport's Image MUST keep raycastTarget=true so the
        // ScrollRect receives drag events. Setting it to false here would
        // break scrolling entirely.
        if (ui.chatScrollRect != null && ui.chatScrollRect.viewport != null)
        {
            var img = ui.chatScrollRect.viewport.GetComponent<Image>();
            if (img != null)
            {
                Undo.RecordObject(img, "Viewport bg");
                img.color = new Color(0f, 0f, 0f, 0.30f);
                img.raycastTarget = true;   // explicit — required for drag
                EditorUtility.SetDirty(img);
            }
        }

        // Belt-and-braces: clear raycastTarget on every TMP_Text inside the
        // ScrollRect's content so drag events bubble up to the ScrollRect.
        // Any text/image that needs clicks (buttons, etc.) keeps theirs.
        DisableRaycastOnText(ui.chatScrollRect, report);

        report.AppendLine("✓ Panels recoloured (charcoal palette)");
    }

    /// <summary>
    /// Walk the ScrollRect's Content subtree and disable raycastTarget on
    /// every TMP_Text / Image that isn't part of a Button. Without this,
    /// the text inside Content intercepts pointer drag and the user can't
    /// actually scroll by grabbing the chat.
    /// </summary>
    static void DisableRaycastOnText(ScrollRect scroll, System.Text.StringBuilder report)
    {
        if (scroll == null || scroll.content == null) return;

        int cleared = 0;
        foreach (var tmp in scroll.content.GetComponentsInChildren<TMPro.TMP_Text>(includeInactive: true))
        {
            if (tmp.GetComponentInParent<Selectable>() != null) continue; // keep button labels
            Undo.RecordObject(tmp, "Disable raycast");
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            cleared++;
        }
        foreach (var img in scroll.content.GetComponentsInChildren<Image>(includeInactive: true))
        {
            if (img.GetComponentInParent<Selectable>() != null) continue;
            Undo.RecordObject(img, "Disable raycast");
            img.raycastTarget = false;
            EditorUtility.SetDirty(img);
            cleared++;
        }
        if (cleared > 0)
            report.AppendLine($"✓ Cleared raycastTarget on {cleared} child graphic(s) — scroll drag now works");
    }

    /// <summary>
    /// Sweep every TMP_Text under the canvas and apply a refined typography
    /// pass — calmer colours, light/regular weights for big text, monospace
    /// letter-spacing on small caps. Skips text that's part of a Button (we
    /// already styled those) and skips text inside the ScrollRect's Content
    /// (those are user chat messages — leave them alone).
    /// </summary>
    static void RestyleTitles(GameObject canvasGO, ClinicalAdvisorUI ui, System.Text.StringBuilder report)
    {
        int titles = 0, body = 0, captions = 0;

        var contentRoot = ui.chatScrollRect != null ? ui.chatScrollRect.content : null;

        foreach (var tmp in canvasGO.GetComponentsInChildren<TMPro.TMP_Text>(includeInactive: true))
        {
            if (tmp == null) continue;
            // Skip button labels — they get their own styling
            if (tmp.GetComponentInParent<Selectable>() != null) continue;
            // Skip chat history + placeholder + anything inside the ScrollRect content
            if (contentRoot != null && tmp.transform.IsChildOf(contentRoot)) continue;
            if (tmp == ui.chatHistoryText) continue;
            if (tmp == ui.statusText) continue;   // styled separately as caption

            Undo.RecordObject(tmp, "Refine title");
            float size = tmp.fontSize;
            if (size >= 28f)
            {
                // Title — light weight, generous tracking, near-white colour
                tmp.color = new Color(0.92f, 0.94f, 0.96f, 1f);
                tmp.fontStyle = FontStyles.Normal;   // not bold — feels more elegant
                tmp.characterSpacing = 4f;
                titles++;
            }
            else if (size >= 18f)
            {
                // Body label
                tmp.color = new Color(0.78f, 0.81f, 0.85f, 1f);
                tmp.fontStyle &= ~FontStyles.Bold;
                tmp.characterSpacing = 1.5f;
                body++;
            }
            else
            {
                // Caption — small grey, slight letter spacing
                tmp.color = new Color(0.60f, 0.63f, 0.68f, 1f);
                tmp.characterSpacing = 2f;
                captions++;
            }
            EditorUtility.SetDirty(tmp);
        }
        report.AppendLine($"✓ Typography pass — titles:{titles}, body:{body}, captions:{captions}");
    }

    static void BumpCanvasScale(GameObject canvasGO, float multiplier, System.Text.StringBuilder report)
    {
        // World-space canvases use transform.localScale — multiply, don't overwrite,
        // so we keep the user's manual placement.
        if (canvasGO.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            Undo.RecordObject(canvasGO.transform, "Scale AI Canvas");
            canvasGO.transform.localScale *= multiplier;
            EditorUtility.SetDirty(canvasGO);
            report.AppendLine($"✓ Canvas scale ×{multiplier} (now {canvasGO.transform.localScale.x:F4})");
        }
        else
        {
            report.AppendLine("• Skipped scale bump (not WorldSpace)");
        }
    }
}
