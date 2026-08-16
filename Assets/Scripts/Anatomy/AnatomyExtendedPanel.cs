using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// פאנל מורחב שמופיע בהצבעה על תת-חלק.
/// יש instance יחיד שמוצג בכל פעם (singleton).
/// </summary>
public class AnatomyExtendedPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static AnatomyExtendedPanel Instance { get; private set; }

    Canvas canvas;
    CanvasGroup canvasGroup;
    TextMeshProUGUI titleTxt;
    TextMeshProUGUI descTxt;
    AnatomyPart currentPart;

    Coroutine fadeRoutine;
    Coroutine hideTimer;

    public Color bgColor = new Color(0.05f, 0.1f, 0.15f, 0.92f);
    public Color borderColor = new Color(0f, 0.78f, 1f, 1f);
    public float fadeIn = 0.2f;
    public float fadeOut = 0.5f;

    public static AnatomyExtendedPanel GetOrCreate()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("AnatomyExtendedPanel");
        Instance = go.AddComponent<AnatomyExtendedPanel>();
        Instance.Build();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Build()
    {
        // Canvas World Space
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 280);
        rt.localScale = Vector3.one * 0.0008f;

        // CanvasGroup לאנימציות fade
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // רקע
        var bgGO = new GameObject("Bg");
        bgGO.transform.SetParent(transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        var bgRT = bgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // גבול זוהר
        var outline = bgGO.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(3, -3);

        // כותרת
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(transform, false);
        titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color = borderColor;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.raycastTarget = false;
        var titleRT = titleTxt.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0, 60);
        titleRT.anchoredPosition = new Vector2(0, -15);

        // תיאור
        var descGO = new GameObject("Description");
        descGO.transform.SetParent(transform, false);
        descTxt = descGO.AddComponent<TextMeshProUGUI>();
        descTxt.fontSize = 22;
        descTxt.color = Color.white;
        descTxt.alignment = TextAlignmentOptions.TopLeft;
        descTxt.enableWordWrapping = true;
        descTxt.raycastTarget = false;
        var descRT = descTxt.rectTransform;
        descRT.anchorMin = Vector2.zero;
        descRT.anchorMax = Vector2.one;
        descRT.offsetMin = new Vector2(20, 20);
        descRT.offsetMax = new Vector2(-20, -80);
    }

    public void ShowFor(AnatomyPart part)
    {
        if (part == null) return;
        currentPart = part;

        // הצב ליד החלק
        Vector3 worldPos = part.transform.position + Vector3.up * 0.1f + Vector3.right * 0.15f;
        transform.position = worldPos;

        // טקסטים
        titleTxt.text = part.PartName;
        descTxt.text = string.IsNullOrEmpty(part.ExtendedDescription)
            ? "<i><color=#666666>No description yet</color></i>"
            : part.ExtendedDescription;

        // אנימציית fade in
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (hideTimer != null) { StopCoroutine(hideTimer); hideTimer = null; }
        fadeRoutine = StartCoroutine(FadeTo(1f, fadeIn));
    }

    public void HideDelayed()
    {
        if (hideTimer != null) StopCoroutine(hideTimer);
        hideTimer = StartCoroutine(HideAfterDelay(0.3f));
    }

    public void HideImmediate()
    {
        if (hideTimer != null) { StopCoroutine(hideTimer); hideTimer = null; }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(0f, fadeOut));
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideImmediate();
        hideTimer = null;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(duration, 0.01f);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    // אם הלייזר נכנס לפאנל - לא לסגור אותו
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTimer != null) { StopCoroutine(hideTimer); hideTimer = null; }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDelayed();
    }

    // קומפוננטה שמתווספת לכל AnatomyPart שמטפלת ב-hover
    // (לא דרך IPointerEnterHandler על הפאנל עצמו)
    // ראה AnatomyHoverProxy למטה
}

/// <summary>
/// פרוקסי קטן על כל תת-חלק עם Collider - מטפל בכניסת/יציאת לייזר ומעדכן את הפאנל.
/// </summary>
public class AnatomyHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public AnatomyPart part;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (part == null) return;
        // הצג רק אם האיבר אכן במצב פיצוץ
        var organ = GetComponentInParent<OrganController>();
        if (organ == null || organ.State != OrganController.OrganState.Exploded) return;

        AnatomyExtendedPanel.GetOrCreate().ShowFor(part);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var panel = AnatomyExtendedPanel.Instance;
        if (panel != null) panel.HideDelayed();
    }
}
