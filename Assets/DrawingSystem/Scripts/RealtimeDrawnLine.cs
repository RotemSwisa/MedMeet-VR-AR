using UnityEngine;
using Normal.Realtime;

// Simple version without RealtimeModel - just sync transform and properties manually
public class RealtimeDrawnLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private RealtimeView realtimeView;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        realtimeView = GetComponent<RealtimeView>();
    }

    public void SetLineProperties(Color color, float width)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    public bool IsOwnedByMe()
    {
        return realtimeView != null && realtimeView.isOwnedLocallySelf;
    }
}