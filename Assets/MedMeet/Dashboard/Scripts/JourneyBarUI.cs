using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One round-trip bar on the Dashboard's "Round trips replaced" list.
///
/// Layout: [icon] City ⇆ Host                                   1,234 km
///         [-------------- filled bar (animated) --------------]
///         Flight · 245 kg CO₂ · 53 L fuel
/// </summary>
public class JourneyBarUI : MonoBehaviour
{
    [Header("Header line")]
    public Image           modeIcon;
    public Sprite          planeSprite;
    public Sprite          carSprite;
    public TextMeshProUGUI routeLabel;
    public TextMeshProUGUI kmLabel;

    [Header("Bar")]
    public RectTransform   barFill;
    public Image           barFillImage;

    [Header("Sub line")]
    public TextMeshProUGUI subLabel;

    [Header("Colours")]
    public Color planeColor = new Color(0.474f, 0.725f, 1.000f, 1f);
    public Color carColor   = new Color(0.372f, 0.878f, 0.659f, 1f);

    [Header("Fill animation")]
    public float fillDuration = 1.1f;

    public void Bind(SustainabilityData.Leg leg, string hostName, float targetFraction, float delay)
    {
        bool plane = leg.isPlane;
        var color  = plane ? planeColor : carColor;

        if (modeIcon != null)
        {
            modeIcon.sprite = plane ? planeSprite : carSprite;
            modeIcon.color  = color;
        }
        if (routeLabel != null)
            routeLabel.text = $"<b>{leg.cityName}</b>  ⇆  {hostName}";
        if (kmLabel != null)
            kmLabel.text = $"{SustainabilityData.Fmt(leg.roundTrip)} km";
        if (subLabel != null)
            subLabel.text = $"{(plane ? "Flight" : "Drive")} · {SustainabilityData.Fmt(leg.co2)} kg CO₂ · {SustainabilityData.Fmt(leg.fuel)} L fuel";
        if (barFillImage != null) barFillImage.color = color;

        StartCoroutine(AnimateFill(Mathf.Clamp01(targetFraction), delay));
    }

    private IEnumerator AnimateFill(float target, float delay)
    {
        if (barFill != null)
            barFill.localScale = new Vector3(0f, 1f, 1f);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < fillDuration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / fillDuration), 3f);  // ease-out cubic
            if (barFill != null)
                barFill.localScale = new Vector3(Mathf.Lerp(0f, target, k), 1f, 1f);
            yield return null;
        }
        if (barFill != null) barFill.localScale = new Vector3(target, 1f, 1f);
    }
}
