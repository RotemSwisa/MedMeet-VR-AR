using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;

public class MicButtonController : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Image glowImage;
    public Button targetButton;

    [Header("Icons")]
    public Sprite micOnSprite;
    public Sprite micOffSprite;

    [Header("Settings")]
    public Color iconNormalColor = Color.white;
    public Color iconMutedColor = Color.red;
    public Color glowColor = Color.green;

    // סף רגישות לרעש. תתחיל מ‑0.05 ותכוון לפי בדיקות.
    [Range(0.0001f, 0.5f)]
    public float sensitivity = 0.05f;

    // כמה זמן מינימלי ברצף נספור כדיבור (שניות)
    [Range(0.05f, 1.0f)]
    public float minSpeechDuration = 0.3f;

    private RealtimeAvatarManager _avatarManager;
    private RealtimeAvatarVoice _localVoice;
    private AvatarNameTag _nameTag;
    private GameObject _overheadUI;

    private bool _isMuted = false;
    private bool _hasFoundAvatar = false;

    private float _accumulatedSpeechTime = 0f;
    private bool _isCurrentlySpeaking = false;
    private string _cachedPlayerName = "Unknown";

    void Start()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
        if (targetButton != null)
            targetButton.onClick.AddListener(ToggleMic);

        _avatarManager = FindFirstObjectByType<RealtimeAvatarManager>();
        UpdateVisuals(false);
    }

    void Update()
    {
        if (!_hasFoundAvatar)
        {
            InitializeAvatar();
            return;
        }

        if (_localVoice == null)
            return;

        // שמירה על mute בפועל
        if (_localVoice.mute != _isMuted)
            _localVoice.mute = _isMuted;

        if (_overheadUI != null)
        {
            bool shouldBeActive = !_isMuted;
            if (_overheadUI.activeSelf != shouldBeActive)
                _overheadUI.SetActive(shouldBeActive);
        }

        HandleSpeechLogic();
    }

    void InitializeAvatar()
    {
        if (_avatarManager != null && _avatarManager.localAvatar != null)
        {
            _localVoice = _avatarManager.localAvatar.GetComponentInChildren<RealtimeAvatarVoice>();
            _nameTag = _avatarManager.localAvatar.GetComponent<AvatarNameTag>();

            Canvas[] allCanvases = _avatarManager.localAvatar.GetComponentsInChildren<Canvas>(true);
            foreach (var c in allCanvases)
            {
                if (c.gameObject.name == "Canvas")
                {
                    _overheadUI = c.gameObject;
                    break;
                }
            }

            if (_localVoice != null)
            {
                _hasFoundAvatar = true;
                _localVoice.mute = _isMuted;
            }

            if (_nameTag != null)
                _cachedPlayerName = _nameTag.GetPlayerName();
        }
    }

    void HandleSpeechLogic()
    {
        // אם המיקרופון מושתק – לא סופרים כלום
        if (_isMuted || _localVoice == null)
        {
            UpdateVisuals(false);
            _isCurrentlySpeaking = false;
            _accumulatedSpeechTime = 0f;
            return;
        }

        float currentVol = _localVoice.voiceVolume;
        bool isTalkingNow = currentVol > sensitivity;

        UpdateVisuals(isTalkingNow);

        if (!isTalkingNow)
        {
            // הפסיק לדבר
            if (_isCurrentlySpeaking)
            {
                // אם יש רצף דיבור של מינימום זמן – תודיע למערכת התמלול שלך
                if (_accumulatedSpeechTime >= minSpeechDuration)
                {
                    NotifyEndOfSpeech(_accumulatedSpeechTime);
                }

                _accumulatedSpeechTime = 0f;
                _isCurrentlySpeaking = false;
            }
            return;
        }

        // מדבר כרגע
        _isCurrentlySpeaking = true;
        _accumulatedSpeechTime += Time.deltaTime;
    }

    /// <summary>
    /// כאן המקום לחבר למערכת ה‑ASR שלך.
    /// הפונקציה קוראת רק כשהיה רצף דיבור אמיתי (מעל minSpeechDuration).
    /// </summary>
    void NotifyEndOfSpeech(float speechDuration)
    {
        if (_nameTag != null)
            _cachedPlayerName = _nameTag.GetPlayerName();
        if (string.IsNullOrEmpty(_cachedPlayerName))
            _cachedPlayerName = "Unknown";

        // כאן אתה אמור להזניק את התמלול (ASR) ולבסוף לקרוא ל‑MeetingAuditor.LogChat
        // לדוגמה (פסאודו קוד):
        //
        // MyTranscriber.Instance.TranscribeLastUtterance(_cachedPlayerName, speechDuration);
        //
        // כשהתמלול מוכן:
        // MeetingAuditor.Instance.LogChat(_cachedPlayerName, realText, speechDuration);
    }

    void UpdateVisuals(bool isTalking)
    {
        if (_isMuted)
        {
            if (iconImage != null)
            {
                if (micOffSprite != null) iconImage.sprite = micOffSprite;
                iconImage.color = iconMutedColor;
            }
            if (glowImage != null)
                glowImage.color = new Color(0, 0, 0, 0);
        }
        else
        {
            if (iconImage != null)
            {
                if (micOnSprite != null) iconImage.sprite = micOnSprite;
                iconImage.color = iconNormalColor;
            }

            if (glowImage != null)
            {
                float targetAlpha = isTalking ? 1f : 0f;
                Color target = glowColor;
                target.a = targetAlpha;
                glowImage.color = Color.Lerp(glowImage.color, target, Time.deltaTime * 15);
            }
        }
    }

    public void ToggleMic()
    {
        _isMuted = !_isMuted;
        if (_localVoice != null)
            _localVoice.mute = _isMuted;
    }
}
