using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// סקריפט פשוט שמרכז את כל הקיצורי מקלדת לבדיקה ב-Editor.
/// מוצא אוטומטית את כל ה-OrganControllers וה-DraggableOrgans בסצנה.
///
/// קיצורים:
///   מקש 1 - פיצוץ/אסיפה של ה-Head (מוח)
///   מקש 2 - פיצוץ/אסיפה של ה-Lungs (ריאות)
///   מקש 3 - הזזה/החזרה של arm bones
///   מקש 4 - הזזה/החזרה של Shoulder muscles
///   מקש 0 - להחזיר הכל למקום
///
/// **בלי תלות בעכבר, בלי תלות ב-VR, פשוט עובד.**
/// </summary>
[DisallowMultipleComponent]
public class AnatomyHotkeys : MonoBehaviour
{
    [Header("── מקשי קיצור ──")]
    public KeyCode key1_HeadExplode = KeyCode.Alpha1;
    public KeyCode key2_LungsExplode = KeyCode.Alpha2;
    public KeyCode key3_ArmBonesMove = KeyCode.Alpha3;
    public KeyCode key4_ShouldersMove = KeyCode.Alpha4;
    public KeyCode key0_ResetAll = KeyCode.Alpha0;

    [Tooltip("מקש A במקלדת מפוצץ את האיבר שהעכבר/לייזר מצביע עליו (Head או Lungs)")]
    public KeyCode keyA_ExplodeHovered = KeyCode.A;

    [Tooltip("מקש R במקלדת מחזיר למקום את האיבר שהמצביע עליו (ראש/ריאות/ידיים/כתפיים)")]
    public KeyCode keyR_ReturnHovered = KeyCode.R;

    [Tooltip("מקש B במקלדת מגדיל/מקטין את האיבר שמצביעים עליו (פועל רק אחרי שהאיבר זז)")]
    public KeyCode keyB_ScaleHovered = KeyCode.B;

    [Header("── הגדרות הזזת איברים נגררים ──")]
    [Tooltip("מרחק שאיברים נגררים זזים בלחיצה")]
    public float dragOutDistance = 0.4f;
    [Tooltip("כיוון ההזזה ב-world space")]
    public Vector3 dragOutDirection = new Vector3(0.7f, 0.3f, -0.5f);

    OrganController head;
    OrganController lungs;
    DraggableOrgan armBones;
    DraggableOrgan shoulderMuscles;

    // לזכור אם איבר נגרר הוצא או לא
    // legacy flags - no longer used (ToggleDraggable checks actual position).
    // Kept here so old prefab serialization doesn't break.

    [Header("── דיבאג ──")]
    [Tooltip("מציג כפתורים על המסך - לבדיקה ב-Editor")]
    public bool showOnScreenButtons = true;

    void Start()
    {
        FindOrgans();
        string foundInfo = $"head={(head != null ? "✓" : "✗")}, lungs={(lungs != null ? "✓" : "✗")}, arms={(armBones != null ? "✓" : "✗")}, shoulders={(shoulderMuscles != null ? "✓" : "✗")}";
        Debug.Log($"[AnatomyHotkeys] Found: {foundInfo}");
        Debug.Log("[AnatomyHotkeys] לחץ במקלדת על 1/2/3/4/0 או לחץ על הכפתורים בפינה השמאלית-עליונה של המסך");
    }

    void OnGUI()
    {
        if (!showOnScreenButtons) return;

        const int W = 220, H = 40, PAD = 6;
        int y = 10;
        var style = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };

        GUI.color = Color.white;
        if (GUI.Button(new Rect(10, y, W, H), $"1: HEAD {(head != null ? "" : "(missing!)")}", style))
        {
            if (head != null) { Debug.Log("[Hotkeys] HEAD button clicked"); head.ToggleExplode(); }
        }
        y += H + PAD;

        if (GUI.Button(new Rect(10, y, W, H), $"2: LUNGS {(lungs != null ? "" : "(missing!)")}", style))
        {
            if (lungs != null) { Debug.Log("[Hotkeys] LUNGS button clicked"); lungs.ToggleExplode(); }
        }
        y += H + PAD;

