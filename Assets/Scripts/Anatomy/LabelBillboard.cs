using UnityEngine;

/// <summary>
/// קומפוננטה דקה שמסובבת אובייקט כך שיפנה תמיד למצלמה.
/// משמש לתוויות צפות שצריכות להיות קריאות מכל זווית.
/// </summary>
public class LabelBillboard : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        // הופך את האובייקט לפנות מולנו - הכיוון של "פנים" הוא מהמצלמה החוצה
        Vector3 forward = transform.position - cam.transform.position;
        if (forward.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
