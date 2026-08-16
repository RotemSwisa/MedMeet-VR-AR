using UnityEngine;
using Normal.Realtime;
using System.Collections;
using System.Linq;
using System.Reflection;

public class AvatarVoiceRecorder : MonoBehaviour
{
    [Header("Dependencies")]
    public RealtimeAvatarVoice avatarVoice;
    public RealtimeView realtimeView;
    private GroqClient groqClient;
    private BoardSync boardSync;

    [Header("Settings - Strict Sensitivity")]
    private float startThreshold = 0.025f;
    private float stopThreshold = 0.01f;
    private float silenceThreshold = 0.8f;
    private float maxRecordTime = 45.0f;

    // סטטוסים
    public bool IsTalking { get; private set; } = false;
    private bool _forceMute = false;
    private bool isMonitoring = false;
    private bool isSpeechDetected = false;

    // משתני הקלטה
    private float silenceTimer = 0f;
    private float recordingTimer = 0f;
    private float sessionTimer = 0f;
    private float currentSessionPeakVolume = 0f;

    // חיבור ל-Normcore
    private AudioClip sharedClip;
    private string activeDeviceName;
    private int lastReadPosition = 0;

    // Shadow recording is DISABLED — calling Microphone.Start() on the same
    // device Normcore already records on breaks the network audio for all
    // other participants.  These fields are kept so the code compiles but
    // shadow recording is never activated.
    private AudioClip myOwnClip;
    private bool usingShadowRecording = false;   // always stays false

    // Tracks whether monitoring has ever started so we only reset
    // lastReadPosition on the very first call (to skip pre-session audio).
    // Subsequent calls — after StopAndSend — keep reading from where we left off.
    private bool _hasEverStarted = false;

    // באפר פנימי
    private float[] internalBuffer;
    private int internalBufferPos = 0;

    private AudioSource foundAudioSource;

    void Start()
    {
        Debug.Log($"🚀 AvatarVoiceRecorder STARTED on object: {gameObject.name}");

        groqClient = FindFirstObjectByType<GroqClient>();
        boardSync = FindFirstObjectByType<BoardSync>();

        if (avatarVoice == null) avatarVoice = GetComponent<RealtimeAvatarVoice>();
        if (avatarVoice == null) avatarVoice = GetComponentInParent<RealtimeAvatarVoice>();

        if (realtimeView == null) realtimeView = GetComponent<RealtimeView>();
        if (realtimeView == null) realtimeView = GetComponentInParent<RealtimeView>();

        internalBuffer = new float[44100 * 60];

        StartCoroutine(WaitForNormcoreMic());
    }

    IEnumerator WaitForNormcoreMic()
    {
        yield return new WaitForSeconds(1.0f);

        // Bail early when this avatar is just a replay placeholder (no
        // RealtimeView model) or when Normcore isn't connected. Without this
        // the coroutine spins for 5 seconds and then logs a scary CRITICAL
        // error that's harmless but visually alarming.
        if (avatarVoice == null || realtimeView == null)
        {
            Debug.Log("[AvatarVoiceRecorder] No RealtimeAvatarVoice / RealtimeView — voice capture skipped (replay or offline).");
            yield break;
        }
        if (realtimeView.realtime == null || !realtimeView.realtime.connected)
        {
            Debug.Log("[AvatarVoiceRecorder] Normcore not connected — voice capture skipped.");
            yield break;
        }

        foundAudioSource = GetComponent<AudioSource>();
        if (foundAudioSource == null && avatarVoice != null) foundAudioSource = avatarVoice.GetComponent<AudioSource>();

        int attempts = 0;
        bool found = false;

        while (attempts < 10)
        {
            if (foundAudioSource != null && foundAudioSource.clip != null)
            {
                sharedClip = foundAudioSource.clip;
                Debug.Log("✅ Found Clip in public AudioSource!");
                found = true;
                break;
            }

            if (avatarVoice != null)
            {
                sharedClip = FindClipRecursively(avatarVoice, 0);
                if (sharedClip != null)
                {
                    Debug.Log("✅ FOUND IT! Stolen via Deep Reflection.");
                    found = true;
                    break;
                }
            }

            yield return new WaitForSeconds(0.5f);
            attempts++;
        }

        if (!found)
        {
            // Shadow recording (Microphone.Start on Normcore's device) is intentionally
            // disabled — it interferes with Normcore audio and breaks voice for all
            // other participants.  Log the failure and wait for the shared clip instead.
            Debug.LogWarning("⚠️ Shared clip not yet available — will retry in Update via CheckMuteAndMonitoring.");
        }

        if (found)
        {
            // בדיקה ראשונית
            CheckMuteAndMonitoring();
        }
        else
        {
            // Downgraded from LogError to LogWarning — this is an expected
            // condition during replay or when running without Normcore. The
            // red "CRITICAL" log was misleading users into thinking something
            // had crashed when in fact the rest of the pipeline is fine.
            Debug.LogWarning("[AvatarVoiceRecorder] No audio clip found within 5s — voice capture inactive for this avatar.");
        }
    }

