using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using System.IO;
using System;
using System.Text;
using TMPro;
using System.Linq;
using UnityEngine.Android; // ספרייה לטיפול בהרשאות אנדרואיד

// --- מחלקה לשמירת נתונים עבור הגרפים ---
[System.Serializable]
public class PlayerSessionStats
{
    public string playerName;
    public string role;
    public double joinTime; // שומרים כ-double כדי להתאים לשעון השרת המדויק
    public float totalSpeechTime; // סה"כ שניות דיבור
    public bool isOnline; // *** הוספה קריטית: האם השחקן מחובר כרגע ***
}
// --- הוסף את המחלקה הזו מתחת ל-PlayerSessionStats ---
[System.Serializable]
public class StatsWrapper
{
    public List<PlayerSessionStats> statsList = new List<PlayerSessionStats>();
}

public class MeetingAuditor : MonoBehaviour
{
    public static MeetingAuditor Instance;

    [Header("Settings")]
    public RealtimeAvatarManager avatarManager;
    public Realtime realtime;

    // *** חיבור למנהל הזמן (חובה שיהיה בסצנה לחישוב זמן פגישה) ***
    public SessionTimeManager timeManager;

    [Header("UI Display")]
    public TMP_Text onScreenLog;
    public TMP_Text participantsText;
    public int maxLinesOnScreen = 6;

    // --- נתונים עבור הגרפים ---
    public Dictionary<string, PlayerSessionStats> playerStats = new Dictionary<string, PlayerSessionStats>();
    private float unitySessionStartTime;

    // --- נתונים עבור ה-CSV ---
    private Dictionary<int, string> connectedPlayers = new Dictionary<int, string>();
    private Dictionary<string, int> nameToIdMap = new Dictionary<string, int>();
    private Dictionary<int, string> playerGenders = new Dictionary<int, string>();
    private Dictionary<int, string> playerRolesMap = new Dictionary<int, string>();

    // זמני שהייה ל-CSV
    private Dictionary<int, DateTime> playerJoinTimes = new Dictionary<int, DateTime>();
    private Dictionary<int, string> historyNames = new Dictionary<int, string>();
    private Dictionary<int, TimeSpan> finalDurations = new Dictionary<int, TimeSpan>();
    private Dictionary<int, double> totalSpeechDurationCSV = new Dictionary<int, double>();

    private DateTime sessionStartTime;
    private StringBuilder csvContent = new StringBuilder();
    private string filePath;
    private List<string> screenLines = new List<string>();
    private bool isQuitting = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        sessionStartTime = DateTime.Now;
        unitySessionStartTime = Time.time;

