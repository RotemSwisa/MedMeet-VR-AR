using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Moves the controller visual spheres (LeftHandModel / RightHandModel) to exactly
/// match the avatar's actual wrist bone position — computed by VRIKController.
///
/// Also changes all XRInteractorLineVisual laser colors on startup:
///   Normal (invalid) → calm teal    (#4DD8F0)
///   Hover  (valid)   → white        (unchanged)
///
/// Attach this script to the DoctorPlayer / Femaledoctor ROOT GameObject.
/// </summary>
public class HandSphereSync : MonoBehaviour
{
    [Header("Avatar Animator (for bone transforms)")]
    [Tooltip("The Animator that drives the avatar body (VRIKController lives here).")]
    public Animator bodyAnimator;

    [Header("Visual Spheres to move")]
    public Transform leftHandSphere;   // LeftHandModel
    public Transform rightHandSphere;  // RightHandModel

    [Header("Laser Color Settings")]
    [Tooltip("Normal (no hover) laser color. Default = calm teal.")]
    public Color normalLaserColor  = new Color(0.30f, 0.85f, 0.94f, 1f); // teal
    [Tooltip("Hover (valid hit) laser color.")]
    public Color hoverLaserColor   = Color.white;

    [Header("Options")]
    [Tooltip("Hide the sphere mesh renderer (show only laser ray from hands)")]
    public bool hideSpheres = true;

    private Transform leftWristBone;
    private Transform rightWristBone;

    void Start()
    {
        // Auto-find spheres by common name if not assigned in Inspector
        if (leftHandSphere == null)
            leftHandSphere  = FindTransformByName("LeftHandModel")
                           ?? FindTransformByName("Left Hand Model")
                           ?? FindTransformByName("LeftController");

        if (rightHandSphere == null)
            rightHandSphere = FindTransformByName("RightHandModel")
                           ?? FindTransformByName("Right Hand Model")
                           ?? FindTransformByName("RightController");

        // Auto-find Animator if not assigned
        if (bodyAnimator == null)
            bodyAnimator = GetComponentInChildren<Animator>();

        if (bodyAnimator != null && bodyAnimator.avatar != null && bodyAnimator.avatar.isHuman)
        {
            leftWristBone  = bodyAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightWristBone = bodyAnimator.GetBoneTransform(HumanBodyBones.RightHand);

            if (leftWristBone  == null) Debug.LogWarning("[HandSphereSync] Left wrist bone not found.");
            if (rightWristBone == null) Debug.LogWarning("[HandSphereSync] Right wrist bone not found.");
        }
        else
        {
            Debug.LogWarning("[HandSphereSync] Humanoid Animator not found — sphere sync disabled.");
        }

            // ── Always deactivate LeftHandModel / RightHandModel ─────────────────
        // These are the grey sphere controller visuals. We want ONLY the teal
        // laser ray to be visible — no sphere, no controller model, nothing.
        // SetActive(false) is used instead of renderer.enabled = false because
        // the sphere objects may have child renderers that renderer.enabled misses.
        // (LateUpdate skips inactive objects automatically.)
        SetSphereInactive(leftHandSphere);
        SetSphereInactive(rightHandSphere);

        // Change laser colors on all XRInteractorLineVisual in the scene
        SetupLaserColors();
    }

    void LateUpdate()
    {
        // Keep the two sphere transforms we already found hidden — that's all.
        // (The earlier aggressive scene-wide sweep was removed because it
        // was matching transforms used by the XR laser line, which could
        // appear to throw off the laser direction.)
        EnforceSphereHidden(leftHandSphere);
        EnforceSphereHidden(rightHandSphere);
    }

    /// <summary>
    /// If something re-activated the sphere model since we deactivated it in
    /// Start, immediately turn it off again. Cheap — one boolean compare per
    /// hand per frame when nothing is wrong, one SetActive call when it is.
    /// </summary>
    void EnforceSphereHidden(Transform sphere)
    {
        if (sphere == null) return;
        if (sphere.gameObject.activeSelf) sphere.gameObject.SetActive(false);
    }

    // (SweepControllerSpheres / SweepHierarchy removed — too aggressive,
    // was occasionally hiding renderers the XR laser depends on.)

    /// <summary>
    /// Completely deactivates the sphere GameObject so it is invisible and
    /// produces no rendering cost. Safe even if called on null.
    /// Disabling via SetActive is more reliable than renderer.enabled = false
    /// because it catches ALL child renderers with a single call.
    /// </summary>
    void SetSphereInactive(Transform sphere)
    {
        if (sphere == null) return;
        sphere.gameObject.SetActive(false);
        Debug.Log($"[HandSphereSync] Deactivated controller model: {sphere.name}");
    }

    void SetupLaserColors()
    {
        // Build gradient helpers
        var tealGrad  = MakeGradient(normalLaserColor);
        var whiteGrad = MakeGradient(hoverLaserColor);

        // Find all ray line visuals in the scene (works even if inside prefabs)
        var lineVisuals = FindObjectsOfType<XRInteractorLineVisual>(true);

        foreach (var lv in lineVisuals)
        {
            lv.invalidColorGradient = tealGrad;   // normal laser → teal
            lv.validColorGradient   = whiteGrad;  // hover → white
            Debug.Log($"[HandSphereSync] Set laser color on: {lv.gameObject.name}");
        }

        if (lineVisuals.Length == 0)
            Debug.LogWarning("[HandSphereSync] No XRInteractorLineVisual found in scene.");
    }

    static Transform FindTransformByName(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    static Gradient MakeGradient(Color c)
    {
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        return grad;
    }
}
