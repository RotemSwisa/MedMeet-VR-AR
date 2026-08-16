using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// מנהל כפתור אייקון עם אנימציה, Tooltip, וצליל
/// תומך בעכבר ובקונטרולרים של VR
/// </summary>
public class IconButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("ה-Tooltip שמופיע בעברת עכבר או קונטרולר")]
    public GameObject tooltip;

    [Tooltip("האייקון עצמו (Image)")]
    public Image iconImage;

    [Header("Audio")]
    [Tooltip("צליל קליק")]
    public AudioClip clickSound;

    [Header("Animation Settings")]
    [Tooltip("כמה להגדיל בHover (1.2 = 20% יותר גדול)")]
    public float hoverScale = 1.2f;

    [Tooltip("מהירות האנימציה")]
    public float animationSpeed = 5f;

    [Header("VR Settings")]
    [Tooltip("האם להפעיל תמיכה ב-VR")]
    public bool enableVRSupport = true;

    [Tooltip("מרחק מקסימלי לזיהוי raycast מהקונטרולר")]
    public float maxRaycastDistance = 10f;

    // פרטי
    private Vector3 originalScale;
    private AudioSource audioSource;
    private bool isHovering = false;
    private Coroutine scaleCoroutine;
    private XRSimpleInteractable xrInteractable;

    void Start()
    {
        // שמירת גודל מקורי
        originalScale = transform.localScale;

        // הסתרת Tooltip
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }

        // הכנת AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;

        // חיבור לכפתור רגיל (עכבר)
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // הוספת תמיכה ב-VR
        if (enableVRSupport)
        {
            SetupVRInteraction();
        }
    }

    /// <summary>
    /// הגדרת תמיכה באינטראקציות VR
    /// </summary>
    void SetupVRInteraction()
    {
        // בדיקה אם יש XRSimpleInteractable - אם לא, נוסיף
        xrInteractable = GetComponent<XRSimpleInteractable>();
        if (xrInteractable == null)
        {
            xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // חיבור לאירועים של VR
        xrInteractable.hoverEntered.AddListener(OnVRHoverEnter);
        xrInteractable.hoverExited.AddListener(OnVRHoverExit);
        xrInteractable.selectEntered.AddListener(OnVRSelect);

        // וידוא שיש Collider לזיהוי
        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

            // התאמת גודל ה-Collider ל-RectTransform אם זה UI
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                boxCollider.size = new Vector3(
                    rectTransform.rect.width,
                    rectTransform.rect.height,
                    1f
                );
            }
        }
    }

    // ========== אירועי עכבר (Desktop) ==========

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverStart();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverEnd();
    }

    // ========== אירועי VR ==========

    void OnVRHoverEnter(HoverEnterEventArgs args)
    {
        OnHoverStart();
    }

    void OnVRHoverExit(HoverExitEventArgs args)
    {
        OnHoverEnd();
    }

    void OnVRSelect(SelectEnterEventArgs args)
    {
        OnButtonClick();
    }

    // ========== לוגיקת Hover משותפת ==========

    void OnHoverStart()
    {
        isHovering = true;

        // הצגת Tooltip
        if (tooltip != null)
        {
            tooltip.SetActive(true);
        }

        // אנימציית הגדלה
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale * hoverScale));

        // אפקט זוהר על האייקון
        if (iconImage != null)
        {
            StartCoroutine(GlowEffect());
        }
    }

    void OnHoverEnd()
    {
        isHovering = false;

        // הסתרת Tooltip
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }

        // חזרה לגודל מקורי
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
    }

    // ========== אנימציות ==========

    IEnumerator ScaleAnimation(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                Time.deltaTime * animationSpeed
            );
            yield return null;
        }
        transform.localScale = targetScale;
    }

    IEnumerator GlowEffect()
    {
        if (iconImage == null) yield break;

        Color originalColor = iconImage.color;
        Color glowColor = new Color(1f, 1f, 0.5f, 1f); // צהוב בהיר

        float elapsed = 0f;
        float duration = 0.3f;

        // זוהר
        while (elapsed < duration && isHovering)
        {
            elapsed += Time.deltaTime;
            iconImage.color = Color.Lerp(originalColor, glowColor, elapsed / duration);
            yield return null;
        }

        // חזרה
        elapsed = 0f;
        while (elapsed < duration && isHovering)
        {
            elapsed += Time.deltaTime;
            iconImage.color = Color.Lerp(glowColor, originalColor, elapsed / duration);
            yield return null;
        }

        iconImage.color = originalColor;
    }

    void OnButtonClick()
    {
        // השמעת צליל
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // אנימציית לחיצה
        StartCoroutine(ClickAnimation());

        // שחרור הכפתור מיד אחרי לחיצה (למנוע שהוא יישאר selected)
        StartCoroutine(DeselectButton());
    }

    /// <summary>
    /// משחרר את הכפתור מיד אחרי לחיצה
    /// </summary>
    IEnumerator DeselectButton()
    {
        yield return new WaitForEndOfFrame();

        // מוציאים את הכפתור ממצב selected
        EventSystem current = EventSystem.current;
        if (current != null && current.currentSelectedGameObject == gameObject)
        {
            current.SetSelectedGameObject(null);
        }

        // מאלצים את הכפתור לחזור למצב Normal
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.OnDeselect(null);
        }
    }

    IEnumerator ClickAnimation()
    {
        Vector3 pressedScale = originalScale * 0.9f;

        // כווץ
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, pressedScale, elapsed / 0.1f);
            yield return null;
        }

        // חזרה
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(pressedScale, originalScale, elapsed / 0.1f);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    void OnDestroy()
    {
        // ניקוי listeners
        if (xrInteractable != null)
        {
            xrInteractable.hoverEntered.RemoveListener(OnVRHoverEnter);
            xrInteractable.hoverExited.RemoveListener(OnVRHoverExit);
            xrInteractable.selectEntered.RemoveListener(OnVRSelect);
        }
    }
}