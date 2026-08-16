using System.Collections.Generic;
using UnityEngine;

// משחזר אירועי Spawn/Destroy + תמונות + ציורים
public class DynamicObjectReplayer : MonoBehaviour
{
    private List<DynamicObjectEvent> events;
    private int currentEventIndex = 0;
    private bool isPlaying = false;
    private float playbackTime = 0f;
    private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();

    public void Initialize(List<DynamicObjectEvent> dynamicEvents)
    {
        events = dynamicEvents;
        Debug.Log($"DynamicObjectReplayer: אותחל עם {events.Count} אירועים");
    }

    public void StartPlayback()
    {
        isPlaying = true;
        playbackTime = 0f;
        currentEventIndex = 0;
        spawnedObjects.Clear();

        // ✨ נקה ציורים ישנים!
        ClearOldDrawings();

        Debug.Log("DynamicObjectReplayer: התחלת שחזור");
    }

    public void StopPlayback()
    {
        isPlaying = false;

        foreach (var obj in spawnedObjects.Values)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    public void PausePlayback()
    {
        isPlaying = false;
    }

    public void ResumePlayback()
    {
        isPlaying = true;
    }

    void Update()
    {
        if (!isPlaying || events == null || events.Count == 0) return;

        playbackTime += Time.deltaTime;

        while (currentEventIndex < events.Count && events[currentEventIndex].time <= playbackTime)
        {
            ProcessEvent(events[currentEventIndex]);
            currentEventIndex++;
        }
    }

    private void ProcessEvent(DynamicObjectEvent evt)
    {
        if (evt.eventType == DynamicEventType.Spawn)
        {
            GameObject prefab = Resources.Load<GameObject>(evt.prefabName);
            if (prefab != null)
            {
                GameObject spawned = Instantiate(prefab, evt.position, evt.rotation);

                // ✨ תיקון קריטי! ID זהה למה שב-ModelsMenu
                AddObjectRecorderToSpawnedObject(spawned, evt.prefabName);

                spawnedObjects[evt.prefabName] = spawned;
                Debug.Log($"DynamicObjectReplayer: Spawned '{evt.prefabName}' at {playbackTime:F2}s");
            }
            else
            {
                Debug.LogError($"DynamicObjectReplayer: Prefab '{evt.prefabName}' לא נמצא ב-Resources!");
            }
        }
        else if (evt.eventType == DynamicEventType.DocumentSpawn)
        {
            SpawnDocument(evt);
        }
        else if (evt.eventType == DynamicEventType.Destroy)
        {
            if (spawnedObjects.ContainsKey(evt.prefabName))
            {
                GameObject obj = spawnedObjects[evt.prefabName];
                if (obj != null)
                {
                    Destroy(obj);
                }
                spawnedObjects.Remove(evt.prefabName);
                Debug.Log($"DynamicObjectReplayer: Destroyed '{evt.prefabName}' at {playbackTime:F2}s");
            }
        }
        else if (evt.eventType == DynamicEventType.DrawingDotSpawn)
        {
            SpawnDrawingDot(evt);
        }
    }

    // ✨ נקה ציורים ישנים בתחילת הריפליי
    void ClearOldDrawings()
    {
        DrawingDot[] oldDots = FindObjectsOfType<DrawingDot>();
        foreach (DrawingDot dot in oldDots)
        {
            Destroy(dot.gameObject);
        }

        StrokeLineRenderer[] oldLines = FindObjectsOfType<StrokeLineRenderer>();
        foreach (StrokeLineRenderer line in oldLines)
        {
            Destroy(line.gameObject);
        }

        Debug.Log($"DynamicObjectReplayer: ניקיתי {oldDots.Length} נקודות ו-{oldLines.Length} קווים ישנים");
    }

    // ✨ תיקון קריטי! ID זהה למה שב-ModelsMenu
    void AddObjectRecorderToSpawnedObject(GameObject obj, string prefabName)
    {
        if (obj == null) return;

        ObjectRecorder recorder = obj.GetComponent<ObjectRecorder>();
        if (recorder == null)
        {
            recorder = obj.AddComponent<ObjectRecorder>();
        }

        // ✨ ID פשוט - זהה למה שב-ModelsMenu!
        string modelName = prefabName.Replace("Medical_Models/", "");
        recorder.objectID = $"Model_{modelName}"; // ללא InstanceID!
        recorder.objectType = "MedicalModel";
        recorder.recordTransform = false; // לא להקליט ב-Replay!
        recorder.recordVisibility = false;

        Debug.Log($"DynamicObjectReplayer: ObjectRecorder ל-{prefabName} עם ID: {recorder.objectID}");
    }

    private void SpawnDocument(DynamicObjectEvent evt)
    {
        GameObject prefab = Resources.Load<GameObject>("DocumentPrefab");
        if (prefab == null)
        {
            Debug.LogError("DynamicObjectReplayer: DocumentPrefab לא נמצא ב-Resources!");
            return;
        }

        GameObject doc = Instantiate(prefab, evt.position, evt.rotation);

        byte[] imageBytes = System.Convert.FromBase64String(evt.textureBase64);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        DocumentItem docItem = doc.GetComponent<DocumentItem>();
        if (docItem != null)
        {
            docItem.pages = new Texture2D[] { texture };
            docItem.fileName = evt.fileName;
            docItem.isPDF = evt.isPDF;
            docItem.ShowPage(0);

            Debug.Log($"DynamicObjectReplayer: DocumentSpawn '{evt.fileName}' at {playbackTime:F2}s");
        }

        string key = $"Document_{evt.fileName}_{evt.time}";
        spawnedObjects[key] = doc;
    }

    private void SpawnDrawingDot(DynamicObjectEvent evt)
    {
        GameObject prefab = Resources.Load<GameObject>(evt.prefabName);
        if (prefab == null)
        {
            Debug.LogError($"DynamicObjectReplayer: DrawingDot Prefab לא נמצא ב-Resources!");
            return;
        }

        GameObject dotObj = Instantiate(prefab, evt.position, evt.rotation);

        DrawingDot dot = dotObj.GetComponent<DrawingDot>();
        if (dot != null)
        {
            dot.SetColor(evt.dotColor);
            dot.SetSize(evt.dotSize);
            dot.SetStrokeInfo(evt.strokeID, evt.pointIndex);
        }

        CreateOrUpdateStrokeLine(evt.strokeID, evt.dotColor, evt.dotSize);

        string key = $"DrawingDot_S{evt.strokeID}_P{evt.pointIndex}";
        spawnedObjects[key] = dotObj;
    }

    void CreateOrUpdateStrokeLine(int strokeID, Color color, float width)
    {
        StrokeLineRenderer[] existingLines = FindObjectsOfType<StrokeLineRenderer>();
        foreach (StrokeLineRenderer line in existingLines)
        {
            if (line.gameObject.name == $"StrokeLine_{strokeID}")
            {
                return;
            }
        }

        GameObject lineObj = new GameObject($"StrokeLine_{strokeID}");
        StrokeLineRenderer lineRenderer = lineObj.AddComponent<StrokeLineRenderer>();
        lineRenderer.Initialize(strokeID, color, width);

        Debug.Log($"DynamicObjectReplayer: קו חדש Stroke {strokeID}");
    }

    public bool IsPlaybackComplete()
    {
        if (events == null || events.Count == 0) return true;
        return currentEventIndex >= events.Count;
    }
}