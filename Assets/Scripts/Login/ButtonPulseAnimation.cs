using UnityEngine;

public class ButtonPulseAnimation : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("מהירות הדופק")]
    public float pulseSpeed = 2f;

    [Tooltip("כמה להגדיל")]
    public float scaleAmount = 1.1f;

    private Vector3 originalScale;
    private float time;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // חישוב גודל בצורת סינוס
        time += Time.deltaTime * pulseSpeed;
        float scale = 1f + (Mathf.Sin(time) * (scaleAmount - 1f));

        // העלה על הכפתור
        transform.localScale = originalScale * scale;
    }
}