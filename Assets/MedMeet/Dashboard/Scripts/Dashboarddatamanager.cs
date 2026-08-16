using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that holds the live data shown on the Sustainability Showcase.
///
/// Two code paths:
///   1. Legacy: SetParticipantCities(cities) — kept for backwards compatibility
///      with the old SavingsBarChart / MetricCardUI scripts.
///   2. Showcase: ApplyShowcaseSession(cities, names, demos) — populates the
///      full SustainabilityData.Impact used by the new 3-screen flow.
///
/// Live UI subscribes to OnDataUpdated for both legacy and showcase metrics.
/// </summary>
public class DashboardDataManager : MonoBehaviour
{
    public static DashboardDataManager Instance { get; private set; }

    public event Action OnDataUpdated;

    [Header("Session Setup")]
    [Tooltip("Display name shown on the dashboard header")]
    public string sessionName = "Cardiology Team Sync";

    [Tooltip("Sheets-saved virtual print rate, used by legacy live metrics")]
    public float pagesPerMinute = 2.3f;

    [Header("Update Rates")]
    [Tooltip("Seconds between live updates")]
    public float liveUpdateIntervalSeconds = 30f;

    [Header("Participant Locations (filled before session)")]
    public List<string> participantCityNames = new List<string>
    { "Tel Aviv", "London", "New York" };

    [Header("Participant Names (parallel to participantCityNames; optional)")]
    public List<string> participantNames = new List<string>
    { "Dr. Maya Levi", "Dr. James Carter", "Dr. Sarah Kim" };

    [Header("Hands-on Demos")]
    [Tooltip("Number of hands-on demonstrations in this meeting")]
    public int handsOnDemos = 3;

    // ── Legacy live values (kept for SavingsBarChart / old MetricCardUI) ────
    public float CO2SavedKg { get; private set; }
    public float KmSaved { get; private set; }
    public float PagesSaved { get; private set; }
    public bool  SignLanguageActive { get; private set; }
    public float SignLanguageSeconds { get; private set; }
    public int   SyringesSaved { get; private set; }
    public int   GlovesSaved { get; private set; }
    public int   EquipmentSetsSaved { get; private set; }
    public float CumulativeCO2Kg { get; private set; }
    public float CumulativeKm { get; private set; }
    public int   TotalSessions { get; private set; }

    // ── Showcase impact ─────────────────────────────────────────────────────
    /// <summary>Latest Impact computed from current participant list.</summary>
    public SustainabilityData.Impact CurrentImpact { get; private set; }

    // ── Private ─────────────────────────────────────────────────────────────
    private float _sessionStartTime;
    private Coroutine _liveUpdateCoroutine;

    void Awake()
    {
        // Scene-scoped singleton. We do NOT use DontDestroyOnLoad because the
        // showcase only exists during the meeting scene — leaving and returning
        // should produce a clean instance, not a stale one with dangling event
        // subscriptions on destroyed UI.
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start() => InitializeSession();

    public void InitializeSession()
    {
        _sessionStartTime = Time.time;
        Recompute();

        // Cumulative tracking
        LoadCumulative();
        TotalSessions++;
        CumulativeCO2Kg += CO2SavedKg;
        CumulativeKm    += KmSaved;
        SaveCumulative();

        if (_liveUpdateCoroutine != null) StopCoroutine(_liveUpdateCoroutine);
        _liveUpdateCoroutine = StartCoroutine(LiveUpdateLoop());

        OnDataUpdated?.Invoke();
    }

    // ── Showcase entry point (called by DashboardSync) ──────────────────────
    public void ApplyShowcaseSession(List<string> cities, List<string> names, int demos)
    {
        participantCityNames = cities != null ? new List<string>(cities) : new List<string>();
        participantNames     = names  != null ? new List<string>(names)  : new List<string>();
        handsOnDemos         = Mathf.Max(0, demos);
        InitializeSession();
    }

    // ── Legacy entry point (used by old setup tool) ─────────────────────────
    public void SetParticipantCities(List<string> cities)
    {
        ApplyShowcaseSession(cities, null, handsOnDemos);
    }

    // ── Core compute (uses SustainabilityData) ──────────────────────────────
    private void Recompute()
    {
        var participants = new List<SustainabilityData.Participant>();
        for (int i = 0; i < participantCityNames.Count; i++)
        {
            var city = SustainabilityData.CityByName(participantCityNames[i]);
            if (city == null) continue;
            string n = (participantNames != null && i < participantNames.Count)
                ? participantNames[i] : $"Participant {i + 1}";
            participants.Add(new SustainabilityData.Participant(n, city.id));
        }

        CurrentImpact = SustainabilityData.Compute(participants, handsOnDemos);

        // Legacy fields
        CO2SavedKg  = CurrentImpact.co2Total;
        KmSaved     = CurrentImpact.km;

        int p = Mathf.Max(1, CurrentImpact.n);
        SyringesSaved      = p * 12;
        GlovesSaved        = CurrentImpact.glovePairs;
        EquipmentSetsSaved = p * 4;
    }

    // ── Live tick (paper count grows over session time) ─────────────────────
    private IEnumerator LiveUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(liveUpdateIntervalSeconds);
            UpdateLiveMetrics();
        }
    }

    private void UpdateLiveMetrics()
    {
        float elapsedMinutes = (Time.time - _sessionStartTime) / 60f;
        PagesSaved = elapsedMinutes * pagesPerMinute;

        int bonusRounds = Mathf.FloorToInt(elapsedMinutes / 10f);
        int p = Mathf.Max(1, CurrentImpact != null ? CurrentImpact.n : 1);
        SyringesSaved      = p * 12 + bonusRounds * p;
        EquipmentSetsSaved = p * 4 + bonusRounds;

        OnDataUpdated?.Invoke();
    }

    // ── Sign language API (unchanged) ──────────────────────────────────────
    public void SetSignLanguageActive(bool active)
    {
        SignLanguageActive = active;
        OnDataUpdated?.Invoke();
    }

    public void AddSignLanguageSeconds(float delta)
    {
        SignLanguageSeconds += delta;
    }

    // ── Persistence ─────────────────────────────────────────────────────────
    private const string PrefCo2      = "MM_CumulativeCO2";
    private const string PrefKm       = "MM_CumulativeKm";
    private const string PrefSessions = "MM_TotalSessions";

    private void LoadCumulative()
    {
        CumulativeCO2Kg = PlayerPrefs.GetFloat(PrefCo2, 0f);
        CumulativeKm    = PlayerPrefs.GetFloat(PrefKm,  0f);
        TotalSessions   = PlayerPrefs.GetInt(PrefSessions, 0);
    }

    private void SaveCumulative()
    {
        PlayerPrefs.SetFloat(PrefCo2,  CumulativeCO2Kg);
        PlayerPrefs.SetFloat(PrefKm,   CumulativeKm);
        PlayerPrefs.SetInt(PrefSessions, TotalSessions);
        PlayerPrefs.Save();
    }

    public float SessionElapsedSeconds => Time.time - _sessionStartTime;
}
