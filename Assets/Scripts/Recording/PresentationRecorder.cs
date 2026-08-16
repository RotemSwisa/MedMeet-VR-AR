using System.Collections.Generic;
using UnityEngine;

// ✨ מקליט שינויים במצגת
public class PresentationRecorder : MonoBehaviour
{
    private VRPresentationSystem presentationSystem;
    private List<PresentationFrame> frames = new List<PresentationFrame>();
    private bool isRecording = false;
    private float recordingStartTime;
    private int lastRecordedSlide = -1;

    void Awake()
    {
        presentationSystem = GetComponent<VRPresentationSystem>();
    }

    public void StartRecording()
    {
        frames.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastRecordedSlide = -1;

        // הקלט את השקף הנוכחי
        RecordCurrentSlide();

        Debug.Log("PresentationRecorder: התחלת הקלטה");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"PresentationRecorder: סיום הקלטה. נשמרו {frames.Count} פריימים");
    }

    void Update()
    {
        if (!isRecording || presentationSystem == null) return;

        // ✨ גישה דרך הפונקציה הציבורית
        int currentSlide = presentationSystem.GetCurrentSlideIndex();

        if (currentSlide != lastRecordedSlide)
        {
            RecordCurrentSlide();
        }
    }

    private void RecordCurrentSlide()
    {
        if (presentationSystem == null) return;

        float time = Time.time - recordingStartTime;
        int currentSlide = presentationSystem.GetCurrentSlideIndex();

        PresentationFrame frame = new PresentationFrame
        {
            time = time,
            slideIndex = currentSlide
        };

        frames.Add(frame);
        lastRecordedSlide = currentSlide;

        Debug.Log($"PresentationRecorder: הקלטתי שקף {currentSlide} בזמן {time:F2}");
    }

    public List<PresentationFrame> GetRecordingData()
    {
        return new List<PresentationFrame>(frames);
    }
}

// ✨ מבנה נתונים לפריים מצגת
[System.Serializable]
public class PresentationFrame
{
    public float time;
    public int slideIndex;
}