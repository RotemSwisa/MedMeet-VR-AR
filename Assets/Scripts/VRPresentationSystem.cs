using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;

/// <summary>
/// מערכת הצגת מצגת ב-VR על טלוויזיה וירטואלית
/// מאפשר ניווט בין שקפים עם כפתורים
/// מסונכרן בין כל המשתתפים!
/// </summary>
public class VRPresentationSystem : RealtimeComponent<PresentationSyncModel>
{
    [Header("Presentation Settings")]
    [Tooltip("גרור לכאן את כל תמונות המצגת לפי הסדר")]
    public Sprite[] presentationSlides;

    [Tooltip("Image Component של הטלוויזיה")]
    public Image tvScreen;

    [Header("Navigation Buttons")]
    [Tooltip("כפתור הבא")]
    public Button nextButton;

    [Tooltip("כפתור קודם")]
    public Button previousButton;

    [Header("UI Elements (Optional)")]
    [Tooltip("טקסט שמראה מספר שקף נוכחי (אופציונלי)")]
    public Text slideNumberText;

    [Tooltip("האם להציג אפקט מעבר?")]
    public bool useTransitionEffect = true;

    [Tooltip("משך אפקט המעבר בשניות")]
    public float transitionDuration = 0.3f;

    // Private variables
    private bool isTransitioning = false;

    // ✨ חדש! גישה ציבורית לשקף הנוכחי
    public int GetCurrentSlideIndex()
    {
        if (model != null)
        {
            return model.currentSlide;
        }
        return 0;
    }

    protected override void OnRealtimeModelReplaced(PresentationSyncModel previousModel, PresentationSyncModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.currentSlideDidChange -= CurrentSlideDidChange;
        }

        if (currentModel != null)
        {
            currentModel.currentSlideDidChange += CurrentSlideDidChange;

            if (currentModel.isFreshModel)
            {
                currentModel.currentSlide = 0;
            }

            UpdateSlideDisplay(currentModel.currentSlide);
        }
    }

    private void CurrentSlideDidChange(PresentationSyncModel model, int value)
    {
        UpdateSlideDisplay(value);
    }

    void Start()
    {
        if (presentationSlides == null || presentationSlides.Length == 0)
        {
            Debug.LogError("❌ לא הוספו שקפים למצגת! גרור תמונות ל-presentationSlides");
            return;
        }

        if (tvScreen == null)
        {
            Debug.LogError("❌ חסר TV Screen Image Component!");
            return;
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextSlide);
        }
        else
        {
            Debug.LogWarning("⚠️ לא חובר כפתור Next");
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(PreviousSlide);
        }
        else
        {
            Debug.LogWarning("⚠️ לא חובר כפתור Previous");
        }

        UpdateSlideDisplay(0);

        Debug.Log($"✅ מערכת מצגת הופעלה - {presentationSlides.Length} שקפים + Multiplayer Sync");
    }

    public void NextSlide()
    {
        if (isTransitioning || model == null) return;

        int currentIndex = model.currentSlide;

        if (currentIndex < presentationSlides.Length - 1)
        {
            model.currentSlide = currentIndex + 1;
            PlayClickSound();
            Debug.Log($"▶️ מעבר לשקף {model.currentSlide + 1}");
        }
        else
        {
            Debug.Log("📍 זה השקף האחרון");
            ShakeButton(nextButton);
        }
    }

    public void PreviousSlide()
    {
        if (isTransitioning || model == null) return;

        int currentIndex = model.currentSlide;

        if (currentIndex > 0)
        {
            model.currentSlide = currentIndex - 1;
            PlayClickSound();
            Debug.Log($"◀️ חזרה לשקף {model.currentSlide + 1}");
        }
        else
        {
            Debug.Log("📍 זה השקף הראשון");
            ShakeButton(previousButton);
        }
    }

    public void JumpToSlide(int slideIndex)
    {
        if (model == null) return;

        if (slideIndex >= 0 && slideIndex < presentationSlides.Length)
        {
            model.currentSlide = slideIndex;
        }
    }

    private void UpdateSlideDisplay(int index)
    {
        if (useTransitionEffect)
        {
            StartCoroutine(TransitionToSlide(index));
        }
        else
        {
            tvScreen.sprite = presentationSlides[index];
            UpdateUI(index);
        }
    }

    private System.Collections.IEnumerator TransitionToSlide(int index)
    {
        isTransitioning = true;

        float elapsed = 0f;
        Color startColor = tvScreen.color;

        while (elapsed < transitionDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / (transitionDuration / 2));
            tvScreen.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        tvScreen.sprite = presentationSlides[index];

        elapsed = 0f;
        while (elapsed < transitionDuration / 2)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / (transitionDuration / 2));
            tvScreen.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        tvScreen.color = startColor;

        UpdateUI(index);
        isTransitioning = false;
    }

    private void UpdateUI(int currentIndex)
    {
        if (slideNumberText != null)
        {
            slideNumberText.text = $"{currentIndex + 1} / {presentationSlides.Length}";
        }

        if (previousButton != null)
        {
            previousButton.interactable = (currentIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.interactable = (currentIndex < presentationSlides.Length - 1);
        }

        Debug.Log($"📊 מציג שקף {currentIndex + 1}/{presentationSlides.Length}");
    }

    private void ShakeButton(Button button)
    {
        if (button != null)
        {
            StartCoroutine(ShakeCoroutine(button.transform));
        }
    }

    private System.Collections.IEnumerator ShakeCoroutine(Transform target)
    {
        Vector3 originalPosition = target.localPosition;
        float shakeDuration = 0.2f;
        float shakeAmount = 5f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-shakeAmount, shakeAmount);
            target.localPosition = new Vector3(x, originalPosition.y, originalPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPosition;
    }

    private void PlayClickSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextSlide();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousSlide();
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            JumpToSlide(0);
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            JumpToSlide(presentationSlides.Length - 1);
        }
    }
}