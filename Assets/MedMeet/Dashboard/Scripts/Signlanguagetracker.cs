using UnityEngine;

/// <summary>
/// גשר בין SignLanguagePlayer הקיים לבין DashboardDataManager.
/// הצמד את הסקריפט הזה לאותו GameObject שיש עליו SignLanguagePlayer.
///
/// איך לחבר:
///   1. גרור את הסקריפט הזה לאותו GameObject שעליו SignLanguagePlayer.
///   2. אין צורך בשינויים בכפתור — הוא ממשיך לקרוא ל-SignLanguagePlayer.ToggleSignLanguage().
///   3. הסקריפט הזה מאזין ל-isActive ומעדכן את הדשבורד אוטומטית.
/// </summary>
public class SignLanguageTracker : MonoBehaviour
{
    // נשלף אוטומטית מה-GameObject
    private SignLanguagePlayer _player;
    private bool _wasActive;

    void Awake()
    {
        _player = GetComponent<SignLanguagePlayer>();
        if (_player == null)
            Debug.LogError("SignLanguageTracker: לא נמצא SignLanguagePlayer על אותו GameObject!");
    }

    void Update()
    {
        if (_player == null) return;

        bool isNowActive = _player.isActive;

        // זיהוי שינוי מצב → עדכן DataManager
        if (isNowActive != _wasActive)
        {
            _wasActive = isNowActive;
            if (DashboardDataManager.Instance != null)
                DashboardDataManager.Instance.SetSignLanguageActive(isNowActive);
        }

        // צבירת זמן שניות כשפעיל
        if (isNowActive && DashboardDataManager.Instance != null)
            DashboardDataManager.Instance.AddSignLanguageSeconds(Time.deltaTime);
    }
}