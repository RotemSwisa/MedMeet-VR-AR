using UnityEngine;

/// <summary>
/// Per-frame helper added to each replay avatar by ReplayManager. Two jobs:
///
///   1. HEAD LOCK — every LateUpdate, force the head bone back to its
///      initial rest-pose rotation. This runs AFTER ReplayAvatar.Update has
///      written the recorded head transform, so it overrides any wobble.
///      Result: head stays still in playback, identical to live behaviour
///      (where VRIKController.lockHeadStatic does the same job).
///
///   2. WALKING ANIMATION — every Update, measure the avatar root's horizontal
///      speed (delta over Time.deltaTime), smooth it, and push it into the
///      Animator's "MoveSpeed" + "MotionSpeed" parameters. The walk BlendTree
///      then plays exactly when the recorded avatar moves and freezes when
///      it stops — same as live AvatarMovementSync does.
///
/// Why this exists instead of trusting the live components:
///   ReplayAvatar.Update is the one that writes the position/rotation values
///   each frame. VRIKController's lockHead may capture the wrong baseline
///   while ReplayAvatar is still wiring up references, and AvatarMovementSync
///   sometimes initialises before ReplayAvatar.Initialize finds its bones.
///   This helper sidesteps both races by running after they've finished.
/// </summary>
public class ReplayAvatarHelper : MonoBehaviour
{
    [Header("Walking animation")]
    [Tooltip("Below this distance (metres) moved per frame the avatar stays in idle.")]
    public float moveDetectionPerFrame = 0.001f;

    [Tooltip("MoveSpeed value pushed to the animator while the avatar is moving. " +
             "Set to your BlendTree's Walk threshold (typical: 1.5-2.0). Higher = faster gait.")]
    public float walkBlendValue = 1.8f;

    [Tooltip("MotionSpeed value while moving — controls animation playback rate.")]
    public float walkPlaybackRate = 1.0f;

    private Animator  _animator;
    private Transform _headBone;
    private Quaternion _frozenHeadLocalRotation;
    private bool       _frozenHeadCaptured;

    private Vector3 _previousRootPosition;
    private bool    _previousCaptured;

    private static readonly int MoveSpeedHash   = Animator.StringToHash("MoveSpeed");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int IsGroundedHash  = Animator.StringToHash("IsGrounded");
    private static readonly int FreeFallHash    = Animator.StringToHash("FreeFall");

    private bool _hasMoveSpeed;
    private bool _hasMotionSpeed;

    void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>(true);

        if (_animator != null && _animator.avatar != null && _animator.avatar.isHuman)
        {
            _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
        }

        if (_animator != null)
        {
            foreach (AnimatorControllerParameter p in _animator.parameters)
            {
                if (p.nameHash == MoveSpeedHash)   _hasMoveSpeed   = true;
                if (p.nameHash == MotionSpeedHash) _hasMotionSpeed = true;
                if (p.nameHash == IsGroundedHash)  _animator.SetBool(IsGroundedHash, true);
                if (p.nameHash == FreeFallHash)    _animator.SetBool(FreeFallHash, false);
            }
        }

        // _previousRootPosition gets seeded on the first Update tick (not here)
        // so the spawn-to-first-replay-frame jump doesn't fake a walk burst.
    }

    void Update()
    {
        if (_animator == null) return;

        // ── Walking animation (binary: moving or idle) ───────────────────
        // Simpler approach than measuring exact speed: detect ANY position
        // change between frames and push the walk-blend value immediately.
        // This avoids the BlendTree never reaching its Walk threshold due
        // to interpolation smoothing the per-frame delta below the cutoff.
        //
        // Capture the FIRST position the very next frame after the helper
        // mounts so the spawn-position-to-first-recorded-position jump does
        // NOT register as movement (which was causing the "running at start"
        // and equally the "running at end as recording wraps up" artefacts).
        if (!_previousCaptured)
        {
            _previousRootPosition = transform.position;
            _previousCaptured = true;
            return;
        }

        Vector3 delta = transform.position - _previousRootPosition;
        delta.y = 0f;
        bool isMoving = delta.sqrMagnitude > (moveDetectionPerFrame * moveDetectionPerFrame);

        _previousRootPosition = transform.position;

        if (_hasMoveSpeed)
            _animator.SetFloat(MoveSpeedHash,   isMoving ? walkBlendValue : 0f);
        if (_hasMotionSpeed)
            _animator.SetFloat(MotionSpeedHash, isMoving ? walkPlaybackRate : 1f);
    }

    void LateUpdate()
    {
        // ── Head lock ────────────────────────────────────────────────────
        // Runs after every other component (including the Animator and
        // ReplayAvatar.Update) has finished writing transforms this frame.
        // Capture the initial pose once, then snap the head back to it every
        // frame so it doesn't bob along with the body's animation.
        if (_headBone == null) return;
        if (!_frozenHeadCaptured)
        {
            _frozenHeadLocalRotation = _headBone.localRotation;
            _frozenHeadCaptured = true;
            return;
        }
        _headBone.localRotation = _frozenHeadLocalRotation;
    }
}