    AudioClip FindClipRecursively(object obj, int depth)
    {
        if (obj == null || depth > 2) return null;

        var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(AudioClip))
            {
                var val = field.GetValue(obj);
                if (val != null) return (AudioClip)val;
            }
            else if (!field.FieldType.IsPrimitive && field.FieldType != typeof(string) && field.FieldType.Namespace != null && field.FieldType.Namespace.StartsWith("Normal"))
            {
                var nestedObj = field.GetValue(obj);
                if (nestedObj != null)
                {
                    var res = FindClipRecursively(nestedObj, depth + 1);
                    if (res != null) return res;
                }
            }
        }
        return null;
    }

    // --- עדכון חשוב: פונקציה שבודקת ומעדכנת מצב כל הזמן ---
    void CheckMuteAndMonitoring()
    {
        bool systemMuted = _forceMute || (avatarVoice != null && avatarVoice.mute);

        // 1. אם אנחנו מושתקים אבל המערכת מקליטה - תעצור
        if (systemMuted && isMonitoring)
        {
            StopAndSend(); // עוצר הקלטה ושולח מה שיש (אם היה)
            isMonitoring = false;
        }
        // 2. אם אנחנו לא מושתקים אבל המערכת ישנה - תתעורר!
        else if (!systemMuted && !isMonitoring)
        {
            // מוודאים שיש לנו מיקרופון תקין לפני שמתחילים
            bool micReady = (sharedClip != null) || (usingShadowRecording && myOwnClip != null);
            if (micReady)
            {
                StartMonitoring();
            }
        }
    }

    public void SetMuteState(bool isMuted)
    {
        _forceMute = isMuted;
        CheckMuteAndMonitoring();
    }

    void StartMonitoring()
    {
        isMonitoring = true;
        isSpeechDetected = false;
        recordingTimer = 0f;
        silenceTimer = 0f;
        sessionTimer = 0f;
        currentSessionPeakVolume = 0f;
        IsTalking = false;
        internalBufferPos = 0;   // clear the accumulation buffer for this new sentence

        // ── lastReadPosition handling ────────────────────────────────────────
        // ONLY on the very first start do we (re-)initialise the read-head.
        // On every subsequent call (restart after StopAndSend) we intentionally
        // leave lastReadPosition wherever ReadMicData set it last, so we
        // continue reading from the CURRENT mic position instead of from 0.
        // Reading from 0 was the root cause of the "first sentence repeats" bug.
        if (!_hasEverStarted)
        {
            _hasEverStarted = true;
            // Shadow recording is disabled, so usingShadowRecording is always false.
            // For the Normcore shared clip we cannot reliably query GetPosition(null)
            // because Normcore may use a non-default device name.  Starting at 0 is
            // acceptable for the very first sentence only — subsequent sentences
            // continue from the current position (see above).
            lastReadPosition = 0;
        }
        // else: keep lastReadPosition — avoids re-reading already-sent audio.

       // Debug.Log($"▶️ Monitoring Started (readPos={lastReadPosition})");
    }

    void Update()
    {
        // Defensive: RealtimeView throws "This view doesn't have a model yet"
        // when Normcore hasn't finished connecting. Wait until the model
        // exists (currentModel != null) before reading ownership.
        if (realtimeView == null) return;
        if (realtimeView.realtime == null || !realtimeView.realtime.connected) return;
        if (!realtimeView.isOwnedLocallySelf) return;

        // *** התיקון: בדיקת סטטוס קבועה ***
        // זה מה שיגרום לזה לעבוד מיד כשהמיקרופון נפתח, גם בהתחלה
        CheckMuteAndMonitoring();

        if (!isMonitoring) return;

        bool isAlive = false;
        if (usingShadowRecording) isAlive = Microphone.IsRecording(activeDeviceName);
        else isAlive = (sharedClip != null);

        if (!isAlive)
        {
            isMonitoring = false;
            StartCoroutine(WaitForNormcoreMic());
            return;
        }

        ReadMicData();

        float currentVol = GetLastVolumeRMS();
        if (currentVol > currentSessionPeakVolume) currentSessionPeakVolume = currentVol;

        recordingTimer += Time.deltaTime;

        IsTalking = isSpeechDetected || (currentVol > startThreshold);

        if (!isSpeechDetected)
        {
            sessionTimer += Time.deltaTime;
            // רענון באפר אם שקט מדי הרבה זמן
            if (sessionTimer > 15.0f)
            {
                // Silence timeout — jump read-head to current mic position so
                // we don't accumulate stale silence samples into the next sentence.
                AudioClip sourceClip = usingShadowRecording ? myOwnClip : sharedClip;
                if (sourceClip != null)
                {
                    int curPos = Microphone.GetPosition(usingShadowRecording ? activeDeviceName : null);
                    if (curPos > 0) lastReadPosition = curPos;
                }
                StartMonitoring();
                return;
            }

            if (currentVol > startThreshold)
            {
                isSpeechDetected = true;
                silenceTimer = 0f;
            }
        }
        else
        {
            if (recordingTimer >= maxRecordTime) { StopAndSend(); return; }

            if (currentVol < stopThreshold)
            {
                silenceTimer += Time.deltaTime;
                if (silenceTimer > silenceThreshold) StopAndSend();
            }
            else silenceTimer = 0f;
        }
    }

    void ReadMicData()
    {
        AudioClip sourceClip = usingShadowRecording ? myOwnClip : sharedClip;
        string device = usingShadowRecording ? activeDeviceName : null;

        if (sourceClip == null) return;

        // clip.samples is the total length of the circular mic buffer
        int clipSamples = sourceClip.samples;
        if (clipSamples <= 0) return;

        int currentMicPos = Microphone.GetPosition(device);

        // GetPosition returns 0 before the mic has written anything — skip until it moves
        if (currentMicPos <= 0) return;

        if (currentMicPos == lastReadPosition) return;

        // Microphone buffer wrapped around — reset read-head to start of new lap
        if (currentMicPos < lastReadPosition)
            lastReadPosition = 0;

        int amountToRead = currentMicPos - lastReadPosition;
        if (amountToRead <= 0) return;

        // ── Guard: clamp so offset + length never exceeds clip.samples ──────
        // This is what causes the SoundManager "invalid parameter" crash.
        if (lastReadPosition >= clipSamples)
        {
            lastReadPosition = currentMicPos;
            return;
        }
        if (lastReadPosition + amountToRead > clipSamples)
            amountToRead = clipSamples - lastReadPosition;

        if (amountToRead <= 0) return;

        float[] tempData = new float[amountToRead];

        // Safe to call now — offset and length are within clip bounds
        sourceClip.GetData(tempData, lastReadPosition);

        if (internalBufferPos + amountToRead < internalBuffer.Length)
        {
            System.Array.Copy(tempData, 0, internalBuffer, internalBufferPos, amountToRead);
            internalBufferPos += amountToRead;
        }

        lastReadPosition = currentMicPos;
    }

    float GetLastVolumeRMS()
    {
        if (internalBufferPos < 256) return 0f;
        float sum = 0;
        for (int i = 0; i < 256; i++)
        {
            float val = internalBuffer[internalBufferPos - 1 - i];
            sum += val * val;
        }
        return Mathf.Sqrt(sum / 256);
    }

    void StopAndSend()
    {
        // לפני שאנחנו עוצרים, שומרים את הסטטוס
        bool wasTalking = IsTalking;

        isMonitoring = false;
        IsTalking = false;
        float finalPeakVolume = currentSessionPeakVolume;

        // שליחה רק אם ההקלטה ארוכה משנייה והיה בה תוכן
        if (recordingTimer > 1.0f && internalBufferPos > 0)
        {
            AudioClip clipToSend = AudioClip.Create("GroqRecording", internalBufferPos, 1, 44100, false);
            float[] exactData = new float[internalBufferPos];
            System.Array.Copy(internalBuffer, exactData, internalBufferPos);
            clipToSend.SetData(exactData, 0);

            if (groqClient != null)
            {
                StartCoroutine(groqClient.SendAudioForTranscription(clipToSend, (text) =>
                {
                    HandleTranscription(text, recordingTimer, finalPeakVolume);
                }));
            }
        }

        // אם עצרנו בגלל סוף משפט (ולא בגלל Mute), המערכת תפעיל את עצמה מחדש אוטומטית ב-Update הבא
        // בזכות CheckMuteAndMonitoring
    }

    IEnumerator RestartMonitoringSoon()
    {
        yield return new WaitForSeconds(0.2f);
        StartMonitoring();
    }

    void HandleTranscription(string text, float duration, float peakVolume)
    {
        string cleanText = CleanHallucinations(text, peakVolume);
        if (string.IsNullOrEmpty(cleanText)) return;

        if (boardSync != null)
        {
            var nameTag = GetComponent<AvatarNameTag>();
            string myName = nameTag != null ? nameTag.GetPlayerName() : "Unknown";
            boardSync.AddMessage(myName, cleanText, duration);
        }

        SignLanguagePlayer.Instance?.ProcessText(cleanText);
        VoiceCommandManager.Instance?.ProcessText(cleanText);


    }

    string CleanHallucinations(string rawText, float peakVolume)
    {
        if (string.IsNullOrEmpty(rawText)) return "";
        string processed = rawText.Trim();
        string lower = processed.ToLower();

        if (lower.Contains(".com") || lower.Contains(".con") || lower.Contains("http")) return "";

        string[] noiseHallucinations = new string[] {
            "you", "thank you", "thanks", "subtitles", "watching"
        };

        if (noiseHallucinations.Contains(lower.Replace(".", "").Replace("?", "").Replace("!", "")))
        {
            if (peakVolume < 0.02f) return "";
        }

        string[] hardHallucinations = new string[] {
            "mbc", "copyright", "all rights reserved", "captioning",
            "amara", "org", "ted.com", "subtitle by"
        };

        if (hardHallucinations.Any(h => lower.Contains(h))) return "";

        return processed;
    }
}