using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MedMeet Tools → Audit Scene Lighting
///
/// Lists every Light + ReflectionProbe in the open scene with its intensity,
/// type and parent path so you can quickly spot the source of a too-bright
/// room. Read-only — does NOT change anything in the scene.
///
/// Companion menu items let you:
///   • Disable every Light above a threshold (default 5.0)
///   • Disable the ambient skybox / environment lighting (if it's blowing out)
/// Each destructive action is undoable via Edit → Undo.
/// </summary>
public static class LightAuditTool
{
    private const float DefaultThreshold = 5.0f;

    [MenuItem("MedMeet Tools/Audit Scene Lighting")]
    public static void Audit()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var probes = Object.FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Scene Lighting Audit ===");
        sb.AppendLine($"Lights:            {lights.Length}");
        sb.AppendLine($"Reflection probes: {probes.Length}");
        sb.AppendLine();
        sb.AppendLine("AMBIENT / ENVIRONMENT:");
        sb.AppendLine($"  ambientMode      = {RenderSettings.ambientMode}");
        sb.AppendLine($"  ambientIntensity = {RenderSettings.ambientIntensity}");
        sb.AppendLine($"  reflectionIntensity = {RenderSettings.reflectionIntensity}");
        sb.AppendLine($"  skybox           = {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "<none>")}");
        sb.AppendLine();

        if (lights.Length == 0) { sb.AppendLine("(no Light components in scene)"); }
        else
        {
            // Sort hottest first so the culprit is at the top
            var sorted = lights.OrderByDescending(l => l.intensity).ToList();
            sb.AppendLine("LIGHTS (sorted by intensity, hottest first):");
            foreach (var l in sorted)
            {
                string path = GetTransformPath(l.transform);
                string flag = l.intensity >= DefaultThreshold ? "  ⚠️ BRIGHT" : "";
                string scene = l.gameObject.scene.IsValid() ? l.gameObject.scene.name : "(prefab)";
                sb.AppendLine($"  [{scene}] {path}");
                sb.AppendLine($"      type={l.type}  intensity={l.intensity:F2}  range={l.range:F1}  enabled={l.enabled}  active={l.gameObject.activeInHierarchy}{flag}");
            }
        }

        if (probes.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("REFLECTION PROBES:");
            foreach (var p in probes)
            {
                sb.AppendLine($"  {GetTransformPath(p.transform)}");
                sb.AppendLine($"      intensity={p.intensity:F2}  enabled={p.enabled}  active={p.gameObject.activeInHierarchy}");
            }
        }

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Scene Lighting Audit",
            sb.ToString() + "\n\nSee the Console for the full list. The hottest light is at the TOP " +
            "of the LIGHTS section — usually that's the culprit when a room looks blown-out.",
            "OK");
    }

    [MenuItem("MedMeet Tools/⚡ Restore All Lights (re-enable everything)")]
    public static void RestoreAllLights()
    {
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int restored = 0;
        foreach (var l in lights)
        {
            if (!l.enabled)
            {
                Undo.RecordObject(l, "Restore light");
                l.enabled = true;
                EditorUtility.SetDirty(l);
                restored++;
            }
            // Also re-activate the GameObject in case something disabled it entirely
            if (!l.gameObject.activeSelf)
            {
                Undo.RecordObject(l.gameObject, "Restore light GO");
                l.gameObject.SetActive(true);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Lights restored",
            $"Re-enabled {restored} Light component(s).\n\nPress Ctrl+S to save.", "OK");
        Debug.Log($"[LightAudit] Restored {restored} lights");
    }

    [MenuItem("MedMeet Tools/Dim Lights Above 5.0 Intensity")]
    public static void DimBrightLights()
    {
        if (!EditorUtility.DisplayDialog("Dim Bright Lights",
            $"This will disable every Light whose intensity is ≥ {DefaultThreshold} in the open scene.\n\n" +
            "Undoable via Edit → Undo (Ctrl+Z). Continue?", "Disable", "Cancel"))
            return;

        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int disabled = 0;
        foreach (var l in lights)
        {
            if (l.intensity >= DefaultThreshold && l.enabled)
            {
                Undo.RecordObject(l, "Disable bright light");
                l.enabled = false;
                EditorUtility.SetDirty(l);
                disabled++;
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Done",
            $"Disabled {disabled} light(s) with intensity ≥ {DefaultThreshold}.\n\nPress Ctrl+S to save the scene.",
            "OK");
    }

    [MenuItem("MedMeet Tools/Soften Ambient Lighting")]
    public static void SoftenAmbient()
    {
        float oldAmb  = RenderSettings.ambientIntensity;
        float oldRefl = RenderSettings.reflectionIntensity;

        if (!EditorUtility.DisplayDialog("Soften Ambient",
            $"Current values:\n" +
            $"  Ambient intensity    = {oldAmb:F2}\n" +
            $"  Reflection intensity = {oldRefl:F2}\n\n" +
            "Set both to 0.4 (a calm baseline)?\n\n" +
            "Note: RenderSettings is a static class — this change is NOT undoable via Ctrl+Z. " +
            "If you want to revert, manually set the values in Window → Rendering → Lighting → Environment.",
            "Soften to 0.4", "Cancel"))
            return;

        RenderSettings.ambientIntensity    = 0.4f;
        RenderSettings.reflectionIntensity = 0.4f;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"[LightAudit] Ambient intensity {oldAmb:F2} → 0.40, reflection {oldRefl:F2} → 0.40");
        EditorUtility.DisplayDialog("Done",
            $"Ambient {oldAmb:F2} → 0.40\nReflection {oldRefl:F2} → 0.40\n\nSave the scene with Ctrl+S.",
            "OK");
    }

    private static string GetTransformPath(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        int hops = 0;
        while (cur != null && hops++ < 20)
        {
            parts.Insert(0, cur.name);
            cur = cur.parent;
        }
        return string.Join("/", parts);
    }
}
