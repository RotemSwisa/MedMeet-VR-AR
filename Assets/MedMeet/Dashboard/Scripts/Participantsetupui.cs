using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SetupCanvas controller — Screen 1.
///
/// In multiplayer:
///   • Every client sees one row per joined participant.
///   • Each client edits ONLY their own row (the one with their clientID).
///   • Other clients' rows are read-only previews.
///   • If a client hasn't added themselves yet, an "Add me" prompt appears.
///   • Any client can press Calculate impact once ≥ 2 cities are mapped.
///
/// In single-player / no Normcore:
///   • clientID resolves to 0 and everything still works locally.
/// </summary>
public class ParticipantSetupUI : MonoBehaviour
{
    // ── Inspector wiring ───────────────────────────────────────────────────
    [Header("Participant rows container (vertical layout group)")]
    public RectTransform rowsContainer;

    [Header("ParticipantRow prefab (created by setup tool)")]
    public GameObject participantRowPrefab;

    [Header("Buttons")]
    public Button addParticipantButton;
    public Button calculateButton;

    [Header("Readiness label (under Calculate)")]
    public TextMeshProUGUI readinessLabel;

    [Header("Hands-on demos stepper")]
    public TextMeshProUGUI demosLabel;
    public Button demosMinusButton;
    public Button demosPlusButton;

    [Header("Right panel — Journeys list")]
    public RectTransform journeysContainer;
    public GameObject journeyRowPrefab;
    public TextMeshProUGUI journeysEmptyLabel;

    [Header("Right panel — Tally values")]
    public TextMeshProUGUI tallyLocationsValue;
    public TextMeshProUGUI tallyRoutesValue;
    public TextMeshProUGUI tallyKmValue;

    [Header("City picker popup (shared, built once)")]
    public CitySelectPopup citySelectPopup;

    [Header("Step dots — optional")]
    public StepDotsUI stepDots;

    private const int MinForCalculate = 2;

    // local UI cache of spawned rows by clientID
    private readonly Dictionary<int, ParticipantRowUI> _rowsByClient = new Dictionary<int, ParticipantRowUI>();

    // Local-only entries used when Normcore is absent or disconnected
    private readonly List<DashboardSync.Entry> _localEntries = new List<DashboardSync.Entry>();
    private int _localNextId = 0;
    private int _localDemos = 3;

    /// <summary>Returns true when we have a live, synced Normcore session.</summary>
    private bool IsOnline => DashboardSync.Instance != null && DashboardSync.Instance.IsModelReady;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Start()
    {
        if (participantRowPrefab == null || rowsContainer == null)
        {
            Debug.LogError("[ParticipantSetupUI] SetupCanvas not built yet — run " +
                "'MedMeet Tools → Setup Sustainability Showcase' once before pressing Play.", this);
            return;
        }

        if (addParticipantButton != null)
            addParticipantButton.onClick.AddListener(OnAddMe);
        if (calculateButton != null)
            calculateButton.onClick.AddListener(OnCalculate);
        if (demosMinusButton != null)
            demosMinusButton.onClick.AddListener(() => OnDemos(-1));
        if (demosPlusButton != null)
            demosPlusButton.onClick.AddListener(() => OnDemos(+1));

        if (DashboardSync.Instance != null)
            DashboardSync.Instance.OnEntriesChanged += Rebuild;

        if (stepDots != null) stepDots.SetStep(0);

        // Seed local fallback entries so the screen always shows something usable
        if (!IsOnline) SeedLocalEntries();

        Rebuild();
    }

    void Update()
    {
        // If Normcore connects after Start(), drop the local sample and rebuild
        if (_localEntries.Count > 0 && IsOnline)
        {
            _localEntries.Clear();
            Rebuild();
        }
    }

    void OnDestroy()
    {
        if (DashboardSync.Instance != null)
            DashboardSync.Instance.OnEntriesChanged -= Rebuild;
    }

    void OnEnable()
    {
        if (stepDots != null) stepDots.SetStep(0);
        Rebuild();
    }

    // ── UI actions ─────────────────────────────────────────────────────────
    private void OnAddMe()
    {
        if (IsOnline)
        {
            DashboardSync.Instance.UpsertMyEntry(name: "", cityId: "");
        }
        else
        {
            _localEntries.Add(new DashboardSync.Entry(_localNextId++, "", ""));
            Rebuild();
        }
    }

    private void OnDemos(int delta)
    {
        if (IsOnline)
        {
            DashboardSync.Instance.SetDemos(DashboardSync.Instance.CurrentDemos + delta);
        }
        else
        {
            _localDemos = Mathf.Clamp(_localDemos + delta, 0, 12);
            Rebuild();
        }
    }

