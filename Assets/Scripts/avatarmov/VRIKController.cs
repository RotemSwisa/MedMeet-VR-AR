using Normal.Realtime;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Full VR body IK — hands, elbows, head bone override, spine lean.
/// Requires: Humanoid Animator on same GameObject + IK Pass ON.
///
/// KEY BEHAVIOUR:
///   • LOCAL avatar  — reads Camera.main / XR devices as usual.
///   • REMOTE avatar — reads ONLY from avatarInputs transforms (which Normcore
///     moves to the network-synced positions). Never touches Camera.main or XR
///     devices, so remote players always show THEIR OWN head/hand data.
/// </summary>
[RequireComponent(typeof(Animator))]
public class VRIKController : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Root prefab that holds RealtimeAvatarInputs (DoctorPlayer/Femaledoctor root).")]
    public RealtimeAvatarInputs avatarInputs;

    [Header("Hand IK Weights")]
    [Range(0f, 1f)] public float leftHandWeight  = 1f;
    [Range(0f, 1f)] public float rightHandWeight = 1f;
    [Tooltip("Tick this when one hand drops to the avatar's side instead of following the controller. " +
             "Forces both leftHandWeight and rightHandWeight to 1.0 every frame, overriding any value " +
             "set above. Safe to leave on permanently — it just guarantees the IK is always active.")]
    public bool forceWeightsToOne = false;

    [Header("Hand Swap — if controllers appear mirrored, enable this")]
    [Tooltip("Enable if the avatar's left hand tracks the physical right controller and vice versa.")]
    public bool swapHandControllers = false;

    [Header("Elbow Hints — כיפוף מרפקים")]
    [Range(0f, 1f)] public float elbowHintWeight = 1f;
    public float elbowBackOffset = 0.55f;
    public float elbowDownOffset = 0.15f;

    [Header("Head Tracking — ראש עוקב אחרי המצלמה")]
    [Tooltip("When true, the head bone is FORCED to stay in its idle pose " +
             "(no camera tracking, no animation bobbing). Overrides every other " +
             "head setting. Use this for demos where you want a perfectly still head.")]
    public bool lockHeadStatic = true;
    [Tooltip("Enable to override the animation head bobbing with real VR camera rotation. " +
             "Ignored when lockHeadStatic is true.")]
    public bool enableHeadTracking = true;
    [Range(0f, 1f)] public float headTrackingWeight = 1f;
    [Tooltip("Smoothing — 0=instant, 25=smooth. For VR use 20-30.")]
    public float headSmoothSpeed = 22f;
    [Tooltip("Offset (metres) to push the head bone forward. Keep at 0 — use localCameraNearClip instead to prevent head mesh visibility.")]
    public float headForwardOffset = 0f;

    [Header("Local Player — Prevent Seeing Own Head")]
    [Tooltip("Near clip plane (metres) set on the local player's camera so nearby head-mesh polygons are clipped and never rendered. 0.15 m works well for Quest. Set to 0 to disable.")]
    public float localCameraNearClip = 0.15f;

    [Header("Spine Lean (SetLookAt body weight)")]
    [Range(0f, 0.4f)] public float spineWeight = 0.12f;

    [Header("Left Hand Fine-Tune")]
    public Vector3 leftHandPositionOffset  = Vector3.zero;
    public Vector3 leftHandRotationOffset  = Vector3.zero;

    [Header("Right Hand Fine-Tune")]
    public Vector3 rightHandPositionOffset = Vector3.zero;
    public Vector3 rightHandRotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // ── Inspector context-menu helpers (right-click the script header) ──────
    /// <summary>Zero every hand position/rotation offset. Use when the
    /// hand appears way off and you don't know what value was set.</summary>
    [ContextMenu("Reset Hand Offsets to Zero")]
    private void ResetHandOffsets()
    {
        leftHandPositionOffset  = Vector3.zero;
        leftHandRotationOffset  = Vector3.zero;
        rightHandPositionOffset = Vector3.zero;
        rightHandRotationOffset = Vector3.zero;
        Debug.Log("[VRIKController] Hand offsets reset to zero.");
    }

    /// <summary>Force both hand IK weights back to 1 (full strength).</summary>
    [ContextMenu("Reset Hand Weights to 1")]
    private void ResetHandWeights()
    {
        leftHandWeight  = 1f;
        rightHandWeight = 1f;
        Debug.Log("[VRIKController] Hand weights forced to 1.");
    }

    /// <summary>Flip the controller mapping if the hands look mirrored.</summary>
    [ContextMenu("Toggle Swap Hand Controllers")]
    private void ToggleSwapHands()
    {
        swapHandControllers = !swapHandControllers;
        Debug.Log($"[VRIKController] swapHandControllers → {swapHandControllers}");
    }

    /// <summary>Mirror the right-hand offsets to the left hand. Use when
    /// the right hand sits correctly but the left is misaligned — most
    /// humanoid rigs are symmetric so mirroring usually fixes the left.</summary>
    [ContextMenu("Mirror Right Hand Offsets → Left")]
    private void MirrorRightToLeft()
    {
        // Position mirror: flip X only (Y up/down + Z forward stay the same)
        leftHandPositionOffset = new Vector3(
            -rightHandPositionOffset.x,
             rightHandPositionOffset.y,
             rightHandPositionOffset.z);

        // Rotation mirror for a symmetric humanoid: flip Y and Z signs
        leftHandRotationOffset = new Vector3(
             rightHandRotationOffset.x,
            -rightHandRotationOffset.y,
            -rightHandRotationOffset.z);

        Debug.Log($"[VRIKController] {name}: mirrored right→left.\n" +
                  $"  leftHandPositionOffset = {leftHandPositionOffset}\n" +
                  $"  leftHandRotationOffset = {leftHandRotationOffset}");
    }

    /// <summary>Same direction, useful if only the left was tuned.</summary>
    [ContextMenu("Mirror Left Hand Offsets → Right")]
    private void MirrorLeftToRight()
    {
        rightHandPositionOffset = new Vector3(
            -leftHandPositionOffset.x,
             leftHandPositionOffset.y,
             leftHandPositionOffset.z);
        rightHandRotationOffset = new Vector3(
             leftHandRotationOffset.x,
            -leftHandRotationOffset.y,
            -leftHandRotationOffset.z);
        Debug.Log($"[VRIKController] {name}: mirrored left→right.");
    }

    /// <summary>One-click "fix everything" — zero offsets, weight = 1,
    /// elbow hint reasonable, head locked. Use when a prefab is mis-tuned
    /// and you want a clean baseline.</summary>
    [ContextMenu("⚡ Normalise All IK Settings (full reset)")]
    private void NormaliseAll()
    {
        ResetHandOffsets();
        ResetHandWeights();
        elbowHintWeight = 1f;
        elbowBackOffset = 0.55f;
        elbowDownOffset = 0.15f;
        lockHeadStatic  = true;
        Debug.Log("[VRIKController] ⚡ All IK settings normalised to safe defaults.");
    }

    /// <summary>Print every value the script is currently using.</summary>
    [ContextMenu("Debug: Print Current State")]
    private void DebugPrintState()
    {
        Debug.Log(
            $"[VRIKController] state for {name}\n" +
            $"  isLocal              = {_isLocalAvatar}\n" +
            $"  leftHandWeight       = {leftHandWeight}\n" +
            $"  rightHandWeight      = {rightHandWeight}\n" +
            $"  swapHandControllers  = {swapHandControllers}\n" +
            $"  leftHandPosOffset    = {leftHandPositionOffset}\n" +
            $"  leftHandRotOffset    = {leftHandRotationOffset}\n" +
            $"  rightHandPosOffset   = {rightHandPositionOffset}\n" +
            $"  rightHandRotOffset   = {rightHandRotationOffset}\n" +
            $"  elbowHintWeight      = {elbowHintWeight}\n" +
            $"  lockHeadStatic       = {lockHeadStatic}\n" +
            $"  avatarInputs.left    = {(avatarInputs?.leftHand  != null ? avatarInputs.leftHand.name  : "NULL")}\n" +
            $"  avatarInputs.right   = {(avatarInputs?.rightHand != null ? avatarInputs.rightHand.name : "NULL")}\n" +
            $"  avatarInputs.head    = {(avatarInputs?.head      != null ? avatarInputs.head.name      : "NULL")}",
            this);
    }

    // ── internals ──────────────────────────────────────────────────────────
    private Animator     animator;
    private bool         ready        = false;
    private bool         _isLocalAvatar;
    private InputDevice  leftXRDevice;
    private InputDevice  rightXRDevice;

    // Bone transforms (cached at Start)
    private Transform headBone;
    private Transform leftUpperArmBone;
    private Transform rightUpperArmBone;

    // ── lifecycle ──────────────────────────────────────────────────────────
    void Start()
    {
        animator = GetComponent<Animator>();
        if (!animator) { Debug.LogError("[VRIKController] No Animator!"); return; }
        if (animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogError("[VRIKController] Animator Avatar is NOT Humanoid! " +
                           "Model Importer → Rig → Animation Type → Humanoid → Apply.");
            return;
        }

        // ── Determine if this instance belongs to the local player ──────────
        // RealtimeView.isOwnedLocallyInHierarchy == true  → our own avatar
        //                                         == false → remote player's avatar
        // If there is no RealtimeView (editor / no Normcore) treat as local.
        // Wrap in try/catch because Normcore throws "view doesn't have a model
        // yet" when the room isn't connected (replay mode, editor without
        // internet, scene reload), and we don't want that to crash the avatar.
        var rv = GetComponentInParent<RealtimeView>();
        try
        {
            _isLocalAvatar = (rv == null) || rv.isOwnedLocallyInHierarchy;
        }
        catch (System.Exception)
        {
            _isLocalAvatar = true;   // safe default — drives camera setup
        }

        // ── Auto-find avatarInputs (search parent chain first, then children) ──
        if (!avatarInputs) avatarInputs = GetComponentInParent<RealtimeAvatarInputs>();
        if (!avatarInputs) avatarInputs = GetComponentInChildren<RealtimeAvatarInputs>();
        // Do NOT fall back to FindObjectOfType — with multiple players that
        // would find the WRONG avatar's inputs.

        // Cache bones
        headBone          = animator.GetBoneTransform(HumanBodyBones.Head);
        leftUpperArmBone  = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArmBone = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

        // Note: NO automatic clamp or weight override here — those would mutate
        // legitimate Inspector values that worked before. Use the right-click
        // context-menu actions on the script header to reset values manually.

        ready = true;

        // ── Local player: push camera near clip to hide own head mesh ─────────
        // Each client independently renders all avatar meshes. For the LOCAL player,
        // any polygon of their own head mesh that falls within localCameraNearClip metres
        // of the camera is clipped by the GPU before rasterisation — invisible, clean.
        // Remote players' cameras still render the head mesh normally (they have their
        // own cameras, unaffected by this setting on the local machine).
        if (_isLocalAvatar && localCameraNearClip > 0f)
        {
            // Set near clip on the active XR camera (Camera.main in VR mode).
            // VRARSwitcher will set it again on the OVR camera when switching to AR.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.nearClipPlane = localCameraNearClip;
                if (showDebugInfo)
                    Debug.Log($"[VRIKController] Camera.main.nearClipPlane → {localCameraNearClip} m (head-mesh clip)");
            }
        }

        if (showDebugInfo)
            Debug.Log($"[VRIKController] ✅ Ready | isLocal={_isLocalAvatar} | " +
                      $"Head: {headBone?.name ?? "NOT FOUND"} | " +
                      $"LeftArm: {leftUpperArmBone?.name ?? "NOT FOUND"}");
    }

    void Update()
    {
        // Keep XR device references alive (no-op on remote avatars — results unused)
        if (!leftXRDevice.isValid)  leftXRDevice  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightXRDevice.isValid) rightXRDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    // ── IK pass ────────────────────────────────────────────────────────────
    void OnAnimatorIK(int layerIndex)
    {
        if (!ready) return;

        // Optional safety: if the user ticked the override box, guarantee the
        // IK is fully active regardless of whatever value sits in the weight
        // sliders or got copied in from a stale prefab.
        if (forceWeightsToOne)
        {
            leftHandWeight  = 1f;
            rightHandWeight = 1f;
        }

        // Resolve which transform feeds which IK goal (swap flag handles mirror fix)
        Transform srcLeft  = swapHandControllers ? avatarInputs?.rightHand : avatarInputs?.leftHand;
        Transform srcRight = swapHandControllers ? avatarInputs?.leftHand  : avatarInputs?.rightHand;
        XRNode    nodeLeft  = swapHandControllers ? XRNode.RightHand : XRNode.LeftHand;
        XRNode    nodeRight = swapHandControllers ? XRNode.LeftHand  : XRNode.RightHand;

        var leftT  = GetTracking(nodeLeft,  srcLeft);
        var rightT = GetTracking(nodeRight, srcRight);

        // ── Spine lean ────────────────────────────────────────────────────
        Transform cam = GetCamera();
        if (cam != null && spineWeight > 0f)
        {
            animator.SetLookAtPosition(cam.position + cam.forward * 3f);
            animator.SetLookAtWeight(1f, spineWeight, 0f, 0f, 0.5f);
        }

        // ── Hand IK ───────────────────────────────────────────────────────
        ApplyHandIK(AvatarIKGoal.LeftHand,  leftT,  leftHandWeight,
                    leftHandPositionOffset,  leftHandRotationOffset);
        ApplyHandIK(AvatarIKGoal.RightHand, rightT, rightHandWeight,
                    rightHandPositionOffset, rightHandRotationOffset);

        // ── Elbow hints ───────────────────────────────────────────────────
        if (elbowHintWeight > 0f)
        {
            ApplyElbowHint(AvatarIKHint.LeftElbow,  leftT,  leftUpperArmBone);
            ApplyElbowHint(AvatarIKHint.RightElbow, rightT, rightUpperArmBone);
        }
    }

    // Cached head pose so lockHeadStatic can keep the head fixed even if the
    // Animator's BlendTree tries to add idle bobbing.
    private Quaternion _frozenHeadLocalRotation;
    private bool       _frozenHeadCaptured;

    // ── Head bone override ─────────────────────────────────────────────────
    // Runs AFTER Animator finishes. Replaces animation head-bob with real
    // camera orientation (local) or network-synced head pose (remote).
    void LateUpdate()
    {
        if (!ready || headBone == null) return;

        // ── HEAD LOCK ─────────────────────────────────────────────────────
        // When locked, we capture the rest-pose rotation once and then force
        // the head bone back to it every frame AFTER the Animator finishes.
        // Result: zero head bobbing, zero camera tracking — completely still.
        if (lockHeadStatic)
        {
            if (!_frozenHeadCaptured)
            {
                _frozenHeadLocalRotation = headBone.localRotation;
                _frozenHeadCaptured = true;
            }
            headBone.localRotation = _frozenHeadLocalRotation;
            return;
        }

        if (!enableHeadTracking) return;

        // In the Unity editor the XR camera sits in a fixed rig orientation that
        // doesn't represent a real head pose.  Applying tracking rotates the avatar
        // head to face the scene-camera angle and makes the avatar look wrong.
        // On the actual headset the camera IS the physical head, so tracking is correct.
        // Remote avatars always use their own network-synced head data (fine on both).
        if (Application.isEditor && _isLocalAvatar) return;

        Transform cam = GetCamera();
        if (cam == null) return;   // remote avatar with no synced head data → leave as-is

        Transform parent = headBone.parent;
        if (parent != null)
        {
            Quaternion cameraInParentSpace = Quaternion.Inverse(parent.rotation) * cam.rotation;
            headBone.localRotation = Quaternion.Slerp(
                headBone.localRotation, cameraInParentSpace,
                headSmoothSpeed * Time.deltaTime * headTrackingWeight);
        }
        else
        {
            headBone.rotation = Quaternion.Slerp(
                headBone.rotation, cam.rotation,
                headSmoothSpeed * Time.deltaTime * headTrackingWeight);
        }

        // Push head bone slightly forward so the mesh doesn't clip through
        // the camera on other players' screens during walking animation.
        if (headForwardOffset > 0f)
            headBone.position += headBone.forward * headForwardOffset;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    void ApplyHandIK(AvatarIKGoal goal,
                     (Vector3 pos, Quaternion rot, bool valid) t,
                     float weight, Vector3 posOff, Vector3 rotOff)
    {
        if (!t.valid || weight <= 0f)
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);
            return;
        }
        Quaternion finalRot = t.rot * Quaternion.Euler(rotOff);
        Vector3    finalPos = t.pos + finalRot * posOff;

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKRotationWeight(goal, weight);
        animator.SetIKPosition(goal, finalPos);
        animator.SetIKRotation(goal, finalRot);
    }

    void ApplyElbowHint(AvatarIKHint hint,
                        (Vector3 pos, Quaternion rot, bool valid) handT,
                        Transform upperArm)
    {
        if (!handT.valid || elbowHintWeight <= 0f) return;

        Vector3 shoulder = upperArm != null
            ? upperArm.position
            : transform.position + Vector3.up * 1.4f;

        Vector3 armDir   = (handT.pos - shoulder).normalized;
        Vector3 avatarUp = transform.up;
        Vector3 outward  = Vector3.Cross(armDir, avatarUp);

        Vector3 mid     = (shoulder + handT.pos) * 0.5f;
        Vector3 back    = -transform.forward;
        Vector3 hintPos = mid
                        + back    * elbowBackOffset
                        + outward * (elbowBackOffset * 0.5f)
                        + Vector3.down * elbowDownOffset;

        animator.SetIKHintPositionWeight(hint, elbowHintWeight);
        animator.SetIKHintPosition(hint, hintPos);
    }

    /// <summary>
    /// Returns tracking data for one hand.
    /// Priority: avatarInputs transform → XR device (LOCAL only) → invalid.
    /// Remote avatars never fall back to XR devices so they can't "steal"
    /// the local player's controller data.
    /// </summary>
    (Vector3, Quaternion, bool) GetTracking(XRNode node, Transform avatarHand)
    {
        // Network-synced transform (works for both local and remote)
        if (avatarHand != null)
            return (avatarHand.position, avatarHand.rotation, true);

        // Raw XR device — only meaningful on the local player's machine
        if (_isLocalAvatar)
        {
            InputDevice dev = node == XRNode.LeftHand ? leftXRDevice : rightXRDevice;
            if (dev.isValid &&
                dev.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p) &&
                dev.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion r))
                return (p, r, true);
        }

        return (Vector3.zero, Quaternion.identity, false);
    }

    /// <summary>
    /// Returns the camera/head transform to use for head tracking.
    /// • avatarInputs.head — set for local avatar (real camera transform) OR
    ///   driven by Normcore for remote avatars (synced head pose).
    /// • Camera.main — fallback ONLY for local avatar (editor / no avatarInputs).
    /// </summary>
    Transform GetCamera()
    {
        if (avatarInputs?.head != null) return avatarInputs.head;
        return _isLocalAvatar ? Camera.main?.transform : null;
    }
}
