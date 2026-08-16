using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// מצמיד לכל TextMeshPro שמציג מספר.
/// כשקוראים ל-SetValue() הוא מאייר בצורה חלקה מהערך הישן לחדש.
/// </summary>
public class AnimatedCounter : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI label;

    [Header("Format")]
    [Tooltip("פורמט C# — {0:F0} = מספר שלם, {0:F1} = עשרוני אחד")]
    public string format = "{0:F0}";

    [Tooltip("סיומת אחרי המספר, למשל ' kg' או ' km'")]
    public string suffix = "";

    [Header("Animation")]
    [Tooltip("כמה שניות אורכת הנפשת המונה")]
    public float animationDuration = 1.2f;

    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // -------------------------------------------------------------------
    private float _currentValue;
    private float _targetValue;
    private Coroutine _animCoroutine;

    // -------------------------------------------------------------------
    void Awake()
    {
        if (label == null)
            label = GetComponent<TextMeshProUGUI>();
    }

    // -------------------------------------------------------------------
    /// <summary>
    /// קרא לזה כדי לשנות את ערך המונה עם הנפשה.
    /// </summary>
    public void SetValue(float newValue)
    {
        if (Mathf.Approximately(_targetValue, newValue)) return;

        _targetValue = newValue;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateTo(newValue));
    }

    /// <summary>
    /// הגדרת ערך מיידית ללא הנפשה (למשל באתחול).
    /// </summary>
    public void SetValueImmediate(float value)
    {
        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _currentValue = value;
        _targetValue = value;
        UpdateLabel(value);
    }

    // -------------------------------------------------------------------
    private IEnumerator AnimateTo(float target)
    {
        float startValue = _currentValue;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float ev = easeCurve.Evaluate(t);

            _currentValue = Mathf.Lerp(startValue, target, ev);
            UpdateLabel(_currentValue);

            yield return null;
        }

        _currentValue = target;
        UpdateLabel(_currentValue);
    }

    private void UpdateLabel(float value)
    {
        if (label == null) return;
        label.text = string.Format(format, value) + suffix;
    }
}