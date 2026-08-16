
using Normal.Realtime;
using Normal.Realtime.Serialization;

/// <summary>
/// Normcore Sync Model למצגת
/// מסנכרן את מספר השקף הנוכחי בין כל המשתתפים
/// </summary>
[RealtimeModel]
public partial class PresentationSyncModel
{
    [RealtimeProperty(1, true, true)]
    private int _currentSlide;
}
