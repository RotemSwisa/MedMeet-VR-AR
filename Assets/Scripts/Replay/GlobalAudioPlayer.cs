using System.Collections.Generic;
using UnityEngine;

// משמיע את האודיו הכללי שהוקלט
public class GlobalAudioPlayer : MonoBehaviour
{
    private List<AudioFrame> audioFrames;
    private AudioSource audioSource;
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D Sound (לא spatial)
    }

    public void Initialize(List<AudioFrame> frames)
    {
        audioFrames = frames;
        Debug.Log($"GlobalAudioPlayer: אותחל עם {audioFrames.Count} פריימי אודיו");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentFrameIndex = 0;

        Debug.Log("GlobalAudioPlayer: התחלת השמעת אודיו");
    }

    public void StopPlayback()
    {
        isPlaying = false;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void Update()
    {
        if (!isPlaying || audioFrames == null || audioFrames.Count == 0) return;

        playbackTime += Time.deltaTime;

        // השמע פריימי אודיו שהגיע זמנם
        while (currentFrameIndex < audioFrames.Count &&
               audioFrames[currentFrameIndex].time <= playbackTime)
        {
            PlayAudioFrame(audioFrames[currentFrameIndex]);
            currentFrameIndex++;
        }
    }

    private void PlayAudioFrame(AudioFrame frame)
    {
        if (frame.audioData == null || frame.audioData.Length == 0) return;

        // צור AudioClip מהדאטה
        AudioClip clip = AudioClip.Create(
            "GlobalAudio",
            frame.audioData.Length,
            frame.channels,
            frame.frequency,
            false
        );

        clip.SetData(frame.audioData, 0);
        audioSource.PlayOneShot(clip);
    }

    public bool IsPlaybackComplete()
    {
        if (audioFrames == null || audioFrames.Count == 0) return true;
        return currentFrameIndex >= audioFrames.Count;
    }
}