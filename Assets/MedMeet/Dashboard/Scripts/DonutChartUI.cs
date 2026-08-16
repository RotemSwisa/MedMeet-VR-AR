using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three-segment donut chart driven by Unity's filled Image component.
///
/// Three Image children are stacked: each is set to Image.Type.Filled +
/// FillMethod.Radial360 with the same outer/inner radii drawn by sprites.
/// Their fillAmount values represent the cumulative fraction of the total
/// (segment-2 hides under segment-3 etc.) so colours stack cleanly.
///
/// AnimateDraw() eases the fill from 0 → final over the given duration.
/// </summary>
public class DonutChartUI : MonoBehaviour
{
    [Header("Three radial-filled images. Order: air, road, vr")]
    public Image airImage;
    public Image roadImage;
    public Image vrImage;

    [Header("Background ring (full 360, neutral colour)")]
    public Image backgroundRing;

    [Header("Colours")]
    public Color airColor  = new Color(0.474f, 0.725f, 1.000f, 1f);
    public Color roadColor = new Color(0.219f, 0.839f, 0.812f, 1f);
    public Color vrColor   = new Color(0.372f, 0.878f, 0.659f, 1f);

    private float _airFrac, _roadFrac, _vrFrac;
    private Coroutine _anim;

    void Awake() => ApplyColours();

    private void ApplyColours()
    {
        if (airImage  != null) airImage.color  = airColor;
        if (roadImage != null) roadImage.color = roadColor;
        if (vrImage   != null) vrImage.color   = vrColor;
    }

    /// <summary>Sets the three values (kg CO2). Caller should also AnimateDraw().</summary>
    public void SetSegments(float air, float road, float vr)
    {
        float total = Mathf.Max(0.0001f, air + road + vr);
        _airFrac  = air  / total;
        _roadFrac = road / total;
        _vrFrac   = vr   / total;
    }

    /// <summary>Animate fillAmount from 0 → final over duration seconds.</summary>
    public void AnimateDraw(float duration = 1.0f)
    {
        if (!isActiveAndEnabled) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Draw(duration));
    }

    private IEnumerator Draw(float duration)
    {
        ApplyColours();
        if (backgroundRing != null) backgroundRing.fillAmount = 1f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));

            // Stack: outermost-drawn (vr) takes the full fraction, middle (road)
            // takes vr+road, innermost (air) takes the whole drawn arc.
            float total = _airFrac + _roadFrac + _vrFrac;
            float drawTotal = total * k;
            float air  = Mathf.Min(_airFrac, drawTotal);
            float road = Mathf.Min(_airFrac + _roadFrac, drawTotal);
            float vr   = drawTotal;

            // The order of stacking from outermost (drawn last) to innermost (drawn first):
            // We render vr-image largest fill, road on top, air on top — so set them
            // so each successive layer occupies its slice.
            // Simpler: vrImage shows full drawTotal, roadImage shows air+road,
            // airImage shows just air. Air sits on top.
            if (vrImage   != null) vrImage.fillAmount   = vr;
            if (roadImage != null) roadImage.fillAmount = road;
            if (airImage  != null) airImage.fillAmount  = air;

            yield return null;
        }

        if (vrImage   != null) vrImage.fillAmount   = _airFrac + _roadFrac + _vrFrac;
        if (roadImage != null) roadImage.fillAmount = _airFrac + _roadFrac;
        if (airImage  != null) airImage.fillAmount  = _airFrac;
    }
}
