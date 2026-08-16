using UnityEngine;
using Normal.Realtime;

public class DocumentSyncAsset : RealtimeComponent<DocumentSyncModel>
{
    private DocumentItem documentItem;

    void Awake()
    {
        documentItem = GetComponent<DocumentItem>();
    }

    protected override void OnRealtimeModelReplaced(DocumentSyncModel previousModel, DocumentSyncModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.documentDataDidChange -= OnDocumentDataChanged;
        }

        if (currentModel != null)
        {
            currentModel.documentDataDidChange += OnDocumentDataChanged;

            // אם יש מידע, נטען אותו
            if (!string.IsNullOrEmpty(currentModel.documentData) && documentItem != null)
            {
                LoadDocumentFromBase64(currentModel.documentData, currentModel.fileName, currentModel.isPDF);
            }
        }
    }

    // שליחת המסמך לכולם
    public void ShareDocument(byte[] imageBytes, string fileName, bool isPDF)
    {
        if (model == null)
        {
            Debug.LogWarning("Model is null - cannot share document");
            return;
        }

        // המרה ל-Base64 (פשוט לשליחה)
        string base64 = System.Convert.ToBase64String(imageBytes);

        model.documentData = base64;
        model.fileName = fileName;
        model.isPDF = isPDF;

        Debug.Log($"Shared document: {fileName} ({imageBytes.Length} bytes)");
    }

    void OnDocumentDataChanged(DocumentSyncModel model, string base64Data)
    {
        if (string.IsNullOrEmpty(base64Data)) return;

        Debug.Log("Received document data change - loading...");
        LoadDocumentFromBase64(base64Data, model.fileName, model.isPDF);
    }

    void LoadDocumentFromBase64(string base64, string fileName, bool isPDF)
    {
        try
        {
            byte[] imageBytes = System.Convert.FromBase64String(base64);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            if (documentItem != null)
            {
                documentItem.pages = new Texture2D[] { texture };
                documentItem.fileName = fileName;
                documentItem.isPDF = isPDF;
                documentItem.ShowPage(0);

                Debug.Log($"Document loaded: {fileName}");
            }
            else
            {
                Debug.LogWarning("DocumentItem is null!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load document: " + e.Message);
        }
    }
}