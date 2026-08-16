using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MedMeet Tools → Clean Scene
/// Finds and removes simulation objects that don't belong in the VR meeting room:
/// TongueDepressor, Flashlight_01, Male_avatar, Canvas (1) and related items.
/// </summary>
public static class CleanSceneTool
{
    // Exact names of objects to remove (case-sensitive)
    private static readonly string[] ExactNames = new[]
    {
        "Male_avatar",
        "Flashlight_01",
        "Canvas (1)",
    };

    // Substrings — any object whose name *contains* one of these is flagged.
    // Keep these tight to avoid removing legitimate scene objects.
    private static readonly string[] ContainsNames = new[]
    {
        "TongueDepressor",
        "Tongue Depressor",
    };

    [MenuItem("MedMeet Tools/Clean Scene — Remove Simulation Objects")]
    public static void CleanScene()
    {
        var toDelete = new List<GameObject>();
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var go in allObjects)
        {
            if (IsSimulationObject(go.name))
                toDelete.Add(go);
        }

        // De-duplicate: if a child is already covered by a parent in the list, skip it
        var pruned = new List<GameObject>();
        foreach (var go in toDelete)
        {
            bool parentAlsoInList = false;
            Transform p = go.transform.parent;
            while (p != null)
            {
                if (toDelete.Contains(p.gameObject)) { parentAlsoInList = true; break; }
                p = p.parent;
            }
            if (!parentAlsoInList) pruned.Add(go);
        }

        if (pruned.Count == 0)
        {
            EditorUtility.DisplayDialog("Clean Scene",
                "No simulation objects found in the current scene.\n\n" +
                "Make sure the VR meeting scene is open.", "OK");
            return;
        }

        string list = "";
        foreach (var go in pruned)
            list += $"  • {GetFullPath(go)}\n";

        bool confirm = EditorUtility.DisplayDialog(
            "Remove Simulation Objects",
            $"Found {pruned.Count} object(s) that don't belong in the VR meeting room:\n\n{list}\n" +
            "This action is undoable (Ctrl+Z).\nProceed?",
            "Remove All", "Cancel");

        if (!confirm) return;

        Undo.SetCurrentGroupName("Clean Scene — Remove Simulation Objects");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (var go in pruned)
        {
            Debug.Log($"[CleanScene] Removing: {GetFullPath(go)}");
            Undo.DestroyObjectImmediate(go);
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog("Clean Scene",
            $"✅ Removed {pruned.Count} simulation object(s).\n\n" +
            "Save the scene now (Ctrl + S).", "OK");
    }

    private static bool IsSimulationObject(string name)
    {
        foreach (var exact in ExactNames)
            if (name == exact) return true;

        foreach (var sub in ContainsNames)
            if (name.Contains(sub)) return true;

        return false;
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
