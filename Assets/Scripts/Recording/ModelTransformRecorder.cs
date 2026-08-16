using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

// ✨ מקליט מודלים עם RealtimeTransform בצורה נכונה!
public class ModelTransformRecorder : MonoBehaviour
{
    public string modelName; // "Heart", "Lungs", "Brain"

    private List<TransformFrame> frames = new List<TransformFrame>();
    private bool isRecording = false;
    private float recordingStartTime;
    private float lastFrameTime;
    private float frameInterval = 0.0333f; // 30 FPS

    private RealtimeTransform realtimeTransform;

    void Awake()
    {
        realtimeTransform = GetComponent<RealtimeTransform>();

        if (realtimeTransform == null)
        {
            Debug.LogWarning($"ModelTransformRecorder: אין RealtimeTransform על {gameObject.name}!");
        }
    }

    public void StartRecording()
    {
        frames.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastFrameTime = 0f;

        Debug.Log($"ModelTransformRecorder: התחלת הקלטה עבור {modelName}");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"ModelTransformRecorder: סיום הקלטה. נשמרו {frames.Count} פריימים עבור {modelName}");
    }

    void LateUpdate() // ✨ LateUpdate כדי לקרוא אחרי RealtimeTransform!
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
        TransformFrame frame = new TransformFrame
        {
            time = time,
            position = transform.position, // ✨ World position!
            rotation = transform.rotation  // ✨ World rotation!
        };

        frames.Add(frame);
    }

    public ModelRecordingData GetRecordingData()
    {
        return new ModelRecordingData
        {
            modelName = modelName,
            frames = new List<TransformFrame>(frames)
        };
    }
}

// ✨ מבנה נתונים למודל
[System.Serializable]
public class ModelRecordingData
{
    public string modelName;
    public List<TransformFrame> frames = new List<TransformFrame>();
}