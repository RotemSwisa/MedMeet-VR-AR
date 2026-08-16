using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// קומפוננטה על כל תת-חלק אנטומי (לדוגמה Female_Muscular_Masseter).
/// שומרת מיקום מקורי לפני פיצוץ + מפיקה שם ידידותי אוטומטי.
/// </summary>
[DisallowMultipleComponent]
public class AnatomyPart : MonoBehaviour
{
    [Header("── תוכן (אם ריק - יחושב אוטומטית מהשם) ──")]
    [Tooltip("שם קצר לתווית. אם ריק - יחושב משם ה-GameObject (Female_Muscular_Masseter -> Masseter)")]
    public string PartName;

    [Tooltip("הסבר ארוך לפאנל המורחב כשמצביעים. תמלא בעצמך כשתרצה")]
    [TextArea(3, 8)]
    public string ExtendedDescription;

    [Tooltip("אם לא Vector3.zero - יחליף את כיוון הפיצוץ הרדיאלי. השאר 0,0,0 כדי שהמערכת תחשב אוטומטית")]
    public Vector3 ExplodedOffsetOverride = Vector3.zero;

    // ─── Runtime state (מתמלא אוטומטית) ───
    [HideInInspector] public Vector3 originalLocalPosition;
    [HideInInspector] public Quaternion originalLocalRotation;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        if (string.IsNullOrEmpty(PartName))
        {
            PartName = CleanName(gameObject.name);
        }
    }

    /// <summary>
    /// הופך שם GameObject מבולגן לשם ידידותי לתצוגה.
    /// "Female_Muscular_Masseter" -> "Masseter"
    /// "Female_Nervous_Cerebellum_DONT_SUBDIVIDE" -> "Cerebellum"
    /// "Linkerlong" -> "Left Lung" (תרגום)
    /// </summary>
    public static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string result = raw;

        // הסר prefixes אנטומיים נפוצים
        string[] prefixes = {
            "Female_", "Male_",
            "Muscular_", "Skeletal_", "Nervous_",
            "Circulatory_", "Digestive_", "Lymphatic_"
        };
        bool keepRemoving = true;
        while (keepRemoving)
        {
            keepRemoving = false;
            foreach (var p in prefixes)
            {
                if (result.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(p.Length);
                    keepRemoving = true;
                    break;
                }
            }
        }

        // הסר הערות של האמן כמו _DONT_SUBDIVIDE
        result = result.Replace("_DONT_SUBDIVIDE", "");
        result = result.Replace("DONT_SUBDIVIDE", "");

        // הסר ספרות בסוף שם (bronchi1 -> bronchi, Cranium001 -> Cranium)
        result = Regex.Replace(result, @"\d+$", "");

        // החלף underscores ברווחים
        result = result.Replace("_", " ").Trim();

        // תרגומים ידועים (הולנדית/גרמנית -> אנגלית)
        var translations = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Linkerlong",                 "Left Lung" },
            { "Linker long",                "Left Lung" },
            { "Rechterlong",                "Right Lung" },
            { "Rechter long",               "Right Lung" },
            { "Trachea kraakbeen",          "Trachea Cartilage" },
            { "Tussen hyoid en thyroid",    "Hyoid-Thyroid Membrane" },
            { "Hyoid bone",                 "Hyoid Bone" },
            { "Thyroid gland",              "Thyroid Gland" },
            { "bronchi",                    "Bronchi" }
        };
        if (translations.TryGetValue(result, out var translated))
        {
            result = translated;
        }

        // Title case - אות גדולה בתחילת כל מילה
        if (result.Length > 0)
        {
            var parts = result.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : "");
                }
            }
            result = string.Join(" ", parts);
        }

        return result;
    }
}
