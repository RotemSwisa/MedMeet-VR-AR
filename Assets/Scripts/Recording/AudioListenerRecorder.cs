using System.Collections.Generic;
using UnityEngine;

// מקליט את כל הסאונד במשחק - גרסה מתוקנת
public class AudioListenerRecorder : MonoBehaviour
{
    public static AudioListenerRecorder Instance { get; private set; }

    private List<AudioFrame> audioFrames = new List<AudioFrame>();
    private bool isRecording = false;
    private float recordingStartTime;

    private AudioClip recordingClip;
    private string microphoneDevice;
    private int lastSamplePosition = 0;
    private const int SAMPLE_RATE = 44100;
    private const int FRAME_SIZE = 2048;

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
        audioFrames.Clear();
        isRecording = true;
        recordingStartTime = Time.time;
        lastSamplePosition = 0;

        // בדוק אם יש מיקרופון זמין
        if (Microphone.devices.Length > 0)
        {
            // נסה למצוא "Stereo Mix" או "What U Hear" (הקלטת Output)
            microphoneDevice = null;

            foreach (string device in Microphone.devices)
            {
                // חפש התקן הקלטה של Output
                if (device.ToLower().Contains("stereo mix") ||
                    device.ToLower().Contains("what u hear") ||
                    device.ToLower().Contains("wave out") ||
                    device.ToLower().Contains("loopback"))
                {
                    microphoneDevice = device;
                    break;
                }
            }

            // אם לא מצאנו, השתמש במיקרופון רגיל (זה יקליט רק מה שאתה אומר)
            if (microphoneDevice == null)
            {
                microphoneDevice = Microphone.devices[0];
                Debug.LogWarning($"AudioListenerRecorder: לא נמצא Stereo Mix, משתמש ב: {microphoneDevice}. יוקלט רק מיקרופון, לא כל הסאונד.");
            }
            else
            {
                Debug.Log($"AudioListenerRecorder: משתמש ב: {microphoneDevice}");
            }

            // התחל הקלטה (10 שניות, loop)
            recordingClip = Microphone.Start(microphoneDevice, true, 10, SAMPLE_RATE);

            if (recordingClip != null)
            {
                Debug.Log($"AudioListenerRecorder: התחלת הקלטה מ-{microphoneDevice}");
            }
            else
            {
                Debug.LogError("AudioListenerRecorder: נכשל ביצירת AudioClip!");
                isRecording = false;
            }
        }
        else
        {
            Debug.LogError("AudioListenerRecorder: לא נמצאו התקני שמע!");
            isRecording = false;
        }
    }

    public void StopRecording()
    {
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }

        isRecording = false;
        Debug.Log($"AudioListenerRecorder: סיום הקלטה. נשמרו {audioFrames.Count} פריימים");
    }

    void Update()
    {
        if (!isRecording || recordingClip == null) return;

        int currentPosition = Microphone.GetPosition(microphoneDevice);
        if (currentPosition < 0) return;

        // טפל ב-wraparound
        if (currentPosition < lastSamplePosition)
        {
            lastSamplePosition = 0;
        }

        int samplesAvailable = currentPosition - lastSamplePosition;

        // אם יש מספיק דגימות, שמור פריים
        if (samplesAvailable >= FRAME_SIZE)
        {
            float[] samples = new float[samplesAvailable];
            recordingClip.GetData(samples, lastSamplePosition);

            // חלק לפריימים
            for (int i = 0; i < samplesAvailable; i += FRAME_SIZE)
            {
                int length = Mathf.Min(FRAME_SIZE, samplesAvailable - i);
                float[] frameData = new float[length];
                System.Array.Copy(samples, i, frameData, 0, length);

                AudioFrame frame = new AudioFrame
                {
                    time = Time.time - recordingStartTime,
                    audioData = frameData,
                    frequency = SAMPLE_RATE,
                    channels = 1
                };

                audioFrames.Add(frame);
            }

            lastSamplePosition = currentPosition;
        }
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
    }

    public List<AudioFrame> GetRecordedAudio()
    {
        return new List<AudioFrame>(audioFrames);
    }

    public bool IsRecording => isRecording;
}