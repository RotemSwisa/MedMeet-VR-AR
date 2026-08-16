using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel] // הוספת האטריביוט הזה חשובה
public class AvatarNameModel : RealtimeModel
{
    // אתחול כרמז ריק למניעת Null Reference
    private string _playerName = "";
    private int _playerNamePropertyID = 1;

    public delegate void PlayerNameChanged(AvatarNameModel model, string value);
    public event PlayerNameChanged playerNameDidChange;

    public string playerName
    {
        get { return _playerName; }
        set
        {
            if (_playerName == value) return;
            _playerName = value;
            InvalidateReliableLength();
            FirePlayerNameDidChange(value);
        }
    }

    // --- הקוד שלך למטה נראה תקין, השארתי אותו ---
    protected override int WriteLength(StreamContext context)
    {
        return WriteStream.WriteStringLength((uint)_playerNamePropertyID, _playerName);
    }

    protected override void Write(WriteStream stream, StreamContext context)
    {
        stream.WriteString((uint)_playerNamePropertyID, _playerName);
    }

    protected override void Read(ReadStream stream, StreamContext context)
    {
        uint propertyID;
        while (stream.ReadNextPropertyID(out propertyID))
        {
            if (propertyID == _playerNamePropertyID)
            {
                string value = stream.ReadString();
                if (_playerName != value)
                {
                    _playerName = value;
                    FirePlayerNameDidChange(value);
                }
            }
            else
            {
                stream.SkipProperty();
            }
        }
    }

    private void FirePlayerNameDidChange(string value)
    {
        if (playerNameDidChange != null)
            playerNameDidChange(this, value);
    }
}