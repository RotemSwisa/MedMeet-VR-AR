using UnityEngine;
using Normal.Realtime;

public class DrawnLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private RealtimeView realtimeView;
    private RealtimeTransform realtimeTransform;
    private RealtimeDrawnLine realtimeDrawnLine;
    private BoxCollider boxCollider;

    // Store initial properties to sync
    [System.NonSerialized]
    public Color lineColor = Color.blue;
    [System.NonSerialized]
    public float lineWidth = 0.01f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        boxCollider = GetComponent<BoxCollider>();

        // Get or add Realtime components
        realtimeView = GetComponent<RealtimeView>();
        if (realtimeView == null)
        {
            realtimeView = gameObject.AddComponent<RealtimeView>();
        }

        realtimeTransform = GetComponent<RealtimeTransform>();
        if (realtimeTransform == null)
        {
            realtimeTransform = gameObject.AddComponent<RealtimeTransform>();
            realtimeTransform.syncPosition = false; // Lines don't move
            realtimeTransform.syncRotation = false;
            realtimeTransform.syncScale = false;
        }

        realtimeDrawnLine = GetComponent<RealtimeDrawnLine>();
        if (realtimeDrawnLine == null)
        {
            realtimeDrawnLine = gameObject.AddComponent<RealtimeDrawnLine>();
        }

        // Make sure collider exists
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
        }
    }

    void Start()
    {
        // Request ownership when created
        if (realtimeView != null && !realtimeView.isOwnedLocallySelf)
        {
            realtimeView.RequestOwnership();
        }

        // Apply initial color/width after a short delay (to ensure sync)
        Invoke(nameof(ApplyInitialProperties), 0.1f);
    }

    void ApplyInitialProperties()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }
    }

    void LateUpdate()
    {
        // Update collider to match line bounds
        if (lineRenderer != null && boxCollider != null && lineRenderer.positionCount > 1)
        {
            UpdateColliderBounds();
        }
    }

    void UpdateColliderBounds()
    {
        // Calculate bounds from line points
        Bounds bounds = new Bounds(lineRenderer.GetPosition(0), Vector3.zero);

        for (int i = 1; i < lineRenderer.positionCount; i++)
        {
            bounds.Encapsulate(lineRenderer.GetPosition(i));
        }

        // Add padding based on line width
        float padding = Mathf.Max(lineRenderer.startWidth * 2f, 0.05f);
        bounds.Expand(padding);

        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
    }

    public void SetLineProperties(Color color, float width)
    {
        lineColor = color;
        lineWidth = width;

        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        // Sync via RealtimeDrawnLine
        if (realtimeDrawnLine != null)
        {
            realtimeDrawnLine.SetLineProperties(color, width);
        }
    }
}