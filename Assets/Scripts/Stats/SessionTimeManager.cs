using UnityEngine;
using Normal.Realtime;
using System.Collections;

public class SessionTimeManager : RealtimeComponent<SessionTimeModel>
{
    public static SessionTimeManager Instance;

    [Header("References")]
    public RealtimeAvatarManager avatarManager;
    public Realtime realtime;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (realtime != null)
        {
            realtime.didConnectToRoom += OnDidConnectToRoom;
        }
    }

    // --- חיבור למודל של Normcore ---
    protected override void OnRealtimeModelReplaced(SessionTimeModel previousModel, SessionTimeModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.sessionStatsJsonDidChange -= OnServerDataChanged;
        }

        if (currentModel != null)
        {
            if (currentModel.isFreshModel)
            {
                // בדוק שה-room קיים לפני גישה ל-time
                if (realtime != null && realtime.room != null)
                {
                    currentModel.startTime = realtime.room.time;
                }
                currentModel.sessionStatsJson = "";
            }

            if (!string.IsNullOrEmpty(currentModel.sessionStatsJson))
            {
                if (MeetingAuditor.Instance != null)
                    MeetingAuditor.Instance.LoadDataFromJson(currentModel.sessionStatsJson);
            }

            currentModel.sessionStatsJsonDidChange += OnServerDataChanged;
        }
    }

    // פונקציה שנקראת אוטומטית כשהשרת מעדכן את המידע
    private void OnServerDataChanged(SessionTimeModel model, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            // עדכון ה-MeetingAuditor המקומי בנתונים החדשים שהגיעו
            MeetingAuditor.Instance.LoadDataFromJson(value);
        }
    }

    // פונקציה ש-MeetingAuditor יקרא לה כדי לשמור שינויים לענן
    public void SaveDataToCloud(string json)
    {
        if (model != null)
        {
            model.sessionStatsJson = json;
        }
    }

    // --- לוגיקת זמן (הקוד הישן שלך) ---
    private void OnDidConnectToRoom(Realtime room)
    {
        StartCoroutine(CheckIfFirstPlayer());
    }

    private IEnumerator CheckIfFirstPlayer()
    {
        yield return new WaitForSeconds(1.0f);

        if (avatarManager.avatars.Count <= 1)
        {
            Debug.Log("👑 I am the first player! Resetting session timer.");
            if (model != null) model.startTime = realtime.room.time;
        }
        else
        {
            Debug.Log($"👥 Join existing session. Players found: {avatarManager.avatars.Count}");
            // אם הצטרפנו, אנחנו מושכים את המידע מהמודל (קורה ב-OnRealtimeModelReplaced)
        }
    }

    public float GetCurrentSessionTime()
    {
        if (model == null || realtime == null || realtime.room == null) return 0f;
        double duration = realtime.room.time - model.startTime;
        return (float)System.Math.Max(0, duration);
    }
}