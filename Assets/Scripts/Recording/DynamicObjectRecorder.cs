using System.Collections.Generic;
using UnityEngine;

// מקליט אירועי Spawn/Destroy של אובייקטים דינמיים + תמונות + ציורים
public class DynamicObjectRecorder : MonoBehaviour
{
    public static DynamicObjectRecorder Instance { get; private set; }

    private List<DynamicObjectEvent> events = new List<DynamicObjectEvent>();
    private bool isRecording = false;
    private float recordingStartTime;

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
        events.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        Debug.Log("DynamicObjectRecorder: התחלת הקלטה");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"DynamicObjectRecorder: סיום הקלטה. נרשמו {events.Count} אירועים");
    }

    // קרא לזה כשמייצרים אובייקט רגיל (מודל וכו')
    public void RecordSpawn(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (!isRecording) return;

        float time = Time.time - recordingStartTime;

        DynamicObjectEvent evt = new DynamicObjectEvent
        {
            time = time,
            eventType = DynamicEventType.Spawn,
            prefabName = prefabName,
            position = position,
            rotation = rotation
        };

        events.Add(evt);
        Debug.Log($"DynamicObjectRecorder: Spawn '{prefabName}' at {time:F2}s");
    }

    // קרא לזה כשמעלים תמונה/PDF
    public void RecordDocumentSpawn(Texture2D texture, string fileName, bool isPDF, int pageCount, Vector3 position, Quaternion rotation)
    {
        if (!isRecording) return;

        float time = Time.time - recordingStartTime;

        // המרת Texture ל-Base64
        byte[] pngBytes = texture.EncodeToPNG();
        string base64 = System.Convert.ToBase64String(pngBytes);

        DynamicObjectEvent evt = new DynamicObjectEvent
        {
            time = time,
            eventType = DynamicEventType.DocumentSpawn,
            prefabName = "DocumentPrefab",
            position = position,
            rotation = rotation,
            textureBase64 = base64,
            fileName = fileName,
            isPDF = isPDF,
            pageCount = pageCount
        };

        events.Add(evt);
        Debug.Log($"DynamicObjectRecorder: DocumentSpawn '{fileName}' (PDF: {isPDF}, Pages: {pageCount}) at {time:F2}s");
    }

    // ✨ חדש! הקלטת נקודת ציור
    public void RecordDrawingDot(Vector3 position, Color color, float size, int strokeID, int pointIndex)
    {
        if (!isRecording) return;

        float time = Time.time - recordingStartTime;

        DynamicObjectEvent evt = new DynamicObjectEvent
        {
            time = time,
            eventType = DynamicEventType.DrawingDotSpawn,
            prefabName = "DrawingDot",
            position = position,
            rotation = Quaternion.identity,
            strokeID = strokeID,
            pointIndex = pointIndex,
            dotColor = color,
            dotSize = size
        };

        events.Add(evt);
        Debug.Log($"DynamicObjectRecorder: DrawingDot S{strokeID}_P{pointIndex} at {time:F2}s");
    }

    // קרא לזה כשמוחקים אובייקט
    public void RecordDestroy(string prefabName)
    {
        if (!isRecording) return;

        float time = Time.time - recordingStartTime;

        DynamicObjectEvent evt = new DynamicObjectEvent
        {
            time = time,
            eventType = DynamicEventType.Destroy,
            prefabName = prefabName
        };

        events.Add(evt);
        Debug.Log($"DynamicObjectRecorder: Destroy '{prefabName}' at {time:F2}s");
    }

    public List<DynamicObjectEvent> GetRecordedEvents()
    {
        return new List<DynamicObjectEvent>(events);
    }

    public bool IsRecording => isRecording;
}