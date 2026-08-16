using UnityEngine;
using UnityEngine.XR;
using System.Collections;

/// <summary>
/// מסנכרן את ידי האווטר לשלטי Meta Quest 3
/// מוצא את כל ה-references אוטומטית בזמן ריצה - מתאים לפריפב של Normcore
///
/// איך לחבר:
/// 1. גרור סקריפט זה לתוך הפריפב על Male_avatar (האב)
/// 2. אין צורך לגרור כלום ב-Inspector - הכל נמצא אוטומטית
/// 3. אם שמות העצמות שלך שונים, שנה את הקבועים בחלק העליון
/// </summary>
public class VRHandIK : MonoBehaviour
{
    [Header("=== שמות עצמות (שנה אם צריך) ===")]
    public string leftUpperArmName = "LeftArm";
    public string leftForeArmName = "LeftForeArm";
    public string leftHandBoneName = "LeftHand";
    public string rightUpperArmName = "RightArm";
    public string rightForeArmName = "RightForeArm";
    public string rightHandBoneName = "RightHand";

    [Header("=== כיוון המרפק ===")]
    [Tooltip("שנה בין -1 ל-1 אם המרפק יוצא לכיוון לא נכון")]
    public float elbowBendLeft = -1f;
    public float elbowBendRight = 1f;

    [Header("=== Offsets ===")]
    public Vector3 leftPositionOffset = new Vector3(0f, 0f, -0.05f);
    public Vector3 rightPositionOffset = new Vector3(0f, 0f, -0.05f);
    public Vector3 leftRotationOffset = Vector3.zero;
    public Vector3 rightRotationOffset = Vector3.zero;

    [Header("=== Smoothing ===")]
    [Range(1f, 30f)]
    public float smoothing = 20f;

    [Header("=== Debug ===")]
    public bool showDebugLogs = true;

    private Transform leftController;
    private Transform rightController;
    private Transform leftUpperArm;
    private Transform leftForeArm;
    private Transform leftHandBone;
    private Transform rightUpperArm;
    private Transform rightForeArm;
    private Transform rightHandBone;

    private bool isReady = false;

    void Start()
    {
        StartCoroutine(InitializeAfterFrame());
    }

    IEnumerator InitializeAfterFrame()
    {
        // מחכה שני פריימים כדי לתת ל-Normcore לסיים לטעון
        yield return null;
        yield return null;

        FindControllers();
        FindArmBones();

        isReady = leftController != null
               && rightController != null
               && leftHandBone != null
               && rightHandBone != null;

        if (isReady)
            Log("מוכן - כל ה-references נמצאו בהצלחה");
        else
            LogWarning("חלק מה-references לא נמצאו! בדוק את Console");
    }

    void FindControllers()
    {
        // חיפוש XR Origin בסצנה לפי שמות אפשריים
        GameObject xrOrigin = GameObject.Find("XR Origin (VR)")
                           ?? GameObject.Find("XR Origin")
                           ?? GameObject.Find("XR Rig")
                           ?? GameObject.Find("[XR Rig]");

        if (xrOrigin == null)
        {
            LogWarning("לא נמצא אובייקט XR Origin בסצנה!");
            return;
        }

        Transform[] allChildren = xrOrigin.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            string n = t.name;
            if (n == "Left Hand Controller" || n == "LeftHandController" || n == "LeftController")
                leftController = t;
            if (n == "Right Hand Controller" || n == "RightHandController" || n == "RightController")
                rightController = t;
        }

        Log(leftController != null ? $"Left Controller נמצא: {leftController.name}" : "Left Controller לא נמצא!");
        Log(rightController != null ? $"Right Controller נמצא: {rightController.name}" : "Right Controller לא נמצא!");
    }

    void FindArmBones()
    {
        // חיפוש עצמות בתוך הפריפב עצמו
        Transform[] allBones = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allBones)
        {
            string n = t.name;
            if (n == leftUpperArmName) leftUpperArm = t;
            if (n == leftForeArmName) leftForeArm = t;
            if (n == leftHandBoneName) leftHandBone = t;
            if (n == rightUpperArmName) rightUpperArm = t;
            if (n == rightForeArmName) rightForeArm = t;
            if (n == rightHandBoneName) rightHandBone = t;
        }

        Log(leftHandBone != null ? $"יד שמאל נמצאה: {leftHandBone.name}" : $"עצם '{leftHandBoneName}' לא נמצאה!");
        Log(rightHandBone != null ? $"יד ימין נמצאה: {rightHandBone.name}" : $"עצם '{rightHandBoneName}' לא נמצאה!");
    }

    void LateUpdate()
    {
        if (!isReady) return;

        SolveArmIK(leftUpperArm, leftForeArm, leftHandBone,
                   leftController, leftPositionOffset, leftRotationOffset,
                   elbowBendLeft, true);

        SolveArmIK(rightUpperArm, rightForeArm, rightHandBone,
                   rightController, rightPositionOffset, rightRotationOffset,
                   elbowBendRight, false);
    }

    void SolveArmIK(
        Transform upperArm, Transform foreArm, Transform hand,
        Transform controller, Vector3 posOffset, Vector3 rotOffset,
        float elbowBend, bool isLeft)
    {
        if (upperArm == null || foreArm == null || hand == null) return;

        Vector3 targetPos = controller.position + controller.TransformDirection(posOffset);
        Quaternion targetRot = controller.rotation * Quaternion.Euler(rotOffset);

        float upperLen = Vector3.Distance(upperArm.position, foreArm.position);
        float lowerLen = Vector3.Distance(foreArm.position, hand.position);
        float totalLen = upperLen + lowerLen;

        Vector3 toTarget = targetPos - upperArm.position;
        float dist = Mathf.Clamp(toTarget.magnitude, 0.001f, totalLen * 0.999f);

        // Law of cosines - זווית המרפק
        float cosAngle = (upperLen * upperLen + dist * dist - lowerLen * lowerLen)
                         / (2f * upperLen * dist);
        float angle = Mathf.Acos(Mathf.Clamp(cosAngle, -1f, 1f)) * Mathf.Rad2Deg;

        // Pole vector - כיוון שאליו המרפק "מתכופף"
        Vector3 poleDir = upperArm.TransformDirection(
            new Vector3(elbowBend, 0f, -0.5f)).normalized;

        // סיבוב הזרוע העליונה
        Quaternion upperTarget = Quaternion.LookRotation(toTarget.normalized, poleDir)
                               * Quaternion.Euler(angle, 0f, 0f);
        upperArm.rotation = Quaternion.Slerp(upperArm.rotation, upperTarget,
                                             Time.deltaTime * smoothing);

        // סיבוב האמה
        Vector3 toHand = targetPos - foreArm.position;
        if (toHand.sqrMagnitude > 0.0001f)
        {
            foreArm.rotation = Quaternion.Slerp(foreArm.rotation,
                Quaternion.LookRotation(toHand.normalized, foreArm.up),
                Time.deltaTime * smoothing);
        }

        // סיבוב הכף יד בדיוק לפי השלט
        hand.rotation = Quaternion.Slerp(hand.rotation, targetRot,
                                         Time.deltaTime * smoothing);
    }

    void Log(string msg) { if (showDebugLogs) Debug.Log("[VRHandIK] " + msg); }
    void LogWarning(string msg) { Debug.LogWarning("[VRHandIK] " + msg); }
}