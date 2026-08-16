using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;
using TMPro;

/// <summary>
/// AR Sign Language Recognition — uses the device camera (WebCamTexture) to capture
/// a person performing sign language in front of the user, sends frames to Groq's
/// vision model (llama-3.2-11b-vision-preview), and displays the recognized Hebrew
/// text as subtitles on the AR canvas.
///
/// SETUP (done automatically by MedMeet Tools → Setup Sign Language AR):
///   • Attach to a GameObject in the scene (auto-created by the Editor tool).
///   • References (subtitleText, subtitlePanel, toggleButton) wired by Editor tool.
///   • Reuses the API key from the existing GroqClient in the scene.
///
/// USAGE IN AR MODE:
///   • Press the "זיהוי שפת סימנים" button that appears in AR mode.
///   • Point the headset camera at the person signing.
///   • Recognized Hebrew words appear as subtitles at the bottom of the view.
///   • PC / Editor simulation: press 1–9 keys to inject test signs (no button press needed in Editor).
/// </summary>
public class ARSignLanguageSystem : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("UI")]
    public TextMeshProUGUI subtitleText;
    public GameObject      subtitlePanel;
    public TextMeshProUGUI toggleButtonLabel;

    [Header("Camera")]
    [Tooltip("Leave empty to use the first available camera")]
    public string preferredCameraName = "";
    public int camWidth  = 640;
    public int camHeight = 480;

    [Header("Recognition")]
    [Tooltip("Seconds between automatic capture attempts — keep ≥ 1 to respect rate limits.")]
    public float captureInterval = 1.5f;
    [Tooltip("Seconds of silence before subtitle is cleared")]
    public float clearAfterSeconds = 8f;
    [Tooltip("JPEG quality sent to API (lower = faster, less accurate)")]
    [Range(40, 95)] public int jpegQuality = 85;
    [Tooltip("Number of frames to send per request. 2 lets the model see motion between two captures (recommended for ISL).")]
    [Range(1, 3)] public int framesPerRequest = 2;
    [Tooltip("Seconds between the consecutive frames within one multi-frame request.")]
    [Range(0.15f, 1.0f)] public float interFrameDelay = 0.45f;

    [Header("Groq Vision")]
    [Tooltip("Model used for sign recognition — vision-capable model required. " +
             "Maverick 17B (128 experts) is the most capable free vision model on Groq.")]
    public string groqModel = "meta-llama/llama-4-maverick-17b-128e-instruct";

    // ── Quest controller demo mode ──────────────────────────────────────────
    [Header("Quest controller demo mode (A / B / X / Y → sign word)")]
    [Tooltip("Word shown when the RIGHT-controller A button is pressed.")]
    public string aButtonWord = "Thank you";
    [Tooltip("Word shown when the RIGHT-controller B button is pressed.")]
    public string bButtonWord = "Hello";
    [Tooltip("Word shown when the LEFT-controller X button is pressed.")]
    public string xButtonWord = "Yes";
    [Tooltip("Word shown when the LEFT-controller Y button is pressed.")]
    public string yButtonWord = "No";
    [Tooltip("Legacy flag — IGNORED. Controller buttons now ALWAYS require the " +
             "sign-language system to be explicitly ON (toggled via the AR button) " +
             "before they inject a word. This prevents A/B/X/Y from colliding with " +
             "other features that share the same buttons.")]
    public bool controllerSimWorksWhenOff = false;

    // ── State ────────────────────────────────────────────────────────────────

    private bool   isActive;
    private string currentSentence = "";
    private float  lastSignTime;

    private WebCamTexture webcamTexture;
    private Texture2D     captureBuffer;
    private bool          cameraReady;
    private bool          requestInFlight;

    private string groqApiKey;

    // Edge-detection state for Quest controller buttons — remembers each
    // button's pressed state from the previous frame so that one PRESS only
    // injects the word once (instead of firing every frame while held).
    private bool _prevA, _prevB, _prevX, _prevY;

    // ── Keyboard simulation (PC / Editor) ───────────────────────────────────

    private static readonly (KeyCode key, string word)[] SimKeys =
    {
        (KeyCode.Alpha1, "שלום"),
        (KeyCode.Alpha2, "תודה"),
        (KeyCode.Alpha3, "כן"),
        (KeyCode.Alpha4, "לא"),
        (KeyCode.Alpha5, "עזרה"),
        (KeyCode.Alpha6, "רופא"),
        (KeyCode.Alpha7, "כאב"),
        (KeyCode.Alpha8, "מה שלומך"),
        (KeyCode.Alpha9, "טוב"),
        (KeyCode.Alpha0, "חולה"),
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        // Resolve API key from existing GroqClient in scene
        if (GroqClient.Instance != null)
            groqApiKey = GroqClient.Instance.ApiKey;
        else
            Debug.LogWarning("[ARSignLang] GroqClient not found in scene — API calls will fail.");

        SetVisible(false);
    }

    void OnDestroy() => StopCamera();

    void Update()
    {
        // ── Editor keyboard simulation ─────────────────────────────────────
        // In the Unity editor the keys 1–9 inject test words so you can verify
        // the subtitle panel without needing a real camera or clicking the button.
        // On-device this block is compiled out (no overhead).
#if UNITY_EDITOR
        foreach (var (key, word) in SimKeys)
        {
            if (Input.GetKeyDown(key))
            {
                AppendWord(word);
                // Make panel visible even if recognition was not started via button
                if (subtitlePanel != null) subtitlePanel.SetActive(true);
                Debug.Log($"[ARSignLang] Keyboard sim → {word}");
            }
        }
        if (Input.GetKeyDown(KeyCode.Backspace)) RemoveLastWord();
        if (Input.GetKeyDown(KeyCode.Return))    ClearSentence();
#endif

        // ── Quest controller demo simulation ────────────────────────────────
        // Polls the right-hand A/B and left-hand X/Y buttons every frame.
        // Each fresh press (not hold) appends the mapped word and forces the
        // subtitle panel visible — so the presenter just walks up wearing the
        // Quest and clicks a button to "speak" a sign. Runs on-device AND in
        // the editor (when an XR device is connected via the simulator).
        PollControllerSimulation();

        if (!isActive) return;

        // Auto-clear after silence
        if (!string.IsNullOrEmpty(currentSentence) &&
            Time.time - lastSignTime > clearAfterSeconds)
            ClearSentence();
    }

    // ── Controller polling helpers ──────────────────────────────────────────
    private void PollControllerSimulation()
    {
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (CheckButtonPress(right, CommonUsages.primaryButton,   ref _prevA))
            InjectControllerWord(aButtonWord, "A");
        if (CheckButtonPress(right, CommonUsages.secondaryButton, ref _prevB))
            InjectControllerWord(bButtonWord, "B");
        if (CheckButtonPress(left,  CommonUsages.primaryButton,   ref _prevX))
            InjectControllerWord(xButtonWord, "X");
        if (CheckButtonPress(left,  CommonUsages.secondaryButton, ref _prevY))
            InjectControllerWord(yButtonWord, "Y");
    }

    /// <summary>
    /// Returns true exactly once per press transition (released → pressed).
    /// Updates `prev` so subsequent frames while the button is still held
    /// do not re-trigger.
    /// </summary>
    private static bool CheckButtonPress(InputDevice device,
                                          InputFeatureUsage<bool> button,
                                          ref bool prev)
    {
        bool now = false;
        if (device.isValid) device.TryGetFeatureValue(button, out now);
        bool justPressed = now && !prev;
        prev = now;
        return justPressed;
    }

    private void InjectControllerWord(string word, string buttonName)
    {
        if (string.IsNullOrEmpty(word)) return;
        // STRICT: controller buttons inject a sign word ONLY when the
        // sign-language system has been explicitly turned on via the AR
        // toggle button (StartRecognition → isActive = true). Without this
        // guard the A/B/X/Y buttons would inject "Hello" / "Thank you" / etc.
        // every time another feature in the project uses the same buttons.
        if (!isActive) return;

        AppendWord(word);
        if (subtitleText  != null) subtitleText.gameObject.SetActive(true);
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        Debug.Log($"[ARSignLang] Controller sim ({buttonName}) → {word}");
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void ToggleRecognition()
    {
        if (isActive) StopRecognition();
        else          StartRecognition();
    }

    public void StartRecognition()
    {
        if (isActive) return;
        isActive = true;
        ClearSentence();
        SetVisible(true);
        UpdateButtonLabel();
        StartCamera();
        StartCoroutine(CaptureLoop());
        Debug.Log("[ARSignLang] Started.");
    }

    public void StopRecognition()
    {
        if (!isActive) return;
        isActive = false;
        StopAllCoroutines();
        StopCamera();
        SetVisible(false);
        UpdateButtonLabel();
        Debug.Log("[ARSignLang] Stopped.");
    }

    // ── Camera ───────────────────────────────────────────────────────────────

    private void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            // Important: on Quest 3 this hits because WebCamTexture cannot
            // access the world-facing passthrough cameras without Meta's
            // Passthrough Camera API. Keyboard sim still works in Editor.
            Debug.LogWarning("[ARSignLang] No camera devices reported by WebCamTexture.\n" +
                             "  • PC: check that a webcam is connected and that Unity has " +
                             "    camera permission (Edit → Project Settings → Player → Webcam Usage).\n" +
                             "  • Quest 3: the passthrough cameras are not exposed via WebCamTexture. " +
                             "    Use Meta's Passthrough Camera API or simulate via the 1–9 keys.");
            ShowStatus("⚠️ No camera detected");
            return;
        }

        string deviceName = devices[0].name;
        foreach (var d in devices)
            if (!string.IsNullOrEmpty(preferredCameraName) && d.name.Contains(preferredCameraName))
                deviceName = d.name;

        webcamTexture = new WebCamTexture(deviceName, camWidth, camHeight, 30);
        webcamTexture.Play();
        captureBuffer = new Texture2D(camWidth, camHeight, TextureFormat.RGB24, false);
        cameraReady   = true;
        Debug.Log($"[ARSignLang] Camera: {deviceName} ({camWidth}×{camHeight})");
        // Give the user a clear visual that the system is alive even before
        // the first recognition comes back.
        ShowStatus("📷 Listening for signs…");
    }

    private void StopCamera()
    {
        if (webcamTexture != null) { webcamTexture.Stop(); Destroy(webcamTexture); webcamTexture = null; }
        if (captureBuffer != null) { Destroy(captureBuffer); captureBuffer = null; }
        cameraReady = false;
    }

    // ── Capture → Groq Vision loop ───────────────────────────────────────────

    private IEnumerator CaptureLoop()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(captureInterval);

            if (!cameraReady || webcamTexture == null || !webcamTexture.didUpdateThisFrame)
                continue;
            if (requestInFlight) continue;
            if (string.IsNullOrEmpty(groqApiKey)) continue;

            yield return CaptureAndAnalyse(silentOnMiss: true);
        }
    }

    /// <summary>
    /// PUBLIC — fired by the manual "Capture Sign Now" button. Grabs the
    /// current frame(s) and sends them to Groq immediately, regardless of
    /// the captureInterval tick. Lets the presenter freeze a clear pose and
    /// trigger detection on demand.
    /// </summary>
    public void CaptureSignNow()
    {
        if (!isActive)
        {
            Debug.Log("[ARSignLang] Capture-now ignored — system is OFF. Press the toggle button first.");
            return;
        }
        if (requestInFlight)
        {
            ShowStatus("Already analysing… please wait.");
            return;
        }
        StartCoroutine(CaptureAndAnalyse(silentOnMiss: false));
    }

    /// <summary>
    /// Captures `framesPerRequest` consecutive frames (spaced by
    /// interFrameDelay seconds) and sends them all in one Groq Vision
    /// request. Two frames give the model enough motion context to tell
    /// "hand raised" from "hand lowering".
    /// </summary>
    private IEnumerator CaptureAndAnalyse(bool silentOnMiss)
    {
        if (!cameraReady || webcamTexture == null) yield break;

        var images = new System.Collections.Generic.List<string>();
        int frames = Mathf.Max(1, framesPerRequest);

        for (int i = 0; i < frames; i++)
        {
            // Wait for the webcam to push a fresh frame before sampling
            yield return new WaitUntil(() => webcamTexture.didUpdateThisFrame);

            captureBuffer.SetPixels32(webcamTexture.GetPixels32());
            captureBuffer.Apply();
            byte[] jpegBytes = captureBuffer.EncodeToJPG(jpegQuality);
            images.Add(System.Convert.ToBase64String(jpegBytes));

            if (i < frames - 1) yield return new WaitForSeconds(interFrameDelay);
        }

        ShowStatus("Analysing…");
        yield return SendToGroqVision(images, silentOnMiss);
    }

    private IEnumerator SendToGroqVision(System.Collections.Generic.List<string> base64Images,
                                         bool silentOnMiss)
    {
        requestInFlight = true;

        // The prompt now DESCRIBES each ISL sign visually instead of asking
        // the model to "know" Israeli Sign Language. Vision models like
        // Llama-4 Maverick are far better at matching visual descriptions
        // than at recalling specialised sign-language datasets they were
        // never trained on. The presenter rehearses these specific gestures.
        string prompt =
            "You are analysing " + base64Images.Count + " consecutive frame(s) of a person making " +
            "an Israeli Sign Language (ISL) gesture for a medical conversation.\n\n" +
            "Match what you see against ONE of these signs (look at hand shape, hand position " +
            "near the body, and movement between frames):\n" +
            "  • שלום  (Hello)      — open hand near forehead/temple, palm forward, slight wave.\n" +
            "  • תודה  (Thank you)  — flat hand touches chin or lips, then moves forward.\n" +
            "  • כן    (Yes)        — closed fist with knuckles up, small nod up-and-down.\n" +
            "  • לא    (No)         — index + middle finger extended, tapping the thumb twice.\n" +
            "  • עזרה  (Help)       — closed fist resting on top of an open palm, lifted upward.\n" +
            "  • רופא  (Doctor)     — two fingers (index + middle) tap the inside of the opposite wrist.\n" +
            "  • כאב   (Pain)       — both index fingers pointing at each other, twisting motion.\n" +
            "  • חולה  (Sick)       — middle finger of the dominant hand touches the forehead.\n" +
            "  • טוב   (Good)       — thumbs-up, hand near the chest.\n" +
            "  • מים   (Water)      — 'W' shape (3 fingers) taps the chin.\n\n" +
            "RULES:\n" +
            " 1. If ONE sign clearly matches, reply with ONLY its Hebrew word, nothing else.\n" +
            " 2. If hands are not visible, the image is blurry, or you are NOT confident, reply " +
            "    with exactly: none\n" +
            " 3. Do NOT explain, do NOT add punctuation, do NOT translate.";

        string json = BuildVisionRequest(base64Images, prompt);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest("https://api.groq.com/openai/v1/chat/completions",
                                            UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type",  "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

        yield return req.SendWebRequest();

        requestInFlight = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ARSignLang] Groq error: {req.error} | {req.downloadHandler.text}");
            if (!silentOnMiss) ShowStatus("⚠️ API error — check internet / API key");
            yield break;
        }

        string word = ExtractGroqText(req.downloadHandler.text).Trim();

        if (string.IsNullOrEmpty(word) || word.ToLower() == "none" || word == ".")
        {
            if (!silentOnMiss) ShowStatus("No clear sign detected — try again");
            yield break;
        }

        AppendWord(word);
        Debug.Log($"[ARSignLang] Recognized: {word}");
    }

    // ── JSON helpers ─────────────────────────────────────────────────────────

    private string BuildVisionRequest(System.Collections.Generic.List<string> base64Images, string userPrompt)
    {
        // Build the user content array: one image_url block per frame, then
        // the prompt text. Hand-built JSON keeps zero dependency on JsonUtility
        // (which doesn't handle nested OpenAI-style content arrays).
        var content = new StringBuilder();
        for (int i = 0; i < base64Images.Count; i++)
        {
            if (i > 0) content.Append(",");
            content.Append("{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:image/jpeg;base64,")
                   .Append(base64Images[i]).Append("\"}}");
        }
        content.Append(",{\"type\":\"text\",\"text\":").Append(JsonString(userPrompt)).Append("}");

        return "{" +
            $"\"model\":\"{groqModel}\"," +
            "\"messages\":[{" +
                "\"role\":\"user\"," +
                "\"content\":[" + content + "]" +
            "}]," +
            "\"max_tokens\":30," +
            "\"temperature\":0.1" +
        "}";
    }

    // Legacy single-image overload — kept so any external caller still compiles.
    private string BuildVisionRequest(string base64Image, string userPrompt)
        => BuildVisionRequest(new System.Collections.Generic.List<string> { base64Image }, userPrompt);

    private static string JsonString(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";

    private static string ExtractGroqText(string json)
    {
        // Parse: {"choices":[{"message":{"content":"..."}}]}
        const string marker = "\"content\":\"";
        int start = json.IndexOf(marker);
        if (start < 0) return "";
        start += marker.Length;
        int end = json.IndexOf("\"", start);
        while (end > 0 && json[end - 1] == '\\') end = json.IndexOf("\"", end + 1);
        if (end < 0) return "";
        return json.Substring(start, end - start)
                   .Replace("\\n", " ")
                   .Replace("\\\"", "\"")
                   .Trim();
    }

    // ── Sentence helpers ─────────────────────────────────────────────────────

    private void AppendWord(string word)
    {
        currentSentence = string.IsNullOrEmpty(currentSentence)
            ? word
            : currentSentence + " " + word;

        lastSignTime = Time.time;
        RefreshDisplay();

        StopCoroutine(nameof(AutoClear));
        StartCoroutine(nameof(AutoClear));
    }

    private void RemoveLastWord()
    {
        int i = currentSentence.LastIndexOf(' ');
        currentSentence = i >= 0 ? currentSentence[..i] : "";
        RefreshDisplay();
    }

    private void ClearSentence()
    {
        currentSentence = "";
        RefreshDisplay();
    }

    private IEnumerator AutoClear()
    {
        yield return new WaitForSeconds(clearAfterSeconds);
        ClearSentence();
    }

    // ── Display ──────────────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (subtitleText != null) subtitleText.text = currentSentence;
        bool hasText = !string.IsNullOrEmpty(currentSentence);
        if (subtitlePanel != null) subtitlePanel.SetActive(hasText && isActive);
    }

    /// <summary>
    /// Show a transient status message in the subtitle area without
    /// overwriting the recognised sentence. Used during the demo so the
    /// presenter knows when a request is in flight, when the API failed,
    /// or when no hand was detected. Restores the real sentence after 1.5s.
    /// </summary>
    private void ShowStatus(string status)
    {
        if (subtitleText == null) return;
        StopCoroutine(nameof(StatusFadeBack));
        subtitleText.text = status;
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        StartCoroutine(nameof(StatusFadeBack));
        Debug.Log("[ARSignLang] Status → " + status);
    }

    private IEnumerator StatusFadeBack()
    {
        yield return new WaitForSeconds(1.6f);
        // Only restore the sentence if we still have one — otherwise stay quiet
        if (subtitleText != null) subtitleText.text = currentSentence;
        if (subtitlePanel != null)
            subtitlePanel.SetActive(!string.IsNullOrEmpty(currentSentence) && isActive);
    }

    private void SetVisible(bool v)
    {
        if (subtitleText  != null) subtitleText.gameObject.SetActive(v);
        if (subtitlePanel != null) subtitlePanel.SetActive(false); // hidden until text arrives
    }

    private void UpdateButtonLabel()
    {
        if (toggleButtonLabel != null)
            toggleButtonLabel.text = isActive ? "Sign Language   ON  ●" : "Sign Language   OFF";
    }
}
