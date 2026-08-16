using System.Collections.Generic;
using UnityEngine;

// ✨ מקליט את נתוני הגרף (Stats Board)
public class StatsRecorder : MonoBehaviour
{
    public static StatsRecorder Instance { get; private set; }

    [Header("Settings")]
    public float snapshotInterval = 0.5f; // דגימה כל חצי שנייה (כמו ה-refreshRate של הגרף)

    private List<StatsSnapshot> snapshots = new List<StatsSnapshot>();
    private bool isRecording = false;
    private float recordingStartTime;
    private float lastSnapshotTime = 0f;

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

    public void StartRecording()
    {
        snapshots.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastSnapshotTime = 0f;

        // הקלט snapshot ראשון
        RecordSnapshot(0f);

        Debug.Log("StatsRecorder: התחלת הקלטת נתוני Stats");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"StatsRecorder: סיום הקלטה. נרשמו {snapshots.Count} snapshots");
    }

    void Update()
    {
        if (!isRecording || MeetingAuditor.Instance == null) return;

        float currentTime = Time.time - recordingStartTime;

        // הקלט snapshot כל X שניות
        if (currentTime - lastSnapshotTime >= snapshotInterval)
        {
            RecordSnapshot(currentTime);
            lastSnapshotTime = currentTime;
        }
    }

    private void RecordSnapshot(float time)
    {
        var allStats = MeetingAuditor.Instance.GetAllPlayerStats();

        StatsSnapshot snapshot = new StatsSnapshot
        {
            time = time,
            playerStats = new List<PlayerStatsData>()
        };

        // העתק את כל נתוני השחקנים
        foreach (var kvp in allStats)
        {
            PlayerSessionStats stats = kvp.Value;

            PlayerStatsData playerData = new PlayerStatsData
            {
                playerName = stats.playerName,
                role = stats.role,
                joinTime = stats.joinTime,
                totalSpeechTime = stats.totalSpeechTime,
                isOnline = stats.isOnline
            };

            snapshot.playerStats.Add(playerData);
        }

        snapshots.Add(snapshot);
        Debug.Log($"StatsRecorder: צילום snapshot בזמן {time:F2}s עם {snapshot.playerStats.Count} שחקנים");
    }

    public List<StatsSnapshot> GetRecordedSnapshots()
    {
        return new List<StatsSnapshot>(snapshots);
    }
}