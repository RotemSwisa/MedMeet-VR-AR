using UnityEngine;
using Normal.Realtime;

public class AvatarTimeSync : RealtimeComponent<AvatarTimeModel>
{
    private void Start()
    {
        // רק הבעלים של האווטאר קובע את זמן הכניסה שלו
        if (realtimeView.isOwnedLocallyInHierarchy)
        {
            // אנחנו שומרים את זמן החדר הנוכחי
            model.joinTime = realtime.room.time;
        }
    }

    public double GetJoinTime()
    {
        if (model != null) return model.joinTime;
        return 0;
    }
}