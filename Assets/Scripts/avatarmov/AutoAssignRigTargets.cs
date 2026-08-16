using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AutoAssignRigTargets : MonoBehaviour
{
    [Header("Rig Targets - צור Empty GameObjects תחת Rig 1")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [Header("Constraints")]
    public MultiRotationConstraint[] spineConstraints;
    public MultiRotationConstraint headConstraint;
    public TwoBoneIKConstraint leftHandIK;
    public TwoBoneIKConstraint rightHandIK;

    [Header("Smoothing")]
    public float positionSmoothing = 15f;
    public float rotationSmoothing = 15f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Transform vrCamera;
    private Transform leftController;
    private Transform rightController;

    void Start()
    {
        FindVRComponents();
        AssignConstraintTargets();
    }

    void FindVRComponents()
    {
        vrCamera = Camera.main?.transform;
        if (vrCamera == null)
        {
            Camera cam = FindObjectOfType<Camera>();
            if (cam != null) vrCamera = cam.transform;
        }

        if (vrCamera != null && showDebugInfo)
            Debug.Log("✅ Found VR Camera: " + vrCamera.name);

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            string lowerName = obj.name.ToLower();
            if (lowerName.Contains("lefthand") || lowerName.Contains("left hand"))
                leftController = obj.transform;
            if (lowerName.Contains("righthand") || lowerName.Contains("right hand"))
                rightController = obj.transform;
        }
    }

    void AssignConstraintTargets()
    {
        // חיבור Spine Constraints ל-HeadTarget
        if (headTarget != null && spineConstraints != null)
        {
            foreach (var constraint in spineConstraints)
            {
                if (constraint != null)
                {
                    var data = constraint.data;
                    data.sourceObjects.Clear();
                    data.sourceObjects.Add(new WeightedTransform(headTarget, 1f));
                    constraint.data = data;
                }
            }
        }

        // חיבור Head Constraint
        if (headTarget != null && headConstraint != null)
        {
            var data = headConstraint.data;
            data.sourceObjects.Clear();
            data.sourceObjects.Add(new WeightedTransform(headTarget, 1f));
            headConstraint.data = data;
        }

        // חיבור Hand IK
        if (leftHandTarget != null && leftHandIK != null)
        {
            var data = leftHandIK.data;
            data.target = leftHandTarget;
            leftHandIK.data = data;
        }

        if (rightHandTarget != null && rightHandIK != null)
        {
            var data = rightHandIK.data;
            data.target = rightHandTarget;
            rightHandIK.data = data;
        }
    }

    void LateUpdate()
    {
        // עדכון HeadTarget לעקוב אחרי VR Camera
        if (vrCamera != null && headTarget != null)
        {
            headTarget.position = Vector3.Lerp(headTarget.position, vrCamera.position, Time.deltaTime * positionSmoothing);
            headTarget.rotation = Quaternion.Slerp(headTarget.rotation, vrCamera.rotation, Time.deltaTime * rotationSmoothing);
        }

        // עדכון Hand Targets
        if (leftController != null && leftHandTarget != null)
        {
            leftHandTarget.position = Vector3.Lerp(leftHandTarget.position, leftController.position, Time.deltaTime * positionSmoothing);
            leftHandTarget.rotation = Quaternion.Slerp(leftHandTarget.rotation, leftController.rotation, Time.deltaTime * rotationSmoothing);
        }

        if (rightController != null && rightHandTarget != null)
        {
            rightHandTarget.position = Vector3.Lerp(rightHandTarget.position, rightController.position, Time.deltaTime * positionSmoothing);
            rightHandTarget.rotation = Quaternion.Slerp(rightHandTarget.rotation, rightController.rotation, Time.deltaTime * rotationSmoothing);
        }
    }
}