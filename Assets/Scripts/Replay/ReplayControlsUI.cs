using UnityEngine;
using UnityEngine.UI;

// פקדי Replay בזמן צפייה
public class ReplayControlsUI : MonoBehaviour
{
    [Header("Control Buttons")]
    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Button resumeButton;

    [Header("Status Display")]
    public Text statusText;
    public Slider progressSlider;
    public Text timeText; // "00:30 / 02:15"

    [Header("Settings")]
    public GameObject controlsPanel; // הפאנל שמכיל את הפקדים

    private bool isInitialized = false;

    void Start()
    {
        // חבר כפתורים
        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPause);

        if (stopButton != null)
            stopButton.onClick.AddListener(OnStop);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResume);

        // התחל עם הפאנל כבוי
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }

    void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (ReplayManager.Instance == null) return;

        bool isPlaying = ReplayManager.Instance.IsPlaying;
        RecordingData recording = ReplayManager.Instance.CurrentRecording;

        // עדכן כפתורים
        if (playButton != null)
            playButton.interactable = !isPlaying && recording != null;

        if (pauseButton != null)
            pauseButton.interactable = isPlaying;

        if (stopButton != null)
            stopButton.interactable = recording != null;

        if (resumeButton != null)
            resumeButton.interactable = !isPlaying && recording != null;

        // עדכן טקסט סטטוס
        if (statusText != null)
        {
            if (recording == null)
                statusText.text = "אין הקלטה טעונה";
            else if (isPlaying)
                statusText.text = "מנגן...";
            else
                statusText.text = "מושהה";
        }

        // עדכן Progress (זה יצריך שינוי קטן ב-ReplayManager)
        // כרגע נשאיר ריק
    }

    public void OnPlay()
    {
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StartPlayback();
            ShowControls();
        }
    }

    public void OnPause()
    {
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.PausePlayback();
        }
    }

    public void OnStop()
    {
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.StopPlayback();
            HideControls();
        }
    }

    public void OnResume()
    {
        if (ReplayManager.Instance != null)
        {
            ReplayManager.Instance.ResumePlayback();
        }
    }

    public void ShowControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(true);
    }

    public void HideControls()
    {
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
    }
}