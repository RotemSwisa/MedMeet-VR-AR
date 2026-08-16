using System;
using System.Collections.Generic;
using System.Linq;
using Normal.Realtime;
using UnityEngine;

/// <summary>
/// Scene-level Normcore component that syncs the Sustainability Showcase
/// across all participants.
///
/// Each client edits ONLY their own entry (tagged with their Normcore clientID).
/// All clients see the same list. The host (first client in the room) controls
/// the screen flow (Setup → Loading → Dashboard) and the demos count.
/// </summary>
public class DashboardSync : RealtimeComponent<DashboardSyncModel>
{
    public enum ScreenState { Setup = 0, Loading = 1, Dashboard = 2 }

    public static DashboardSync Instance { get; private set; }

    [Tooltip("DashboardSceneSetup in the scene — drives canvas activation.")]
    public DashboardSceneSetup sceneSetup;

    [Tooltip("Realtime instance used to resolve the local clientID. Auto-found if null.")]
    public Realtime realtime;

    /// <summary>Fires whenever the synced screen state changes.</summary>
    public event Action<ScreenState> OnScreenStateChanged;

    /// <summary>Fires when the participant list (entries CSV) or demos count changes.</summary>
    public event Action OnEntriesChanged;

    void Awake()
    {
        Instance = this;
        if (realtime == null) realtime = FindFirstObjectByType<Realtime>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int LocalClientID
    {
        get
        {
            if (realtime == null) realtime = FindFirstObjectByType<Realtime>();
            if (realtime != null && realtime.connected) return realtime.clientID;
            return -1;
        }
    }

    public bool IsModelReady => model != null;

    // ── Normcore wiring ─────────────────────────────────────────────────────
    protected override void OnRealtimeModelReplaced(
        DashboardSyncModel previousModel, DashboardSyncModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.sessionStartedDidChange -= OnSessionStartedChanged;
            previousModel.screenStateDidChange    -= OnScreenStateRemote;
            previousModel.entriesCSVDidChange     -= OnEntriesRemote;
            previousModel.demosCountDidChange     -= OnDemosRemote;
        }

        if (currentModel != null)
        {
            if (!currentModel.isFreshModel)
            {
                ApplyScreenState((ScreenState) currentModel.screenState);
                if (currentModel.sessionStarted) ApplySessionData(currentModel);
            }

            currentModel.sessionStartedDidChange += OnSessionStartedChanged;
            currentModel.screenStateDidChange    += OnScreenStateRemote;
            currentModel.entriesCSVDidChange     += OnEntriesRemote;
            currentModel.demosCountDidChange     += OnDemosRemote;
        }
    }

    private void OnSessionStartedChanged(DashboardSyncModel m, bool started)
    {
        if (started) ApplySessionData(m);
    }
    private void OnScreenStateRemote(DashboardSyncModel m, int v) => ApplyScreenState((ScreenState) v);
    private void OnEntriesRemote   (DashboardSyncModel m, string _) => OnEntriesChanged?.Invoke();
    private void OnDemosRemote     (DashboardSyncModel m, int _)    => OnEntriesChanged?.Invoke();

    // ── Per-client entry API ────────────────────────────────────────────────
    [Serializable]
    public class Entry
    {
        public int    clientID;
        public string name;
        public string cityId;
        public Entry(int id, string n, string c) { clientID = id; name = n; cityId = c; }
    }

    /// <summary>All entries currently in the synced model (read-only snapshot).</summary>
    public List<Entry> AllEntries()
        => ParseEntries(model?.entriesCSV);

    /// <summary>Find the local client's entry, or null if they haven't added one yet.</summary>
    public Entry MyEntry()
    {
        int me = LocalClientID;
        return me < 0 ? null : AllEntries().FirstOrDefault(e => e.clientID == me);
    }

