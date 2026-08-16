using System.Collections.Generic;
using UnityEngine;

// משחזר תנועות של שחקן מוקלט
public class ReplayAvatar : MonoBehaviour
{
    [Header("References")]
    public Transform bodyTransform;
    public Transform headTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform nameCanvas;
    public TMPro.TextMeshProUGUI nameTextTMP;
    public AudioSource audioSource;

    private PlayerRecordingData recordingData;
    private int currentBodyFrame = 0;
    private int currentHeadFrame = 0;
    private int currentLeftHandFrame = 0;
    private int currentRightHandFrame = 0;
    private int currentNameCanvasFrame = 0;
    private int currentAudioFrame = 0;

    private bool isPlaying = false;
    private float playbackTime = 0f;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    public void Initialize(PlayerRecordingData data, GameObject avatarPrefab)
    {
        recordingData = data;

        // ✨ תיקון: אל תיצור Instantiate! חפש בעצמו!
        FindTransformsInModel(transform); // ← חפש ב-transform עצמו, לא ב-Prefab חדש!

        // עדכן את השם ב-Canvas
        if (nameTextTMP != null)
        {
            nameTextTMP.text = data.playerName;
            Debug.Log($"ReplayAvatar: עדכן שם ל-{data.playerName}");
        }
        else
        {
            Debug.LogWarning($"ReplayAvatar: לא נמצא TextMeshPro לעדכון שם!");
        }
    }

    private void FindTransformsInModel(Transform model)
    {
        // Body = ה-Root של המודל
        if (bodyTransform == null)
            bodyTransform = model; // זה עצמו!

        // חפש בכל הילדים
        Transform[] allChildren = model.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            string childName = child.name.ToLower();

            // Head
            if (headTransform == null && childName.Contains("head"))
            {
                headTransform = child;
                Debug.Log($"ReplayAvatar: מצא Head = {child.name}");
            }

            // Left Hand
            else if (leftHandTransform == null && childName.Contains("left") && childName.Contains("hand"))
            {
                leftHandTransform = child;
                Debug.Log($"ReplayAvatar: מצא Left Hand = {child.name}");
            }

            // Right Hand
            else if (rightHandTransform == null && childName.Contains("right") && childName.Contains("hand"))
            {
                rightHandTransform = child;
                Debug.Log($"ReplayAvatar: מצא Right Hand = {child.name}");
            }

            // Canvas
            else if (nameCanvas == null && childName == "canvas")
            {
                nameCanvas = child;
                Debug.Log($"ReplayAvatar: מצא Canvas = {child.name}");
            }
        }

        // אם נמצא Canvas, חפש TextMeshPro
        if (nameCanvas != null)
        {
            nameTextTMP = nameCanvas.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (nameTextTMP != null)
            {
                Debug.Log($"ReplayAvatar: מצא TextMeshPro");
            }
            else
            {
                Debug.LogWarning($"ReplayAvatar: לא נמצא TextMeshPro ב-Canvas!");
            }
        }

        // Debug - הדפס מה נמצא
        Debug.Log($"ReplayAvatar Summary for {(recordingData != null ? recordingData.playerName : "Unknown")}:");
        Debug.Log($"  Body: {(bodyTransform != null ? bodyTransform.name : "NULL")}");
        Debug.Log($"  Head: {(headTransform != null ? headTransform.name : "NULL")}");
        Debug.Log($"  Left: {(leftHandTransform != null ? leftHandTransform.name : "NULL")}");
        Debug.Log($"  Right: {(rightHandTransform != null ? rightHandTransform.name : "NULL")}");
        Debug.Log($"  Canvas: {(nameCanvas != null ? nameCanvas.name : "NULL")}");
    }


    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentBodyFrame = 0;
        currentHeadFrame = 0;
        currentLeftHandFrame = 0;
        currentRightHandFrame = 0;
        currentNameCanvasFrame = 0;
        currentAudioFrame = 0;

        Debug.Log($"ReplayAvatar: התחלת שחזור עבור {recordingData.playerName}");
    }

    public void StopPlayback()
    {
        isPlaying = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
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
        if (!isPlaying) return;

        playbackTime += Time.deltaTime;

        UpdateTransform(recordingData.bodyFrames, ref currentBodyFrame, bodyTransform);
        UpdateTransform(recordingData.headFrames, ref currentHeadFrame, headTransform);
        UpdateTransform(recordingData.leftHandFrames, ref currentLeftHandFrame, leftHandTransform);
        UpdateTransform(recordingData.rightHandFrames, ref currentRightHandFrame, rightHandTransform);

        if (recordingData.nameCanvasFrames != null && recordingData.nameCanvasFrames.Count > 0)
        {
            UpdateTransform(recordingData.nameCanvasFrames, ref currentNameCanvasFrame, nameCanvas);
        }

        PlayAudio();
    }

    private void UpdateTransform(List<TransformFrame> frames, ref int currentIndex, Transform target)
    {
        if (target == null || frames.Count == 0) return;

        while (currentIndex < frames.Count - 1 && frames[currentIndex + 1].time <= playbackTime)
        {
            currentIndex++;
        }

        if (currentIndex >= frames.Count) return;

        TransformFrame currentFrame = frames[currentIndex];

        if (currentIndex < frames.Count - 1)
        {
            TransformFrame nextFrame = frames[currentIndex + 1];
            float t = Mathf.InverseLerp(currentFrame.time, nextFrame.time, playbackTime);

            target.position = Vector3.Lerp(currentFrame.position, nextFrame.position, t);
            target.rotation = Quaternion.Slerp(currentFrame.rotation, nextFrame.rotation, t);
        }
        else
        {
            target.position = currentFrame.position;
            target.rotation = currentFrame.rotation;
        }
    }

    private void PlayAudio()
    {
        if (recordingData.audioFrames.Count == 0) return;

        while (currentAudioFrame < recordingData.audioFrames.Count &&
               recordingData.audioFrames[currentAudioFrame].time <= playbackTime)
        {
            AudioFrame frame = recordingData.audioFrames[currentAudioFrame];

            AudioClip clip = AudioClip.Create(
                "ReplayAudio",
                frame.audioData.Length,
                frame.channels,
                frame.frequency,
                false
            );
            clip.SetData(frame.audioData, 0);

            audioSource.PlayOneShot(clip);
            currentAudioFrame++;
        }
    }

    public bool IsPlaybackComplete()
    {
        if (recordingData.bodyFrames.Count == 0) return true;
        return playbackTime >= recordingData.bodyFrames[recordingData.bodyFrames.Count - 1].time;
    }
}