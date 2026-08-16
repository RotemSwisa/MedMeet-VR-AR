using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-bar 1-2-3 step indicator shown on every showcase canvas.
/// Active step glows teal; completed steps show a checkmark.
///
/// Set up by SustainabilityShowcaseBuilder; just call SetStep(0/1/2) at runtime.
/// </summary>
public class StepDotsUI : MonoBehaviour
{
    [System.Serializable]
    public class StepEntry
    {
        public Image           bubble;
        public TextMeshProUGUI bubbleLabel;
        public TextMeshProUGUI nameLabel;
        public Image           connectorToNext;
    }

    public StepEntry[] steps = new StepEntry[3];

    public Color activeBubble   = new Color(0.219f, 0.839f, 0.812f, 1f);
    public Color completedColor = new Color(0.219f, 0.839f, 0.812f, 1f);
    public Color inactiveBubble = new Color(0.470f, 0.705f, 0.784f, 0.18f);
    public Color activeText     = Color.white;
    public Color inactiveText   = new Color(0.435f, 0.549f, 0.600f, 1f);
    public Color activeName     = Color.white;
    public Color inactiveName   = new Color(0.661f, 0.760f, 0.800f, 1f);

    public void SetStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            var s = steps[i];
            if (s == null) continue;
            bool completed = i < index;
            bool active    = i == index;

            if (s.bubble != null)
                s.bubble.color = (completed || active) ? activeBubble : inactiveBubble;
            if (s.bubbleLabel != null)
            {
                s.bubbleLabel.text  = completed ? "✓" : (i + 1).ToString();
                s.bubbleLabel.color = (completed || active) ? new Color(0.019f, 0.129f, 0.149f) : inactiveText;
            }
            if (s.nameLabel != null)
                s.nameLabel.color = active ? activeName : (completed ? activeName : inactiveName);

            if (s.connectorToNext != null)
                s.connectorToNext.color = i < index
                    ? completedColor
                    : SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.16f);
        }
    }
}
