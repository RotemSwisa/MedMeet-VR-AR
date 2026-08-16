using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// פקטורי שיוצר תווית 3D עם:
///   - TextMeshPro טקסט קצר (PartName)
///   - LineRenderer leader line מהחלק אל בסיס התווית
///   - LabelBillboard כדי שתפנה למצלמה
///
/// משמש את OrganController בזמן Awake.
/// </summary>
public static class AnatomyLabelFactory
{
    /// <summary>
    /// יצור תווית עבור AnatomyGroup (תווית אחת לקבוצה שלמה)
    /// </summary>
    public static GameObject CreateGroupLabel(AnatomyGroup group, Transform parent, Color textColor, Color bgColor)
    {
        if (group == null) return null;

        var labelGO = new GameObject("GroupLabel_" + (string.IsNullOrEmpty(group.GroupName) ? "Unnamed" : group.GroupName));
        labelGO.transform.SetParent(parent, true);
        labelGO.transform.localRotation = Quaternion.identity;

        labelGO.AddComponent<LabelBillboard>();

        var canvasGO = new GameObject("LabelCanvas");
        canvasGO.transform.SetParent(labelGO.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(480, 120);
        rt.localScale = Vector3.one * 0.0009f;
        rt.localPosition = Vector3.zero;

        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = string.IsNullOrEmpty(group.GroupName) ? "—" : group.GroupName;
        tmp.color = textColor;
        tmp.fontSize = 56;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var txtRT = tmp.rectTransform;
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(15, 8); txtRT.offsetMax = new Vector2(-15, -8);

        // שמור הפניה לטקסט בקבוצה - מאפשר עדכון השם בריצה
        group.labelText = tmp;

        return labelGO;
    }

    public static GameObject CreateLabel(AnatomyPart part, Color textColor, Color bgColor, float offsetMeters)
    {
        // יצור GameObject תווית (ילד של החלק)
        var labelGO = new GameObject(part.gameObject.name + "_Label");
        labelGO.transform.SetParent(part.transform, false);
        labelGO.transform.localPosition = Vector3.up * offsetMeters * 2f + Vector3.right * offsetMeters;
        labelGO.transform.localRotation = Quaternion.identity;

        // הוסף billboard
        labelGO.AddComponent<LabelBillboard>();

        // יצור Canvas World Space
        var canvasGO = new GameObject("LabelCanvas");
        canvasGO.transform.SetParent(labelGO.transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 80);
        rt.localScale = Vector3.one * 0.0005f; // 0.5mm per pixel - תווית של ~15cm
        rt.localPosition = Vector3.zero;

        // רקע
        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // טקסט
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(canvasGO.transform, false);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = part.PartName;
        tmp.color = textColor;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var txtRT = tmp.rectTransform;
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 5);
        txtRT.offsetMax = new Vector2(-10, -5);

        // Leader Line - LineRenderer מהחלק לבסיס התווית
        var lineGO = new GameObject("LeaderLine");
        lineGO.transform.SetParent(labelGO.transform, false);
        var line = lineGO.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.startWidth = 0.002f;
        line.endWidth = 0.002f;
        line.positionCount = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(textColor.r, textColor.g, textColor.b, 0.6f);
        line.endColor = new Color(textColor.r, textColor.g, textColor.b, 0.3f);

        // קומפוננטה שמעדכנת את הקו כל פריים
        var updater = lineGO.AddComponent<LeaderLineUpdater>();
        updater.startTarget = part.transform;
        updater.endTarget = labelGO.transform;
        updater.line = line;

        return labelGO;
    }
}

/// <summary>
/// מעדכן קו מנחה כל פריים (מנקודה A לנקודה B).
/// </summary>
public class LeaderLineUpdater : MonoBehaviour
{
    public Transform startTarget;
    public Transform endTarget;
    public LineRenderer line;

    void LateUpdate()
    {
        if (line == null || startTarget == null || endTarget == null) return;
        line.SetPosition(0, startTarget.position);
        line.SetPosition(1, endTarget.position);
    }
}
