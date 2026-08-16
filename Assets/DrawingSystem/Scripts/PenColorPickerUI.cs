using UnityEngine;
using UnityEngine.UI;

public class PenColorPickerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PenController penController;
    [SerializeField] private Transform penTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("UI Position Settings")]
    [SerializeField] private Vector3 uiOffset = new Vector3(0.15f, 0.05f, 0f);
    [SerializeField] private bool faceCamera = true;

    [Header("Color Buttons")]
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Color[] colors;

    [Header("Current Color Display")]
    [SerializeField] private Image currentColorIndicator;

    private bool isVisible = false;
    private Camera mainCamera;
    private bool colorButtonPressed = false; // Prevent double clicks

    void Start()
    {
        mainCamera = Camera.main;

        // Setup color buttons
        if (colorButtons != null && colors != null)
        {
            for (int i = 0; i < colorButtons.Length && i < colors.Length; i++)
            {
                int index = i; // Capture for closure
                colorButtons[i].onClick.AddListener(() => SelectColor(colors[index]));
            }
        }

        // Start hidden
        HideUI();
    }

    void Update()
    {
        if (isVisible)
        {
            // Position UI near pen
            if (penTransform != null)
            {
                transform.position = penTransform.position + penTransform.TransformDirection(uiOffset);

                // Face camera
                if (faceCamera && mainCamera != null)
                {
                    transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                   mainCamera.transform.rotation * Vector3.up);
                }
            }
        }
    }

    public void ShowUI()
    {
        isVisible = true;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void HideUI()
    {
        isVisible = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void ToggleUI()
    {
        if (isVisible)
            HideUI();
        else
            ShowUI();
    }

    void SelectColor(Color color)
    {
        if (colorButtonPressed) return; // Prevent double clicks
        colorButtonPressed = true;

        if (penController != null)
        {
            penController.SetPenColor(color);

            // Update indicator
            if (currentColorIndicator != null)
            {
                currentColorIndicator.color = color;
            }

            Debug.Log($"Color changed to: {color}");
        }

        // Reset after short delay
        Invoke(nameof(ResetColorButton), 0.3f);
    }

    void ResetColorButton()
    {
        colorButtonPressed = false;
    }

    public void SetPenController(PenController controller)
    {
        penController = controller;
    }
}