using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DashboardCanvas (Screen 3) controller — the impact report.
///
/// Layout (built by SustainabilityShowcaseBuilder):
///   LEFT  hero panel: MedMeet logo, "CO₂ EMISSIONS AVOIDED", big animated
///         CO₂ value, "≈ X trees working for a year" pill,
///         travel-avoided + time-saved tiles, meta footer
///         (participants / host / hands-on demos).
///   RIGHT column:
///         • Three resource cards (Fuel / Paper / Gloves) with animated counters.
///         • Round-trips-replaced bar list + CO₂-by-source donut.
///         • "That's like…" equivalences strip + New-calculation button.
///
/// All metrics come from DashboardDataManager.Instance.CurrentImpact.
/// </summary>
public class DashboardUIController : MonoBehaviour
{
    // ── Hero ────────────────────────────────────────────────────────────────
    [Header("Hero (left panel)")]
    public TextMeshProUGUI co2HeadlineValue;     // animated count
    public TextMeshProUGUI co2HeadlineUnit;      // "kg"
    public TextMeshProUGUI treesPillValue;       // "≈ 217.3 trees working for a year"
    public TextMeshProUGUI travelAvoidedValue;   // "25k km"
    public TextMeshProUGUI travelAvoidedUnit;    // "km"
    public TextMeshProUGUI timeSavedValue;       // "49 h"
    public TextMeshProUGUI timeSavedUnit;        // "h"
    public TextMeshProUGUI metaParticipants;     // "3 participants"
    public TextMeshProUGUI metaHost;             // "Host · Tel Aviv"
    public TextMeshProUGUI metaDemos;            // "3 hands-on demos"

    // ── Resource cards ──────────────────────────────────────────────────────
    [Header("Fuel card")]
    public TextMeshProUGUI fuelValue;
    public TextMeshProUGUI fuelUnit;
    public TextMeshProUGUI fuelSub;

    [Header("Paper card")]
    public TextMeshProUGUI sheetsValue;
    public TextMeshProUGUI sheetsUnit;
    public TextMeshProUGUI sheetsSub;

    [Header("Gloves card")]
    public TextMeshProUGUI glovesValue;
    public TextMeshProUGUI glovesUnit;
    public TextMeshProUGUI glovesSub;

    // ── Journeys & donut ────────────────────────────────────────────────────
    [Header("Round trips replaced — bar list")]
    public RectTransform journeyBarsContainer;
    public GameObject    journeyBarPrefab;

    [Header("CO₂ by source — donut")]
    public DonutChartUI donut;
    public TextMeshProUGUI donutTotalLabel;       // big number inside donut
    public TextMeshProUGUI donutUnitLabel;        // "kg CO₂"
    public TextMeshProUGUI donutAirLegend;
    public TextMeshProUGUI donutRoadLegend;
    public TextMeshProUGUI donutVRLegend;

    // ── Equivalences strip ──────────────────────────────────────────────────
    [Header("Equivalences strip")]
    public TextMeshProUGUI equivWaterValue;       // L of water
    public TextMeshProUGUI equivCarValue;         // km not driven
    public TextMeshProUGUI equivPhoneValue;       // phone charges

    // ── Reset / step dots ───────────────────────────────────────────────────
    [Header("New calculation")]
    public Button newCalculationButton;

    [Header("Step dots — optional")]
    public StepDotsUI stepDots;

    // ── Counter behaviour ───────────────────────────────────────────────────
    [Header("Count-up animation")]
    public float countUpDuration = 1.4f;

    private DashboardDataManager _dm;
    private Coroutine _animationCoroutine;

    void Awake() => _dm = DashboardDataManager.Instance;

    void Start()
    {
        if (_dm == null) _dm = DashboardDataManager.Instance;
        if (newCalculationButton != null)
            newCalculationButton.onClick.AddListener(OnNewCalculation);
        if (_dm != null) _dm.OnDataUpdated += Refresh;
    }

    void OnDestroy()
    {
        if (_dm != null) _dm.OnDataUpdated -= Refresh;
    }

    void OnEnable()
    {
        if (stepDots != null) stepDots.SetStep(2);
        Refresh();
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(PlayEntryAnimation());
    }

