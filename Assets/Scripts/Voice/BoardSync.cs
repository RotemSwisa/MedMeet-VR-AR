using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class BoardModel
{
    [RealtimeProperty(1, true, true)]
    private string _latestMessage;
    [RealtimeProperty(2, true, true)]
    private string _senderName;
    [RealtimeProperty(3, true, true)]
    private float _speechDuration;
}

public class BoardSync : RealtimeComponent<BoardModel>
{
    // AI ADVISOR event — מופעל כשמגיעה הודעה חדשה
    public event System.Action<string, string> OnNewMessage;

    private string lastProcessedMessage = "";

    // *** AI ADVISOR: רשימה שצוברת את כל ההודעות של הפגישה ***
    private System.Collections.Generic.List<string> conversationHistory
        = new System.Collections.Generic.List<string>();

    protected override void OnRealtimeModelReplaced(BoardModel previousModel, BoardModel currentModel)
    {
        if (previousModel != null) previousModel.latestMessageDidChange -= OnMessageReceived;
        if (currentModel != null)
        {
            if (currentModel.isFreshModel)
            {
                currentModel.latestMessage = "";
                currentModel.senderName = "";
                currentModel.speechDuration = 0f;
            }
            currentModel.latestMessageDidChange += OnMessageReceived;
        }
    }

    private void OnMessageReceived(BoardModel model, string message)
    {
        if (string.IsNullOrEmpty(message) || message == lastProcessedMessage) return;
        lastProcessedMessage = message;

        string sender = model.senderName;
        float duration = model.speechDuration;

        // *** AI ADVISOR: שמירת ההודעה להיסטוריה ***
        conversationHistory.Add($"{sender}: {message}");

        // *** AI ADVISOR: הפעלת event ***
        OnNewMessage?.Invoke(sender, message);

        if (MeetingAuditor.Instance != null)
        {
            string displayLine = $"<color=yellow>{sender}:</color> {message}";
            MeetingAuditor.Instance.ForceAddLineToScreen(displayLine);
            MeetingAuditor.Instance.LogChat(sender, message, duration);
        }
    }

    public void AddMessage(string playerName, string message, float duration)
    {
        model.senderName = playerName;
        model.speechDuration = duration;
        model.latestMessage = message;
    }

    // *** AI ADVISOR: מחזיר את כל השיחה כטקסט אחד לשליחה ל-Gemini ***
    public string GetFullTranscript()
    {
        if (conversationHistory.Count == 0) return "";
        return string.Join("\n", conversationHistory);
    }
}