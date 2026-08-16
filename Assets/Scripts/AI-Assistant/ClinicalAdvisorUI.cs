using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClinicalAdvisorUI : MonoBehaviour
{
    [Header("Chat Display")]
    [Tooltip("התוכן שבתוך ה-ScrollView — כאן מתווסף טקסט")]
    public TextMeshProUGUI chatHistoryText;

    [Tooltip("ה-ScrollRect של הצ'אט")]
    public ScrollRect chatScrollRect;

    [Tooltip("טקסט סטטוס קטן למעלה")]
    public TextMeshProUGUI statusText;

    [Tooltip("הפאנל הכללי")]
    public GameObject advisorPanel;

    [Header("Voice Input Buttons")]
    public Button startQuestionButton;
    public Button sendVoiceButton;

    [Header("Text Input")]
    [Tooltip("שדה הקלדה לכתיבה ידנית")]
    public TMP_InputField textInputField;

    [Tooltip("כפתור שליחת טקסט")]
    public Button sendTextButton;

    [Header("TTS")]
    public bool enableTTS = false;

    // פנימי
    private BoardSync boardSync;
    private GeminiClient geminiClient;
    private bool isWaiting = false;
    private bool isRecordingQuestion = false;
    private List<string> voiceBuffer = new List<string>();
    private ChatScrollLock scrollLock;
    // Stored so the "Read aloud" button can replay it at any time.
    private string lastAIResponse = "";

    // היסטוריית צ'אט מלאה
    private System.Text.StringBuilder chatHistory = new System.Text.StringBuilder();

    public static ClinicalAdvisorUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        boardSync = FindFirstObjectByType<BoardSync>();
        geminiClient = GeminiClient.Instance;

        // Ensure the ScrollRect has a ChatScrollLock attached so the scroll
        // position stays where the user left it instead of snapping to top.
        if (chatScrollRect != null)
        {
            scrollLock = chatScrollRect.GetComponent<ChatScrollLock>();
            if (scrollLock == null)
                scrollLock = chatScrollRect.gameObject.AddComponent<ChatScrollLock>();
        }

        if (boardSync != null)
            boardSync.OnNewMessage += OnNewMessageFromBoard;

        if (startQuestionButton != null)
            startQuestionButton.onClick.AddListener(OnStartQuestionPressed);

        if (sendVoiceButton != null)
            sendVoiceButton.onClick.AddListener(OnSendVoicePressed);

        if (sendTextButton != null)
            sendTextButton.onClick.AddListener(OnSendTextPressed);

        // Enter שולח גם מהמקלדת על PC
        if (textInputField != null)
            textInputField.onSubmit.AddListener((_) => OnSendTextPressed());

        SetStatus("Ready");
        AppendToChat("System", "AI Clinical Advisor ready. Ask a question by voice or text.", "#888888");

        if (advisorPanel != null)
            advisorPanel.SetActive(true);
    }

    void OnDestroy()
    {
        if (boardSync != null)
            boardSync.OnNewMessage -= OnNewMessageFromBoard;
    }

    // ─── VOICE ───────────────────────────────────────────

    public void OnStartQuestionPressed()
    {
        voiceBuffer.Clear();
        isRecordingQuestion = true;
        SetStatus("🎙️ Listening... press Send when done.");
        Debug.Log("Voice recording started");
    }

    void OnNewMessageFromBoard(string sender, string message)
    {
        if (!isRecordingQuestion) return;
        voiceBuffer.Add($"{sender}: {message}");
        SetStatus($"🎙️ Captured {voiceBuffer.Count} message(s)...");
    }

    public void OnSendVoicePressed()
    {
        if (!isRecordingQuestion)
        {
            SetStatus("Press 'Start Question' first.");
            return;
        }

        isRecordingQuestion = false;

        if (voiceBuffer.Count == 0)
        {
            SetStatus("No speech detected. Try again.");
            return;
        }

        string transcript = string.Join("\n", voiceBuffer);
        AppendToChat("You (voice)", transcript, "#FFD700"); // צהוב
        StartCoroutine(FetchAdvice(transcript));
    }

    // ─── TEXT ────────────────────────────────────────────

    public void OnSendTextPressed()
    {
        if (textInputField == null) return;

        string userText = textInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return;

        textInputField.text = "";

        AppendToChat("You (text)", userText, "#FFD700"); // צהוב
        StartCoroutine(FetchAdvice(userText));

        // מחזיר פוקוס לשדה הטקסט על PC
#if !UNITY_ANDROID
        textInputField.ActivateInputField();
#endif
    }

    // ─── READ ALOUD (TTS) ────────────────────────────────
    //
    // Hooked up by the Speak button (added via the
    // MedMeet Tools → Add TTS Speak Button menu). Replays the last AI
    // response through Android's built-in TTS engine. Free, no API.
    public void OnSpeakLastResponsePressed()
    {
        if (string.IsNullOrEmpty(lastAIResponse))
        {
            SetStatus("Nothing to read yet — ask the AI a question first.");
            return;
        }
        if (AndroidTTSPlayer.Instance != null)
        {
            AndroidTTSPlayer.Instance.Speak(lastAIResponse);
            SetStatus("🔊 Reading aloud…");
        }
        else
        {
            Debug.Log($"[TTS would say]: {lastAIResponse}");
            SetStatus("🔊 (TTS player not in scene — added in editor on Quest)");
        }
    }

    /// <summary>Stops any ongoing TTS playback. Safe to call any time.</summary>
    public void OnStopSpeakingPressed()
    {
        if (AndroidTTSPlayer.Instance != null) AndroidTTSPlayer.Instance.Stop();
    }

    // ─── AI CALL ─────────────────────────────────────────

    IEnumerator FetchAdvice(string transcript)
    {
        if (isWaiting)
        {
            SetStatus("Still waiting for previous response...");
            yield break;
        }

        isWaiting = true;
        SetStatus("🔄 AI is thinking...");
        AppendToChat("AI", "...", "#4FC3F7"); // כחול בהיר

        if (sendVoiceButton != null) sendVoiceButton.interactable = false;
        if (sendTextButton != null) sendTextButton.interactable = false;

        string result = null;
        bool done = false;

        geminiClient.AskForClinicalAdvice(transcript, (response) =>
        {
            result = response;
            done = true;
        });

        yield return new WaitUntil(() => done);

        // מחליף את ה-"..." בתשובה האמיתית
        ReplaceLastAIMessage(result);
        SetStatus("✅ Ready");

        // Remember the latest AI response so the new Speak button can replay it
        if (!string.IsNullOrEmpty(result))
            lastAIResponse = result;

        if (enableTTS && !string.IsNullOrEmpty(result))
            SpeakText(result);

        if (sendVoiceButton != null) sendVoiceButton.interactable = true;
        if (sendTextButton != null) sendTextButton.interactable = true;

        isWaiting = false;
    }

    // ─── CHAT HISTORY ────────────────────────────────────

    void AppendToChat(string sender, string message, string colorHex)
    {
        chatHistory.AppendLine($"<color={colorHex}><b>{sender}:</b></color> {message}");
        RefreshChatDisplay();
    }

    void ReplaceLastAIMessage(string newMessage)
    {
        // מחליף את השורה האחרונה (שהיתה "...") בתשובה האמיתית
        string current = chatHistory.ToString();
        int lastAI = current.LastIndexOf("<color=#4FC3F7><b>AI:</b></color> ...");
        if (lastAI >= 0)
        {
            chatHistory.Clear();
            chatHistory.Append(current.Substring(0, lastAI));
            chatHistory.AppendLine($"<color=#4FC3F7><b>AI:</b></color> {newMessage}");
        }
        else
        {
            AppendToChat("AI", newMessage, "#4FC3F7");
        }
        RefreshChatDisplay();
    }

    void RefreshChatDisplay()
    {
        if (chatHistoryText != null)
            chatHistoryText.text = chatHistory.ToString();

        // Let ChatScrollLock decide whether to stay where the user is or
        // follow the new bottom. This prevents the snap-to-top bug that
        // happened after every layout rebuild.
        if (scrollLock != null)
            scrollLock.NotifyContentChanged();
        else if (chatScrollRect != null)
            StartCoroutine(LegacyScrollToBottom());
    }

    // Fallback used only when ChatScrollLock is unavailable (defensive — the
    // component is auto-added in Start() so this rarely runs).
    IEnumerator LegacyScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null && chatScrollRect.verticalNormalizedPosition <= 0.15f)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    // ─── STATUS & TTS ────────────────────────────────────

    void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void TriggerFromVoice()
    {
        OnSendVoicePressed();
    }

    void SpeakText(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject tts = new AndroidJavaObject("android.speech.tts.TextToSpeech",
                activity, new TTSInitListener());
            StartCoroutine(SpeakAfterInit(tts, text));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"TTS Error: {e.Message}");
        }
#else
        Debug.Log($"[TTS would say]: {text}");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator SpeakAfterInit(AndroidJavaObject tts, string text)
    {
        yield return new WaitForSeconds(1.0f);
        tts.Call<int>("speak", text, 0, null, "utteranceId");
    }

    class TTSInitListener : AndroidJavaProxy
    {
        public TTSInitListener() : base("android.speech.tts.TextToSpeech$OnInitListener") { }
        public void onInit(int status)
        {
            Debug.Log(status == 0 ? "✅ TTS initialized" : "⚠️ TTS init failed");
        }
    }
#endif
}