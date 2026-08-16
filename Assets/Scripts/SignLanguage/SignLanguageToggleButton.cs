using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR-mode Sign Language recognition toggle button.
///
/// Visual states:
///   OFF  → dark blue background  · "Sign Language  OFF"
///   ON   → bright green background · "Sign Language  ON  ●"
///
/// Wired automatically by SignLanguageSetupTool.
/// Shown/hidden by VRARSwitcher when entering/leaving AR mode.
/// </summary>
[RequireComponent(typeof(Button))]
public class SignLanguageToggleButton : MonoBehaviour
{
    [Header("References")]
    public ARSignLanguageSystem signLanguageSystem;
    public TextMeshProUGUI      buttonLabel;

    [Header("Labels (English)")]
    public string labelWhenOff = "Sign Language   OFF";
    public string labelWhenOn  = "Sign Language   ON  ●";

    [Header("Button Colors")]
    public Color colorWhenOff = new Color(0.13f, 0.18f, 0.32f, 1f);   // dark navy
    public Color colorWhenOn  = new Color(0.10f, 0.65f, 0.35f, 1f);   // vivid green

    // ── State ─────────────────────────────────────────────────────────────
    private bool   _isOn;
    private Button _button;
    private Image  _background;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        _button     = GetComponent<Button>();
        _background = GetComponent<Image>();

        if (_button != null)
            _button.onClick.AddListener(OnButtonClick);
    }

    void Start()
    {
        if (signLanguageSystem == null)
            signLanguageSystem = FindFirstObjectByType<ARSignLanguageSystem>();

        RefreshVisuals();
    }

    // ── Called by VRARSwitcher when switching VR ↔ AR ─────────────────────
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible && _isOn)
        {
            _isOn = false;
            signLanguageSystem?.StopRecognition();
            RefreshVisuals();
        }
    }

    // ── Button click ───────────────────────────────────────────────────────
    public void OnButtonClick()
    {
        _isOn = !_isOn;
        RefreshVisuals();
        signLanguageSystem?.ToggleRecognition();
    }

    // ── Visual update ──────────────────────────────────────────────────────
    private void RefreshVisuals()
    {
        if (buttonLabel  != null)
            buttonLabel.text = _isOn ? labelWhenOn : labelWhenOff;

        if (_background  != null)
            _background.color = _isOn ? colorWhenOn : colorWhenOff;

        // Slightly enlarge font when active to draw attention
        if (buttonLabel != null)
            buttonLabel.fontStyle = _isOn ? FontStyles.Bold : FontStyles.Normal;
    }
}
