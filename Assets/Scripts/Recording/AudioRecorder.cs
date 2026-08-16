using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

// ����� ����� �� ���� ���
public class AudioRecorder : MonoBehaviour
{
    private RealtimeAvatarVoice avatarVoice;
    private List<AudioFrame> audioFrames = new List<AudioFrame>();
    private bool isRecording = false;
    private float recordingStartTime;

    // Cached on the main thread (Awake/StartRecording) so the audio-thread
    // path in SaveAudioFrame doesn't need to touch UnityEngine.AudioSettings —
    // which throws "GetSampleRate can only be called from the main thread"
    // when read from OnAudioFilterRead.
    private int _cachedSampleRate = 48000;

    private List<float> currentAudioBuffer = new List<float>();
    private const int BUFFER_SIZE = 2048;

    void Awake()
    {
        _cachedSampleRate = AudioSettings.outputSampleRate;
    }

    void Start()
    {
        avatarVoice = GetComponent<RealtimeAvatarVoice>();
        if (avatarVoice == null)
        {
            // Old behaviour was to keep running and throw thread/null errors
            // every frame, breaking the whole recording pipeline. Now we
            // self-disable so the rest of the recording (avatars, objects,
            // movement) works exactly like it did BEFORE AudioRecorder was
            // added. The recording JSON will have an empty audioFrames list,
            // which is what the old working recordings already had.
            Debug.LogWarning("AudioRecorder: RealtimeAvatarVoice not found on " +
                             gameObject.name + " — audio capture disabled, but " +
                             "movement and object recording continue normally.");
            enabled = false;   // stops Update + OnAudioFilterRead
            return;
        }
    }

    public void StartRecording()
    {
        audioFrames.Clear();
        currentAudioBuffer.Clear();
        isRecording = true;
        // Use AudioSettings.dspTime because OnAudioFilterRead runs on the
        // audio thread, and Time.time can only be read from the main thread.
        // dspTime is the canonical thread-safe clock for audio code in Unity.
        recordingStartTime = (float) AudioSettings.dspTime;
        // Re-cache the sample rate in case the audio device changed between
        // Awake and this call (e.g. user switched headphones).
        _cachedSampleRate = AudioSettings.outputSampleRate;
        Debug.Log("AudioRecorder: ����� ����� ����� ���� " + gameObject.name);
    }

    public void StopRecording()
    {
        isRecording = false;

        // ���� �� �� ����� �����
        if (currentAudioBuffer.Count > 0)
        {
            SaveAudioFrame();
        }

        Debug.Log($"AudioRecorder: ���� �����. ����� {audioFrames.Count} ������� �� �����");
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isRecording) return;

        // ���� ������ �����
        currentAudioBuffer.AddRange(data);

        // ������� ���, ���� �����
        if (currentAudioBuffer.Count >= BUFFER_SIZE)
        {
            SaveAudioFrame();
        }
    }

    private void SaveAudioFrame()
    {
        AudioFrame frame = new AudioFrame
        {
            // dspTime is the audio-thread-safe clock; Time.time would throw
            // "get_time can only be called from the main thread" because this
            // method is invoked from OnAudioFilterRead on the audio thread.
            time = (float) AudioSettings.dspTime - recordingStartTime,
            audioData = currentAudioBuffer.ToArray(),
            frequency = _cachedSampleRate,    // cached on main thread
            channels = 1 // ����
        };

        audioFrames.Add(frame);
        currentAudioBuffer.Clear();
    }

    public List<AudioFrame> GetRecordedAudio()
    {
        return new List<AudioFrame>(audioFrames);
    }
}