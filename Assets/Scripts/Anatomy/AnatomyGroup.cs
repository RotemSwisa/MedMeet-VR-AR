using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// קבוצה של תת-חלקים אנטומיים שזזים ביחד בפיצוץ.
/// לדוגמה במוח: כל ה-Skeletal_* יחד = קבוצה אחת.
/// </summary>
[System.Serializable]
public class AnatomyGroup
{
    [Tooltip("שם הקבוצה (יוצג בתווית)")]
    public string GroupName;

    [Tooltip("הסבר ארוך לפאנל המורחב")]
    [TextArea(2, 6)]
    public string Description;

    [Tooltip("רשימת ה-Transforms שמרכיבים את הקבוצה")]
    public List<Transform> Parts = new List<Transform>();

    [Tooltip("אם לא Vector3.zero - כיוון הפיצוץ ידני (במרחב WORLD). אחרת מחושב אוטומטית")]
    public Vector3 DirectionOverride = Vector3.zero;

    [Tooltip("מכפיל למרחק הפיצוץ של הקבוצה הספציפית הזו (1 = רגיל, 0.5 = חצי, 2 = כפול)")]
    public float distanceMultiplier = 1f;

    // ─── Runtime (מתמלא ע"י OrganController) ───
    [HideInInspector] public List<Vector3> originalLocalPositions = new List<Vector3>();
    [HideInInspector] public GameObject labelGO;
    // הפניה לרכיב הטקסט - מאפשר לעדכן את שם התווית אם משנים את GroupName בריצה
    [System.NonSerialized] public TMPro.TextMeshProUGUI labelText;

    public void CacheOriginalPositions()
    {
        originalLocalPositions.Clear();
        foreach (var p in Parts)
        {
            if (p == null) { originalLocalPositions.Add(Vector3.zero); continue; }
            originalLocalPositions.Add(p.localPosition);
        }
    }

    public Vector3 ComputeWorldCenter()
    {
        var b = ComputeWorldBounds(out bool found);
        return found ? b.center : FallbackCenter();
    }

    /// <summary>
    /// מחזיר את ה-bounds המאוחד של כל הרינדורים בקבוצה.
    /// `found = false` אם אין רינדורים בכלל.
    /// </summary>
    public Bounds ComputeWorldBounds(out bool found)
    {
        found = false;
        Bounds b = new Bounds();
        foreach (var p in Parts)
        {
            if (p == null) continue;
            var rends = p.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (!found) { b = r.bounds; found = true; }
                else b.Encapsulate(r.bounds);
            }
        }
        return b;
    }

    Vector3 FallbackCenter()
    {
        Vector3 sum = Vector3.zero;
        int n = 0;
        foreach (var p in Parts)
        {
            if (p == null) continue;
            sum += p.position; n++;
        }
        return n > 0 ? sum / n : Vector3.zero;
    }
}
