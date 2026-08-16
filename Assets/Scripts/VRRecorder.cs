using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

public class VRRecorder : MonoBehaviour
{
    [Header("UI Elements")]
    public Button recordButton;
    public Button povRecordButton;
    public Button spectatorRecordButton;
    public TextMeshProUGUI povButtonText;
    public TextMeshProUGUI spectatorButtonText;
    public Image recordingIndicator;
    public Image spectatorIndicator;
    public TextMeshProUGUI recText;
    public TextMeshProUGUI spectatorRecText;

    [Header("Cameras")]
    public Camera spectatorCamera;

    [Header("Recording Settings")]
    public string recordingsFolder = "Recordings";
    public int frameRate = 30;
    public int width = 1920;
    public int height = 1080;

    [Header("Indicator Settings")]
    public float blinkSpeed = 0.5f;

#if UNITY_EDITOR
    private RecorderController povRecorderController;
    private RecorderController spectatorRecorderController;
#endif

    private bool isPOVRecording = false;
    private bool isSpectatorRecording = false;
    private Coroutine povBlinkCoroutine;
    private Coroutine spectatorBlinkCoroutine;
    private RenderTexture spectatorRenderTexture;

    void Start()
    {
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(ShowRecordingOptions);
        }

        if (povRecordButton != null)
        {
            povRecordButton.onClick.AddListener(TogglePOVRecording);
        }

        if (spectatorRecordButton != null)
        {
            spectatorRecordButton.onClick.AddListener(ToggleSpectatorRecording);
        }

#if UNITY_EDITOR
        string folderPath = Application.dataPath + "/../" + recordingsFolder;
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }
#endif

        if (recordingIndicator != null) recordingIndicator.enabled = false;
        if (spectatorIndicator != null) spectatorIndicator.enabled = false;
        if (recText != null) recText.enabled = false;
        if (spectatorRecText != null) spectatorRecText.enabled = false;

        if (povRecordButton != null) povRecordButton.gameObject.SetActive(false);
        if (spectatorRecordButton != null) spectatorRecordButton.gameObject.SetActive(false);

#if !UNITY_EDITOR
        // ב-Build - הסתר את כפתור ההקלטה
        if (recordButton != null)
        {
            recordButton.gameObject.SetActive(false);
            Debug.LogWarning("⚠️ Recording feature is only available in Unity Editor.");
        }
#endif
    }

    void ShowRecordingOptions()
    {
#if UNITY_EDITOR
        bool isActive = povRecordButton.gameObject.activeSelf;
        if (povRecordButton != null) povRecordButton.gameObject.SetActive(!isActive);
        if (spectatorRecordButton != null) spectatorRecordButton.gameObject.SetActive(!isActive);
#else
        Debug.LogWarning("⚠️ Recording is only available in Unity Editor.");
#endif
    }

    void TogglePOVRecording()
    {
#if UNITY_EDITOR
        if (isPOVRecording)
        {
            StopPOVRecording();
        }
        else
        {
            StartPOVRecording();
        }
#else
        Debug.LogWarning("⚠️ Recording is only available in Unity Editor.");
#endif
    }

    void ToggleSpectatorRecording()
    {
#if UNITY_EDITOR
        if (isSpectatorRecording)
        {
            StopSpectatorRecording();
        }
        else
        {
            StartSpectatorRecording();
        }
#else
        Debug.LogWarning("⚠️ Recording is only available in Unity Editor.");
#endif
    }

