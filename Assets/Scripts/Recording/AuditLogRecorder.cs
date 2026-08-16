using System.Collections.Generic;
using UnityEngine;

// ✨ מקליט את לוג ההתחברויות והתמלול
public class AuditLogRecorder : MonoBehaviour
{
    public static AuditLogRecorder Instance { get; private set; }

    private List<AuditLogFrame> logFrames = new List<AuditLogFrame>();
    private bool isRecording = false;
    private float recordingStartTime;

    // שמירת מצב קודם כדי לזהות שינויים
    private int lastLineCount = 0;

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
        logFrames.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastLineCount = 0;

        // אם יש לוג קיים, הקלט את כל השורות הנוכחיות
        if (MeetingAuditor.Instance != null)
        {
            var currentLines = GetCurrentLogLines();
            foreach (string line in currentLines)
            {
                RecordLogLine(line, 0f); // זמן 0 - שורות שהיו לפני ההקלטה
            }
        }

        Debug.Log("AuditLogRecorder: התחלת הקלטת לוג");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"AuditLogRecorder: סיום הקלטה. נרשמו {logFrames.Count} שורות");
    }

    void Update()
    {
        if (!isRecording || MeetingAuditor.Instance == null) return;

        // בדוק אם נוספו שורות חדשות
        var currentLines = GetCurrentLogLines();
        if (currentLines.Count > lastLineCount)
        {
            // הקלט את השורות החדשות
            float currentTime = Time.time - recordingStartTime;
            for (int i = lastLineCount; i < currentLines.Count; i++)
            {
                RecordLogLine(currentLines[i], currentTime);
            }
            lastLineCount = currentLines.Count;
        }
    }

    private void RecordLogLine(string line, float time)
    {
        AuditLogFrame frame = new AuditLogFrame
        {
            time = time,
            logLine = line
        };

        logFrames.Add(frame);
        Debug.Log($"AuditLogRecorder: רשמתי שורה בזמן {time:F2}s: {line}");
    }

    // קבלת השורות הנוכחיות מה-MeetingAuditor
    private List<string> GetCurrentLogLines()
    {
        // גישה ל-screenLines דרך Reflection (כי הם private)
        var auditor = MeetingAuditor.Instance;
        var field = auditor.GetType().GetField("screenLines",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var lines = field.GetValue(auditor) as List<string>;
            return lines ?? new List<string>();
        }

        return new List<string>();
    }

    public List<AuditLogFrame> GetRecordedLog()
    {
        return new List<AuditLogFrame>(logFrames);
    }
}