using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated "shooting star" backdrop for the AI canvas. Inspired by Groq's
/// landing-page hero: small dots drift downwards on a dark gradient with a
/// faint glowing trail, creating a sense of motion without distracting from
/// the chat.
///
/// Implementation:
///   • Owns a pool of Image children (small white dots) parented to a
///     RectTransform background panel.
///   • Each frame, every star drifts down + slightly to one side and fades
///     in / out. When a star reaches the bottom (or its life ends), it gets
///     re-spawned at the top with a fresh randomised speed + size + life.
///   • Pure UI Image components — no particles, no shaders. Safe for VR.
///
/// Attach this to a UI panel (with RectTransform) that fills the chat
/// background. The panel should sit BEHIND every other chat child and have
/// raycastTarget=false on its Image so it never steals clicks.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StarfieldBackground : MonoBehaviour
{
    [Header("Pool")]
    [Range(20, 200)] public int starCount = 80;

    [Header("Movement")]
    public Vector2 speedRangeY = new Vector2(50f, 180f);   // px/sec downward
    public Vector2 driftRangeX = new Vector2(-12f, 12f);   // px/sec sideways
    public Vector2 sizeRange   = new Vector2(2f, 6f);       // diameter, px
    public Vector2 lifeRange   = new Vector2(2.5f, 6.0f);  // seconds before respawn

    [Header("Appearance")]
    [Tooltip("Background tint behind the stars. Set alpha to 0 for transparent.")]
    public Color backgroundColor = new Color(0.020f, 0.024f, 0.039f, 1f);
    [Tooltip("Star colour. Alpha drives the peak brightness.")]
    public Color starColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Optional brighter trail colour used by larger stars.")]
    public Color trailColor = new Color(0.45f, 0.78f, 1f, 0.55f);

    [Header("Bounds (auto on Start, override if needed)")]
    public bool autoFitToRect = true;
    public Vector2 fieldSize = new Vector2(1000, 700);

    private RectTransform _rt;
    private Sprite _dotSprite;
    private readonly List<Star> _stars = new List<Star>();

    private struct Star
    {
        public RectTransform rt;
        public CanvasGroup   cg;
        public Vector2       vel;
        public float         life, age;
        public float         size;
        public bool          hasTrail;
        public RectTransform trail;
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        EnsureBackground();
        _dotSprite = MakeCircleSprite(32);
        if (autoFitToRect) fieldSize = _rt.rect.size;
        for (int i = 0; i < starCount; i++) SpawnStar(initialAge: Random.Range(0f, 1f));
    }

    void Update()
    {
        if (autoFitToRect) fieldSize = _rt.rect.size;
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < _stars.Count; i++)
        {
            var s = _stars[i];
            s.age += dt;
            s.rt.anchoredPosition += s.vel * dt;

            // Fade in over first 20% of life, out over last 30%
            float t = s.age / s.life;
            float alpha = 1f;
            if (t < 0.20f)        alpha = t / 0.20f;
            else if (t > 0.70f)   alpha = 1f - (t - 0.70f) / 0.30f;
            s.cg.alpha = Mathf.Clamp01(alpha) * starColor.a;

            // Trail follows the star (slightly above, longer tail)
            if (s.hasTrail && s.trail != null)
            {
                s.trail.anchoredPosition = s.rt.anchoredPosition + new Vector2(0, s.size * 6f);
            }

            // Respawn when off-bounds or finished its life
            Vector2 pos = s.rt.anchoredPosition;
            bool offscreen = pos.y < -fieldSize.y * 0.55f
                          || pos.y >  fieldSize.y * 0.55f
                          || pos.x < -fieldSize.x * 0.55f
                          || pos.x >  fieldSize.x * 0.55f;
            if (s.age >= s.life || offscreen)
            {
                RespawnStar(ref s);
            }
            _stars[i] = s;
        }
    }

    // ── Setup helpers ──────────────────────────────────────────────────────
    private void EnsureBackground()
    {
        var bgImg = GetComponent<Image>();
        if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
        bgImg.color = backgroundColor;
        bgImg.raycastTarget = false;
        bgImg.maskable = true;
    }

    private void SpawnStar(float initialAge)
    {
        var go = new GameObject($"Star_{_stars.Count}", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform) go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float size = Random.Range(sizeRange.x, sizeRange.y);
        rt.sizeDelta = new Vector2(size, size);

        var img = go.GetComponent<Image>();
        img.sprite = _dotSprite;
        img.color = starColor;
        img.raycastTarget = false;
        img.maskable = true;

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var star = new Star
        {
            rt = rt, cg = cg,
            life = Random.Range(lifeRange.x, lifeRange.y),
            age  = Random.Range(0f, lifeRange.y) * initialAge,
            size = size,
            hasTrail = size > sizeRange.x + (sizeRange.y - sizeRange.x) * 0.6f,
        };

        // Brighter / bigger stars get a small trail above them
        if (star.hasTrail)
        {
            var tg = new GameObject("Trail", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            tg.transform.SetParent(transform, false);
            var trt = (RectTransform) tg.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(size * 0.8f, size * 12f);
            var ti = tg.GetComponent<Image>();
            ti.sprite = _dotSprite;
            ti.color  = new Color(trailColor.r, trailColor.g, trailColor.b, trailColor.a * 0.35f);
            ti.raycastTarget = false;
            tg.GetComponent<CanvasGroup>().blocksRaycasts = false;
            star.trail = trt;
        }

        RespawnStar(ref star);
        _stars.Add(star);
    }

    private void RespawnStar(ref Star s)
    {
        s.age = 0f;
        s.life = Random.Range(lifeRange.x, lifeRange.y);
        s.size = Random.Range(sizeRange.x, sizeRange.y);
        s.rt.sizeDelta = new Vector2(s.size, s.size);
        s.rt.anchoredPosition = new Vector2(
            Random.Range(-fieldSize.x * 0.50f, fieldSize.x * 0.50f),
            Random.Range(fieldSize.y * 0.30f, fieldSize.y * 0.55f));   // start near the top
        s.vel = new Vector2(
            Random.Range(driftRangeX.x, driftRangeX.y),
            -Random.Range(speedRangeY.x, speedRangeY.y));               // downward
        if (s.trail != null)
        {
            s.trail.sizeDelta = new Vector2(s.size * 0.8f, s.size * 12f);
            s.trail.anchoredPosition = s.rt.anchoredPosition + new Vector2(0, s.size * 6f);
        }
    }

    // ── Programmatic circle sprite (no external asset needed) ───────────────
    private static Sprite MakeCircleSprite(int diameter)
    {
        var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = diameter * 0.5f;
        var cols = new Color32[diameter * diameter];
        for (int y = 0; y < diameter; y++)
        for (int x = 0; x < diameter; x++)
        {
            float dx = x + 0.5f - r;
            float dy = y + 0.5f - r;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // Soft edge — fade alpha across the last 1.5 pixel ring
            float a = Mathf.Clamp01((r - dist) / 1.5f);
            cols[y * diameter + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f));
    }
}
