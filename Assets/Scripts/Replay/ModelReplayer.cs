using System.Collections.Generic;
using UnityEngine;

// ✨ משחזר תנועות של מודלים מוקלטים (Heart, Lungs, Brain)
public class ModelReplayer : MonoBehaviour
{
    private ModelRecordingData recordingData;
    private int currentFrame = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    public void Initialize(ModelRecordingData data)
    {
        recordingData = data;
        Debug.Log($"ModelReplayer: אותחל עבור {data.modelName} עם {data.frames.Count} פריימים");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentFrame = 0;
        Debug.Log($"ModelReplayer [{recordingData.modelName}]: התחלת שחזור");
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
        if (!isPlaying || recordingData == null || recordingData.frames.Count == 0) return;

        playbackTime += Time.deltaTime;

        // מצא את הפריים הנוכחי
        while (currentFrame < recordingData.frames.Count - 1 &&
               recordingData.frames[currentFrame + 1].time <= playbackTime)
        {
            currentFrame++;
        }

        if (currentFrame >= recordingData.frames.Count) return;

        TransformFrame currentFrameData = recordingData.frames[currentFrame];

        // ✨ אינטרפולציה חלקה בין פריימים
        if (currentFrame < recordingData.frames.Count - 1)
        {
            TransformFrame nextFrame = recordingData.frames[currentFrame + 1];
            float t = Mathf.InverseLerp(currentFrameData.time, nextFrame.time, playbackTime);

            transform.position = Vector3.Lerp(currentFrameData.position, nextFrame.position, t);
            transform.rotation = Quaternion.Slerp(currentFrameData.rotation, nextFrame.rotation, t);
        }
        else
        {
            // פריים אחרון
            transform.position = currentFrameData.position;
            transform.rotation = currentFrameData.rotation;
        }
    }

    public bool IsPlaybackComplete()
    {
        if (recordingData == null || recordingData.frames.Count == 0) return true;
        return playbackTime >= recordingData.frames[recordingData.frames.Count - 1].time;
    }
}