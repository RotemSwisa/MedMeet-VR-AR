using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LoadingCanvas (Screen 2) controller.
///
/// Visual behaviour, mirroring loading.jsx:
///   • Earth at canvas centre, bobbing vertically on a soft sine.
///   • Atmospheric glow halo pulsing behind the earth.
///   • Airplane orbits the earth on a continuous circular path, rotating
///     to keep its nose pointing forward.
///   • Progress bar fills from 0 → 100% over `totalSeconds`.
///   • Status message cycles through four lines while progress advances.
///   • When 100% is reached, the host instance advances the synced screen
///     state to Dashboard; everyone follows.
/// </summary>
public class LoadingCanvasController : MonoBehaviour
{
    [Header("Earth / Airplane")]
    public RectTransform earthRoot;       // bobs vertically on a sine
    public RectTransform glowRoot;        // pulses scale + opacity
    public Image         glowImage;
    public RectTransform orbitRoot;       // rotates the plane around earth
    public RectTransform airplaneRoot;    // positioned on orbit ring, kept tangent

    [Header("Timing")]
    [Tooltip("Total duration of the loading sequence (host only — others follow sync).")]
    public float totalSeconds   = 3.4f;
    public float earthBobMagnitude = 14f;
    public float earthBobSpeed     = 1f;
    public float planeOrbitSpeed   = 51.4f;  // degrees per second (≈ 7s per loop)
    public float planeOrbitRadius  = 300f;

    [Header("Progress bar + label")]
    public RectTransform progressFill;    // pivot 0,0.5 — scales X from 0 → 1
    public TextMeshProUGUI progressLabel; // "28%"
    public TextMeshProUGUI messageLabel;  // status message
    public TextMeshProUGUI titleLabel;    // "Your results are arriving…"

    [Header("Step dots — optional")]
    public StepDotsUI stepDots;

    private static readonly string[] LoadMessages =
    {
        "Mapping the journeys you replaced…",
        "Estimating fuel and CO₂ avoided…",
        "Tallying paper and single-use savings…",
        "Composing your impact report…",
    };

    private float _t0;
    private bool  _advancing;

    void OnEnable()
    {
        _t0 = Time.time;
        _advancing = false;
        if (stepDots != null) stepDots.SetStep(1);
        if (titleLabel != null) titleLabel.text = "Your results are arriving…";
        StartCoroutine(RunSequence());
    }

    void Update()
    {
        // Bob earth
        if (earthRoot != null)
        {
            var p = earthRoot.anchoredPosition;
            p.y = Mathf.Sin((Time.time - _t0) * earthBobSpeed * Mathf.PI * 0.66f) * earthBobMagnitude;
            earthRoot.anchoredPosition = p;
        }

        // Pulse glow
        if (glowRoot != null)
        {
            float pulse = (Mathf.Sin((Time.time - _t0) * 1.6f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, 1.06f, pulse);
            glowRoot.localScale = new Vector3(scale, scale, 1f);
            if (glowImage != null)
            {
                var c = glowImage.color;
                c.a = Mathf.Lerp(0.45f, 0.85f, pulse);
                glowImage.color = c;
            }
        }

        // Rotate orbit (plane is a child positioned at orbitRadius)
        if (orbitRoot != null)
            orbitRoot.localEulerAngles = new Vector3(0, 0, -(Time.time - _t0) * planeOrbitSpeed);
    }

    private IEnumerator RunSequence()
    {
        // Place plane on orbit ring (top of circle) with nose tangent to circle
        if (airplaneRoot != null)
        {
            airplaneRoot.anchoredPosition = new Vector2(0f, planeOrbitRadius);
            airplaneRoot.localEulerAngles = new Vector3(0f, 0f, 45f);  // matches loading.jsx
        }

        float elapsed = 0f;
        int   lastMsg = -1;
        while (elapsed < totalSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalSeconds);

            // Progress fill (uses scaleX on a child anchored left-centre)
            if (progressFill != null)
                progressFill.localScale = new Vector3(t, 1f, 1f);

            if (progressLabel != null)
                progressLabel.text = Mathf.RoundToInt(t * 100f) + "%";

            int msgIdx = Mathf.Clamp(Mathf.FloorToInt(t * LoadMessages.Length),
                                     0, LoadMessages.Length - 1);
            if (msgIdx != lastMsg)
            {
                lastMsg = msgIdx;
                if (messageLabel != null) messageLabel.text = LoadMessages[msgIdx];
            }
            yield return null;
        }

        // Finalise visuals
        if (progressFill  != null) progressFill.localScale = new Vector3(1f, 1f, 1f);
        if (progressLabel != null) progressLabel.text       = "100%";
        if (messageLabel  != null) messageLabel.text        = "Report ready.";

        // Brief settle then advance everybody
        yield return new WaitForSeconds(0.35f);
        if (_advancing) yield break;
        _advancing = true;

        if (DashboardSync.Instance != null && DashboardSync.Instance.IsModelReady)
            DashboardSync.Instance.SetDashboard();
        else
        {
            var setup = FindFirstObjectByType<DashboardSceneSetup>();
            if (setup != null) setup.ShowDashboard();
        }
    }
}
