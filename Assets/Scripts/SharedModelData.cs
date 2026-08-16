using UnityEngine; // השורה הזו חסרה לך!
using Normal.Realtime;

[RealtimeModel]
public partial class SharedModelData
{
    [RealtimeProperty(1, true)]
    private bool _isVisible;

    [RealtimeProperty(2, true)]
    private Quaternion _rotation;

    [RealtimeProperty(3, true)]
    private string _modelName;
}