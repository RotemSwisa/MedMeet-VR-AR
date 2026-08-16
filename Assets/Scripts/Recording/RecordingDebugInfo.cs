using UnityEngine;
using System.IO;

// מציג מידע Debug על ההקלטות
public class RecordingDebugInfo : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showDebugInfo = true;
    public KeyCode toggleKey = KeyCode.F1;

    private string debugText = "";
    private GUIStyle style;

    void Start()
    {
        style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugInfo = !showDebugInfo;
        }

        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // רקע שחור
        GUI.Box(new Rect(10, 10, 400, 300), "");

        // טקסט
        GUI.Label(new Rect(20, 20, 380, 280), debugText, style);
    }

    private void UpdateDebugInfo()
    {
        debugText = "=== Recording System Debug ===\n\n";

        // Recording Manager
        if (RecordingManager.Instance != null)
        {
            debugText += $"Recording Active: {RecordingManager.Instance.IsRecording}\n";
            debugText += $"Recordings Path: {RecordingManager.Instance.GetRecordingsDirectory()}\n";

            var recordings = RecordingManager.Instance.GetAllRecordings();
            debugText += $"Total Recordings: {recordings.Count}\n\n";
        }
        else
        {
            debugText += "RecordingManager: NOT FOUND\n\n";
        }

        // Replay Manager
        if (ReplayManager.Instance != null)
        {
            debugText += $"Replay Active: {ReplayManager.Instance.IsPlaying}\n";

            if (ReplayManager.Instance.CurrentRecording != null)
            {
                var rec = ReplayManager.Instance.CurrentRecording;
                debugText += $"Current Recording: {rec.recordingName}\n";
                debugText += $"Duration: {rec.duration:F2}s\n";
                debugText += $"Players: {rec.players.Count}\n";
            }
            else
            {
                debugText += "No Recording Loaded\n";
            }
        }
        else
        {
            debugText += "ReplayManager: NOT FOUND\n";
        }

        debugText += $"\n[{toggleKey}] Toggle Debug Info";
    }

    // פונקציה ידנית להדפסת מידע על הקלטה
    public static void PrintRecordingInfo(RecordingData recording)
    {
        if (recording == null)
        {
            Debug.Log("Recording is NULL");
            return;
        }

        Debug.Log("=== Recording Info ===");
        Debug.Log($"Name: {recording.recordingName}");
        Debug.Log($"Duration: {recording.duration:F2} seconds");
        Debug.Log($"Frame Rate: {recording.frameRate}");
        Debug.Log($"Players: {recording.players.Count}");

        foreach (var player in recording.players)
        {
            Debug.Log($"\n  Player: {player.playerName} (ID: {player.clientID})");
            Debug.Log($"    Body Frames: {player.bodyFrames.Count}");
            Debug.Log($"    Head Frames: {player.headFrames.Count}");
            Debug.Log($"    Left Hand: {player.leftHandFrames.Count}");
            Debug.Log($"    Right Hand: {player.rightHandFrames.Count}");
            Debug.Log($"    Audio Frames: {player.audioFrames.Count}");
        }
    }
}