        if (GUI.Button(new Rect(10, y, W, H), $"3: ARM BONES {(armBones != null ? "" : "(missing!)")}", style))
        {
            if (armBones != null) ToggleDraggable(armBones);
        }
        y += H + PAD;

        if (GUI.Button(new Rect(10, y, W, H), $"4: SHOULDERS {(shoulderMuscles != null ? "" : "(missing!)")}", style))
        {
            if (shoulderMuscles != null) ToggleDraggable(shoulderMuscles);
        }
        y += H + PAD;

        GUI.color = new Color(1f, 0.7f, 0.7f);
        if (GUI.Button(new Rect(10, y, W, H), "0: RESET ALL", style))
        {
            if (armBones != null) armBones.ReturnToOrigin();
            if (shoulderMuscles != null) shoulderMuscles.ReturnToOrigin();
            if (head != null && head.State == OrganController.OrganState.Exploded) head.ToggleExplode();
            if (lungs != null && lungs.State == OrganController.OrganState.Exploded) lungs.ToggleExplode();
        }
        GUI.color = Color.white;
    }

    void FindOrgans()
    {
        var allOrgans = FindObjectsByType<OrganController>(FindObjectsSortMode.None);
        foreach (var o in allOrgans)
        {
            string n = o.name.ToLower();
            if (n.Contains("head")) head = o;
            else if (n.Contains("lung")) lungs = o;
        }

        var allDrag = FindObjectsByType<DraggableOrgan>(FindObjectsSortMode.None);
        foreach (var d in allDrag)
        {
            string n = d.name.ToLower();
            if (n.Contains("arm")) armBones = d;
            else if (n.Contains("shoulder")) shoulderMuscles = d;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(key1_HeadExplode))
        {
            if (head != null)
            {
                Debug.Log("[AnatomyHotkeys] HEAD explode toggle");
                head.ToggleExplode();
            }
            else Debug.LogWarning("[AnatomyHotkeys] head לא נמצא");
        }

        if (Input.GetKeyDown(key2_LungsExplode))
        {
            if (lungs != null)
            {
                Debug.Log("[AnatomyHotkeys] LUNGS explode toggle");
                lungs.ToggleExplode();
            }
            else Debug.LogWarning("[AnatomyHotkeys] lungs לא נמצא");
        }

        if (Input.GetKeyDown(key3_ArmBonesMove))
        {
            if (armBones != null) ToggleDraggable(armBones);
            else Debug.LogWarning("[AnatomyHotkeys] arm bones לא נמצא");
        }

        if (Input.GetKeyDown(key4_ShouldersMove))
        {
            if (shoulderMuscles != null) ToggleDraggable(shoulderMuscles);
            else Debug.LogWarning("[AnatomyHotkeys] shoulder muscles לא נמצא");
        }

        if (Input.GetKeyDown(key0_ResetAll))
        {
            Debug.Log("[AnatomyHotkeys] RESET ALL");
            if (armBones != null) armBones.ReturnToOrigin();
            if (shoulderMuscles != null) shoulderMuscles.ReturnToOrigin();
            // OrganControllers - אם מתפוצצים, אסוף
            if (head != null && head.State == OrganController.OrganState.Exploded) head.ToggleExplode();
            if (lungs != null && lungs.State == OrganController.OrganState.Exploded) lungs.ToggleExplode();
        }

        // ── A על האיבר שהמצביע עליו → פיצוץ ──
        if (Input.GetKeyDown(keyA_ExplodeHovered))
        {
            var hoveredOrgan = FindHoveredOrgan();
            if (hoveredOrgan != null)
            {
                Debug.Log($"[AnatomyHotkeys] A → exploding hovered: {hoveredOrgan.name}");
                hoveredOrgan.ToggleExplode();
            }
            else
            {
                Debug.Log("[AnatomyHotkeys] A pressed but no organ is hovered (head/lungs)");
            }
        }

        // ── B על האיבר שהמצביע עליו → הגדלה/הקטנה ──
        if (Input.GetKeyDown(keyB_ScaleHovered))
        {
            var grab = FindHoveredGrabbable();
            if (grab != null)
            {
                Debug.Log($"[AnatomyHotkeys] B → toggle scale: {grab.name}");
                grab.ToggleScale();
            }
            else
            {
                Debug.Log("[AnatomyHotkeys] B pressed but no organ is hovered");
            }
        }

        // ── R על האיבר שהמצביע עליו → חזרה למקום ──
        if (Input.GetKeyDown(keyR_ReturnHovered))
        {
            // ראשית בדוק אם עומדים על head/lungs
            var hoveredOrgan = FindHoveredOrgan();
            if (hoveredOrgan != null)
            {
                Debug.Log($"[AnatomyHotkeys] R → returning hovered organ: {hoveredOrgan.name}");
                ReturnOrganToOrigin(hoveredOrgan);
                return;
            }
            // אחרת בדוק arm bones / shoulder muscles
            var hoveredDrag = FindHoveredDraggable();
            if (hoveredDrag != null)
            {
                Debug.Log($"[AnatomyHotkeys] R → returning hovered draggable: {hoveredDrag.name}");
                hoveredDrag.ReturnToOrigin();
                return;
            }
            Debug.Log("[AnatomyHotkeys] R pressed but no organ is hovered");
        }
    }

    void ReturnOrganToOrigin(OrganController oc)
    {
        // אם מתפוצץ - אסוף קודם
        if (oc.State == OrganController.OrganState.Exploded) oc.ToggleExplode();
        // וגם להחזיר למיקום אם זז
        var grab = oc.GetComponent<AnatomyGrabbable>();
        if (grab != null) grab.ReturnToOrigin();
    }

    /// <summary>
    /// משתמש ב-AnatomyGrabbable.IsHovered (אותו hover detection שעובד ויזואלית).
    /// יותר אמין מ-Physics.Raycast - כי כבר מטופל גם עבור VR וגם עבור עכבר ב-Editor.
    /// </summary>
    OrganController FindHoveredOrgan()
    {
        if (head != null)
        {
            var g = head.GetComponent<AnatomyGrabbable>();
            if (g != null && g.IsHovered) return head;
        }
        if (lungs != null)
        {
            var g = lungs.GetComponent<AnatomyGrabbable>();
            if (g != null && g.IsHovered) return lungs;
        }
        return null;
    }

    DraggableOrgan FindHoveredDraggable()
    {
        if (armBones != null)
        {
            var g = armBones.GetComponent<AnatomyGrabbable>();
            if (g != null && g.IsHovered) return armBones;
        }
        if (shoulderMuscles != null)
        {
            var g = shoulderMuscles.GetComponent<AnatomyGrabbable>();
            if (g != null && g.IsHovered) return shoulderMuscles;
        }
        return null;
    }

    /// <summary>
    /// מחזיר את ה-AnatomyGrabbable הראשון שמצביעים עליו (מכל איבר - ראש, ריאות, ידיים, כתפיים).
    /// </summary>
    AnatomyGrabbable FindHoveredGrabbable()
    {
        var oc = FindHoveredOrgan();
        if (oc != null) return oc.GetComponent<AnatomyGrabbable>();
        var dr = FindHoveredDraggable();
        if (dr != null) return dr.GetComponent<AnatomyGrabbable>();
        return null;
    }

    void ToggleDraggable(DraggableOrgan d)
    {
        // בודק לפי המיקום בפועל - לא לפי דגל פנימי.
        // כך שאם הזזת ידנית עם VR ואז לחצת על המקש - יחזיר במקום להזיז עוד.
        var grab = d.GetComponent<AnatomyGrabbable>();
        Transform parent = d.transform.parent;
        Vector3 originalLocal = grab != null ? grab.OriginalLocalPosition : d.transform.localPosition;
        Vector3 origWorld = parent != null ? parent.TransformPoint(originalLocal) : d.transform.position;
        float distance = Vector3.Distance(d.transform.position, origWorld);

        const float thresholdMeters = 0.02f; // 2 ס"מ
        if (distance > thresholdMeters)
        {
            // האיבר זז → החזר
            d.ReturnToOrigin();
            Debug.Log($"[AnatomyHotkeys] {d.name} RETURNED (was {distance:F3}m from origin)");
        }
        else
        {
            // האיבר במקום → הזז החוצה
            d.transform.position += dragOutDirection.normalized * dragOutDistance;
            Debug.Log($"[AnatomyHotkeys] {d.name} moved OUT");
        }
    }
}
