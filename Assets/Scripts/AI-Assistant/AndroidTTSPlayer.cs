using UnityEngine;

/// <summary>
/// Free Android text-to-speech wrapper. Uses the device's built-in TTS engine
/// (the same one Google Maps and your phone's accessibility tools use), so:
///   • No API key, no network call, no per-minute cost.
///   • Works offline on the Quest 3 (it's an Android headset).
///   • In the Unity editor / on PC, falls back to a Debug.Log so you can still
///     see what would have been spoken.
///
/// USAGE
///   1. Drop this component on any GameObject in the scene (it survives scene
///      changes via DontDestroyOnLoad).
///   2. Call AndroidTTSPlayer.Instance.Speak("Your text here") from any code.
///   3. Call AndroidTTSPlayer.Instance.Stop() to interrupt the current speech.
///
/// SAFETY
///   • Creates exactly one TTS engine for the lifetime of the app.
///   • Properly shuts it down in OnDestroy to release the audio focus.
///   • If TTS isn't ready yet when Speak() is called, the request is queued
///     and spoken as soon as init completes.
/// </summary>
public class AndroidTTSPlayer : MonoBehaviour
{
    public static AndroidTTSPlayer Instance { get; private set; }

    [Tooltip("Speech rate multiplier — 1.0 is normal, 0.5 is half speed, 2.0 is twice as fast.")]
    [Range(0.5f, 2f)] public float speechRate = 1.0f;

    [Tooltip("Pitch multiplier — 1.0 is normal.")]
    [Range(0.5f, 2f)] public float pitch = 1.0f;

    [Tooltip("Log to Console whenever Speak is called (helpful while debugging).")]
    public bool logEverySpeech = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _tts;
    private bool _ready;
    private string _pendingText;   // queued while init is running
#endif

#if UNITY_EDITOR_WIN
    // Editor-only audio confirmation. This entire block is removed by the
    // preprocessor when Unity builds for any platform other than Windows
    // editor — it CAN NOT reach the Quest APK. Uses PowerShell + Windows'
    // built-in System.Speech library, so no extra DLL is needed.
    [Tooltip("Editor only: play through PowerShell System.Speech so you can " +
             "hear what would be spoken on Quest. Never included in builds.")]
    public bool useWindowsFallbackInEditor = true;
    private System.Diagnostics.Process _winTtsProcess;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
#if UNITY_ANDROID && !UNITY_EDITOR
        InitEngine();
#endif
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (_tts != null)
            {
                _tts.Call("stop");
                _tts.Call("shutdown");
                _tts.Dispose();
            }
        }
        catch (System.Exception ex) { Debug.LogWarning("[TTS] Shutdown error: " + ex.Message); }
#endif
#if UNITY_EDITOR_WIN
        StopWindowsTts();
#endif
    }

    /// <summary>Speak the given text. Stops any speech already in progress.</summary>
    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (logEverySpeech) Debug.Log("[TTS] Speak: " + Preview(text));

#if UNITY_ANDROID && !UNITY_EDITOR
        if (_tts == null) { Debug.LogWarning("[TTS] Engine missing — call lost."); return; }
        if (!_ready) { _pendingText = text; return; }  // queue until init finishes

        try
        {
            _tts.Call<int>("setSpeechRate", speechRate);
            _tts.Call<int>("setPitch",      pitch);
            _tts.Call<int>("stop");
            // QUEUE_FLUSH = 0  → flush previous utterances and speak now
            _tts.Call<int>("speak", text, 0, null, "medmeet-utterance");
        }
        catch (System.Exception ex) { Debug.LogError("[TTS] Speak error: " + ex.Message); }
#endif

#if UNITY_EDITOR_WIN
        if (useWindowsFallbackInEditor) SpeakOnWindows(text);
#endif
    }

    /// <summary>Stop the current speech immediately.</summary>
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { if (_tts != null) _tts.Call<int>("stop"); }
        catch (System.Exception ex) { Debug.LogWarning("[TTS] Stop error: " + ex.Message); }
