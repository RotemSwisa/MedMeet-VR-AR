using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

/// <summary>
/// AI Clinical Advisor — משתמש ב-Groq API (עובד מישראל, חינמי)
/// שם הקלאס נשאר GeminiClient כדי שלא יצטרכו לשנות כלום ב-ClinicalAdvisorUI
/// </summary>
public class GeminiClient : MonoBehaviour
{
    [Header("Groq API Settings")]
    [Tooltip("API Key מ-console.groq.com — אותו key שיש לכם כבר ב-GroqClient!")]
    public string apiKey = "הכנס-כאן-את-ה-GROQ-API-KEY";

    private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";
    private const string MODEL = "llama-3.3-70b-versatile";

    private const string SYSTEM_PROMPT =
        "You are a clinical AI advisor assisting in a medical VR meeting between doctors. " +
        "Based on the conversation transcript below, provide a brief clinical suggestion or insight in 2-3 sentences. " +
        "Focus on: differential diagnosis, recommended tests, treatment considerations, or safety alerts. " +
        "Be concise, professional, and clinically relevant. Respond in the same language as the conversation.";

    public static GeminiClient Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AskForClinicalAdvice(string conversationTranscript, System.Action<string> onResponse)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("הכנס"))
        {
            Debug.LogError("❌ Groq API Key לא הוגדר! פתח את GeminiClient בInspector והכנס את המפתח.");
            onResponse?.Invoke("API Key missing. Please configure the AI advisor.");
            return;
        }

        StartCoroutine(SendRequest(conversationTranscript, onResponse));
    }

    IEnumerator SendRequest(string transcript, System.Action<string> onResponse)
    {
        string userMessage = "Conversation transcript:\n" + transcript;

        // פורמט OpenAI-compatible שגם Groq משתמש בו
        string jsonBody =
            "{" +
            "\"model\":\"" + MODEL + "\"," +
            "\"messages\":[" +
            "{\"role\":\"system\",\"content\":\"" + EscapeJson(SYSTEM_PROMPT) + "\"}," +
            "{\"role\":\"user\",\"content\":\"" + EscapeJson(userMessage) + "\"}" +
            "]," +
            "\"max_tokens\":300," +
            "\"temperature\":0.7" +
            "}";

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Groq API Error: {request.error}\n{request.downloadHandler.text}");
                onResponse?.Invoke("AI advisor unavailable.");
                yield break;
            }

            string responseText = ParseGroqResponse(request.downloadHandler.text);
            Debug.Log($"✅ Groq Response: {responseText}");
            onResponse?.Invoke(responseText);
        }
    }

    string ParseGroqResponse(string jsonResponse)
    {
        // פורמט: {"choices":[{"message":{"content":"..."}}]}
        try
        {
            int contentIndex = jsonResponse.IndexOf("\"content\":");
            if (contentIndex < 0) return "No response from AI.";

            int start = jsonResponse.IndexOf("\"", contentIndex + 10) + 1;
            int end = start;

            // מוצא את סוף הstring תוך טיפול ב-escaped quotes
            while (end < jsonResponse.Length)
            {
                if (jsonResponse[end] == '"' && jsonResponse[end - 1] != '\\') break;
                end++;
            }

            if (end <= start) return "Parse error.";

            string raw = jsonResponse.Substring(start, end - start);
            raw = raw.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            return raw.Trim();
        }
        catch
        {
            return "Error reading AI response.";
        }
    }

    string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}