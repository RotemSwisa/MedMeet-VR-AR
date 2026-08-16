using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SurgeryManager : MonoBehaviour
{
    [Header("Connections")]
    public LineRenderer targetLine;
    public Transform controllerTransform;
    public TextMeshProUGUI scoreText;

    [Header("Laser Cutting Settings")]
    public float minMoveDistance = 0.005f;
    public LayerMask heartLayer;

    [Header("Improved Scoring Settings")]
    public float perfectDistance = 0.06f;
    public float maxDistance = 0.20f;

    [Header("Cut Visualization")]
    public Color cutColor = Color.red;
    public float cutWidth = 0.015f;

    [Header("Heart Animation")]
    public Transform leftHeartPart;
    public Transform rightHeartPart;
    public float openDistance = 0.015f;
    public float openSpeed = 1.5f;
    public float clearLineDelay = 0.5f;

    private List<Vector3> userPath = new List<Vector3>();
    private List<Vector3> visualPath = new List<Vector3>();

    private bool isCutting = false;
    private bool surgeryComplete = false;

    private InputDevice rightController;
    private LineRenderer cutTrail;

    private Plane heartSurfacePlane;

    // --- משתנים לשמירת המיקום המקורי בשביל הריסטארט ---
    private Vector3 leftHeartStartPos;
    private Vector3 rightHeartStartPos;

    void Awake()
    {
        // שומרים את המיקומים מיד כשהמשחק עולה, לפני שום דבר אחר!
        if (leftHeartPart != null) leftHeartStartPos = leftHeartPart.localPosition;
        if (rightHeartPart != null) rightHeartStartPos = rightHeartPart.localPosition;
    }

    void Start()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        if (devices.Count > 0)
            rightController = devices[0];

        GameObject trailObj = new GameObject("CutTrail_Visual");
        trailObj.transform.SetParent(transform);

        cutTrail = trailObj.AddComponent<LineRenderer>();

        Material cutMat = new Material(Shader.Find("GUI/Text Shader"));
        cutMat.SetInt(
            "unity_GUIZTestMode",
            (int)UnityEngine.Rendering.CompareFunction.Always
        );

        cutTrail.material = cutMat;
        cutTrail.startColor = cutColor;
        cutTrail.endColor = cutColor;
        cutTrail.startWidth = cutWidth;
        cutTrail.endWidth = cutWidth;
        cutTrail.positionCount = 0;
        cutTrail.useWorldSpace = true;

        if (targetLine != null && targetLine.positionCount >= 2)
        {
            BuildHeartPlane();
        }


    }

    void BuildHeartPlane()
    {
        Vector3[] pts = GetTargetPointsInWorldSpace();
        if (pts.Length < 2) return;

        Vector3 lineCenter = (pts[0] + pts[pts.Length - 1]) / 2f;
        Vector3 normal = (controllerTransform.position - lineCenter).normalized;
        heartSurfacePlane = new Plane(normal, lineCenter);
    }

    void Update()
    {
        if (surgeryComplete) return;

        bool triggerPressed = false;
        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

        bool keyboardPress = Input.GetKey(KeyCode.Space);
        bool isPressed = triggerPressed || keyboardPress;

        RaycastHit hit;
        bool isHittingHeart = false;

        Vector3 actualHitPoint = Vector3.zero;
        Vector3 projectedHitPoint = Vector3.zero;

        if (controllerTransform != null &&
            Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit, 10f, heartLayer))
        {
            isHittingHeart = true;
            actualHitPoint = hit.point;

            Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
            float enter;

            projectedHitPoint = heartSurfacePlane.Raycast(ray, out enter) ? ray.GetPoint(enter) : hit.point;
        }

        if (isPressed && isHittingHeart && !isCutting)
            StartSurgery();
        else if (!isPressed && isCutting)
            EndSurgery();

        if (isCutting && isHittingHeart)
        {
            if (userPath.Count == 0 || Vector3.Distance(actualHitPoint, visualPath[visualPath.Count - 1]) > minMoveDistance)
            {
                userPath.Add(projectedHitPoint);
                visualPath.Add(actualHitPoint);
                UpdateCutTrail();
            }
        }
    }

    void StartSurgery()
    {
        userPath.Clear();
        visualPath.Clear();
        cutTrail.positionCount = 0;
        isCutting = true;

        if (targetLine != null && targetLine.positionCount >= 2)
        {
            BuildHeartPlane();
        }

        if (scoreText != null)
        {
            scoreText.text = "Heart Surgery: Operating...";
        }
    }

    void EndSurgery()
    {
        isCutting = false;

        if (userPath.Count == 0)
            return;

        float score = CalculateScore();

        FindObjectOfType<SurgeryUIManager>()?.OnCutAttempt(score, score >= 90f);

        if (SimulationManager.Instance != null)
            SimulationManager.Instance.AddAttempt("Heart", score);

        string feedback = "";
        string colorHex = "";

        if (score >= 90f)
        {
            feedback = "Excellent! Incision successful.";
            colorHex = "#2ECC71";
            surgeryComplete = true;
            TriggerHeartOpen(score);
        }
        else if (score >= 70f)
        {
            feedback = "Almost there... More precision required.";
            colorHex = "#F1C40F";
        }
        else
        {
            feedback = "Incision failed! Patient at risk.";
            colorHex = "#E74C3C";
        }

        string result = $"<color={colorHex}><b>Heart Surgery Score: {score:F1}%</b></color>\n<size=70%><color=#FFFFFF>{feedback}</color></size>";

        if (scoreText != null)
        {
            scoreText.text = result;
        }
    }

    float CalculateScore()
    {
        Vector3[] targetPoints = GetTargetPointsInWorldSpace();

        if (targetPoints.Length < 2 || userPath.Count < 2)
        {
            return 0f;
        }

        float totalAccuracy = 0f;

        foreach (Vector3 p in userPath)
        {
            float closest = float.MaxValue;

            for (int i = 0; i < targetPoints.Length - 1; i++)
            {
                float d = DistancePointToSegmentProjected(p, targetPoints[i], targetPoints[i + 1]);
                if (d < closest) closest = d;
            }

            float pointScore = 0f;

            if (closest <= perfectDistance) pointScore = 100f;
            else if (closest < maxDistance)
            {
                float penalty = (closest - perfectDistance) / (maxDistance - perfectDistance);
                pointScore = 100f * (1f - penalty);
            }

            totalAccuracy += pointScore;
        }

        float finalAccuracy = totalAccuracy / userPath.Count;

        List<Vector3> denseTargetPoints = new List<Vector3>();
        float sampleStep = 0.01f;

        for (int i = 0; i < targetPoints.Length - 1; i++)
        {
            Vector3 start = targetPoints[i];
            Vector3 end = targetPoints[i + 1];

            float dist = Vector3.Distance(start, end);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / sampleStep));

            for (int j = 0; j <= steps; j++)
            {
                Vector3 p = Vector3.Lerp(start, end, (float)j / steps);
                if (denseTargetPoints.Count == 0 || Vector3.Distance(denseTargetPoints[denseTargetPoints.Count - 1], p) > 0.001f)
                {
                    denseTargetPoints.Add(p);
                }
            }
        }

        int coveredPoints = 0;
        float coverageThreshold = perfectDistance * 1.5f;

        foreach (Vector3 tp in denseTargetPoints)
        {
            bool isCovered = false;

            for (int i = 0; i < userPath.Count - 1; i++)
            {
                float d = DistancePointToSegmentProjected(tp, userPath[i], userPath[i + 1]);
                if (d <= coverageThreshold)
                {
                    isCovered = true;
                    break;
                }
            }

            if (isCovered) coveredPoints++;
        }

        float coverageScore = ((float)coveredPoints / denseTargetPoints.Count) * 100f;

        float finalScore = (finalAccuracy * 0.6f) + (coverageScore * 0.4f);
        return Mathf.Clamp(finalScore, 0f, 100f);
    }

    float DistancePointToSegmentProjected(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 projPoint = heartSurfacePlane.ClosestPointOnPlane(point);
        Vector3 projA = heartSurfacePlane.ClosestPointOnPlane(a);
        Vector3 projB = heartSurfacePlane.ClosestPointOnPlane(b);

        Vector3 ab = projB - projA;
        Vector3 ap = projPoint - projA;

        float sqrMag = ab.sqrMagnitude;

        if (sqrMag < 0.000001f)
        {
            return Vector3.Distance(projPoint, projA);
        }

        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / sqrMag);
        Vector3 closestPoint = projA + t * ab;

        return Vector3.Distance(projPoint, closestPoint);
    }

    Vector3[] GetTargetPointsInWorldSpace()
    {
        Vector3[] points = new Vector3[targetLine.positionCount];

        for (int i = 0; i < targetLine.positionCount; i++)
        {
            Vector3 p = targetLine.GetPosition(i);
            if (!targetLine.useWorldSpace) p = targetLine.transform.TransformPoint(p);
            points[i] = p;
        }

        return points;
    }

    void UpdateCutTrail()
    {
        cutTrail.positionCount = visualPath.Count;
        cutTrail.SetPositions(visualPath.ToArray());
    }

    public void TriggerHeartOpen(float finalScore)
    {
        if (leftHeartPart != null && rightHeartPart != null)
        {
            StartCoroutine(AnimateHeartOpen(finalScore));
        }
    }

    private IEnumerator AnimateHeartOpen(float finalScore)
    {
        yield return new WaitForSeconds(clearLineDelay);

        cutTrail.positionCount = 0;
        if (targetLine != null) targetLine.gameObject.SetActive(false);

        Vector3 leftStart = leftHeartPart.localPosition;
        Vector3 rightStart = rightHeartPart.localPosition;

        Vector3 leftTarget = leftStart + new Vector3(openDistance, 0, 0);
        Vector3 rightTarget = rightStart + new Vector3(-openDistance, 0, 0);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * openSpeed;

            leftHeartPart.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
            rightHeartPart.localPosition = Vector3.Lerp(rightStart, rightTarget, t);

            yield return null;
        }

        if (scoreText != null)
        {
            scoreText.text += "\n<color=#3498DB><b>Heart Surgery Completed!</b></color>";
        }

        var __ui = FindObjectOfType<SurgeryUIManager>();
        if (__ui != null)
        {
            __ui.OnOrganCompleted(finalScore);
        }
        else if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.CompleteHeart(finalScore);
        }
    }

    // --- הפונקציה החדשה לאיפוס הלב מתוך מנהל הסימולציה ---
    public void ResetHeartState()
    {
        StopAllCoroutines(); // עוצר את אנימציית הפתיחה אם היא במקרה רצה
        surgeryComplete = false;
        isCutting = false;
        userPath.Clear();
        visualPath.Clear();

        if (cutTrail != null) cutTrail.positionCount = 0;
        if (targetLine != null) targetLine.gameObject.SetActive(true);

        if (leftHeartPart != null) leftHeartPart.localPosition = leftHeartStartPos;
        if (rightHeartPart != null) rightHeartPart.localPosition = rightHeartStartPos;
    }
}