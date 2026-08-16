using UnityEngine;
using UnityEngine.UI;

// מחבר את כפתורי ה-UI הקיימים למערכת ההקלטה
public class RecordingUIController : MonoBehaviour
{
    [Header("Recording Buttons")]
    public Button startRecordingButton;
    public Button stopRecordingButton;
    public Button viewRecordingsButton;

    [Header("Status")]
    public Text statusText; // אופציונלי - להצגת סטטוס

    [Header("Replay Browser")]
    public GameObject replayBrowserPanel; // פאנל של דפדפן ההקלטות

    void Start()
    {
        // חבר את הכפתורים
        if (startRecordingButton != null)
            startRecordingButton.onClick.AddListener(OnStartRecording);

        if (stopRecordingButton != null)
            stopRecordingButton.onClick.AddListener(OnStopRecording);

        if (viewRecordingsButton != null)
            viewRecordingsButton.onClick.AddListener(OnViewRecordings);

        // עדכן UI התחלתי
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    public void OnStartRecording()
    {
        if (RecordingManager.Instance != null)
        {
            RecordingManager.Instance.StartRecording();
            Debug.Log("UI: הקלטה התחילה");
        }
    }

    public void OnStopRecording()
    {
        if (RecordingManager.Instance != null)
        {
            RecordingManager.Instance.StopRecording();
            Debug.Log("UI: הקלטה נעצרה");
        }
    }

    public void OnViewRecordings()
    {
        if (replayBrowserPanel != null)
        {
            ReplayBrowser browser = replayBrowserPanel.GetComponent<ReplayBrowser>();
            if (browser != null)
            {
                browser.OpenBrowser();
            }
            else
            {
                replayBrowserPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("UI: לא הוגדר ReplayBrowser Panel!");
        }
    }

    private void UpdateUI()
    {
        if (RecordingManager.Instance == null) return;

        bool isRecording = RecordingManager.Instance.IsRecording;

        // עדכן כפתורים
        if (startRecordingButton != null)
            startRecordingButton.interactable = !isRecording;

        if (stopRecordingButton != null)
            stopRecordingButton.interactable = isRecording;

        // עדכן טקסט סטטוס
        if (statusText != null)
        {
            statusText.text = isRecording ? "מקליט..." : "מוכן להקלטה";
        }
    }
}