using System.Collections.Generic;
using UnityEngine;

// ✨ משחזר מצגת מהקלטה
public class PresentationReplayer : MonoBehaviour
{
    private VRPresentationSystem presentationSystem;
    private List<PresentationFrame> frames;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    public void Initialize(List<PresentationFrame> presentationFrames)
    {
        frames = presentationFrames;
        presentationSystem = GetComponent<VRPresentationSystem>();

        Debug.Log($"PresentationReplayer: אותחל עם {frames.Count} פריימים");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentFrameIndex = 0;

        // הצג את השקף הראשון
        if (frames != null && frames.Count > 0 && presentationSystem != null)
        {
            presentationSystem.JumpToSlide(frames[0].slideIndex);
            Debug.Log($"PresentationReplayer: מתחיל בשקף {frames[0].slideIndex}");
        }

        Debug.Log("PresentationReplayer: התחלת שחזור");
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
        if (!isPlaying || frames == null || frames.Count == 0 || presentationSystem == null) return;

        playbackTime += Time.deltaTime;

        // בדוק אם הגענו לפריים הבא
        while (currentFrameIndex < frames.Count && frames[currentFrameIndex].time <= playbackTime)
        {
            PresentationFrame frame = frames[currentFrameIndex];

            // החלף לשקף
            presentationSystem.JumpToSlide(frame.slideIndex);

            Debug.Log($"PresentationReplayer: מעבר לשקף {frame.slideIndex} בזמן {playbackTime:F2}");

            currentFrameIndex++;
        }
    }

    public bool IsPlaybackComplete()
    {
        if (frames == null || frames.Count == 0) return true;
        return currentFrameIndex >= frames.Count;
    }
}