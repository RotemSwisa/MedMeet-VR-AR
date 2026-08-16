using UnityEngine;

public class VRBodySync : MonoBehaviour
{
    [Tooltip("גרור לפה את אובייקט ה-Head")]
    public Transform headCamera;

    [Header("Settings")]
    public Vector3 positionOffset = new Vector3(0, -1.6f, -0.2f);
    public float rotationSmoothness = 5f;

    void LateUpdate()
    {
        if (headCamera == null) return;

        // 1. חישוב הסיבוב של הראש (רק לצדדים, בלי למעלה/למטה)
        Quaternion headRotationFlat = Quaternion.Euler(0, headCamera.eulerAngles.y, 0);

        // 2. חישוב המיקום החדש
        // הקסם: אנחנו מכפילים בסיבוב, כדי שה-Offset יהיה תמיד יחסי לכיוון המבט
        Vector3 currentOffset = headRotationFlat * positionOffset;

        transform.position = headCamera.position + currentOffset;

        // 3. סיבוב הגוף שילך אחרי הראש
        transform.rotation = Quaternion.Lerp(transform.rotation, headRotationFlat, Time.deltaTime * rotationSmoothness);
    }
}