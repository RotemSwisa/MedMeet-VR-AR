using UnityEngine;
using Normal.Realtime;

public class DrawingDot : MonoBehaviour
{
    private RealtimeView realtimeView;
    private MeshRenderer meshRenderer;
    private RealtimeDotSync dotSync;

    void Awake()
    {
        realtimeView = GetComponent<RealtimeView>();
        meshRenderer = GetComponent<MeshRenderer>();
        dotSync = GetComponent<RealtimeDotSync>();
    }

    void Start()
    {
        if (realtimeView != null && !realtimeView.isOwnedLocallySelf)
        {
            realtimeView.RequestOwnership();
        }

        // Hide dots - only show the line
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    public void SetColor(Color color)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = color;
        }
    }

    public void SetSize(float size)
    {
        transform.localScale = Vector3.one * size;
    }

    public void SetStrokeInfo(int id, int index)
    {
        if (dotSync != null)
        {
            dotSync.SetStrokeInfo(id, index);
        }
        else
        {
            // Fallback to name
            gameObject.name = $"Dot_S{id}_P{index}";
        }
    }

    public int GetStrokeID()
    {
        if (dotSync != null)
        {
            return dotSync.strokeID;
        }

        // Fallback: try to parse from name
        try
        {
            string name = gameObject.name;
            int startIndex = name.IndexOf("_S") + 2;
            int endIndex = name.IndexOf("_P");
            if (startIndex > 1 && endIndex > startIndex)
            {
                string idStr = name.Substring(startIndex, endIndex - startIndex);
                return int.Parse(idStr);
            }
        }
        catch { }
        return 0;
    }

    public int GetPointIndex()
    {
        if (dotSync != null)
        {
            return dotSync.pointIndex;
        }

        // Fallback: try to parse from name
        try
        {
            string name = gameObject.name;
            int startIndex = name.IndexOf("_P") + 2;
            if (startIndex > 1)
            {
                string indexStr = name.Substring(startIndex);
                return int.Parse(indexStr);
            }
        }
        catch { }
        return 0;
    }
}