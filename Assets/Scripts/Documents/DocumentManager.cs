using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Normal.Realtime;

public class DocumentManager : MonoBehaviour
{
    public static DocumentManager Instance;

    [Header("Prefabs")]
    public GameObject documentPrefab;

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public float spawnDistance = 2f;

    [Header("File Picker")]
    public FilePickerUI filePickerUI;

    [Header("Supported Formats")]
    public string[] supportedImageFormats = { ".jpg", ".jpeg", ".png" };
    public string[] supportedPDFFormats = { ".pdf" };

    private List<GameObject> activeDocuments = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameObject.AddComponent<PDFLoader>();
    }

    void Update()
    {
        activeDocuments.RemoveAll(doc => doc == null);
    }

    public void OpenFilePicker()
    {
        Debug.Log("OpenFilePicker called!");

        if (filePickerUI != null)
        {
            Debug.Log("Opening FilePickerUI...");
            filePickerUI.ShowFilePicker((filePath) => {
                Debug.Log("File selected from UI: " + filePath);
                if (!string.IsNullOrEmpty(filePath))
                    StartCoroutine(LoadDocument(filePath));
            });
        }
        else
        {
            Debug.LogError("FilePickerUI is NULL! Please assign it in Inspector.");
        }
    }

    IEnumerator LoadDocument(string filePath)
    {
        Debug.Log("Loading document: " + filePath);

        string extension = Path.GetExtension(filePath).ToLower();
        string fileName = Path.GetFileName(filePath);

        if (System.Array.Exists(supportedImageFormats, ext => ext == extension))
        {
            yield return StartCoroutine(LoadImage(filePath, fileName));
        }
        else if (System.Array.Exists(supportedPDFFormats, ext => ext == extension))
        {
            yield return StartCoroutine(LoadPDF(filePath, fileName));
        }
        else
        {
            Debug.LogWarning("Unsupported file format: " + extension);
        }
    }

    IEnumerator LoadImage(string path, string fileName)
    {
        Debug.Log("Loading image: " + path);

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        SpawnDocument(new Texture2D[] { texture }, fileName, false);

        yield return null;
    }

    IEnumerator LoadPDF(string path, string fileName)
    {
        Debug.Log("Loading PDF: " + fileName);

        byte[] pdfBytes = File.ReadAllBytes(path);
        bool completed = false;
        Texture2D[] pdfPages = null;

        yield return StartCoroutine(PDFLoader.LoadPDFPages(pdfBytes, (pages) => {
            pdfPages = pages;
            completed = true;
        }));

        while (!completed)
            yield return null;

        if (pdfPages != null && pdfPages.Length > 0)
        {
            SpawnDocument(pdfPages, fileName, true);
            Debug.Log($"PDF loaded: {pdfPages.Length} pages");
        }
        else
        {
            Debug.LogError("Failed to load PDF");
        }
    }

    void SpawnDocument(Texture2D[] pages, string fileName, bool isPDF)
    {
        Normal.Realtime.Realtime realtimeInstance = FindObjectOfType<Normal.Realtime.Realtime>();

        Vector3 spawnPos = Camera.main.transform.position +
                          Camera.main.transform.forward * spawnDistance;
        Quaternion spawnRot = Quaternion.LookRotation(Camera.main.transform.forward);

        // ✨ הקלטה! (רק אם הקלטה פעילה)
        if (DynamicObjectRecorder.Instance != null && DynamicObjectRecorder.Instance.IsRecording)
        {
            DynamicObjectRecorder.Instance.RecordDocumentSpawn(
                pages[0],
                fileName,
                isPDF,
                pages.Length,
                spawnPos,
                spawnRot
            );
        }

        if (realtimeInstance == null)
        {
            Debug.LogError("Realtime not found in scene! Cannot sync documents.");
            SpawnDocumentLocal(pages, fileName, isPDF, spawnPos, spawnRot);
            return;
        }

        GameObject doc = Normal.Realtime.Realtime.Instantiate(
            documentPrefab.name,
            position: spawnPos,
            rotation: spawnRot,
            options: new Normal.Realtime.Realtime.InstantiateOptions
            {
                ownedByClient = true,
                preventOwnershipTakeover = false,
                destroyWhenOwnerLeaves = false,
                useInstance = realtimeInstance
            }
        );

        if (doc == null)
        {
            Debug.LogError("Failed to instantiate document via Realtime!");
            return;
        }

        DocumentItem docItem = doc.GetComponent<DocumentItem>();
        DocumentSyncAsset syncAsset = doc.GetComponent<DocumentSyncAsset>();

        if (docItem != null)
        {
            docItem.pages = pages;
            docItem.fileName = fileName;
            docItem.isPDF = isPDF;
            docItem.ShowPage(0);

            if (syncAsset != null)
            {
                StartCoroutine(ShareDocumentDelayed(syncAsset, pages[0], fileName, isPDF));
            }
            else
            {
                Debug.LogWarning("DocumentSyncAsset not found on prefab!");
            }
        }

        // ✨ הוסף ObjectRecorder כדי להקליט תזוזות
        AddObjectRecorderToDocument(doc, fileName);

        activeDocuments.Add(doc);
        Debug.Log($"Document spawned: {fileName}");
    }

    void SpawnDocumentLocal(Texture2D[] pages, string fileName, bool isPDF, Vector3 pos, Quaternion rot)
    {
        GameObject doc = Instantiate(documentPrefab, pos, rot);

        DocumentItem docItem = doc.GetComponent<DocumentItem>();
        docItem.pages = pages;
        docItem.fileName = fileName;
        docItem.isPDF = isPDF;
        docItem.ShowPage(0);

        // ✨ הוסף ObjectRecorder גם לתמונות מקומיות
        AddObjectRecorderToDocument(doc, fileName);

        activeDocuments.Add(doc);

        Debug.LogWarning("Document created locally only (Realtime not available)");
    }

    // ✨ הוספת ObjectRecorder לתמונה
    // ✨ הוספת ObjectRecorder לתמונה
    void AddObjectRecorderToDocument(GameObject doc, string fileName)
    {
        // בדוק אם הקלטה פעילה
        if (RecordingManager.Instance == null || !RecordingManager.Instance.IsRecording)
        {
            return;
        }

        // הוסף ObjectRecorder
        ObjectRecorder recorder = doc.GetComponent<ObjectRecorder>();
        if (recorder == null)
        {
            recorder = doc.AddComponent<ObjectRecorder>();
        }

        // ✨ תיקון: ID יחודי עם Instance ID!
        recorder.objectID = $"Document_{fileName}_{doc.GetInstanceID()}";
        recorder.objectType = "Document";
        recorder.recordTransform = true;
        recorder.recordVisibility = true;

        // ✨ תיקון: הוסף לרשימת האובייקטים להקלטה!
        if (!RecordingManager.Instance.objectsToRecord.Contains(doc))
        {
            RecordingManager.Instance.objectsToRecord.Add(doc);
        }

        // התחל הקלטה
        recorder.StartRecording();

        Debug.Log($"DocumentManager: הוספתי ObjectRecorder ל-{fileName} עם ID: {recorder.objectID}");
    }

    IEnumerator ShareDocumentDelayed(DocumentSyncAsset syncAsset, Texture2D texture, string fileName, bool isPDF)
    {
        yield return new WaitForSeconds(0.5f);

        byte[] imageBytes = texture.EncodeToPNG();
        syncAsset.ShareDocument(imageBytes, fileName, isPDF);

        Debug.Log("Document shared with all players!");
    }

    public void CloseAllDocuments()
    {
        foreach (var doc in activeDocuments)
        {
            if (doc != null) Destroy(doc);
        }
        activeDocuments.Clear();
    }
}