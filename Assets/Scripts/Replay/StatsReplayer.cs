using System.Collections.Generic;
using UnityEngine;

// ✨ משחזר את נתוני הגרף (Stats Board)
public class StatsReplayer : MonoBehaviour
{
    private List<StatsSnapshot> snapshots;
    private int currentSnapshotIndex = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    public void Initialize(List<StatsSnapshot> recordedSnapshots)
    {
        snapshots = recordedSnapshots;
        Debug.Log($"StatsReplayer: אותחל עם {snapshots.Count} snapshots");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentSnapshotIndex = 0;

        // ✨ תיקון: נקה את הנתונים הקיימים!
        if (MeetingAuditor.Instance != null)
        {
            MeetingAuditor.Instance.playerStats.Clear();
            Debug.Log("StatsReplayer: ניקיתי את playerStats (מאופס!)");
        }

        Debug.Log("StatsReplayer: התחלת שחזור Stats");
    }

    public void StopPlayback()
    {
        isPlaying = false;
    }

    public void PausePlayback()
    {
        isPlaying = false;
    }

    public void ResumePlayback()
    {
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || snapshots == null || snapshots.Count == 0) return;
        if (MeetingAuditor.Instance == null) return;

        playbackTime += Time.deltaTime;

        // עדכן לsnapshot הנכון
        while (currentSnapshotIndex < snapshots.Count - 1 &&
               snapshots[currentSnapshotIndex + 1].time <= playbackTime)
        {
            currentSnapshotIndex++;
        }

        if (currentSnapshotIndex >= snapshots.Count) return;

        // החלף את הנתונים ב-MeetingAuditor
        ApplySnapshot(snapshots[currentSnapshotIndex]);
    }

    private void ApplySnapshot(StatsSnapshot snapshot)
    {
        var auditor = MeetingAuditor.Instance;

        // עדכן את playerStats
        auditor.playerStats.Clear();

        foreach (PlayerStatsData data in snapshot.playerStats)
        {
            PlayerSessionStats stats = new PlayerSessionStats
            {
                playerName = data.playerName,
                role = data.role,
                joinTime = data.joinTime,
                totalSpeechTime = data.totalSpeechTime,
                isOnline = data.isOnline
            };

            auditor.playerStats[data.playerName] = stats;
        }

        // הגרף (MeetingStatsDisplay) יתעדכן אוטומטית כי הוא קורא מ-MeetingAuditor.GetAllPlayerStats()
    }

    public bool IsPlaybackComplete()
    {
        if (snapshots == null || snapshots.Count == 0) return true;
        return currentSnapshotIndex >= snapshots.Count - 1;
    }
}