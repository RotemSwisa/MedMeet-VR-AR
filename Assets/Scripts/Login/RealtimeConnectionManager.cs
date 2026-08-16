using UnityEngine;
using Normal.Realtime;
using UnityEngine.SceneManagement;

public class RealtimeConnectionManager : MonoBehaviour
{
    private Realtime realtime;

    private void Awake()
    {
        realtime = GetComponent<Realtime>();

        if (realtime == null)
        {
            Debug.LogError("Realtime component not found on this object!");
            return;
        }

        // אנו מסתמכים על כך שביטלת את ה-Join Room On Start ב-Inspector.
        // אין צורך בקוד לביטול החיבור האוטומטי.
    }

    private void Start()
    {
        // 1. קריאת שם החדר שנשמר ב-LoginManager
        string roomName = PlayerPrefs.GetString("RoomName", "");

        // אנו קוראים גם את שם המשתמש, למרות שלא נשתמש בו בחיבור
        string playerName = PlayerPrefs.GetString("PlayerName", "Guest");

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("FATAL: No RoomName found. Returning to Login Scene.");
            // נחזור לסצנה הראשונה (נניח מספר 0)
            SceneManager.LoadScene(0);
            return;
        }

        // 2. הפעלת החיבור עם שם החדר כפרמטר
        // *** הפתרון: שימוש באוברלואד Connect(string roomName) ***
        // זה עוקף את הצורך ב-ConnectOptions הבעייתי, והוא הפונקציה הסטנדרטית ב-2.x.
        realtime.Connect(roomName);

        Debug.Log($"Connecting to custom room: {roomName} as client: {playerName}");
    }
}