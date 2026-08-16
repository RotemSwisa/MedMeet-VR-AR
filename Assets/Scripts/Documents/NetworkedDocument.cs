using UnityEngine;
using Normal.Realtime;

public class NetworkedDocument : RealtimeComponent<DocumentModel>
{
    private DocumentItem documentItem;
    private bool isUpdatingFromNetwork = false;
    private float syncInterval = 0.1f;
    private float lastSyncTime = 0f;

    void Awake()
    {
        documentItem = GetComponent<DocumentItem>();
    }

    protected override void OnRealtimeModelReplaced(DocumentModel previousModel, DocumentModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.positionDidChange -= OnPositionChanged;
            previousModel.rotationDidChange -= OnRotationChanged;
            previousModel.scaleDidChange -= OnScaleChanged;
            previousModel.currentPageDidChange -= OnPageChanged;
        }

        if (currentModel != null)
        {
            if (currentModel.isFreshModel)
            {
                model.position = transform.position;
                model.rotation = transform.rotation;
                model.scale = transform.localScale;
                model.currentPage = 0;
            }
            else
            {
                UpdateFromModel();
            }

            currentModel.positionDidChange += OnPositionChanged;
            currentModel.rotationDidChange += OnRotationChanged;
            currentModel.scaleDidChange += OnScaleChanged;
            currentModel.currentPageDidChange += OnPageChanged;
        }
    }

    void Update()
    {
        if (model == null || isUpdatingFromNetwork) return;

        // סנכרון כל X זמן (למניעת עומס)
        if (Time.time - lastSyncTime < syncInterval) return;
        lastSyncTime = Time.time;

        if (Vector3.Distance(transform.position, model.position) > 0.01f)
            model.position = transform.position;

        if (Quaternion.Angle(transform.rotation, model.rotation) > 0.5f)
            model.rotation = transform.rotation;

        if (Vector3.Distance(transform.localScale, model.scale) > 0.01f)
            model.scale = transform.localScale;

        if (documentItem != null && documentItem.currentPage != model.currentPage)
            model.currentPage = documentItem.currentPage;
    }

    void UpdateFromModel()
    {
        isUpdatingFromNetwork = true;
        transform.position = model.position;
        transform.rotation = model.rotation;
        transform.localScale = model.scale;
        if (documentItem != null && documentItem.pages != null && documentItem.pages.Length > 0)
        {
            documentItem.currentPage = model.currentPage;
            // קריאה ל-ShowPage מבלי לגרום לשגיאה
            if (model.currentPage < documentItem.pages.Length)
                documentItem.SetPageFromNetwork(model.currentPage);
        }
        isUpdatingFromNetwork = false;
    }

    void OnPositionChanged(DocumentModel model, Vector3 value)
    {
        if (!isUpdatingFromNetwork)
        {
            isUpdatingFromNetwork = true;
            transform.position = value;
            isUpdatingFromNetwork = false;
        }
    }

    void OnRotationChanged(DocumentModel model, Quaternion value)
    {
        if (!isUpdatingFromNetwork)
        {
            isUpdatingFromNetwork = true;
            transform.rotation = value;
            isUpdatingFromNetwork = false;
        }
    }

    void OnScaleChanged(DocumentModel model, Vector3 value)
    {
        if (!isUpdatingFromNetwork)
        {
            isUpdatingFromNetwork = true;
            transform.localScale = value;
            isUpdatingFromNetwork = false;
        }
    }

    void OnPageChanged(DocumentModel model, int value)
    {
        if (documentItem != null && documentItem.currentPage != value && !isUpdatingFromNetwork)
        {
            isUpdatingFromNetwork = true;
            documentItem.SetPageFromNetwork(value);
            isUpdatingFromNetwork = false;
        }
    }
}