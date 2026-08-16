using UnityEngine;
using UnityEngine.UI;

public class SimpleColorPicker : MonoBehaviour
{
    [SerializeField] private PenController penController;
    [SerializeField] private Image currentColorDisplay;

    [Header("Preset Colors")]
    [SerializeField]
    private Color[] presetColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.black,
        Color.white,
        new Color(1f, 0.5f, 0f), // Orange
        new Color(0.5f, 0f, 1f)  // Purple
    };

    [Header("Width Settings")]
    [SerializeField] private Slider widthSlider;
    [SerializeField] private Text widthValueText;
    [SerializeField] private float minWidth = 0.005f;
    [SerializeField] private float maxWidth = 0.05f;

    private int currentColorIndex = 0;

    void Start()
    {
        if (penController == null)
        {
            Debug.LogError("PenController not assigned to ColorPicker!");
        }

        // Setup width slider
        if (widthSlider != null)
        {
            widthSlider.minValue = minWidth;
            widthSlider.maxValue = maxWidth;
            widthSlider.value = 0.01f;
            widthSlider.onValueChanged.AddListener(OnWidthChanged);
        }

        // Set initial color
        SetColor(presetColors[0]);
    }

    public void NextColor()
    {
        currentColorIndex = (currentColorIndex + 1) % presetColors.Length;
        SetColor(presetColors[currentColorIndex]);
    }

    public void PreviousColor()
    {
        currentColorIndex--;
        if (currentColorIndex < 0)
            currentColorIndex = presetColors.Length - 1;

        SetColor(presetColors[currentColorIndex]);
    }

    public void SetColorByIndex(int index)
    {
        if (index >= 0 && index < presetColors.Length)
        {
            currentColorIndex = index;
            SetColor(presetColors[index]);
        }
    }

    private void SetColor(Color color)
    {
        if (penController != null)
        {
            penController.SetPenColor(color);
        }

        if (currentColorDisplay != null)
        {
            currentColorDisplay.color = color;
        }
    }

    private void OnWidthChanged(float value)
    {
        if (penController != null)
        {
            penController.SetPenWidth(value);
        }

        if (widthValueText != null)
        {
            widthValueText.text = $"{value:F3}";
        }
    }
}