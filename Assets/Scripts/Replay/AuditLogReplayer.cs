using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ✨ משחזר את לוג ההתחברויות והתמלול
public class AuditLogReplayer : MonoBehaviour
{
    private List<AuditLogFrame> logFrames;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    private TMP_Text onScreenLog;
    private List<string> displayedLines = new List<string>();
    private int maxLines = 6;

    public void Initialize(List<AuditLogFrame> frames)
    {
        logFrames = frames;
        Debug.Log($"AuditLogReplayer: אותחל עם {logFrames.Count} שורות לוג");

        FindAuditLogText();
    }

    private void FindAuditLogText()
    {
        GameObject auditObj = GameObject.Find("Text (Audit)");
        if (auditObj != null)
        {
            onScreenLog = auditObj.GetComponent<TMP_Text>();
            if (onScreenLog != null)
            {
                Debug.Log("AuditLogReplayer: נמצא Text (Audit)");
            }
            else
            {
                Debug.LogWarning("AuditLogReplayer: Text (Audit) לא מכיל TMP_Text!");
            }
        }
        else
        {
            Debug.LogWarning("AuditLogReplayer: לא נמצא Text (Audit) בסצנה!");
        }
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentFrameIndex = 0;
        displayedLines.Clear();

        // ✨ תיקון: נקה את screenLines ב-MeetingAuditor!
        if (MeetingAuditor.Instance != null)
        {
            ClearMeetingAuditorLog();
        }

        // נקה את הטקסט על המסך
        if (onScreenLog != null)
        {
            onScreenLog.text = "";
        }

        Debug.Log("AuditLogReplayer: התחלת שחזור לוג (מאופס!)");
    }

    private void ClearMeetingAuditorLog()
    {
        // גישה ל-screenLines דרך Reflection
        var auditor = MeetingAuditor.Instance;
        var field = auditor.GetType().GetField("screenLines",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var lines = field.GetValue(auditor) as List<string>;
            if (lines != null)
            {
                lines.Clear();
                Debug.Log("AuditLogReplayer: ניקיתי את screenLines ב-MeetingAuditor");
            }
        }
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
        if (!isPlaying || logFrames == null || logFrames.Count == 0) return;

        playbackTime += Time.deltaTime;

        while (currentFrameIndex < logFrames.Count && logFrames[currentFrameIndex].time <= playbackTime)
        {
            AddLogLine(logFrames[currentFrameIndex].logLine);
            currentFrameIndex++;
        }
    }

    private void AddLogLine(string line)
    {
        displayedLines.Add(line);

        while (displayedLines.Count > maxLines)
        {
            displayedLines.RemoveAt(0);
        }

        if (onScreenLog != null)
        {
            onScreenLog.text = string.Join("\n", displayedLines);
        }
    }

    public bool IsPlaybackComplete()
    {
        if (logFrames == null || logFrames.Count == 0) return true;
        return currentFrameIndex >= logFrames.Count;
    }
}