using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class AvatarRoleModel
{
    [RealtimeProperty(1, true, true)]
    private string _role;
}