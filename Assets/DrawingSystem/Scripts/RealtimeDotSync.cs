using UnityEngine;
using Normal.Realtime;

// Simple sync using public fields
public class RealtimeDotSync : MonoBehaviour
{
    // Public fields that will be synced manually
    public int strokeID = 0;
    public int pointIndex = 0;

    private bool hasSetInfo = false;

    void Start()
    {
        // Wait a bit for sync, then update name
        Invoke(nameof(UpdateName), 0.2f);
    }

    public void SetStrokeInfo(int id, int index)
    {
        strokeID = id;
        pointIndex = index;
        hasSetInfo = true;
        UpdateName();
    }

    void UpdateName()
    {
        if (strokeID != 0 || hasSetInfo)
        {
            gameObject.name = $"Dot_S{strokeID}_P{pointIndex}";
        }
    }
}