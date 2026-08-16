using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;

public class OverheadIconVisuals : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage; // גרור לפה את ה-Icon
    public GameObject visualContainer; // גרור לפה את ה-Microphone (האובייקט עצמו)

    [Header("Icons")]
    public Sprite micOnSprite;
    public Sprite micOffSprite; // אופציונלי

    [Header("Settings")]
    public Color talkingColor = Color.green;
    public Color idleColor = Color.white;
    public float sensitivity = 0.01f;

    private RealtimeAvatarManager _avatarManager;
    private RealtimeAvatarVoice _voice;
    private AvatarVoiceRecorder _recorder;
    private bool _foundComponents = false;

    void Start()
    {
        // מנסים למצוא את האווטאר שהסקריפט הזה יושב עליו (או שהוא הילד שלו)
        _avatarManager = FindFirstObjectByType<RealtimeAvatarManager>();
    }

    void Update()
    {
        if (!_foundComponents)
        {
            // מחפשים את הרכיבים אצל ההורה (האווטאר)
            _voice = GetComponentInParent<RealtimeAvatarVoice>();
            _recorder = GetComponentInParent<AvatarVoiceRecorder>();

            if (_voice != null)
            {
                _foundComponents = true;
            }
            return;
        }

        // 1. בדיקת מצב Mute
        // אם אנחנו במיוט, נעלים את כל האייקון שמעל הראש
        if (_voice.mute)
        {
            if (visualContainer != null && visualContainer.activeSelf)
                visualContainer.SetActive(false);
            return;
        }
        else
        {
            if (visualContainer != null && !visualContainer.activeSelf)
                visualContainer.SetActive(true);
        }

        // 2. בדיקה האם מדבר
        bool isTalking = false;
        if (_recorder != null) isTalking = _recorder.IsTalking;
        else isTalking = _voice.voiceVolume > sensitivity;

        // 3. עדכון צבע/אייקון
        if (iconImage != null)
        {
            if (micOnSprite != null) iconImage.sprite = micOnSprite;
            // אם מדבר - ירוק, אם לא - לבן
            iconImage.color = isTalking ? talkingColor : idleColor;
        }
    }
}