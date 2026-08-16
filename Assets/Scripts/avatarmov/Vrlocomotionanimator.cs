using UnityEngine;
using System.Collections;

/// <summary>
/// מפעיל אנימציית הליכה ברגליים לפי מהירות התנועה האמיתית ב-VR
/// מוצא את ה-Animator אוטומטית - מתאים לפריפב של Normcore
///
/// איך לחבר:
/// 1. גרור סקריפט זה על Male_avatar (האב)
/// 2. הוסף Animator Component על Male avatar (הבן עם הנראות)
/// 3. צור Animator Controller עם Blend Tree (ראה README בתגובות)
/// </summary>
public class VRLocomotionAnimator : MonoBehaviour
{
    [Header("=== הגדרות הליכה ===")]
    [Tooltip("מהירות שממנה תתחיל אנימציית הליכה (מטר לשנייה)")]
    public float walkThreshold = 0.1f;

    [Tooltip("מהירות שבה האנימציה תגיע למקסימום")]
    public float maxWalkSpeed = 2f;

    [Tooltip("כמה מהר האנימציה מגיבה לשינוי מהירות")]
    [Range(1f, 15f)]
    public float animationSmoothing = 8f;

    [Header("=== שמות פרמטרים ב-Animator Controller ===")]
    public string speedParameterName = "Speed";
    public string isWalkingParameterName = "IsWalking";

    [Header("=== Debug ===")]
    public bool showDebugLogs = true;

    private Animator avatarAnimator;
    private float currentSpeed;
    private Vector3 previousPosition;
    private bool hasSpeedParam;
    private bool hasIsWalkingParam;
    private bool isReady = false;

    void Start()
    {
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        yield return null;
        yield return null;

        // חיפוש Animator אוטומטי בתוך הפריפב
        avatarAnimator = GetComponentInChildren<Animator>();

        if (avatarAnimator == null)
        {
            Debug.LogWarning("[VRLocomotionAnimator] לא נמצא Animator בתוך הפריפב! " +
                             "הוסף Animator Component על 'Male avatar' (הבן עם הנראות).");
            yield break;
        }

        // בדיקה אם הפרמטרים קיימים
        foreach (AnimatorControllerParameter p in avatarAnimator.parameters)
        {
            if (p.name == speedParameterName) hasSpeedParam = true;
            if (p.name == isWalkingParameterName) hasIsWalkingParam = true;
        }

        if (!hasSpeedParam)
            Debug.LogWarning($"[VRLocomotionAnimator] פרמטר '{speedParameterName}' לא קיים ב-Animator Controller. " +
                              "צור Blend Tree עם פרמטר זה (ראה הוראות).");

        previousPosition = transform.position;
        isReady = true;

        if (showDebugLogs)
            Debug.Log("[VRLocomotionAnimator] מוכן - Animator נמצא: " + avatarAnimator.name);
    }

    void Update()
    {
        if (!isReady) return;

        // חישוב מהירות אמיתית (ללא ציר Y)
        Vector3 delta = transform.position - previousPosition;
        delta.y = 0f;
        float rawSpeed = delta.magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // Smoothing
        currentSpeed = Mathf.Lerp(currentSpeed, rawSpeed, Time.deltaTime * animationSmoothing);

        float normalized = Mathf.Clamp01(currentSpeed / maxWalkSpeed);

        if (hasSpeedParam)
            avatarAnimator.SetFloat(speedParameterName, normalized);

        if (hasIsWalkingParam)
            avatarAnimator.SetBool(isWalkingParameterName, currentSpeed > walkThreshold);
    }
}