    public void OnCalculate()
    {
        var entries = CurrentEntries();
        if (entries.Count(e => !string.IsNullOrEmpty(e.cityId)) < MinForCalculate) return;

        if (IsOnline)
        {
            DashboardSync.Instance.StartCalculation();
        }
        else
        {
            // Local fallback — push directly to DataManager and switch screen
            var cityNames = entries
                .Where(e => !string.IsNullOrEmpty(e.cityId))
                .Select(e => SustainabilityData.CityById(e.cityId)?.name ?? e.cityId).ToList();
            var names = entries
                .Where(e => !string.IsNullOrEmpty(e.cityId))
                .Select(e => string.IsNullOrEmpty(e.name) ? "Participant" : e.name).ToList();
            if (DashboardDataManager.Instance != null)
                DashboardDataManager.Instance.ApplyShowcaseSession(cityNames, names, _localDemos);
            var setup = FindFirstObjectByType<DashboardSceneSetup>();
            if (setup != null) setup.ShowLoading();
        }
    }

    // ── Rebuild rows from synced state (INCREMENTAL — keeps focus alive) ───
    //
    // Why incremental: every keystroke calls UpsertMyEntry → entriesCSV
    // changes → OnEntriesChanged fires → Rebuild runs. If Rebuild destroys
    // and re-instantiates the row GameObject, the TMP_InputField loses focus
    // and the VR keyboard closes after the first character. Updating rows
    // in place avoids that.
    private void Rebuild()
    {
        if (rowsContainer == null) return;

        var entries = CurrentEntries();
        int myID = IsOnline ? DashboardSync.Instance.LocalClientID : -1;
        bool meHasEntry = !IsOnline || entries.Any(e => e.clientID == myID);
        var liveIds = new HashSet<int>();

        // 1) Update existing rows in place + spawn missing ones
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            bool isMine = IsOnline ? e.clientID == myID : true;
            liveIds.Add(e.clientID);

            if (_rowsByClient.TryGetValue(e.clientID, out var existing) && existing != null)
            {
                UpdateRowInPlace(existing, e, isMine, i);
            }
            else
            {
                SpawnRow(e, isMine: isMine, indexOverride: i);
            }
        }

        // 2) Remove rows for entries that disappeared
        var stale = _rowsByClient.Keys.Where(id => !liveIds.Contains(id)).ToList();
        foreach (var id in stale)
        {
            if (_rowsByClient[id] != null) Destroy(_rowsByClient[id].gameObject);
            _rowsByClient.Remove(id);
        }

        // 3) Re-order siblings to match the entries list
        for (int i = 0; i < entries.Count; i++)
        {
            if (_rowsByClient.TryGetValue(entries[i].clientID, out var row) && row != null)
                row.transform.SetSiblingIndex(i);
        }

        // 4) Demos label
        int demos = IsOnline ? DashboardSync.Instance.CurrentDemos : _localDemos;
        if (demosLabel != null) demosLabel.text = demos.ToString();

        // 5) Add button visibility — only shown when I haven't added myself yet
        if (addParticipantButton != null)
            addParticipantButton.gameObject.SetActive(!meHasEntry);

        // 6) Readiness + calculate enabled
        int ready = entries.Count(x => !string.IsNullOrEmpty(x.cityId));
        bool canCalc = ready >= MinForCalculate;
        if (calculateButton != null) calculateButton.interactable = canCalc;
        if (readinessLabel != null)
        {
            if (canCalc)
            {
                readinessLabel.text  = $"✓  {ready} locations mapped";
                readinessLabel.color = SustainabilityTheme.Mint;
            }
            else
            {
                readinessLabel.text  = "Add at least 2 participants";
                readinessLabel.color = SustainabilityTheme.Clay;
            }
        }

