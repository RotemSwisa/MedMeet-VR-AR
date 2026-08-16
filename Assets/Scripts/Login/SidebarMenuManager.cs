using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Normal.Realtime;  
using TMPro;

public class SidebarMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject overlayBackground;
    [Tooltip("כפתור פתיחת/סגירת התפריט")]
    public Button menuToggleButton;

    [Tooltip("ה-Panel של התפריט הצדדי")]
    public GameObject sidebarPanel;

    [Tooltip("כפתור סגירה (X)")]
    public Button closeButton;

    [Header("Menu Buttons")]
    [Tooltip("כפתור יציאה מהפגישה")]
    public Button leaveButton;

    [Tooltip("כפתור הקלטה (לעתיד)")]
    public Button recordButton;

    [Tooltip("כפתור העלאת מסמכים (לעתיד)")]
    public Button uploadButton;

    [Tooltip("כפתור לייזר (לעתיד)")]
    public Button laserButton;

    [Header("Settings")]
    [Tooltip("שם Scene לחזרה (LoginScene)")]
    public string loginSceneName = "LoginScene";

    [Tooltip("משך זמן אנימציה (שניות)")]
    public float animationDuration = 0.3f;

    [Tooltip("רוחב התפריט (פיקסלים)")]
    public float sidebarWidth = 300f;

    [Header("Audio (Optional)")]
    [Tooltip("צליל פתיחת תפריט")]
    public AudioClip openSound;

    [Tooltip("צליל סגירת תפריט")]
    public AudioClip closeSound;

    [Header("Participants Display")]
    [Tooltip("טקסט להצגת מספר משתתפים")]
    public TMP_Text participantsText;

    [Tooltip("Realtime component מהסצנה")]
    public Realtime realtime;

    private AudioSource audioSource;
    private bool isMenuOpen = false;
    private RectTransform sidebarRect;
    private Coroutine animationCoroutine;

    void Start()
    {
        // בדיקות חיבור
        if (menuToggleButton == null || sidebarPanel == null)
        {
            Debug.LogError("❌ UI Elements חסרים! בדקי ב-Inspector.");
            return;
        }

        // קבלת RectTransform של הSidebar
        sidebarRect = sidebarPanel.GetComponent<RectTransform>();

        // חיבור כפתורים
        menuToggleButton.onClick.AddListener(ToggleMenu);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveMeetingClicked);
        }

        // כפתורים עתידיים (מושבתים)
        SetupFutureButtons();

        // הכנת AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // התחלה: התפריט סגור
        sidebarPanel.SetActive(false);
        isMenuOpen = false;

        Debug.Log("✅ SidebarMenuManager initialized!");

        if (realtime == null)
        {
            realtime = FindObjectOfType<Realtime>();

            if (realtime == null)
            {
                Debug.LogWarning("⚠️ Realtime component not found! Participant count won't work.");
            }
        }

        // עדכון ראשוני של מספר המשתתפים
        UpdateParticipantCount();

        Debug.Log("✅ SidebarMenuManager initialized!");
    }

    // הגדרת כפתורים עתידיים
    void SetupFutureButtons()
    {
        // כפתור הקלטה
        if (recordButton != null)
        {
            recordButton.interactable = false;
            recordButton.onClick.AddListener(() => ShowComingSoon("Record Meeting"));
        }

        // כפתור העלאה
        if (uploadButton != null)
        {
            uploadButton.interactable = false;
            uploadButton.onClick.AddListener(() => ShowComingSoon("Upload Documents"));
        }

        // כפתור לייזר
        if (laserButton != null)
        {
            laserButton.interactable = false;
            laserButton.onClick.AddListener(() => ShowComingSoon("Laser Pointer"));
        }
    }

    // פתיחה/סגירה של התפריט
    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    // פתיחת תפריט
    public void OpenMenu()
    {
        if (isMenuOpen) return;

        Debug.Log("📂 Opening sidebar menu");

        // צליל
        PlaySound(openSound);

        // הפעלת הPanel
        sidebarPanel.SetActive(true);

        // אנימציה
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateSidebar(true));
        if (overlayBackground != null)
        {
            overlayBackground.SetActive(true);
        }

        isMenuOpen = true;
    }

    // סגירת תפריט
    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        Debug.Log("📁 Closing sidebar menu");

        // צליל
        PlaySound(closeSound);

        // אנימציה
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateSidebar(false));

        if (overlayBackground != null)
        {
            overlayBackground.SetActive(false);
        }

        isMenuOpen = false;
    }

    // אנימציית Slide In/Out
    IEnumerator AnimateSidebar(bool slideIn)
    {
        float startPos = slideIn ? -sidebarWidth : 0;
        float endPos = slideIn ? 0 : -sidebarWidth;

        float elapsed = 0;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Ease Out Cubic לאנימציה חלקה
            t = 1f - Mathf.Pow(1f - t, 3f);

            float currentPos = Mathf.Lerp(startPos, endPos, t);
            sidebarRect.anchoredPosition = new Vector2(currentPos, 0);

            yield return null;
        }

        // ודא מיקום סופי
        sidebarRect.anchoredPosition = new Vector2(endPos, 0);

        // כיבוי הPanel אם נסגר
        if (!slideIn)
        {
            sidebarPanel.SetActive(false);
        }
    }

    // כפתור Leave Meeting
    void OnLeaveMeetingClicked()
    {
        Debug.Log("🚪 Leave Meeting clicked");

        // סגירת התפריט
        CloseMenu();

        // המתן קצר ואז יציאה
        StartCoroutine(LeaveMeetingWithDelay());
    }

    IEnumerator LeaveMeetingWithDelay()
    {
        yield return new WaitForSeconds(0.3f);

        Debug.Log("👋 Leaving meeting...");

        // ניקוי נתונים (אופציונלי)
        // PlayerPrefs.DeleteKey("PlayerName");
        // PlayerPrefs.Save();

        // חזרה ל-LoginScene
        if (Application.CanStreamedLevelBeLoaded(loginSceneName))
        {
            SceneManager.LoadScene(loginSceneName);
        }
        else
        {
            Debug.LogError($"❌ Scene '{loginSceneName}' not found!");
        }
    }

    // הודעה "Coming Soon"
    void ShowComingSoon(string featureName)
    {
        Debug.Log($"🔮 {featureName} - Coming in Sprint 2!");
        // בעתיד: הצגת popup
    }

    // השמעת צליל
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // סגירה בלחיצה על ESC (PC בלבד)
    void Update()
    {
        // עדכון מספר משתתפים בכל פריים
        UpdateParticipantCount();

        // סגירה בלחיצה על ESC (PC בלבד)
        //if (Input.GetKeyDown(KeyCode.Escape) && isMenuOpen)
       // {
        //    CloseMenu();
        //}
    }

    // ניקוי
    void OnDestroy()
    {
        if (menuToggleButton != null)
        {
            menuToggleButton.onClick.RemoveListener(ToggleMenu);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseMenu);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveMeetingClicked);
        }
    }

    private int lastPlayerCount = 0;

    void UpdateParticipantCount()
    {
        if (participantsText == null)
            return;

        // ספירת אווטרים
        int playerCount = CountActiveAvatars();

        // זיהוי שינוי
        if (playerCount != lastPlayerCount)
        {
            if (playerCount > lastPlayerCount)
            {
                Debug.Log($"✅ Player joined! Total: {playerCount}");
                // אנימציה אופציונלית
                if (participantsText != null)
                {
                    StartCoroutine(PulseText(participantsText.transform));
                }
            }
            else if (playerCount < lastPlayerCount)
            {
                Debug.Log($"👋 Player left. Total: {playerCount}");
            }

            lastPlayerCount = playerCount;
        }

        // עדכון טקסט
        participantsText.text = $"👥 Participants: {playerCount}";

        // עדכון צבע
        if (playerCount > 1)
        {
            participantsText.color = new Color(0.5f, 1f, 0.5f);
        }
        else if (playerCount == 1)
        {
            participantsText.color = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            participantsText.text = "👥 Connecting...";
            participantsText.color = new Color(1f, 0.7f, 0.3f);
        }
    }

    int CountActiveAvatars()
    {
        RealtimeAvatar[] avatars = FindObjectsOfType<RealtimeAvatar>();
        int count = 0;

        foreach (RealtimeAvatar avatar in avatars)
        {
            if (avatar != null && avatar.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    // אנימציה קטנה
    IEnumerator PulseText(Transform textTransform)
    {
        if (textTransform == null) yield break;

        Vector3 originalScale = textTransform.localScale;
        float duration = 0.15f;

        // הגדלה
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.15f, elapsed / duration);
            textTransform.localScale = originalScale * scale;
            yield return null;
        }

        // חזרה
        elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.15f, 1f, elapsed / duration);
            textTransform.localScale = originalScale * scale;
            yield return null;
        }

        textTransform.localScale = originalScale;
    }
}
