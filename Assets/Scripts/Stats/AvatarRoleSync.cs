using UnityEngine;
using Normal.Realtime;

public class AvatarRoleSync : RealtimeComponent<AvatarRoleModel>
{
    private string _localRole;

    private void Start()
    {
        // אם זה השחקן המקומי (אני), טען את התפקיד מהזיכרון ושדר אותו לרשת
        if (realtimeView.isOwnedLocallyInHierarchy)
        {
            _localRole = PlayerPrefs.GetString("PlayerRole", "Guest");
            model.role = _localRole;
        }
    }

    // פונקציה שה-Auditor ישתמש בה כדי לקרוא את התפקיד
    public string GetRole()
    {
        return model.role;
    }
}