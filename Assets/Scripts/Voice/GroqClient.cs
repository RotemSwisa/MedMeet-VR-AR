using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GroqClient : MonoBehaviour
{
    // Leave this empty. Anything typed here is serialised into the scene file
    // and would be committed with it. The key is loaded at runtime instead.
    [SerializeField] private string apiKey = "";

    public static GroqClient Instance { get; private set; }
    public string ApiKey => apiKey;

    void Awake()
    {
        Instance = this;
        if (string.IsNullOrEmpty(apiKey))
            apiKey = LoadKeyFromLocalConfig();
    }

    /// <summary>
    /// Reads the Groq API key from a location that is not under source control:
    /// the GROQ_API_KEY environment variable, or Assets/Resources/groq_key.txt
    /// (git-ignored). Never hard-code the key into a script or a scene.
    /// </summary>
    private static string LoadKeyFromLocalConfig()
    {
        string fromEnv = System.Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (!string.IsNullOrEmpty(fromEnv))
            return fromEnv;

        TextAsset keyFile = Resources.Load<TextAsset>("groq_key");
        if (keyFile != null && !string.IsNullOrEmpty(keyFile.text))
            return keyFile.text.Trim();

        Debug.LogWarning(
            "GroqClient: no API key found. Set the GROQ_API_KEY environment " +
            "variable, or create Assets/Resources/groq_key.txt containing the key.");
        return string.Empty;
    }

    private string urlTranslation = "https://api.groq.com/openai/v1/audio/translations";

    // *** תיקון: פרומפט שמונע ריבועים ותווים מיוחדים ***
    private string systemPrompt = "Translate the Hebrew speech to concise English. Use only standard English letters and punctuation. Do not use URLs, emojis, or special symbols. If audio is noise, output nothing.";

    public delegate void TranscriptionCallback(string text);

    public IEnumerator SendAudioForTranscription(AudioClip clip, TranscriptionCallback onSuccess)
    {
        byte[] wavData = AudioUtils.EncodeToWAV(clip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-large-v3");

        // שימוש בפרומפט החדש
        form.AddField("prompt", systemPrompt);

        // טמפרטורה 0.2 נותנת גמישות קלה לזיהוי מבטא
        form.AddField("temperature", "0.2");
        form.AddField("response_format", "json");

        using (UnityWebRequest www = UnityWebRequest.Post(urlTranslation, form))
        {
            www.SetRequestHeader("Authorization", "Bearer " + apiKey);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Groq Error: {www.error} | {www.downloadHandler.text}");
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                string transcribedText = ExtractTextFromJson(jsonResponse);

                // פילטר: האם ה-AI פלט את ההוראות של עצמו?
                if (IsPromptLeak(transcribedText))
                {
                    Debug.LogWarning("⚠️ Groq hallucinated the prompt instructions. Ignoring.");
                }
                else if (!string.IsNullOrWhiteSpace(transcribedText))
                {
                    onSuccess?.Invoke(transcribedText);
                }
            }
        }
    }

    private bool IsPromptLeak(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string lower = text.ToLower();

        if (lower.Contains("translate the hebrew")) return true;
        if (lower.Contains("concise english")) return true;
        if (lower.Contains("output nothing")) return true;

        return false;
    }

    private string ExtractTextFromJson(string json)
    {
        int startIndex = json.IndexOf("\"text\":");
        if (startIndex == -1) return "";

        startIndex += 7;

        while (startIndex < json.Length && (json[startIndex] == ' ' || json[startIndex] == ':' || json[startIndex] == '"' || json[startIndex] == '\n'))
        {
            startIndex++;
        }

        int endIndex = json.IndexOf("\"", startIndex);
        while (endIndex > 0 && json[endIndex - 1] == '\\')
        {
            endIndex = json.IndexOf("\"", endIndex + 1);
        }

        if (endIndex == -1) return "";

        string result = json.Substring(startIndex, endIndex - startIndex);
        return result.Replace("\\n", " ").Replace("\\\"", "\"").Trim();
    }
}