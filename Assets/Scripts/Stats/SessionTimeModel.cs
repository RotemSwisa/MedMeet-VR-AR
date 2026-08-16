using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class SessionTimeModel
{
    // 1. משתנה זמן התחלה (היה קיים)
    [RealtimeProperty(1, true, true)]
    private double _startTime;

    // 2. משתנה חדש: שומר את כל נתוני הגרף (Stats) כטקסט JSON
    // ChangeType.Unreliable = שולח רק את האחרון (טוב לביצועים), אבל לטקסט ארוך עדיף Reliable
    [RealtimeProperty(2, true, true)]
    private string _sessionStatsJson;
}