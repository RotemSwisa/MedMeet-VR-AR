using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// City picker overlay shown when a participant taps their city button.
///
/// Layout: full-canvas dim, centred panel with a header, optional search
/// (typing not required in VR — we expose every city as a button), and a
/// scrollable grid of city buttons. Selecting a city closes the popup.
/// </summary>
public class CitySelectPopup : MonoBehaviour
{
    [Header("Root container (the dim background — toggled active/inactive)")]
    public GameObject root;

    [Header("Scroll content (vertical layout group — buttons spawn here)")]
    public RectTransform contentContainer;

    [Header("City button prefab")]
    public GameObject cityButtonPrefab;

    [Header("Close button (X)")]
    public Button closeButton;

    [Header("Search field (optional — works with a keyboard panel)")]
    public TMP_InputField searchField;

    private Action<string> _onPicked;
    private string         _selectedId;
    private readonly List<GameObject> _spawned = new List<GameObject>();

    void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (searchField != null) searchField.onValueChanged.AddListener(OnSearchChanged);
        if (root != null) root.SetActive(false);
    }

    public void Open(string preselectedCityId, Action<string> onPicked)
    {
        _onPicked   = onPicked;
        _selectedId = preselectedCityId;
        if (root != null) root.SetActive(true);
        if (searchField != null) searchField.SetTextWithoutNotify("");
        Rebuild("");
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnSearchChanged(string q) => Rebuild(q);

    private void Rebuild(string query)
    {
        if (contentContainer == null || cityButtonPrefab == null) return;
        foreach (var go in _spawned) if (go != null) Destroy(go);
        _spawned.Clear();

        string q = (query ?? "").Trim().ToLowerInvariant();
        foreach (var c in SustainabilityData.Cities)
        {
            if (q.Length > 0 && !(c.name.ToLowerInvariant().Contains(q) ||
                                  c.country.ToLowerInvariant().Contains(q)))
                continue;

            var btnGO = Instantiate(cityButtonPrefab, contentContainer);
            btnGO.SetActive(true);
            _spawned.Add(btnGO);

            var name  = btnGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            var ctry  = btnGO.transform.Find("Country")?.GetComponent<TextMeshProUGUI>();
            var bg    = btnGO.GetComponent<Image>();
            var btn   = btnGO.GetComponent<Button>();

            if (name != null) name.text = c.name;
            if (ctry != null) ctry.text = c.country;

            bool selected = c.id == _selectedId;
            if (bg != null)
                bg.color = selected
                    ? SustainabilityTheme.Tint(SustainabilityTheme.Teal, 0.22f)
                    : SustainabilityTheme.Tint(SustainabilityTheme.Bg2, 0.55f);

            string captureId = c.id;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => { _onPicked?.Invoke(captureId); Close(); });
            }
        }
    }
}
