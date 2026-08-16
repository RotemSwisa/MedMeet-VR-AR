using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives three horizontal progress-bar fills (CO₂, Km, Pages) from DashboardDataManager.
/// Call AnimateBars() to trigger the fill-in animation (called by DashboardUIController
/// after the plane animation completes).
///
/// Unity wiring (Inspector):
///   co2BarFill   — Image with ImageType = Filled, FillMethod = Horizontal
///   kmBarFill    — same
///   pagesBarFill — same
///   co2Label / kmLabel / pagesLabel — TMP labels next to each bar
///   maxCO2Kg     — value that represents a full (100%) bar (default 5000)
///   maxKmSaved   — default 20000
///   maxPages     — default 500
/// </summary>
public class SavingsBarChart : MonoBehaviour
{
    [Header("CO₂ Bar")]
    public Image              co2BarFill;
    public TextMeshProUGUI    co2Label;
    [Tooltip("CO₂ value that fills the bar to 100%")]
    public float              maxCO2Kg = 5000f;

    [Header("Km Bar")]
    public Image              kmBarFill;
    public TextMeshProUGUI    kmLabel;
    [Tooltip("Km value that fills the bar to 100%")]
    public float              maxKmSaved = 20000f;

    [Header("Pages Bar")]
    public Image              pagesBarFill;
    public TextMeshProUGUI    pagesLabel;
    [Tooltip("Pages value that fills the bar to 100%")]
    public float              maxPages = 500f;

    [Header("Animation")]
    [Tooltip("Seconds to fill each bar")]
    public float fillDuration = 1.4f;
    [Tooltip("Pause between bars")]
    public float delayBetweenBars = 0.15f;

    // ── State ─────────────────────────────────────────────────────────────
    private DashboardDataManager _dm;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Start()
    {
        _dm = DashboardDataManager.Instance;
        if (_dm == null) return;

        _dm.OnDataUpdated += OnDataUpdated;

        // Start bars at zero
        SetFillImmediate(co2BarFill,   co2Label,   0f, 0f, "0 kg CO₂");
        SetFillImmediate(kmBarFill,    kmLabel,    0f, 0f, "0 km");
        SetFillImmediate(pagesBarFill, pagesLabel, 0f, 0f, "0 pages");
    }

    void OnDestroy()
    {
        if (_dm != null) _dm.OnDataUpdated -= OnDataUpdated;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Called by DashboardUIController after the plane animation finishes.</summary>
    public void AnimateBars()
    {
        if (_dm == null) _dm = DashboardDataManager.Instance;
        if (_dm == null) return;
        StartCoroutine(RunBarAnimation());
    }

    // ── Internal ───────────────────────────────────────────────────────────

    private IEnumerator RunBarAnimation()
    {
        yield return AnimateBar(co2BarFill,   co2Label,   _dm.CO2SavedKg, maxCO2Kg,   "{0:F0} kg CO₂");
        yield return new WaitForSeconds(delayBetweenBars);
        yield return AnimateBar(kmBarFill,    kmLabel,    _dm.KmSaved,    maxKmSaved,  "{0:N0} km");
        yield return new WaitForSeconds(delayBetweenBars);
        yield return AnimateBar(pagesBarFill, pagesLabel, _dm.PagesSaved, maxPages,    "{0:N0} pages");
    }

    private IEnumerator AnimateBar(Image fill, TextMeshProUGUI label,
                                   float targetValue, float maxValue, string fmt)
    {
        if (fill == null) yield break;

        float targetFill = maxValue > 0f ? Mathf.Clamp01(targetValue / maxValue) : 0f;
        float elapsed    = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fillDuration);
            fill.fillAmount = Mathf.Lerp(0f, targetFill, t);
            if (label != null)
                label.text = string.Format(fmt, Mathf.Lerp(0f, targetValue, t));
            yield return null;
        }

        fill.fillAmount = targetFill;
        if (label != null) label.text = string.Format(fmt, targetValue);
    }

    private void SetFillImmediate(Image fill, TextMeshProUGUI label, float fillAmt, float value, string text)
    {
        if (fill  != null) fill.fillAmount = fillAmt;
        if (label != null) label.text      = text;
    }

    private void OnDataUpdated()
    {
        if (_dm == null) return;
        // Live refresh (no animation — just snap to current value)
        if (co2BarFill   != null) co2BarFill.fillAmount   = Mathf.Clamp01(_dm.CO2SavedKg  / maxCO2Kg);
        if (kmBarFill    != null) kmBarFill.fillAmount    = Mathf.Clamp01(_dm.KmSaved     / maxKmSaved);
        if (pagesBarFill != null) pagesBarFill.fillAmount = Mathf.Clamp01(_dm.PagesSaved  / maxPages);

        if (co2Label   != null) co2Label.text   = $"{_dm.CO2SavedKg:F0} kg CO₂";
        if (kmLabel    != null) kmLabel.text    = $"{_dm.KmSaved:N0} km";
        if (pagesLabel != null) pagesLabel.text = $"{_dm.PagesSaved:N0} pages";
    }
}
