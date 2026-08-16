#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// כלי דיאגנוסטיקה - מדפיס ל-Console בדיוק מה קיים בסצנה
/// ומה חסר במערכת ה-Anatomy.
/// </summary>
public static class AnatomyDiagnostic
{
    const string ROOT_NAME = "ecorche_-_anatomy_study";

    [MenuItem("MedMeet Tools/Diagnose Anatomy Setup")]
    public static void Diagnose()
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine("ANATOMY DIAGNOSTIC REPORT");
        sb.AppendLine("═══════════════════════════════════════════\n");

        // 1. חפש את השורש
        var root = GameObject.Find(ROOT_NAME);
        if (root == null)
        {
            sb.AppendLine($"❌ לא נמצא GameObject בשם '{ROOT_NAME}'");
            sb.AppendLine("\nכל ה-GameObjects ברמה העליונה של הסצנה:");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots) sb.AppendLine($"   • {r.name}");
            Debug.Log(sb.ToString());
            return;
        }
        sb.AppendLine($"✅ נמצא: '{ROOT_NAME}'");
        sb.AppendLine($"   Position: {root.transform.position}");
        sb.AppendLine($"   Scale: {root.transform.localScale}");

        // BodyManager?
        sb.AppendLine($"   BodyManager: {(root.GetComponent<BodyManager>() != null ? "✅ קיים" : "❌ חסר")}");
        sb.AppendLine($"   AnatomyHotkeys: {(root.GetComponent<AnatomyHotkeys>() != null ? "✅ קיים" : "❌ חסר")}");

        sb.AppendLine($"\n📋 ילדי השורש ({root.transform.childCount}):");
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var c = root.transform.GetChild(i);
            sb.AppendLine($"   • {c.name}  ({c.childCount} ילדים)");
        }

        sb.AppendLine("\n───────────────────────────────────────────");
        sb.AppendLine("בדיקה לכל איבר צפוי:");
        sb.AppendLine("───────────────────────────────────────────\n");

        CheckOrgan(sb, root, "head", true);
        CheckOrgan(sb, root, "Lungs", true);
        CheckOrgan(sb, root, "lungs", true);
        CheckDraggable(sb, root, "arm bones");
        CheckDraggable(sb, root, "Shoulder muscles");
        CheckDraggable(sb, root, "shoulder muscles");

        sb.AppendLine("\n═══════════════════════════════════════════");
        sb.AppendLine("מה לעשות:");
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine("אם משהו חסר ❌ → הרץ 'MedMeet Tools → Setup Anatomy Explosion'");
        sb.AppendLine("אם יש ✅ אבל לא עובד → צרף screenshot של הדיאגנוסטיקה הזו");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Anatomy Diagnostic",
            "הדוח הודפס ל-Console.\nלחץ על Console כדי לראות אותו.", "אישור");
    }

    static void CheckOrgan(StringBuilder sb, GameObject root, string name, bool checkRecursive)
    {
        var child = FindChild(root.transform, name, checkRecursive);
        if (child == null)
        {
            sb.AppendLine($"❌ '{name}' לא נמצא בכלל");
            return;
        }
        sb.AppendLine($"✅ '{name}' נמצא (תחת: {child.parent?.name ?? "root"})");
        sb.AppendLine($"   ילדים: {child.childCount}");

        var oc = child.GetComponent<OrganController>();
        if (oc == null)
        {
            sb.AppendLine($"   ❌ OrganController חסר!");
        }
        else
        {
            sb.AppendLine($"   ✅ OrganController קיים. Groups: {oc.Groups?.Count ?? 0}");
            if (oc.Groups != null)
            {
                for (int i = 0; i < oc.Groups.Count; i++)
                {
                    var g = oc.Groups[i];
                    sb.AppendLine($"      [{i}] {g.GroupName} - {g.Parts.Count} parts");
                }
            }
        }

        var rb = child.GetComponent<Rigidbody>();
        sb.AppendLine($"   Rigidbody: {(rb != null ? "✅" : "❌")}");
        var col = child.GetComponent<Collider>();
        sb.AppendLine($"   Collider: {(col != null ? "✅ " + col.GetType().Name : "❌ חסר!")}");

        // XRSimpleInteractable (replaces XRGrabInteractable)
        var simpleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
        if (simpleType != null)
        {
            var simple = child.GetComponent(simpleType);
            sb.AppendLine($"   XRSimpleInteractable: {(simple != null ? "✅" : "❌ חסר!")}");
        }
        var anatomyGrab = child.GetComponent<AnatomyGrabbable>();
        sb.AppendLine($"   AnatomyGrabbable: {(anatomyGrab != null ? "✅" : "❌ חסר!")}");

        // Warn if old XRGrabInteractable still exists (should have been removed)
        var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
        if (grabType != null)
        {
            var grab = child.GetComponent(grabType);
            if (grab != null)
            {
                sb.AppendLine($"   ⚠️ XRGrabInteractable עדיין קיים - היה אמור להיות מוסר. הרץ Refresh Anatomy Grab Components");
            }
        }
        sb.AppendLine();
    }

    static void CheckDraggable(StringBuilder sb, GameObject root, string name)
    {
        var child = FindChild(root.transform, name, true);
        if (child == null)
        {
            sb.AppendLine($"❌ '{name}' לא נמצא");
            return;
        }
        sb.AppendLine($"✅ '{name}' נמצא");
        var d = child.GetComponent<DraggableOrgan>();
        sb.AppendLine($"   DraggableOrgan: {(d != null ? "✅" : "❌ חסר!")}");
        if (d != null) sb.AppendLine($"      OrganName: '{d.OrganName}'");

        var simpleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
        if (simpleType != null)
        {
            var simple = child.GetComponent(simpleType);
            sb.AppendLine($"   XRSimpleInteractable: {(simple != null ? "✅" : "❌ חסר!")}");
        }
        var ag = child.GetComponent<AnatomyGrabbable>();
        sb.AppendLine($"   AnatomyGrabbable: {(ag != null ? "✅" : "❌ חסר!")}");
        sb.AppendLine();
    }

    static Transform FindChild(Transform parent, string name, bool recursive)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (string.Equals(c.name, name, System.StringComparison.OrdinalIgnoreCase)) return c;
        }
        if (recursive)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                var sub = FindChild(c, name, true);
                if (sub != null) return sub;
            }
        }
        return null;
    }
}
#endif
