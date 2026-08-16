using UnityEngine;

/// <summary>
/// Drives walking animation by setting MoveSpeed (blend tree position)
/// AND MotionSpeed (playback rate) so leg animation always matches actual movement pace.
///
/// Result: when barely moving, legs barely move. When walking normally, animation
/// plays at exactly the right tempo. No more "hopping one leg" effect.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AvatarMovementSync : MonoBehaviour
{
    [Header("Tracked Transform")]
    [Tooltip("Leave empty to track this GameObject's own position (VRBodySync moves this root).")]
    public Transform trackedTransform;

    [Header("Speed Settings")]
    [Tooltip("Below this speed the avatar stays in Idle (avoids micro-jitter)")]
    public float walkThreshold = 0.1f;

    [Tooltip("At this speed the animation plays at exactly 1x speed (reference walk pace m/s)")]
    public float referenceWalkSpeed = 1.8f;

    [Tooltip("Highest speed mapped into the blend tree (m/s = blend tree max threshold)")]
    public float maxSpeed = 5f;

    [Tooltip("Smoothing of speed display — prevents animation jitter")]
    public float smoothing = 7f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private Animator  animator;
    private Vector3   previousPosition;
    private float     smoothedSpeed;

    // Cached hashes (fast lookup)
    private static readonly int MoveSpeedHash  = Animator.StringToHash("MoveSpeed");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int IsGroundedHash  = Animator.StringToHash("IsGrounded");
    private static readonly int FreeFallHash    = Animator.StringToHash("FreeFall");

    private bool hasMoveSpeed;
    private bool hasMotionSpeed;

    void Start()
    {
        animator = GetComponent<Animator>();

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == MoveSpeedHash)   hasMoveSpeed   = true;
            if (p.nameHash == MotionSpeedHash)  hasMotionSpeed = true;
            if (p.nameHash == IsGroundedHash)   animator.SetBool(IsGroundedHash, true);
            if (p.nameHash == FreeFallHash)     animator.SetBool(FreeFallHash, false);
        }

        previousPosition = GetTarget().position;

        if (showDebugInfo)
            Debug.Log($"[AvatarMovementSync] MoveSpeed: {hasMoveSpeed}, MotionSpeed: {hasMotionSpeed}");
    }

    void Update()
    {
        Transform target = GetTarget();
        Vector3   delta  = target.position - previousPosition;
        delta.y = 0f;

        float rawSpeed = delta.magnitude / Time.deltaTime;
        if (rawSpeed < walkThreshold) rawSpeed = 0f;
        rawSpeed = Mathf.Min(rawSpeed, maxSpeed);

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, smoothing * Time.deltaTime);
        previousPosition = target.position;

        // MoveSpeed → which clip in BlendTree (Idle/Walk/Run position)
        if (hasMoveSpeed)
            animator.SetFloat(MoveSpeedHash, smoothedSpeed);

        // MotionSpeed → playback RATE of the active animation clip.
        // At referenceWalkSpeed the animation plays at 1x, matching real movement.
        // Below that, animation slows down proportionally → legs match feet.
        if (hasMotionSpeed)
        {
            float motionSpeed = smoothedSpeed > 0.01f
                ? Mathf.Clamp(smoothedSpeed / referenceWalkSpeed, 0.1f, 3f)
                : 1f; // idle: play at normal rate (avoids frozen idle)
            animator.SetFloat(MotionSpeedHash, motionSpeed);
        }

        if (showDebugInfo && smoothedSpeed > 0.05f)
            Debug.Log($"[AvatarMovementSync] speed={smoothedSpeed:F2} motionSpeed=" +
                      $"{(smoothedSpeed/referenceWalkSpeed):F2}");
    }

    Transform GetTarget() => trackedTransform != null ? trackedTransform : transform;
}
