using UnityEngine;
using Normal.Realtime;

public class RealtimeAvatarInputs : MonoBehaviour
{
    [Header("Targets (מוגדר אוטומטית ע\"י ה-Switcher)")]
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Avatar Parts (גרור לכאן מתוך הפריפאב)")]
    public Transform avatarHead;
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

    private Transform lastHead;
    private Transform lastLeftHand;
    private Transform lastRightHand;

    void LateUpdate()
    {
        // בדוק אם משהו השתנה
        if (head != lastHead || leftHand != lastLeftHand || rightHand != lastRightHand)
        {
            Debug.Log($"[RealtimeAvatarInputs] References updated - Head: {head?.name}, Left: {leftHand?.name}, Right: {rightHand?.name}");
            lastHead = head;
            lastLeftHand = leftHand;
            lastRightHand = rightHand;
        }

        // עדכן רק אם יש targets תקינים
        if (head != null && avatarHead != null)
        {
            avatarHead.position = head.position;
            avatarHead.rotation = head.rotation;
        }
        else if (head == null && avatarHead != null)
        {
            Debug.LogWarning("[RealtimeAvatarInputs] Head target is null!");
        }

        if (leftHand != null && avatarLeftHand != null)
        {
            avatarLeftHand.position = leftHand.position;
            avatarLeftHand.rotation = leftHand.rotation;
        }

        if (rightHand != null && avatarRightHand != null)
        {
            avatarRightHand.position = rightHand.position;
            avatarRightHand.rotation = rightHand.rotation;
        }
    }
}