#if UNITY_EDITOR
    void StartPOVRecording()
    {
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        povRecorderController = new RecorderController(controllerSettings);

        var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorder.name = "POV Recording";
        videoRecorder.Enabled = true;
        videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        var gameViewInput = new GameViewInputSettings
        {
            OutputWidth = width,
            OutputHeight = height
        };
        videoRecorder.ImageInputSettings = gameViewInput;

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        videoRecorder.OutputFile = recordingsFolder + "/POV_Recording_" + timestamp;

        controllerSettings.FrameRate = frameRate;
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.SetRecordModeToManual();

        povRecorderController.PrepareRecording();
        povRecorderController.StartRecording();

        isPOVRecording = true;
        UpdatePOVButtonText();

        if (recordingIndicator != null)
        {
            povBlinkCoroutine = StartCoroutine(BlinkPOVIndicator());
        }

        Debug.Log("🎥 הקלטת POV התחילה!");
    }

    void StopPOVRecording()
    {
        if (povRecorderController != null)
        {
            povRecorderController.StopRecording();
            isPOVRecording = false;
            UpdatePOVButtonText();

            if (povBlinkCoroutine != null)
            {
                StopCoroutine(povBlinkCoroutine);
                povBlinkCoroutine = null;
            }

            if (recordingIndicator != null) recordingIndicator.enabled = false;
            if (recText != null) recText.enabled = false;

            Debug.Log("⏹ הקלטת POV נעצרה!");
        }
    }

    void StartSpectatorRecording()
    {
        if (spectatorCamera == null)
        {
            Debug.LogError("❌ Spectator Camera לא מוגדרת!");
            return;
        }

        spectatorRenderTexture = new RenderTexture(width, height, 24);
        spectatorCamera.targetTexture = spectatorRenderTexture;
        spectatorCamera.enabled = true;

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        spectatorRecorderController = new RecorderController(controllerSettings);

        var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorder.name = "Spectator Recording";
        videoRecorder.Enabled = true;
        videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        var renderTextureInput = new RenderTextureInputSettings
        {
            RenderTexture = spectatorRenderTexture
        };
        videoRecorder.ImageInputSettings = renderTextureInput;

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        videoRecorder.OutputFile = recordingsFolder + "/Spectator_Recording_" + timestamp;

        controllerSettings.FrameRate = frameRate;
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.AddRecorderSettings(videoRecorder);
        controllerSettings.SetRecordModeToManual();

        spectatorRecorderController.PrepareRecording();
        spectatorRecorderController.StartRecording();

        isSpectatorRecording = true;
        UpdateSpectatorButtonText();

        if (spectatorIndicator != null)
        {
            spectatorBlinkCoroutine = StartCoroutine(BlinkSpectatorIndicator());
        }

        Debug.Log("📹 הקלטת Spectator התחילה!");
    }

    void StopSpectatorRecording()
    {
        if (spectatorRecorderController != null)
        {
            spectatorRecorderController.StopRecording();
            isSpectatorRecording = false;
            UpdateSpectatorButtonText();

            if (spectatorBlinkCoroutine != null)
            {
                StopCoroutine(spectatorBlinkCoroutine);
                spectatorBlinkCoroutine = null;
            }

            if (spectatorIndicator != null) spectatorIndicator.enabled = false;
            if (spectatorRecText != null) spectatorRecText.enabled = false;

            if (spectatorCamera != null)
            {
                spectatorCamera.targetTexture = null;
                spectatorCamera.enabled = false;
            }

            if (spectatorRenderTexture != null)
            {
                spectatorRenderTexture.Release();
                Destroy(spectatorRenderTexture);
                spectatorRenderTexture = null;
            }

            Debug.Log("⏹ הקלטת Spectator נעצרה!");
        }
    }
#endif

    void UpdatePOVButtonText()
    {
        if (povButtonText != null)
        {
            povButtonText.text = isPOVRecording ? "⏹ Stop POV" : "🎥 POV Recording";
        }
    }

    void UpdateSpectatorButtonText()
    {
        if (spectatorButtonText != null)
        {
            spectatorButtonText.text = isSpectatorRecording ? "⏹ Stop Spectator" : "📹 Spectator Recording";
        }
    }

    IEnumerator BlinkPOVIndicator()
    {
        while (isPOVRecording)
        {
            if (recordingIndicator != null)
            {
                recordingIndicator.enabled = !recordingIndicator.enabled;
            }

            if (recText != null)
            {
                recText.enabled = recordingIndicator.enabled;
            }

            yield return new WaitForSeconds(blinkSpeed);
        }

        if (recordingIndicator != null) recordingIndicator.enabled = false;
        if (recText != null) recText.enabled = false;
    }

    IEnumerator BlinkSpectatorIndicator()
    {
        while (isSpectatorRecording)
        {
            if (spectatorIndicator != null)
            {
                spectatorIndicator.enabled = !spectatorIndicator.enabled;
            }

            if (spectatorRecText != null)
            {
                spectatorRecText.enabled = spectatorIndicator.enabled;
            }

            yield return new WaitForSeconds(blinkSpeed);
        }

        if (spectatorIndicator != null) spectatorIndicator.enabled = false;
        if (spectatorRecText != null) spectatorRecText.enabled = false;
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if (isPOVRecording) StopPOVRecording();
        if (isSpectatorRecording) StopSpectatorRecording();
#endif
    }
}