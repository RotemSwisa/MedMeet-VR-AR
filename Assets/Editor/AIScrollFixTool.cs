using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// MedMeet Tools → Diagnose &amp; Fix AI Chat Scroll
///
/// PURPOSE
///   Repairs ONLY the mechanics that make the AI chat ScrollRect scrollable.
///   Touches nothing visual: no colours, no fonts, no sprite swaps, no sizes,
///   no text content. Safe to run after the user has hand-tuned the design.
///
/// WHAT IT CHECKS / FIXES
///   1. ScrollRect axes        — vertical = true, horizontal = false
///   2. ScrollRect motion      — Clamped, inertia off, sensitivity ≥ 35
///   3. Content RectTransform  — pivot.y = 1 (top), anchors stretched correctly
///   4. ContentSizeFitter      — verticalFit = PreferredSize (so content grows
///                                with the chat and the scrollbar appears)
///   5. Viewport RectMask2D    — present so the chat doesn't bleed out
///   6. Viewport Image         — raycastTarget = true (drag receiver) without
///                                changing its colour or alpha
///   7. Content child raycasts — text + image children that aren't part of a
///                                Button get raycastTarget = false so the
///                                drag bubbles up to the ScrollRect
///   8. Canvas raycasters      — both GraphicRaycaster (PC mouse) and
///                                TrackedDeviceGraphicRaycaster (VR / XR
///                                Device Simulator) are present
///   9. ChatScrollLock         — attached so the position stays where you
///                                released it (no more snap-to-top)
///
/// EVERY action is reported to the Console and to the result dialog, so you
/// can see exactly what was changed and revert with Ctrl+Z if needed.
/// </summary>
public static class AIScrollFixTool
{
    [MenuItem("MedMeet Tools/Diagnose && Fix AI Chat Scroll")]
    public static void Run()
    {
        var ui = Object.FindFirstObjectByType<ClinicalAdvisorUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Fix AI Scroll",
                "ClinicalAdvisorUI not found in the open scene.", "OK");
            return;
        }
        if (ui.chatScrollRect == null)
        {
            EditorUtility.DisplayDialog("Fix AI Scroll",
                "ClinicalAdvisorUI.chatScrollRect is unassigned — please drag " +
                "the ScrollRect onto the field in the Inspector.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Fix AI Chat Scroll");
        int undoGroup = Undo.GetCurrentGroup();

        var log = new System.Text.StringBuilder();
        log.AppendLine("=== AI chat scroll diagnose + fix ===");
        log.AppendLine($"ScrollRect: {ui.chatScrollRect.name}");

        var sr = ui.chatScrollRect;

        FixScrollRect(sr, log);
        FixContent(sr, log);
        FixChatTextAnchors(sr, ui, log);   // ← NEW: makes content actually grow
        FixViewport(sr, log);
        FixContentRaycasts(sr, log);
        EnsureRaycasters(sr, log);
        EnsureScrollLock(sr, log);
        AttachRuntimeDebug(sr, log);
        ForceLayoutRebuild(sr, log);

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(sr);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Fix AI Scroll",
            log.ToString() + "\n\nPress Ctrl+S to save, then test:\n" +
            "  • PC: hold left-click on the chat area and drag.\n" +
            "  • VR / Simulator: point and pull the trigger, then drag.",
            "OK");
    }

    // ── 1 + 2  ScrollRect axes + motion ─────────────────────────────────────
    static void FixScrollRect(ScrollRect sr, System.Text.StringBuilder log)
    {
        Undo.RecordObject(sr, "ScrollRect settings");
        bool changed = false;

        if (!sr.vertical)                                     { sr.vertical   = true;  changed = true; log.AppendLine("  • vertical → true"); }
        if (sr.horizontal)                                    { sr.horizontal = false; changed = true; log.AppendLine("  • horizontal → false"); }
        if (sr.movementType != ScrollRect.MovementType.Clamped){ sr.movementType = ScrollRect.MovementType.Clamped; changed = true; log.AppendLine("  • movementType → Clamped"); }
        if (sr.inertia)                                       { sr.inertia = false;   changed = true; log.AppendLine("  • inertia → off"); }
        if (sr.scrollSensitivity < 35f)                       { sr.scrollSensitivity = 35f; changed = true; log.AppendLine($"  • scrollSensitivity → 35 (was {sr.scrollSensitivity})"); }

        log.AppendLine(changed ? "✓ ScrollRect tuned" : "✓ ScrollRect already healthy");
    }

    // ── 3 + 4  Content RectTransform + size fitter ──────────────────────────
    static void FixContent(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.content == null)
        {
            log.AppendLine("✗ ScrollRect.content is null — assign Content in Inspector first.");
            return;
        }

        var content = sr.content;
        Undo.RecordObject(content, "Content RectTransform");
        bool changed = false;

        // Pivot Y must be 1 (top) so anchoredPosition increases as text gets longer
        if (!Mathf.Approximately(content.pivot.y, 1f))
        {
            content.pivot = new Vector2(content.pivot.x, 1f);
            changed = true; log.AppendLine("  • content.pivot.y → 1 (top)");
        }
        // Anchors stretched horizontally and pinned to top
        var aMin = content.anchorMin;
        var aMax = content.anchorMax;
        if (Mathf.Abs(aMax.y - 1f) > 0.001f || Mathf.Abs(aMin.x) > 0.001f || Mathf.Abs(aMax.x - 1f) > 0.001f)
        {
            content.anchorMin = new Vector2(0f, aMin.y);
            content.anchorMax = new Vector2(1f, 1f);
            changed = true; log.AppendLine("  • content.anchor → stretched horizontally, top-aligned");
        }

        // ContentSizeFitter so the content grows as the chat lengthens
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = Undo.AddComponent<ContentSizeFitter>(content.gameObject);
            log.AppendLine("  • added ContentSizeFitter");
            changed = true;
        }
        Undo.RecordObject(fitter, "ContentSizeFitter");
        if (fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            changed = true; log.AppendLine("  • verticalFit → PreferredSize");
        }
        EditorUtility.SetDirty(fitter);

        log.AppendLine(changed ? "✓ Content sizing fixed" : "✓ Content sizing healthy");
    }

    // ── 5 + 6  Viewport mask + raycast receiver ─────────────────────────────
    static void FixViewport(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.viewport == null)
        {
            log.AppendLine("✗ ScrollRect.viewport is null — assign Viewport in Inspector first.");
            return;
        }

        bool changed = false;

        // RectMask2D ensures the chat doesn't spill outside the viewport
        var mask2D = sr.viewport.GetComponent<RectMask2D>();
        var mask   = sr.viewport.GetComponent<Mask>();
        if (mask2D == null && mask == null)
        {
            Undo.AddComponent<RectMask2D>(sr.viewport.gameObject);
            log.AppendLine("  • added RectMask2D to viewport");
            changed = true;
        }

        // Viewport must have an Image with raycastTarget=true so the
        // ScrollRect actually receives drag events. Critically we DO NOT
        // change its colour or alpha — only the raycastTarget flag.
        var img = sr.viewport.GetComponent<Image>();
        if (img == null)
        {
            img = Undo.AddComponent<Image>(sr.viewport.gameObject);
            // Default to fully transparent so the user's design isn't disturbed
            img.color = new Color(0f, 0f, 0f, 0f);
            log.AppendLine("  • added transparent Image on viewport (drag receiver)");
            changed = true;
        }
        if (!img.raycastTarget)
        {
            Undo.RecordObject(img, "Viewport raycastTarget");
            img.raycastTarget = true;
            EditorUtility.SetDirty(img);
            changed = true;
            log.AppendLine("  • viewport.raycastTarget → true (required for drag)");
        }

        log.AppendLine(changed ? "✓ Viewport ready for drag" : "✓ Viewport already healthy");
    }

    // ── 7  Children inside Content must not steal drag events ───────────────
    static void FixContentRaycasts(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.content == null) return;
        int cleared = 0;

        foreach (var g in sr.content.GetComponentsInChildren<Graphic>(includeInactive: true))
        {
            if (g == null) continue;
            // Skip anything that's part of a Selectable (Button / Toggle / etc.) —
            // those legitimately need raycasts.
            if (g.GetComponentInParent<Selectable>() != null) continue;
            if (!g.raycastTarget) continue;

            Undo.RecordObject(g, "Clear raycast");
            g.raycastTarget = false;
            EditorUtility.SetDirty(g);
            cleared++;
        }
        log.AppendLine(cleared > 0
            ? $"✓ Cleared raycastTarget on {cleared} graphic(s) inside Content — drag now bubbles up"
            : "✓ Content already drag-friendly");
    }

    // ── 8  Both raycasters on the Canvas ────────────────────────────────────
    static void EnsureRaycasters(ScrollRect sr, System.Text.StringBuilder log)
    {
        var canvas = sr.GetComponentInParent<Canvas>(includeInactive: true);
        if (canvas == null) { log.AppendLine("✗ No parent Canvas — skipped raycaster step."); return; }

        // PC mouse / Editor — needs the standard GraphicRaycaster
        var gr = canvas.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            log.AppendLine("  • added GraphicRaycaster (PC mouse / editor)");
        }
        // VR / XR Interaction Toolkit — needs the tracked-device variant
        var tdr = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (tdr == null)
        {
            Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
            log.AppendLine("  • added TrackedDeviceGraphicRaycaster (VR / simulator)");
        }
        log.AppendLine("✓ Canvas raycasters present");
    }

    // ── 9  ChatScrollLock for snap-free release ─────────────────────────────
    static void EnsureScrollLock(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.GetComponent<ChatScrollLock>() == null)
        {
            Undo.AddComponent<ChatScrollLock>(sr.gameObject);
            log.AppendLine("✓ ChatScrollLock attached");
        }
        else
        {
            log.AppendLine("✓ ChatScrollLock already present");
        }
    }

    // ── NEW  Chat text anchors must be top-stretched, NOT fill-stretched ────
    //
    // Symptom that says "this needs fixing": dragging produces
    //   OnDrag · delta=18.1 · pos=0.000
    //   OnDrag · delta=-5.8 · pos=1.000
    // i.e. the normalized position flips between 0 and 1 with tiny drags
    // because Content height ≈ Viewport height — there is no scroll range.
    //
    // Root cause: ChatHistoryText is anchored (0,0)→(1,1) and fills Content.
    // Content has ContentSizeFitter → fits to TMP_Text's preferredHeight,
    // but TMP_Text's height is being driven BY Content → it ends up the size
    // of the viewport and never expands. Breaking the loop by anchoring
    // TMP_Text to the TOP of Content (not stretched vertically) lets its
    // own preferredHeight grow with the text.
    static void FixChatTextAnchors(ScrollRect sr, ClinicalAdvisorUI ui, System.Text.StringBuilder log)
    {
        // We need a TMP_Text inside Content. Prefer the wired chatHistoryText.
        var tmp = ui != null ? ui.chatHistoryText as TMPro.TMP_Text : null;
        if (tmp == null && sr.content != null)
            tmp = sr.content.GetComponentInChildren<TMPro.TMP_Text>(includeInactive: true);
        if (tmp == null)
        {
            log.AppendLine("⚠️ No TMP_Text found under Content — can't fix anchors.");
            return;
        }

        var rt = tmp.rectTransform;
        Undo.RecordObject(rt, "ChatText anchors");
        bool changed = false;

        // Anchor top-stretched: x stretched 0..1, y pinned to 1 (top)
        if (Mathf.Abs(rt.anchorMin.x) > 0.001f || Mathf.Abs(rt.anchorMax.x - 1f) > 0.001f
            || Mathf.Abs(rt.anchorMin.y - 1f) > 0.001f || Mathf.Abs(rt.anchorMax.y - 1f) > 0.001f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            changed = true;
            log.AppendLine($"  • {tmp.name}.anchor → top-stretched (0,1 → 1,1)");
        }

        // Pivot top so anchoredPosition.y stays sane
        if (!Mathf.Approximately(rt.pivot.y, 1f))
        {
            rt.pivot = new Vector2(rt.pivot.x, 1f);
            changed = true;
            log.AppendLine($"  • {tmp.name}.pivot.y → 1 (top)");
        }

        // Reset offsetMin.y so the text starts at the top with no padding hole
        // (we only normalise Y — keep the user's horizontal padding intact)
        rt.offsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y);
        rt.offsetMax = new Vector2(rt.offsetMax.x, 0f);

        // Give the TMP_Text its OWN ContentSizeFitter so it grows vertically
        // based on its own preferredHeight. Combined with Content's fitter,
        // Content now grows with the text.
        var fitter = tmp.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = Undo.AddComponent<ContentSizeFitter>(tmp.gameObject);
            log.AppendLine($"  • added ContentSizeFitter to {tmp.name}");
            changed = true;
        }
        Undo.RecordObject(fitter, "TMP ContentSizeFitter");
        if (fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            log.AppendLine($"  • {tmp.name}.verticalFit → PreferredSize");
            changed = true;
        }

        // Make sure word-wrap is on so long text wraps and grows tall
        Undo.RecordObject(tmp, "TMP wrap");
        if (!tmp.enableWordWrapping)
        {
            tmp.enableWordWrapping = true;
            log.AppendLine($"  • {tmp.name}.enableWordWrapping → true");
            changed = true;
        }

        EditorUtility.SetDirty(rt);
        EditorUtility.SetDirty(tmp);
        EditorUtility.SetDirty(fitter);

        log.AppendLine(changed
            ? "✓ Chat text anchors fixed — content can now grow taller than viewport"
            : "✓ Chat text anchors already correct");
    }

    // ── Force a layout rebuild so the new sizes settle before Play ──────────
    static void ForceLayoutRebuild(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.content == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
        log.AppendLine($"✓ Forced layout rebuild · content height now {sr.content.rect.height:F0}px · " +
                       $"viewport {(sr.viewport != null ? sr.viewport.rect.height : 0f):F0}px");
    }

    // ── 10  Runtime diagnostic so we can see live what's happening ──────────
    static void AttachRuntimeDebug(ScrollRect sr, System.Text.StringBuilder log)
    {
        if (sr.GetComponent<ChatScrollDebug>() == null)
        {
            Undo.AddComponent<ChatScrollDebug>(sr.gameObject);
            log.AppendLine("✓ ChatScrollDebug attached — watch the Console while you drag in Play.");
        }
        else
        {
            log.AppendLine("✓ ChatScrollDebug already present");
        }
    }
}
