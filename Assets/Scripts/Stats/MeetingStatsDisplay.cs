using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class MeetingStatsDisplay : MonoBehaviour
{
    [Header("Pie Chart Visuals")]
    public GameObject pieSegmentPrefab;
    public Transform pieChartContainer;

    [Tooltip("גודל הטקסט על העוגה")]
    [Range(20, 150)] public float pieFontSize = 55f;

    [Tooltip("מרחק הטקסט ממרכז העוגה - שחק עם זה כדי להוציא את הטקסט החוצה")]
    [Range(0, 300)] public float textRadius = 130f; // שליטה על המרחק

    [Tooltip("תיקון ידני לסיבוב הטקסט אם הוא הפוך (נסה 180)")]
    [Range(0, 180)] public float textRotationY = 0f;

    [Tooltip("תיקון זווית הטקסט")]
    [Range(-180, 180)] public float textRotationZ = 0f;

    [Header("Legend References")]
    public GameObject statRowPrefab;
    public Transform legendContainer;

    [Header("Main Timer")]
    public TMP_Text sessionTimerText;

    [Header("Settings")]
    public float refreshRate = 0.5f;

    // --- שינוי: מבנה נתונים להגדרת צבעים לפי תפקיד ---
    [System.Serializable]
    public struct RoleColorDefinition
    {
        public string roleName; // השם המדויק של התפקיד
        public Color color;     // הצבע שאת רוצה עבורו
    }

    [Tooltip("כאן את יכולה להגדיר צבע ספציפי לכל תפקיד")]
    public List<RoleColorDefinition> specificRoleColors;

    [Tooltip("מאגר צבעים לשימוש במקרה של תפקיד שלא הוגדר לו צבע ספציפי")]
    public Color[] fallbackColors;
    // ---------------------------------------------------

    private float timer = 0f;

    void Update()
    {
        if (MeetingAuditor.Instance != null)
        {
            // עדכון השעון עם הזמן המסונכרן
            float sessionTime = MeetingAuditor.Instance.GetTotalSessionTime();
            System.TimeSpan t = System.TimeSpan.FromSeconds(sessionTime);
            if (sessionTimerText)
            {
                sessionTimerText.text = string.Format("TOTAL TIME\n{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }

        timer += Time.deltaTime;
        if (timer >= refreshRate)
        {
            RefreshVisualsMixed();
            timer = 0f;
        }
    }

    void RefreshVisualsMixed()
    {
        if (MeetingAuditor.Instance == null) return;

        // ניקוי אלמנטים ישנים
        foreach (Transform child in pieChartContainer) Destroy(child.gameObject);
        foreach (Transform child in legendContainer) Destroy(child.gameObject);

        var allStats = MeetingAuditor.Instance.GetAllPlayerStats();

        // --- חישוב נתונים לעוגה (לפי תפקיד) ---
        Dictionary<string, float> speechByRole = new Dictionary<string, float>();
        float totalSessionSpeech = 0f;

        foreach (var stat in allStats.Values)
        {
            string role = string.IsNullOrEmpty(stat.role) ? "Guest" : stat.role;
            if (!speechByRole.ContainsKey(role)) speechByRole.Add(role, 0f);

            speechByRole[role] += stat.totalSpeechTime;
            totalSessionSpeech += stat.totalSpeechTime;
        }

        if (totalSessionSpeech == 0) totalSessionSpeech = 1f;

        // --- ציור העוגה ---
        float currentRotation = 0f;

        foreach (var roleEntry in speechByRole.OrderBy(x => x.Key))
        {
            string roleName = roleEntry.Key;
            float roleTime = roleEntry.Value;

            if (roleTime <= 0) continue;

            float fillAmount = roleTime / totalSessionSpeech;
            float percentage = fillAmount * 100f;
            Color roleColor = GetColorForRole(roleName);

            // יצירת הפלח
            GameObject newSeg = Instantiate(pieSegmentPrefab, pieChartContainer);
            PieSegment segmentScript = newSeg.GetComponent<PieSegment>();

            if (segmentScript != null)
            {
                segmentScript.segmentImage.fillAmount = fillAmount;
                segmentScript.segmentImage.color = roleColor;

                // סיבוב הקונטיינר של הפלח
                newSeg.transform.localRotation = Quaternion.Euler(0, 0, -currentRotation * 360f);

                // מיקום הטקסט
                if (percentage > 5f)
                {
                    segmentScript.percentageText.text = $"{roleName}\n{percentage:F0}%";
                    segmentScript.percentageText.fontSize = pieFontSize;

                    // חישוב אמצע הפלח והזזת הטקסט החוצה
                    float halfSliceAngle = (fillAmount * 360f) / 2f;
                    Vector3 textPos = Quaternion.Euler(0, 0, -halfSliceAngle) * Vector3.up * textRadius;
                    segmentScript.percentageText.transform.localPosition = textPos;

                    // יישור הטקסט ביחס לקיר
                    Quaternion wallRotation = pieChartContainer.rotation;
                    segmentScript.percentageText.transform.rotation = wallRotation * Quaternion.Euler(0, textRotationY, textRotationZ);
                }
                else
                {
                    segmentScript.percentageText.text = "";
                }
            }

            currentRotation += fillAmount;
        }

        // --- רשימה בצד (Legend) ---
        foreach (var stat in allStats.Values)
        {
            // *** תנאי קריטי: אם השחקן לא מחובר, מדלגים עליו ***
            if (!stat.isOnline) continue;

            string pName = stat.playerName;
            string pRole = string.IsNullOrEmpty(stat.role) ? "Guest" : stat.role;
            float stayTime = MeetingAuditor.Instance.GetPlayerStayTime(pName);
            float personalFill = stat.totalSpeechTime / totalSessionSpeech;
            Color pColor = GetColorForRole(pRole);

            GameObject newRow = Instantiate(statRowPrefab, legendContainer);
            StatRow rowScript = newRow.GetComponent<StatRow>();
            if (rowScript != null)
            {
                string displayName = $"{pRole} | {pName}";
                rowScript.Setup(displayName, pColor, personalFill, stayTime);
            }
        }
    }

    Color GetColorForRole(string roleName)
    {
        // 1. בדיקה האם המשתמש הגדיר צבע ספציפי לתפקיד הזה
        if (specificRoleColors != null)
        {
            foreach (var definition in specificRoleColors)
            {
                // משווים את השם (מתעלמים מאותיות גדולות/קטנות כדי למנוע טעויות)
                if (string.Equals(definition.roleName, roleName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return definition.color;
                }
            }
        }

        // 2. אם לא נמצא צבע ספציפי, משתמשים בלוגיקה הישנה כגיבוי
        if (fallbackColors != null && fallbackColors.Length > 0)
        {
            int hash = System.Math.Abs(roleName.GetHashCode());
            int index = hash % fallbackColors.Length;
            return fallbackColors[index];
        }

        // 3. אם הכל נכשל - צבע רנדומלי
        Random.InitState(roleName.GetHashCode());
        return Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 0.9f);
    }

    // פונקציה לסגירת החלון
    public void ClosePanel()
    {
        // אפשרות א': אם הסקריפט יושב על האובייקט שאת רוצה לסגור
        gameObject.SetActive(false);

        // אפשרות ב': אם הסקריפט יושב על אובייקט פנימי ואת רוצה לסגור את כל הקנבס,
        // תצטרכי לגרור את הקנבס למשתנה, או לסגור את ה"הורה" של האובייקט:
        // transform.parent.gameObject.SetActive(false); 
    }
}