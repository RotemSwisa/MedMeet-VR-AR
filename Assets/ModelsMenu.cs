using UnityEngine;
using Normal.Realtime;
using Debug = UnityEngine.Debug;

public class ModelsMenu : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject MenuPanel;

    [Header("Main Sidebar")]
    public GameObject SidebarPanel;

    [Header("Spawn Points (Drag Transforms here)")]
    public Transform spawnPointHeart;
    public Transform spawnPointLungs;
    public Transform spawnPointBrain;

    private GameObject _currentHeart;
    private GameObject _currentLungs;
    private GameObject _currentBrain;

    void Update()
    {
        if (SidebarPanel != null && !SidebarPanel.activeSelf && MenuPanel.activeSelf)
        {
            MenuPanel.SetActive(false);
        }
    }

    public void ToggleMenu()
    {
        if (MenuPanel != null)
            MenuPanel.SetActive(!MenuPanel.activeSelf);
    }

    // -------------------- כפתורי ה-UI --------------------

    public void ShowHeart()
    {
        if (_currentHeart != null)
        {
            RemoveObjectRecorder(_currentHeart, "Heart");
            Realtime.Destroy(_currentHeart);
            _currentHeart = null;
            RecordAction("Heart", false, Vector3.zero, Quaternion.identity);
        }
        else
        {
            _currentHeart = Realtime.Instantiate(
                "Medical_Models/Heart",
                spawnPointHeart.position,
                spawnPointHeart.rotation,
                new Realtime.InstantiateOptions
                {
                    ownedByClient = true,
                    preventOwnershipTakeover = false
                });

            // ✨ תיקון קריטי: ID פשוט וקבוע!
            AddObjectRecorderToModel(_currentHeart, "Heart");
            RecordAction("Heart", true, spawnPointHeart.position, spawnPointHeart.rotation);
        }
    }

    public void ShowLungs()
    {
        if (_currentLungs != null)
        {
            RemoveObjectRecorder(_currentLungs, "Lungs");
            Realtime.Destroy(_currentLungs);
            _currentLungs = null;
            RecordAction("Lungs", false, Vector3.zero, Quaternion.identity);
        }
        else
        {
            _currentLungs = Realtime.Instantiate(
                "Medical_Models/Lungs",
                spawnPointLungs.position,
                spawnPointLungs.rotation,
                new Realtime.InstantiateOptions
                {
                    ownedByClient = true,
                    preventOwnershipTakeover = false
                });

            AddObjectRecorderToModel(_currentLungs, "Lungs");
            RecordAction("Lungs", true, spawnPointLungs.position, spawnPointLungs.rotation);
        }
    }

    public void ShowBrain()
    {
        if (_currentBrain != null)
        {
            RemoveObjectRecorder(_currentBrain, "Brain");
            Realtime.Destroy(_currentBrain);
            _currentBrain = null;
            RecordAction("Brain", false, Vector3.zero, Quaternion.identity);
        }
        else
        {
            _currentBrain = Realtime.Instantiate(
                "Medical_Models/Brain",
                spawnPointBrain.position,
                spawnPointBrain.rotation,
                new Realtime.InstantiateOptions
                {
                    ownedByClient = true,
                    preventOwnershipTakeover = false
                });

            AddObjectRecorderToModel(_currentBrain, "Brain");
            RecordAction("Brain", true, spawnPointBrain.position, spawnPointBrain.rotation);
        }
    }

    // ✨ תיקון קריטי! ID פשוט ללא InstanceID
    void AddObjectRecorderToModel(GameObject model, string modelName)
    {
        if (model == null) return;

        // בדוק אם הקלטה פעילה
        if (RecordingManager.Instance == null || !RecordingManager.Instance.IsRecording)
        {
            return;
        }

        // הוסף ObjectRecorder
        ObjectRecorder recorder = model.GetComponent<ObjectRecorder>();
        if (recorder == null)
        {
            recorder = model.AddComponent<ObjectRecorder>();
        }

        // ✨ ID פשוט וקבוע - ללא InstanceID!
        recorder.objectID = $"Model_{modelName}";
        recorder.objectType = "MedicalModel";
        recorder.recordTransform = true;
        recorder.recordVisibility = true;

        // הוסף לרשימת אובייקטים להקלטה
        if (!RecordingManager.Instance.objectsToRecord.Contains(model))
        {
            RecordingManager.Instance.objectsToRecord.Add(model);
        }

        // התחל הקלטה
        recorder.StartRecording();

        Debug.Log($"ModelsMenu: הוספתי ObjectRecorder ל-{modelName} עם ID: {recorder.objectID}");
    }

    void RemoveObjectRecorder(GameObject model, string modelName)
    {
        if (model == null) return;

        ObjectRecorder recorder = model.GetComponent<ObjectRecorder>();
        if (recorder != null)
        {
            recorder.StopRecording();

            if (RecordingManager.Instance != null)
            {
                RecordingManager.Instance.objectsToRecord.Remove(model);
            }

            Debug.Log($"ModelsMenu: הסרתי ObjectRecorder מ-{modelName}");
        }
    }

    private void RecordAction(string logicalName, bool isCreated, Vector3 position, Quaternion rotation)
    {
        if (DynamicObjectRecorder.Instance == null) return;

        string resourcesPath = $"Medical_Models/{logicalName}";

        if (isCreated)
        {
            DynamicObjectRecorder.Instance.RecordSpawn(
                resourcesPath,
                position,
                rotation
            );
        }
        else
        {
            DynamicObjectRecorder.Instance.RecordDestroy(resourcesPath);
        }
    }
}