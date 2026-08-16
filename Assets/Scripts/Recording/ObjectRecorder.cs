using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

// מקליט אובייקטים דינמיים (מודלים, מסמכים, לוח וכו')
public class ObjectRecorder : MonoBehaviour
{
    [Header("What to Record")]
    public bool recordTransform = true;
    public bool recordVisibility = true;
    public bool recordTextContent = false;

    [Header("Object Info")]
    public string objectID;
    public string objectType = "Generic";

    private List<ObjectFrame> frames = new List<ObjectFrame>();
    private bool isRecording = false;
    private float recordingStartTime;
    private float lastFrameTime;
    public float frameInterval = 0.033f;

    private Renderer objectRenderer;
    private TMPro.TextMeshPro tmpText;
    private TextMesh textMesh;

    // ✨ חדש! תמיכה ב-Normcore
    private RealtimeTransform realtimeTransform;
    private bool hasRealtimeTransform = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"{objectType}_{gameObject.GetInstanceID()}";
        }

        objectRenderer = GetComponent<Renderer>();
        tmpText = GetComponent<TMPro.TextMeshPro>();
        textMesh = GetComponent<TextMesh>();

        // ✨ בדוק אם יש RealtimeTransform
        realtimeTransform = GetComponent<RealtimeTransform>();
        hasRealtimeTransform = (realtimeTransform != null);

        if (hasRealtimeTransform)
        {
            Debug.Log($"ObjectRecorder [{objectID}]: נמצא RealtimeTransform - אקליט ממנו!");
        }
    }

    public void StartRecording()
    {
        frames.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastFrameTime = 0f;

        Debug.Log($"ObjectRecorder: התחלת הקלטה עבור {objectID}");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"ObjectRecorder: סיום הקלטה. נשמרו {frames.Count} פריימים");
    }

    void Update()
    {
        if (!isRecording) return;

        float currentTime = Time.time - recordingStartTime;

        if (currentTime - lastFrameTime >= frameInterval)
        {
            RecordFrame(currentTime);
            lastFrameTime = currentTime;
        }
    }

    private void RecordFrame(float time)
    {
        ObjectFrame frame = new ObjectFrame
        {
            time = time,
            objectID = objectID,
            objectType = objectType
        };

        // ✨ Transform - בדוק אם יש RealtimeTransform
        if (recordTransform)
        {
            // ✨ תיקון קריטי! השתמש ב-world position/rotation תמיד!
            if (hasRealtimeTransform && realtimeTransform != null)
            {
                // עבור RealtimeTransform - קח את ה-world transform
                frame.position = transform.position;
                frame.rotation = transform.rotation;
                frame.scale = transform.localScale;
            }
            else
            {
                // עבור Transform רגיל
                frame.position = transform.position;
                frame.rotation = transform.rotation;
                frame.scale = transform.localScale;
            }
        }

        // Visibility
        if (recordVisibility)
        {
            bool isActive = gameObject.activeInHierarchy;
            bool isRendererEnabled = objectRenderer != null ? objectRenderer.enabled : true;
            frame.isVisible = isActive && isRendererEnabled;

            if (frames.Count == 0 || frames[frames.Count - 1].isVisible != frame.isVisible)
            {
                Debug.Log($"ObjectRecorder [{objectID}]: Visibility = {frame.isVisible} at {time:F2}");
            }
        }
        else
        {
            frame.isVisible = gameObject.activeSelf;
        }

        // Text Content
        if (recordTextContent)
        {
            if (tmpText != null)
                frame.textContent = tmpText.text;
            else if (textMesh != null)
                frame.textContent = textMesh.text;
        }

        frames.Add(frame);
    }

    // ✨ חדש! הקלטה מיידית של Visibility
    public void RecordVisibilityChange()
    {
        if (!isRecording) return;

        float currentTime = Time.time - recordingStartTime;
        RecordFrame(currentTime);

        Debug.Log($"ObjectRecorder [{objectID}]: Recorded immediate visibility change at {currentTime:F2}");
    }

    public List<ObjectFrame> GetRecordingData()
    {
        return new List<ObjectFrame>(frames);
    }
}