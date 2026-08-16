using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class AvatarTimeModel
{
    // משתנה שיחזיק את זמן השרת ברגע הכניסה
    [RealtimeProperty(1, true, true)]
    private double _joinTime;
}