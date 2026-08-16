using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// רכיב לכל כרטיס מדד בודד בדשבורד.
/// בוחרים את סוג המדד ב-Inspector, והכרטיס מתעדכן אוטומטית.
/// </summary>
public class MetricCardUI : MonoBehaviour
{
    // -------------------------------------------------------------------
    // איזה מדד הכרטיס הזה מציג
    // -------------------------------------------------------------------
    public enum MetricType
    {
        CO2Saved,
        KmSaved,
        PagesSaved,
        SignLanguageTime,
        SyringesSaved,
        GlovesSaved,
        EquipmentSetsSaved,
        CumulativeCO2,
        CumulativeKm,
        TotalSessions
    }

    // -------------------------------------------------------------------
    [Header("Metric")]
    public MetricType metricType = MetricType.CO2Saved;

    [Header("UI References")]
    public TextMeshProUGUI valueLabel;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI subtitleLabel;
    public Image accentBar;       // הפס הצבעוני בראש הכרטיס (אופציונלי)

    [Header("Display Settings")]
    [Tooltip("פורמט מספרי — {0:F1} לעשרוני, {0:F0} לשלם, {0:N0} עם פסיקים")]
    public string numberFormat = "{0:F0}";
    public string unitSuffix = "";
    public Color accentColor = new Color(0.094f, 0.373f, 0.647f); // Medical Blue

    // -------------------------------------------------------------------
    private AnimatedCounter _counter;
    private DashboardDataManager _dm;

    // -------------------------------------------------------------------
    void Start()
    {
        _counter = GetComponent<AnimatedCounter>();
        if (_counter == null)
            _counter = gameObject.AddComponent<AnimatedCounter>();

        _counter.format = numberFormat;
        _counter.suffix = unitSuffix;
        _counter.label = valueLabel;

        if (accentBar != null)
            accentBar.color = accentColor;

        // הצטרף לאירוע עדכון
        if (DashboardDataManager.Instance != null)
        {
            _dm = DashboardDataManager.Instance;
            _dm.OnDataUpdated += Refresh;
            Refresh(); // עדכון ראשוני מיידי
        }
        else
        {
            Debug.LogWarning($"MetricCardUI ({metricType}): DashboardDataManager לא נמצא.");
        }
    }

    void OnDestroy()
    {
        if (_dm != null)
            _dm.OnDataUpdated -= Refresh;
    }

    // -------------------------------------------------------------------
    // שפת סימנים — עדכון בזמן אמת (Polling כל פריים)
    // -------------------------------------------------------------------
    void Update()
    {
        if (metricType == MetricType.SignLanguageTime && _dm != null)
            RefreshSignLanguageTime();
    }

    // -------------------------------------------------------------------
    public void Refresh()
    {
        if (_dm == null) return;

        float value = GetCurrentValue();
        _counter.SetValue(value);
    }

    // -------------------------------------------------------------------
    private float GetCurrentValue()
    {
        return metricType switch
        {
            MetricType.CO2Saved => _dm.CO2SavedKg,
            MetricType.KmSaved => _dm.KmSaved,
            MetricType.PagesSaved => _dm.PagesSaved,
            MetricType.SignLanguageTime => _dm.SignLanguageSeconds,
            MetricType.SyringesSaved => _dm.SyringesSaved,
            MetricType.GlovesSaved => _dm.GlovesSaved,
            MetricType.EquipmentSetsSaved => _dm.EquipmentSetsSaved,
            MetricType.CumulativeCO2 => _dm.CumulativeCO2Kg,
            MetricType.CumulativeKm => _dm.CumulativeKm,
            MetricType.TotalSessions => _dm.TotalSessions,
            _ => 0f
        };
    }

    // -------------------------------------------------------------------
    // עדכון מיוחד לשפת הסימנים — מציג MM:SS
    // -------------------------------------------------------------------
    private void RefreshSignLanguageTime()
    {
        float totalSeconds = _dm.SignLanguageSeconds;
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        if (valueLabel != null)
            valueLabel.text = $"{minutes:D2}:{seconds:D2}";

        // צבע הכרטיס — כחול כשפעיל, אפור כשכבוי
        if (accentBar != null)
            accentBar.color = _dm.SignLanguageActive
                ? new Color(0.094f, 0.373f, 0.647f)   // Medical Blue
                : new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }
}