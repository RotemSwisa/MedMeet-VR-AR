using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class KnowledgeScreenController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pickerPanel;
    public GameObject displayPanel;

    [Header("Display")]
    public RawImage displayImage;
    public VideoPlayer videoPlayer;
    public DocumentLibrary documentLibrary;
    public DocumentPickerUI pickerUI;

    [Header("Navigation")]
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject pageNavGroup;        // אובייקט שמכיל prev+next+indicator, מוסתר בסרטון
    public TMPro.TextMeshProUGUI pageIndicatorText;

    private int _currentDocumentIndex = -1;
    private int _currentPageIndex = 0;

    public void SelectDocument(int documentIndex)
    {
        _currentDocumentIndex = documentIndex;
        _currentPageIndex = 0;

        var doc = documentLibrary.allDocuments[documentIndex];

        pickerPanel.SetActive(false);
        displayPanel.SetActive(true);

        if (doc.isVideo)
            ShowVideo(doc);
        else
            ShowPage();
    }

    private void ShowPage()
    {
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);
        displayImage.gameObject.SetActive(true);
        if (pageNavGroup) pageNavGroup.SetActive(true);
        UpdateDisplay();
    }

    private void ShowVideo(DocumentData doc)
    {
        if (pageNavGroup) pageNavGroup.SetActive(false);

        // צור RenderTexture וחבר לפני Play
        RenderTexture rt = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = rt;
        displayImage.texture = rt;
        displayImage.gameObject.SetActive(true);

        videoPlayer.clip = doc.videoClip;
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;
        vp.Play();
    }

    public void NextPage()
    {
        if (_currentDocumentIndex < 0) return;
        var doc = documentLibrary.allDocuments[_currentDocumentIndex];
        if (_currentPageIndex < doc.pages.Length - 1)
        {
            _currentPageIndex++;
            UpdateDisplay();
        }
    }

    public void PrevPage()
    {
        if (_currentPageIndex > 0)
        {
            _currentPageIndex--;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (_currentDocumentIndex < 0 || documentLibrary == null) return;
        var doc = documentLibrary.allDocuments[_currentDocumentIndex];
        if (doc.pages == null || doc.pages.Length == 0) return;

        _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, doc.pages.Length - 1);
        displayImage.texture = doc.pages[_currentPageIndex];

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{_currentPageIndex + 1} / {doc.pages.Length}";

        if (prevButton) prevButton.SetActive(_currentPageIndex > 0);
        if (nextButton) nextButton.SetActive(_currentPageIndex < doc.pages.Length - 1);
    }

    public void OpenPicker()
    {
        displayPanel.SetActive(false);
        pickerPanel.SetActive(true);
        pickerUI.Open(this);
    }

    public void BackToPicker()
    {
        if (videoPlayer.isPlaying) videoPlayer.Stop();
        displayPanel.SetActive(false);
        pickerPanel.SetActive(true);
    }
}