        // בקשת הרשאות לאנדרואיד (Quest)
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
        }

        SetupCSV();
        UpdateScreenDisplay("--- System Ready ---");
        UpdateCountUI();
    }

    private void Start()
    {
        // מנסה למצוא אוטומטית את מנהל הזמן אם שכחת לגרור אותו
        if (timeManager == null) timeManager = FindObjectOfType<SessionTimeManager>();

        if (avatarManager != null)
        {
            avatarManager.avatarCreated += OnAvatarCreated;
            avatarManager.avatarDestroyed += OnAvatarDestroyed;

            if (realtime == null) realtime = FindFirstObjectByType<Realtime>();

            StartCoroutine(ScanForExistingPlayersWhenConnected());
        }
    }

    // --- פונקציות עזר עבור הגרפים ---
    // --- הוסף את הפונקציות האלו לתוך MeetingAuditor ---

    // 1. פונקציה שאוספת את כל הנתונים ושולחת לשרת
    public void SyncToCloud()
    {
        if (SessionTimeManager.Instance == null) return;

        StatsWrapper wrapper = new StatsWrapper();
        // המרת המילון לרשימה כדי שיוניטי תוכל לשמור אותו
        wrapper.statsList = new List<PlayerSessionStats>(playerStats.Values);

        string json = JsonUtility.ToJson(wrapper);

        // שליחה למנהל הזמן (שמעדכן את המודל ברשת)
        SessionTimeManager.Instance.SaveDataToCloud(json);
    }

    // 2. פונקציה שמקבלת מידע מהשרת ומעדכנת את הגרף המקומי
    public void LoadDataFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            StatsWrapper wrapper = JsonUtility.FromJson<StatsWrapper>(json);

            foreach (var stat in wrapper.statsList)
            {
                if (playerStats.ContainsKey(stat.playerName))
                {
                    // אם השחקן כבר קיים אצלי, אני מעדכן אותו רק אם המידע בענן חדש יותר (זמן דיבור גבוה יותר)
                    if (stat.totalSpeechTime > playerStats[stat.playerName].totalSpeechTime)
                    {
                        playerStats[stat.playerName] = stat;
                    }
                    // עדכון סטטוס אונליין
                    playerStats[stat.playerName].isOnline = stat.isOnline;
                }
                else
                {
                    // שחקן חדש שלא ידעתי עליו (מההיסטוריה) - מוסיף אותו
                    playerStats.Add(stat.playerName, stat);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load JSON data: {e.Message}");
        }
    }
    public Dictionary<string, PlayerSessionStats> GetAllPlayerStats()
    {
        return playerStats;
    }

    // *** פונקציה לחישוב זמן הפגישה הכולל (Total Time) ***
    public float GetTotalSessionTime()
    {
        // עדיפות 1: מנהל הזמן (מאפשר איפוס כשנכנסים לחדר נטוש)
        if (timeManager != null)
        {
            return timeManager.GetCurrentSessionTime();
        }

        // עדיפות 2: זמן השרת הכללי (גיבוי)
        if (realtime != null && realtime.connected && realtime.room != null)
        {
            return (float)realtime.room.time;
        }

        // עדיפות 3: זמן מקומי
        return Time.time - unitySessionStartTime;
    }

    // *** פונקציה לחישוב זמן שהייה של שחקן (Stay Time) ***
    public float GetPlayerStayTime(string playerName)
    {
        if (playerStats.ContainsKey(playerName))
        {
            float stayTime = 0f;

            // אם מחוברים לשרת, החישוב הוא: זמן נוכחי בשרת פחות זמן הכניסה שהשחקן דיווח
            if (realtime != null && realtime.connected && realtime.room != null)
            {
                double rawStay = realtime.room.time - playerStats[playerName].joinTime;
                stayTime = (float)Math.Max(0, rawStay);
            }
            else
            {
                // גיבוי אופליין
                stayTime = (float)(Time.time - playerStats[playerName].joinTime);
            }

            // מניעה של מצב שבו זמן השהייה גדול מזמן החדר (בגלל הפרשי שניות)
            float totalSessionTime = GetTotalSessionTime();
            if (stayTime > totalSessionTime) stayTime = totalSessionTime;

            return stayTime;
        }
        return 0f;
    }

    // --- סריקת שחקנים ---
    private IEnumerator ScanForExistingPlayersWhenConnected()
    {
        while (realtime != null && !realtime.connected) yield return null;
        yield return new WaitForSeconds(1.0f);

        foreach (var kvp in avatarManager.avatars)
        {
            ProcessAvatar(kvp.Value);
        }
        UpdateCountUI();
    }

    private void OnAvatarCreated(RealtimeAvatarManager manager, RealtimeAvatar avatar, bool isLocalAvatar)
    {
        StartCoroutine(ProcessAvatarDelayed(avatar));
    }

    private IEnumerator ProcessAvatarDelayed(RealtimeAvatar avatar)
    {
        // המתנה של 1.5 שניות כדי לוודא שכל הנתונים (תפקיד, זמן) הסתנכרנו ברשת
        yield return new WaitForSeconds(1.5f);
        if (avatar != null) ProcessAvatar(avatar);
    }

    private void ProcessAvatar(RealtimeAvatar avatar)
    {
        int id = avatar.realtimeView.ownerIDSelf;

        string detectedGender = "Unknown";
        if (HasTagRecursive(avatar.transform, "Female")) detectedGender = "Female";
        else if (HasTagRecursive(avatar.transform, "Male")) detectedGender = "Male";

        if (!playerGenders.ContainsKey(id)) playerGenders.Add(id, detectedGender);
        else if (detectedGender != "Unknown") playerGenders[id] = detectedGender;

        var nameTag = avatar.GetComponent<AvatarNameTag>();
        string name = (nameTag != null) ? nameTag.GetPlayerName() : "Loading...";
        if (!string.IsNullOrEmpty(name)) name = name.Trim();

        // 1. קריאת תפקיד
        string role = "Guest";
        var roleSync = avatar.GetComponent<AvatarRoleSync>();
        if (roleSync != null)
        {
            role = roleSync.GetRole();
        }

        // 2. קריאת זמן כניסה (מהסקריפט החדש AvatarTimeSync)
        double joinTime = 0;
        var timeSync = avatar.GetComponent<AvatarTimeSync>();

        // אם מצאנו את הסקריפט, נשתמש בו (גם אם זה 0.0)
        if (timeSync != null)
        {
            joinTime = timeSync.GetJoinTime();
        }
        else if (realtime.room != null)
        {
            // אחרת, ברירת מחדל לזמן הנוכחי של החדר
            joinTime = realtime.room.time;
        }
        else
        {
            joinTime = Time.time;
        }

        if (!playerRolesMap.ContainsKey(id)) playerRolesMap.Add(id, role);
        else playerRolesMap[id] = role;

        if (string.IsNullOrEmpty(name) || name == "Unknown" || name == "Loading...")
        {
            if (!connectedPlayers.ContainsKey(id)) connectedPlayers.Add(id, "Loading...");
        }
        else
        {
            // שליחת כל הנתונים (כולל זמן הכניסה המדויק) לרישום
            RegisterPlayer(id, name, role, joinTime);
        }
    }

    private bool HasTagRecursive(Transform parent, string tag)
    {
        if (parent.CompareTag(tag)) return true;
        foreach (Transform child in parent)
        {
            if (HasTagRecursive(child, tag)) return true;
        }
        return false;
    }

    // פונקציית גישור עבור AvatarNameTag (למנוע שגיאות קוד ישן)
    public void RegisterPlayerName(int clientID, string rawName)
    {
        string role = playerRolesMap.ContainsKey(clientID) ? playerRolesMap[clientID] : "Guest";
        double currentTime = (realtime.room != null) ? realtime.room.time : Time.time;
        RegisterPlayer(clientID, rawName, role, currentTime);
    }

    // הפונקציה הראשית לרישום שחקן
    public void RegisterPlayer(int clientID, string rawName, string role, double joinTime)
    {
        string name = rawName.Trim();

        // עדכון סטטיסטיקות בזמן אמת
        if (!playerStats.ContainsKey(name))
        {
            playerStats.Add(name, new PlayerSessionStats
            {
                playerName = name,
                role = role,
                joinTime = joinTime, // שימוש בזמן האמיתי
                totalSpeechTime = 0f,
                isOnline = true // *** סימון שהשחקן מחובר ***
            });
        }
        else
        {
            playerStats[name].role = role;
            playerStats[name].isOnline = true; // *** עדכון שהשחקן חזר ***

            // עדכון זמן כניסה - מקבלים כל עדכון שמגיע מהרשת (חיובי)
            if (joinTime >= 0)
            {
                playerStats[name].joinTime = joinTime;
            }
        }
        SyncToCloud();
        // עדכון CSV וניהול שחקנים מחוברים
        if (connectedPlayers.ContainsKey(clientID))
        {
            string oldName = connectedPlayers[clientID];
            if (oldName != name)
            {
                connectedPlayers[clientID] = name;
                historyNames[clientID] = name;

                if (!nameToIdMap.ContainsKey(name)) nameToIdMap.Add(name, clientID);
                else nameToIdMap[name] = clientID;

                if (oldName == "Loading..." || oldName.StartsWith("Unknown"))
                {
                    RecordJoinTime(clientID);
                    string gender = playerGenders.ContainsKey(clientID) ? playerGenders[clientID] : "Unknown";
                    LogEvent("CONNECTED", name, role, clientID, gender);
                }
            }
        }
        else
        {
            connectedPlayers.Add(clientID, name);
            if (!historyNames.ContainsKey(clientID)) historyNames.Add(clientID, name);
            else historyNames[clientID] = name;

            if (!nameToIdMap.ContainsKey(name)) nameToIdMap.Add(name, clientID);

            RecordJoinTime(clientID);
            string gender = playerGenders.ContainsKey(clientID) ? playerGenders[clientID] : "Unknown";
            LogEvent("CONNECTED", name, role, clientID, gender);
        }
        UpdateCountUI();
    }

    private void RecordJoinTime(int id)
    {
        if (!playerJoinTimes.ContainsKey(id)) playerJoinTimes[id] = DateTime.Now;
        if (!totalSpeechDurationCSV.ContainsKey(id)) totalSpeechDurationCSV[id] = 0.0;
    }

    private void OnAvatarDestroyed(RealtimeAvatarManager manager, RealtimeAvatar avatar, bool isLocalAvatar)
    {
        if (isQuitting) return;

        int id = avatar.realtimeView.ownerIDSelf;
        string name = connectedPlayers.ContainsKey(id) ? connectedPlayers[id] : "UnknownID_" + id;
        string gender = playerGenders.ContainsKey(id) ? playerGenders[id] : "Unknown";
        string role = playerRolesMap.ContainsKey(id) ? playerRolesMap[id] : "Unknown";

        // *** עדכון שהשחקן יצא (Offline) בסטטיסטיקה של הגרפים ***
        if (playerStats.ContainsKey(name))
        {
            playerStats[name].isOnline = false;
        }
        SyncToCloud();

        string durationString = "";
        if (playerJoinTimes.ContainsKey(id))
        {
            TimeSpan stayDuration = DateTime.Now - playerJoinTimes[id];
            durationString = $"{stayDuration.Minutes:D2}:{stayDuration.Seconds:D2}";

            if (!finalDurations.ContainsKey(id)) finalDurations[id] = stayDuration;
            else finalDurations[id] += stayDuration;

            playerJoinTimes.Remove(id);
        }

        LogEvent("DISCONNECTED", name, role, id, gender, durationString);

        if (connectedPlayers.ContainsKey(id)) connectedPlayers.Remove(id);
        if (nameToIdMap.ContainsKey(name) && nameToIdMap[name] == id) nameToIdMap.Remove(name);

        UpdateCountUI();
    }

    // --- CSV Setup ---
    private void SetupCSV()
    {
        string directory = "";
        string fileName = $"Audit_{DateTime.Now:yyyy-MM-dd_HH-mm}.csv";

#if UNITY_EDITOR
        directory = Directory.GetParent(Application.dataPath).FullName + "/MeetingLogs";
#elif UNITY_ANDROID
        directory = "/storage/emulated/0/Download";
#elif UNITY_STANDALONE
        directory = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "MeetingLogs");
