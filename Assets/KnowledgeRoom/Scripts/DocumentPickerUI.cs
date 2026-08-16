using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DocumentPickerUI : MonoBehaviour
{
    [Header("References")]
    public DocumentLibrary documentLibrary;
    public Transform itemContainer;         // Grid/Vertical Layout Group
    public GameObject documentItemPrefab;   // כפתור אחד ברשימה
    public TMP_InputField searchField;
    public GameObject panelRoot;            // האובייקט הראשי של הפאנל

    private KnowledgeScreenController _targetScreen;
    private List<GameObject> _spawnedItems = new List<GameObject>();

    public void Open(KnowledgeScreenController screen)
    {
        _targetScreen = screen;
        panelRoot.SetActive(true);
        PopulateList("");
    }

    public void Close()
    {
        panelRoot.SetActive(false);
    }

    // נקרא מה-InputField של החיפוש (OnValueChanged)
    public void OnSearchChanged(string query)
    {
        PopulateList(query);
    }

    private void PopulateList(string query)
    {
        // נקה ישן
        foreach (var item in _spawnedItems)
            Destroy(item);
        _spawnedItems.Clear();

        // בנה חדש
        for (int i = 0; i < documentLibrary.allDocuments.Length; i++)
        {
            var doc = documentLibrary.allDocuments[i];

            // פילטר חיפוש
            if (!string.IsNullOrEmpty(query))
            {
                bool titleMatch = doc.documentTitle.ToLower().Contains(query.ToLower());
                bool categoryMatch = doc.category.ToLower().Contains(query.ToLower());
                if (!titleMatch && !categoryMatch) continue;
            }

            var itemGO = Instantiate(documentItemPrefab, itemContainer);
            _spawnedItems.Add(itemGO);

            // הגדר טקסט ואירוע
            var btn = itemGO.GetComponent<Button>();
            var label = itemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = $"{doc.documentTitle}\n<size=70%><color=#888>{doc.category}</color></size>";

            int index = i; // חשוב! closure
            btn.onClick.AddListener(() =>
            {
                _targetScreen.SelectDocument(index);
                Close();
            });
        }
    }
}