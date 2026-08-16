using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row inside the SetupCanvas participant list.
/// Layout: [index badge] [name input] [city button] [host pill OR remove btn]
///
/// `isMine` controls whether the row is interactive: only the local client
/// can edit their own entry. Other clients' rows are read-only previews.
/// </summary>
public class ParticipantRowUI : MonoBehaviour
{
    [Header("Visual parts")]
    public TextMeshProUGUI indexLabel;
    public Image           indexBackground;
    public TMP_InputField  nameInput;
    public TextMeshProUGUI nameReadonly;     // shown when not mine
    public Button          cityButton;
    public TextMeshProUGUI cityLabel;
    public TextMeshProUGUI cityCountryLabel;
    public GameObject      hostPill;
    public GameObject      youPill;          // "YOU" badge for the local row
    public Button          removeButton;

    [Header("Colours")]
    public Color hostBgColor    = new Color(0.219f, 0.839f, 0.812f, 1f);
    public Color guestBgColor   = new Color(0.184f, 0.431f, 0.560f, 1f);
    public Color hostTextColor  = new Color(0.019f, 0.129f, 0.149f, 1f);
    public Color guestTextColor = Color.white;

    public int OwnerClientID { get; private set; }
    public string CurrentName  { get; private set; }
    public string CurrentCityId{ get; private set; }

    private Action<string> _onName;
    private Action         _onPickCity;
    private Action         _onRemove;

    public void Bind(int index, string name, string cityId, bool isMine,
                     Action<string> onName, Action onPickCity, Action onRemove)
    {
        OwnerClientID    = index;
        CurrentName      = name ?? "";
        CurrentCityId    = cityId ?? "";
        _onName     = onName;
        _onPickCity = onPickCity;
        _onRemove   = onRemove;

        // Show editable input only for my own row
        if (nameInput != null)
        {
            nameInput.gameObject.SetActive(isMine);
            nameInput.SetTextWithoutNotify(CurrentName);
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener(v =>
            {
                CurrentName = v;
                _onName?.Invoke(v);
            });
        }
        if (nameReadonly != null)
        {
            nameReadonly.gameObject.SetActive(!isMine);
            nameReadonly.text = string.IsNullOrEmpty(CurrentName) ? "—" : CurrentName;
        }

        if (cityButton != null)
        {
            cityButton.interactable = isMine;
            cityButton.onClick.RemoveAllListeners();
            if (isMine) cityButton.onClick.AddListener(() => _onPickCity?.Invoke());
        }
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(isMine && index > 0);
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => _onRemove?.Invoke());
        }
        if (youPill != null) youPill.SetActive(isMine);

        SetIndex(index, isHost: index == 0);
        SetCity(cityId);
    }

    public void SetIndex(int index, bool isHost)
    {
        if (indexLabel      != null) indexLabel.text  = (index + 1).ToString();
        if (indexBackground != null) indexBackground.color = isHost ? hostBgColor : guestBgColor;
        if (indexLabel      != null) indexLabel.color = isHost ? hostTextColor : guestTextColor;
        if (hostPill        != null) hostPill.SetActive(isHost);
        if (nameInput       != null && nameInput.placeholder is TextMeshProUGUI ph)
            ph.text = isHost ? "Host name" : "Your name";
    }

    public void SetCity(string cityId)
    {
        var c = string.IsNullOrEmpty(cityId) ? null : SustainabilityData.CityById(cityId);
        CurrentCityId = cityId ?? "";
        if (cityLabel != null)
        {
            cityLabel.text  = c == null ? "Pick city" : c.name;
            cityLabel.color = c == null ? SustainabilityTheme.InkFaint : SustainabilityTheme.Ink;
        }
        if (cityCountryLabel != null)
        {
            cityCountryLabel.text = c == null ? "" : c.country;
            cityCountryLabel.gameObject.SetActive(c != null);
        }
        if (cityButton != null)
        {
            var img = cityButton.GetComponent<Image>();
            if (img != null)
                img.color = c == null
                    ? SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f)
                    : SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.18f);
        }
    }
}