#else
        directory = Application.persistentDataPath;
#endif
        filePath = Path.Combine(directory, fileName);

        if (!Directory.Exists(directory))
        {
            try { Directory.CreateDirectory(directory); } catch { }
        }

        csvContent.AppendLine("Timestamp,Event,Player Name,Role,Gender,Player ID,Details / Duration");
        WriteToFile();
        Debug.Log($"CSV Path Initialized: {filePath}");
    }

    // --- פונקציות לוג ---
    public void LogChat(string playerName, string message, float duration)
    {
        // 1. סינון דיבור קצר מדי – לא נחשב בכלל
        if (duration < 0.3f)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        string cleanMessage = message.Trim();

        // 2. סינון "הזיות" נפוצות של מודלים (תוכל להרחיב את הרשימה)
        string[] hallucinationPhrases =
        {
        "thanks for watching",
        "thank you for watching",
        "thanks for joining",
        "this concludes the meeting",
        "that concludes the meeting"
    };

        string lower = cleanMessage.ToLowerInvariant();
        foreach (var phrase in hallucinationPhrases)
        {
            if (lower.Contains(phrase))
            {
                // לא נכניס לסטטיסטיקה ולא ל‑CSV
                return;
            }
        }

        // 3. אין טקסט אמיתי → לא נחשב. (אם תרצה רק זמן בלי טקסט – תשנה כאן.)
        if (cleanMessage.Length < 3) // פחות מ‑3 תווים – נזרוק
            return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string safeMessage = cleanMessage.Replace(",", " ").Replace("\"", "'").Replace("\n", " ");
        string cleanName = playerName.Trim();

        double currentTime = (realtime.room != null) ? realtime.room.time : Time.time;

        // עדכון הסטטיסטיקות לעוגה
        if (playerStats.ContainsKey(cleanName))
        {
            playerStats[cleanName].totalSpeechTime += duration;
        }
        else
        {
            playerStats.Add(cleanName, new PlayerSessionStats
            {
                playerName = cleanName,
                role = "Guest",
                joinTime = currentTime,
                totalSpeechTime = duration,
                isOnline = true
            });
            SyncToCloud();
        }

        int foundID = -1;
        if (nameToIdMap.ContainsKey(cleanName)) foundID = nameToIdMap[cleanName];

        string gender = "Unknown";
        string role = "Unknown";

        if (foundID != -1)
        {
            if (playerGenders.ContainsKey(foundID)) gender = playerGenders[foundID];
            if (playerRolesMap.ContainsKey(foundID)) role = playerRolesMap[foundID];

            if (!totalSpeechDurationCSV.ContainsKey(foundID))
                totalSpeechDurationCSV[foundID] = 0;
            totalSpeechDurationCSV[foundID] += duration;
        }

        // 4. רק עכשיו כותבים ל‑CSV – כי יש טקסט אמיתי שעבר סינון
        string csvLine = $"{timestamp},SPEECH_TRANSCRIPT,{cleanName},{role},{gender},{foundID},{safeMessage} ({duration:F2}s)";
        csvContent.AppendLine(csvLine);
        WriteToFile();
    }

    private void LogEvent(string eventType, string playerName, string role, int id, string gender, string extraInfo = "")
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string csvLine = $"{timestamp},{eventType},{playerName},{role},{gender},{id},{extraInfo}";
        csvContent.AppendLine(csvLine);
        WriteToFile();

        string screenLine = $"[{timestamp}] {playerName} ({role}): {eventType}";
        if (extraInfo != "") screenLine += $" ({extraInfo})";
        UpdateScreenDisplay(screenLine);
    }

    private void UpdateCountUI()
    {
        if (participantsText != null)
        {
            int count = 0;
            foreach (var p in connectedPlayers) if (p.Value != "Loading...") count++;
            participantsText.text = $"Participants: {count}";
        }
    }

    public void ForceAddLineToScreen(string line)
    {
        UpdateScreenDisplay(line);
    }

    private void UpdateScreenDisplay(string newLine)
    {
        if (onScreenLog == null) return;
        screenLines.Add(newLine);
        while (screenLines.Count > maxLinesOnScreen) screenLines.RemoveAt(0);
        onScreenLog.text = string.Join("\n", screenLines);
    }

    private void WriteToFile()
    {
        try
        {
            File.WriteAllText(filePath, csvContent.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Write Error: {e.Message}");
        }
    }

    // *** פונקציית סיום: מחשבת זמן לפי מנהל הזמן המסונכרן ***
    private void OnApplicationQuit()
    {
        isQuitting = true;

        double finalSessionSeconds = 0;

        // עדיפות 1: קבלת זמן מהמנהל שמסונכרן לכולם
        if (timeManager != null)
        {
            finalSessionSeconds = timeManager.GetCurrentSessionTime();
        }
        // עדיפות 2: זמן שרת גולמי
        else if (realtime != null && realtime.connected && realtime.room != null)
        {
            finalSessionSeconds = realtime.room.time;
        }
        else
        {
            finalSessionSeconds = (DateTime.Now - sessionStartTime).TotalSeconds;
        }

        TimeSpan sessionTotalTime = TimeSpan.FromSeconds(finalSessionSeconds);

        LogEvent("SESSION_END", "Host", "Host", 0, "N/A");

        double globalTotalSpeech = 0.0;
        foreach (var kvp in totalSpeechDurationCSV) globalTotalSpeech += kvp.Value;

        csvContent.AppendLine("");
        csvContent.AppendLine("--- SESSION SUMMARY ---");
        csvContent.AppendLine($"Total Session Duration,,,,,,{sessionTotalTime.Hours:D2}:{sessionTotalTime.Minutes:D2}:{sessionTotalTime.Seconds:D2}");
        csvContent.AppendLine($"Total Session Speech Time (Sec),,,,,,{globalTotalSpeech:F2}");
        csvContent.AppendLine("");
        csvContent.AppendLine("Player Name,Role,Gender,Total Stay Time (MM:SS),Total Speech Time (Sec),% Dominance");

        foreach (var player in historyNames)
        {
            int id = player.Key;
            string name = player.Value;

            if (playerJoinTimes.ContainsKey(id))
            {
                TimeSpan currentSessionDuration = DateTime.Now - playerJoinTimes[id];
                if (!finalDurations.ContainsKey(id)) finalDurations[id] = currentSessionDuration;
                else finalDurations[id] += currentSessionDuration;
            }

            string gender = playerGenders.ContainsKey(id) ? playerGenders[id] : "Unknown";
            string role = playerRolesMap.ContainsKey(id) ? playerRolesMap[id] : "Guest";

            TimeSpan totalStay = finalDurations.ContainsKey(id) ? finalDurations[id] : TimeSpan.Zero;
            double speechTime = totalSpeechDurationCSV.ContainsKey(id) ? totalSpeechDurationCSV[id] : 0.0;

            double dominancePercent = 0.0;
            if (globalTotalSpeech > 0)
                dominancePercent = (speechTime / globalTotalSpeech) * 100.0;

            csvContent.AppendLine($"{name},{role},{gender},{totalStay:mm\\:ss},{speechTime:F2},{dominancePercent:F2}%");
        }
        WriteToFile();
    }
    // --- פונקציות עזר ל-Replay ---

    public void ClearScreenLog()
    {
        screenLines.Clear();
        if (onScreenLog != null)
        {
            onScreenLog.text = "";
        }
        Debug.Log("MeetingAuditor: ניקיתי את הלוג על המסך");
    }

    public void ClearPlayerStats()
    {
        playerStats.Clear();
        Debug.Log("MeetingAuditor: ניקיתי את playerStats");
    }
}