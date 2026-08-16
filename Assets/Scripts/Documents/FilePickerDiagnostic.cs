using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diagnostic helper for the FilePickerUI panel.
///
/// PROBLEM SCENARIO:
///   The Console shows "Panel activated" + "Found N files" but the user can't
///   see the file-picker UI. Usually means the panel is enabled but:
///     • positioned outside the VR camera's view
///     • on a Canvas with mode World Space pointing the wrong way
///     • hidden behind another canvas with a higher sort order
///     • parented to an inactive parent (active in self, inactive in hierarchy)
///
/// USAGE:
///   1. Attach this component to the DocumentManager GameObject (or any
///      GameObject in the scene).
///   2. In the Inspector, drag the SAME FilePickerUI that DocumentManager uses
///      into the 'picker' field.
///   3. Hit Play. After clicking the Documents button, watch the Console —
///      a [FilePickerDiagnostic] line shows you everything about where the
///      panel is, whether it's actually rendered, and what canvas it sits on.
///
/// Read-only — never modifies the picker or anything else.
/// </summary>
public class FilePickerDiagnostic : MonoBehaviour
{
    [Tooltip("Drag the FilePickerUI here (the same one assigned to DocumentManager.filePickerUI).")]
    public FilePickerUI picker;

    [Tooltip("Print diagnostics every time the panel transitions from inactive→active.")]
    public bool logOnEachOpen = true;

    private bool _prevActive;

    void Awake()
    {
        if (picker == null && DocumentManager.Instance != null)
            picker = DocumentManager.Instance.filePickerUI;
    }

    void Update()
    {
        if (picker == null || picker.fileListPanel == null) return;
        bool nowActive = picker.fileListPanel.activeInHierarchy;
        if (logOnEachOpen && nowActive && !_prevActive)
        {
            Diagnose();
        }
        _prevActive = nowActive;
    }

    [ContextMenu("Diagnose Now")]
    public void Diagnose()
    {
        if (picker == null) { Debug.LogWarning("[FilePickerDiagnostic] picker not assigned."); return; }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== FilePickerUI Diagnostic ===");
        sb.AppendLine($"FilePickerUI GameObject: '{picker.name}', active={picker.gameObject.activeInHierarchy}");

        DescribeGO("fileListPanel", picker.fileListPanel, sb);
        DescribeGO("fileListContent", picker.fileListContent != null ? picker.fileListContent.gameObject : null, sb);
        DescribeGO("fileButtonPrefab", picker.fileButtonPrefab, sb);
        DescribeGO("closeButton",      picker.closeButton != null ? picker.closeButton.gameObject : null, sb);
        DescribeGO("titleText",        picker.titleText   != null ? picker.titleText.gameObject   : null, sb);

        if (picker.fileListPanel != null)
        {
            var canvas = picker.fileListPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                sb.AppendLine($"Canvas: '{canvas.name}'");
                sb.AppendLine($"  renderMode = {canvas.renderMode}");
                sb.AppendLine($"  sortingOrder = {canvas.sortingOrder}");
                sb.AppendLine($"  worldCamera = {(canvas.worldCamera != null ? canvas.worldCamera.name : "<null>")}");
                sb.AppendLine($"  enabled = {canvas.enabled}");
                sb.AppendLine($"  Canvas worldPos = {canvas.transform.position:F3}");
                sb.AppendLine($"  Canvas localScale = {canvas.transform.localScale:F4}");
            }
            else
            {
                sb.AppendLine("✗ NO Canvas in parent chain — the panel can't be rendered without one!");
            }

            // Look at the buttons that should exist after Found 2 files
            int childCount = picker.fileListContent != null ? picker.fileListContent.childCount : 0;
            sb.AppendLine($"fileListContent children (= spawned buttons): {childCount}");
        }

        Debug.Log(sb.ToString(), picker);
    }

    private static void DescribeGO(string label, GameObject go, System.Text.StringBuilder sb)
    {
        if (go == null) { sb.AppendLine($"{label} = <null>"); return; }
        sb.AppendLine($"{label}: '{go.name}'");
        sb.AppendLine($"  activeSelf      = {go.activeSelf}");
        sb.AppendLine($"  activeInHierarchy = {go.activeInHierarchy}");

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            sb.AppendLine($"  worldPos        = {rt.position:F3}");
            sb.AppendLine($"  anchoredPos     = {rt.anchoredPosition:F2}");
            sb.AppendLine($"  sizeDelta       = {rt.sizeDelta:F1}");
            sb.AppendLine($"  rect.size       = {rt.rect.size:F1}");
            sb.AppendLine($"  localScale      = {rt.localScale:F4}");
        }

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
            sb.AppendLine($"  CanvasGroup alpha = {cg.alpha:F2}, interactable = {cg.interactable}, blocksRaycasts = {cg.blocksRaycasts}");

        var img = go.GetComponent<Image>();
        if (img != null)
            sb.AppendLine($"  Image colour alpha = {img.color.a:F2}, enabled = {img.enabled}");
    }

}