        // 7) Right panel journeys
        RefreshJourneys(entries);
    }

    /// <summary>
    /// Update an existing ParticipantRowUI's visible state without re-binding
    /// the input field (which would clobber the user's current cursor).
    /// Only re-bind when isMine or city changed — never on name typing.
    /// </summary>
    private void UpdateRowInPlace(ParticipantRowUI row, DashboardSync.Entry e, bool isMine, int idx)
    {
        bool needRebind =
            (row.CurrentCityId ?? "") != (e.cityId ?? "") ||
            // If the name in the model differs from what the user has typed AND
            // this row isn't currently being edited, refresh — but skip when it
            // IS being edited so the keyboard stays open.
            ((row.CurrentName ?? "") != (e.name ?? "") &&
             !(isMine && row.nameInput != null && row.nameInput.isFocused));

        if (needRebind)
        {
            row.Bind(
                index:      idx,
                name:       e.name,
                cityId:     e.cityId,
                isMine:     isMine,
                onName:     n  => OnRowNameChanged(e.clientID, n),
                onPickCity: () => OnRowCityRequested(e.clientID, e.cityId),
                onRemove:   () => OnRowRemove(e.clientID));
        }
        else
        {
            // Just update the lightweight bits (index + host badge)
            row.SetIndex(idx, isHost: idx == 0);
        }
    }

    private void SpawnRow(DashboardSync.Entry e, bool isMine, int indexOverride = -1)
    {
        if (participantRowPrefab == null || rowsContainer == null) return;
        var go = Instantiate(participantRowPrefab, rowsContainer);
        go.SetActive(true);
        var rowUI = go.GetComponent<ParticipantRowUI>();
        if (rowUI == null) return;

        int idx = indexOverride >= 0 ? indexOverride : _rowsByClient.Count;
        rowUI.Bind(
            index:      idx,
            name:       e.name,
            cityId:     e.cityId,
            isMine:     isMine,
            onName:     n  => OnRowNameChanged(e.clientID, n),
            onPickCity: () => OnRowCityRequested(e.clientID, e.cityId),
            onRemove:   () => OnRowRemove(e.clientID));
        _rowsByClient[e.clientID] = rowUI;
    }

    private void OnRowNameChanged(int clientID, string newName)
    {
        if (IsOnline)
        {
            if (clientID != DashboardSync.Instance.LocalClientID) return;
            var mine = DashboardSync.Instance.MyEntry();
            DashboardSync.Instance.UpsertMyEntry(newName, mine?.cityId ?? "");
        }
        else
        {
            var e = _localEntries.FirstOrDefault(x => x.clientID == clientID);
            if (e != null) { e.name = newName; RefreshJourneys(_localEntries); }
        }
    }

    private void OnRowCityRequested(int clientID, string currentCityId)
    {
        if (citySelectPopup == null) return;
        citySelectPopup.Open(currentCityId, picked =>
        {
            if (IsOnline)
            {
                if (clientID != DashboardSync.Instance.LocalClientID) return;
                var mine = DashboardSync.Instance.MyEntry();
                DashboardSync.Instance.UpsertMyEntry(mine?.name ?? "", picked);
            }
            else
            {
                var e = _localEntries.FirstOrDefault(x => x.clientID == clientID);
                if (e != null) { e.cityId = picked; Rebuild(); }
            }
        });
    }

    private void OnRowRemove(int clientID)
    {
        if (IsOnline)
        {
            if (clientID != DashboardSync.Instance.LocalClientID) return;
            DashboardSync.Instance.RemoveMyEntry();
        }
        else
        {
            _localEntries.RemoveAll(x => x.clientID == clientID);
            Rebuild();
        }
    }

    // ── Right-panel journeys + tallies ─────────────────────────────────────
    private void RefreshJourneys(List<DashboardSync.Entry> entries)
    {
        if (journeysContainer == null) return;
        for (int i = journeysContainer.childCount - 1; i >= 0; i--)
            Destroy(journeysContainer.GetChild(i).gameObject);

        var ready = entries.Where(e => !string.IsNullOrEmpty(e.cityId)).ToList();
        SustainabilityData.City host = ready.Count >= 1 ? SustainabilityData.CityById(ready[0].cityId) : null;
        bool empty = host == null || ready.Count < 2;
        if (journeysEmptyLabel != null) journeysEmptyLabel.gameObject.SetActive(empty);

        float liveKm = 0f;
        if (host != null)
        {
            for (int i = 1; i < ready.Count; i++)
            {
                var c = SustainabilityData.CityById(ready[i].cityId);
                if (c == null) continue;
                float oneWay = SustainabilityData.Haversine(c, host);
                liveKm += oneWay * 2f;
                if (journeyRowPrefab != null)
                {
                    var rowGO = Instantiate(journeyRowPrefab, journeysContainer);
                    rowGO.SetActive(true);
                    rowGO.GetComponent<JourneyRowUI>()?.Bind(c.name, host.name, oneWay, oneWay >= SustainabilityData.CarThresholdKm);
                }
            }
        }
        if (tallyLocationsValue != null) tallyLocationsValue.text = ready.Count.ToString();
        if (tallyRoutesValue    != null) tallyRoutesValue.text    = Mathf.Max(0, ready.Count - 1).ToString();
        if (tallyKmValue        != null) tallyKmValue.text        = "~" + SustainabilityData.FmtCompact(liveKm);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private List<DashboardSync.Entry> CurrentEntries()
    {
        if (IsOnline) return DashboardSync.Instance.AllEntries();
        return new List<DashboardSync.Entry>(_localEntries);
    }

    private void SeedLocalEntries()
    {
        _localEntries.Clear();
        _localEntries.Add(new DashboardSync.Entry(_localNextId++, "Dr. Maya Levi",    "tlv"));
        _localEntries.Add(new DashboardSync.Entry(_localNextId++, "Dr. James Carter", "lon"));
        _localEntries.Add(new DashboardSync.Entry(_localNextId++, "Dr. Sarah Kim",    "nyc"));
        _localDemos = 3;
    }
}
