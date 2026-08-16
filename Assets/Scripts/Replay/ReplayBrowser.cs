using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI לבחירת הקלטות
public class ReplayBrowser : MonoBehaviour
{
    [Header("UI References")]
    public Transform recordingsListContent; // Content של ScrollView
    public GameObject recordingButtonPrefab; // Prefab של כפתור
    public Button closeButton;

    private List<string> availableRecordings;

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseBrowser);
        }

        // אל תרענן כאן - רק כשפותחים
        // RefreshRecordingsList();
    }

    void OnEnable()
    {
        // רענן כל פעם שהפאנל נפתח
        RefreshRecordingsList();
    }

    public void RefreshRecordingsList()
    {
        Debug.Log("ReplayBrowser: מרענן רשימת הקלטות...");

        // נקה רשימה קודמת
        foreach (Transform child in recordingsListContent)
        {
            Destroy(child.gameObject);
        }

        // קבל רשימת הקלטות
        availableRecordings = RecordingManager.Instance.GetAllRecordings();

        Debug.Log($"ReplayBrowser: נמצאו {availableRecordings.Count} הקלטות");

        if (availableRecordings.Count == 0)
        {
            // הצג הודעה שאין הקלטות
            GameObject emptyText = new GameObject("EmptyText");
            emptyText.transform.SetParent(recordingsListContent);

            RectTransform rect = emptyText.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 50);

            Text text = emptyText.AddComponent<Text>();
            text.text = "אין הקלטות זמינות";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            return;
        }

        // צור כפתור לכל הקלטה
        foreach (string recordingName in availableRecordings)
        {
            CreateRecordingButton(recordingName);
        }

        Debug.Log($"ReplayBrowser: נוצרו {availableRecordings.Count} כפתורים");
    }

    private void CreateRecordingButton(string recordingName)
    {
        GameObject buttonObj;

        if (recordingButtonPrefab != null)
        {
            buttonObj = Instantiate(recordingButtonPrefab, recordingsListContent);
        }
        else
        {
            // צור כפתור בסיסי
            buttonObj = new GameObject(recordingName);
            buttonObj.transform.SetParent(recordingsListContent);

            Image img = buttonObj.AddComponent<Image>();
            Button btn = buttonObj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = FormatRecordingName(recordingName);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        // ← חשוב! עדכן את הטקסט גם אם יש Prefab
        Text[] allTexts = buttonObj.GetComponentsInChildren<Text>();
        foreach (Text txt in allTexts)
        {
            txt.text = FormatRecordingName(recordingName);
        }

        // גם ל-TextMeshPro אם יש
        TMPro.TextMeshProUGUI[] allTMPs = buttonObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmp in allTMPs)
        {
            tmp.text = FormatRecordingName(recordingName);
        }

        // הוסף Listener לכפתור
        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(() => OnRecordingSelected(recordingName));
    }

    // פונקציה שממירה שם קובץ לפורמט יפה
    private string FormatRecordingName(string fileName)
    {
        // שם הקובץ: "Recording_20251215_183136.json"
        // נרצה: "15/12/2024 18:31"

        try
        {
            // הסר .json
            string nameWithoutExt = fileName.Replace(".json", "");

            // פצל לפי _
            string[] parts = nameWithoutExt.Split('_');

            if (parts.Length >= 3)
            {
                string datePart = parts[1]; // "20251215"
                string timePart = parts[2]; // "183136"

                // חלץ תאריך
                string year = datePart.Substring(0, 4);
                string month = datePart.Substring(4, 2);
                string day = datePart.Substring(6, 2);

                // חלץ שעה
                string hour = timePart.Substring(0, 2);
                string minute = timePart.Substring(2, 2);

                // פורמט יפה
                return $"{day}/{month}/{year} {hour}:{minute}";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ReplayBrowser: לא ניתן לפרסר את השם {fileName}: {e.Message}");
        }

        // אם נכשל, החזר את השם המקורי
        return fileName;
    }

    private void OnRecordingSelected(string recordingName)
    {
        Debug.Log($"ReplayBrowser: נבחרה הקלטה: {recordingName}");

        // כנס ל-Replay Mode ואז טען
        if (ReplaySceneManager.Instance != null)
        {
            ReplaySceneManager.Instance.LoadReplayAndEnter(recordingName);
        }
        else
        {
            // אם אין Scene Manager, רק תשמיע
            ReplayManager.Instance.LoadAndPlayRecording(recordingName);
        }

        // Defer the browser close by one frame so other onClick listeners on
        // the same recording button (IconButtonController's coroutines) finish
        // before the GameObject is deactivated. Without this, Unity throws
        // "Coroutine couldn't be started because the game object is inactive".
        StartCoroutine(CloseBrowserNextFrame());
    }

    private System.Collections.IEnumerator CloseBrowserNextFrame()
    {
        yield return null;
        CloseBrowser();
    }

    public void CloseBrowser()
    {
        gameObject.SetActive(false);
    }

    public void OpenBrowser()
    {
        gameObject.SetActive(true);
        RefreshRecordingsList();
    }
}