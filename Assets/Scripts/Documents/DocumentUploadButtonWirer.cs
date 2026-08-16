using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime fix for the "DocumentUplod" button whose Inspector OnClick target
/// is null (because the Canvas was turned into a prefab and Unity stripped
/// the scene-object reference). This script:
///
///   1. Looks for a Button named "DocumentUplod" (or close variants) under
///      the GameObject it's attached to.
///   2. On Start, clears any non-persistent listeners on that button and
///      adds a single one that calls DocumentManager.Instance.OpenFilePicker().
///
/// SAFE TO USE because:
///   • Only touches the one button it finds by name.
///   • Does NOT modify the prefab asset, the DocumentManager, or anything
///     else in the project.
///   • If the button isn't found (e.g. on a scene that doesn't have the
///     menu) it logs a single info message and exits.
///
/// USAGE:
///   Attach this component to ANY GameObject that's a parent of the menu
///   canvas — the Canvas itself works, or a manager GameObject, or even
///   the DocumentManager. On Start it walks its own children looking for
///   the button. Idempotent — running multiple instances is harmless.
/// </summary>
public class DocumentUploadButtonWirer : MonoBehaviour
{
    [Tooltip("Name of the button to wire (case-insensitive substring match). " +
             "Defaults to the typo'd name that's currently in the project prefab.")]
    public string buttonNameContains = "DocumentUplod";

    [Tooltip("Print a [Wirer] line to the Console when wiring succeeds.")]
    public bool logSuccess = true;

    void Start()
    {
        Button button = FindButtonInChildren(transform, buttonNameContains);
        if (button == null)
        {
            Debug.Log($"[DocumentUploadButtonWirer] No button named '*{buttonNameContains}*' " +
                      "found under this GameObject. Move the component to a parent of the menu canvas " +
                      "or update buttonNameContains in the Inspector.");
            return;
        }

        // Replace runtime listeners. We leave persistent (Inspector-wired)
        // listeners alone because they're the ones we're working around —
        // Unity will silently no-op them because their target is null, which
        // is exactly the bug we're patching.
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        if (logSuccess)
            Debug.Log($"[DocumentUploadButtonWirer] ✓ Wired '{button.name}' → DocumentManager.OpenFilePicker()");
    }

    /// <summary>Actual click handler — re-resolves the singleton every press so
    /// scene reloads don't leave a stale reference.</summary>
    private void OnClick()
    {
        var dm = DocumentManager.Instance;
        if (dm == null)
        {
            Debug.LogError("[DocumentUploadButtonWirer] DocumentManager.Instance is null — " +
                           "make sure a DocumentManager GameObject exists in the scene.");
            return;
        }
        dm.OpenFilePicker();
    }

    /// <summary>Recursive search through inactive + active children for the first
    /// Button whose GameObject name contains the configured substring.</summary>
    private static Button FindButtonInChildren(Transform root, string nameSubstring)
    {
        if (root == null || string.IsNullOrEmpty(nameSubstring)) return null;
        string needle = nameSubstring.ToLowerInvariant();

        var buttons = root.GetComponentsInChildren<Button>(includeInactive: true);
        foreach (var b in buttons)
        {
            if (b == null) continue;
            if (b.name.ToLowerInvariant().Contains(needle)) return b;
        }
        return null;
    }
}
