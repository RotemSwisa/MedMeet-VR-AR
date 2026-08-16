using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BrainSurgeryManager : MonoBehaviour
{
    [Header("Connections")]
    public Transform controllerTransform;
    public TextMeshProUGUI scoreText;

    [Header("Step 1 Settings")]
    public LineRenderer lineStep1;
    public Transform brainPart06;

    [Header("Step 2 Settings")]
    public LineRenderer lineStep2;
    public Transform brainPart04;

    [Header("Laser Cutting Settings")]
    public float minMoveDistance = 0.005f;
    public LayerMask brainLayer;

    [Header("Improved Scoring Settings")]
    public float perfectDistance = 0.06f;
    public float maxDistance = 0.20f;

    [Header("Cut Visualization")]
    public Color cutColor = Color.red;
    public float cutWidth = 0.015f;

    [Header("Brain Animation & Timing")]
    public float openDistance = 0.2f;
    public float openSpeed = 1.5f;
    public float clearLineDelay = 0.5f;

    private List<Vector3> userPath = new List<Vector3>();
    private List<Vector3> visualPath = new List<Vector3>();

    private bool isCutting = false;
    private bool isAnimating = false;
    private int currentStep = 1;

    private InputDevice rightController;
    private LineRenderer cutTrail;
    private Plane brainSurfacePlane;

    // --- ������ ������ ������ ������ ����� �������� ---
    private Vector3 part06StartPos;
    private Vector3 part04StartPos;

    void Awake()
    {
        // ������ �� �������� ��� ������� ����
        if (brainPart06 != null) part06StartPos = brainPart06.localPosition;
        if (brainPart04 != null) part04StartPos = brainPart04.localPosition;
    }

    void Start()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        if (devices.Count > 0)
            rightController = devices[0];

        GameObject trailObj = new GameObject("BrainCutTrail_Visual");
        trailObj.transform.SetParent(transform);

        cutTrail = trailObj.AddComponent<LineRenderer>();
        Material cutMat = new Material(Shader.Find("GUI/Text Shader"));
        cutMat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        cutTrail.material = cutMat;
        cutTrail.startColor = cutColor;
        cutTrail.endColor = cutColor;
        cutTrail.startWidth = cutWidth;
        cutTrail.endWidth = cutWidth;
        cutTrail.positionCount = 0;
        cutTrail.useWorldSpace = true;

        currentStep = 1;
        if (lineStep2 != null) lineStep2.gameObject.SetActive(false);
        if (lineStep1 != null) lineStep1.gameObject.SetActive(true);

        BuildBrainPlane();

    }

    LineRenderer GetActiveLine()
    {
        return currentStep == 1 ? lineStep1 : lineStep2;
    }

    void BuildBrainPlane()
    {
        LineRenderer activeLine = GetActiveLine();
        if (activeLine == null) return;

        Vector3[] pts = GetTargetPointsInWorldSpace(activeLine);
        if (pts.Length < 2) return;

        Vector3 lineCenter = (pts[0] + pts[pts.Length - 1]) / 2f;

        if (controllerTransform != null)
        {
            Vector3 normal = (controllerTransform.position - lineCenter).normalized;
            brainSurfacePlane = new Plane(normal, lineCenter);
        }
    }

    void Update()
    {
        if (isAnimating || currentStep > 2) return;

        bool triggerPressed = false;
        rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        bool keyboardPress = Input.GetKey(KeyCode.Space);
        bool isPressed = triggerPressed || keyboardPress;

        RaycastHit hit;
        bool isHittingBrain = false;
        Vector3 actualHitPoint = Vector3.zero;
        Vector3 projectedHitPoint = Vector3.zero;

        if (controllerTransform != null &&
            Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit, 10f, brainLayer))
        {
            isHittingBrain = true;
            actualHitPoint = hit.point;

            Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
            float enter;

            projectedHitPoint = brainSurfacePlane.Raycast(ray, out enter) ? ray.GetPoint(enter) : hit.point;
        }

        if (isPressed && isHittingBrain && !isCutting)
            StartSurgery();
        else if (!isPressed && isCutting)
            EndSurgery();

        if (isCutting && isHittingBrain)
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

        BuildBrainPlane();

        if (scoreText != null)
        {
            scoreText.text = $"Brain Step {currentStep}: Operating...";
        }
    }

    void EndSurgery()
    {
        isCutting = false;
        if (userPath.Count == 0) return;

        float score = CalculateScore();

        FindObjectOfType<SurgeryUIManager>()?.OnCutAttempt(score, score >= 90f);

        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.AddAttempt("Brain" + currentStep, score);
        }

        string feedback = "";
        string colorHex = "";

        if (score >= 90f)
        {
            feedback = $"Excellent! Step {currentStep} successful.";
            colorHex = "#2ECC71";

            if (currentStep == 1)
            {
                StartCoroutine(TransitionToStep2());
            }
            else if (currentStep == 2)
            {
                StartCoroutine(FinishSurgery(score));
            }
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

        string result = $"<color={colorHex}><b>Brain Step {currentStep} Score: {score:F1}%</b></color>\n<size=70%><color=#FFFFFF>{feedback}</color></size>";

        if (scoreText != null)
        {
            scoreText.text = result;
        }
    }

    IEnumerator TransitionToStep2()
    {
        isAnimating = true;

        yield return new WaitForSeconds(clearLineDelay);

        cutTrail.positionCount = 0;
        if (lineStep1 != null) lineStep1.gameObject.SetActive(false);

        if (brainPart06 != null)
        {
            Vector3 startPos = brainPart06.localPosition;
            Vector3 targetPos = startPos + (Vector3.right * openDistance);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                brainPart06.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        userPath.Clear();
        visualPath.Clear();

        if (lineStep2 != null) lineStep2.gameObject.SetActive(true);

        currentStep = 2;
        BuildBrainPlane();
        isAnimating = false;
    }

    IEnumerator FinishSurgery(float finalScore)
    {
        isAnimating = true;
        currentStep = 3;

        yield return new WaitForSeconds(clearLineDelay);

        cutTrail.positionCount = 0;
        if (lineStep2 != null) lineStep2.gameObject.SetActive(false);

        if (brainPart04 != null)
        {
            Vector3 startPos = brainPart04.localPosition;
            Vector3 targetPos = startPos + (Vector3.left * openDistance);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                brainPart04.localPosition = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        if (scoreText != null)
        {
            scoreText.text += "\n<color=#3498DB><b>Brain Surgery Completed!</b></color>";
        }

        var __ui = FindObjectOfType<SurgeryUIManager>();
        if (__ui != null)
        {
            __ui.OnOrganCompleted(finalScore);
        }
        else if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.CompleteBrain(finalScore);
        }

        isAnimating = false;
    }

    float CalculateScore()
    {
        LineRenderer activeLine = GetActiveLine();
        Vector3[] targetPoints = GetTargetPointsInWorldSpace(activeLine);

        if (targetPoints.Length < 2 || userPath.Count < 2) return 0f;

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
        Vector3 projPoint = brainSurfacePlane.ClosestPointOnPlane(point);
        Vector3 projA = brainSurfacePlane.ClosestPointOnPlane(a);
        Vector3 projB = brainSurfacePlane.ClosestPointOnPlane(b);

        Vector3 ab = projB - projA;
        Vector3 ap = projPoint - projA;
        float sqrMag = ab.sqrMagnitude;

        if (sqrMag < 0.000001f) return Vector3.Distance(projPoint, projA);

        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / sqrMag);
        Vector3 closestPoint = projA + t * ab;
        return Vector3.Distance(projPoint, closestPoint);
    }

    Vector3[] GetTargetPointsInWorldSpace(LineRenderer line)
    {
        Vector3[] points = new Vector3[line.positionCount];
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 p = line.GetPosition(i);
            if (!line.useWorldSpace) p = line.transform.TransformPoint(p);
            points[i] = p;
        }
        return points;
    }

    void UpdateCutTrail()
    {
        cutTrail.positionCount = visualPath.Count;
        cutTrail.SetPositions(visualPath.ToArray());
    }

    // --- �������� ����� ������ ���� ���� ���� ��������� ---
    public void ResetBrainState()
    {
        StopAllCoroutines(); // ���� �� ��������� �� ����
        currentStep = 1;
        isCutting = false;
        isAnimating = false;
        userPath.Clear();
        visualPath.Clear();

        if (cutTrail != null) cutTrail.positionCount = 0;

        if (lineStep1 != null) lineStep1.gameObject.SetActive(true);
        if (lineStep2 != null) lineStep2.gameObject.SetActive(false);

        if (brainPart06 != null) brainPart06.localPosition = part06StartPos;
        if (brainPart04 != null) brainPart04.localPosition = part04StartPos;

        BuildBrainPlane();
    }
}