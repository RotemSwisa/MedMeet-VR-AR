using UnityEngine;
using Normal.Realtime;

// מנהל מעבר בין Recording Mode ל-Replay Mode
public class ReplaySceneManager : MonoBehaviour
{
    public static ReplaySceneManager Instance { get; private set; }

    [Header("Mode Settings")]
    public bool isReplayMode = false;

    [Header("References")]
    public Realtime realtime;
    public string savedRoomName;
    public GameObject multiplayerObjects;
    public FreeRoamController freeRoamController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (realtime != null && realtime.connected)
        {
            savedRoomName = PlayerPrefs.GetString("RoomName", "DefaultRoom");
        }

        if (isReplayMode)
        {
            EnterReplayMode();
        }
    }

    public void EnterReplayMode()
    {
        isReplayMode = true;

        Debug.Log("ReplaySceneManager: מתחיל כניסה ל-Replay Mode");

        // ✅ שלב 1: כבה את RealtimeAvatarManager מיד!
        RealtimeAvatarManager avatarManager = realtime?.GetComponent<RealtimeAvatarManager>();
        if (avatarManager != null)
        {
            avatarManager.enabled = false;
            Debug.Log("ReplaySceneManager: כיביתי את RealtimeAvatarManager");
        }

        // ✅ שלב 2: התנתק מ-Normcore
        if (realtime != null && realtime.connected)
        {
            realtime.Disconnect();
            Debug.Log("ReplaySceneManager: התנתקות מ-Normcore");
        }

        // ✅ שלב 3: מחק את כל האווטרים הקיימים מיד (לא Coroutine!)
        DestroyAllExistingAvatars();

        // ✅ שלב 4: הפעל FreeRoam
        if (freeRoamController != null)
        {
            freeRoamController.EnableFreeRoam();
        }

        Debug.Log("ReplaySceneManager: סיימתי כניסה ל-Replay Mode");
    }

    // ✨ תיקון: מחיקה מיידית (לא Coroutine) של אווטרים קיימים
    private void DestroyAllExistingAvatars()
    {
        // מצא את כל ה-RealtimeAvatars שכבר קיימים
        RealtimeAvatar[] existingAvatars = FindObjectsOfType<RealtimeAvatar>();

        Debug.Log($"ReplaySceneManager: מצאתי {existingAvatars.Length} אווטרים קיימים למחיקה");

        foreach (RealtimeAvatar avatar in existingAvatars)
        {
            Debug.Log($"ReplaySceneManager: מוחק אווטר קיים - {avatar.gameObject.name}");
            Destroy(avatar.gameObject);
        }

        Debug.Log("ReplaySceneManager: כל האווטרים הקיימים נמחקו!");
    }

    public void ExitReplayMode()
    {
        isReplayMode = false;

        // כבה FreeRoam
        if (freeRoamController != null)
        {
            freeRoamController.DisableFreeRoam();
        }

        // הפעל בחזרה את RealtimeAvatarManager
        RealtimeAvatarManager avatarManager = realtime?.GetComponent<RealtimeAvatarManager>();
        if (avatarManager != null)
        {
            avatarManager.enabled = true;
            Debug.Log("ReplaySceneManager: הפעלתי את RealtimeAvatarManager בחזרה");
        }

        // התחבר בחזרה ל-Normcore
        if (realtime != null && !realtime.connected)
        {
            string roomToReconnect = !string.IsNullOrEmpty(savedRoomName)
                ? savedRoomName
                : PlayerPrefs.GetString("RoomName", "DefaultRoom");
            realtime.Connect(roomToReconnect);
            Debug.Log($"ReplaySceneManager: התחברות חזרה ל-{roomToReconnect}");
        }

        // עצור Replay אם פעיל
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StopPlayback();
        }

        Debug.Log("ReplaySceneManager: יצאנו מ-Replay Mode");
    }

    public void LoadReplayAndEnter(string recordingFileName)
    {
        EnterReplayMode();

        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.LoadAndPlayRecording(recordingFileName);
        }
    }

    // ✨ חדש! פונקציה שה-ReplayManager יכול לקרוא לה אחרי יצירת אווטרים
    public void OnReplayAvatarsCreated()
    {
        Debug.Log("ReplaySceneManager: אווטרי Replay נוצרו בהצלחה!");
    }
}