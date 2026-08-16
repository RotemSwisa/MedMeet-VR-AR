using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Normal.Realtime;

// המנהל הראשי של מערכת ההקלטה
public class RecordingManager : MonoBehaviour
{
    public static RecordingManager Instance { get; private set; }

    [Header("Settings")]
    public string recordingsFolderName = "Recordings";

    [Header("Objects to Record")]
    public List<GameObject> objectsToRecord = new List<GameObject>();
    public bool useTagSystem = false;
    public string[] objectTagsToRecord = { "RecordableObject" };

    private List<PlayerRecorder> activeRecorders = new List<PlayerRecorder>();
    private List<ObjectRecorder> activeObjectRecorders = new List<ObjectRecorder>();
    private bool isRecording = false;
    private float recordingStartTime;
    private string currentRecordingPath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        string path = GetRecordingsPath();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"RecordingManager: נוצרה תיקייה: {path}");
        }

        EnsureRecordersExist();
    }

    private void EnsureRecordersExist()
    {
        if (AuditLogRecorder.Instance == null)
        {
            GameObject auditObj = new GameObject("AuditLogRecorder");
            auditObj.AddComponent<AuditLogRecorder>();
            Debug.Log("RecordingManager: יצרתי AuditLogRecorder");
        }

        if (StatsRecorder.Instance == null)
        {
            GameObject statsObj = new GameObject("StatsRecorder");
            statsObj.AddComponent<StatsRecorder>();
            Debug.Log("RecordingManager: יצרתי StatsRecorder");
        }
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("RecordingManager: הקלטה כבר פעילה!");
            return;
        }

        RealtimeAvatar[] avatars = FindObjectsOfType<RealtimeAvatar>();

        if (avatars.Length == 0)
        {
            Debug.LogError("RecordingManager: לא נמצאו שחקנים להקלטה!");
            return;
        }

        activeRecorders.Clear();
        foreach (RealtimeAvatar avatar in avatars)
        {
            PlayerRecorder recorder = avatar.GetComponent<PlayerRecorder>();
            if (recorder == null)
            {
                recorder = avatar.gameObject.AddComponent<PlayerRecorder>();
            }

            recorder.StartRecording();
            activeRecorders.Add(recorder);
        }

        StartObjectRecording();
        RecordExistingModels(); // ✨ חדש!

        if (DynamicObjectRecorder.Instance != null)
        {
            DynamicObjectRecorder.Instance.StartRecording();
        }

        if (AuditLogRecorder.Instance != null)
        {
            AuditLogRecorder.Instance.StartRecording();
        }

        if (StatsRecorder.Instance != null)
        {
            StatsRecorder.Instance.StartRecording();
        }

        RecordPresentationState(); // ✨ חדש!

        isRecording = true;
        recordingStartTime = Time.time;

        Debug.Log($"RecordingManager: התחלת הקלטה עם {activeRecorders.Count} שחקנים, {activeObjectRecorders.Count} אובייקטים");
    }

    private void StartObjectRecording()
    {
        activeObjectRecorders.Clear();

        foreach (GameObject obj in objectsToRecord)
        {
            if (obj == null) continue;

            ObjectRecorder recorder = obj.GetComponent<ObjectRecorder>();
            if (recorder == null)
            {
                recorder = obj.AddComponent<ObjectRecorder>();
            }

            recorder.StartRecording();
            activeObjectRecorders.Add(recorder);
        }

        if (useTagSystem)
        {
            foreach (string tag in objectTagsToRecord)
            {
                try
                {
                    GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
                    foreach (GameObject obj in taggedObjects)
                    {
                        ObjectRecorder recorder = obj.GetComponent<ObjectRecorder>();
                        if (recorder == null)
                        {
                            recorder = obj.AddComponent<ObjectRecorder>();
                        }

                        if (!activeObjectRecorders.Contains(recorder))
                        {
                            recorder.StartRecording();
                            activeObjectRecorders.Add(recorder);
                        }
                    }
                }
                catch (UnityException)
                {
                    Debug.LogWarning($"RecordingManager: Tag '{tag}' לא קיים במערכת. דלג עליו.");
                }
            }
        }
    }

    // ✨ חדש! הקלטת מודלים שכבר קיימים
    private void RecordExistingModels()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            string modelName = ""; // ✨ הצהרה מקומית!

            // בדוק אם זה מודל רפואי
            if (obj.name.Contains("Heart(Clone)"))
            {
                modelName = "Heart";
            }
            else if (obj.name.Contains("Lungs(Clone)"))
            {
                modelName = "Lungs";
            }
            else if (obj.name.Contains("Brain(Clone)"))
            {
                modelName = "Brain";
            }
            else
            {
                continue; // לא מודל רפואי - דלג
            }

            // הוסף ObjectRecorder אם אין
            ObjectRecorder recorder = obj.GetComponent<ObjectRecorder>();
            if (recorder == null)
            {
                recorder = obj.AddComponent<ObjectRecorder>();

                recorder.objectID = $"Model_{modelName}";
                recorder.objectType = "MedicalModel";
                recorder.recordTransform = true;
                recorder.recordVisibility = true;

                Debug.Log($"RecordingManager: מצאתי מודל קיים: {modelName}");
            }

            if (!objectsToRecord.Contains(obj))
            {
                objectsToRecord.Add(obj);
            }

            recorder.StartRecording();

            if (!activeObjectRecorders.Contains(recorder))
            {
                activeObjectRecorders.Add(recorder);
            }

            // הקלט Spawn event עבור המודל הקיים
            if (DynamicObjectRecorder.Instance != null)
            {
                string resourcesPath = $"Medical_Models/{modelName}";
                DynamicObjectRecorder.Instance.RecordSpawn(
                    resourcesPath,
                    obj.transform.position,
                    obj.transform.rotation
                );

                Debug.Log($"RecordingManager: הקלטתי Spawn עבור מודל קיים: {modelName}");
            }
        }
    }

    // ✨ חדש! הקלטת מצב המצגת
    private void RecordPresentationState()
    {
        VRPresentationSystem presentation = FindObjectOfType<VRPresentationSystem>();
        if (presentation != null)
        {
            PresentationRecorder recorder = presentation.GetComponent<PresentationRecorder>();
            if (recorder == null)
            {
                recorder = presentation.gameObject.AddComponent<PresentationRecorder>();
            }

            recorder.StartRecording();
            Debug.Log("RecordingManager: התחלתי הקלטת מצגת");
        }
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("RecordingManager: אין הקלטה פעילה!");
            return;
        }

        isRecording = false;
        float duration = Time.time - recordingStartTime;

        RecordingData recordingData = new RecordingData
        {
            recordingName = "Recording_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"),
            duration = duration,
            frameRate = 30
        };

        foreach (PlayerRecorder recorder in activeRecorders)
        {
            recorder.StopRecording();
            recordingData.players.Add(recorder.GetRecordingData());
        }

        foreach (ObjectRecorder recorder in activeObjectRecorders)
        {
            recorder.StopRecording();

            ObjectRecordingData objData = new ObjectRecordingData
            {
                objectID = recorder.objectID,
                objectType = recorder.objectType,
                prefabPath = GetPrefabPath(recorder.gameObject),
                frames = recorder.GetRecordingData()
            };

            recordingData.objects.Add(objData);
        }

        if (DynamicObjectRecorder.Instance != null)
        {
            DynamicObjectRecorder.Instance.StopRecording();
            recordingData.dynamicEvents = DynamicObjectRecorder.Instance.GetRecordedEvents();
        }

        if (AuditLogRecorder.Instance != null)
        {
            AuditLogRecorder.Instance.StopRecording();
            recordingData.auditLog = AuditLogRecorder.Instance.GetRecordedLog();
        }

        if (StatsRecorder.Instance != null)
        {
            StatsRecorder.Instance.StopRecording();
            recordingData.statsSnapshots = StatsRecorder.Instance.GetRecordedSnapshots();
        }

        // ✨ מצגת
        VRPresentationSystem presentation = FindObjectOfType<VRPresentationSystem>();
        if (presentation != null)
        {
            PresentationRecorder recorder = presentation.GetComponent<PresentationRecorder>();
            if (recorder != null)
            {
                recorder.StopRecording();
                recordingData.presentationFrames = recorder.GetRecordingData();
                Debug.Log($"RecordingManager: שמרתי {recordingData.presentationFrames.Count} פריימי מצגת");
            }
        }

        SaveRecording(recordingData);

        Debug.Log($"RecordingManager: ההקלטה הסתיימה. משך: {duration:F2} שניות");
    }

    private string GetPrefabPath(GameObject obj)
    {
        if (obj.name.Contains("Heart")) return "Medical_Models/Heart";
        if (obj.name.Contains("Lungs")) return "Medical_Models/Lungs";
        if (obj.name.Contains("Brain")) return "Medical_Models/Brain";

        DocumentItem docItem = obj.GetComponent<DocumentItem>();
        if (docItem != null) return "DocumentPrefab";

        DrawingDot dot = obj.GetComponent<DrawingDot>();
        if (dot != null) return "DrawingDot";

        return "";
    }

    private void SaveRecording(RecordingData data)
    {
        string json = JsonUtility.ToJson(data, true);
        string fileName = data.recordingName + ".json";
        currentRecordingPath = Path.Combine(GetRecordingsPath(), fileName);

        try
        {
            File.WriteAllText(currentRecordingPath, json);
            Debug.Log($"RecordingManager: ההקלטה נשמרה ב: {currentRecordingPath}");
            Debug.Log($"גודל קובץ: {(new FileInfo(currentRecordingPath).Length / 1024f):F2} KB");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RecordingManager: שגיאה בשמירת הקובץ: {e.Message}");
        }
    }

    public RecordingData LoadRecording(string fileName)
    {
        string path = Path.Combine(GetRecordingsPath(), fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"RecordingManager: הקובץ לא נמצא: {path}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            RecordingData data = JsonUtility.FromJson<RecordingData>(json);
            Debug.Log($"RecordingManager: ההקלטה נטענה: {fileName}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RecordingManager: שגיאה בטעינת הקובץ: {e.Message}");
            return null;
        }
    }

    public List<string> GetAllRecordings()
    {
        string path = GetRecordingsPath();
        List<string> recordings = new List<string>();

        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.json");
            foreach (string file in files)
            {
                recordings.Add(Path.GetFileName(file));
            }
        }

        return recordings;
    }

    private string GetRecordingsPath()
    {
        return Path.Combine(Application.persistentDataPath, recordingsFolderName);
    }

    public bool IsRecording => isRecording;
    public string GetRecordingsDirectory() => GetRecordingsPath();
}