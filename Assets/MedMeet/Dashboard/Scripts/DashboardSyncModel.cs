using Normal.Realtime;

/// <summary>
/// Normcore RealtimeModel synced to every participant.
///
/// Per-client entries:
///   entriesCSV — every client writes their OWN row. Format:
///       clientID|name|cityId ; clientID|name|cityId ; …
///   Anyone may write, but the UI convention is each client only edits the
///   row tagged with their own clientID. Last-writer-wins on collisions.
///
/// Shared session controls (host writes):
///   demosCount      — hands-on demos count.
///   screenState     — 0 Setup, 1 Loading, 2 Dashboard.
///   sessionStarted  — set true when host presses "Calculate impact".
/// </summary>
[RealtimeModel]
public partial class DashboardSyncModel
{
    [RealtimeProperty(1, true, true)] private string _entriesCSV;
    [RealtimeProperty(2, true, true)] private bool   _sessionStarted;
    [RealtimeProperty(3, true, true)] private int    _screenState;
    [RealtimeProperty(4, true, true)] private int    _demosCount;
}