#endif

#if UNITY_EDITOR_WIN
        StopWindowsTts();
#endif
    }

#if UNITY_EDITOR_WIN
    /// <summary>
    /// Editor-only Windows fallback. Spawns a hidden PowerShell process that
    /// invokes the System.Speech.Synthesis assembly built into Windows.
    /// Runs out-of-process so it never blocks Unity's main thread, and any
    /// previous utterance is killed before a new one starts.
    /// </summary>
    private void SpeakOnWindows(string text)
    {
        try
        {
            StopWindowsTts();   // interrupt any previous utterance

            // Escape double-quotes for the PowerShell command line.
            // Replace newlines with spaces so we get a single utterance.
            string safe = text.Replace("\"", "'")
                              .Replace("\n", " ")
                              .Replace("\r", " ");

            string cmd =
                "Add-Type -AssemblyName System.Speech;" +
                "$s = New-Object System.Speech.Synthesis.SpeechSynthesizer;" +
               $"$s.Rate = {(int)((speechRate - 1f) * 10f)};" +
               $"$s.Speak(\\\"{safe}\\\")";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName        = "powershell.exe",
                Arguments       = "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"",
                CreateNoWindow  = true,
                UseShellExecute = false,
                WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            _winTtsProcess = System.Diagnostics.Process.Start(psi);
            Debug.Log("[TTS] Windows fallback speaking…");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[TTS] Windows fallback failed: " + ex.Message);
        }
    }

    private void StopWindowsTts()
    {
        try
        {
            if (_winTtsProcess != null && !_winTtsProcess.HasExited)
                _winTtsProcess.Kill();
        }
        catch { /* process may have already exited */ }
        _winTtsProcess = null;
    }
#endif

    /// <summary>True once the TTS engine has finished initialising.</summary>
    public bool IsReady
#if UNITY_ANDROID && !UNITY_EDITOR
        => _ready;
#else
        => true;   // editor mode is always "ready" (logs only)
#endif

    // ── Android-only init plumbing ─────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitEngine()
    {
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                activity,
                new InitListener(this));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TTS] Failed to create engine: " + ex.Message);
        }
    }

    private void OnTtsReady(bool ok)
    {
        _ready = ok;
        if (!ok) { Debug.LogWarning("[TTS] Engine init failed."); return; }
        Debug.Log("[TTS] Ready");
        if (!string.IsNullOrEmpty(_pendingText))
        {
            string t = _pendingText;
            _pendingText = null;
            Speak(t);
        }
    }

    /// <summary>
    /// Java proxy that bridges Android's OnInitListener callback back into
    /// the C# side. The Android engine calls onInit(status) once init is done;
    /// status == 0 (SUCCESS) means TTS is usable.
    /// </summary>
    private class InitListener : AndroidJavaProxy
    {
        private readonly AndroidTTSPlayer _owner;
        public InitListener(AndroidTTSPlayer owner)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        { _owner = owner; }

        // ReSharper disable once UnusedMember.Local — called from Android
        public void onInit(int status)
        {
            // Hop back to the Unity thread before touching anything
            UnityMainThreadDispatcher.Run(() => _owner.OnTtsReady(status == 0));
        }
    }
#endif

    private static string Preview(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= 80 ? s : s.Substring(0, 77) + "...";
    }
}

/// <summary>
/// Tiny utility for marshalling callbacks from background threads (Android
/// JNI, Normcore, network coroutines) back onto Unity's main thread. Kept
/// in this file so AndroidTTSPlayer is self-contained.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly System.Collections.Generic.Queue<System.Action> _queue
        = new System.Collections.Generic.Queue<System.Action>();

    public static void Run(System.Action action)
    {
        if (action == null) return;
        if (_instance == null)
        {
            var go = new GameObject("[UnityMainThreadDispatcher]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
        }
        lock (_queue) _queue.Enqueue(action);
    }

    void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)
            {
                try { _queue.Dequeue()?.Invoke(); }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