    /// <summary>Add or update the local client's entry. Writes the whole CSV atomically.</summary>
    public void UpsertMyEntry(string name, string cityId)
    {
        if (model == null) return;
        int me = LocalClientID;
        if (me < 0) me = 0;   // offline / editor fallback
        var entries = ParseEntries(model.entriesCSV);
        var mine = entries.FirstOrDefault(e => e.clientID == me);
        if (mine == null)
        {
            mine = new Entry(me, name ?? "", cityId ?? "");
            entries.Add(mine);
        }
        else
        {
            mine.name   = name   ?? mine.name;
            mine.cityId = cityId ?? mine.cityId;
        }
        model.entriesCSV = SerializeEntries(entries);
    }

    /// <summary>Remove the local client's entry from the shared list.</summary>
    public void RemoveMyEntry()
    {
        if (model == null) return;
        int me = LocalClientID;
        var entries = ParseEntries(model.entriesCSV).Where(e => e.clientID != me).ToList();
        model.entriesCSV = SerializeEntries(entries);
    }

    /// <summary>Host-only: change the shared hands-on demos count.</summary>
    public void SetDemos(int demos)
    {
        if (model == null) return;
        model.demosCount = Mathf.Clamp(demos, 0, 12);
    }

    public int CurrentDemos          => model?.demosCount ?? 0;
    public ScreenState CurrentScreen => model != null ? (ScreenState) model.screenState : ScreenState.Setup;

    // ── Host actions (anyone can press but typically the meeting host does) ─
    public void StartCalculation()
    {
        if (model == null)
        {
            ApplyScreenState(ScreenState.Loading);
            return;
        }
        ApplySessionData(model);
        model.sessionStarted = true;
        model.screenState    = (int) ScreenState.Loading;
    }

    public void SetDashboard() => SetScreen(ScreenState.Dashboard);
    public void SetSetup()     => SetScreen(ScreenState.Setup);
    public void SetLoading()   => SetScreen(ScreenState.Loading);

    private void SetScreen(ScreenState s)
    {
        if (model == null) { ApplyScreenState(s); return; }
        model.screenState = (int) s;
    }

    private void ApplyScreenState(ScreenState s)
    {
        if (sceneSetup != null) sceneSetup.ShowScreen(s);
        OnScreenStateChanged?.Invoke(s);
    }

    private void ApplySessionData(DashboardSyncModel m)
    {
        var entries = ParseEntries(m.entriesCSV);
        var cities = entries.Where(e => !string.IsNullOrEmpty(e.cityId)).Select(e => e.cityId).ToList();
        var names  = entries.Where(e => !string.IsNullOrEmpty(e.cityId)).Select(e => string.IsNullOrEmpty(e.name) ? "Participant" : e.name).ToList();
        // Cities here are SustainabilityData city IDs (e.g. "tlv") — convert to display names for DataManager
        var cityNames = cities.Select(id =>
        {
            var c = SustainabilityData.CityById(id);
            return c != null ? c.name : id;
        }).ToList();
        if (DashboardDataManager.Instance != null)
            DashboardDataManager.Instance.ApplyShowcaseSession(cityNames, names, m.demosCount);
        OnEntriesChanged?.Invoke();
    }

    // ── CSV helpers ─────────────────────────────────────────────────────────
    public static List<Entry> ParseEntries(string csv)
    {
        var list = new List<Entry>();
        if (string.IsNullOrEmpty(csv)) return list;
        foreach (var row in csv.Split(';'))
        {
            if (string.IsNullOrEmpty(row)) continue;
            var p = row.Split('|');
            if (p.Length < 3) continue;
            if (!int.TryParse(p[0], out int id)) continue;
            list.Add(new Entry(id, p[1], p[2]));
        }
        return list;
    }

    public static string SerializeEntries(List<Entry> entries)
    {
        if (entries == null || entries.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0) sb.Append(';');
            var e = entries[i];
            sb.Append(e.clientID).Append('|')
              .Append((e.name   ?? "").Replace('|', '_').Replace(';', '_')).Append('|')
              .Append((e.cityId ?? "").Replace('|', '_').Replace(';', '_'));
        }
        return sb.ToString();
    }
}
