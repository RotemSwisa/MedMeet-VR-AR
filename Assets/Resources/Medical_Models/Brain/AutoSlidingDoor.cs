using UnityEngine;

/// <summary>
/// AutoSlidingDoor - דלתות הזזה אוטומטיות שנפתחות כשהשחקן מתקרב.
///
/// איך להשתמש:
///   1. בחר את ה-GameObject של הדלת ב-Hierarchy (או צור GameObject ריק שיהיה ה"מנהל" של הדלתות)
///   2. גרור עליו את הסקריפט הזה
///   3. גרור את ה-Transform של הלוח השמאלי לשדה "Left Panel"
///   4. גרור את הלוח הימני לשדה "Right Panel"
///   5. הגדר Player Anchor = ה-Transform של ה-VR camera (או השחקן)
///   6. שנה Open Distance / Trigger Range לפי הצורך
///   7. הרץ - הדלת תפתח אוטומטית כשתתקרב
///
/// אם יש לך דלת רגילה (לא הזזה) - השאר Left Panel ושים שם את הדלת היחידה.
/// הסקריפט יסובב אותה במקום להזיז (Use Rotation = true).
/// </summary>
[DisallowMultipleComponent]
public class AutoSlidingDoor : MonoBehaviour
{
    [Header("── Door Panels ──")]
    [Tooltip("הלוח השמאלי של הדלת (זז שמאלה)")]
    public Transform leftPanel;
    [Tooltip("הלוח הימני של הדלת (זז ימינה). אם null - רק לוח שמאלי")]
    public Transform rightPanel;

    [Header("── Player Detection ──")]
    [Tooltip("ה-Transform של השחקן (בד״כ ה-VR camera או XR Origin)")]
    public Transform playerAnchor;
    [Tooltip("מרחק בו הדלת תפתח")]
    public float triggerRange = 3f;
    [Tooltip("האם לחפש את השחקן אוטומטית (לפי tag MainCamera)")]
    public bool autoFindPlayer = true;

    [Header("── Animation ──")]
    [Tooltip("מרחק פתיחה במטרים (כמה הלוח זז הצידה)")]
    public float openDistance = 1.0f;
    [Tooltip("כיוון פתיחה - איזה ציר זז (X=שמאל/ימין, Z=קדימה/אחורה)")]
    public OpenAxis openAxis = OpenAxis.X;
    [Tooltip("מהירות פתיחה/סגירה")]
    public float openSpeed = 3f;
    [Tooltip("אם דלוק - הדלת תסתובב במקום להזיז (לדלת ציר)")]
    public bool useRotation = false;
    [Tooltip("זווית פתיחה במעלות (רק אם Use Rotation דלוק)")]
    public float openAngle = 90f;

    [Header("── Sound (optional) ──")]
    [Tooltip("צליל פתיחה/סגירה")]
    public AudioClip openSound;
    public AudioClip closeSound;

    Vector3 leftStartPos, rightStartPos;
    Quaternion leftStartRot, rightStartRot;
    float currentOpenT = 0f;
    bool isOpen = false;
    AudioSource audioSrc;

    public enum OpenAxis { X, Y, Z }

    void Awake()
    {
        if (leftPanel != null)
        {
            leftStartPos = leftPanel.localPosition;
            leftStartRot = leftPanel.localRotation;
        }
        if (rightPanel != null)
        {
            rightStartPos = rightPanel.localPosition;
            rightStartRot = rightPanel.localRotation;
        }

        if (openSound != null || closeSound != null)
        {
            audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 1f;
        }
    }

    void Start()
    {
        if (autoFindPlayer && playerAnchor == null)
        {
            var cam = Camera.main;
            if (cam != null) playerAnchor = cam.transform;
        }
    }

    void Update()
    {
        if (playerAnchor == null) return;

        float dist = Vector3.Distance(transform.position, playerAnchor.position);
        bool shouldBeOpen = dist <= triggerRange;

        // Trigger sound on state change
        if (shouldBeOpen && !isOpen)
        {
            PlaySound(openSound);
            isOpen = true;
        }
        else if (!shouldBeOpen && isOpen)
        {
            PlaySound(closeSound);
            isOpen = false;
        }

        // Animate open/close
        float target = shouldBeOpen ? 1f : 0f;
        currentOpenT = Mathf.MoveTowards(currentOpenT, target, Time.deltaTime * openSpeed);

        ApplyOpenState(currentOpenT);
    }

    void ApplyOpenState(float t)
    {
        Vector3 offsetAxis = OpenAxisToVector(openAxis);

        if (useRotation)
        {
            // Rotation-based opening (for hinged doors)
            if (leftPanel != null)
                leftPanel.localRotation = leftStartRot * Quaternion.AngleAxis(openAngle * t, offsetAxis);
            if (rightPanel != null)
                rightPanel.localRotation = rightStartRot * Quaternion.AngleAxis(-openAngle * t, offsetAxis);
        }
        else
        {
            // Sliding open
            if (leftPanel != null)
                leftPanel.localPosition = leftStartPos + offsetAxis * (-openDistance * t);
            if (rightPanel != null)
                rightPanel.localPosition = rightStartPos + offsetAxis * (openDistance * t);
        }
    }

    Vector3 OpenAxisToVector(OpenAxis axis)
    {
        switch (axis)
        {
            case OpenAxis.X: return Vector3.right;
            case OpenAxis.Y: return Vector3.up;
            case OpenAxis.Z: return Vector3.forward;
        }
        return Vector3.right;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSrc == null) return;
        audioSrc.PlayOneShot(clip);
    }

    // Visualize the trigger range in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.78f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}
