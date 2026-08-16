using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MedMeet Tools → Normalise Avatar IK (Male_avatar + Femaledoctor)
///
/// Finds the two avatars by name and applies a consistent, safe baseline of
/// VRIKController settings so both hands track equally on both characters.
/// Does NOT change the prefab assets — only the scene-instance values.
///
/// Settings applied:
///   • leftHandWeight  = 1, rightHandWeight = 1, forceWeightsToOne = true
///   • swapHandControllers = false (caller can re-enable if mirrored)
///   • leftHandPositionOffset  / rightHandPositionOffset  = (0, 0, 0)
///   • leftHandRotationOffset  / rightHandRotationOffset  = (0, 0, 0)
///   • elbowHintWeight = 1, elbowBackOffset = 0.55, elbowDownOffset = 0.15
///   • lockHeadStatic = true  (matches earlier fix)
///
/// After running, do small Inspector tweaks (e.g. rotation offsets to
/// straighten the male's palm) instead of fighting prefab-level state.
/// </summary>
public static class AvatarIKNormaliseTool
{
    private static readonly string[] AvatarNames = { "Male_avatar", "Femaledoctor" };

    [MenuItem("MedMeet Tools/Normalise Avatar IK (Male_avatar + Femaledoctor)")]
    public static void Run()
    {
        var found = new List<VRIKController>();

        // Look at every VRIKController in the scene (active + inactive) so we
        // don't miss the female if her avatar is currently disabled.
        var allControllers = Object.FindObjectsByType<VRIKController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var c in allControllers)
        {
            // Match by walking up the parent chain so a VRIKController nested
            // a few levels deep is still recognised by its avatar root name.
            Transform t = c.transform;
            int hops = 0;
            while (t != null && hops++ < 10)
            {
                foreach (var name in AvatarNames)
                {
                    if (t.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found.Add(c);
                        goto NextController;
                    }
                }
                t = t.parent;
            }
            NextController:;
        }

        if (found.Count == 0)
        {
            EditorUtility.DisplayDialog("Normalise Avatar IK",
                "Couldn't find a VRIKController under any GameObject named " +
                "'Male_avatar' or 'Femaledoctor'.\n\nMake sure the avatar prefabs " +
                "are instantiated in the open scene before running this tool.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Normalise Avatar IK");
        int group = Undo.GetCurrentGroup();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Normalise Avatar IK ===");
        report.AppendLine($"Affected controllers: {found.Count}");

        foreach (var c in found)
        {
            Undo.RecordObject(c, "Normalise IK");

            c.leftHandWeight        = 1f;
            c.rightHandWeight       = 1f;
            c.forceWeightsToOne     = true;
            c.swapHandControllers   = false;
            c.leftHandPositionOffset  = Vector3.zero;
            c.rightHandPositionOffset = Vector3.zero;
            c.leftHandRotationOffset  = Vector3.zero;
            c.rightHandRotationOffset = Vector3.zero;
            c.elbowHintWeight  = 1f;
            c.elbowBackOffset  = 0.55f;
            c.elbowDownOffset  = 0.15f;
            c.lockHeadStatic   = true;

            EditorUtility.SetDirty(c);
            report.AppendLine($"  ✓ {GetPath(c.transform)}");
        }

        Undo.CollapseUndoOperations(group);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log(report);
        EditorUtility.DisplayDialog("Normalise Avatar IK",
            report + "\n\nPress Play. Both avatars should now use identical IK.\n" +
            "If a palm is rotated, tweak its 'Right Hand Rotation Offset' in the Inspector " +
            "(typical fixes: 0,0,90 / 0,0,-90 / 0,90,0) and use the context-menu " +
            "Mirror Right→Left to copy to the other hand.\n\n" +
            "Press Ctrl+S to save.",
            "OK");
    }

    private static string GetPath(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        int hops = 0;
        while (cur != null && hops++ < 12)
        {
            parts.Insert(0, cur.name);
            cur = cur.parent;
        }
        return string.Join("/", parts);
    }
}