    // ── Data refresh (static labels) ───────────────────────────────────────
    public void Refresh()
    {
        if (_dm == null) _dm = DashboardDataManager.Instance;
        if (_dm == null || _dm.CurrentImpact == null) return;

        var imp = _dm.CurrentImpact;

        if (co2HeadlineUnit != null)  co2HeadlineUnit.text  = "kg";
        if (travelAvoidedUnit != null) travelAvoidedUnit.text = "km";
        if (timeSavedUnit != null)    timeSavedUnit.text    = "h";

        // Static labels (animated values are filled by entry animation)
        if (treesPillValue != null)
            treesPillValue.text = $"≈ {SustainabilityData.Fmt(imp.treesEquivYear, 1)} trees working for a year";

        if (metaParticipants != null) metaParticipants.text = $"{imp.n} participants";
        if (metaHost         != null) metaHost.text         = imp.host != null ? $"Host · {imp.host.name}" : "—";
        if (metaDemos        != null) metaDemos.text        = $"{imp.demos} hands-on demos";

        if (fuelUnit  != null) fuelUnit.text  = "L";
        if (fuelSub   != null) fuelSub.text   = $"{imp.flights} flight{(imp.flights != 1 ? "s" : "")} · {imp.drives} drive{(imp.drives != 1 ? "s" : "")}";

        if (sheetsUnit != null) sheetsUnit.text = "sheets";
        if (sheetsSub  != null) sheetsSub.text  = $"≈ {SustainabilityData.Fmt(imp.waterL)} L water saved";

        if (glovesUnit != null) glovesUnit.text = "pairs";
        if (glovesSub  != null) glovesSub.text  = $"{SustainabilityData.Fmt(imp.gloveCo2, 1)} kg CO₂ · VR demos";

        if (donutUnitLabel != null) donutUnitLabel.text = "kg CO₂";

        // Donut segments
        if (donut != null)
        {
            float air  = 0f, road = 0f;
            foreach (var l in imp.legs) if (l.isPlane) air += l.co2; else road += l.co2;
            float vr = imp.gloveCo2;
            donut.SetSegments(air, road, vr);
        }

        if (donutAirLegend  != null) donutAirLegend.text  = LegendLine("Air travel",  imp);
        if (donutRoadLegend != null) donutRoadLegend.text = LegendLine("Road travel", imp);
        if (donutVRLegend   != null) donutVRLegend.text   = LegendLine("VR demos",    imp);

        // Equivalences
        float carKmEquiv  = imp.co2Total / SustainabilityData.Co2PerKmCar;
        float phoneCharges = imp.co2Total / 0.008f;
        if (equivWaterValue != null) equivWaterValue.text = SustainabilityData.FmtCompact(imp.waterL);
        if (equivCarValue   != null) equivCarValue.text   = SustainabilityData.FmtCompact(carKmEquiv);
        if (equivPhoneValue != null) equivPhoneValue.text = SustainabilityData.FmtCompact(phoneCharges);

        RebuildJourneyBars(imp);
    }

    private string LegendLine(string label, SustainabilityData.Impact imp)
    {
        float air  = 0f, road = 0f;
        foreach (var l in imp.legs) if (l.isPlane) air += l.co2; else road += l.co2;
        float vr = imp.gloveCo2;
        float v = label.StartsWith("Air") ? air : label.StartsWith("Road") ? road : vr;
        float total = Mathf.Max(0.0001f, air + road + vr);
        int pct = Mathf.RoundToInt(v / total * 100f);
        return $"<b>{label}</b>\n<size=70%>{SustainabilityData.Fmt(v, v < 100f ? 1 : 0)} kg · {pct}%</size>";
    }

    private void RebuildJourneyBars(SustainabilityData.Impact imp)
    {
        if (journeyBarsContainer == null) return;
        for (int i = journeyBarsContainer.childCount - 1; i >= 0; i--)
            Destroy(journeyBarsContainer.GetChild(i).gameObject);

        if (imp.host == null || imp.legs == null) return;

        float maxLeg = 1f;
        foreach (var l in imp.legs) if (l.roundTrip > maxLeg) maxLeg = l.roundTrip;

        for (int i = 0; i < imp.legs.Count; i++)
        {
            var leg = imp.legs[i];
            if (journeyBarPrefab == null) break;
            var go = Instantiate(journeyBarPrefab, journeyBarsContainer);
            go.SetActive(true);
            var bar = go.GetComponent<JourneyBarUI>();
            bar?.Bind(leg, imp.host.name, leg.roundTrip / maxLeg, 0.1f + i * 0.08f);
        }
    }

    // ── Entry animation (count-up + donut draw + bars) ──────────────────────
    private IEnumerator PlayEntryAnimation()
    {
        // Wait a frame so all labels exist
        yield return null;

        if (_dm == null || _dm.CurrentImpact == null) yield break;
        var imp = _dm.CurrentImpact;

        // Set 0 then animate to target
        StartCoroutine(CountUp(co2HeadlineValue,   0f, imp.co2Total,   imp.co2Total < 100f ? 1 : 0, 0.15f));
        StartCoroutine(CountUp(travelAvoidedValue, 0f, imp.km,         0, 0.25f, compact: true));
        StartCoroutine(CountUp(timeSavedValue,     0f, imp.hours,      imp.hours < 10f ? 1 : 0, 0.30f));
        StartCoroutine(CountUp(fuelValue,          0f, imp.fuel,       imp.fuel < 100f ? 1 : 0, 0.35f));
        StartCoroutine(CountUp(sheetsValue,        0f, imp.sheets,     0, 0.42f));
        StartCoroutine(CountUp(glovesValue,        0f, imp.glovePairs, 0, 0.49f));

        // Donut number animates too
        float air  = 0f, road = 0f;
        foreach (var l in imp.legs) if (l.isPlane) air += l.co2; else road += l.co2;
        float total = air + road + imp.gloveCo2;
        StartCoroutine(CountUp(donutTotalLabel,    0f, total,          total < 100f ? 1 : 0, 0.60f));

        if (donut != null)
        {
            yield return new WaitForSeconds(0.6f);
            donut.AnimateDraw(1.0f);
        }
    }

    private IEnumerator CountUp(TextMeshProUGUI label, float from, float to, int decimals,
                                float delay, bool compact = false)
    {
        if (label == null) yield break;
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / countUpDuration));
            float v = Mathf.Lerp(from, to, k);
            label.text = compact
                ? SustainabilityData.FmtCompact(v)
                : SustainabilityData.Fmt(v, decimals);
            yield return null;
        }
        label.text = compact
            ? SustainabilityData.FmtCompact(to)
            : SustainabilityData.Fmt(to, decimals);
    }

    // ── Reset ──────────────────────────────────────────────────────────────
    public void OnNewCalculation()
    {
        if (DashboardSync.Instance != null && DashboardSync.Instance.IsModelReady)
            DashboardSync.Instance.SetSetup();
        else
        {
            var setup = FindFirstObjectByType<DashboardSceneSetup>();
            if (setup != null) setup.ShowSetup();
        }
    }
}
