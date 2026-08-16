using System.Collections.Generic;
using UnityEngine;

// משחזר אובייקט שהוקלט
public class ReplayObject : MonoBehaviour
{
    private ObjectRecordingData recordingData;
    private int currentFrame = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    private Renderer objectRenderer;
    private TMPro.TextMeshPro tmpText;
    private TextMesh textMesh;

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        tmpText = GetComponent<TMPro.TextMeshPro>();
        textMesh = GetComponent<TextMesh>();
    }

    public void Initialize(ObjectRecordingData data)
    {
        recordingData = data;
        Debug.Log($"ReplayObject: אותחל עבור {data.objectID}");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentFrame = 0;
        Debug.Log($"ReplayObject [{recordingData.objectID}]: התחלת שחזור");
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

        ObjectFrame currentFrameData = recordingData.frames[currentFrame];

        // עדכן Transform
        transform.position = currentFrameData.position;
        transform.rotation = currentFrameData.rotation;
        transform.localScale = currentFrameData.scale;

        // עדכן Visibility
        bool shouldBeVisible = currentFrameData.isVisible;

        if (objectRenderer != null)
        {
            if (objectRenderer.enabled != shouldBeVisible)
            {
                objectRenderer.enabled = shouldBeVisible;
                Debug.Log($"ReplayObject [{recordingData.objectID}]: Visibility = {shouldBeVisible} at {playbackTime:F2}");
            }
        }

        // גם את האובייקט עצמו
        if (gameObject.activeSelf != shouldBeVisible)
        {
            gameObject.SetActive(shouldBeVisible);
            Debug.Log($"ReplayObject [{recordingData.objectID}]: Active = {shouldBeVisible} at {playbackTime:F2}");
        }

        // עדכן Text
        if (!string.IsNullOrEmpty(currentFrameData.textContent))
        {
            if (tmpText != null)
                tmpText.text = currentFrameData.textContent;
            else if (textMesh != null)
                textMesh.text = currentFrameData.textContent;
        }
    }

    public bool IsPlaybackComplete()
    {
        if (recordingData == null || recordingData.frames.Count == 0) return true;
        return playbackTime >= recordingData.frames[recordingData.frames.Count - 1].time;
    }
}