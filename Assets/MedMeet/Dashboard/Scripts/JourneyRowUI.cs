using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in the right-side "Journeys you're replacing" list on the Setup screen,
/// and on the Dashboard's Round-trips-replaced bar list.
///
/// Layout: [✈ / 🚗 icon] "City → Host" / "Flight or Drive avoided"  ……  km value
/// </summary>
public class JourneyRowUI : MonoBehaviour
{
    public Image           modeIcon;
    public Sprite          planeSprite;
    public Sprite          carSprite;
    public TextMeshProUGUI routeLabel;
    public TextMeshProUGUI modeLabel;
    public TextMeshProUGUI kmLabel;
    public TextMeshProUGUI kmUnitLabel;

    public Color planeTint = new Color(0.474f, 0.725f, 1f, 1f);  // sky
    public Color carTint   = new Color(0.372f, 0.878f, 0.659f, 1f);  // mint

    public void Bind(string cityName, string hostName, float oneWayKm, bool isPlane)
    {
        if (modeIcon != null)
        {
            modeIcon.sprite = isPlane ? planeSprite : carSprite;
            modeIcon.color  = isPlane ? planeTint   : carTint;
        }
        if (routeLabel != null) routeLabel.text  = $"<b>{cityName}</b>  →  {hostName}";
        if (modeLabel  != null) modeLabel.text   = isPlane ? "Flight avoided" : "Drive avoided";
        if (kmLabel    != null) kmLabel.text     = SustainabilityData.Fmt(oneWayKm);
        if (kmUnitLabel!= null) kmUnitLabel.text = "km";
    }
}
